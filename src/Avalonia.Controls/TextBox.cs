using Avalonia.Input.Platform;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Utils;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Metadata;
using Avalonia.Data;
using Avalonia.Layout;
using Avalonia.Utilities;
using Avalonia.Controls.Metadata;
using Avalonia.Media.TextFormatting;

namespace Avalonia.Controls
{
    [PseudoClasses(":empty")]
    public class TextBox : TemplatedControl, UndoRedoHelper<TextBox.UndoRedoState>.IUndoRedoHost
    {
        public static KeyGesture CutGesture { get; } = AvaloniaLocator.Current
            .GetService<PlatformHotkeyConfiguration>()?.Cut.FirstOrDefault();

        public static KeyGesture CopyGesture { get; } = AvaloniaLocator.Current
            .GetService<PlatformHotkeyConfiguration>()?.Copy.FirstOrDefault();

        public static KeyGesture PasteGesture { get; } = AvaloniaLocator.Current
            .GetService<PlatformHotkeyConfiguration>()?.Paste.FirstOrDefault();
        
        public static readonly StyledProperty<bool> AcceptsReturnProperty =
            AvaloniaProperty.Register<TextBox, bool>(nameof(AcceptsReturn));

        public static readonly StyledProperty<bool> AcceptsTabProperty =
            AvaloniaProperty.Register<TextBox, bool>(nameof(AcceptsTab));

        public static readonly DirectProperty<TextBox, int> CaretIndexProperty =
            AvaloniaProperty.RegisterDirect<TextBox, int>(
                nameof(CaretIndex),
                o => o.CaretIndex,
                (o, v) => o.CaretIndex = v);

        public static readonly StyledProperty<bool> IsReadOnlyProperty =
            AvaloniaProperty.Register<TextBox, bool>(nameof(IsReadOnly));

        public static readonly StyledProperty<char> PasswordCharProperty =
            AvaloniaProperty.Register<TextBox, char>(nameof(PasswordChar));

        public static readonly StyledProperty<IBrush> SelectionBrushProperty =
            AvaloniaProperty.Register<TextBox, IBrush>(nameof(SelectionBrushProperty));

        public static readonly StyledProperty<IBrush> SelectionForegroundBrushProperty =
            AvaloniaProperty.Register<TextBox, IBrush>(nameof(SelectionForegroundBrushProperty));

        public static readonly StyledProperty<IBrush> CaretBrushProperty =
            AvaloniaProperty.Register<TextBox, IBrush>(nameof(CaretBrushProperty));

        public static readonly DirectProperty<TextBox, int> SelectionStartProperty =
            AvaloniaProperty.RegisterDirect<TextBox, int>(
                nameof(SelectionStart),
                o => o.SelectionStart,
                (o, v) => o.SelectionStart = v);

        public static readonly DirectProperty<TextBox, int> SelectionEndProperty =
            AvaloniaProperty.RegisterDirect<TextBox, int>(
                nameof(SelectionEnd),
                o => o.SelectionEnd,
                (o, v) => o.SelectionEnd = v);

        public static readonly StyledProperty<int> MaxLengthProperty =
            AvaloniaProperty.Register<TextBox, int>(nameof(MaxLength), defaultValue: 0);

        public static readonly DirectProperty<TextBox, string> TextProperty =
            TextBlock.TextProperty.AddOwnerWithDataValidation<TextBox>(
                o => o.Text,
                (o, v) => o.Text = v,
                defaultBindingMode: BindingMode.TwoWay,
                enableDataValidation: true);

        public static readonly StyledProperty<TextAlignment> TextAlignmentProperty =
            TextBlock.TextAlignmentProperty.AddOwner<TextBox>();

        /// <summary>
        /// Defines the <see cref="HorizontalAlignment"/> property.
        /// </summary>
        public static readonly StyledProperty<HorizontalAlignment> HorizontalContentAlignmentProperty =
            ContentControl.HorizontalContentAlignmentProperty.AddOwner<TextBox>();

