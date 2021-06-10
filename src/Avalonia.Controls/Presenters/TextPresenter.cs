using System;
using System.Reactive.Linq;
using Avalonia.Input.TextInput;
using Avalonia.Media;
using Avalonia.Metadata;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Avalonia.Layout;
using Avalonia.Media.Immutable;

namespace Avalonia.Controls.Presenters
{
    public class TextPresenter : Control
    {
        public static readonly DirectProperty<TextPresenter, int> CaretIndexProperty =
            TextBox.CaretIndexProperty.AddOwner<TextPresenter>(
                o => o.CaretIndex,
                (o, v) => o.CaretIndex = v);

        public static readonly StyledProperty<bool> RevealPasswordProperty =
            AvaloniaProperty.Register<TextPresenter, bool>(nameof(RevealPassword));

        public static readonly StyledProperty<char> PasswordCharProperty =
            AvaloniaProperty.Register<TextPresenter, char>(nameof(PasswordChar));

        public static readonly StyledProperty<IBrush> SelectionBrushProperty =
            AvaloniaProperty.Register<TextPresenter, IBrush>(nameof(SelectionBrushProperty));

        public static readonly StyledProperty<IBrush> SelectionForegroundBrushProperty =
            AvaloniaProperty.Register<TextPresenter, IBrush>(nameof(SelectionForegroundBrushProperty));

        public static readonly StyledProperty<IBrush> CaretBrushProperty =
            AvaloniaProperty.Register<TextPresenter, IBrush>(nameof(CaretBrushProperty));

        public static readonly DirectProperty<TextPresenter, int> SelectionStartProperty =
            TextBox.SelectionStartProperty.AddOwner<TextPresenter>(
                o => o.SelectionStart,
                (o, v) => o.SelectionStart = v);

        public static readonly DirectProperty<TextPresenter, int> SelectionEndProperty =
            TextBox.SelectionEndProperty.AddOwner<TextPresenter>(
                o => o.SelectionEnd,
                (o, v) => o.SelectionEnd = v);

        /// <summary>
        /// Defines the <see cref="Text"/> property.
        /// </summary>
        public static readonly DirectProperty<TextPresenter, string> TextProperty =
            AvaloniaProperty.RegisterDirect<TextPresenter, string>(
                nameof(Text),
                o => o.Text,
                (o, v) => o.Text = v);

        /// <summary>
        /// Defines the <see cref="TextAlignment"/> property.
        /// </summary>
        public static readonly StyledProperty<TextAlignment> TextAlignmentProperty =
            TextBlock.TextAlignmentProperty.AddOwner<TextPresenter>();

        /// <summary>
        /// Defines the <see cref="TextWrapping"/> property.
        /// </summary>
        public static readonly StyledProperty<TextWrapping> TextWrappingProperty =
            TextBlock.TextWrappingProperty.AddOwner<TextPresenter>();

        /// <summary>
        /// Defines the <see cref="Background"/> property.
        /// </summary>
        public static readonly StyledProperty<IBrush> BackgroundProperty =
            Border.BackgroundProperty.AddOwner<TextPresenter>();

        private readonly DispatcherTimer _caretTimer;
        private int _caretIndex;
        private int _selectionStart;
        private int _selectionEnd;
        private bool _caretBlink;
        private string _text;
        private FormattedText _formattedText;
        private Size _constraint;

