using System;
using System.Linq;
using System.Reactive.Disposables;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives.PopupPositioning;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.LogicalTree;
using Avalonia.Metadata;
using Avalonia.Platform;
using Avalonia.VisualTree;

#nullable enable

namespace Avalonia.Controls.Primitives
{
    /// <summary>
    /// Displays a popup window.
    /// </summary>
    public class Popup : Control, IVisualTreeHost
    {
        public static readonly StyledProperty<bool> WindowManagerAddShadowHintProperty =
            AvaloniaProperty.Register<PopupRoot, bool>(nameof(WindowManagerAddShadowHint), true);

        /// <summary>
        /// Defines the <see cref="Child"/> property.
        /// </summary>
        public static readonly StyledProperty<Control?> ChildProperty =
            AvaloniaProperty.Register<Popup, Control?>(nameof(Child));

        /// <summary>
        /// Defines the <see cref="IsOpen"/> property.
        /// </summary>
        public static readonly DirectProperty<Popup, bool> IsOpenProperty =
            AvaloniaProperty.RegisterDirect<Popup, bool>(
                nameof(IsOpen),
                o => o.IsOpen,
                (o, v) => o.IsOpen = v);

        /// <summary>
        /// Defines the <see cref="PlacementAnchor"/> property.
        /// </summary>
        public static readonly StyledProperty<PopupAnchor> PlacementAnchorProperty =
            AvaloniaProperty.Register<Popup, PopupAnchor>(nameof(PlacementAnchor));

        /// <summary>
        /// Defines the <see cref="PlacementConstraintAdjustment"/> property.
        /// </summary>
        public static readonly StyledProperty<PopupPositionerConstraintAdjustment> PlacementConstraintAdjustmentProperty =
            AvaloniaProperty.Register<Popup, PopupPositionerConstraintAdjustment>(
                nameof(PlacementConstraintAdjustment),
                PopupPositionerConstraintAdjustment.FlipX | PopupPositionerConstraintAdjustment.FlipY |
                PopupPositionerConstraintAdjustment.ResizeX | PopupPositionerConstraintAdjustment.ResizeY);

        /// <summary>
        /// Defines the <see cref="PlacementGravity"/> property.
        /// </summary>
        public static readonly StyledProperty<PopupGravity> PlacementGravityProperty =
            AvaloniaProperty.Register<Popup, PopupGravity>(nameof(PlacementGravity));

        /// <summary>
        /// Defines the <see cref="PlacementMode"/> property.
        /// </summary>
        public static readonly StyledProperty<PlacementMode> PlacementModeProperty =
            AvaloniaProperty.Register<Popup, PlacementMode>(nameof(PlacementMode), defaultValue: PlacementMode.Bottom);

        /// <summary>
        /// Defines the <see cref="PlacementRect"/> property.
        /// </summary>
        public static readonly StyledProperty<Rect?> PlacementRectProperty =
            AvaloniaProperty.Register<Popup, Rect?>(nameof(PlacementRect));

        /// <summary>
        /// Defines the <see cref="PlacementTarget"/> property.
        /// </summary>
        public static readonly StyledProperty<Control?> PlacementTargetProperty =
            AvaloniaProperty.Register<Popup, Control?>(nameof(PlacementTarget));

#pragma warning disable 618
        /// <summary>
        /// Defines the <see cref="ObeyScreenEdges"/> property.
        /// </summary>
        public static readonly StyledProperty<bool> ObeyScreenEdgesProperty =
            AvaloniaProperty.Register<Popup, bool>(nameof(ObeyScreenEdges), true);
#pragma warning restore 618

        public static readonly StyledProperty<bool> OverlayDismissEventPassThroughProperty =
            AvaloniaProperty.Register<Popup, bool>(nameof(OverlayDismissEventPassThrough));

        public static readonly DirectProperty<Popup, IInputElement> OverlayInputPassThroughElementProperty =
            AvaloniaProperty.RegisterDirect<Popup, IInputElement>(
                nameof(OverlayInputPassThroughElement),
                o => o.OverlayInputPassThroughElement,
                (o, v) => o.OverlayInputPassThroughElement = v);

        /// <summary>
        /// Defines the <see cref="HorizontalOffset"/> property.
        /// </summary>
        public static readonly StyledProperty<double> HorizontalOffsetProperty =
            AvaloniaProperty.Register<Popup, double>(nameof(HorizontalOffset));

