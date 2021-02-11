using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Utilities;

namespace Avalonia.Media.TextFormatting
{
    /// <summary>
    /// Represents a multi line text layout.
    /// </summary>
    public class TextLayout
    {
        private static readonly char[] s_empty = { '\u200B' };

        private readonly ReadOnlySlice<char> _text;
        private readonly TextParagraphProperties _paragraphProperties;
        private readonly IReadOnlyList<ValueSpan<TextRunProperties>> _textStyleOverrides;
        private readonly TextTrimming _textTrimming;

        /// <summary>
        /// Initializes a new instance of the <see cref="TextLayout" /> class.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <param name="typeface">The typeface.</param>
        /// <param name="fontSize">Size of the font.</param>
        /// <param name="foreground">The foreground.</param>
        /// <param name="textAlignment">The text alignment.</param>
        /// <param name="textWrapping">The text wrapping.</param>
        /// <param name="textTrimming">The text trimming.</param>
        /// <param name="textDecorations">The text decorations.</param>
        /// <param name="maxWidth">The maximum width.</param>
        /// <param name="maxHeight">The maximum height.</param>
        /// <param name="lineHeight">The height of each line of text.</param>
        /// <param name="maxLines">The maximum number of text lines.</param>
        /// <param name="textStyleOverrides">The text style overrides.</param>
        public TextLayout(
            string text,
            Typeface typeface,
            double fontSize,
            IBrush foreground,
            TextAlignment textAlignment = TextAlignment.Left,
            TextWrapping textWrapping = TextWrapping.NoWrap,
            TextTrimming textTrimming = TextTrimming.None,
            TextDecorationCollection textDecorations = null,
            double maxWidth = double.PositiveInfinity,
            double maxHeight = double.PositiveInfinity,
            double lineHeight = double.NaN,
            int maxLines = 0,
            IReadOnlyList<ValueSpan<TextRunProperties>> textStyleOverrides = null)
        {
            _text = string.IsNullOrEmpty(text) ?
                new ReadOnlySlice<char>() :
                new ReadOnlySlice<char>(text.AsMemory());

            _paragraphProperties =
                CreateTextParagraphProperties(typeface, fontSize, foreground, textAlignment, textWrapping,
                    textDecorations, lineHeight);

            _textTrimming = textTrimming;

            _textStyleOverrides = textStyleOverrides;

            LineHeight = lineHeight;

            MaxWidth = maxWidth;

            MaxHeight = maxHeight;

            MaxLines = maxLines;

            UpdateLayout();
        }

        /// <summary>
        /// Gets or sets the height of each line of text.
        /// </summary>
        /// <remarks>
        /// A value of NaN (equivalent to an attribute value of "Auto") indicates that the line height
        /// is determined automatically from the current font characteristics. The default is NaN.
        /// </remarks>
        public double LineHeight { get; }

        /// <summary>
        /// Gets the maximum width.
        /// </summary>
        public double MaxWidth { get; }

        /// <summary>
        /// Gets the maximum height.
        /// </summary>
        public double MaxHeight { get; }

        /// <summary>
        /// Gets the maximum number of text lines.
        /// </summary>
        public int MaxLines { get; }

        /// <summary>
        /// Gets the text lines.
        /// </summary>
        /// <value>
        /// The text lines.
        /// </value>
        public IReadOnlyList<TextLine> TextLines { get; private set; }

        /// <summary>
        /// Gets the size of the layout.
        /// </summary>
        /// <value>
        /// The bounds.
        /// </value>
        public Size Size { get; private set; }

        /// <summary>
        /// Draws the text layout.
        /// </summary>
        /// <param name="context">The drawing context.</param>
        /// <param name="origin">The origin.</param>
        public void Draw(DrawingContext context, Point origin)
        {
            if (!TextLines.Any())
            {
                return;
            }

            var (currentX, currentY) = origin;

            foreach (var textLine in TextLines)
            {
                textLine.Draw(context, new Point(currentX + textLine.Start, currentY));

                currentY += textLine.Height;
            }
        }

        /// <summary>
        /// Get the pixel location relative to the top-left of the layout box given the text position.
        /// </summary>
        /// <param name="textPosition">The text position.</param>
        /// <returns></returns>
        public Rect HitTestTextPosition(int textPosition)
        {
            if (TextLines.Count == 0)
            {
                return new Rect();
            }

            if (textPosition < 0 || textPosition >= _text.Length)
            {
                var lastLine = TextLines[TextLines.Count - 1];

                var lineX = lastLine.Width;

                var lineY = Size.Height - lastLine.Height;

                return new Rect(lineX, lineY, 0, lastLine.Height);
            }

            var currentY = 0.0;

            foreach (var textLine in TextLines)
            {
                if (textLine.TextRange.End < textPosition)
                {
                    currentY += textLine.Height;

                    continue;
                }

                var characterHit = new CharacterHit(textPosition);

                var startX = textLine.GetDistanceFromCharacterHit(characterHit);

                var nextCharacterHit = textLine.GetNextCaretCharacterHit(characterHit);

                var endX = textLine.GetDistanceFromCharacterHit(nextCharacterHit);

                return new Rect(startX, currentY, endX - startX, textLine.Height);
            }

            return new Rect();
        }