        static TextPresenter()
        {
            AffectsRender<TextPresenter>(SelectionBrushProperty, TextBlock.ForegroundProperty, 
                                         SelectionForegroundBrushProperty, CaretBrushProperty,
                                         SelectionStartProperty, SelectionEndProperty);
            
            AffectsMeasure<TextPresenter>(TextProperty, PasswordCharProperty, RevealPasswordProperty, 
                TextAlignmentProperty, TextWrappingProperty, TextBlock.FontSizeProperty,
                TextBlock.FontStyleProperty, TextBlock.FontWeightProperty, TextBlock.FontFamilyProperty);

            Observable.Merge<AvaloniaPropertyChangedEventArgs>(TextProperty.Changed, TextBlock.ForegroundProperty.Changed,
                TextAlignmentProperty.Changed, TextWrappingProperty.Changed,
                TextBlock.FontSizeProperty.Changed, TextBlock.FontStyleProperty.Changed, 
                TextBlock.FontWeightProperty.Changed, TextBlock.FontFamilyProperty.Changed,
                SelectionStartProperty.Changed, SelectionEndProperty.Changed,
                SelectionForegroundBrushProperty.Changed, PasswordCharProperty.Changed, RevealPasswordProperty.Changed
            ).AddClassHandler<TextPresenter>((x, _) => x.InvalidateFormattedText());

            CaretIndexProperty.Changed.AddClassHandler<TextPresenter>((x, e) => x.CaretIndexChanged((int)e.NewValue));
        }

        public TextPresenter()
        {
            _text = string.Empty;
            _caretTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
            _caretTimer.Tick += CaretTimerTick;
        }

        /// <summary>
        /// Gets or sets a brush used to paint the control's background.
        /// </summary>
        public IBrush Background
        {
            get => GetValue(BackgroundProperty);
            set => SetValue(BackgroundProperty, value);
        }

        /// <summary>
        /// Gets or sets the text.
        /// </summary>
        [Content]
        public string Text
        {
            get => _text;
            set => SetAndRaise(TextProperty, ref _text, value);
        }

        /// <summary>
        /// Gets or sets the font family.
        /// </summary>
        public FontFamily FontFamily
        {
            get => TextBlock.GetFontFamily(this);
            set => TextBlock.SetFontFamily(this, value);
        }

        /// <summary>
        /// Gets or sets the font size.
        /// </summary>
        public double FontSize
        {
            get => TextBlock.GetFontSize(this);
            set => TextBlock.SetFontSize(this, value);
        }

        /// <summary>
        /// Gets or sets the font style.
        /// </summary>
        public FontStyle FontStyle
        {
            get => TextBlock.GetFontStyle(this);
            set => TextBlock.SetFontStyle(this, value);
        }

        /// <summary>
        /// Gets or sets the font weight.
        /// </summary>
        public FontWeight FontWeight
        {
            get => TextBlock.GetFontWeight(this);
            set => TextBlock.SetFontWeight(this, value);
        }

        /// <summary>
        /// Gets or sets a brush used to paint the text.
        /// </summary>
        public IBrush Foreground
        {
            get => TextBlock.GetForeground(this);
            set => TextBlock.SetForeground(this, value);
        }

        /// <summary>
        /// Gets or sets the control's text wrapping mode.
        /// </summary>
        public TextWrapping TextWrapping
        {
            get => GetValue(TextWrappingProperty);
            set => SetValue(TextWrappingProperty, value);
        }

        /// <summary>
        /// Gets or sets the text alignment.
        /// </summary>
        public TextAlignment TextAlignment
        {
            get => GetValue(TextAlignmentProperty);
            set => SetValue(TextAlignmentProperty, value);
        }

        /// <summary>
        /// Gets the <see cref="FormattedText"/> used to render the text.
        /// </summary>
        public FormattedText FormattedText
        {
            get
            {
                return _formattedText ?? (_formattedText = CreateFormattedText());
            }
        }

        public int CaretIndex
        {
            get
            {
                return _caretIndex;
            }

            set
            {
                value = CoerceCaretIndex(value);
                SetAndRaise(CaretIndexProperty, ref _caretIndex, value);
            }
        }

        public char PasswordChar
        {
            get => GetValue(PasswordCharProperty);
            set => SetValue(PasswordCharProperty, value);
        }

        public bool RevealPassword
        {
            get => GetValue(RevealPasswordProperty);
            set => SetValue(RevealPasswordProperty, value);
        }

        public IBrush SelectionBrush
        {
            get => GetValue(SelectionBrushProperty);
            set => SetValue(SelectionBrushProperty, value);
        }

        public IBrush SelectionForegroundBrush
        {
            get => GetValue(SelectionForegroundBrushProperty);
            set => SetValue(SelectionForegroundBrushProperty, value);
        }

