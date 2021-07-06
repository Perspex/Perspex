using System;
using System.Collections.Generic;
using System.Diagnostics;
using Avalonia.Controls;
using Avalonia.Controls.Platform.Surfaces;
using Avalonia.Controls.Primitives.PopupPositioning;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Rendering;
using Avalonia.Threading;
using Avalonia.Utilities;

namespace Avalonia.Headless
{
    class HeadlessWindowImpl : IWindowImpl, IPopupImpl, IFramebufferPlatformSurface, IHeadlessWindow
    {
        private IKeyboardDevice _keyboard;
        private Stopwatch _st = Stopwatch.StartNew();
        private Pointer _mousePointer;
        private WriteableBitmap _lastRenderedFrame;
        private object _sync = new object();
        public bool IsPopup { get; }

        public HeadlessWindowImpl(bool isPopup)
        {
            IsPopup = isPopup;
            Surfaces = new object[] { this };
            _keyboard = AvaloniaLocator.Current.GetService<IKeyboardDevice>();
            _mousePointer = new Pointer(Pointer.GetNextFreeId(), PointerType.Mouse, true);
            MouseDevice = new MouseDevice(_mousePointer);
            ClientSize = new Size(1024, 768);
        }

        public void Dispose()
        {
            Closed?.Invoke();
            _lastRenderedFrame?.Dispose();
            _lastRenderedFrame = null;
        }

        public Size ClientSize { get; set; }
        public double RenderScaling { get; } = 1;
        public double DesktopScaling => RenderScaling;
        public IEnumerable<object> Surfaces { get; }
        public Action<RawInputEventArgs> Input { get; set; }
        public Action<Rect> Paint { get; set; }
        public Action<Size> Resized { get; set; }
        public Action<double> ScalingChanged { get; set; }

        public IRenderer CreateRenderer(IRenderRoot root)
            => new DeferredRenderer(root, AvaloniaLocator.Current.GetService<IRenderLoop>());

        public void Invalidate(Rect rect)
        {
        }

        public void SetInputRoot(IInputRoot inputRoot)
        {
            InputRoot = inputRoot;
        }

        public IInputRoot InputRoot { get; set; }

        public Point PointToClient(PixelPoint point) => point.ToPoint(RenderScaling);

        public PixelPoint PointToScreen(Point point) => PixelPoint.FromPoint(point, RenderScaling);

        public void SetCursor(ICursorImpl cursor)
        {

        }

        public Action Closed { get; set; }
        public IMouseDevice MouseDevice { get; }

        public void Show(bool activate, bool isDialog)
        {
            if (activate)
                Dispatcher.UIThread.Post(() => Activated?.Invoke(), DispatcherPriority.Input);
        }

        public void Hide()
        {
            Dispatcher.UIThread.Post(() => Deactivated?.Invoke(), DispatcherPriority.Input);
        }

        public void BeginMoveDrag()
        {

        }

        public void BeginResizeDrag(WindowEdge edge)
        {

        }

        public PixelPoint Position { get; set; }
        public Action<PixelPoint> PositionChanged { get; set; }
        public void Activate()
        {
            Dispatcher.UIThread.Post(() => Activated?.Invoke(), DispatcherPriority.Input);
        }

        public Action Deactivated { get; set; }
        public Action Activated { get; set; }
        public IPlatformHandle Handle { get; } = new PlatformHandle(IntPtr.Zero, "STUB");
        public Size MaxClientSize { get; } = new Size(1920, 1280);
        public void Resize(Size clientSize)
        {
            // Emulate X11 behavior here
            if (IsPopup)
                DoResize(clientSize);
            else
                Dispatcher.UIThread.Post(() =>
                {
                    DoResize(clientSize);
                });
        }

        void DoResize(Size clientSize)
        {
            // Uncomment this check and experience a weird bug in layout engine
            if (ClientSize != clientSize)
            {
                ClientSize = clientSize;
                Resized?.Invoke(clientSize);
            }
        }

        public void SetMinMaxSize(Size minSize, Size maxSize)
        {

        }

        public void SetTopmost(bool value)
        {

        }

        public IScreenImpl Screen { get; } = new HeadlessScreensStub();
        public WindowState WindowState { get; set; }
        public Action<WindowState> WindowStateChanged { get; set; }
        public void SetTitle(string title)
        {

        }

        public void SetSystemDecorations(bool enabled)
        {

        }

        public void SetIcon(IWindowIconImpl icon)
        {

        }

        public void ShowTaskbarIcon(bool value)
        {

        }

        public void CanResize(bool value)
        {

        }

        public Func<bool> Closing { get; set; }

        class FramebufferProxy : ILockedFramebuffer
        {
            private readonly ILockedFramebuffer _fb;
            private readonly Action _onDispose;
            private bool _disposed;