        /// <summary>
        /// Defines the <see cref="IsLightDismissEnabled"/> property.
        /// </summary>
        public static readonly StyledProperty<bool> IsLightDismissEnabledProperty =
            AvaloniaProperty.Register<Popup, bool>(nameof(IsLightDismissEnabled));

        /// <summary>
        /// Defines the <see cref="VerticalOffset"/> property.
        /// </summary>
        public static readonly StyledProperty<double> VerticalOffsetProperty =
            AvaloniaProperty.Register<Popup, double>(nameof(VerticalOffset));

        /// <summary>
        /// Defines the <see cref="StaysOpen"/> property.
        /// </summary>
        [Obsolete("Use IsLightDismissEnabledProperty")]
        public static readonly DirectProperty<Popup, bool> StaysOpenProperty =
            AvaloniaProperty.RegisterDirect<Popup, bool>(
                nameof(StaysOpen),
                o => o.StaysOpen,
                (o, v) => o.StaysOpen = v,
                true);

        /// <summary>
        /// Defines the <see cref="Topmost"/> property.
        /// </summary>
        public static readonly StyledProperty<bool> TopmostProperty =
            AvaloniaProperty.Register<Popup, bool>(nameof(Topmost));

        /// <summary>
        /// Defines the <see cref="ShouldConstrainToRootBounds"/> property.
        /// </summary>
        public static readonly StyledProperty<bool> ShouldConstrainToRootBoundsProperty =
            AvaloniaProperty.Register<Popup, bool>(nameof(ShouldConstrainToRootBounds), false);

        /// <summary>
        /// Defines the <see cref="IsConstrainedToRootBounds"/> property.
        /// </summary>
        public static readonly DirectProperty<Popup, bool> IsConstrainedToRootBoundsProperty =
            AvaloniaProperty.RegisterDirect<Popup, bool>(nameof(IsConstrainedToRootBounds), o => o.IsConstrainedToRootBounds);

        private bool _isOpenRequested = false;
        private bool _isOpen;
        private bool _ignoreIsOpenChanged;
        private bool _isConstrainedToRootBounds;
        private PopupOpenState? _openState;
        private IInputElement _overlayInputPassThroughElement;

        /// <summary>
        /// Initializes static members of the <see cref="Popup"/> class.
        /// </summary>
        static Popup()
        {
            IsHitTestVisibleProperty.OverrideDefaultValue<Popup>(false);
            ChildProperty.Changed.AddClassHandler<Popup>((x, e) => x.ChildChanged(e));
            IsOpenProperty.Changed.AddClassHandler<Popup>((x, e) => x.IsOpenChanged((AvaloniaPropertyChangedEventArgs<bool>)e));
        }

        /// <summary>
        /// Raised when the popup closes.
        /// </summary>
        public event EventHandler<EventArgs>? Closed;

        /// <summary>
        /// Raised when the popup opens.
        /// </summary>
        public event EventHandler? Opened;

        public IPopupHost? Host => _openState?.PopupHost;

        public bool WindowManagerAddShadowHint
        {
            get { return GetValue(WindowManagerAddShadowHintProperty); }
            set { SetValue(WindowManagerAddShadowHintProperty, value); }
        }

        /// <summary>
        /// Gets or sets the control to display in the popup.
        /// </summary>
        [Content]
        public Control? Child
        {
            get { return GetValue(ChildProperty); }
            set { SetValue(ChildProperty, value); }
        }

