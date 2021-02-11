using System;
using System.Collections.Generic;
using Avalonia.Media.TextFormatting.Unicode;
using Avalonia.Platform;
using Avalonia.Utilities;

namespace Avalonia.Media.TextFormatting
{
    internal class TextLineImpl : TextLine
    {
        private readonly List<TextRun> _textRuns;
        private readonly double _paragraphWidth;
        private readonly TextParagraphProperties _paragraphProperties;
        private readonly TextLineMetrics _textLineMetrics;

        public TextLineImpl(List<TextRun> textRuns, TextRange textRange, double paragraphWidth,
            TextParagraphProperties paragraphProperties, TextLineBreak lineBreak = null, bool hasCollapsed = false)
        {
            TextRange = textRange;
            TextLineBreak = lineBreak;
            HasCollapsed = hasCollapsed;

            _textRuns = textRuns;
            _paragraphWidth = paragraphWidth;
            _paragraphProperties = paragraphProperties;

            _textLineMetrics = CreateLineMetrics();
        }

        /// <inheritdoc/>
        public override TextRange TextRange { get; }

        /// <inheritdoc/>
        public override IReadOnlyList<TextRun> TextRuns => _textRuns;

        /// <inheritdoc/>
        public override TextLineBreak TextLineBreak { get; }

        /// <inheritdoc/>
        public override bool HasCollapsed { get; }

        /// <inheritdoc/>
        public override bool HasOverflowed => _textLineMetrics.HasOverflowed;

        /// <inheritdoc/>
        public override double Baseline => _textLineMetrics.TextBaseline;

        /// <inheritdoc/>
        public override double Extent => _textLineMetrics.Height;

        /// <inheritdoc/>
        public override double Height => _textLineMetrics.Height;

        /// <inheritdoc/>
        public override int NewLineLength => _textLineMetrics.NewLineLength;

        /// <inheritdoc/>
        public override double OverhangAfter => 0;

        /// <inheritdoc/>
        public override double OverhangLeading => 0;

        /// <inheritdoc/>
        public override double OverhangTrailing => 0;

        /// <inheritdoc/>
        public override int TrailingWhitespaceLength => _textLineMetrics.TrailingWhitespaceLength;

        /// <inheritdoc/>
        public override double Start => _textLineMetrics.Start;

        /// <inheritdoc/>
        public override double Width => _textLineMetrics.Width;

        /// <inheritdoc/>
        public override double WidthIncludingTrailingWhitespace => _textLineMetrics.WidthIncludingTrailingWhitespace;

        /// <inheritdoc/>
        public override void Draw(DrawingContext drawingContext, Point lineOrigin)
        {
            var (currentX, currentY) = lineOrigin;

            foreach (var textRun in _textRuns)
            {
                // TODO: Should probably use DrawableRun
                if (textRun is ShapedTextCharacters shapedText)
                {
                    var offsetY = Baseline - shapedText.GlyphRun.BaselineOrigin.Y;

                    shapedText.Draw(drawingContext, new Point(currentX, currentY + offsetY));

                    currentX += shapedText.Size.Width;
                }
            }
        }

