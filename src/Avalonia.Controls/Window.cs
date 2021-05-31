using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Platform;
using Avalonia.Controls.Platform;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Styling;
using JetBrains.Annotations;

namespace Avalonia.Controls
{
    /// <summary>
    /// Determines how a <see cref="Window"/> will size itself to fit its content.
    /// </summary>
    [Flags]
    public enum SizeToContent
    {
        /// <summary>
        /// The window will not automatically size itself to fit its content.
        /// </summary>
        Manual = 0,

        /// <summary>
        /// The window will size itself horizontally to fit its content.
        /// </summary>
        Width = 1,

        /// <summary>
        /// The window will size itself vertically to fit its content.
        /// </summary>
        Height = 2,

        /// <summary>
        /// The window will size itself horizontally and vertically to fit its content.
        /// </summary>
        WidthAndHeight = 3,
    }

    /// <summary>
    /// Determines system decorations (title bar, border, etc) for a <see cref="Window"/>
    /// </summary>
    public enum SystemDecorations
    {
        /// <summary>
        /// No decorations
        /// </summary>
        None = 0,

        /// <summary>
        /// Window border without titlebar
        /// </summary>
        BorderOnly = 1,

        /// <summary>
        /// Fully decorated (default)
        /// </summary>
        Full = 2
    }

    /// <summary>
    /// A top-level window.
    /// </summary>
    public class Window : WindowBase, IStyleable, IFocusScope, ILayoutRoot
    {
        private readonly List<(Window child, bool isDialog)> _children = new List<(Window, bool)>();
        private bool _isExtendedIntoWindowDecorations;
        private Thickness _windowDecorationMargin;
        private Thickness _offScreenMargin;

        /// <summary>
        /// Defines the <see cref="SizeToContent"/> property.
        /// </summary>
        public static readonly StyledProperty<SizeToContent> SizeToContentProperty =
            AvaloniaProperty.Register<Window, SizeToContent>(nameof(SizeToContent));

        /// <summary>
        /// Enables or disables system window decorations (title bar, buttons, etc)
        /// </summary>
        [Obsolete("Use SystemDecorationsProperty instead")]
        public static readonly DirectProperty<Window, bool> HasSystemDecorationsProperty =
            AvaloniaProperty.RegisterDirect<Window, bool>(
                nameof(HasSystemDecorations),
                o => o.HasSystemDecorations,
                (o, v) => o.HasSystemDecorations = v);

        /// <summary>
        /// Defines the <see cref="ExtendClientAreaToDecorationsHint"/> property.
        /// </summary>
        public static readonly StyledProperty<bool> ExtendClientAreaToDecorationsHintProperty =
            AvaloniaProperty.Register<Window, bool>(nameof(ExtendClientAreaToDecorationsHint), false);

        public static readonly StyledProperty<ExtendClientAreaChromeHints> ExtendClientAreaChromeHintsProperty =
            AvaloniaProperty.Register<Window, ExtendClientAreaChromeHints>(nameof(ExtendClientAreaChromeHints), ExtendClientAreaChromeHints.Default);

        public static readonly StyledProperty<double> ExtendClientAreaTitleBarHeightHintProperty =
            AvaloniaProperty.Register<Window, double>(nameof(ExtendClientAreaTitleBarHeightHint), -1);

        /// <summary>
        /// Defines the <see cref="IsExtendedIntoWindowDecorations"/> property.
        /// </summary>
        public static readonly DirectProperty<Window, bool> IsExtendedIntoWindowDecorationsProperty =
            AvaloniaProperty.RegisterDirect<Window, bool>(nameof(IsExtendedIntoWindowDecorations),
                o => o.IsExtendedIntoWindowDecorations,
                unsetValue: false);

        /// <summary>
        /// Defines the <see cref="WindowDecorationMargin"/> property.
        /// </summary>
        public static readonly DirectProperty<Window, Thickness> WindowDecorationMarginProperty =
            AvaloniaProperty.RegisterDirect<Window, Thickness>(nameof(WindowDecorationMargin),
                o => o.WindowDecorationMargin);

        public static readonly DirectProperty<Window, Thickness> OffScreenMarginProperty =
            AvaloniaProperty.RegisterDirect<Window, Thickness>(nameof(OffScreenMargin),
                o => o.OffScreenMargin);

