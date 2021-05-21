using System;
using System.Collections.Generic;
using System.ComponentModel;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Input.GestureRecognizers;
using Avalonia.Input.TextInput;
using Avalonia.Interactivity;
using Avalonia.VisualTree;

namespace Avalonia.Input
{
    [PseudoClasses(":disabled", ":focus", ":focus-visible", ":pointerover")]
    public abstract class ContentInputElement : StyledElement, IInputElement
    {
        private EventHandlers _eventHandlers;

        #region Interactive

        /// <inheritdoc/>
        IInteractive? IInteractive.InteractiveParent => ((IInputElement)this).InputParent;

        /// <inheritdoc/>
        public void AddHandler(
            RoutedEvent routedEvent,
            Delegate handler,
            RoutingStrategies routes = RoutingStrategies.Direct | RoutingStrategies.Bubble,
            bool handledEventsToo = false)
        {
            _eventHandlers.AddHandler(routedEvent, handler, routes, handledEventsToo);
        }

        /// <inheritdoc/>
        public void AddHandler<TEventArgs>(
            RoutedEvent<TEventArgs> routedEvent,
            EventHandler<TEventArgs> handler,
            RoutingStrategies routes = RoutingStrategies.Direct | RoutingStrategies.Bubble,
            bool handledEventsToo = false) where TEventArgs : RoutedEventArgs
        {
            _eventHandlers.AddHandler(routedEvent, handler, routes, handledEventsToo);
        }

        /// <inheritdoc/>
        public void RemoveHandler(RoutedEvent routedEvent, Delegate handler)
        {
            _eventHandlers.RemoveHandler(routedEvent, handler);
        }

        /// <inheritdoc/>
        public void RemoveHandler<TEventArgs>(RoutedEvent<TEventArgs> routedEvent, EventHandler<TEventArgs> handler)
            where TEventArgs : RoutedEventArgs
        {
            RemoveHandler(routedEvent, (Delegate)handler);
        }

        /// <inheritdoc/>
        public void RaiseEvent(RoutedEventArgs e)
        {
            EventHandlers.RaiseEvent(this, e);
        }

        /// <inheritdoc/>
        void IInteractive.AddToEventRoute(RoutedEvent routedEvent, EventRoute route)
        {
            _eventHandlers.AddToEventRoute(this, routedEvent, route);
        }

        #endregion

        #region InputElement

        /// <summary>
        /// Defines the <see cref="Focusable"/> property.
        /// </summary>
        public static readonly StyledProperty<bool> FocusableProperty =
            AvaloniaProperty.Register<ContentInputElement, bool>(nameof(Focusable));

        /// <summary>
        /// Defines the <see cref="IsEnabled"/> property.
        /// </summary>
        public static readonly StyledProperty<bool> IsEnabledProperty =
            AvaloniaProperty.Register<ContentInputElement, bool>(nameof(IsEnabled), true);

        /// <summary>
        /// Gets or sets associated mouse cursor.
        /// </summary>
        public static readonly StyledProperty<Cursor?> CursorProperty =
            AvaloniaProperty.Register<ContentInputElement, Cursor?>(nameof(Cursor), null, true);

        /// <summary>
        /// Defines the <see cref="IsHitTestVisible"/> property.
        /// </summary>
        public static readonly StyledProperty<bool> IsHitTestVisibleProperty =
            AvaloniaProperty.Register<ContentInputElement, bool>(nameof(IsHitTestVisible), true);

        private bool _isEffectivelyEnabled = true;
        private bool _isFocused;
        private bool _isKeyboardFocusWithin;
        private bool _isFocusVisible;
        private bool _isPointerOver;
        private GestureRecognizerCollection? _gestureRecognizers;

