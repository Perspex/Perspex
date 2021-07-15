using System;
using System.Reactive.Linq;
using Avalonia.Controls.Platform;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.Input.TextInput;
using Avalonia.Layout;
using Avalonia.Logging;
using Avalonia.LogicalTree;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Rendering;
using Avalonia.Styling;
using Avalonia.Utilities;
using Avalonia.VisualTree;
using JetBrains.Annotations;

namespace Avalonia.Controls
{
    /// <summary>
    /// Base class for top-level widgets.
    /// </summary>
    /// <remarks>
    /// This class acts as a base for top level widget.
    /// It handles scheduling layout, styling and rendering as well as
    /// tracking the widget's <see cref="ClientSize"/>.
    /// </remarks>
    public abstract class TopLevel : ContentControl,
        IInputRoot,
        ILayoutRoot,
        IRenderRoot,
        ICloseable,
        IStyleHost,
        ILogicalRoot,
        ITextInputMethodRoot,
        IWeakSubscriber<ResourcesChangedEventArgs>
    {
        /// <summary>
        /// Defines the <see cref="ClientSize"/> property.
        /// </summary>
        public static readonly DirectProperty<TopLevel, Size> ClientSizeProperty =
            AvaloniaProperty.RegisterDirect<TopLevel, Size>(nameof(ClientSize), o => o.ClientSize);

        /// <summary>
        /// Defines the <see cref="FrameSize"/> property.
        /// </summary>
        public static readonly DirectProperty<TopLevel, Size?> FrameSizeProperty =
            AvaloniaProperty.RegisterDirect<TopLevel, Size?>(nameof(FrameSize), o => o.FrameSize);

        /// <summary>
        /// Defines the <see cref="IInputRoot.PointerOverElement"/> property.
        /// </summary>
        public static readonly StyledProperty<IInputElement> PointerOverElementProperty =
            AvaloniaProperty.Register<TopLevel, IInputElement>(nameof(IInputRoot.PointerOverElement));

        /// <summary>
        /// Defines the <see cref="TransparencyLevelHint"/> property.
        /// </summary>
        public static readonly StyledProperty<WindowTransparencyLevel> TransparencyLevelHintProperty =
            AvaloniaProperty.Register<TopLevel, WindowTransparencyLevel>(nameof(TransparencyLevelHint), WindowTransparencyLevel.None);

        /// <summary>
        /// Defines the <see cref="ActualTransparencyLevel"/> property.
        /// </summary>
        public static readonly DirectProperty<TopLevel, WindowTransparencyLevel> ActualTransparencyLevelProperty =
            AvaloniaProperty.RegisterDirect<TopLevel, WindowTransparencyLevel>(nameof(ActualTransparencyLevel), 
                o => o.ActualTransparencyLevel, 
                unsetValue: WindowTransparencyLevel.None);        

        /// <summary>
        /// Defines the <see cref="TransparencyBackgroundFallbackProperty"/> property.
        /// </summary>
        public static readonly StyledProperty<IBrush> TransparencyBackgroundFallbackProperty =
            AvaloniaProperty.Register<TopLevel, IBrush>(nameof(TransparencyBackgroundFallback), Brushes.White);

        private readonly IInputManager _inputManager;
        private readonly IAccessKeyHandler _accessKeyHandler;
        private readonly IKeyboardNavigationHandler _keyboardNavigationHandler;
        private readonly IPlatformRenderInterface _renderInterface;
        private readonly IGlobalStyles _globalStyles;
        private Size _clientSize;
        private Size? _frameSize;
        private WindowTransparencyLevel _actualTransparencyLevel;
        private ILayoutManager _layoutManager;
        private Border _transparencyFallbackBorder;

        /// <summary>
        /// Initializes static members of the <see cref="TopLevel"/> class.
        /// </summary>
        static TopLevel()
        {
            KeyboardNavigation.TabNavigationProperty.OverrideDefaultValue<TopLevel>(KeyboardNavigationMode.Cycle);
            AffectsMeasure<TopLevel>(ClientSizeProperty);

            TransparencyLevelHintProperty.Changed.AddClassHandler<TopLevel>(
                (tl, e) => 
                {
                    if (tl.PlatformImpl != null)
                    {
                        tl.PlatformImpl.SetTransparencyLevelHint((WindowTransparencyLevel)e.NewValue);
                        tl.HandleTransparencyLevelChanged(tl.PlatformImpl.TransparencyLevel);
                    }
                });
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TopLevel"/> class.
        /// </summary>
        /// <param name="impl">The platform-specific window implementation.</param>
        public TopLevel(ITopLevelImpl impl)
            : this(impl, AvaloniaLocator.Current)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TopLevel"/> class.
        /// </summary>
        /// <param name="impl">The platform-specific window implementation.</param>
        /// <param name="dependencyResolver">
        /// The dependency resolver to use. If null the default dependency resolver will be used.
        /// </param>
        public TopLevel(ITopLevelImpl impl, IAvaloniaDependencyResolver dependencyResolver)
        {
            if (impl == null)
            {
                throw new InvalidOperationException(
                    "Could not create window implementation: maybe no windowing subsystem was initialized?");
            }

            PlatformImpl = impl;

            _actualTransparencyLevel = PlatformImpl.TransparencyLevel;            

            dependencyResolver = dependencyResolver ?? AvaloniaLocator.Current;
            var styler = TryGetService<IStyler>(dependencyResolver);

            _accessKeyHandler = TryGetService<IAccessKeyHandler>(dependencyResolver);
            _inputManager = TryGetService<IInputManager>(dependencyResolver);
            _keyboardNavigationHandler = TryGetService<IKeyboardNavigationHandler>(dependencyResolver);
            _renderInterface = TryGetService<IPlatformRenderInterface>(dependencyResolver);
            _globalStyles = TryGetService<IGlobalStyles>(dependencyResolver);

            Renderer = impl.CreateRenderer(this);

            if (Renderer != null)
            {
                Renderer.SceneInvalidated += SceneInvalidated;
            }

            impl.SetInputRoot(this);

            impl.Closed = HandleClosed;
            impl.Input = HandleInput;
            impl.Paint = HandlePaint;
            impl.Resized = HandleResized;
            impl.ScalingChanged = HandleScalingChanged;
            impl.TransparencyLevelChanged = HandleTransparencyLevelChanged;

            _keyboardNavigationHandler?.SetOwner(this);
            _accessKeyHandler?.SetOwner(this);

            if (_globalStyles is object)
            {
                _globalStyles.GlobalStylesAdded += ((IStyleHost)this).StylesAdded;
                _globalStyles.GlobalStylesRemoved += ((IStyleHost)this).StylesRemoved;
            }

            styler?.ApplyStyles(this);

            ClientSize = impl.ClientSize;
            FrameSize = impl.FrameSize;
            
            this.GetObservable(PointerOverElementProperty)
                .Select(
                    x => (x as InputElement)?.GetObservable(CursorProperty) ?? Observable.Empty<Cursor>())
                .Switch().Subscribe(cursor => PlatformImpl?.SetCursor(cursor?.PlatformImpl));

            if (((IStyleHost)this).StylingParent is IResourceHost applicationResources)
            {
                WeakSubscriptionManager.Subscribe(
                    applicationResources,
                    nameof(IResourceHost.ResourcesChanged),
                    this);
            }

            impl.LostFocus += PlatformImpl_LostFocus;
        }

        /// <summary>
        /// Fired when the window is opened.
        /// </summary>
        public event EventHandler Opened;

        /// <summary>
        /// Fired when the window is closed.
        /// </summary>
        public event EventHandler Closed;

        /// <summary>
        /// Gets or sets the client size of the window.
        /// </summary>
        public Size ClientSize
        {
            get { return _clientSize; }
            protected set { SetAndRaise(ClientSizeProperty, ref _clientSize, value); }
        }

        /// <summary>
        /// Gets or sets the total size of the window.
        /// </summary>
        public Size? FrameSize
        {
            get { return _frameSize; }
            protected set { SetAndRaise(FrameSizeProperty, ref _frameSize, value); }
        }

        /// <summary>
        /// Gets or sets the <see cref="WindowTransparencyLevel"/> that the TopLevel should use when possible.
        /// </summary>
        public WindowTransparencyLevel TransparencyLevelHint
        {
            get { return GetValue(TransparencyLevelHintProperty); }
            set { SetValue(TransparencyLevelHintProperty, value); }
        }

        /// <summary>
        /// Gets the acheived <see cref="WindowTransparencyLevel"/> that the platform was able to provide.
        /// </summary>
        public WindowTransparencyLevel ActualTransparencyLevel
        {
            get => _actualTransparencyLevel;
            private set => SetAndRaise(ActualTransparencyLevelProperty, ref _actualTransparencyLevel, value);
        }        

        /// <summary>
        /// Gets or sets the <see cref="IBrush"/> that transparency will blend with when transparency is not supported.
        /// By default this is a solid white brush.
        /// </summary>
        public IBrush TransparencyBackgroundFallback
        {
            get => GetValue(TransparencyBackgroundFallbackProperty);
            set => SetValue(TransparencyBackgroundFallbackProperty, value);
        }

        public ILayoutManager LayoutManager
        {
            get
            {
                if (_layoutManager == null)
                    _layoutManager = CreateLayoutManager();
                return _layoutManager;
            }
        }

        /// <summary>
        /// Gets the platform-specific window implementation.
        /// </summary>
        [CanBeNull]
        public ITopLevelImpl PlatformImpl { get; private set; }
        
        /// <summary>
        /// Gets the renderer for the window.
        /// </summary>
        public IRenderer Renderer { get; private set; }

        /// <summary>
        /// Gets the access key handler for the window.
        /// </summary>
        IAccessKeyHandler IInputRoot.AccessKeyHandler => _accessKeyHandler;

        /// <summary>
        /// Gets or sets the keyboard navigation handler for the window.
        /// </summary>
        IKeyboardNavigationHandler IInputRoot.KeyboardNavigationHandler => _keyboardNavigationHandler;

        /// <summary>
        /// Gets or sets the input element that the pointer is currently over.
        /// </summary>
        IInputElement IInputRoot.PointerOverElement
        {
            get { return GetValue(PointerOverElementProperty); }
            set { SetValue(PointerOverElementProperty, value); }
        }

        /// <inheritdoc/>
        IMouseDevice IInputRoot.MouseDevice => PlatformImpl?.MouseDevice;

        void IWeakSubscriber<ResourcesChangedEventArgs>.OnEvent(object sender, ResourcesChangedEventArgs e)
        {
            ((ILogical)this).NotifyResourcesChanged(e);
        }

        /// <summary>
        /// Gets or sets a value indicating whether access keys are shown in the window.
        /// </summary>
        bool IInputRoot.ShowAccessKeys
        {
            get { return GetValue(AccessText.ShowAccessKeyProperty); }
            set { SetValue(AccessText.ShowAccessKeyProperty, value); }
        }

        /// <inheritdoc/>
        double ILayoutRoot.LayoutScaling => PlatformImpl?.RenderScaling ?? 1;

        /// <inheritdoc/>
        double IRenderRoot.RenderScaling => PlatformImpl?.RenderScaling ?? 1;

        IStyleHost IStyleHost.StylingParent => _globalStyles;

        IRenderTarget IRenderRoot.CreateRenderTarget() => CreateRenderTarget();

        /// <inheritdoc/>
        protected virtual IRenderTarget CreateRenderTarget()
        {
            if(PlatformImpl == null)
                throw new InvalidOperationException("Can't create render target, PlatformImpl is null (might be already disposed)");
            return _renderInterface.CreateRenderTarget(PlatformImpl.Surfaces);
        }

        /// <inheritdoc/>
        void IRenderRoot.Invalidate(Rect rect)
        {
            PlatformImpl?.Invalidate(rect);
        }
        
        /// <inheritdoc/>
        Point IRenderRoot.PointToClient(PixelPoint p)
        {
            return PlatformImpl?.PointToClient(p) ?? default;
        }

        /// <inheritdoc/>
        PixelPoint IRenderRoot.PointToScreen(Point p)
        {
            return PlatformImpl?.PointToScreen(p) ?? default;
        }
        
        /// <summary>
        /// Creates the layout manager for this <see cref="TopLevel" />.
        /// </summary>
        protected virtual ILayoutManager CreateLayoutManager() => new LayoutManager(this);

        /// <summary>
        /// Handles a paint notification from <see cref="ITopLevelImpl.Resized"/>.
        /// </summary>
        /// <param name="rect">The dirty area.</param>
        protected virtual void HandlePaint(Rect rect)
        {
            Renderer?.Paint(rect);
        }

        /// <summary>
        /// Handles a closed notification from <see cref="ITopLevelImpl.Closed"/>.
        /// </summary>
        protected virtual void HandleClosed()
        {
            if (_globalStyles is object)
            {
                _globalStyles.GlobalStylesAdded -= ((IStyleHost)this).StylesAdded;
                _globalStyles.GlobalStylesRemoved -= ((IStyleHost)this).StylesRemoved;
            }

            Renderer?.Dispose();
            Renderer = null;
            
            var logicalArgs = new LogicalTreeAttachmentEventArgs(this, this, null);
            ((ILogical)this).NotifyDetachedFromLogicalTree(logicalArgs);

            var visualArgs = new VisualTreeAttachmentEventArgs(this, this);
            OnDetachedFromVisualTreeCore(visualArgs);

            (this as IInputRoot).MouseDevice?.TopLevelClosed(this);
            PlatformImpl = null;
            OnClosed(EventArgs.Empty);

            LayoutManager?.Dispose();
        }

        /// <summary>
        /// Handles a resize notification from <see cref="ITopLevelImpl.Resized"/>.
        /// </summary>
        /// <param name="clientSize">The new client size.</param>
        protected virtual void HandleResized(Size clientSize)
        {
            ClientSize = clientSize;
            FrameSize = PlatformImpl.FrameSize;
            Width = clientSize.Width;
            Height = clientSize.Height;
            LayoutManager.ExecuteLayoutPass();
            Renderer?.Resized(clientSize);
        }

        /// <summary>
        /// Handles a window scaling change notification from 
        /// <see cref="ITopLevelImpl.ScalingChanged"/>.
        /// </summary>
        /// <param name="scaling">The window scaling.</param>
        protected virtual void HandleScalingChanged(double scaling)
        {
            LayoutHelper.InvalidateSelfAndChildrenMeasure(this);
        }

        private bool TransparencyLevelsMatch (WindowTransparencyLevel requested, WindowTransparencyLevel received)
        {
            if(requested == received)
            {
                return true;
            }
            else if(requested >= WindowTransparencyLevel.Blur && received >= WindowTransparencyLevel.Blur)
            {
                return true;
            }

            return false;
        }

        protected virtual void HandleTransparencyLevelChanged(WindowTransparencyLevel transparencyLevel)
        {
            if(_transparencyFallbackBorder != null)
            {
                if(transparencyLevel == WindowTransparencyLevel.None || 
                    TransparencyLevelHint == WindowTransparencyLevel.None || 
                    !TransparencyLevelsMatch(TransparencyLevelHint, transparencyLevel))
                {
                    _transparencyFallbackBorder.Background = TransparencyBackgroundFallback;
                }
                else
                {
                    _transparencyFallbackBorder.Background = null;
                }
            }

            ActualTransparencyLevel = transparencyLevel;
        }

        /// <inheritdoc/>
        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);

            throw new InvalidOperationException(
                $"Control '{GetType().Name}' is a top level control and cannot be added as a child.");
        }

        protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
        {
            base.OnApplyTemplate(e);

            _transparencyFallbackBorder = e.NameScope.Find<Border>("PART_TransparencyFallback");

            HandleTransparencyLevelChanged(PlatformImpl.TransparencyLevel);
        }

        /// <summary>
        /// Raises the <see cref="Opened"/> event.
        /// </summary>
        /// <param name="e">The event args.</param>
        protected virtual void OnOpened(EventArgs e) => Opened?.Invoke(this, e);

        /// <summary>
        /// Raises the <see cref="Closed"/> event.
        /// </summary>
        /// <param name="e">The event args.</param>
        protected virtual void OnClosed(EventArgs e) => Closed?.Invoke(this, e);

        /// <summary>
        /// Tries to get a service from an <see cref="IAvaloniaDependencyResolver"/>, logging a
        /// warning if not found.
        /// </summary>
        /// <typeparam name="T">The service type.</typeparam>
        /// <param name="resolver">The resolver.</param>
        /// <returns>The service.</returns>
        private T TryGetService<T>(IAvaloniaDependencyResolver resolver) where T : class
        {
            var result = resolver.GetService<T>();

            if (result == null)
            {
                Logger.TryGet(LogEventLevel.Warning, LogArea.Control)?.Log(
                    this,
                    "Could not create {Service} : maybe Application.RegisterServices() wasn't called?",
                    typeof(T));
            }

            return result;
        }

        /// <summary>
        /// Handles input from <see cref="ITopLevelImpl.Input"/>.
        /// </summary>
        /// <param name="e">The event args.</param>
        private void HandleInput(RawInputEventArgs e)
        {
            _inputManager.ProcessInput(e);
        }

        private void SceneInvalidated(object sender, SceneInvalidatedEventArgs e)
        {
            (this as IInputRoot).MouseDevice.SceneInvalidated(this, e.DirtyRect);
        }

        void PlatformImpl_LostFocus()
        {
            var focused = (IVisual)FocusManager.Instance.Current;
            if (focused == null)
                return;
            while (focused.VisualParent != null)
                focused = focused.VisualParent;

            if (focused == this)
                KeyboardDevice.Instance.SetFocusedElement(null, NavigationMethod.Unspecified, KeyModifiers.None);
        }

        ITextInputMethodImpl ITextInputMethodRoot.InputMethod =>
            (PlatformImpl as ITopLevelImplWithTextInputMethod)?.TextInputMethod;
    }
}