        /// <summary>
        /// Defines the <see cref="SystemDecorations"/> property.
        /// </summary>
        public static readonly StyledProperty<SystemDecorations> SystemDecorationsProperty =
            AvaloniaProperty.Register<Window, SystemDecorations>(nameof(SystemDecorations), SystemDecorations.Full);

        /// <summary>
        /// Defines the <see cref="ShowActivated"/> property.
        /// </summary>
        public static readonly StyledProperty<bool> ShowActivatedProperty =
            AvaloniaProperty.Register<Window, bool>(nameof(ShowActivated), true);

        /// <summary>
        /// Enables or disables the taskbar icon
        /// </summary>
        public static readonly StyledProperty<bool> ShowInTaskbarProperty =
            AvaloniaProperty.Register<Window, bool>(nameof(ShowInTaskbar), true);

        /// <summary>
        /// Represents the current window state (normal, minimized, maximized)
        /// </summary>
        public static readonly StyledProperty<WindowState> WindowStateProperty =
            AvaloniaProperty.Register<Window, WindowState>(nameof(WindowState));

        /// <summary>
        /// Defines the <see cref="Title"/> property.
        /// </summary>
        public static readonly StyledProperty<string> TitleProperty =
            AvaloniaProperty.Register<Window, string>(nameof(Title), "Window");

        /// <summary>
        /// Defines the <see cref="Icon"/> property.
        /// </summary>
        public static readonly StyledProperty<WindowIcon> IconProperty =
            AvaloniaProperty.Register<Window, WindowIcon>(nameof(Icon));

        /// <summary>
        /// Defines the <see cref="WindowStartupLocation"/> property.
        /// </summary>
        public static readonly DirectProperty<Window, WindowStartupLocation> WindowStartupLocationProperty =
            AvaloniaProperty.RegisterDirect<Window, WindowStartupLocation>(
                nameof(WindowStartupLocation),
                o => o.WindowStartupLocation,
                (o, v) => o.WindowStartupLocation = v);

        public static readonly StyledProperty<bool> CanResizeProperty =
            AvaloniaProperty.Register<Window, bool>(nameof(CanResize), true);

        /// <summary>
        /// Routed event that can be used for global tracking of window destruction
        /// </summary>
        public static readonly RoutedEvent WindowClosedEvent =
            RoutedEvent.Register<Window, RoutedEventArgs>("WindowClosed", RoutingStrategies.Direct);

        /// <summary>
        /// Routed event that can be used for global tracking of opening windows
        /// </summary>
        public static readonly RoutedEvent WindowOpenedEvent =
            RoutedEvent.Register<Window, RoutedEventArgs>("WindowOpened", RoutingStrategies.Direct);



        private readonly NameScope _nameScope = new NameScope();
        private object _dialogResult;
        private readonly Size _maxPlatformClientSize;
        private WindowStartupLocation _windowStartupLocation;