        /// <summary>
        /// Initializes static members of the <see cref="ContentInputElement"/> class.
        /// </summary>
        static ContentInputElement()
        {
            KeyboardNavigation.TabNavigationProperty.AddOwner<ContentInputElement>();
            KeyboardNavigation.IsTabStopProperty.AddOwner<ContentInputElement>();
            KeyboardNavigation.TabOnceActiveElementProperty.AddOwner<ContentInputElement>();

            // The read-only properties are shared with InputElement
            InputElement.IsEffectivelyEnabledProperty.AddOwner<ContentInputElement>(
                x => x.IsEffectivelyEnabled
            );
            InputElement.IsPointerOverProperty.AddOwner<ContentInputElement>(
                x => x.IsPointerOver
            );
            InputElement.IsKeyboardFocusWithinProperty.AddOwner<ContentInputElement>(
                x => x.IsKeyboardFocusWithin
            );
            InputElement.IsFocusedProperty.AddOwner<ContentInputElement>(
                x => x.IsFocused
            );

            InputElement.GotFocusEvent.AddClassHandler<ContentInputElement>((x, e) => x.OnGotFocus(e));
            InputElement.LostFocusEvent.AddClassHandler<ContentInputElement>((x, e) => x.OnLostFocus(e));
            InputElement.KeyDownEvent.AddClassHandler<ContentInputElement>((x, e) => x.OnKeyDown(e));
            InputElement.KeyUpEvent.AddClassHandler<ContentInputElement>((x, e) => x.OnKeyUp(e));
            InputElement.TextInputEvent.AddClassHandler<ContentInputElement>((x, e) => x.OnTextInput(e));
            InputElement.PointerEnterEvent.AddClassHandler<ContentInputElement>((x, e) => x.OnPointerEnterCore(e));
            InputElement.PointerLeaveEvent.AddClassHandler<ContentInputElement>((x, e) => x.OnPointerLeaveCore(e));
            InputElement.PointerMovedEvent.AddClassHandler<ContentInputElement>((x, e) => x.OnPointerMoved(e));
            InputElement.PointerPressedEvent.AddClassHandler<ContentInputElement>((x, e) => x.OnPointerPressed(e));
            InputElement.PointerReleasedEvent.AddClassHandler<ContentInputElement>((x, e) => x.OnPointerReleased(e));
            InputElement.PointerCaptureLostEvent.AddClassHandler<ContentInputElement>((x, e) => x.OnPointerCaptureLost(e));
            InputElement.PointerWheelChangedEvent.AddClassHandler<ContentInputElement>((x, e) => x.OnPointerWheelChanged(e));
        }

        public ContentInputElement()
        {
            UpdatePseudoClasses(IsFocused, IsPointerOver);
        }

        /// <summary>
        /// Occurs when the control receives focus.
        /// </summary>
        public event EventHandler<GotFocusEventArgs> GotFocus
        {
            add { AddHandler(InputElement.GotFocusEvent, value); }
            remove { RemoveHandler(InputElement.GotFocusEvent, value); }
        }

        /// <summary>
        /// Occurs when the control loses focus.
        /// </summary>
        public event EventHandler<RoutedEventArgs> LostFocus
        {
            add { AddHandler(InputElement.LostFocusEvent, value); }
            remove { RemoveHandler(InputElement.LostFocusEvent, value); }
        }

        /// <summary>
        /// Occurs when a key is pressed while the control has focus.
        /// </summary>
        public event EventHandler<KeyEventArgs> KeyDown
        {
            add { AddHandler(InputElement.KeyDownEvent, value); }
            remove { RemoveHandler(InputElement.KeyDownEvent, value); }
        }

        /// <summary>
        /// Occurs when a key is released while the control has focus.
        /// </summary>
        public event EventHandler<KeyEventArgs> KeyUp
        {
            add { AddHandler(InputElement.KeyUpEvent, value); }
            remove { RemoveHandler(InputElement.KeyUpEvent, value); }
        }

        /// <summary>
        /// Occurs when a user typed some text while the control has focus.
        /// </summary>
        public event EventHandler<TextInputEventArgs> TextInput
        {
            add { AddHandler(InputElement.TextInputEvent, value); }
            remove { RemoveHandler(InputElement.TextInputEvent, value); }
        }

        /// <summary>
        /// Occurs when an input element gains input focus and input method is looking for the corresponding client
        /// </summary>
        public event EventHandler<TextInputMethodClientRequestedEventArgs> TextInputMethodClientRequested
        {
            add { AddHandler(InputElement.TextInputMethodClientRequestedEvent, value); }
            remove { RemoveHandler(InputElement.TextInputMethodClientRequestedEvent, value); }
        }

        /// <summary>
        /// Occurs when an input element gains input focus and input method is asking for required content options
        /// </summary>
        public event EventHandler<TextInputOptionsQueryEventArgs> TextInputOptionsQuery
        {
            add { AddHandler(InputElement.TextInputOptionsQueryEvent, value); }
            remove { RemoveHandler(InputElement.TextInputOptionsQueryEvent, value); }
        }

        /// <summary>
        /// Occurs when the pointer enters the control.
        /// </summary>
        public event EventHandler<PointerEventArgs> PointerEnter
        {
            add { AddHandler(InputElement.PointerEnterEvent, value); }
            remove { RemoveHandler(InputElement.PointerEnterEvent, value); }
        }

