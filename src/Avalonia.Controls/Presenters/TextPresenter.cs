using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;
using Avalonia.Metadata;
using Avalonia.Threading;
using Avalonia.Utilities;
using Avalonia.VisualTree;
using Avalonia.Layout;

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
        private TextLayout _textLayout;
        private Size _constraint;

        private Rect _caretBounds;
        private Point _navigationPosition;

        static TextPresenter()
        {
            AffectsRender<TextPresenter>(SelectionBrushProperty, TextBlock.ForegroundProperty, 
                                         SelectionForegroundBrushProperty, CaretBrushProperty,
                                         SelectionStartProperty, SelectionEndProperty);
            
            AffectsMeasure<TextPresenter>(TextProperty, PasswordCharProperty, RevealPasswordProperty, 
                TextAlignmentProperty, TextWrappingProperty, TextBlock.FontSizeProperty,
                TextBlock.FontStyleProperty, TextBlock.FontWeightProperty, TextBlock.FontFamilyProperty);

            Observable.Merge<AvaloniaPropertyChangedEventArgs>(TextProperty.Changed,
                TextBlock.ForegroundProperty.Changed,
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
        /// Gets the <see cref="TextLayout"/> used to render the text.
        /// </summary>
        public TextLayout TextLayout
        {
            get
            {
                return _textLayout ?? (_textLayout = CreateTextLayout());
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
                _caretBounds = TextLayout.GetRectFromCharacterIndex(value, true);
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
            var hit = TextLayout.HitTestPoint(point);
            _caretBounds = hit.CaretBounds;
            _navigationPosition = _caretBounds.Position;
            return hit.TextPosition;
        }

        /// <summary>
        /// Creates the <see cref="TextLayout"/> used to render the text.
        /// </summary>
        /// <param name="constraint">The constraint of the text.</param>
        /// <param name="text">The text to format.</param>
        /// <param name="typeface"></param>
        /// <param name="textStyleOverrides"></param>
        /// <returns>A <see cref="TextLayout"/> object.</returns>
        private TextLayout CreateTextLayoutInternal(Size constraint, string text, Typeface typeface,
            IReadOnlyList<ValueSpan<TextRunProperties>> textStyleOverrides)
        {
            var textLayout = new TextLayout(text ?? string.Empty, typeface, FontSize, Foreground, TextAlignment,
                TextWrapping, maxWidth: constraint.Width, maxHeight: constraint.Height,
                textStyleOverrides: textStyleOverrides);

            return textLayout;
        }

        /// <summary>
        /// Invalidates <see cref="TextLayout"/>.
        /// </summary>
        protected void InvalidateFormattedText()
        {
            _textLayout = null;
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

            var top = 0d;
            var left = 0.0;

            var (_, textHeight) = TextLayout.Size;

            if (Bounds.Height < textHeight)
            {
                switch (VerticalAlignment)
                {
                    case VerticalAlignment.Center:
                        top += (Bounds.Height - textHeight) / 2;
                        break;

                    case VerticalAlignment.Bottom:
                        top += (Bounds.Height - textHeight);
                        break;
                }
            }

            TextLayout.Draw(context, new Point(left, top));
        }

        public override void Render(DrawingContext context)
        {
            if (_constraint != Bounds.Size)
            {
                _constraint = Bounds.Size;

                InvalidateFormattedText();
            }

            var selectionStart = SelectionStart;
            var selectionEnd = SelectionEnd;

            if (selectionStart != selectionEnd)
            {
                var start = Math.Min(selectionStart, selectionEnd);
                var length = Math.Max(selectionStart, selectionEnd) - start;

                var rects = TextLayout.HitTestTextRange(start, length);

                foreach (var rect in rects)
                {
                    context.FillRectangle(SelectionBrush, rect);
                }
            }

            RenderInternal(context);

            if (selectionStart == selectionEnd)
            {
                var caretBrush = CaretBrush;

                if (caretBrush is null)
                {
                    var backgroundColor = (Background as SolidColorBrush)?.Color;

                    if (backgroundColor.HasValue)
                    {
                        byte red = (byte)~(backgroundColor.Value.R);
                        byte green = (byte)~(backgroundColor.Value.G);
                        byte blue = (byte)~(backgroundColor.Value.B);

                        caretBrush = new SolidColorBrush(Color.FromRgb(red, green, blue));
                    }
                    else
                        caretBrush = Brushes.Black;
                }

                if (_caretBlink)
                {
                    var x = Math.Floor(_caretBounds.X) + 0.5;
                    var y = Math.Floor(_caretBounds.Y) + 0.5;
                    var b = Math.Ceiling(_caretBounds.Bottom) - 0.5;

                    context.DrawLine(
                        new Pen(caretBrush, 1),
                        new Point(x, y),
                        new Point(x, b));
                }
            }
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
                    //var rect = TextLayout.HitTestTextPosition(caretIndex);
                    //_caretPosition = rect;
                    this.BringIntoView(_caretBounds);
                }
                else
                {
                    // The measure is currently invalid so there's no point trying to bring the 
                    // current char into view until a measure has been carried out as the scroll
                    // viewer extents may not be up-to-date.
                    Dispatcher.UIThread.Post(
                        () =>
                        {
                            //var rect = TextLayout.HitTestTextPosition(caretIndex);
                            this.BringIntoView(_caretBounds);
                        },
                        DispatcherPriority.Render);
                }
            }
        }

        /// <summary>
        /// Creates the <see cref="TextLayout"/> used to render the text.
        /// </summary>
        /// <returns>A <see cref="TextLayout"/> object.</returns>
        protected virtual TextLayout CreateTextLayout()
        {
            TextLayout result;

            var text = Text;

            var typeface = new Typeface(FontFamily, FontStyle, FontWeight);

            var selectionStart = SelectionStart;
            var selectionEnd = SelectionEnd;
            var start = Math.Min(selectionStart, selectionEnd);
            var length = Math.Max(selectionStart, selectionEnd) - start;

            IReadOnlyList<ValueSpan<TextRunProperties>> textStyleOverrides = null;

            if (length > 0)
            {
                textStyleOverrides = new[]
                {
                    new ValueSpan<TextRunProperties>(start, length,
                        new GenericTextRunProperties(typeface, FontSize,
                            foregroundBrush: SelectionForegroundBrush ?? Brushes.White))
                };
            }

            if (PasswordChar != default(char) && !RevealPassword)
            {
                result = CreateTextLayoutInternal(_constraint, new string(PasswordChar, text?.Length ?? 0), typeface,
                    textStyleOverrides);
            }
            else
            {
                result = CreateTextLayoutInternal(_constraint, text, typeface, textStyleOverrides);
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
            if (string.IsNullOrEmpty(Text))
            {
                return new Size();
            }

            _constraint = TextWrapping == TextWrapping.Wrap ?
                new Size(availableSize.Width, double.PositiveInfinity) :
                Size.Infinity;

            _textLayout = null;

            return TextLayout.Size;
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            var text = Text;

            if (!string.IsNullOrEmpty(text))
            {
                return MeasureInternal(availableSize);
            }

            var typeface = new Typeface(FontFamily, FontStyle, FontWeight);

            var textLayout = new TextLayout("X", typeface, FontSize, null, TextAlignment,
                maxWidth: availableSize.Width, maxHeight: availableSize.Height);

            return textLayout.Size;
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

        public void MoveCaretVertical(LogicalDirection direction = LogicalDirection.Forward)
        {
            var lineIndex = TextLayout.GetLineIndexFromCharacterIndex(CaretIndex);

            if (lineIndex < 0)
            {
                return;
            }

            var currentX = _navigationPosition.X;

            var currentY = _navigationPosition.Y;

            if (direction == LogicalDirection.Forward)
            {
                if (lineIndex + 1 > TextLayout.TextLines.Count - 1)
                {
                    return;
                }

                var textLine = TextLayout.TextLines[lineIndex];
                
                currentY += textLine.Height;
            }
            else
            {
                if (lineIndex - 1 < 0)
                {
                    return;
                }

                var textLine = TextLayout.TextLines[--lineIndex];

                currentY -= textLine.Height;
            }

            var caretIndex = TextLayout.GetCharacterIndexFromPoint(new Point(currentX, currentY), true);

            CaretIndex = caretIndex;
            
            _navigationPosition = _navigationPosition.WithY(_caretBounds.Y);
        }

        public void MoveCaretHorizontal(LogicalDirection direction = LogicalDirection.Forward)
        {
            var caretIndex = CaretIndex;
            
            var lineIndex = TextLayout.GetLineIndexFromCharacterIndex(caretIndex);

            if (lineIndex < 0)
            {
                return;
            }

            if (direction == LogicalDirection.Forward)
            {
                while (lineIndex < TextLayout.TextLines.Count)
                {
                    var textLine = TextLayout.TextLines[lineIndex];

                    var characterHit = textLine.GetNextCaretCharacterHit(new CharacterHit(caretIndex));

                    caretIndex = characterHit.FirstCharacterIndex + characterHit.TrailingLength;

                    if (caretIndex <= CaretIndex)
                    {
                        lineIndex++;
                        
                        continue;
                    }
                    
                    break;
                }
            }
            else
            {
                while (lineIndex >= 0)
                {
                    var textLine = TextLayout.TextLines[lineIndex];

                    var characterHit = textLine.GetPreviousCaretCharacterHit(new CharacterHit(caretIndex));

                    caretIndex = characterHit.FirstCharacterIndex + characterHit.TrailingLength;

                    if (caretIndex >= CaretIndex)
                    {
                        lineIndex--;

                        continue;
                    }

                    break;
                }
            }

            CaretIndex = caretIndex;
            
            _navigationPosition = _caretBounds.Position;
        }

        internal Rect GetCursorRectangle()
        {
            return _caretBounds;
        }
    }
}
