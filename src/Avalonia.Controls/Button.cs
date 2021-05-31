using System;
using System.Linq;
using System.Windows.Input;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Platform;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.VisualTree;

namespace Avalonia.Controls
{
    /// <summary>
    /// Defines how a <see cref="Button"/> reacts to clicks.
    /// </summary>
    public enum ClickMode
    {
        /// <summary>
        /// The <see cref="Button.Click"/> event is raised when the pointer is released.
        /// </summary>
        Release,

        /// <summary>
        /// The <see cref="Button.Click"/> event is raised when the pointer is pressed.
        /// </summary>
        Press,
    }

    /// <summary>
    /// A button control.
    /// </summary>
    [PseudoClasses(":pressed")]
    public class Button : ContentControl, ICommandSource
    {
        /// <summary>
        /// Defines the <see cref="ClickMode"/> property.
        /// </summary>
        public static readonly StyledProperty<ClickMode> ClickModeProperty =
            AvaloniaProperty.Register<Button, ClickMode>(nameof(ClickMode));

        /// <summary>
        /// Defines the <see cref="Command"/> property.
        /// </summary>
        public static readonly DirectProperty<Button, ICommand> CommandProperty =
            AvaloniaProperty.RegisterDirect<Button, ICommand>(nameof(Command),
                button => button.Command, (button, command) => button.Command = command, enableDataValidation: true);

        /// <summary>
        /// Defines the <see cref="HotKey"/> property.
        /// </summary>
        public static readonly StyledProperty<KeyGesture> HotKeyProperty =
            HotKeyManager.HotKeyProperty.AddOwner<Button>();

        /// <summary>
        /// Defines the <see cref="CommandParameter"/> property.
        /// </summary>
        public static readonly StyledProperty<object> CommandParameterProperty =
            AvaloniaProperty.Register<Button, object>(nameof(CommandParameter));

        /// <summary>
        /// Defines the <see cref="IsDefaultProperty"/> property.
        /// </summary>
        public static readonly StyledProperty<bool> IsDefaultProperty =
            AvaloniaProperty.Register<Button, bool>(nameof(IsDefault));

        /// <summary>
        /// Defines the <see cref="IsCancelProperty"/> property.
        /// </summary>
        public static readonly StyledProperty<bool> IsCancelProperty =
            AvaloniaProperty.Register<Button, bool>(nameof(IsCancel));

        /// <summary>
        /// Defines the <see cref="Click"/> event.
        /// </summary>
        public static readonly RoutedEvent<RoutedEventArgs> ClickEvent =
            RoutedEvent.Register<Button, RoutedEventArgs>(nameof(Click), RoutingStrategies.Bubble);

        public static readonly StyledProperty<bool> IsPressedProperty =
            AvaloniaProperty.Register<Button, bool>(nameof(IsPressed));

        /// <summary>
        /// Defines the <see cref="Flyout"/> property
        /// </summary>
        public static readonly StyledProperty<FlyoutBase> FlyoutProperty =
            AvaloniaProperty.Register<Button, FlyoutBase>(nameof(Flyout));

        private ICommand _command;
        private bool _commandCanExecute = true;
        private KeyGesture _hotkey;

        /// <summary>
        /// Initializes static members of the <see cref="Button"/> class.
        /// </summary>
        static Button()
        {
            FocusableProperty.OverrideDefaultValue(typeof(Button), true);
            CommandProperty.Changed.Subscribe(CommandChanged);
            CommandParameterProperty.Changed.Subscribe(CommandParameterChanged);
            IsDefaultProperty.Changed.Subscribe(IsDefaultChanged);
            IsCancelProperty.Changed.Subscribe(IsCancelChanged);
        }

        public Button()
        {
            UpdatePseudoClasses(IsPressed);
        }

        /// <summary>
        /// Raised when the user clicks the button.
        /// </summary>
        public event EventHandler<RoutedEventArgs> Click
        {
            add { AddHandler(ClickEvent, value); }
            remove { RemoveHandler(ClickEvent, value); }
        }