        /// <summary>
        /// Defines the <see cref="VerticalAlignment"/> property.
        /// </summary>
        public static readonly StyledProperty<VerticalAlignment> VerticalContentAlignmentProperty =
            ContentControl.VerticalContentAlignmentProperty.AddOwner<TextBox>();

        public static readonly StyledProperty<TextWrapping> TextWrappingProperty =
            TextBlock.TextWrappingProperty.AddOwner<TextBox>();

        public static readonly StyledProperty<string> WatermarkProperty =
            AvaloniaProperty.Register<TextBox, string>(nameof(Watermark));

        public static readonly StyledProperty<bool> UseFloatingWatermarkProperty =
            AvaloniaProperty.Register<TextBox, bool>(nameof(UseFloatingWatermark));

        public static readonly DirectProperty<TextBox, string> NewLineProperty =
            AvaloniaProperty.RegisterDirect<TextBox, string>(nameof(NewLine),
                textbox => textbox.NewLine, (textbox, newline) => textbox.NewLine = newline);

        public static readonly StyledProperty<object> InnerLeftContentProperty =
            AvaloniaProperty.Register<TextBox, object>(nameof(InnerLeftContent));

        public static readonly StyledProperty<object> InnerRightContentProperty =
            AvaloniaProperty.Register<TextBox, object>(nameof(InnerRightContent));

        public static readonly StyledProperty<bool> RevealPasswordProperty =
            AvaloniaProperty.Register<TextBox, bool>(nameof(RevealPassword));
        
        public static readonly DirectProperty<TextBox, bool> CanCutProperty =
                    AvaloniaProperty.RegisterDirect<TextBox, bool>(
                        nameof(CanCut),
                        o => o.CanCut);

        public static readonly DirectProperty<TextBox, bool> CanCopyProperty =
            AvaloniaProperty.RegisterDirect<TextBox, bool>(
                nameof(CanCopy),
                o => o.CanCopy);

        public static readonly DirectProperty<TextBox, bool> CanPasteProperty =
                    AvaloniaProperty.RegisterDirect<TextBox, bool>(
                        nameof(CanPaste),
                        o => o.CanPaste);

        struct UndoRedoState : IEquatable<UndoRedoState>
        {
            public string Text { get; }
            public int CaretPosition { get; }

            public UndoRedoState(string text, int caretPosition)
            {
                Text = text;
                CaretPosition = caretPosition;
            }

            public bool Equals(UndoRedoState other) => ReferenceEquals(Text, other.Text) || Equals(Text, other.Text);
        }

        private string _text;
        private int _caretIndex;
        private int _selectionStart;
        private int _selectionEnd;
        private TextPresenter _presenter;
        private TextBoxTextInputMethodClient _imClient = new TextBoxTextInputMethodClient();
        private UndoRedoHelper<UndoRedoState> _undoRedoHelper;
        private bool _isUndoingRedoing;
        private bool _ignoreTextChanges;
        private bool _canCut;
        private bool _canCopy;
        private bool _canPaste;
        private string _newLine = Environment.NewLine;
        private static readonly string[] invalidCharacters = new String[1] { "\u007f" };

        static TextBox()
        {
            FocusableProperty.OverrideDefaultValue(typeof(TextBox), true);
            TextInputMethodClientRequestedEvent.AddClassHandler<TextBox>((tb, e) =>
            {
                e.Client = tb._imClient;
            });
        }

        public TextBox()
        {
            var horizontalScrollBarVisibility = Observable.CombineLatest(
                this.GetObservable(AcceptsReturnProperty),
                this.GetObservable(TextWrappingProperty),
                (acceptsReturn, wrapping) =>
                {
                    if (wrapping != TextWrapping.NoWrap)
                    {
                        return ScrollBarVisibility.Disabled;
                    }

                    return acceptsReturn ? ScrollBarVisibility.Auto : ScrollBarVisibility.Hidden;
                });
            this.Bind(
                ScrollViewer.HorizontalScrollBarVisibilityProperty,
                horizontalScrollBarVisibility,
                BindingPriority.Style);
            _undoRedoHelper = new UndoRedoHelper<UndoRedoState>(this);

            UpdatePseudoclasses();
        }