        /// <inheritdoc/>
        public override TextLine Collapse(params TextCollapsingProperties[] collapsingPropertiesList)
        {
            if (collapsingPropertiesList == null || collapsingPropertiesList.Length == 0)
            {
                return this;
            }

            var collapsingProperties = collapsingPropertiesList[0];

            var runIndex = 0;
            var currentWidth = 0.0;
            var textRange = TextRange;
            var collapsedLength = 0;

            var shapedSymbol = CreateShapedSymbol(collapsingProperties.Symbol);

            var availableWidth = collapsingProperties.Width - shapedSymbol.Size.Width;

            while (runIndex < _textRuns.Count)
            {
                var currentRun = _textRuns[runIndex];

                var shapedText = currentRun as ShapedTextCharacters;

                if (shapedText != null)
                {
                    currentWidth += shapedText.Size.Width;

                    if (currentWidth > availableWidth)
                    {
                        if (TextFormatterImpl.TryMeasureCharacters(shapedText, availableWidth, out var measuredLength))
                        {
                            if (collapsingProperties.Style == TextCollapsingStyle.TrailingWord &&
                                measuredLength < textRange.End)
                            {
                                var currentBreakPosition = 0;

                                var lineBreaker = new LineBreakEnumerator(currentRun.Text);

                                while (currentBreakPosition < measuredLength && lineBreaker.MoveNext())
                                {
                                    var nextBreakPosition = lineBreaker.Current.PositionMeasure;

                                    if (nextBreakPosition == 0)
                                    {
                                        break;
                                    }

                                    if (nextBreakPosition >= measuredLength)
                                    {
                                        break;
                                    }

                                    currentBreakPosition = nextBreakPosition;
                                }

                                measuredLength = currentBreakPosition;
                            }
                        }

                        collapsedLength += measuredLength;

                        var splitResult = TextFormatterImpl.SplitTextRuns(_textRuns, collapsedLength);

                        var shapedTextCharacters = new List<TextRun>(splitResult.First.Count + 1);

                        shapedTextCharacters.AddRange(splitResult.First);

                        shapedTextCharacters.Add(shapedSymbol);

                        textRange = new TextRange(textRange.Start, collapsedLength);

                        return new TextLineImpl(shapedTextCharacters, textRange, _paragraphWidth, _paragraphProperties,
                            TextLineBreak, true);
                    }

                    availableWidth -= shapedText.Size.Width;
                }

                collapsedLength += currentRun.Text.Length;

                runIndex++;
            }

            return this;
        }

        private TextLineMetrics CreateLineMetrics()
        {
            var width = 0d;
            var widthIncludingWhitespace = 0d;
            var trailingWhitespaceLength = 0;
            var newLineLength = 0;
            var ascent = 0d;
            var descent = 0d;
            var lineGap = 0d;

            for (var index = 0; index < _textRuns.Count; index++)
            {
                var textRun = _textRuns[index];

                if (textRun is ShapedTextCharacters shapedText)
                {
                    var fontMetrics =
                        new FontMetrics(textRun.Properties.Typeface, textRun.Properties.FontRenderingEmSize);

                    if (ascent > fontMetrics.Ascent)
                    {
                        ascent = fontMetrics.Ascent;
                    }

                    if (descent < fontMetrics.Descent)
                    {
                        descent = fontMetrics.Descent;
                    }

                    if (lineGap < fontMetrics.LineGap)
                    {
                        lineGap = fontMetrics.LineGap;
                    }

                    if (index == _textRuns.Count - 1)
                    {
                        width = widthIncludingWhitespace + shapedText.GlyphRun.Metrics.Width;
                        widthIncludingWhitespace += shapedText.GlyphRun.Metrics.WidthIncludingTrailingWhitespace;
                        trailingWhitespaceLength = shapedText.GlyphRun.Metrics.TrailingWhitespaceLength;
                        newLineLength = shapedText.GlyphRun.Metrics.NewlineLength;
                    }
                    else
                    {
                        widthIncludingWhitespace += shapedText.GlyphRun.Metrics.WidthIncludingTrailingWhitespace;
                    }
                }
                else if (textRun is TextEndOfLine /* TODO This includes end of paragraph, which seems intentional */)
                {
                    if (index == _textRuns.Count - 1)
                    {
                        newLineLength = 1;
                    }
                }
            }

            var start = GetParagraphOffsetX(width, _paragraphWidth, _paragraphProperties.TextAlignment);

            var lineHeight = _paragraphProperties.LineHeight;

            var height = double.IsNaN(lineHeight) || MathUtilities.IsZero(lineHeight) ?
                descent - ascent + lineGap :
                lineHeight;

            return new TextLineMetrics(widthIncludingWhitespace > _paragraphWidth, height, newLineLength, start,
                -ascent, trailingWhitespaceLength, width, widthIncludingWhitespace);
        }