        public IEnumerable<Rect> HitTestTextRange(int start, int length)
        {
            if (start + length <= 0)
            {
                return Array.Empty<Rect>();
            }

            var result = new List<Rect>(TextLines.Count);

            var currentY = 0d;

            foreach (var textLine in TextLines)
            {
                var currentX = textLine.Start;

                if (textLine.TextRange.End < start)
                {
                    currentY += textLine.Height;

                    continue;
                }

                if (start > textLine.TextRange.Start)
                {
                    currentX += textLine.GetDistanceFromCharacterHit(new CharacterHit(start));
                }

                var endX = textLine.GetDistanceFromCharacterHit(new CharacterHit(start + length));

                result.Add(new Rect(currentX, currentY, endX - currentX, textLine.Height));

                if (textLine.TextRange.Start + textLine.TextRange.Length >= start + length)
                {
                    break;
                }

                currentY += textLine.Height;
            }

            return result;
        }

        public TextHitTestResult HitTestPoint(in Point point)
        {
            var currentY = 0d;

            var lineIndex = 0;
            TextLine currentLine = null;
            CharacterHit characterHit;

            for (; lineIndex < TextLines.Count; lineIndex++)
            {
                currentLine = TextLines[lineIndex];

                if (currentY + currentLine.Height > point.Y)
                {
                    characterHit = currentLine.GetCharacterHitFromDistance(point.X);

                    //var currentX = currentLine.GetDistanceFromCharacterHit(characterHit);

                    //var position = new Rect(currentX, currentY, 0, currentLine.Height);

                    return GetHitTestResult(currentLine, characterHit, point);
                }

                currentY += currentLine.Height;
            }

            if (currentLine is null)
            {
                return new TextHitTestResult();
            }

            characterHit = currentLine.GetNextCaretCharacterHit(new CharacterHit(currentLine.TextRange.End));

            //var currentX = currentLine.GetDistanceFromCharacterHit(characterHit);

            //var position = new Rect(currentX, Size.Height - currentLine.Height, 0, currentLine.Height);

            return GetHitTestResult(currentLine, characterHit, point);
        }

        private TextHitTestResult GetHitTestResult(TextLine textLine, CharacterHit characterHit, Point point)
        {
            var (x, y) = point;

            var lastTrailingIndex = textLine.TextRange.Start + textLine.TextRange.Length;

            var isInside = x >= 0 && x <= textLine.Width && y >= 0 && y <= textLine.Height;

            if (x >= textLine.Width && textLine.TextRange.Length > 0 && textLine.NewLineLength > 0)
            {
                lastTrailingIndex -= textLine.NewLineLength;
            }

            var textPosition = characterHit.FirstCharacterIndex + characterHit.TrailingLength;

            var isTrailing = lastTrailingIndex == textPosition && characterHit.TrailingLength > 0 ||
                             y > Size.Height;

            return new TextHitTestResult { IsInside = isInside, IsTrailing = isTrailing, TextPosition = textPosition };
        }

        /// <summary>
        /// Creates the default <see cref="TextParagraphProperties"/> that are used by the <see cref="TextFormatter"/>.
        /// </summary>
        /// <param name="typeface">The typeface.</param>
        /// <param name="fontSize">The font size.</param>
        /// <param name="foreground">The foreground.</param>
        /// <param name="textAlignment">The text alignment.</param>
        /// <param name="textWrapping">The text wrapping.</param>
        /// <param name="textDecorations">The text decorations.</param>
        /// <param name="lineHeight">The height of each line of text.</param>
        /// <returns></returns>
        private static TextParagraphProperties CreateTextParagraphProperties(Typeface typeface, double fontSize,
            IBrush foreground, TextAlignment textAlignment, TextWrapping textWrapping,
            TextDecorationCollection textDecorations, double lineHeight)
        {
            var textRunStyle = new GenericTextRunProperties(typeface, fontSize, textDecorations, foreground);

            return new GenericTextParagraphProperties(FlowDirection.LeftToRight, textAlignment, true, false,
                textRunStyle, textWrapping, lineHeight, 0);
        }