        public IBrush CaretBrush
        {
            get => GetValue(CaretBrushProperty);
            set => SetValue(CaretBrushProperty, value);
        }

        public int SelectionStart
        {
            get
            {
                return _selectionStart;
            }

            set
            {
                value = CoerceCaretIndex(value);
                SetAndRaise(SelectionStartProperty, ref _selectionStart, value);
            }
        }

        public int SelectionEnd
        {
            get
            {
                return _selectionEnd;
            }

            set
            {
                value = CoerceCaretIndex(value);
                SetAndRaise(SelectionEndProperty, ref _selectionEnd, value);
            }
        }

        public int GetCaretIndex(Point point)
        {
            var hit = FormattedText.HitTestPoint(point);
            return hit.TextPosition + (hit.IsTrailing ? 1 : 0);
        }

        /// <summary>
        /// Creates the <see cref="FormattedText"/> used to render the text.
        /// </summary>
        /// <param name="constraint">The constraint of the text.</param>
        /// <param name="text">The text to format.</param>
        /// <returns>A <see cref="FormattedText"/> object.</returns>
        private FormattedText CreateFormattedTextInternal(Size constraint, string text)
        {
            return new FormattedText
            {
                Constraint = constraint,
                Typeface = new Typeface(FontFamily, FontStyle, FontWeight),
                FontSize = FontSize,
                Text = text ?? string.Empty,
                TextAlignment = TextAlignment,
                TextWrapping = TextWrapping,
            };
        }

        /// <summary>
        /// Invalidates <see cref="FormattedText"/>.
        /// </summary>
        protected void InvalidateFormattedText()
        {
            _formattedText = null;
        }

        /// <summary>
        /// Renders the <see cref="TextPresenter"/> to a drawing context.
        /// </summary>
        /// <param name="context">The drawing context.</param>
        private void RenderInternal(DrawingContext context)
        {
            var background = Background;

            if (background != null)
            {
                context.FillRectangle(background, new Rect(Bounds.Size));
            }

            double top = 0;
            var textSize = FormattedText.Bounds.Size;

            if (Bounds.Height < textSize.Height)
            {
                switch (VerticalAlignment)
                {
                    case VerticalAlignment.Center:
                        top += (Bounds.Height - textSize.Height) / 2;
                        break;

                    case VerticalAlignment.Bottom:
                        top += (Bounds.Height - textSize.Height);
                        break;
                }
            }

            context.DrawText(Foreground, new Point(0, top), FormattedText);
        }

        public override void Render(DrawingContext context)
        {
            FormattedText.Constraint = Bounds.Size;

            _constraint = Bounds.Size;

            var selectionStart = SelectionStart;
            var selectionEnd = SelectionEnd;

            if (selectionStart != selectionEnd)
            {
                var start = Math.Min(selectionStart, selectionEnd);
                var length = Math.Max(selectionStart, selectionEnd) - start;

                var rects = FormattedText.HitTestTextRange(start, length);

                foreach (var rect in rects)
                {
                    context.FillRectangle(SelectionBrush, rect);
                }
            }

            RenderInternal(context);

            if (selectionStart == selectionEnd && _caretBlink)
            {
                var caretBrush = CaretBrush?.ToImmutable();

                if (caretBrush is null)
                {
                    var backgroundColor = (Background as ISolidColorBrush)?.Color;
                    if (backgroundColor.HasValue)
                    {
                        byte red = (byte)~(backgroundColor.Value.R);
                        byte green = (byte)~(backgroundColor.Value.G);
                        byte blue = (byte)~(backgroundColor.Value.B);

                        caretBrush = new ImmutableSolidColorBrush(Color.FromRgb(red, green, blue));
                    }
                    else
                    {
                        caretBrush = Brushes.Black;
                    }
                }

                var (p1, p2) = GetCaretPoints();
                context.DrawLine(
                    new ImmutablePen(caretBrush, 1),
                    p1, p2);
            }
        }