        /// <summary>
        /// Gets or sets a value indicating how the <see cref="Button"/> should react to clicks.
        /// </summary>
        public ClickMode ClickMode
        {
            get { return GetValue(ClickModeProperty); }
            set { SetValue(ClickModeProperty, value); }
        }

        /// <summary>
        /// Gets or sets an <see cref="ICommand"/> to be invoked when the button is clicked.
        /// </summary>
        public ICommand Command
        {
            get { return _command; }
            set { SetAndRaise(CommandProperty, ref _command, value); }
        }

        /// <summary>
        /// Gets or sets an <see cref="KeyGesture"/> associated with this control
        /// </summary>
        public KeyGesture HotKey
        {
            get { return GetValue(HotKeyProperty); }
            set { SetValue(HotKeyProperty, value); }
        }

        /// <summary>
        /// Gets or sets a parameter to be passed to the <see cref="Command"/>.
        /// </summary>
        public object CommandParameter
        {
            get { return GetValue(CommandParameterProperty); }
            set { SetValue(CommandParameterProperty, value); }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the button is the default button for the
        /// window.
        /// </summary>
        public bool IsDefault
        {
            get { return GetValue(IsDefaultProperty); }
            set { SetValue(IsDefaultProperty, value); }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the button is the Cancel button for the
        /// window.
        /// </summary>
        public bool IsCancel
        {
            get { return GetValue(IsCancelProperty); }
            set { SetValue(IsCancelProperty, value); }
        }

        public bool IsPressed
        {
            get { return GetValue(IsPressedProperty); }
            private set { SetValue(IsPressedProperty, value); }
        }

        /// <summary>
        /// Gets or sets the Flyout that should be shown with this button
        /// </summary>
        public FlyoutBase Flyout
        {
            get => GetValue(FlyoutProperty);
            set => SetValue(FlyoutProperty, value);
        }

        protected override bool IsEnabledCore => base.IsEnabledCore && _commandCanExecute; 

        /// <inheritdoc/>
        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);

            if (IsDefault)
            {
                if (e.Root is IInputElement inputElement)
                {
                    ListenForDefault(inputElement);
                }
            }
            if (IsCancel)
            {
                if (e.Root is IInputElement inputElement)
                {
                    ListenForCancel(inputElement);
                }
            }
        }

        /// <inheritdoc/>
        protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnDetachedFromVisualTree(e);

            if (IsDefault)
            {
                if (e.Root is IInputElement inputElement)
                {
                    StopListeningForDefault(inputElement);
                }
            }
        }

        protected override void OnAttachedToLogicalTree(LogicalTreeAttachmentEventArgs e)
        {
            if (_hotkey != null) // Control attached again, set Hotkey to create a hotkey manager for this control
            {
                HotKey = _hotkey;
            }
            
            base.OnAttachedToLogicalTree(e);

            if (Command != null)
            {
                Command.CanExecuteChanged += CanExecuteChanged;
                CanExecuteChanged(this, EventArgs.Empty);
            }
        }

        protected override void OnDetachedFromLogicalTree(LogicalTreeAttachmentEventArgs e)
        {
            // This will cause the hotkey manager to dispose the observer and the reference to this control
            if (HotKey != null)
            {
                _hotkey = HotKey;
                HotKey = null;
            }

            base.OnDetachedFromLogicalTree(e);

            if (Command != null)
            {
                Command.CanExecuteChanged -= CanExecuteChanged;
            }
        }