        /// <summary>
        /// Updates the current bounds.
        /// </summary>
        /// <param name="textLine">The text line.</param>
        /// <param name="width">The current width.</param>
        /// <param name="height">The current height.</param>
        private static void UpdateBounds(TextLine textLine, ref double width, ref double height)
        {
            if (width < textLine.Width)
            {
                width = textLine.Width;
            }

            height += textLine.Height;
        }

        /// <summary>
        /// Creates an empty text line.
        /// </summary>
        /// <returns>The empty text line.</returns>
        private TextLine CreateEmptyTextLine(int startingIndex)
        {
            var properties = _paragraphProperties.DefaultTextRunProperties;

            var glyphRun = TextShaper.Current.ShapeText(new ReadOnlySlice<char>(s_empty, startingIndex, 1),
                properties.Typeface, properties.FontRenderingEmSize, properties.CultureInfo);

            var textRuns = new List<TextRun>
            {
                new ShapedTextCharacters(glyphRun, _paragraphProperties.DefaultTextRunProperties)
            };

            var textRange = new TextRange(startingIndex, 1);

            return new TextLineImpl(textRuns, textRange, MaxWidth, _paragraphProperties);
        }

        /// <summary>
        /// Updates the layout and applies specified text style overrides.
        /// </summary>
        private void UpdateLayout()
        {
            if (_text.IsEmpty || MathUtilities.IsZero(MaxWidth) || MathUtilities.IsZero(MaxHeight))
            {
                var textLine = CreateEmptyTextLine(0);

                TextLines = new List<TextLine> { textLine };

                Size = new Size(0, textLine.Height);
            }
            else
            {
                var textLines = new List<TextLine>();

                double width = 0.0, height = 0.0;

                var currentPosition = 0;

                var textSource = new FormattedTextSource(_text,
                    _paragraphProperties.DefaultTextRunProperties, _textStyleOverrides);

                TextLine previousLine = null;

                while (currentPosition < _text.Length)
                {
                    var textLine = TextFormatter.Current.FormatLine(textSource, currentPosition, MaxWidth,
                        _paragraphProperties, previousLine?.TextLineBreak);

                    currentPosition += textLine.TextRange.Length;

                    if (textLines.Count > 0)
                    {
                        if (textLines.Count == MaxLines || !double.IsPositiveInfinity(MaxHeight) &&
                            height + textLine.Height > MaxHeight)
                        {
                            if (previousLine?.TextLineBreak != null && _textTrimming != TextTrimming.None)
                            {
                                var collapsedLine =
                                    previousLine.Collapse(GetCollapsingProperties(MaxWidth));

                                textLines[textLines.Count - 1] = collapsedLine;
                            }

                            break;
                        }
                    }

                    var hasOverflowed = textLine.HasOverflowed;

                    if (hasOverflowed && _textTrimming != TextTrimming.None)
                    {
                        textLine = textLine.Collapse(GetCollapsingProperties(MaxWidth));
                    }

                    textLines.Add(textLine);

                    UpdateBounds(textLine, ref width, ref height);

                    previousLine = textLine;

                    if (currentPosition != _text.Length || textLine.TextLineBreak == null)
                    {
                        continue;
                    }

                    var emptyTextLine = CreateEmptyTextLine(currentPosition);

                    textLines.Add(emptyTextLine);
                }

                Size = new Size(width, height);

                TextLines = textLines;
            }
        }

        /// <summary>
        /// Gets the <see cref="TextCollapsingProperties"/> for current text trimming mode.
        /// </summary>
        /// <param name="width">The collapsing width.</param>
        /// <returns>The <see cref="TextCollapsingProperties"/>.</returns>
        private TextCollapsingProperties GetCollapsingProperties(double width)
        {
            return _textTrimming switch
            {
                TextTrimming.CharacterEllipsis => new TextTrailingCharacterEllipsis(width,
                    _paragraphProperties.DefaultTextRunProperties),
                TextTrimming.WordEllipsis => new TextTrailingWordEllipsis(width,
                    _paragraphProperties.DefaultTextRunProperties),
                _ => throw new ArgumentOutOfRangeException(),
            };
        }

        public int GetLineIndexFromCharacterIndex(int charIndex)
        {
            if (TextLines is null)
            {
                return -1;
            }

            if (charIndex < 0)
            {
                return -1;
            }

            if (charIndex > _text.Length - 1)
            {
                return TextLines.Count - 1;
            }

            for (var index = 0; index < TextLines.Count; index++)
            {
                var textLine = TextLines[index];

                if (textLine.TextRange.End < charIndex)
                {
                    continue;
                }

                if (charIndex >= textLine.Start && charIndex <= textLine.TextRange.End)
                {
                    return index;
                }
            }

            return TextLines.Count - 1;
        }