        /// <summary>
        /// Occurs when the pointer leaves the control.
        /// </summary>
        public event EventHandler<PointerEventArgs> PointerLeave
        {
            add { AddHandler(InputElement.PointerLeaveEvent, value); }
            remove { RemoveHandler(InputElement.PointerLeaveEvent, value); }
        }

        /// <summary>
        /// Occurs when the pointer moves over the control.
        /// </summary>
        public event EventHandler<PointerEventArgs> PointerMoved
        {
            add { AddHandler(InputElement.PointerMovedEvent, value); }
            remove { RemoveHandler(InputElement.PointerMovedEvent, value); }
        }

        /// <summary>
        /// Occurs when the pointer is pressed over the control.
        /// </summary>
        public event EventHandler<PointerPressedEventArgs> PointerPressed
        {
            add { AddHandler(InputElement.PointerPressedEvent, value); }
            remove { RemoveHandler(InputElement.PointerPressedEvent, value); }
        }

        /// <summary>
        /// Occurs when the pointer is released over the control.
        /// </summary>
        public event EventHandler<PointerReleasedEventArgs> PointerReleased
        {
            add { AddHandler(InputElement.PointerReleasedEvent, value); }
            remove { RemoveHandler(InputElement.PointerReleasedEvent, value); }
        }

        /// <summary>
        /// Occurs when the control or its child control loses the pointer capture for any reason,
        /// event will not be triggered for a parent control if capture was transferred to another child of that parent control
        /// </summary>
        public event EventHandler<PointerCaptureLostEventArgs> PointerCaptureLost
        {
            add => AddHandler(InputElement.PointerCaptureLostEvent, value);
            remove => RemoveHandler(InputElement.PointerCaptureLostEvent, value);
        }

        /// <summary>
        /// Occurs when the mouse wheen is scrolled over the control.
        /// </summary>
        public event EventHandler<PointerWheelEventArgs> PointerWheelChanged
        {
            add { AddHandler(InputElement.PointerWheelChangedEvent, value); }
            remove { RemoveHandler(InputElement.PointerWheelChangedEvent, value); }
        }

        /// <summary>
        /// Occurs when a tap gesture occurs on the control.
        /// </summary>
        public event EventHandler<RoutedEventArgs> Tapped
        {
            add { AddHandler(InputElement.TappedEvent, value); }
            remove { RemoveHandler(InputElement.TappedEvent, value); }
        }