        /// <inheritdoc/>
        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                OnClick();
                e.Handled = true;
            }
            else if (e.Key == Key.Space)
            {
                if (ClickMode == ClickMode.Press)
                {
                    OnClick();
                }
                IsPressed = true;
                e.Handled = true;
            }
            else if (e.Key == Key.Escape && Flyout != null)
            {
                // If Flyout doesn't have focusable content, close the flyout here
                Flyout.Hide();
            }

            base.OnKeyDown(e);
        }

        /// <inheritdoc/>
        protected override void OnKeyUp(KeyEventArgs e)
        {
            if (e.Key == Key.Space)
            {
                if (ClickMode == ClickMode.Release)
                {
                    OnClick();
                }
                IsPressed = false;
                e.Handled = true;
            }
        }

        /// <summary>
        /// Invokes the <see cref="Click"/> event.
        /// </summary>
        protected virtual void OnClick()
        {
            OpenFlyout();

            var e = new RoutedEventArgs(ClickEvent);
            RaiseEvent(e);

            if (!e.Handled && Command?.CanExecute(CommandParameter) == true)
            {
                Command.Execute(CommandParameter);
                e.Handled = true;
            }
        }

        protected virtual void OpenFlyout()
        {
            Flyout?.ShowAt(this);
        }

        /// <inheritdoc/>
        protected override void OnPointerPressed(PointerPressedEventArgs e)
        {
            base.OnPointerPressed(e);

            if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            {
                IsPressed = true;
                e.Handled = true;

                if (ClickMode == ClickMode.Press)
                {
                    OnClick();
                }
            }
        }

        /// <inheritdoc/>
        protected override void OnPointerReleased(PointerReleasedEventArgs e)
        {
            base.OnPointerReleased(e);

            if (IsPressed && e.InitialPressMouseButton == MouseButton.Left)
            {
                IsPressed = false;
                e.Handled = true;

                if (ClickMode == ClickMode.Release &&
                    this.GetVisualsAt(e.GetPosition(this)).Any(c => this == c || this.IsVisualAncestorOf(c)))
                {
                    OnClick();
                }
            }
        }
        
        protected override void OnPointerCaptureLost(PointerCaptureLostEventArgs e)
        {
            IsPressed = false;
        }

        protected override void OnPropertyChanged<T>(AvaloniaPropertyChangedEventArgs<T> change)
        {
            base.OnPropertyChanged(change);

            if (change.Property == IsPressedProperty)
            {
                UpdatePseudoClasses(change.NewValue.GetValueOrDefault<bool>());
            }
            else if (change.Property == FlyoutProperty)
            {
                // If flyout is changed while one is already open, make sure we 
                // close the old one first
                if (change.OldValue.GetValueOrDefault() is FlyoutBase oldFlyout &&
                    oldFlyout.IsOpen)
                {
                    oldFlyout.Hide();
                }
            }
        }

        protected override AutomationPeer OnCreateAutomationPeer(IAutomationNodeFactory factory)
        {
            return new ButtonAutomationPeer(factory, this);
        }

        protected override void UpdateDataValidation<T>(AvaloniaProperty<T> property, BindingValue<T> value)
        {
            base.UpdateDataValidation(property, value);
            if (property == CommandProperty)
            {
                if (value.Type == BindingValueType.BindingError)
                {
                    if (_commandCanExecute)
                    {
                        _commandCanExecute = false;
                        UpdateIsEffectivelyEnabled();
                    }
                }
            }
        }

        internal void PerformClick() => OnClick();

        /// <summary>
        /// Called when the <see cref="Command"/> property changes.
        /// </summary>
        /// <param name="e">The event args.</param>
        private static void CommandChanged(AvaloniaPropertyChangedEventArgs e)
        {
            if (e.Sender is Button button)
            {
                if (((ILogical)button).IsAttachedToLogicalTree)
                {
                    if (e.OldValue is ICommand oldCommand)
                    {
                        oldCommand.CanExecuteChanged -= button.CanExecuteChanged;
                    }

                    if (e.NewValue is ICommand newCommand)
                    {
                        newCommand.CanExecuteChanged += button.CanExecuteChanged;
                    }
                }

                button.CanExecuteChanged(button, EventArgs.Empty);
            }
        }

        /// <summary>
        /// Called when the <see cref="CommandParameter"/> property changes.
        /// </summary>
        /// <param name="e">The event args.</param>
        private static void CommandParameterChanged(AvaloniaPropertyChangedEventArgs e)
        {
            if (e.Sender is Button button)
            {
                button.CanExecuteChanged(button, EventArgs.Empty);
            }
        }

        /// <summary>
        /// Called when the <see cref="IsDefault"/> property changes.
        /// </summary>
        /// <param name="e">The event args.</param>
        private static void IsDefaultChanged(AvaloniaPropertyChangedEventArgs e)
        {
            var button = e.Sender as Button;
            var isDefault = (bool)e.NewValue;

            if (button?.VisualRoot is IInputElement inputRoot)
            {
                if (isDefault)
                {
                    button.ListenForDefault(inputRoot);
                }
                else
                {
                    button.StopListeningForDefault(inputRoot);
                }
            }
        }

        /// <summary>
        /// Called when the <see cref="IsCancel"/> property changes.
        /// </summary>
        /// <param name="e">The event args.</param>
        private static void IsCancelChanged(AvaloniaPropertyChangedEventArgs e)
        {
            var button = e.Sender as Button;
            var isCancel = (bool)e.NewValue;

            if (button?.VisualRoot is IInputElement inputRoot)
            {
                if (isCancel)
                {
                    button.ListenForCancel(inputRoot);
                }
                else
                {
                    button.StopListeningForCancel(inputRoot);
                }
            }
        }

        /// <summary>
        /// Called when the <see cref="ICommand.CanExecuteChanged"/> event fires.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event args.</param>
        private void CanExecuteChanged(object sender, EventArgs e)
        {
            var canExecute = Command == null || Command.CanExecute(CommandParameter);

            if (canExecute != _commandCanExecute)
            {
                _commandCanExecute = canExecute;
                UpdateIsEffectivelyEnabled();
            }
        }

        /// <summary>
        /// Starts listening for the Enter key when the button <see cref="IsDefault"/>.
        /// </summary>
        /// <param name="root">The input root.</param>
        private void ListenForDefault(IInputElement root)
        {
            root.AddHandler(KeyDownEvent, RootDefaultKeyDown);
        }

        /// <summary>
        /// Starts listening for the Escape key when the button <see cref="IsCancel"/>.
        /// </summary>
        /// <param name="root">The input root.</param>
        private void ListenForCancel(IInputElement root)
        {
            root.AddHandler(KeyDownEvent, RootCancelKeyDown);
        }

        /// <summary>
        /// Stops listening for the Enter key when the button is no longer <see cref="IsDefault"/>.
        /// </summary>
        /// <param name="root">The input root.</param>
        private void StopListeningForDefault(IInputElement root)
        {
            root.RemoveHandler(KeyDownEvent, RootDefaultKeyDown);
        }

        /// <summary>
        /// Stops listening for the Escape key when the button is no longer <see cref="IsCancel"/>.
        /// </summary>
        /// <param name="root">The input root.</param>
        private void StopListeningForCancel(IInputElement root)
        {
            root.RemoveHandler(KeyDownEvent, RootCancelKeyDown);
        }

        /// <summary>
        /// Called when a key is pressed on the input root and the button <see cref="IsDefault"/>.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event args.</param>
        private void RootDefaultKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && IsVisible && IsEnabled)
            {
                OnClick();
            }
        }

        /// <summary>
        /// Called when a key is pressed on the input root and the button <see cref="IsCancel"/>.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event args.</param>
        private void RootCancelKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape && IsVisible && IsEnabled)
            {
                OnClick();
            }
        }

        private void UpdatePseudoClasses(bool isPressed)
        {
            PseudoClasses.Set(":pressed", isPressed);
        }

        void ICommandSource.CanExecuteChanged(object sender, EventArgs e) => this.CanExecuteChanged(sender, e);
    }
}