        /// <summary>
        /// Initializes static members of the <see cref="Window"/> class.
        /// </summary>
        static Window()
        {
            BackgroundProperty.OverrideDefaultValue(typeof(Window), Brushes.White);
            TitleProperty.Changed.AddClassHandler<Window>((s, e) => s.PlatformImpl?.SetTitle((string)e.NewValue));
            ShowInTaskbarProperty.Changed.AddClassHandler<Window>((w, e) => w.PlatformImpl?.ShowTaskbarIcon((bool)e.NewValue));

            IconProperty.Changed.AddClassHandler<Window>((s, e) => s.PlatformImpl?.SetIcon(((WindowIcon)e.NewValue)?.PlatformImpl));

            CanResizeProperty.Changed.AddClassHandler<Window>((w, e) => w.PlatformImpl?.CanResize((bool)e.NewValue));

            WindowStateProperty.Changed.AddClassHandler<Window>(
                (w, e) => { if (w.PlatformImpl != null) w.PlatformImpl.WindowState = (WindowState)e.NewValue; });

            ExtendClientAreaToDecorationsHintProperty.Changed.AddClassHandler<Window>(
                (w, e) => { if (w.PlatformImpl != null) w.PlatformImpl.SetExtendClientAreaToDecorationsHint((bool)e.NewValue); });

            ExtendClientAreaChromeHintsProperty.Changed.AddClassHandler<Window>(
                (w, e) =>
                {
                    if (w.PlatformImpl != null)
                    {
                        w.PlatformImpl.SetExtendClientAreaChromeHints((ExtendClientAreaChromeHints)e.NewValue);
                    }
                });

            ExtendClientAreaTitleBarHeightHintProperty.Changed.AddClassHandler<Window>(
                (w, e) => { if (w.PlatformImpl != null) w.PlatformImpl.SetExtendClientAreaTitleBarHeightHint((double)e.NewValue); });

            MinWidthProperty.Changed.AddClassHandler<Window>((w, e) => w.PlatformImpl?.SetMinMaxSize(new Size((double)e.NewValue, w.MinHeight), new Size(w.MaxWidth, w.MaxHeight)));
            MinHeightProperty.Changed.AddClassHandler<Window>((w, e) => w.PlatformImpl?.SetMinMaxSize(new Size(w.MinWidth, (double)e.NewValue), new Size(w.MaxWidth, w.MaxHeight)));
            MaxWidthProperty.Changed.AddClassHandler<Window>((w, e) => w.PlatformImpl?.SetMinMaxSize(new Size(w.MinWidth, w.MinHeight), new Size((double)e.NewValue, w.MaxHeight)));
            MaxHeightProperty.Changed.AddClassHandler<Window>((w, e) => w.PlatformImpl?.SetMinMaxSize(new Size(w.MinWidth, w.MinHeight), new Size(w.MaxWidth, (double)e.NewValue)));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Window"/> class.
        /// </summary>
        public Window()
            : this(PlatformManager.CreateWindow())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Window"/> class.
        /// </summary>
        /// <param name="impl">The window implementation.</param>
        public Window(IWindowImpl impl)
            : base(impl)
        {
            impl.Closing = HandleClosing;
            impl.GotInputWhenDisabled = OnGotInputWhenDisabled;
            impl.WindowStateChanged = HandleWindowStateChanged;
            _maxPlatformClientSize = PlatformImpl?.MaxAutoSizeHint ?? default(Size);
            impl.ExtendClientAreaToDecorationsChanged = ExtendClientAreaToDecorationsChanged;            
            this.GetObservable(ClientSizeProperty).Skip(1).Subscribe(x => PlatformImpl?.Resize(x));

            PlatformImpl?.ShowTaskbarIcon(ShowInTaskbar);
        }

        /// <summary>
        /// Gets the platform-specific window implementation.
        /// </summary>
        [CanBeNull]
        public new IWindowImpl PlatformImpl => (IWindowImpl)base.PlatformImpl;

        /// <summary>
        /// Gets or sets a value indicating how the window will size itself to fit its content.
        /// </summary>
        public SizeToContent SizeToContent
        {
            get { return GetValue(SizeToContentProperty); }
            set { SetValue(SizeToContentProperty, value); }
        }

        /// <summary>
        /// Gets or sets the title of the window.
        /// </summary>
        public string Title
        {
            get { return GetValue(TitleProperty); }
            set { SetValue(TitleProperty, value); }
        }

        /// <summary>
        /// Enables or disables system window decorations (title bar, buttons, etc)
        /// </summary>
        [Obsolete("Use SystemDecorations instead")]
        public bool HasSystemDecorations
        {
            get => SystemDecorations == SystemDecorations.Full;
            set
            {
                var oldValue = HasSystemDecorations;

                if (oldValue != value)
                {
                    SystemDecorations = value ? SystemDecorations.Full : SystemDecorations.None;
                    RaisePropertyChanged(HasSystemDecorationsProperty, oldValue, value);
                }
            }
        }

        /// <summary>
        /// Gets or sets if the ClientArea is Extended into the Window Decorations (chrome or border).
        /// </summary>
        public bool ExtendClientAreaToDecorationsHint
        {
            get { return GetValue(ExtendClientAreaToDecorationsHintProperty); }
            set { SetValue(ExtendClientAreaToDecorationsHintProperty, value); }
        }

        /// <summary>
        /// Gets or Sets the <see cref="Avalonia.Platform.ExtendClientAreaChromeHints"/> that control
        /// how the chrome looks when the client area is extended.
        /// </summary>
        public ExtendClientAreaChromeHints ExtendClientAreaChromeHints
        {
            get => GetValue(ExtendClientAreaChromeHintsProperty);
            set => SetValue(ExtendClientAreaChromeHintsProperty, value);
        }

        /// <summary>
        /// Gets or Sets the TitlebarHeightHint for when the client area is extended.
        /// A value of -1 will cause the titlebar to be auto sized to the OS default.
        /// Any other positive value will cause the titlebar to assume that height.
        /// </summary>
        public double ExtendClientAreaTitleBarHeightHint
        {
            get => GetValue(ExtendClientAreaTitleBarHeightHintProperty);
            set => SetValue(ExtendClientAreaTitleBarHeightHintProperty, value);
        }        

        /// <summary>
        /// Gets if the ClientArea is Extended into the Window Decorations.
        /// </summary>
        public bool IsExtendedIntoWindowDecorations
        {
            get => _isExtendedIntoWindowDecorations;
            private set => SetAndRaise(IsExtendedIntoWindowDecorationsProperty, ref _isExtendedIntoWindowDecorations, value);
        }        

        /// <summary>
        /// Gets the WindowDecorationMargin.
        /// This tells you the thickness around the window that is used by borders and the titlebar.
        /// </summary>
        public Thickness WindowDecorationMargin
        {
            get => _windowDecorationMargin;
            private set => SetAndRaise(WindowDecorationMarginProperty, ref _windowDecorationMargin, value);
        }        

        /// <summary>
        /// Gets the window margin that is hidden off the screen area.
        /// This is generally only the case on Windows when in Maximized where the window border
        /// is hidden off the screen. This Margin may be used to ensure user content doesnt overlap this space.
        /// </summary>
        public Thickness OffScreenMargin
        {
            get => _offScreenMargin;
            private set => SetAndRaise(OffScreenMarginProperty, ref _offScreenMargin, value);
        }

        /// <summary>
        /// Sets the system decorations (title bar, border, etc)
        /// </summary>
        public SystemDecorations SystemDecorations
        {
            get { return GetValue(SystemDecorationsProperty); }
            set { SetValue(SystemDecorationsProperty, value); }
        }

        /// <summary>
        /// Gets or sets a value that indicates whether a window is activated when first shown. 
        /// </summary>
        public bool ShowActivated
        {
            get { return GetValue(ShowActivatedProperty); }
            set { SetValue(ShowActivatedProperty, value); }
        }

        /// <summary>
        /// Enables or disables the taskbar icon
        /// </summary>
        /// 
        public bool ShowInTaskbar
        {
            get { return GetValue(ShowInTaskbarProperty); }
            set { SetValue(ShowInTaskbarProperty, value); }
        }

        /// <summary>
        /// Gets or sets the minimized/maximized state of the window.
        /// </summary>
        public WindowState WindowState
        {
            get { return GetValue(WindowStateProperty); }
            set { SetValue(WindowStateProperty, value); }
        }

        /// <summary>
        /// Enables or disables resizing of the window.
        /// Note that if <see cref="HasSystemDecorations"/> is set to False then this property
        /// has no effect and should be treated as a recommendation for the user setting HasSystemDecorations.
        /// </summary>
        public bool CanResize
        {
            get { return GetValue(CanResizeProperty); }
            set { SetValue(CanResizeProperty, value); }
        }

        /// <summary>
        /// Gets or sets the icon of the window.
        /// </summary>
        public WindowIcon Icon
        {
            get { return GetValue(IconProperty); }
            set { SetValue(IconProperty, value); }
        }

        /// <summary>
        /// Gets or sets the startup location of the window.
        /// </summary>
        public WindowStartupLocation WindowStartupLocation
        {
            get { return _windowStartupLocation; }
            set { SetAndRaise(WindowStartupLocationProperty, ref _windowStartupLocation, value); }
        }

        /// <summary>
        /// Gets or sets the window position in screen coordinates.
        /// </summary>
        public PixelPoint Position
        {
            get { return PlatformImpl?.Position ?? PixelPoint.Origin; }
            set
            {
                PlatformImpl?.Move(value);
            }
        }

        /// <summary>
        /// Starts moving a window with left button being held. Should be called from left mouse button press event handler
        /// </summary>
        public void BeginMoveDrag(PointerPressedEventArgs e) => PlatformImpl?.BeginMoveDrag(e);

        /// <summary>
        /// Starts resizing a window. This function is used if an application has window resizing controls. 
        /// Should be called from left mouse button press event handler
        /// </summary>
        public void BeginResizeDrag(WindowEdge edge, PointerPressedEventArgs e) => PlatformImpl?.BeginResizeDrag(edge, e);

        /// <inheritdoc/>
        Type IStyleable.StyleKey => typeof(Window);

        /// <summary>
        /// Fired before a window is closed.
        /// </summary>
        public event EventHandler<CancelEventArgs> Closing;

        /// <summary>
        /// Closes the window.
        /// </summary>
        public void Close()
        {
            Close(false);
        }

        /// <summary>
        /// Closes a dialog window with the specified result.
        /// </summary>
        /// <param name="dialogResult">The dialog result.</param>
        /// <remarks>
        /// When the window is shown with the <see cref="ShowDialog{TResult}(Window)"/>
        /// or <see cref="ShowDialog{TResult}(Window)"/> method, the
        /// resulting task will produce the <see cref="_dialogResult"/> value when the window
        /// is closed.
        /// </remarks>
        public void Close(object dialogResult)
        {
            _dialogResult = dialogResult;
            Close(false);
        }

        internal void Close(bool ignoreCancel)
        {
            bool close = true;

            try
            {
                if (!ignoreCancel && ShouldCancelClose())
                {
                    close = false;
                }
            }
            finally
            {
                if (close)
                {
                    CloseInternal();
                }
            }
        }

        /// <summary>
        /// Handles a closing notification from <see cref="IWindowImpl.Closing"/>.
        /// <returns>true if closing is cancelled. Otherwise false.</returns>
        /// </summary>
        protected virtual bool HandleClosing()
        {
            if (!ShouldCancelClose())
            {
                CloseInternal();
                return false;
            }
            
            return true;
        }

        private void CloseInternal()
        {
            foreach (var (child, _) in _children.ToList())
            {
                child.CloseInternal();
            }

            if (Owner is Window owner)
            {
                owner.RemoveChild(this);
            }

            Owner = null;

            PlatformImpl?.Dispose();
        }

        private bool ShouldCancelClose(CancelEventArgs args = null)
        {
            if (args is null)
            {
                args = new CancelEventArgs();
            }
            
            bool canClose = true;

            foreach (var (child, _) in _children.ToList())
            {
                if (child.ShouldCancelClose(args))
                {
                    canClose = false;
                }
            }

            if (canClose)
            {
                OnClosing(args);

                return args.Cancel;
            }

            return true;
        }

        protected virtual void HandleWindowStateChanged(WindowState state)
        {
            WindowState = state;

            if (state == WindowState.Minimized)
            {
                Renderer.Stop();
            }
            else
            {
                Renderer.Start();
            }
        }

        protected virtual void ExtendClientAreaToDecorationsChanged(bool isExtended)
        {
            IsExtendedIntoWindowDecorations = isExtended;
            WindowDecorationMargin = PlatformImpl.ExtendedMargins;
            OffScreenMargin = PlatformImpl.OffScreenMargin;
        }

        /// <summary>
        /// Hides the window but does not close it.
        /// </summary>
        public override void Hide()
        {
            if (!IsVisible)
            {
                return;
            }

            using (BeginAutoSizing())
            {
                Renderer?.Stop();

                if (Owner is Window owner)
                {
                    owner.RemoveChild(this);
                }

                Owner = null;

                PlatformImpl?.Hide();
            }

            IsVisible = false;
        }

        /// <summary>
        /// Shows the window.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// The window has already been closed.
        /// </exception>
        public override void Show()
        {
            ShowCore(null);
        }

        /// <summary>
        /// Shows the window as a child of <paramref name="parent"/>.
        /// </summary>
        /// <param name="parent">Window that will be a parent of the shown window.</param>
        /// <exception cref="InvalidOperationException">
        /// The window has already been closed.
        /// </exception>
        public void Show(Window parent)
        {
            if (parent is null)
            {
                throw new ArgumentNullException(nameof(parent), "Showing a child window requires valid parent.");
            }

            ShowCore(parent);
        }

        private void ShowCore(Window parent)
        {
            if (PlatformImpl == null)
            {
                throw new InvalidOperationException("Cannot re-show a closed window.");
            }

            if (IsVisible)
            {
                return;
            }

            RaiseEvent(new RoutedEventArgs(WindowOpenedEvent));

            EnsureInitialized();
            IsVisible = true;

            var initialSize = new Size(
                double.IsNaN(Width) ? ClientSize.Width : Width,
                double.IsNaN(Height) ? ClientSize.Height : Height);

            if (initialSize != ClientSize)
            {
                using (BeginAutoSizing())
                {
                    PlatformImpl?.Resize(initialSize);
                }
            }

            LayoutManager.ExecuteInitialLayoutPass();

            using (BeginAutoSizing())
            {
                if (parent != null)
                {
                    PlatformImpl?.SetParent(parent.PlatformImpl);
                }
                
                Owner = parent;
                parent?.AddChild(this, false);
                
                SetWindowStartupLocation(Owner?.PlatformImpl);
                
                PlatformImpl?.Show(ShowActivated);
                Renderer?.Start();                
            }
            OnOpened(EventArgs.Empty);
        }

        /// <summary>
        /// Shows the window as a dialog.
        /// </summary>
        /// <param name="owner">The dialog's owner window.</param>
        /// <exception cref="InvalidOperationException">
        /// The window has already been closed.
        /// </exception>
        /// <returns>
        /// A task that can be used to track the lifetime of the dialog.
        /// </returns>
        public Task ShowDialog(Window owner)
        {
            return ShowDialog<object>(owner);
        }

        /// <summary>
        /// Shows the window as a dialog.
        /// </summary>
        /// <typeparam name="TResult">
        /// The type of the result produced by the dialog.
        /// </typeparam>
        /// <param name="owner">The dialog's owner window.</param>
        /// <returns>.
        /// A task that can be used to retrieve the result of the dialog when it closes.
        /// </returns>
        public Task<TResult> ShowDialog<TResult>(Window owner)
        {
            if (owner == null)
            {
                throw new ArgumentNullException(nameof(owner));
            }

            if (IsVisible)
            {
                throw new InvalidOperationException("The window is already being shown.");
            }

            RaiseEvent(new RoutedEventArgs(WindowOpenedEvent));

            EnsureInitialized();
            IsVisible = true;

            var initialSize = new Size(
                double.IsNaN(Width) ? ClientSize.Width : Width,
                double.IsNaN(Height) ? ClientSize.Height : Height);

            if (initialSize != ClientSize)
            {
                using (BeginAutoSizing())
                {
                    PlatformImpl?.Resize(initialSize);
                }
            }

            LayoutManager.ExecuteInitialLayoutPass();

            var result = new TaskCompletionSource<TResult>();

            using (BeginAutoSizing())
            {
                PlatformImpl?.SetParent(owner.PlatformImpl);
                Owner = owner;
                owner.AddChild(this, true);
                
                SetWindowStartupLocation(owner.PlatformImpl);
                
                PlatformImpl?.Show(ShowActivated);

                Renderer?.Start();

                Observable.FromEventPattern<EventHandler, EventArgs>(
                        x => Closed += x,
                        x => Closed -= x)
                    .Take(1)
                    .Subscribe(_ =>
                    {
                        owner.Activate();
                        result.SetResult((TResult)(_dialogResult ?? default(TResult)));
                    });

                OnOpened(EventArgs.Empty);
            }

            return result.Task;
        }

        private void UpdateEnabled()
        {
            bool isEnabled = true;

            foreach (var (_, isDialog)  in _children)
            {
                if (isDialog)
                {
                    isEnabled = false;
                    break;
                }
            }

            PlatformImpl.SetEnabled(isEnabled);
        }

        private void AddChild(Window window, bool isDialog)
        {
            _children.Add((window, isDialog));
            UpdateEnabled();
        }

        private void RemoveChild(Window window)
        {
            for (int i = _children.Count - 1; i >= 0; i--)
            {
                var (child, _) = _children[i];

                if (ReferenceEquals(child, window))
                {
                    _children.RemoveAt(i);
                }
            }

            UpdateEnabled();
        }

        private void OnGotInputWhenDisabled()
        {
            Window firstDialogChild = null;

            foreach (var (child, isDialog)  in _children)
            {
                if (isDialog)
                {
                    firstDialogChild = child;
                    break;
                }
            }

            if (firstDialogChild != null)
            {
                firstDialogChild.OnGotInputWhenDisabled();
            }
            else
            {
                Activate();
            }
        }

        private void SetWindowStartupLocation(IWindowBaseImpl owner = null)
        {
            var scaling = owner?.DesktopScaling ?? PlatformImpl?.DesktopScaling ?? 1;

            // TODO: We really need non-client size here.
            var rect = new PixelRect(
                PixelPoint.Origin,
                PixelSize.FromSize(ClientSize, scaling));

            if (WindowStartupLocation == WindowStartupLocation.CenterScreen)
            {
                var screen = Screens.ScreenFromPoint(owner?.Position ?? Position);

                if (screen != null)
                {
                    Position = screen.WorkingArea.CenterRect(rect).Position;
                }
            }
            else if (WindowStartupLocation == WindowStartupLocation.CenterOwner)
            {
                if (owner != null)
                {
                    // TODO: We really need non-client size here.
                    var ownerRect = new PixelRect(
                        owner.Position,
                        PixelSize.FromSize(owner.ClientSize, scaling));
                    Position = ownerRect.CenterRect(rect).Position;
                }
            }
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            var sizeToContent = SizeToContent;
            var clientSize = ClientSize;
            var constraint = clientSize;
            var maxAutoSize = PlatformImpl?.MaxAutoSizeHint ?? Size.Infinity;

            if (sizeToContent.HasAllFlags(SizeToContent.Width))
            {
                constraint = constraint.WithWidth(maxAutoSize.Width);
            }

            if (sizeToContent.HasAllFlags(SizeToContent.Height))
            {
                constraint = constraint.WithHeight(maxAutoSize.Height);
            }

            var result = base.MeasureOverride(constraint);

            if (!sizeToContent.HasAllFlags(SizeToContent.Width))
            {
                if (!double.IsInfinity(availableSize.Width))
                {
                    result = result.WithWidth(availableSize.Width);
                }
                else
                {
                    result = result.WithWidth(clientSize.Width);
                }
            }

            if (!sizeToContent.HasAllFlags(SizeToContent.Height))
            {
                if (!double.IsInfinity(availableSize.Height))
                {
                    result = result.WithHeight(availableSize.Height);
                }
                else
                {
                    result = result.WithHeight(clientSize.Height);
                }
            }

            return result;
        }

        protected sealed override Size ArrangeSetBounds(Size size)
        {
            using (BeginAutoSizing())
            {
                PlatformImpl?.Resize(size);
                return ClientSize;
            }
        }

        protected sealed override void HandleClosed()
        {
            RaiseEvent(new RoutedEventArgs(WindowClosedEvent));

            base.HandleClosed();

            if (Owner is Window owner)
            {
                owner.RemoveChild(this);
            }

            Owner = null;
        }

        /// <inheritdoc/>
        protected sealed override void HandleResized(Size clientSize)
        {
            if (!AutoSizing)
            {
                SizeToContent = SizeToContent.Manual;
            }

            Width = clientSize.Width;
            Height = clientSize.Height;

            base.HandleResized(clientSize);
        }

        /// <summary>
        /// Raises the <see cref="Closing"/> event.
        /// </summary>
        /// <param name="e">The event args.</param>
        /// <remarks>
        /// A type that derives from <see cref="Window"/>  may override <see cref="OnClosing"/>. The
        /// overridden method must call <see cref="OnClosing"/> on the base class if the
        /// <see cref="Closing"/> event needs to be raised.
        /// </remarks>
        protected virtual void OnClosing(CancelEventArgs e) => Closing?.Invoke(this, e);

        protected override void OnPropertyChanged<T>(AvaloniaPropertyChangedEventArgs<T> change)
        {
            if (change.Property == SystemDecorationsProperty)
            {
                var typedNewValue = change.NewValue.GetValueOrDefault<SystemDecorations>();

                PlatformImpl?.SetSystemDecorations(typedNewValue);

                var o = change.OldValue.GetValueOrDefault<SystemDecorations>() == SystemDecorations.Full;
                var n = typedNewValue == SystemDecorations.Full;

                if (o != n)
                {
#pragma warning disable CS0618 // Type or member is obsolete
                    RaisePropertyChanged(HasSystemDecorationsProperty, o, n);
#pragma warning restore CS0618 // Type or member is obsolete
                }
            }
        }

        protected override AutomationPeer OnCreateAutomationPeer(IAutomationNode node)
        {
            return new WindowAutomationPeer(node, this);
        }
    }
}