        /// <summary>
        /// Occurs when a double-tap gesture occurs on the control.
        /// </summary>
        public event EventHandler<RoutedEventArgs> DoubleTapped
        {
            add { AddHandler(InputElement.DoubleTappedEvent, value); }
            remove { RemoveHandler(InputElement.DoubleTappedEvent, value); }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the control can receive focus.
        /// </summary>
        public bool Focusable
        {
            get { return GetValue(FocusableProperty); }
            set { SetValue(FocusableProperty, value); }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the control is enabled for user interaction.
        /// </summary>
        public bool IsEnabled
        {
            get { return GetValue(IsEnabledProperty); }
            set { SetValue(IsEnabledProperty, value); }
        }

        /// <summary>
        /// Gets or sets associated mouse cursor.
        /// </summary>
        public Cursor? Cursor
        {
            get { return GetValue(CursorProperty); }
            set { SetValue(CursorProperty, value); }
        }

        /// <summary>
        /// Gets a value indicating whether keyboard focus is anywhere within the element or its visual tree child elements.
        /// </summary>
        public bool IsKeyboardFocusWithin
        {
            get => _isKeyboardFocusWithin;
            internal set => SetAndRaise(InputElement.IsKeyboardFocusWithinProperty, ref _isKeyboardFocusWithin, value);
        }

        /// <summary>
        /// Gets a value indicating whether the control is focused.
        /// </summary>
        public bool IsFocused
        {
            get { return _isFocused; }
            private set { SetAndRaise(InputElement.IsFocusedProperty, ref _isFocused, value); }
        }

        /// <inheritdoc/>
        public bool IsTabFocusable => KeyboardNavigation.GetIsTabStop(this);

        /// <inheritdoc/>
        public KeyboardNavigationMode TabNavigation => KeyboardNavigation.GetTabNavigation(this);

        /// <inheritdoc/>
        public IInputElement? TabOnceActiveElement => KeyboardNavigation.GetTabOnceActiveElement(this);

        /// <summary>
        /// Gets or sets a value indicating whether the control is considered for hit testing.
        /// </summary>
        public bool IsHitTestVisible
        {
            get { return GetValue(IsHitTestVisibleProperty); }
            set { SetValue(IsHitTestVisibleProperty, value); }
        }

        /// <summary>
        /// Gets a value indicating whether the pointer is currently over the control.
        /// </summary>
        public bool IsPointerOver
        {
            get { return _isPointerOver; }
            internal set { SetAndRaise(InputElement.IsPointerOverProperty, ref _isPointerOver, value); }
        }

        /// <inheritdoc/>
        public bool IsEffectivelyEnabled
        {
            get => _isEffectivelyEnabled;
            private set
            {
                SetAndRaise(InputElement.IsEffectivelyEnabledProperty, ref _isEffectivelyEnabled, value);
                PseudoClasses.Set(":disabled", !value);
            }
        }

        public List<KeyBinding> KeyBindings { get; } = new List<KeyBinding>();

        /// <inheritdoc/>
        public IInputRoot? InputRoot
        {
            get
            {
                var closestVisual = this.GetClosestVisual();
                if (closestVisual is { IsAttachedToVisualTree: true })
                {
                    return closestVisual.VisualRoot as IInputRoot;
                }
                else
                {
                    return null;
                }
            }
        }

        public abstract IInputElement? InputParent { get; }

        public abstract IEnumerable<IInputElement> InputChildren { get; }

        /// <summary>
        /// Allows a derived class to override the enabled state of the control.
        /// </summary>
        /// <remarks>
        /// Derived controls may wish to disable the enabled state of the control without overwriting the
        /// user-supplied <see cref="IsEnabled"/> setting. This can be done by overriding this property
        /// to return the overridden enabled state. If the value returned from <see cref="IsEnabledCore"/>
        /// should change, then the derived control should call <see cref="UpdateIsEffectivelyEnabled()"/>.
        /// </remarks>
        protected virtual bool IsEnabledCore => IsEnabled;

        public GestureRecognizerCollection GestureRecognizers
            => _gestureRecognizers ?? (_gestureRecognizers = new GestureRecognizerCollection(this));

        /// <summary>
        /// Focuses the control.
        /// </summary>
        public void Focus()
        {
            FocusManager.Instance?.Focus(this);
        }

        protected void OnDetachedFromVisualTreeCore(VisualTreeAttachmentEventArgs e)
        {
            if (IsFocused)
            {
                FocusManager.Instance.Focus(null);
            }
        }

        /// <inheritdoc/>
        protected void OnAttachedToVisualTreeCore(VisualTreeAttachmentEventArgs e)
        {
            UpdateIsEffectivelyEnabled();
        }

        /// <summary>
        /// Called before the <see cref="GotFocus"/> event occurs.
        /// </summary>
        /// <param name="e">The event args.</param>
        protected virtual void OnGotFocus(GotFocusEventArgs e)
        {
            var isFocused = e.Source == this;
            _isFocusVisible = isFocused && (e.NavigationMethod == NavigationMethod.Directional || e.NavigationMethod == NavigationMethod.Tab);
            IsFocused = isFocused;
        }

        /// <summary>
        /// Called before the <see cref="LostFocus"/> event occurs.
        /// </summary>
        /// <param name="e">The event args.</param>
        protected virtual void OnLostFocus(RoutedEventArgs e)
        {
            _isFocusVisible = false;
            IsFocused = false;
        }

        /// <summary>
        /// Called before the <see cref="KeyDown"/> event occurs.
        /// </summary>
        /// <param name="e">The event args.</param>
        protected virtual void OnKeyDown(KeyEventArgs e)
        {
        }

        /// <summary>
        /// Called before the <see cref="KeyUp"/> event occurs.
        /// </summary>
        /// <param name="e">The event args.</param>
        protected virtual void OnKeyUp(KeyEventArgs e)
        {
        }

        /// <summary>
        /// Called before the <see cref="TextInput"/> event occurs.
        /// </summary>
        /// <param name="e">The event args.</param>
        protected virtual void OnTextInput(TextInputEventArgs e)
        {
        }

        /// <summary>
        /// Called before the <see cref="PointerEnter"/> event occurs.
        /// </summary>
        /// <param name="e">The event args.</param>
        protected virtual void OnPointerEnter(PointerEventArgs e)
        {
        }

        /// <summary>
        /// Called before the <see cref="PointerLeave"/> event occurs.
        /// </summary>
        /// <param name="e">The event args.</param>
        protected virtual void OnPointerLeave(PointerEventArgs e)
        {
        }

        /// <summary>
        /// Called before the <see cref="PointerMoved"/> event occurs.
        /// </summary>
        /// <param name="e">The event args.</param>
        protected virtual void OnPointerMoved(PointerEventArgs e)
        {
            if (_gestureRecognizers?.HandlePointerMoved(e) == true)
                e.Handled = true;
        }

        /// <summary>
        /// Called before the <see cref="PointerPressed"/> event occurs.
        /// </summary>
        /// <param name="e">The event args.</param>
        protected virtual void OnPointerPressed(PointerPressedEventArgs e)
        {
            if (_gestureRecognizers?.HandlePointerPressed(e) == true)
                e.Handled = true;
        }

        /// <summary>
        /// Called before the <see cref="PointerReleased"/> event occurs.
        /// </summary>
        /// <param name="e">The event args.</param>
        protected virtual void OnPointerReleased(PointerReleasedEventArgs e)
        {
            if (_gestureRecognizers?.HandlePointerReleased(e) == true)
                e.Handled = true;
        }

        /// <summary>
        /// Called before the <see cref="PointerCaptureLost"/> event occurs.
        /// </summary>
        /// <param name="e">The event args.</param>
        protected virtual void OnPointerCaptureLost(PointerCaptureLostEventArgs e)
        {
            _gestureRecognizers?.HandlePointerCaptureLost(e);
        }

        /// <summary>
        /// Called before the <see cref="PointerWheelChanged"/> event occurs.
        /// </summary>
        /// <param name="e">The event args.</param>
        protected virtual void OnPointerWheelChanged(PointerWheelEventArgs e)
        {
        }

        protected override void OnPropertyChanged<T>(AvaloniaPropertyChangedEventArgs<T> change)
        {
            base.OnPropertyChanged(change);

            if (change.Property == InputElement.IsFocusedProperty)
            {
                UpdatePseudoClasses(change.NewValue.GetValueOrDefault<bool>(), null);
            }
            else if (change.Property == InputElement.IsPointerOverProperty)
            {
                UpdatePseudoClasses(null, change.NewValue.GetValueOrDefault<bool>());
            }
            else if (change.Property == InputElement.IsKeyboardFocusWithinProperty)
            {
                PseudoClasses.Set(":focus-within", _isKeyboardFocusWithin);
            }
            else if (change.Property == InputElement.IsEnabledProperty)
            {
                UpdateIsEffectivelyEnabled();
            }
        }

        /// <summary>
        /// Updates the <see cref="IsEffectivelyEnabled"/> property value according to the parent
        /// control's enabled state and <see cref="IsEnabledCore"/>.
        /// </summary>
        protected void UpdateIsEffectivelyEnabled()
        {
            UpdateIsEffectivelyEnabled(((IInputElement)this).InputParent);
        }

        /// <summary>
        /// Called before the <see cref="PointerEnter"/> event occurs.
        /// </summary>
        /// <param name="e">The event args.</param>
        private void OnPointerEnterCore(PointerEventArgs e)
        {
            IsPointerOver = true;
            OnPointerEnter(e);
        }

        /// <summary>
        /// Called before the <see cref="PointerLeave"/> event occurs.
        /// </summary>
        /// <param name="e">The event args.</param>
        private void OnPointerLeaveCore(PointerEventArgs e)
        {
            IsPointerOver = false;
            OnPointerLeave(e);
        }

        /// <summary>
        /// Updates the <see cref="IsEffectivelyEnabled"/> property based on the parent's
        /// <see cref="IsEffectivelyEnabled"/>.
        /// </summary>
        /// <param name="parent">The parent control.</param>
        private void UpdateIsEffectivelyEnabled(IInputElement? parent)
        {
            IsEffectivelyEnabled = IsEnabledCore && (parent?.IsEffectivelyEnabled ?? true);

            // PERF-SENSITIVE: This is called on entire hierarchy and using foreach or LINQ
            // will cause extra allocations and overhead.

            var children = ((IInputElement)this).InputChildren;

            foreach (var child in children)
            {
                if (child is ContentInputElement inputElement)
                {
                    inputElement?.UpdateIsEffectivelyEnabled(this);
                }
            }
        }

        private void UpdatePseudoClasses(bool? isFocused, bool? isPointerOver)
        {
            if (isFocused.HasValue)
            {
                PseudoClasses.Set(":focus", isFocused.Value);
                PseudoClasses.Set(":focus-visible", _isFocusVisible);
            }

            if (isPointerOver.HasValue)
            {
                PseudoClasses.Set(":pointerover", isPointerOver.Value);
            }
        }
        #endregion
    }
}
