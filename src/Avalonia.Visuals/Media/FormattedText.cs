using System;
using System.Collections;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using Avalonia.Media.TextFormatting;
using Avalonia.Utilities;

namespace Avalonia.Media
{
    /// <summary>
    /// The FormattedText class is targeted at programmers needing to add some simple text to a MIL visual.
    /// </summary>
    public class FormattedText
    {
        public const double DefaultRealToIdeal = 28800.0 / 96;
        public const double DefaultIdealToReal = 1 / DefaultRealToIdeal;
        public const int IdealInfiniteWidth = 0x3FFFFFFE;
        public const double RealInfiniteWidth = IdealInfiniteWidth * DefaultIdealToReal;

        public const double GreatestMultiplierOfEm = 100;

        private const double MaxFontEmSize = RealInfiniteWidth / GreatestMultiplierOfEm;

        // properties and format runs
        private ReadOnlySlice<char> _text;
        private readonly SpanVector _formatRuns = new SpanVector(null);
        private SpanPosition _latestPosition;

        private GenericTextParagraphProperties _defaultParaProps;

        private double _maxTextWidth = double.PositiveInfinity;
        private double[] _maxTextWidths;
        private double _maxTextHeight = double.PositiveInfinity;
        private int _maxLineCount = int.MaxValue;
        private TextTrimming _trimming = TextTrimming.WordEllipsis;

        // text source callbacks
        private TextSourceImplementation _textSourceImpl;

        // cached metrics
        private CachedMetrics _metrics;

        /// <summary>
        /// Construct a FormattedText object.
        /// </summary>
        /// <param name="textToFormat">String of text to be displayed.</param>
        /// <param name="culture">Culture of text.</param>
        /// <param name="flowDirection">Flow direction of text.</param>
        /// <param name="typeface">Type face used to display text.</param>
        /// <param name="emSize">Font em size in visual units (1/96 of an inch).</param>
        /// <param name="foreground">Foreground brush used to render text.</param>
        public FormattedText(
            string textToFormat,
            CultureInfo culture,
            FlowDirection flowDirection,
            Typeface typeface,
            double emSize,
            IBrush foreground)
        {
            InitFormattedText(textToFormat, culture, flowDirection, typeface, emSize, foreground);
        }

        private void InitFormattedText(string textToFormat, CultureInfo culture, FlowDirection flowDirection,
            Typeface typeface, double emSize, IBrush foreground)
        {
            if (culture is null)
            {
                throw new ArgumentNullException(nameof(culture));
            }

            ValidateFlowDirection(flowDirection, nameof(flowDirection));

            ValidateFontSize(emSize);

            _text = textToFormat != null ?
                new ReadOnlySlice<char>(textToFormat.AsMemory()) :
                throw new ArgumentNullException(nameof(textToFormat));

            var runProps = new GenericTextRunProperties(
                typeface,
                emSize,
                null, // decorations
                foreground,
                null, // highlight background
                BaselineAlignment.Baseline,
                culture
            );

            _latestPosition = _formatRuns.SetValue(0, _text.Length, runProps, _latestPosition);

            _defaultParaProps = new GenericTextParagraphProperties(
                flowDirection,
                TextAlignment.Left,
                false,
                false,
                runProps,
                TextWrapping.WrapWithOverflow,
                0, // line height not specified
                0 // indentation not specified
            );

            InvalidateMetrics();
        }

        private static void ValidateFontSize(double emSize)
        {
            if (emSize <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(emSize), "The parameter value must be greater than zero.");
            }

            if (emSize > MaxFontEmSize)
            {
                throw new ArgumentOutOfRangeException(nameof(emSize), $"The parameter value cannot be greater than '{MaxFontEmSize}'");
            }

            if (double.IsNaN(emSize))
            {
                throw new ArgumentOutOfRangeException(nameof(emSize), "The parameter value must be a number.");
            }
        }

        private static void ValidateFlowDirection(FlowDirection flowDirection, string parameterName)
        {
            if ((int)flowDirection < 0 || (int)flowDirection > (int)FlowDirection.RightToLeft)
            {
                throw new InvalidEnumArgumentException(parameterName, (int)flowDirection, typeof(FlowDirection));
            }
        }

        private int ValidateRange(int startIndex, int count)
        {
            if (startIndex < 0 || startIndex > _text.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(startIndex));
            }

            var limit = startIndex + count;

            if (count < 0 || limit < startIndex || limit > _text.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }

            return limit;
        }

        private void InvalidateMetrics()
        {
            _metrics = null;
        }

        /// <summary>
        /// Sets foreground brush used for drawing text
        /// </summary>
        /// <param name="foregroundBrush">Foreground brush</param>
        public void SetForegroundBrush(IBrush foregroundBrush)
        {
            SetForegroundBrush(foregroundBrush, 0, _text.Length);
        }

        /// <summary>
        /// Sets foreground brush used for drawing text
        /// </summary>
        /// <param name="foregroundBrush">Foreground brush</param>
        /// <param name="startIndex">The start index of initial character to apply the change to.</param>
        /// <param name="count">The number of characters the change should be applied to.</param>
        public void SetForegroundBrush(IBrush foregroundBrush, int startIndex, int count)
        {
            var limit = ValidateRange(startIndex, count);
            for (var i = startIndex; i < limit;)
            {
                var formatRider = new SpanRider(_formatRuns, _latestPosition, i);
                i = Math.Min(limit, i + formatRider.Length);

#pragma warning disable 6506
                // Presharp warns that runProps is not validated, but it can never be null 
                // because the rider is already checked to be in range

                if (!(formatRider.CurrentElement is GenericTextRunProperties runProps))
                {
                    throw new NotSupportedException($"{nameof(runProps)} can not be null.");
                }

                if (runProps.ForegroundBrush == foregroundBrush)
                {
                    continue;
                }

                var newProps = new GenericTextRunProperties(
                    runProps.Typeface,
                    runProps.FontRenderingEmSize,
                    runProps.TextDecorations,
                    foregroundBrush,
                    runProps.BackgroundBrush,
                    runProps.BaselineAlignment,
                    runProps.CultureInfo
                );

#pragma warning restore 6506
                _latestPosition = _formatRuns.SetValue(formatRider.CurrentPosition, i - formatRider.CurrentPosition,
                    newProps, formatRider.SpanPosition);
            }
        }

        /// <summary>
        /// Sets or changes the font family for the text object 
        /// </summary>
        /// <param name="fontFamily">Font family name</param>
        public void SetFontFamily(string fontFamily)
        {
            SetFontFamily(fontFamily, 0, _text.Length);
        }

        /// <summary>
        /// Sets or changes the font family for the text object 
        /// </summary>
        /// <param name="fontFamily">Font family name</param>
        /// <param name="startIndex">The start index of initial character to apply the change to.</param>
        /// <param name="count">The number of characters the change should be applied to.</param>
        public void SetFontFamily(string fontFamily, int startIndex, int count)
        {
            if (fontFamily == null)
            {
                throw new ArgumentNullException(nameof(fontFamily));
            }

            SetFontFamily(new FontFamily(fontFamily), startIndex, count);
        }

        /// <summary>
        /// Sets or changes the font family for the text object 
        /// </summary>
        /// <param name="fontFamily">Font family</param>
        public void SetFontFamily(FontFamily fontFamily)
        {
            SetFontFamily(fontFamily, 0, _text.Length);
        }

        /// <summary>
        /// Sets or changes the font family for the text object 
        /// </summary>
        /// <param name="fontFamily">Font family</param>
        /// <param name="startIndex">The start index of initial character to apply the change to.</param>
        /// <param name="count">The number of characters the change should be applied to.</param>
        public void SetFontFamily(FontFamily fontFamily, int startIndex, int count)
        {
            if (fontFamily == null)
            {
                throw new ArgumentNullException(nameof(fontFamily));
            }

            var limit = ValidateRange(startIndex, count);

            for (var i = startIndex; i < limit;)
            {
                var formatRider = new SpanRider(_formatRuns, _latestPosition, i);

                i = Math.Min(limit, i + formatRider.Length);

#pragma warning disable 6506
                // Presharp warns that runProps is not validated, but it can never be null 
                // because the rider is already checked to be in range

                if (!(formatRider.CurrentElement is GenericTextRunProperties runProps))
                {
                    throw new NotSupportedException($"{nameof(runProps)} can not be null.");
                }

                var oldTypeface = runProps.Typeface;

                if (fontFamily.Equals(oldTypeface.FontFamily))
                {
                    continue;
                }

                var newProps = new GenericTextRunProperties(
                    new Typeface(fontFamily, oldTypeface.Style, oldTypeface.Weight),
                    runProps.FontRenderingEmSize,
                    runProps.TextDecorations,
                    runProps.ForegroundBrush,
                    runProps.BackgroundBrush,
                    runProps.BaselineAlignment,
                    runProps.CultureInfo
                    );

#pragma warning restore 6506
                _latestPosition = _formatRuns.SetValue(formatRider.CurrentPosition, i - formatRider.CurrentPosition,
                    newProps, formatRider.SpanPosition);

                InvalidateMetrics();
            }
        }


        /// <summary>
        /// Sets or changes the font em size measured in MIL units
        /// </summary>
        /// <param name="emSize">Font em size</param>
        public void SetFontSize(double emSize)
        {
            SetFontSize(emSize, 0, _text.Length);
        }

        /// <summary>
        /// Sets or changes the font em size measured in MIL units
        /// </summary>
        /// <param name="emSize">Font em size</param>
        /// <param name="startIndex">The start index of initial character to apply the change to.</param>
        /// <param name="count">The number of characters the change should be applied to.</param>
        public void SetFontSize(double emSize, int startIndex, int count)
        {
            ValidateFontSize(emSize);

            var limit = ValidateRange(startIndex, count);
            for (var i = startIndex; i < limit;)
            {
                var formatRider = new SpanRider(_formatRuns, _latestPosition, i);

                i = Math.Min(limit, i + formatRider.Length);

#pragma warning disable 6506
                // Presharp warns that runProps is not validated, but it can never be null 
                // because the rider is already checked to be in range

                if (!(formatRider.CurrentElement is GenericTextRunProperties runProps))
                {
                    throw new NotSupportedException($"{nameof(runProps)} can not be null.");
                }

                if (runProps.FontRenderingEmSize == emSize)
                {
                    continue;
                }

                var newProps = new GenericTextRunProperties(
                    runProps.Typeface,
                    emSize,
                    runProps.TextDecorations,
                    runProps.ForegroundBrush,
                    runProps.BackgroundBrush,
                    runProps.BaselineAlignment,
                    runProps.CultureInfo
                );

                _latestPosition = _formatRuns.SetValue(formatRider.CurrentPosition, i - formatRider.CurrentPosition,
                    newProps, formatRider.SpanPosition);

#pragma warning restore 6506
                InvalidateMetrics();
            }
        }

        /// <summary>
        /// Sets or changes the culture for the text object.
        /// </summary>
        /// <param name="culture">The new culture for the text object.</param>
        public void SetCulture(CultureInfo culture)
        {
            SetCulture(culture, 0, _text.Length);
        }

        /// <summary>
        /// Sets or changes the culture for the text object.
        /// </summary>
        /// <param name="culture">The new culture for the text object.</param>
        /// <param name="startIndex">The start index of initial character to apply the change to.</param>
        /// <param name="count">The number of characters the change should be applied to.</param>
        public void SetCulture(CultureInfo culture, int startIndex, int count)
        {
            if (culture is null)
            {
                throw new ArgumentNullException(nameof(culture));
            }

            var limit = ValidateRange(startIndex, count);

            for (var i = startIndex; i < limit;)
            {
                var formatRider = new SpanRider(_formatRuns, _latestPosition, i);

                i = Math.Min(limit, i + formatRider.Length);

#pragma warning disable 6506 
                // Presharp warns that runProps is not validated, but it can never be null 
                // because the rider is already checked to be in range

                if (!(formatRider.CurrentElement is GenericTextRunProperties runProps))
                {
                    throw new NotSupportedException($"{nameof(runProps)} can not be null.");
                }

                if (runProps.CultureInfo.Equals(culture))
                {
                    continue;
                }

                var newProps = new GenericTextRunProperties(
                    runProps.Typeface,
                    runProps.FontRenderingEmSize,
                    runProps.TextDecorations,
                    runProps.ForegroundBrush,
                    runProps.BackgroundBrush,
                    runProps.BaselineAlignment,
                    culture
                );

#pragma warning restore 6506
                _latestPosition = _formatRuns.SetValue(formatRider.CurrentPosition, i - formatRider.CurrentPosition,
                    newProps, formatRider.SpanPosition);

                InvalidateMetrics();
            }
        }

        /// <summary>
        /// Sets or changes the font weight
        /// </summary>
        /// <param name="weight">Font weight</param>
        public void SetFontWeight(FontWeight weight)
        {
            SetFontWeight(weight, 0, _text.Length);
        }

        /// <summary>
        /// Sets or changes the font weight
        /// </summary>
        /// <param name="weight">Font weight</param>
        /// <param name="startIndex">The start index of initial character to apply the change to.</param>
        /// <param name="count">The number of characters the change should be applied to.</param>
        public void SetFontWeight(FontWeight weight, int startIndex, int count)
        {
            var limit = ValidateRange(startIndex, count);

            for (var i = startIndex; i < limit;)
            {
                var formatRider = new SpanRider(_formatRuns, _latestPosition, i);

                i = Math.Min(limit, i + formatRider.Length);

#pragma warning disable 6506 
                // Presharp warns that runProps is not validated, but it can never be null 
                // because the rider is already checked to be in range

                if (!(formatRider.CurrentElement is GenericTextRunProperties runProps))
                {
                    throw new NotSupportedException($"{nameof(runProps)} can not be null.");
                }

                var oldTypeface = runProps.Typeface;

                if (oldTypeface.Weight == weight)
                {
                    continue;
                }

                var newProps = new GenericTextRunProperties(
                    new Typeface(oldTypeface.FontFamily, oldTypeface.Style, weight),
                    runProps.FontRenderingEmSize,
                    runProps.TextDecorations,
                    runProps.ForegroundBrush,
                    runProps.BackgroundBrush,
                    runProps.BaselineAlignment,
                    runProps.CultureInfo
                    );
#pragma warning restore 6506 
                _latestPosition = _formatRuns.SetValue(formatRider.CurrentPosition, i - formatRider.CurrentPosition, newProps, formatRider.SpanPosition);

                InvalidateMetrics();
            }
        }

        /// <summary>
        /// Sets or changes the font style
        /// </summary>
        /// <param name="style">Font style</param>
        public void SetFontStyle(FontStyle style)
        {
            SetFontStyle(style, 0, _text.Length);
        }

        /// <summary>
        /// Sets or changes the font style
        /// </summary>
        /// <param name="style">Font style</param>
        /// <param name="startIndex">The start index of initial character to apply the change to.</param>
        /// <param name="count">The number of characters the change should be applied to.</param>
        public void SetFontStyle(FontStyle style, int startIndex, int count)
        {
            var limit = ValidateRange(startIndex, count);
            for (var i = startIndex; i < limit;)
            {
                var formatRider = new SpanRider(_formatRuns, _latestPosition, i);

                i = Math.Min(limit, i + formatRider.Length);

#pragma warning disable 6506 
                // Presharp warns that runProps is not validated, but it can never be null 
                // because the rider is already checked to be in range

                if (!(formatRider.CurrentElement is GenericTextRunProperties runProps))
                {
                    throw new NotSupportedException($"{nameof(runProps)} can not be null.");
                }

                var oldTypeface = runProps.Typeface;

                if (oldTypeface.Style == style)
                {
                    continue;
                }

                var newProps = new GenericTextRunProperties(
                    new Typeface(oldTypeface.FontFamily, style, oldTypeface.Weight),
                    runProps.FontRenderingEmSize,
                    runProps.TextDecorations,
                    runProps.ForegroundBrush,
                    runProps.BackgroundBrush,
                    runProps.BaselineAlignment,
                    runProps.CultureInfo
                    );
#pragma warning restore 6506

                _latestPosition = _formatRuns.SetValue(formatRider.CurrentPosition, i - formatRider.CurrentPosition, newProps, formatRider.SpanPosition);

                InvalidateMetrics(); // invalidate cached metrics
            }
        }

        /// <summary>
        /// Sets or changes the type face
        /// </summary>
        /// <param name="typeface">Typeface</param>
        public void SetFontTypeface(Typeface typeface)
        {
            SetFontTypeface(typeface, 0, _text.Length);
        }

        /// <summary>
        /// Sets or changes the type face
        /// </summary>
        /// <param name="typeface">Typeface</param>
        /// <param name="startIndex">The start index of initial character to apply the change to.</param>
        /// <param name="count">The number of characters the change should be applied to.</param>
        public void SetFontTypeface(Typeface typeface, int startIndex, int count)
        {
            var limit = ValidateRange(startIndex, count);

            for (var i = startIndex; i < limit;)
            {
                var formatRider = new SpanRider(_formatRuns, _latestPosition, i);

                i = Math.Min(limit, i + formatRider.Length);

#pragma warning disable 6506 
                // Presharp warns that runProps is not validated, but it can never be null 
                // because the rider is already checked to be in range

                if (!(formatRider.CurrentElement is GenericTextRunProperties runProps))
                {
                    throw new NotSupportedException($"{nameof(runProps)} can not be null.");
                }

                if (runProps.Typeface == typeface)
                {
                    continue;
                }

                var newProps = new GenericTextRunProperties(
                    typeface,
                    runProps.FontRenderingEmSize,
                    runProps.TextDecorations,
                    runProps.ForegroundBrush,
                    runProps.BackgroundBrush,
                    runProps.BaselineAlignment,
                    runProps.CultureInfo
                    );
#pragma warning restore 6506

                _latestPosition = _formatRuns.SetValue(formatRider.CurrentPosition, i - formatRider.CurrentPosition,
                    newProps, formatRider.SpanPosition);

                InvalidateMetrics();
            }
        }

        /// <summary>
        /// Sets or changes the text decorations
        /// </summary>
        /// <param name="textDecorations">Text decorations</param>
        public void SetTextDecorations(TextDecorationCollection textDecorations)
        {
            SetTextDecorations(textDecorations, 0, _text.Length);
        }

        /// <summary>
        /// Sets or changes the text decorations
        /// </summary>
        /// <param name="textDecorations">Text decorations</param>
        /// <param name="startIndex">The start index of initial character to apply the change to.</param>
        /// <param name="count">The number of characters the change should be applied to.</param>
        public void SetTextDecorations(TextDecorationCollection textDecorations, int startIndex, int count)
        {
            var limit = ValidateRange(startIndex, count);

            for (var i = startIndex; i < limit;)
            {
                var formatRider = new SpanRider(_formatRuns, _latestPosition, i);

                i = Math.Min(limit, i + formatRider.Length);

#pragma warning disable 6506 
                // Presharp warns that runProps is not validated, but it can never be null 
                // because the rider is already checked to be in range

                if (!(formatRider.CurrentElement is GenericTextRunProperties runProps))
                {
                    throw new NotSupportedException($"{nameof(runProps)} can not be null.");
                }

                if (runProps.TextDecorations == textDecorations)
                {
                    continue;
                }

                var newProps = new GenericTextRunProperties(
                    runProps.Typeface,
                    runProps.FontRenderingEmSize,
                    textDecorations,
                    runProps.ForegroundBrush,
                    runProps.BackgroundBrush,
                    runProps.BaselineAlignment,
                    runProps.CultureInfo
                    );
#pragma warning restore 6506

                _latestPosition = _formatRuns.SetValue(formatRider.CurrentPosition, i - formatRider.CurrentPosition,
                    newProps, formatRider.SpanPosition);
            }
        }

        /// Note: enumeration is temporarily made private
        /// because of PS #828532
        /// 
        /// <summary>
        /// Strongly typed enumerator used for enumerating text lines
        /// </summary>
        private struct LineEnumerator : IEnumerator, IDisposable
        {
            private int _lineCount;
            private double _totalHeight;
            private TextLine _nextLine;
            private readonly TextFormatter _formatter;
            private readonly FormattedText _that;

            // these are needed because _currentLine can be disposed before the next MoveNext() call
            private double _previousHeight;

            // line break before _currentLine, needed in case we have to reformat it with collapsing symbol
            private TextLineBreak _previousLineBreak;

            internal LineEnumerator(FormattedText text)
            {
                _previousHeight = 0;
                Length = 0;
                _previousLineBreak = null;

                Position = 0;
                _lineCount = 0;
                _totalHeight = 0;
                Current = null;
                _nextLine = null;
                _formatter = TextFormatter.Current;
                _that = text;
                _that._textSourceImpl ??= new TextSourceImplementation(_that);
            }

            public void Dispose()
            {
                Current = null;

                _nextLine = null;
            }

            private int Position { get; set; }

            private int Length { get; set; }

            /// <summary>
            /// Gets the current text line in the collection
            /// </summary>
            public TextLine Current { get; private set; }

            /// <summary>
            /// Gets the current text line in the collection
            /// </summary>
            object IEnumerator.Current => Current;

            /// <summary>
            /// Gets the paragraph width used to format the current text line
            /// </summary>
            internal double CurrentParagraphWidth
            {
                get
                {
                    return MaxLineLength(_lineCount);
                }
            }

            private double MaxLineLength(int line)
            {
                if (_that._maxTextWidths == null)
                    return _that._maxTextWidth;
                return _that._maxTextWidths[Math.Min(line, _that._maxTextWidths.Length - 1)];
            }

            /// <summary>
            /// Advances the enumerator to the next text line of the collection
            /// </summary>
            /// <returns>true if the enumerator was successfully advanced to the next element;
            /// false if the enumerator has passed the end of the collection</returns>
            public bool MoveNext()
            {
                if (Current == null)
                {   // this is the first line
                    if (_that._text.Length == 0)
                    {
                        return false;
                    }

                    Current = FormatLine(
                        _that._textSourceImpl,
                        Position,
                        MaxLineLength(_lineCount),
                        _that._defaultParaProps,
                        null // no previous line break
                        );

                    // check if this line fits the text height
                    if (_totalHeight + Current.Height > _that._maxTextHeight)
                    {
                        Current = null;

                        return false;
                    }
                    Debug.Assert(_nextLine == null);
                }
                else
                {
                    // there is no next line or it didn't fit
                    // either way we're finished
                    if (_nextLine == null)
                    {
                        return false;
                    }

                    _totalHeight += _previousHeight;
                    Position += Length;
                    ++_lineCount;

                    Current = _nextLine;
                    _nextLine = null;
                }

                var currentLineBreak = Current.TextLineBreak;

                // this line is guaranteed to fit the text height
                Debug.Assert(_totalHeight + Current.Height <= _that._maxTextHeight);

                // now, check if the next line fits, we need to do this on this iteration
                // because we might need to add ellipsis to the current line
                // as a result of the next line measurement

                // maybe there is no next line at all
                if (Position + Current.TextRange.Length < _that._text.Length)
                {
                    bool nextLineFits;

                    if (_lineCount + 1 >= _that._maxLineCount)
                    {
                        nextLineFits = false;
                    }
                    else
                    {
                        _nextLine = FormatLine(
                            _that._textSourceImpl,
                            Position + Current.TextRange.Length,
                            MaxLineLength(_lineCount + 1),
                            _that._defaultParaProps,
                            currentLineBreak
                            );

                        nextLineFits = (_totalHeight + Current.Height + _nextLine.Height <= _that._maxTextHeight);
                    }

                    if (!nextLineFits)
                    {
                        _nextLine = null;

                        if (_that._trimming != TextTrimming.None && !Current.HasCollapsed)
                        {
                            // recreate the current line with ellipsis added
                            // Note: Paragraph ellipsis is not supported today. We'll workaround
                            // it here by faking a non-wrap text on finite column width.
                            var currentWrap = _that._defaultParaProps.TextWrapping;

                            _that._defaultParaProps.SetTextWrapping(TextWrapping.NoWrap);

                            Current = FormatLine(
                                _that._textSourceImpl,
                                Position,
                                MaxLineLength(_lineCount),
                                _that._defaultParaProps,
                                _previousLineBreak
                                );

                            currentLineBreak = Current.TextLineBreak;

                            _that._defaultParaProps.SetTextWrapping(currentWrap);
                        }
                    }
                }

                _previousHeight = Current.Height;

                Length = Current.TextRange.Length;

                _previousLineBreak = currentLineBreak;

                return true;
            }

            /// <summary>
            /// Wrapper of TextFormatter.FormatLine that auto-collapses the line if needed.
            /// </summary>
            private TextLine FormatLine(ITextSource textSource, int textSourcePosition, double maxLineLength, TextParagraphProperties paraProps, TextLineBreak lineBreak)
            {
                var line = _formatter.FormatLine(
                    textSource,
                    textSourcePosition,
                    maxLineLength,
                    paraProps,
                    lineBreak
                    );

                if (_that._trimming != TextTrimming.None && line.HasOverflowed && line.TextRange.Length > 0)
                {
                    // what I really need here is the last displayed text run of the line
                    // textSourcePosition + line.Length - 1 works except the end of paragraph case,
                    // where line length includes the fake paragraph break run
                    Debug.Assert(_that._text.Length > 0 && textSourcePosition + line.TextRange.Length <= _that._text.Length + 1);

                    var thatFormatRider = new SpanRider(
                        _that._formatRuns,
                        _that._latestPosition,
                        Math.Min(textSourcePosition + line.TextRange.Length - 1, _that._text.Length - 1)
                        );

                    var lastRunProps = thatFormatRider.CurrentElement as GenericTextRunProperties;

                    TextCollapsingProperties trailingEllipsis;

                    if (_that._trimming == TextTrimming.CharacterEllipsis)
                    {
                        trailingEllipsis = new TextTrailingCharacterEllipsis(maxLineLength, lastRunProps);
                    }
                    else
                    {
                        Debug.Assert(_that._trimming == TextTrimming.WordEllipsis);
                        trailingEllipsis = new TextTrailingWordEllipsis(maxLineLength, lastRunProps);
                    }

                    var collapsedLine = line.Collapse(trailingEllipsis);

                    line = collapsedLine;
                }
                return line;
            }


            /// <summary>
            /// Sets the enumerator to its initial position,
            /// which is before the first element in the collection
            /// </summary>
            public void Reset()
            {
                Position = 0;
                _lineCount = 0;
                _totalHeight = 0;
                Current = null;
                _nextLine = null;
            }
        }

        /// <summary>
        /// Returns an enumerator that can iterate through the text line collection
        /// </summary>
        private LineEnumerator GetEnumerator()
        {
            return new LineEnumerator(this);
        }
