using System;
using System.Collections.Generic;
using Avalonia.Media.TextFormatting.Unicode;
using Avalonia.Utilities;

namespace Avalonia.Media.TextFormatting
{
    internal class TextFormatterImpl : TextFormatter
    {
        /// <inheritdoc cref="TextFormatter.FormatLine"/>
        public override TextLine FormatLine(ITextSource textSource, int firstTextSourceIndex, double paragraphWidth,
            TextParagraphProperties paragraphProperties, TextLineBreak previousLineBreak = null)
        {
            var textWrapping = paragraphProperties.TextWrapping;

            var textRuns = FetchTextRuns(textSource, firstTextSourceIndex, previousLineBreak, out var nextLineBreak);

            var textRange = GetTextRange(textRuns);

            TextLine textLine;

            switch (textWrapping)
            {
                case TextWrapping.NoWrap:
                {
                    textLine = new TextLineImpl(textRuns, textRange, paragraphWidth, paragraphProperties,
                        nextLineBreak);
                    break;
                }
                case TextWrapping.WrapWithOverflow:
                case TextWrapping.Wrap:
                {
                    textLine = PerformTextWrapping(textRuns, textRange, paragraphWidth, paragraphProperties,
                        nextLineBreak);
                    break;
                }
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return textLine;
        }

        /// <summary>
        /// Measures the number of characters that fit into available width.
        /// </summary>
        /// <param name="textCharacters">The text run.</param>
        /// <param name="availableWidth">The available width.</param>
        /// <param name="count">The count of fitting characters.</param>
        /// <returns>
        /// <c>true</c> if characters fit into the available width; otherwise, <c>false</c>.
        /// </returns>
        internal static bool TryMeasureCharacters(ShapedTextCharacters textCharacters, double availableWidth,
            out int count)
        {
            var glyphRun = textCharacters.GlyphRun;

            if (glyphRun.Size.Width < availableWidth)
            {
                count = glyphRun.Characters.Length;

                return true;
            }

            var glyphCount = 0;

            var currentWidth = 0.0;

            if (glyphRun.GlyphAdvances.IsEmpty)
            {
                var glyphTypeface = glyphRun.GlyphTypeface;

                for (var i = 0; i < glyphRun.GlyphClusters.Length; i++)
                {
                    var glyph = glyphRun.GlyphIndices[i];

                    var advance = glyphTypeface.GetGlyphAdvance(glyph) * glyphRun.Scale;

                    if (currentWidth + advance > availableWidth)
                    {
                        break;
                    }

                    currentWidth += advance;

                    glyphCount++;
                }
            }
            else
            {
                foreach (var advance in glyphRun.GlyphAdvances)
                {
                    if (currentWidth + advance > availableWidth)
                    {
                        break;
                    }

                    currentWidth += advance;

                    glyphCount++;
                }
            }

            if (glyphCount == 0)
            {
                count = 0;

                return false;
            }

            if (glyphCount == glyphRun.GlyphIndices.Length)
            {
                count = glyphRun.Characters.Length;

                return true;
            }

            if (glyphRun.GlyphClusters.IsEmpty)
            {
                count = glyphCount;

                return true;
            }

            var firstCluster = glyphRun.GlyphClusters[0];

            var lastCluster = glyphRun.GlyphClusters[glyphCount];

            if (glyphRun.IsLeftToRight)
            {
                count = lastCluster - firstCluster;
            }
            else
            {
                count = firstCluster - lastCluster;
            }
           

            return count > 0;
        }

        /// <summary>
        /// Split a sequence of runs into two segments at specified length.
        /// </summary>
        /// <param name="textRuns">The text run's.</param>
        /// <param name="length">The length to split at.</param>
        /// <returns>The split text runs.</returns>
        internal static SplitTextRunsResult SplitTextRuns(List<TextRun> textRuns, int length)
        {
            var currentLength = 0;

            for (var i = 0; i < textRuns.Count; i++)
            {
                var currentRun = textRuns[i];

                if (currentLength + currentRun.Text.Length < length)
                {
                    currentLength += currentRun.Text.Length;
                    continue;
                }

                var firstCount = currentRun.Text.Length >= 1 ? i + 1 : i;

                var first = new List<TextRun>(firstCount);

                if (firstCount > 1)
                {
                    for (var j = 0; j < i; j++)
                    {
                        first.Add(textRuns[j]);
                    }
                }

                var secondCount = textRuns.Count - firstCount;

                if (currentLength + currentRun.Text.Length == length)
                {
                    var second = new List<TextRun>(secondCount);

                    var offset = currentRun.Text.Length > 1 ? 1 : 0;

                    if (secondCount > 0)
                    {
                        for (var j = 0; j < secondCount; j++)
                        {
                            second.Add(textRuns[i + j + offset]);
                        }
                    }

                    first.Add(currentRun);

                    return new SplitTextRunsResult(first, second);
                }
                else
                {
                    secondCount++;

                    var second = new List<TextRun>(secondCount);

                    if (currentRun is ShapedTextCharacters shapedText)
                    {
                        var split = shapedText.Split(length - currentLength);

                        first.Add(split.First);

                        second.Add(split.Second);
                    }
                    else
                    {
                        // We don't consider special runs like Inlines, EndOfParagraph, etc. as splittable,
                        // and simply sort them into the first or second bucket depending on remaining space
                        if (currentRun.Text.Length <= length - currentLength)
                        {
                            first.Add(currentRun);
                        }
                        else
                        {
                            second.Add(currentRun);
                        }
                    }

                    if (secondCount > 0)
                    {
                        for (var j = 1; j < secondCount; j++)
                        {
                            second.Add(textRuns[i + j]);
                        }
                    }

                    return new SplitTextRunsResult(first, second);
                }
            }

            return new SplitTextRunsResult(textRuns, null);
        }

        /// <summary>
        /// Fetches text runs.
        /// </summary>
        /// <param name="textSource">The text source.</param>
        /// <param name="firstTextSourceIndex">The first text source index.</param>
        /// <param name="previousLineBreak">Previous line break. Can be null.</param>
        /// <param name="nextLineBreak">Next line break. Can be null.</param>
        /// <returns>
        /// The formatted text runs.
        /// </returns>
        private static List<TextRun> FetchTextRuns(ITextSource textSource,
            int firstTextSourceIndex, TextLineBreak previousLineBreak, out TextLineBreak nextLineBreak)
        {
            nextLineBreak = default;

            var currentLength = 0;

            var textRuns = new List<TextRun>();

            if (previousLineBreak != null)
            {
                for (var index = 0; index < previousLineBreak.RemainingCharacters.Count; index++)
                {
                    var shapedCharacters = previousLineBreak.RemainingCharacters[index];

                    if (shapedCharacters == null)
                    {
                        continue;
                    }

                    textRuns.Add(shapedCharacters);

                    if (TryGetLineBreak(shapedCharacters, out var runLineBreak))
                    {
                        var splitResult = SplitTextRuns(textRuns, currentLength + runLineBreak.PositionWrap);

                        if (++index < previousLineBreak.RemainingCharacters.Count)
                        {
                            for (; index < previousLineBreak.RemainingCharacters.Count; index++)
                            {
                                splitResult.Second.Add(previousLineBreak.RemainingCharacters[index]);
                            }
                        }

                        nextLineBreak = new TextLineBreak(splitResult.Second);

                        return splitResult.First;
                    }

                    currentLength += shapedCharacters.Text.Length;
                }
            }

            firstTextSourceIndex += currentLength;

            var textRunEnumerator = new TextRunEnumerator(textSource, firstTextSourceIndex);

            while (textRunEnumerator.MoveNext())
            {
                var textRun = textRunEnumerator.Current;

                switch (textRun)
                {
                    case TextCharacters textCharacters:
                    {
                        var shapeableRuns = textCharacters.GetShapeableCharacters();

                        foreach (var run in shapeableRuns)
                        {
                            var glyphRun = TextShaper.Current.ShapeText(run.Text, run.Properties.Typeface,
                                run.Properties.FontRenderingEmSize, run.Properties.CultureInfo);

                            var shapedCharacters = new ShapedTextCharacters(glyphRun, run.Properties);

                            textRuns.Add(shapedCharacters);
                        }

                        break;
                    }
                }

                if (TryGetLineBreak(textRun, out var runLineBreak))
                {
                    var splitResult = SplitTextRuns(textRuns, currentLength + runLineBreak.PositionWrap);

                    nextLineBreak = new TextLineBreak(splitResult.Second);

                    return splitResult.First;
                }

                currentLength += textRun.Text.Length;
            }

            if (textRunEnumerator.Current is TextEndOfParagraph)
            {
                textRuns.Add(textRunEnumerator.Current);
            }

            return textRuns;
        }

        private static bool TryGetLineBreak(TextRun textRun, out LineBreak lineBreak)
        {
            lineBreak = default;

            if (textRun.Text.IsEmpty)
            {
                return false;
            }

            var lineBreakEnumerator = GetLineBreakEnumerator(textRun);

            while (lineBreakEnumerator.MoveNext())
            {
                if (!lineBreakEnumerator.Current.Required)
                {
                    continue;
                }

                lineBreak = lineBreakEnumerator.Current;

                if (lineBreak.PositionWrap >= textRun.Text.Length)
                {
                    return true;
                }

                return true;
            }

            return false;
        }

        /// <summary>
        /// Performs text wrapping returns a list of text lines.
        /// </summary>
        /// <param name="textRuns">The text run's.</param>
        /// <param name="textRange">The text range that is covered by the text runs.</param>
        /// <param name="paragraphWidth">The paragraph width.</param>
        /// <param name="paragraphProperties">The text paragraph properties.</param>
        /// <param name="currentLineBreak">The current line break if the line was explicitly broken.</param>
        /// <returns>The wrapped text line.</returns>
        private static TextLine PerformTextWrapping(List<TextRun> textRuns, TextRange textRange,
            double paragraphWidth, TextParagraphProperties paragraphProperties, TextLineBreak currentLineBreak)
        {
            var availableWidth = paragraphWidth;
            var currentWidth = 0.0;
            var measuredLength = 0;

            foreach (var currentRun in textRuns)
            {
                if (currentRun is ShapedTextCharacters shapedText)
                {
                    if (currentWidth + shapedText.Size.Width > availableWidth)
                    {
                        if (TryMeasureCharacters(shapedText, paragraphWidth - currentWidth, out var count))
                        {
                            measuredLength += count;
                        }

                        break;
                    }

                    currentWidth += shapedText.Size.Width;
                }

                measuredLength += currentRun.Text.Length;
            }

            var currentLength = 0;

            var lastWrapPosition = 0;

            var currentPosition = 0;

            if (measuredLength == 0 && paragraphProperties.TextWrapping != TextWrapping.WrapWithOverflow)
            {
                measuredLength = 1;
            }
            else
            {
                for (var index = 0; index < textRuns.Count; index++)
                {
                    var currentRun = textRuns[index];

                    var lineBreaker = GetLineBreakEnumerator(currentRun);

                    var breakFound = false;

                    while (lineBreaker.MoveNext())
                    {
                        if (lineBreaker.Current.Required &&
                            currentLength + lineBreaker.Current.PositionMeasure <= measuredLength)
                        {
                            breakFound = true;

                            currentPosition = currentLength + lineBreaker.Current.PositionWrap;

                            break;
                        }

                        if ((paragraphProperties.TextWrapping != TextWrapping.WrapWithOverflow || lastWrapPosition != 0) &&
                            currentLength + lineBreaker.Current.PositionMeasure > measuredLength)
                        {
                            if (lastWrapPosition > 0)
                            {
                                currentPosition = lastWrapPosition;
                            }
                            else
                            {
                                currentPosition = currentLength + lineBreaker.Current.PositionWrap;
                            }

                            breakFound = true;

                            break;
                        }

                        if (currentLength + lineBreaker.Current.PositionWrap >= measuredLength)
                        {
                            currentPosition = currentLength + lineBreaker.Current.PositionWrap;

                            if (index < textRuns.Count - 1 &&
                                lineBreaker.Current.PositionWrap == currentRun.Text.Length)
                            {
                                var nextRun = textRuns[index + 1];

                                lineBreaker = GetLineBreakEnumerator(nextRun);

                                if (lineBreaker.MoveNext() &&
                                    lineBreaker.Current.PositionMeasure == 0)
                                {
                                    currentPosition += lineBreaker.Current.PositionWrap;
                                }
                            }

                            breakFound = true;

                            break;
                        }

                        lastWrapPosition = currentLength + lineBreaker.Current.PositionWrap;
                    }

                    if (!breakFound)
                    {
                        currentLength += currentRun.Text.Length;

                        continue;
                    }

                    measuredLength = currentPosition;

                    break;
                }
            }
            
            var splitResult = SplitTextRuns(textRuns, measuredLength);

            textRange = new TextRange(textRange.Start, measuredLength);

            var remainingCharacters = splitResult.Second;

            if (currentLineBreak?.RemainingCharacters != null)
            {
                if (remainingCharacters != null)
                {
                    remainingCharacters.AddRange(currentLineBreak.RemainingCharacters);
                }
                else
                {
                    remainingCharacters = new List<TextRun>(currentLineBreak.RemainingCharacters);
                }
            }

            var lineBreak = remainingCharacters != null && remainingCharacters.Count > 0 ?
                new TextLineBreak(remainingCharacters) :
                null;

            return new TextLineImpl(splitResult.First, textRange, paragraphWidth, paragraphProperties,
                lineBreak);
        }

        /// <summary>
        /// Gets the text range that is covered by the text runs.
        /// </summary>
        /// <param name="textRuns">The text runs.</param>
        /// <returns>The text range that is covered by the text runs.</returns>
        private static TextRange GetTextRange(IReadOnlyList<TextRun> textRuns)
        {
            if (textRuns is null || textRuns.Count == 0)
            {
                return new TextRange();
            }

            var firstTextRun = textRuns[0];

            if (textRuns.Count == 1)
            {
                return new TextRange(firstTextRun.Text.Start, firstTextRun.Text.Length);
            }

            var start = firstTextRun.Text.Start;

            var end = textRuns[textRuns.Count - 1].Text.End + 1;

            return new TextRange(start, end - start);
        }

        internal readonly struct SplitTextRunsResult
        {
            public SplitTextRunsResult(List<TextRun> first, List<TextRun> second)
            {
                First = first;

                Second = second;
            }

            /// <summary>
            /// Gets the first text runs.
            /// </summary>
            /// <value>
            /// The first text runs.
            /// </value>
            public List<TextRun> First { get; }

            /// <summary>
            /// Gets the second text runs.
            /// </summary>
            /// <value>
            /// The second text runs.
            /// </value>
            public List<TextRun> Second { get; }
        }

        private struct TextRunEnumerator
        {
            private readonly ITextSource _textSource;
            private int _pos;

            public TextRunEnumerator(ITextSource textSource, int firstTextSourceIndex)
            {
                _textSource = textSource;
                _pos = firstTextSourceIndex;
                Current = null;
            }

            // ReSharper disable once MemberHidesStaticFromOuterClass
            public TextRun Current { get; private set; }

            public bool MoveNext()
            {
                Current = _textSource.GetTextRun(_pos);

                if (Current is null)
                {
                    return false;
                }

                if (Current.TextSourceLength == 0)
                {
                    return false;
                }

                _pos += Current.TextSourceLength;

                return !(Current is TextEndOfLine);
            }
        }

        private static LineBreakEnumerator GetLineBreakEnumerator(TextRun run)
        {
            return run switch
            {
                _ => new LineBreakEnumerator(run.Text)
            };
        }
    }
}