        public int GetCharacterIndexFromPoint(Point point, bool snapToText)
        {
            if (TextLines is null)
            {
                return -1;
            }

            var (x, y) = point;

            if (!snapToText && y > Size.Height)
            {
                return -1;
            }

            var currentY = 0d;

            foreach (var textLine in TextLines)
            {
                if (currentY + textLine.Height <= y)
                {
                    currentY += textLine.Height;

                    continue;
                }

                if (x > textLine.WidthIncludingTrailingWhitespace)
                {
                    if (snapToText)
                    {
                        return textLine.TextRange.End;
                    }

                    return -1;
                }

                var characterHit = textLine.GetCharacterHitFromDistance(x);

                return characterHit.FirstCharacterIndex + characterHit.TrailingLength;
            }

            return _text.Length;
        }

        public Rect GetRectFromCharacterIndex(int characterIndex, bool trailingEdge)
        {
            if (TextLines is null)
            {
                return Rect.Empty;
            }

            var distanceY = 0d;

            var currentIndex = 0;

            foreach (var textLine in TextLines)
            {
                if (currentIndex + textLine.TextRange.Length < characterIndex)
                {
                    distanceY += textLine.Height;

                    currentIndex += textLine.TextRange.Length;

                    continue;
                }

                var characterHit = new CharacterHit(characterIndex);

                while (characterHit.FirstCharacterIndex < characterIndex)
                {
                    characterHit = textLine.GetNextCaretCharacterHit(characterHit);
                }

                var distanceX = textLine.GetDistanceFromCharacterHit(trailingEdge ?
                    characterHit :
                    new CharacterHit(characterHit.FirstCharacterIndex));

                if (characterHit.TrailingLength > 0)
                {
                    distanceX += 1;
                }

                return new Rect(distanceX, distanceY, 0, textLine.Height);
            }

            return Rect.Empty;
        }

        private readonly struct FormattedTextSource : ITextSource
        {
            private readonly ReadOnlySlice<char> _text;
            private readonly TextRunProperties _defaultProperties;
            private readonly IReadOnlyList<ValueSpan<TextRunProperties>> _textModifier;

            public FormattedTextSource(ReadOnlySlice<char> text, TextRunProperties defaultProperties,
                IReadOnlyList<ValueSpan<TextRunProperties>> textModifier)
            {
                _text = text;
                _defaultProperties = defaultProperties;
                _textModifier = textModifier;
            }

            public TextRun GetTextRun(int textSourceIndex)
            {
                if (textSourceIndex > _text.End)
                {
                    return new TextEndOfLine();
                }

                var runText = _text.Skip(textSourceIndex);

                if (runText.IsEmpty)
                {
                    return new TextEndOfLine();
                }

                var textStyleRun = CreateTextStyleRun(runText, _defaultProperties, _textModifier);

                return new TextCharacters(runText.Take(textStyleRun.Length), textStyleRun.Value);
            }

            /// <summary>
            /// Creates a span of text run properties that has modifier applied.
            /// </summary>
            /// <param name="text">The text to create the properties for.</param>
            /// <param name="defaultProperties">The default text properties.</param>
            /// <param name="textModifier">The text properties modifier.</param>
            /// <returns>
            /// The created text style run.
            /// </returns>
            private static ValueSpan<TextRunProperties> CreateTextStyleRun(ReadOnlySlice<char> text,
                TextRunProperties defaultProperties, IReadOnlyList<ValueSpan<TextRunProperties>> textModifier)
            {
                if (textModifier == null || textModifier.Count == 0)
                {
                    return new ValueSpan<TextRunProperties>(text.Start, text.Length, defaultProperties);
                }

                var currentProperties = defaultProperties;

                var hasOverride = false;

                var i = 0;

                var length = 0;

                for (; i < textModifier.Count; i++)
                {
                    var propertiesOverride = textModifier[i];

                    var textRange = new TextRange(propertiesOverride.Start, propertiesOverride.Length);

                    if (textRange.End < text.Start)
                    {
                        continue;
                    }

                    if (textRange.Start > text.End)
                    {
                        length = text.Length;
                        break;
                    }

                    if (textRange.Start > text.Start)
                    {
                        if (propertiesOverride.Value != currentProperties)
                        {
                            length = Math.Min(Math.Abs(textRange.Start - text.Start), text.Length);

                            break;
                        }
                    }

                    length += Math.Min(text.Length - length, textRange.Length);

                    if (hasOverride)
                    {
                        continue;
                    }

                    hasOverride = true;

                    currentProperties = propertiesOverride.Value;
                }

                if (length < text.Length && i == textModifier.Count)
                {
                    if (currentProperties == defaultProperties)
                    {
                        length = text.Length;
                    }
                }

                if (length != text.Length)
                {
                    text = text.Take(length);
                }

                return new ValueSpan<TextRunProperties>(text.Start, length, currentProperties);
            }
        }
    }
}