        /// <summary>
        /// Gets or sets a dependency resolver for the <see cref="PopupRoot"/>.
        /// </summary>
        /// <remarks>
        /// This property allows a client to customize the behaviour of the popup by injecting
        /// a specialized dependency resolver into the <see cref="PopupRoot"/>'s constructor.
        /// </remarks>
        public IAvaloniaDependencyResolver? DependencyResolver
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a value that determines how the <see cref="Popup"/> can be dismissed.
        /// </summary>
        /// <remarks>
        /// Light dismiss is when the user taps on any area other than the popup.
        /// </remarks>
        public bool IsLightDismissEnabled
        {
            get => GetValue(IsLightDismissEnabledProperty);
            set => SetValue(IsLightDismissEnabledProperty, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether the popup is currently open.
        /// </summary>
        public bool IsOpen
        {
            get { return _isOpen; }
            set { SetAndRaise(IsOpenProperty, ref _isOpen, value); }
        }

        /// <summary>
        /// Gets or sets the anchor point on the <see cref="PlacementRect"/> when <see cref="PlacementMode"/>
        /// is <see cref="PlacementMode.AnchorAndGravity"/>.
        /// </summary>
        public PopupAnchor PlacementAnchor
        {
            get { return GetValue(PlacementAnchorProperty); }
            set { SetValue(PlacementAnchorProperty, value); }
        }

        /// <summary>
        /// Gets or sets a value describing how the popup position will be adjusted if the
        /// unadjusted position would result in the popup being partly constrained.
        /// </summary>
        public PopupPositionerConstraintAdjustment PlacementConstraintAdjustment
        {
            get { return GetValue(PlacementConstraintAdjustmentProperty); }
            set { SetValue(PlacementConstraintAdjustmentProperty, value); }
        }

        /// <summary>
        /// Gets or sets a value which defines in what direction the popup should open
        /// when <see cref="PlacementMode"/> is <see cref="PlacementMode.AnchorAndGravity"/>.
        /// </summary>
        public PopupGravity PlacementGravity
        {
            get { return GetValue(PlacementGravityProperty); }
            set { SetValue(PlacementGravityProperty, value); }
        }

        /// <summary>
        /// Gets or sets the placement mode of the popup in relation to the <see cref="PlacementTarget"/>.
        /// </summary>
        public PlacementMode PlacementMode
        {
            get { return GetValue(PlacementModeProperty); }
            set { SetValue(PlacementModeProperty, value); }
        }

        /// <summary>
        /// Gets or sets the the anchor rectangle within the parent that the popup will be placed
        /// relative to when <see cref="PlacementMode"/> is <see cref="PlacementMode.AnchorAndGravity"/>.
        /// </summary>
        /// <remarks>
        /// The placement rect defines a rectangle relative to <see cref="PlacementTarget"/> around
        /// which the popup will be opened, with <see cref="PlacementAnchor"/> determining which edge
        /// of the placement target is used.
        /// 
        /// If unset, the anchor rectangle will be the bounds of the <see cref="PlacementTarget"/>.
        /// </remarks>
        public Rect? PlacementRect
        {
            get { return GetValue(PlacementRectProperty); }
            set { SetValue(PlacementRectProperty, value); }
        }

        /// <summary>
        /// Gets or sets the control that is used to determine the popup's position.
        /// </summary>
        [ResolveByName]
        public Control? PlacementTarget
        {
            get { return GetValue(PlacementTargetProperty); }
            set { SetValue(PlacementTargetProperty, value); }
        }

        [Obsolete("This property has no effect")]
        public bool ObeyScreenEdges
        {
            get => GetValue(ObeyScreenEdgesProperty);
            set => SetValue(ObeyScreenEdgesProperty, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether the event that closes the popup is passed
        /// through to the parent window.
        /// </summary>
        /// <remarks>
        /// When <see cref="IsLightDismissEnabled"/> is set to true, clicks outside the the popup
        /// cause the popup to close. When <see cref="OverlayDismissEventPassThrough"/> is set to
        /// false, these clicks will be handled by the popup and not be registered by the parent
        /// window. When set to true, the events will be passed through to the parent window.
        /// </remarks>
        public bool OverlayDismissEventPassThrough
        {
            get => GetValue(OverlayDismissEventPassThroughProperty);
            set => SetValue(OverlayDismissEventPassThroughProperty, value);
        }

        /// <summary>
        /// Gets or sets an element that should receive pointer input events even when underneath
        /// the popup's overlay.
        /// </summary>
        public IInputElement OverlayInputPassThroughElement
        {
            get => _overlayInputPassThroughElement;
            set => SetAndRaise(OverlayInputPassThroughElementProperty, ref _overlayInputPassThroughElement, value);
        }

        /// <summary>
        /// Gets or sets the Horizontal offset of the popup in relation to the <see cref="PlacementTarget"/>.
        /// </summary>
        public double HorizontalOffset
        {
            get { return GetValue(HorizontalOffsetProperty); }
            set { SetValue(HorizontalOffsetProperty, value); }
        }

        /// <summary>
        /// Gets or sets the Vertical offset of the popup in relation to the <see cref="PlacementTarget"/>.
        /// </summary>
        public double VerticalOffset
        {
            get { return GetValue(VerticalOffsetProperty); }
            set { SetValue(VerticalOffsetProperty, value); }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the popup should stay open when the popup is
        /// pressed or loses focus.
        /// </summary>
        [Obsolete("Use IsLightDismissEnabled")]
        public bool StaysOpen
        {
            get => !IsLightDismissEnabled;
            set => IsLightDismissEnabled = !value;
        }

        /// <summary>
        /// Gets or sets whether this popup appears on top of all other windows
        /// </summary>
        public bool Topmost
        {
            get { return GetValue(TopmostProperty); }
            set { SetValue(TopmostProperty, value); }
        }

        /// <summary>
        /// Gets or sets a value that indicates whether the popup should be shown within the bounds of the XAML root.
        /// </summary>
        public bool ShouldConstrainToRootBounds
        {
            get => GetValue(ShouldConstrainToRootBoundsProperty);
            set => SetValue(ShouldConstrainToRootBoundsProperty, value);
        }

        /// <summary>
        /// Gets a value that indicates whether the popup is shown within the bounds of the XAML root.
        /// </summary>
        public bool IsConstrainedToRootBounds
        {
            get => _isConstrainedToRootBounds;
            private set => SetAndRaise(IsConstrainedToRootBoundsProperty, ref _isConstrainedToRootBounds, value);
        }

        /// <summary>
        /// Gets the root of the popup window.
        /// </summary>
        IVisual? IVisualTreeHost.Root => _openState?.PopupHost.HostedVisualTreeRoot;

        /// <summary>
        /// Opens the popup.
        /// </summary>
        public void Open()
        {
            // Popup is currently open
            if (_openState != null)
            {
                return;
            }

            var placementTarget = PlacementTarget ?? this.FindLogicalAncestorOfType<IControl>();

            if (placementTarget == null)
            {
                _isOpenRequested = true;
                return;
            }
            
            var topLevel = placementTarget.VisualRoot as TopLevel;

            if (topLevel == null)
            {
                _isOpenRequested = true;
                return;
            }

            _isOpenRequested = false;
            var popupHost = OverlayPopupHost.CreatePopupHost(placementTarget, DependencyResolver, ShouldConstrainToRootBounds);

            var handlerCleanup = new CompositeDisposable(5);

            void DeferCleanup(IDisposable? disposable)
            {
                if (disposable is null)
                {
                    return;
                }

                handlerCleanup.Add(disposable);
            }

            DeferCleanup(popupHost.BindConstraints(this, WidthProperty, MinWidthProperty, MaxWidthProperty,
                HeightProperty, MinHeightProperty, MaxHeightProperty, TopmostProperty));

            popupHost.SetChild(Child);
            ((ISetLogicalParent)popupHost).SetParent(this);

            popupHost.ConfigurePosition(
                placementTarget,
                PlacementMode,
                new Point(HorizontalOffset, VerticalOffset),
                PlacementAnchor,
                PlacementGravity,
                PlacementConstraintAdjustment,
                PlacementRect);

            DeferCleanup(SubscribeToEventHandler<IPopupHost, EventHandler<TemplateAppliedEventArgs>>(popupHost, RootTemplateApplied,
                (x, handler) => x.TemplateApplied += handler,
                (x, handler) => x.TemplateApplied -= handler));

            if (topLevel is Window window)
            {
                DeferCleanup(SubscribeToEventHandler<Window, EventHandler>(window, WindowDeactivated,
                    (x, handler) => x.Deactivated += handler,
                    (x, handler) => x.Deactivated -= handler));
                
                DeferCleanup(SubscribeToEventHandler<IWindowImpl, Action>(window.PlatformImpl, WindowLostFocus,
                        (x, handler) => x.LostFocus += handler,
                        (x, handler) => x.LostFocus -= handler));
            }
            else
            {
                var parentPopupRoot = topLevel as PopupRoot;

                if (parentPopupRoot?.Parent is Popup popup)
                {
                    DeferCleanup(SubscribeToEventHandler<Popup, EventHandler<EventArgs>>(popup, ParentClosed,
                        (x, handler) => x.Closed += handler,
                        (x, handler) => x.Closed -= handler));
                }
            }

            DeferCleanup(InputManager.Instance?.Process.Subscribe(ListenForNonClientClick));

            var cleanupPopup = Disposable.Create((popupHost, handlerCleanup), state =>
            {
                state.handlerCleanup.Dispose();

                state.popupHost.SetChild(null);
                state.popupHost.Hide();

                ((ISetLogicalParent)state.popupHost).SetParent(null);
                state.popupHost.Dispose();
            });

            if (IsLightDismissEnabled)
            {
                var dismissLayer = LightDismissOverlayLayer.GetLightDismissOverlayLayer(placementTarget);

                if (dismissLayer != null)
                {
                    dismissLayer.IsVisible = true;
                    dismissLayer.InputPassThroughElement = _overlayInputPassThroughElement;
                    
                    DeferCleanup(Disposable.Create(() =>
                    {
                        dismissLayer.IsVisible = false;
                        dismissLayer.InputPassThroughElement = null;
                    }));
                    
                    DeferCleanup(SubscribeToEventHandler<LightDismissOverlayLayer, EventHandler<PointerPressedEventArgs>>(
                        dismissLayer,
                        PointerPressedDismissOverlay,
                        (x, handler) => x.PointerPressed += handler,
                        (x, handler) => x.PointerPressed -= handler));
                }
            }

            _openState = new PopupOpenState(topLevel, popupHost, cleanupPopup);

            WindowManagerAddShadowHintChanged(popupHost, WindowManagerAddShadowHint);

            popupHost.Show();
            IsConstrainedToRootBounds = popupHost is OverlayPopupHost;

            using (BeginIgnoringIsOpen())
            {
                IsOpen = true;
            }

            Opened?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Closes the popup.
        /// </summary>
        public void Close() => CloseCore();

        /// <summary>
        /// Measures the control.
        /// </summary>
        /// <param name="availableSize">The available size for the control.</param>
        /// <returns>A size of 0,0 as Popup itself takes up no space.</returns>
        protected override Size MeasureCore(Size availableSize)
        {
            return new Size();
        }


        /// <inheritdoc/>
        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);
            if (_isOpenRequested)
            {
                Open();
            }
        }

        /// <inheritdoc/>
        protected override void OnDetachedFromLogicalTree(LogicalTreeAttachmentEventArgs e)
        {
            base.OnDetachedFromLogicalTree(e);
            Close();
        }

        private static IDisposable SubscribeToEventHandler<T, TEventHandler>(T target, TEventHandler handler, Action<T, TEventHandler> subscribe, Action<T, TEventHandler> unsubscribe)
        {
            subscribe(target, handler);

            return Disposable.Create((unsubscribe, target, handler), state => state.unsubscribe(state.target, state.handler));
        }

        private void WindowManagerAddShadowHintChanged(IPopupHost host, bool hint)
        {
            if(host is PopupRoot pr)
            {
                pr.PlatformImpl.SetWindowManagerAddShadowHint(hint);
            }
        }

        /// <summary>
        /// Called when the <see cref="IsOpen"/> property changes.
        /// </summary>
        /// <param name="e">The event args.</param>
        private void IsOpenChanged(AvaloniaPropertyChangedEventArgs<bool> e)
        {
            if (!_ignoreIsOpenChanged)
            {
                if (e.NewValue.Value)
                {
                    Open();
                }
                else
                {
                    Close();
                }
            }
        }

        /// <summary>
        /// Called when the <see cref="Child"/> property changes.
        /// </summary>
        /// <param name="e">The event args.</param>
        private void ChildChanged(AvaloniaPropertyChangedEventArgs e)
        {
            LogicalChildren.Clear();

            ((ISetLogicalParent?)e.OldValue)?.SetParent(null);

            if (e.NewValue != null)
            {
                ((ISetLogicalParent)e.NewValue).SetParent(this);
                LogicalChildren.Add((ILogical)e.NewValue);
            }
        }

        private void CloseCore()
        {
            _isOpenRequested = false;
            if (_openState is null)
            {
                using (BeginIgnoringIsOpen())
                {
                    IsOpen = false;
                }

                return;
            }

            _openState.Dispose();
            _openState = null;

            using (BeginIgnoringIsOpen())
            {
                IsOpen = false;
            }

            Closed?.Invoke(this, EventArgs.Empty);

            var focusCheck = FocusManager.Instance?.Current;

            // Focus is set to null as part of popup closing, so we only want to
            // set focus to PlacementTarget if this is the case
            if (focusCheck == null)
            {
                if (PlacementTarget != null)
                {
                    FocusManager.Instance?.Focus(PlacementTarget);
                }
                else
                {
                    var anc = this.FindLogicalAncestorOfType<IControl>();
                    if (anc != null)
                    {
                        FocusManager.Instance?.Focus(anc);
                    }
                }
            }
        }

        private void ListenForNonClientClick(RawInputEventArgs e)
        {
            var mouse = e as RawPointerEventArgs;

            if (IsLightDismissEnabled && mouse?.Type == RawPointerEventType.NonClientLeftButtonDown)
            {
                CloseCore();
            }
        }

        private void PointerPressedDismissOverlay(object sender, PointerPressedEventArgs e)
        {
            if (IsLightDismissEnabled && e.Source is IVisual v && !IsChildOrThis(v))
            {
                CloseCore();

                if (OverlayDismissEventPassThrough)
                {
                    PassThroughEvent(e);
                }
            }
        }

        private void PassThroughEvent(PointerPressedEventArgs e)
        {
            if (e.Source is LightDismissOverlayLayer layer &&
                layer.GetVisualRoot() is IInputElement root)
            {
                var p = e.GetCurrentPoint(root);
                var hit = root.InputHitTest(p.Position, x => x != layer);

                if (hit != null)
                {
                    e.Pointer.Capture(hit);
                    hit.RaiseEvent(e);
                    e.Handled = true;
                }
            }
        }

        private void RootTemplateApplied(object sender, TemplateAppliedEventArgs e)
        {
            if (_openState is null)
            {
                return;
            }

            var popupHost = _openState.PopupHost;

            popupHost.TemplateApplied -= RootTemplateApplied;

            _openState.SetPresenterSubscription(null);

            // If the Popup appears in a control template, then the child controls
            // that appear in the popup host need to have their TemplatedParent
            // properties set.
            if (TemplatedParent != null && popupHost.Presenter != null)
            {
                popupHost.Presenter.ApplyTemplate();

                var presenterSubscription = popupHost.Presenter.GetObservable(ContentPresenter.ChildProperty)
                    .Subscribe(SetTemplatedParentAndApplyChildTemplates);

                _openState.SetPresenterSubscription(presenterSubscription);
            }
        }

        private void SetTemplatedParentAndApplyChildTemplates(IControl control)
        {
            if (control != null)
            {
                var templatedParent = TemplatedParent;

                if (control.TemplatedParent == null)
                {
                    control.SetValue(TemplatedParentProperty, templatedParent);
                }

                control.ApplyTemplate();

                if (!(control is IPresenter) && control.TemplatedParent == templatedParent)
                {
                    foreach (IControl child in control.VisualChildren)
                    {
                        SetTemplatedParentAndApplyChildTemplates(child);
                    }
                }
            }
        }

        private bool IsChildOrThis(IVisual child)
        {
            if (_openState is null)
            {
                return false;
            }

            var popupHost = _openState.PopupHost;

            IVisual? root = child.VisualRoot;
            
            while (root is IHostedVisualTreeRoot hostedRoot)
            {
                if (root == popupHost)
                {
                    return true;
                }

                root = hostedRoot.Host?.VisualRoot;
            }

            return false;
        }
        
        public bool IsInsidePopup(IVisual visual)
        {
            if (_openState is null)
            {
                return false;
            }

            var popupHost = _openState.PopupHost;

            return popupHost != null && ((IVisual)popupHost).IsVisualAncestorOf(visual);
        }

        public bool IsPointerOverPopup => ((IInputElement?)_openState?.PopupHost)?.IsPointerOver ?? false;

        private void WindowDeactivated(object sender, EventArgs e)
        {
            if (IsLightDismissEnabled)
            {
                Close();
            }
        }

        private void ParentClosed(object sender, EventArgs e)
        {
            if (IsLightDismissEnabled)
            {
                Close();
            }
        }
        
        private void WindowLostFocus()
        {
            if (IsLightDismissEnabled)
                Close();
        }

        private IgnoreIsOpenScope BeginIgnoringIsOpen()
        {
            return new IgnoreIsOpenScope(this);
        }

        private readonly struct IgnoreIsOpenScope : IDisposable
        {
            private readonly Popup _owner;

            public IgnoreIsOpenScope(Popup owner)
            {
                _owner = owner;
                _owner._ignoreIsOpenChanged = true;
            }

            public void Dispose()
            {
                _owner._ignoreIsOpenChanged = false;
            }
        }

        private class PopupOpenState : IDisposable
        {
            private readonly IDisposable _cleanup;
            private IDisposable? _presenterCleanup;

            public PopupOpenState(TopLevel topLevel, IPopupHost popupHost, IDisposable cleanup)
            {
                TopLevel = topLevel;
                PopupHost = popupHost;
                _cleanup = cleanup;
            }

            public TopLevel TopLevel { get; }

            public IPopupHost PopupHost { get; }

            public void SetPresenterSubscription(IDisposable? presenterCleanup)
            {
                _presenterCleanup?.Dispose();

                _presenterCleanup = presenterCleanup;
            }

            public void Dispose()
            {
                _presenterCleanup?.Dispose();

                _cleanup.Dispose();
            }
        }
    }
}