#if NEVER
        /// <summary>
        /// Returns an enumerator that can iterate through the text line collection
        /// </summary>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
#endif

        private void AdvanceLineOrigin(ref Point lineOrigin, TextLine currentLine)
        {
            var height = currentLine.Height;

            // advance line origin according to the flow direction
            switch (_defaultParaProps.FlowDirection)
            {
                case FlowDirection.LeftToRight:
                case FlowDirection.RightToLeft:
                    lineOrigin = lineOrigin.WithY(lineOrigin.Y + height);
                    break;
            }
        }

        private class CachedMetrics
        {
            // vertical
            public double Height;
            public double Baseline;

            // horizontal
            public double Width;
            public double WidthIncludingTrailingWhitespace;

            // vertical bounding box metrics
            public double Extent;
            public double OverhangAfter;

            // horizontal bounding box metrics
            public double OverhangLeading;
            public double OverhangTrailing;
        }

        /// <summary>
        /// Defines the flow direction
        /// </summary>
        public FlowDirection FlowDirection
        {
            set
            {
                ValidateFlowDirection(value, "value");
                _defaultParaProps.SetFlowDirection(value);
                InvalidateMetrics();
            }
            get
            {
                return _defaultParaProps.FlowDirection;
            }
        }

        /// <summary>
        /// Defines the alignment of text within the column
        /// </summary>
        public TextAlignment TextAlignment
        {
            set
            {
                _defaultParaProps.SetTextAlignment(value);
                InvalidateMetrics();
            }
            get
            {
                return _defaultParaProps.TextAlignment;
            }
        }

        /// <summary>
        /// Gets or sets the height of, or the spacing between, each line where
        /// zero represents the default line height.
        /// </summary>
        public double LineHeight
        {
            set
            {
                if (value < 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), "Parameter must be greater than or equal to zero.");
                }

                _defaultParaProps.SetLineHeight(value);

                InvalidateMetrics();
            }
            get
            {
                return _defaultParaProps.LineHeight;
            }
        }

        /// <summary>
        /// The MaxTextWidth property defines the alignment edges for the FormattedText.
        /// For example, left aligned text is wrapped such that the leftmost glyph alignment point
        /// on each line falls exactly on the left edge of the rectangle.
        /// Note that for many fonts, especially in italic style, some glyph strokes may extend beyond the edges of the alignment rectangle.
        /// For this reason, it is recommended that clients draw text with at least 1/6 em (i.e of the font size) unused margin space either side.
        /// Zero value of MaxTextWidth is equivalent to the maximum possible paragraph width.
        /// </summary>
        public double MaxTextWidth
        {
            set
            {
                if (value < 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), "Parameter must be greater than or equal to zero.");
                }

                _maxTextWidth = value;

                InvalidateMetrics();
            }
            get
            {
                return _maxTextWidth;
            }
        }

        /// <summary>
        /// Sets the array of lengths,
        /// which will be applied to each line of text in turn.
        /// If the text covers more lines than there are entries in the length array,
        /// the last entry is reused as many times as required.
        /// The maxTextWidths array overrides the MaxTextWidth property.
        /// </summary>
        /// <param name="maxTextWidths">The max text width array</param>
        public void SetMaxTextWidths(double[] maxTextWidths)
        {
            if (maxTextWidths == null || maxTextWidths.Length <= 0)
            {
                throw new ArgumentNullException(nameof(maxTextWidths));
            }

            _maxTextWidths = maxTextWidths;

            InvalidateMetrics();
        }

        /// <summary>
        /// Obtains a copy of the array of lengths,
        /// which will be applied to each line of text in turn.
        /// If the text covers more lines than there are entries in the length array,
        /// the last entry is reused as many times as required.
        /// The maxTextWidths array overrides the MaxTextWidth property.
        /// </summary>
        /// <returns>The copy of max text width array</returns>
        public double[] GetMaxTextWidths()
        {
            return (double[])_maxTextWidths?.Clone();
        }

        /// <summary>
        /// Sets the maximum length of a column of text.
        /// The last line of text displayed is the last whole line that will fit within this limit,
        /// or the nth line as specified by MaxLineCount, whichever occurs first.
        /// Use the Trimming property to control how the omission of text is indicated.
        /// </summary>
        public double MaxTextHeight
        {
            set
            {
                if (value <= 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), $"'{nameof(MaxTextHeight)}' property value must be greater than zero.");
                }

                if (double.IsNaN(value))
                {
                    throw new ArgumentOutOfRangeException(nameof(value), $"'{nameof(MaxTextHeight)}' property value cannot be NaN.");
                }

                _maxTextHeight = value;

                InvalidateMetrics();
            }
            get
            {
                return _maxTextHeight;
            }
        }

        /// <summary>
        /// Defines the maximum number of lines to display.
        /// The last line of text displayed is the lineCount-1'th line,
        /// or the last whole line that will fit within the count set by MaxTextHeight,
        /// whichever occurs first.
        /// Use the Trimming property to control how the omission of text is indicated
        /// </summary>
        public int MaxLineCount
        {
            set
            {
                if (value <= 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), "The parameter value must be greater than zero.");
                }

                _maxLineCount = value;

                InvalidateMetrics();
            }
            get
            {
                return _maxLineCount;
            }
        }

        /// <summary>
        /// Defines how omission of text is indicated.
        /// CharacterEllipsis trimming allows partial words to be displayed,
        /// while WordEllipsis removes whole words to fit.
        /// Both guarantee to include an ellipsis ('...') at the end of the lines
        /// where text has been trimmed as a result of line and column limits.
        /// </summary>
        public TextTrimming Trimming
        {
            set
            {
                if ((int)value < 0 || (int)value > (int)TextTrimming.WordEllipsis)
                {
                    throw new InvalidEnumArgumentException(nameof(value), (int)value, typeof(TextTrimming));
                }

                _trimming = value;

                _defaultParaProps.SetTextWrapping(_trimming == TextTrimming.None ?
                    TextWrapping.Wrap :
                    TextWrapping.WrapWithOverflow);

                InvalidateMetrics();
            }
            get
            {
                return _trimming;
            }
        }

        /// <summary>
        /// Lazily initializes the cached metrics EXCEPT for black box metrics and
        /// returns the CachedMetrics structure.
        /// </summary>
        private CachedMetrics Metrics
        {
            get
            {
                return _metrics ??= DrawAndCalculateMetrics(
                    null, // drawing context
                    new Point(), // drawing offset
                    false);
            }
        }

        /// <summary>
        /// Lazily initializes the cached metrics INCLUDING black box metrics and
        /// returns the CachedMetrics structure.
        /// </summary>
        private CachedMetrics BlackBoxMetrics
        {
            get
            {
                if (_metrics == null || double.IsNaN(_metrics.Extent))
                {
                    // We need to obtain the metrics, including black box metrics.

                    _metrics = DrawAndCalculateMetrics(
                        null,           // drawing context
                        new Point(),    // drawing offset
                        true);          // calculate black box metrics
                }
                return _metrics;
            }
        }

        /// <summary>
        /// The distance from the top of the first line to the bottom of the last line.
        /// </summary>
        public double Height
        {
            get
            {
                return Metrics.Height;
            }
        }

        /// <summary>
        /// The distance from the topmost black pixel of the first line
        /// to the bottommost black pixel of the last line. 
        /// </summary>
        public double Extent
        {
            get
            {
                return BlackBoxMetrics.Extent;
            }
        }

        /// <summary>
        /// The distance from the top of the first line to the baseline of the first line.
        /// </summary>
        public double Baseline
        {
            get
            {
                return Metrics.Baseline;
            }
        }

        /// <summary>
        /// The distance from the bottom of the last line to the extent bottom.
        /// </summary>
        public double OverhangAfter
        {
            get
            {
                return BlackBoxMetrics.OverhangAfter;
            }
        }

        /// <summary>
        /// The maximum distance from the leading black pixel to the leading alignment point of a line.
        /// </summary>
        public double OverhangLeading
        {
            get
            {
                return BlackBoxMetrics.OverhangLeading;
            }
        }

        /// <summary>
        /// The maximum distance from the trailing black pixel to the trailing alignment point of a line.
        /// </summary>
        public double OverhangTrailing
        {
            get
            {
                return BlackBoxMetrics.OverhangTrailing;
            }
        }

        /// <summary>
        /// The maximum advance width between the leading and trailing alignment points of a line,
        /// excluding the width of whitespace characters at the end of the line.
        /// </summary>
        public double Width
        {
            get
            {
                return Metrics.Width;
            }
        }

        /// <summary>
        /// The maximum advance width between the leading and trailing alignment points of a line,
        /// including the width of whitespace characters at the end of the line.
        /// </summary>
        public double WidthIncludingTrailingWhitespace
        {
            get
            {
                return Metrics.WidthIncludingTrailingWhitespace;
            }
        }

        /// <summary>
        /// Draws the text object
        /// </summary>
        internal void Draw(DrawingContext drawingContext, Point origin)
        {
            var lineOrigin = origin;

            if (_metrics != null && !double.IsNaN(_metrics.Extent))
            {
                // we can't use foreach because it requires GetEnumerator and associated classes to be public
                // foreach (TextLine currentLine in this)
                using (var enumerator = GetEnumerator())
                {
                    while (enumerator.MoveNext())
                    {
                        var currentLine = enumerator.Current;

                        currentLine.Draw(drawingContext, lineOrigin);

                        AdvanceLineOrigin(ref lineOrigin, currentLine);
                    }
                }
            }
            else
            {
                // Calculate metrics as we draw to avoid formatting again if we need metrics later; we compute
                // black box metrics too because these are already known as a side-effect of drawing

                _metrics = DrawAndCalculateMetrics(drawingContext, origin, true);
            }
        }

        private CachedMetrics DrawAndCalculateMetrics(DrawingContext drawingContext, Point drawingOffset, bool getBlackBoxMetrics)
        {
            var metrics = new CachedMetrics();

            if (_text.Length == 0)
            {
                return metrics;
            }

            // we can't use foreach because it requires GetEnumerator and associated classes to be public
            // foreach (TextLine currentLine in this)

            using (var enumerator = GetEnumerator())
            {
                var first = true;

                double accBlackBoxLeft, accBlackBoxTop, accBlackBoxRight, accBlackBoxBottom;
                accBlackBoxLeft = accBlackBoxTop = double.MaxValue;
                accBlackBoxRight = accBlackBoxBottom = double.MinValue;

                var origin = new Point(0, 0);

                // Holds the TextLine.Start of the longest line. Thus it will hold the minimum value 
                // of TextLine.Start among all the lines that forms the text. The overhangs (leading and trailing) 
                // are calculated with an offset as a result of the same issue with TextLine.Start. 
                // So, we compute this offset and remove it later from the values of the overhangs.
                var lineStartOfLongestLine = double.MaxValue;

                while (enumerator.MoveNext())
                {
                    // enumerator will dispose the currentLine
                    var currentLine = enumerator.Current;

                    // if we're drawing, do it first as this will compute black box metrics as a side-effect
                    if (drawingContext != null)
                    {
                        currentLine.Draw(drawingContext,
                            new Point(origin.X + drawingOffset.X, origin.Y + drawingOffset.Y));
                    }

                    if (getBlackBoxMetrics)
                    {
                        var blackBoxLeft = origin.X + currentLine.Start + currentLine.OverhangLeading;
                        var blackBoxRight = origin.X + currentLine.Start + currentLine.Width - currentLine.OverhangTrailing;
                        var blackBoxBottom = origin.Y + currentLine.Height + currentLine.OverhangAfter;
                        var blackBoxTop = blackBoxBottom - currentLine.Extent;

                        accBlackBoxLeft = Math.Min(accBlackBoxLeft, blackBoxLeft);
                        accBlackBoxRight = Math.Max(accBlackBoxRight, blackBoxRight);
                        accBlackBoxBottom = Math.Max(accBlackBoxBottom, blackBoxBottom);
                        accBlackBoxTop = Math.Min(accBlackBoxTop, blackBoxTop);

                        metrics.OverhangAfter = currentLine.OverhangAfter;
                    }

                    metrics.Height += currentLine.Height;
                    metrics.Width = Math.Max(metrics.Width, currentLine.Width);
                    metrics.WidthIncludingTrailingWhitespace = Math.Max(metrics.WidthIncludingTrailingWhitespace, currentLine.WidthIncludingTrailingWhitespace);
                    lineStartOfLongestLine = Math.Min(lineStartOfLongestLine, currentLine.Start);

                    if (first)
                    {
                        metrics.Baseline = currentLine.Baseline;
                        first = false;
                    }

                    AdvanceLineOrigin(ref origin, currentLine);
                }

                if (getBlackBoxMetrics)
                {
                    metrics.Extent = accBlackBoxBottom - accBlackBoxTop;
                    metrics.OverhangLeading = accBlackBoxLeft - lineStartOfLongestLine;
                    metrics.OverhangTrailing = metrics.Width - (accBlackBoxRight - lineStartOfLongestLine);
                }
                else
                {
                    // indicate that black box metrics are not known
                    metrics.Extent = double.NaN;
                }
            }

            return metrics;
        }

        private class TextSourceImplementation : ITextSource
        {
            private readonly FormattedText _that;

            public TextSourceImplementation(FormattedText text)
            {
                _that = text;
            }

            /// <inheritdoc/>
            public TextRun GetTextRun(int textSourceCharacterIndex)
            {
                if (textSourceCharacterIndex >= _that._text.Length)
                {
                    return null;
                }

                var thatFormatRider = new SpanRider(_that._formatRuns, _that._latestPosition, textSourceCharacterIndex);

                TextRunProperties properties = thatFormatRider.CurrentElement as GenericTextRunProperties;

                var textCharacters = new TextCharacters(_that._text, textSourceCharacterIndex, thatFormatRider.Length,
                    properties);

                return textCharacters;
            }
        }
    }
}