        public bool AcceptsReturn
        {
            get { return GetValue(AcceptsReturnProperty); }
            set { SetValue(AcceptsReturnProperty, value); }
        }

        public bool AcceptsTab
        {
            get { return GetValue(AcceptsTabProperty); }
            set { SetValue(AcceptsTabProperty, value); }
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
                UndoRedoState state;
                if (_undoRedoHelper.TryGetLastState(out state) && state.Text == Text)
                    _undoRedoHelper.UpdateLastState();
            }
        }

        public bool IsReadOnly
        {
            get { return GetValue(IsReadOnlyProperty); }
            set { SetValue(IsReadOnlyProperty, value); }
        }

        public char PasswordChar
        {
            get => GetValue(PasswordCharProperty);
            set => SetValue(PasswordCharProperty, value);
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
                if (SelectionStart == SelectionEnd)
                {
                    CaretIndex = SelectionStart;
                }
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
                if (SelectionStart == SelectionEnd)
                {
                    CaretIndex = SelectionEnd;
                }
            }
        }

        public int MaxLength
        {
            get { return GetValue(MaxLengthProperty); }
            set { SetValue(MaxLengthProperty, value); }
        }

        [Content]
        public string Text
        {
            get { return _text; }
            set
            {
                if (!_ignoreTextChanges)
                {
                    var caretIndex = CaretIndex;
                    SelectionStart = CoerceCaretIndex(SelectionStart, value);
                    SelectionEnd = CoerceCaretIndex(SelectionEnd, value);
                    CaretIndex = CoerceCaretIndex(caretIndex, value);

                    if (SetAndRaise(TextProperty, ref _text, value) && !_isUndoingRedoing)
                    {
                        _undoRedoHelper.Clear();
                    }
                }
            }
        }

        public string SelectedText
        {
            get { return GetSelection(); }
            set
            {
                _undoRedoHelper.Snapshot();
                if (string.IsNullOrEmpty(value))
                {
                    DeleteSelection();
                }
                else
                {
                    HandleTextInput(value);
                }
                _undoRedoHelper.Snapshot();
            }
        }

        /// <summary>
        /// Gets or sets the horizontal alignment of the content within the control.
        /// </summary>
        public HorizontalAlignment HorizontalContentAlignment
        {
            get { return GetValue(HorizontalContentAlignmentProperty); }
            set { SetValue(HorizontalContentAlignmentProperty, value); }
        }

        /// <summary>
        /// Gets or sets the vertical alignment of the content within the control.
        /// </summary>
        public VerticalAlignment VerticalContentAlignment
        {
            get { return GetValue(VerticalContentAlignmentProperty); }
            set { SetValue(VerticalContentAlignmentProperty, value); }
        }

        public TextAlignment TextAlignment
        {
            get { return GetValue(TextAlignmentProperty); }
            set { SetValue(TextAlignmentProperty, value); }
        }

        public string Watermark
        {
            get { return GetValue(WatermarkProperty); }
            set { SetValue(WatermarkProperty, value); }
        }

        public bool UseFloatingWatermark
        {
            get { return GetValue(UseFloatingWatermarkProperty); }
            set { SetValue(UseFloatingWatermarkProperty, value); }
        }

        public object InnerLeftContent
        {
            get { return GetValue(InnerLeftContentProperty); }
            set { SetValue(InnerLeftContentProperty, value); }
        }

        public object InnerRightContent
        {
            get { return GetValue(InnerRightContentProperty); }
            set { SetValue(InnerRightContentProperty, value); }
        }

        public bool RevealPassword
        {
            get { return GetValue(RevealPasswordProperty); }
            set { SetValue(RevealPasswordProperty, value); }
        }

        public TextWrapping TextWrapping
        {
            get { return GetValue(TextWrappingProperty); }
            set { SetValue(TextWrappingProperty, value); }
        }

        /// <summary>
        /// Gets or sets which characters are inserted when Enter is pressed. Default: <see cref="Environment.NewLine"/>
        /// </summary>
        public string NewLine
        {
            get { return _newLine; }
            set { SetAndRaise(NewLineProperty, ref _newLine, value); }
        }
        
        /// <summary>
        /// Clears the current selection, maintaining the <see cref="CaretIndex"/>
        /// </summary>
        public void ClearSelection()
        {
            SelectionStart = SelectionEnd = CaretIndex;
        }

        /// <summary>
        /// Property for determining if the Cut command can be executed.
        /// </summary>
        public bool CanCut
        {
            get { return _canCut; }
            private set { SetAndRaise(CanCutProperty, ref _canCut, value); }
        }

        /// <summary>
        /// Property for determining if the Copy command can be executed.
        /// </summary>
        public bool CanCopy
        {
            get { return _canCopy; }
            private set { SetAndRaise(CanCopyProperty, ref _canCopy, value); }
        }

        /// <summary>
        /// Property for determining if the Paste command can be executed.
        /// </summary>
        public bool CanPaste
        {
            get { return _canPaste; }
            private set { SetAndRaise(CanPasteProperty, ref _canPaste, value); }
        }

        protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
        {
            _presenter = e.NameScope.Get<TextPresenter>("PART_TextPresenter");
            _imClient.SetPresenter(_presenter);
            if (IsFocused)
            {
                _presenter?.ShowCaret();
            }
        }

        protected override void OnPropertyChanged<T>(AvaloniaPropertyChangedEventArgs<T> change)
        {
            base.OnPropertyChanged(change);

            if (change.Property == TextProperty)
            {
                UpdatePseudoclasses();
                UpdateCommandStates();
            }
        }

        private void UpdateCommandStates()
        {
            var text = GetSelection();
            var isSelectionNullOrEmpty = string.IsNullOrEmpty(text);
            CanCopy = !IsPasswordBox && !isSelectionNullOrEmpty;
            CanCut = !IsPasswordBox && !isSelectionNullOrEmpty && !IsReadOnly;
            CanPaste = !IsReadOnly;
        }

        protected override void OnGotFocus(GotFocusEventArgs e)
        {
            base.OnGotFocus(e);

            // when navigating to a textbox via the tab key, select all text if
            //   1) this textbox is *not* a multiline textbox
            //   2) this textbox has any text to select
            if (e.NavigationMethod == NavigationMethod.Tab &&
                !AcceptsReturn &&
                Text?.Length > 0)
            {
                SelectAll();
            }

            UpdateCommandStates();

            _presenter?.ShowCaret();
        }

        protected override void OnLostFocus(RoutedEventArgs e)
        {
            base.OnLostFocus(e);

            if ((ContextFlyout == null || !ContextFlyout.IsOpen) &&
                (ContextMenu == null || !ContextMenu.IsOpen))
            {
                ClearSelection();
                RevealPassword = false;
            }

            UpdateCommandStates();

            _presenter?.HideCaret();
        }

        protected override void OnTextInput(TextInputEventArgs e)
        {
            if (!e.Handled)
            {
                HandleTextInput(e.Text);
                e.Handled = true;
            }
        }

        private void HandleTextInput(string input)
        {
            if (IsReadOnly)
            {
                return;
            }
            
            input = RemoveInvalidCharacters(input);
            
            if (string.IsNullOrEmpty(input))
            {
                return;
            }
            
            string text = Text ?? string.Empty;
            int caretIndex = CaretIndex;
            int newLength = input.Length + text.Length - Math.Abs(SelectionStart - SelectionEnd);
            
            if (MaxLength > 0 && newLength > MaxLength)
            {
                input = input.Remove(Math.Max(0, input.Length - (newLength - MaxLength)));
            }
            
            if (!string.IsNullOrEmpty(input))
            {
                DeleteSelection();
                caretIndex = CaretIndex;
                text = Text ?? string.Empty;
                SetTextInternal(text.Substring(0, caretIndex) + input + text.Substring(caretIndex));
                MoveHorizontal(1,false,false);
                ClearSelection();
                _undoRedoHelper.DiscardRedo();
            }
        }

        public string RemoveInvalidCharacters(string text)
        {
            for (var i = 0; i < invalidCharacters.Length; i++)
            {
                text = text.Replace(invalidCharacters[i], string.Empty);
            }

            return text;
        }

        public async void Cut()
        {
            var text = GetSelection();
            if (text is null) return;

            _undoRedoHelper.Snapshot();
            Copy();
            DeleteSelection();
            _undoRedoHelper.Snapshot();
        }

        public async void Copy()
        {
            var text = GetSelection();
            if (text is null) return;

            await ((IClipboard)AvaloniaLocator.Current.GetService(typeof(IClipboard)))
                .SetTextAsync(text);
        }

        public async void Paste()
        {
            var text = await ((IClipboard)AvaloniaLocator.Current.GetService(typeof(IClipboard))).GetTextAsync();

            if (string.IsNullOrEmpty(text))
            {
                return;
            }

            _undoRedoHelper.Snapshot();
            HandleTextInput(text);
            _undoRedoHelper.Snapshot();
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            string text = Text ?? string.Empty;
            int caretIndex = CaretIndex;
            bool movement = false;
            bool selection = false;
            bool handled = false;
            var modifiers = e.KeyModifiers;

            var keymap = AvaloniaLocator.Current.GetService<PlatformHotkeyConfiguration>();

            bool Match(List<KeyGesture> gestures) => gestures.Any(g => g.Matches(e));
            bool DetectSelection() => e.KeyModifiers.HasAllFlags(keymap.SelectionModifiers);

            if (Match(keymap.SelectAll))
            {
                SelectAll();
                handled = true;
            }
            else if (Match(keymap.Copy))
            {
                if (!IsPasswordBox)
                {
                    Copy();
                }

                handled = true;
            }
            else if (Match(keymap.Cut))
            {
                if (!IsPasswordBox)
                {
                    Cut();
                }

                handled = true;
            }
            else if (Match(keymap.Paste))
            {
                Paste();
                handled = true;
            }
            else if (Match(keymap.Undo))
            {
                try
                {
                    _isUndoingRedoing = true;
                    _undoRedoHelper.Undo();
                }
                finally
                {
                    _isUndoingRedoing = false;
                }

                handled = true;
            }
            else if (Match(keymap.Redo))
            {
                try
                {
                    _isUndoingRedoing = true;
                    _undoRedoHelper.Redo();
                }
                finally
                {
                    _isUndoingRedoing = false;
                }

                handled = true;
            }
            else if (Match(keymap.MoveCursorToTheStartOfDocument))
            {
                MoveHome(true);
                movement = true;
                selection = false;
                handled = true;
            }
            else if (Match(keymap.MoveCursorToTheEndOfDocument))
            {
                MoveEnd(true);
                movement = true;
                selection = false;
                handled = true;
            }
            else if (Match(keymap.MoveCursorToTheStartOfLine))
            {
                MoveHome(false);
                movement = true;
                selection = false;
                handled = true;

            }
            else if (Match(keymap.MoveCursorToTheEndOfLine))
            {
                MoveEnd(false);
                movement = true;
                selection = false;
                handled = true;
            }
            else if (Match(keymap.MoveCursorToTheStartOfDocumentWithSelection))
            {
                MoveHome(true);
                movement = true;
                selection = true;
                handled = true;
            }
            else if (Match(keymap.MoveCursorToTheEndOfDocumentWithSelection))
            {
                MoveEnd(true);
                movement = true;
                selection = true;
                handled = true;
            }
            else if (Match(keymap.MoveCursorToTheStartOfLineWithSelection))
            {
                MoveHome(false);
                movement = true;
                selection = true;
                handled = true;

            }
            else if (Match(keymap.MoveCursorToTheEndOfLineWithSelection))
            {
                MoveEnd(false);
                movement = true;
                selection = true;
                handled = true;
            }
            else
            {
                var oldCaretIndex = _presenter.CaretIndex;
                bool hasWholeWordModifiers = modifiers.HasFlag(keymap.WholeWordTextActionModifiers);
                switch (e.Key)
                {
                    case Key.Left:
                        selection = DetectSelection();
                        MoveHorizontal(-1, hasWholeWordModifiers, selection);
                        movement = true;
                        break;

                    case Key.Right:
                        selection = DetectSelection();
                        MoveHorizontal(1, hasWholeWordModifiers, selection);
                        movement = true;
                        break;

                    case Key.Up:
                        _presenter.MoveCaretVertical(LogicalDirection.Backward);
                        if (oldCaretIndex != _presenter.CaretIndex)
                        {
                            movement = true;
                        }
                        selection = DetectSelection();
                        break;

                    case Key.Down:
                        _presenter.MoveCaretVertical(LogicalDirection.Forward);
                        if (oldCaretIndex != _presenter.CaretIndex)
                        {
                            movement = true;
                        }
                        selection = DetectSelection();
                        break;

                    case Key.Back:
                        _undoRedoHelper.Snapshot();
                        if (hasWholeWordModifiers && SelectionStart == SelectionEnd)
                        {
                            SetSelectionForControlBackspace();
                        }

                        if (!DeleteSelection() && CaretIndex > 0)
                        {
                            var removedCharacters = 1;
                            // handle deleting /r/n
                            // you don't ever want to leave a dangling /r around. So, if deleting /n, check to see if 
                            // a /r should also be deleted.
                            if (CaretIndex > 1 &&
                                text[CaretIndex - 1] == '\n' &&
                                text[CaretIndex - 2] == '\r')
                            {
                                removedCharacters = 2;
                            }

                            SetTextInternal(text.Substring(0, caretIndex - removedCharacters) +
                                            text.Substring(caretIndex));
                            CaretIndex -= removedCharacters;
                            ClearSelection();
                        }
                        _undoRedoHelper.Snapshot();

                        handled = true;
                        break;

                    case Key.Delete:
                        _undoRedoHelper.Snapshot();
                        if (hasWholeWordModifiers && SelectionStart == SelectionEnd)
                        {
                            SetSelectionForControlDelete();
                        }

                        if (!DeleteSelection() && caretIndex < text.Length)
                        {
                            var removedCharacters = 1;
                            // handle deleting /r/n
                            // you don't ever want to leave a dangling /r around. So, if deleting /n, check to see if 
                            // a /r should also be deleted.
                            if (CaretIndex < text.Length - 1 &&
                                text[caretIndex + 1] == '\n' &&
                                text[caretIndex] == '\r')
                            {
                                removedCharacters = 2;
                            }

                            SetTextInternal(text.Substring(0, caretIndex) +
                                            text.Substring(caretIndex + removedCharacters));
                        }
                        _undoRedoHelper.Snapshot();

                        handled = true;
                        break;

                    case Key.Enter:
                        if (AcceptsReturn)
                        {
                            _undoRedoHelper.Snapshot();
                            HandleTextInput(NewLine);
                            _undoRedoHelper.Snapshot();
                            handled = true;
                        }

                        break;

                    case Key.Tab:
                        if (AcceptsTab)
                        {
                            _undoRedoHelper.Snapshot();
                            HandleTextInput("\t");
                            _undoRedoHelper.Snapshot();
                            handled = true;
                        }
                        else
                        {
                            base.OnKeyDown(e);
                        }

                        break;

                    default:
                        handled = false;
                        break;
                }
            }

            if (movement && selection)
            {
                SelectionEnd = CaretIndex;
            }
            else if (movement)
            {
                ClearSelection();
            }

            if (handled || movement)
            {
                e.Handled = true;
            }
        }

        protected override void OnPointerPressed(PointerPressedEventArgs e)
        {
            var text = Text;

            var clickInfo = e.GetCurrentPoint(this);
            if (text != null && clickInfo.Properties.IsLeftButtonPressed && !(clickInfo.Pointer?.Captured is Border))
            {
                var point = e.GetPosition(_presenter);
                var index = CaretIndex = _presenter.GetCaretIndex(point);
                switch (e.ClickCount)
                {
                    case 1:
                        SelectionStart = SelectionEnd = index;
                        break;
                    case 2:
                        if (!StringUtils.IsStartOfWord(text, index))
                        {
                            SelectionStart = StringUtils.PreviousWord(text, index);
                        }

                        SelectionEnd = StringUtils.NextWord(text, index);
                        break;
                    case 3:
                        SelectAll();
                        break;
                }
            }

            e.Pointer.Capture(_presenter);
            e.Handled = true;
        }

        protected override void OnPointerMoved(PointerEventArgs e)
        {
            // selection should not change during pointer move if the user right clicks
            if (_presenter != null && e.Pointer.Captured == _presenter && e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            {
                var point = e.GetPosition(_presenter);

                point = new Point(
                    MathUtilities.Clamp(point.X, 0, Math.Max(_presenter.Bounds.Width - 1, 0)),
                    MathUtilities.Clamp(point.Y, 0, Math.Max(_presenter.Bounds.Height - 1, 0)));

                CaretIndex = SelectionEnd = _presenter.GetCaretIndex(point);
            }
        }

        protected override void OnPointerReleased(PointerReleasedEventArgs e)
        {
            if (_presenter != null && e.Pointer.Captured == _presenter)
            {
                if (e.InitialPressMouseButton == MouseButton.Right)
                {
                    var point = e.GetPosition(_presenter);
                    var caretIndex = _presenter.GetCaretIndex(point);

                    // see if mouse clicked inside current selection
                    // if it did not, we change the selection to where the user clicked
                    var firstSelection = Math.Min(SelectionStart, SelectionEnd);
                    var lastSelection = Math.Max(SelectionStart, SelectionEnd);
                    var didClickInSelection = SelectionStart != SelectionEnd &&
                        caretIndex >= firstSelection && caretIndex <= lastSelection;
                    if (!didClickInSelection)
                    {
                        CaretIndex = SelectionEnd = SelectionStart = caretIndex;
                    }
                }
                e.Pointer.Capture(null);
            }
        }

        protected override void UpdateDataValidation<T>(AvaloniaProperty<T> property, BindingValue<T> value)
        {
            if (property == TextProperty)
            {
                DataValidationErrors.SetError(this, value.Error);
            }
        }

        private int CoerceCaretIndex(int value) => CoerceCaretIndex(value, Text);

        private int CoerceCaretIndex(int value, string text)
        {
            if (text == null)
            {
                return 0;
            }
            var length = text.Length;

            if (value < 0)
            {
                return 0;
            }
            else if (value > length)
            {
                return length;
            }
            else if (value > 0 && text[value - 1] == '\r' && value < length && text[value] == '\n')
            {
                return value + 1;
            }
            else
            {
                return value;
            }
        }

        public void Clear()
        {
            Text = string.Empty;
        }

        private int DeleteCharacter(int index)
        {
            var start = index + 1;
            var text = Text;
            var c = text[index];
            var result = 1;

            if (c == '\n' && index > 0 && text[index - 1] == '\r')
            {
                --index;
                ++result;
            }
            else if (c == '\r' && index < text.Length - 1 && text[index + 1] == '\n')
            {
                ++start;
                ++result;
            }

            Text = text.Substring(0, index) + text.Substring(start);

            return result;
        }

        private void MoveHorizontal(int direction, bool wholeWord, bool isSelecting)
        {
            var text = Text ?? string.Empty;
            var caretIndex = CaretIndex;

            if (!wholeWord)
            {
                if (SelectionStart != SelectionEnd && !isSelecting)
                {
                    var start = Math.Min(SelectionStart, SelectionEnd);
                    var end = Math.Max(SelectionStart, SelectionEnd);
                    CaretIndex = direction < 0 ? start : end;
                    return;
                }

                _presenter.MoveCaretHorizontal(direction > 0 ? LogicalDirection.Forward : LogicalDirection.Backward);

                _caretIndex = _presenter.CaretIndex;
            }
            else
            {
                if (direction > 0)
                {
                    CaretIndex += StringUtils.NextWord(text, caretIndex) - caretIndex;
                }
                else
                {
                    CaretIndex += StringUtils.PreviousWord(text, caretIndex) - caretIndex;
                }
            }
        }

        private void MoveHome(bool document)
        {
            var text = Text ?? string.Empty;
            var caretIndex = CaretIndex;

            if (document)
            {
                caretIndex = 0;
            }
            else
            {
                var lines = _presenter.TextLayout.TextLines;
                var pos = 0;

                foreach (var line in lines)
                {
                    if (pos + line.TextRange.Length > caretIndex || pos + line.TextRange.Length == text.Length)
                    {
                        break;
                    }

                    pos += line.TextRange.Length;
                }

                caretIndex = pos;
            }

            CaretIndex = caretIndex;
        }

        private void MoveEnd(bool document)
        {
            var text = Text ?? string.Empty;
            var caretIndex = CaretIndex;

            if (document)
            {
                caretIndex = text.Length;
            }
            else
            {
                var lines = _presenter.TextLayout.TextLines;
                var pos = 0;

                foreach (var line in lines)
                {
                    pos += line.TextRange.Length;

                    if (pos > caretIndex)
                    {
                        if (pos < text.Length)
                        {
                            --pos;
                            if (pos > 0 && text[pos - 1] == '\r' && text[pos] == '\n')
                            {
                                --pos;
                            }
                        }

                        break;
                    }
                }

                caretIndex = pos;
            }

            CaretIndex = caretIndex;
        }

        /// <summary>
        /// Select all text in the TextBox
        /// </summary>
        public void SelectAll()
        {
            SelectionStart = 0;
            SelectionEnd = Text?.Length ?? 0;
            CaretIndex = SelectionEnd;
        }

        private bool DeleteSelection()
        {
            if (!IsReadOnly)
            {
                var selectionStart = SelectionStart;
                var selectionEnd = SelectionEnd;

                if (selectionStart != selectionEnd)
                {
                    var start = Math.Min(selectionStart, selectionEnd);
                    var end = Math.Max(selectionStart, selectionEnd);
                    var text = Text;
                    SetTextInternal(text.Substring(0, start) + text.Substring(end));
                    CaretIndex = start;
                    ClearSelection();
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return true;
            }
        }

        private string GetSelection()
        {
            var text = Text;
            if (string.IsNullOrEmpty(text))
                return "";
            var selectionStart = SelectionStart;
            var selectionEnd = SelectionEnd;
            var start = Math.Min(selectionStart, selectionEnd);
            var end = Math.Max(selectionStart, selectionEnd);
            if (start == end || (Text?.Length ?? 0) < end)
            {
                return "";
            }
            return text.Substring(start, end - start);
        }

        private void SetTextInternal(string value)
        {
            try
            {
                _ignoreTextChanges = true;
                SetAndRaise(TextProperty, ref _text, value);
            }
            finally
            {
                _ignoreTextChanges = false;
            }
        }

        private void SetSelectionForControlBackspace()
        {
            SelectionStart = CaretIndex;
            MoveHorizontal(-1, true, false);
            SelectionEnd = CaretIndex;
        }

        private void SetSelectionForControlDelete()
        {
            SelectionStart = CaretIndex;
            MoveHorizontal(1, true, false);
            SelectionEnd = CaretIndex;
        }

        private void UpdatePseudoclasses()
        {
            PseudoClasses.Set(":empty", string.IsNullOrWhiteSpace(Text));
        }

        private bool IsPasswordBox => PasswordChar != default(char);

        UndoRedoState UndoRedoHelper<UndoRedoState>.IUndoRedoHost.UndoRedoState
        {
            get { return new UndoRedoState(Text, CaretIndex); }
            set
            {
                Text = value.Text;
                CaretIndex = value.CaretPosition;
                ClearSelection();
            }
        }
    }
}