        /// <inheritdoc/>
        public override CharacterHit GetCharacterHitFromDistance(double distance)
        {
            distance -= Start;

            if (distance < 0)
            {
                // hit happens before the line, return the first position
                return new CharacterHit(TextRange.Start);
            }

            // process hit that happens within the line
            var characterHit = new CharacterHit();

            foreach (var run in _textRuns)
            {
                if (run is ShapedTextCharacters shapedText)
                {
                    characterHit = shapedText.GlyphRun.GetCharacterHitFromDistance(distance, out _);

                    if (distance <= shapedText.Size.Width)
                    {
                        break;
                    }

                    distance -= shapedText.Size.Width;
                }
            }

            return characterHit;
        }

        /// <inheritdoc/>
        public override double GetDistanceFromCharacterHit(CharacterHit characterHit)
        {
            var characterIndex = characterHit.FirstCharacterIndex + (characterHit.TrailingLength != 0 ? 1 : 0);

            if (characterIndex > TextRange.End)
            {
                if (NewLineLength > 0)
                {
                    return Start + Width;
                }
                return Start + WidthIncludingTrailingWhitespace;
            }

            var currentDistance = Start;

            foreach (var textRun in _textRuns)
            {
                if (textRun is ShapedTextCharacters shapedText)
                {
                    if (characterIndex > shapedText.Text.End)
                    {
                        currentDistance += shapedText.Size.Width;

                        continue;
                    }

                    return currentDistance +
                           shapedText.GlyphRun.GetDistanceFromCharacterHit(new CharacterHit(characterIndex));
                }
            }

            return currentDistance;
        }

        /// <inheritdoc/>
        public override CharacterHit GetNextCaretCharacterHit(CharacterHit characterHit)
        {
            if (TryFindNextCharacterHit(characterHit, out var nextCharacterHit))
            {
                return nextCharacterHit;
            }

            if (characterHit.FirstCharacterIndex + characterHit.TrailingLength <= TextRange.Start + TextRange.Length)
            {
                return characterHit; // Can't move, we're after the last character
            }

            var runIndex = GetRunIndexAtCodepointIndex(TextRange.End);

            var textRun = _textRuns[runIndex];

            if (textRun is ShapedTextCharacters shapedText)
            {
                characterHit = shapedText.GlyphRun.GetNextCaretCharacterHit(characterHit);
            }

            return characterHit; // Can't move, we're after the last character
        }

        /// <inheritdoc/>
        public override CharacterHit GetPreviousCaretCharacterHit(CharacterHit characterHit)
        {
            if (TryFindPreviousCharacterHit(characterHit, out var previousCharacterHit))
            {
                return previousCharacterHit;
            }

            if (characterHit.FirstCharacterIndex < TextRange.Start)
            {
                characterHit = new CharacterHit(TextRange.Start);
            }

            return characterHit; // Can't move, we're before the first character
        }

        /// <inheritdoc/>
        public override CharacterHit GetBackspaceCaretCharacterHit(CharacterHit characterHit)
        {
            // same operation as move-to-previous
            return GetPreviousCaretCharacterHit(characterHit);
        }

