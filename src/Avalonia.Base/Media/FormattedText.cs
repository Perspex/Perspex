using System.Collections.Generic;
using Avalonia.Platform;

namespace Avalonia.Media
{
    /// <summary>
    /// Represents a piece of text with formatting.
    /// </summary>
    public class FormattedText
    {
        private readonly IPlatformRenderInterface _platform;
        private Size _constraint = Size.Infinity;
        private IFormattedTextImpl _platformImpl;
        private IReadOnlyList<FormattedTextStyleSpan> _spans;
        private Typeface _typeface;
        private double _fontSize;
        private string _text;
        private TextAlignment _textAlignment;
        private TextWrapping _textWrapping;

        /// <summary>
        /// Initializes a new instance of the <see cref="FormattedText"/> class.
        /// </summary>
        public FormattedText()
        {
            _platform = AvaloniaLocator.Current.GetService<IPlatformRenderInterface>();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FormattedText"/> class.
        /// </summary>
        /// <param name="platform">The platform render interface.</param>
        public FormattedText(IPlatformRenderInterface platform)
        {
            _platform = platform;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FormattedText"/> class.
        /// </summary>
        /// <param name="text"></param>
        /// <param name="typeface"></param>
        /// <param name="fontSize"></param>
        /// <param name="textAlignment"></param>
        /// <param name="textWrapping"></param>
        /// <param name="constraint"></param>
        public FormattedText(string text, Typeface typeface, double fontSize, TextAlignment textAlignment,
            TextWrapping textWrapping, Size constraint) : this()
        {
            _text = text;

            _typeface = typeface;

            _fontSize = fontSize;

            _textAlignment = textAlignment;

            _textWrapping = textWrapping;

            _constraint = constraint;
        }

        /// <summary>
        /// Gets the bounds of the text within the <see cref="Constraint"/>.
        /// </summary>
        /// <returns>The bounds of the text.</returns>
        public Rect Bounds => PlatformImpl.Bounds;

        /// <summary>
        /// Gets or sets the constraint of the text.
        /// </summary>
        public Size Constraint
        {
            get => _constraint;
            set => Set(ref _constraint, value);
        }

        /// <summary>
        /// Gets or sets the base typeface.
        /// </summary>
        public Typeface Typeface
        {
            get => _typeface;
            set => Set(ref _typeface, value);
        }


        /// <summary>
        /// Gets or sets the font size.
        /// </summary>
        public double FontSize
        {
            get => _fontSize;
            set => Set(ref _fontSize, value);
        }

        /// <summary>
        /// Gets or sets a collection of spans that describe the formatting of subsections of the
        /// text.
        /// </summary>
        public IReadOnlyList<FormattedTextStyleSpan> Spans
        {
            get => _spans;
            set => Set(ref _spans, value);
        }

        /// <summary>
        /// Gets or sets the text.
        /// </summary>
        public string Text
        {
            get => _text;
            set => Set(ref _text, value);
        }

        /// <summary>
        /// Gets or sets the alignment of the text.
        /// </summary>
        public TextAlignment TextAlignment
        {
            get => _textAlignment;
            set => Set(ref _textAlignment, value);
        }

        /// <summary>
        /// Gets or sets the text wrapping.
        /// </summary>
        public TextWrapping TextWrapping
        {
            get => _textWrapping;
            set => Set(ref _textWrapping, value);
        }

        /// <summary>
        /// Gets platform-specific platform implementation.
        /// </summary>
        public IFormattedTextImpl PlatformImpl
        {
            get
            {
                if (_platformImpl == null)
                {
                    _platformImpl = _platform.CreateFormattedText(
                        _text,
                        _typeface,
                        _fontSize,
                        _textAlignment,
                        _textWrapping,
                        _constraint,
                        _spans);
                }

                return _platformImpl;
            }
        }

        /// <summary>
        /// Gets the lines in the text.
        /// </summary>
        /// <returns>
        /// A collection of <see cref="FormattedTextLine"/> objects.
        /// </returns>
        public IEnumerable<FormattedTextLine> GetLines()
        {
            return PlatformImpl.GetLines();
        }

        /// <summary>
        /// Hit tests a point in the text.
        /// </summary>
        /// <param name="point">The point.</param>
        /// <returns>
        /// A <see cref="TextHitTestResult"/> describing the result of the hit test.
        /// </returns>
        public TextHitTestResult HitTestPoint(Point point)
        {
            return PlatformImpl.HitTestPoint(point);
        }

        /// <summary>
        /// Gets the bounds rectangle that the specified character occupies.
        /// </summary>
        /// <param name="index">The index of the character.</param>
        /// <returns>The character bounds.</returns>
        public Rect HitTestTextPosition(int index)
        {
            return PlatformImpl.HitTestTextPosition(index);
        }

        /// <summary>
        /// Gets the bounds rectangles that the specified text range occupies.
        /// </summary>
        /// <param name="index">The index of the first character.</param>
        /// <param name="length">The number of characters in the text range.</param>
        /// <returns>The character bounds.</returns>
        public IEnumerable<Rect> HitTestTextRange(int index, int length)
        {
            return PlatformImpl.HitTestTextRange(index, length);
        }

        private void Set<T>(ref T field, T value)
        {
            if (field != null && field.Equals(value))
            {
                return;
            }

            field = value;

            _platformImpl = null;
        }
    }
}