            public FramebufferProxy(ILockedFramebuffer fb, Action onDispose)
            {
                _fb = fb;
                _onDispose = onDispose;
            }
            public void Dispose()
            {
                if (_disposed)
                    return;
                _disposed = true;
                _fb.Dispose();
                _onDispose();
            }

            public IntPtr Address => _fb.Address;
            public PixelSize Size => _fb.Size;
            public int RowBytes => _fb.RowBytes;
            public Vector Dpi => _fb.Dpi;
            public PixelFormat Format => _fb.Format;
        }

        public ILockedFramebuffer Lock()
        {
            var bmp = new WriteableBitmap(PixelSize.FromSize(ClientSize, RenderScaling), new Vector(96, 96) * RenderScaling);
            var fb = bmp.Lock();
            return new FramebufferProxy(fb, () =>
            {
                lock (_sync)
                {
                    _lastRenderedFrame?.Dispose();
                    _lastRenderedFrame = bmp;
                }
            });
        }

        public IRef<IWriteableBitmapImpl> GetLastRenderedFrame()
        {
            lock (_sync)
                return _lastRenderedFrame?.PlatformImpl?.CloneAs<IWriteableBitmapImpl>();
        }

        private ulong Timestamp => (ulong)_st.ElapsedMilliseconds;

        // TODO: Hook recent Popup changes. 
        IPopupPositioner IPopupImpl.PopupPositioner => null;

        public Size MaxAutoSizeHint => new Size(1920, 1080);

        public Action<WindowTransparencyLevel> TransparencyLevelChanged { get; set; }

        public WindowTransparencyLevel TransparencyLevel => WindowTransparencyLevel.None;

        public Action GotInputWhenDisabled { get; set; }

        public bool IsClientAreaExtendedToDecorations => false;

        public Action<bool> ExtendClientAreaToDecorationsChanged { get; set; }

        public bool NeedsManagedDecorations => false;

        public Thickness ExtendedMargins => new Thickness();

        public Thickness OffScreenMargin => new Thickness();

        public Action LostFocus { get; set; }

        public AcrylicPlatformCompensationLevels AcrylicCompensationLevels => new AcrylicPlatformCompensationLevels(1, 1, 1);

        void IHeadlessWindow.KeyPress(Key key, RawInputModifiers modifiers)
        {
            Input?.Invoke(new RawKeyEventArgs(_keyboard, Timestamp, InputRoot, RawKeyEventType.KeyDown, key, modifiers));
        }

        void IHeadlessWindow.KeyRelease(Key key, RawInputModifiers modifiers)
        {
            Input?.Invoke(new RawKeyEventArgs(_keyboard, Timestamp, InputRoot, RawKeyEventType.KeyUp, key, modifiers));
        }

        void IHeadlessWindow.MouseDown(Point point, int button, RawInputModifiers modifiers)
        {
            Input?.Invoke(new RawPointerEventArgs(MouseDevice, Timestamp, InputRoot,
                button == 0 ? RawPointerEventType.LeftButtonDown :
                button == 1 ? RawPointerEventType.MiddleButtonDown : RawPointerEventType.RightButtonDown,
                point, modifiers));
        }

        void IHeadlessWindow.MouseMove(Point point, RawInputModifiers modifiers)
        {
            Input?.Invoke(new RawPointerEventArgs(MouseDevice, Timestamp, InputRoot,
                RawPointerEventType.Move, point, modifiers));
        }

        void IHeadlessWindow.MouseUp(Point point, int button, RawInputModifiers modifiers)
        {
            Input?.Invoke(new RawPointerEventArgs(MouseDevice, Timestamp, InputRoot,
                button == 0 ? RawPointerEventType.LeftButtonUp :
                button == 1 ? RawPointerEventType.MiddleButtonUp : RawPointerEventType.RightButtonUp,
                point, modifiers));
        }

        void IWindowImpl.Move(PixelPoint point)
        {

        }

        public IPopupImpl CreatePopup()
        {
            // TODO: Hook recent Popup changes. 
            return null;
        }

        public void SetWindowManagerAddShadowHint(bool enabled)
        {
            
        }

        public void SetTransparencyLevelHint(WindowTransparencyLevel transparencyLevel)
        {
            
        }

        public void SetParent(IWindowImpl parent)
        {
            
        }

        public void SetEnabled(bool enable)
        {
            
        }

        public void SetSystemDecorations(SystemDecorations enabled)
        {
            
        }

        public void BeginMoveDrag(PointerPressedEventArgs e)
        {
            
        }

        public void BeginResizeDrag(WindowEdge edge, PointerPressedEventArgs e)
        {
            
        }

        public void SetExtendClientAreaToDecorationsHint(bool extendIntoClientAreaHint)
        {
            
        }

        public void SetExtendClientAreaChromeHints(ExtendClientAreaChromeHints hints)
        {
            
        }

        public void SetExtendClientAreaTitleBarHeightHint(double titleBarHeight)
        {
            
        }
    }
}