        (Point, Point) GetCaretPoints()
        {
            var charPos = FormattedText.HitTestTextPosition(CaretIndex);
            var x = Math.Floor(charPos.X) + 0.5;
            var y = Math.Floor(charPos.Y) + 0.5;
            var b = Math.Ceiling(charPos.Bottom) - 0.5;
            return (new Point(x, y), new Point(x, b));
        }

        public void ShowCaret()
        {
            _caretBlink = true;
            _caretTimer.Start();
            InvalidateVisual();
        }

        public void HideCaret()
        {
            _caretBlink = false;
            _caretTimer.Stop();
            InvalidateVisual();
        }

        internal void CaretIndexChanged(int caretIndex)
        {
            if (this.GetVisualParent() != null)
            {
                if (_caretTimer.IsEnabled)
                {
                    _caretBlink = true;
                    _caretTimer.Stop();
                    _caretTimer.Start();
                    InvalidateVisual();
                }
                else
                {
                    _caretTimer.Start();
                    InvalidateVisual();
                    _caretTimer.Stop();
                }

                if (IsMeasureValid)
                {
                    var rect = FormattedText.HitTestTextPosition(caretIndex);
                    this.BringIntoView(rect);
                }
                else
                {
                    // The measure is currently invalid so there's no point trying to bring the 
                    // current char into view until a measure has been carried out as the scroll
                    // viewer extents may not be up-to-date.
                    Dispatcher.UIThread.Post(
                        () =>
                        {
                            var rect = FormattedText.HitTestTextPosition(caretIndex);
                            this.BringIntoView(rect);
                        },
                        DispatcherPriority.Render);
                }
            }
        }

        /// <summary>
        /// Creates the <see cref="FormattedText"/> used to render the text.
        /// </summary>
        /// <returns>A <see cref="FormattedText"/> object.</returns>
        protected virtual FormattedText CreateFormattedText()
        {
            FormattedText result = null;

            var text = Text;

            if (PasswordChar != default(char) && !RevealPassword)
            {
                result = CreateFormattedTextInternal(_constraint, new string(PasswordChar, text?.Length ?? 0));
            }
            else
            {
                result = CreateFormattedTextInternal(_constraint, text);
            }

            var selectionStart = SelectionStart;
            var selectionEnd = SelectionEnd;
            var start = Math.Min(selectionStart, selectionEnd);
            var length = Math.Max(selectionStart, selectionEnd) - start;

            if (length > 0)
            {
                result.Spans = new[]
                {
                    new FormattedTextStyleSpan(start, length, SelectionForegroundBrush),
                };
            }

            return result;
        }

        /// <summary>
        /// Measures the control.
        /// </summary>
        /// <param name="availableSize">The available size for the control.</param>
        /// <returns>The desired size.</returns>
        private Size MeasureInternal(Size availableSize)
        {
            if (!string.IsNullOrEmpty(Text))
            {
                if (TextWrapping == TextWrapping.Wrap)
                {
                    _constraint = new Size(availableSize.Width, double.PositiveInfinity);
                }
                else
                {
                    _constraint = Size.Infinity;
                }

                _formattedText = null;

                return FormattedText.Bounds.Size;
            }

            return new Size();
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            var text = Text;

            if (!string.IsNullOrEmpty(text))
            {
                return MeasureInternal(availableSize);
            }
            else
            {
                return new FormattedText
                {
                    Text = "X",
                    Typeface = new Typeface(FontFamily, FontStyle, FontWeight),
                    FontSize = FontSize,
                    TextAlignment = TextAlignment,
                    Constraint = availableSize,
                }.Bounds.Size;
            }
        }

        private int CoerceCaretIndex(int value)
        {
            var text = Text;
            var length = text?.Length ?? 0;
            return Math.Max(0, Math.Min(length, value));
        }

        private void CaretTimerTick(object sender, EventArgs e)
        {
            _caretBlink = !_caretBlink;
            InvalidateVisual();
        }

        internal Rect GetCursorRectangle()
        {
            var (p1, p2) = GetCaretPoints();
            return new Rect(p1, p2);
        }
    }
}