        /// <summary>
        /// Tries to find the next character hit.
        /// </summary>
        /// <param name="characterHit">The current character hit.</param>
        /// <param name="nextCharacterHit">The next character hit.</param>
        /// <returns></returns>
        private bool TryFindNextCharacterHit(CharacterHit characterHit, out CharacterHit nextCharacterHit)
        {
            nextCharacterHit = characterHit;

            var codepointIndex = characterHit.FirstCharacterIndex + characterHit.TrailingLength;

            if (codepointIndex > TextRange.End)
            {
                return false; // Cannot go forward anymore
            }

            if (codepointIndex < TextRange.Start)
            {
                codepointIndex = TextRange.Start;
            }

            var runIndex = GetRunIndexAtCodepointIndex(codepointIndex);

            while (runIndex < TextRuns.Count)
            {
                var run = _textRuns[runIndex++] as ShapedTextCharacters;

                if (run == null)
                {
                    continue;
                }

                var foundCharacterHit =
                    run.GlyphRun.FindNearestCharacterHit(characterHit.FirstCharacterIndex + characterHit.TrailingLength, out _);

                var isAtEnd = foundCharacterHit.FirstCharacterIndex + foundCharacterHit.TrailingLength ==
                              TextRange.Length;

                var characterIndex = codepointIndex - run.Text.Start;

                if (characterIndex < 0 && characterHit.TrailingLength == 0)
                {
                    foundCharacterHit = new CharacterHit(foundCharacterHit.FirstCharacterIndex);
                }

                nextCharacterHit = isAtEnd || characterHit.TrailingLength != 0 ?
                    foundCharacterHit :
                    new CharacterHit(foundCharacterHit.FirstCharacterIndex + foundCharacterHit.TrailingLength);

                if (isAtEnd || nextCharacterHit.FirstCharacterIndex > characterHit.FirstCharacterIndex)
                {
                    return true;
                }

                runIndex++;
            }

            return false;
        }

        /// <summary>
        /// Tries to find the previous character hit.
        /// </summary>
        /// <param name="characterHit">The current character hit.</param>
        /// <param name="previousCharacterHit">The previous character hit.</param>
        /// <returns></returns>
        private bool TryFindPreviousCharacterHit(CharacterHit characterHit, out CharacterHit previousCharacterHit)
        {
            if (characterHit.FirstCharacterIndex == TextRange.Start)
            {
                previousCharacterHit = new CharacterHit(TextRange.Start);

                return true;
            }

            previousCharacterHit = characterHit;

            var codepointIndex = characterHit.FirstCharacterIndex + characterHit.TrailingLength;

            if (codepointIndex < TextRange.Start)
            {
                return false; // Cannot go backward anymore.
            }

            var runIndex = GetRunIndexAtCodepointIndex(codepointIndex);

            while (runIndex >= 0)
            {
                var run = _textRuns[runIndex--] as ShapedTextCharacters;
                if (run == null)
                {
                    continue;
                }

                var foundCharacterHit = run.GlyphRun.FindNearestCharacterHit(characterHit.FirstCharacterIndex - 1, out _);

                previousCharacterHit = characterHit.TrailingLength != 0 ?
                    foundCharacterHit :
                    new CharacterHit(foundCharacterHit.FirstCharacterIndex);

                if (previousCharacterHit.FirstCharacterIndex < characterHit.FirstCharacterIndex)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Gets the run index of the specified codepoint index.
        /// </summary>
        /// <param name="codepointIndex">The codepoint index.</param>
        /// <returns>The text run index.</returns>
        private int GetRunIndexAtCodepointIndex(int codepointIndex)
        {
            if (codepointIndex > TextRange.End)
            {
                return _textRuns.Count - 1;
            }

            if (codepointIndex <= 0)
            {
                return 0;
            }

            var runIndex = 0;

            while (runIndex < _textRuns.Count)
            {
                var run = _textRuns[runIndex];

                if (run.Text.End >= codepointIndex)
                {
                    return runIndex;
                }

                runIndex++;
            }

            return runIndex;
        }

        /// <summary>
        /// Creates a shaped symbol.
        /// </summary>
        /// <param name="textRun">The symbol run to shape.</param>
        /// <returns>
        /// The shaped symbol.
        /// </returns>
        internal static ShapedTextCharacters CreateShapedSymbol(TextRun textRun)
        {
            var formatterImpl = AvaloniaLocator.Current.GetService<ITextShaperImpl>();

            var glyphRun = formatterImpl.ShapeText(textRun.Text, textRun.Properties.Typeface, textRun.Properties.FontRenderingEmSize,
                textRun.Properties.CultureInfo);

            return new ShapedTextCharacters(glyphRun, textRun.Properties);
        }
    }
}
