using System.Collections.Generic;
using Avalonia.Media.TextFormatting.Unicode;
using Avalonia.Utilities;

namespace Avalonia.Media.TextFormatting
{
    /// <summary>
    /// A text run that holds text characters.
    /// </summary>
    public class TextCharacters : TextRun
    {
        public TextCharacters(ReadOnlySlice<char> text, TextRunProperties properties)
        {
            TextSourceLength = text.Length;
            Text = text;
            Properties = properties;
        }

        public TextCharacters(ReadOnlySlice<char> text, int offsetToFirstCharacter, int length,
            TextRunProperties properties)
        {
            Text = text.Skip(offsetToFirstCharacter).Take(length);
            TextSourceLength = length;
            Properties = properties;
        }

        /// <inheritdoc />
        public override int TextSourceLength { get; }

        /// <inheritdoc />
        public override ReadOnlySlice<char> Text { get; }

        /// <inheritdoc />
        public override TextRunProperties Properties { get; }

        /// <summary>
        /// Gets a list of <see cref="ShapeableTextCharacters"/>.
        /// </summary>
        /// <returns>The shapeable text characters.</returns>
        internal IList<ShapeableTextCharacters> GetShapeableCharacters()
        {
            var shapeableCharacters = new List<ShapeableTextCharacters>(2);

            var runText = Text;

            while (!runText.IsEmpty)
            {
                var shapeableRun = CreateShapeableRun(runText, Properties);

                shapeableCharacters.Add(shapeableRun);

                runText = runText.Skip(shapeableRun.Text.Length);
            }

            return shapeableCharacters;
        }

        /// <summary>
        /// Creates a shapeable text run with unique properties.
        /// </summary>
        /// <param name="text">The text to create text runs from.</param>
        /// <param name="defaultProperties">The default text run properties.</param>
        /// <returns>A list of shapeable text runs.</returns>
        private ShapeableTextCharacters CreateShapeableRun(ReadOnlySlice<char> text, TextRunProperties defaultProperties)
        {
            var defaultTypeface = defaultProperties.Typeface;

            var currentTypeface = defaultTypeface;

            if (TryGetRunProperties(text, currentTypeface, defaultTypeface, out var count))
            {
                return new ShapeableTextCharacters(text.Take(count),
                    new GenericTextRunProperties(currentTypeface, defaultProperties.FontRenderingEmSize,
                        defaultProperties.TextDecorations, defaultProperties.ForegroundBrush));

            }

            var codepoint = Codepoint.ReadAt(text, count, out _);

            //ToDo: Fix FontFamily fallback
            var matchFound =
                FontManager.Current.TryMatchCharacter(codepoint, defaultTypeface.Style, defaultTypeface.Weight,
                    defaultTypeface.FontFamily, defaultProperties.CultureInfo, out currentTypeface);

            if (matchFound && TryGetRunProperties(text, currentTypeface, defaultTypeface, out count))
            {
                //Fallback found
                return new ShapeableTextCharacters(text.Take(count),
                    new GenericTextRunProperties(currentTypeface, defaultProperties.FontRenderingEmSize,
                    defaultProperties.TextDecorations, defaultProperties.ForegroundBrush));
            }

            // no fallback found
            currentTypeface = defaultTypeface;

            var glyphTypeface = currentTypeface.GlyphTypeface;

            var enumerator = new GraphemeEnumerator(text);

            while (enumerator.MoveNext())
            {
                var grapheme = enumerator.Current;

                if (!grapheme.FirstCodepoint.IsWhiteSpace && glyphTypeface.TryGetGlyph(grapheme.FirstCodepoint, out _))
                {
                    break;
                }

                count += grapheme.Text.Length;
            }

            return new ShapeableTextCharacters(text.Take(count),
                new GenericTextRunProperties(currentTypeface, defaultProperties.FontRenderingEmSize,
                    defaultProperties.TextDecorations, defaultProperties.ForegroundBrush));
        }

        /// <summary>
        /// Tries to get run properties.
        /// </summary>
        /// <param name="defaultTypeface"></param>
        /// <param name="text"></param>
        /// <param name="typeface">The typeface that is used to find matching characters.</param>
        /// <param name="count"></param>
        /// <returns></returns>
        protected bool TryGetRunProperties(ReadOnlySlice<char> text, Typeface typeface, Typeface defaultTypeface,
            out int count)
        {
            if (text.Length == 0)
            {
                count = 0;
                return false;
            }

            var isFallback = typeface != defaultTypeface;

            count = 0;
            var script = Script.Common;
            var direction = BiDiClass.LeftToRight;

            var font = typeface.GlyphTypeface;
            var defaultFont = defaultTypeface.GlyphTypeface;
            
            var enumerator = new GraphemeEnumerator(text);

            while (enumerator.MoveNext())
            {
                var currentGrapheme = enumerator.Current;

                var currentScript = currentGrapheme.FirstCodepoint.Script;

                var currentDirection = currentGrapheme.FirstCodepoint.BiDiClass;

                //// ToDo: Implement BiDi algorithm
                //if (currentScript.HorizontalDirection != direction)
                //{
                //    if (!UnicodeUtility.IsWhiteSpace(grapheme.FirstCodepoint))
                //    {
                //        break;
                //    }
                //}

                if (currentScript != script)
                {
                    if (script == Script.Inherited || script == Script.Common)
                    {
                        script = currentScript;
                    }
                    else
                    {
                        if (currentScript != Script.Inherited && currentScript != Script.Common)
                        {
                            break;
                        }
                    }
                }

                if (currentScript != Script.Common && currentScript != Script.Inherited)
                {
                    if (isFallback && defaultFont.TryGetGlyph(currentGrapheme.FirstCodepoint, out _))
                    {
                        break;
                    }

                    if (!font.TryGetGlyph(currentGrapheme.FirstCodepoint, out _))
                    {
                        break;
                    }
                }

                if (!currentGrapheme.FirstCodepoint.IsWhiteSpace && !font.TryGetGlyph(currentGrapheme.FirstCodepoint, out _))
                {
                    break;
                }

                if (direction == BiDiClass.RightToLeft && currentDirection  == BiDiClass.CommonSeparator)
                {
                    break;
                }

                count += currentGrapheme.Text.Length;
                direction = currentDirection;
            }

            return count > 0;
        }
    }
}
