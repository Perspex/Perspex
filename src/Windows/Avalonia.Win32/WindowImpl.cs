using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;
using Avalonia.Controls;
using Avalonia.Controls.Platform;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.OpenGL;
using Avalonia.OpenGL.Angle;
using Avalonia.OpenGL.Egl;
using Avalonia.OpenGL.Surfaces;
using Avalonia.Platform;
using Avalonia.Rendering;
using Avalonia.Win32.Input;
using Avalonia.Win32.Interop;
using Avalonia.Win32.OpenGl;
using Avalonia.Win32.WinRT;
using Avalonia.Win32.WinRT.Composition;
using static Avalonia.Win32.Interop.UnmanagedMethods;

namespace Avalonia.Win32
{
    /// <summary>
    /// Window implementation for Win32 platform.
    /// </summary>
    public partial class WindowImpl : IWindowImpl, EglGlPlatformSurface.IEglWindowGlPlatformSurfaceInfo,
        ITopLevelImplWithNativeControlHost
    {
        private static readonly List<WindowImpl> s_instances = new List<WindowImpl>();

        private static readonly IntPtr DefaultCursor = LoadCursor(
            IntPtr.Zero, new IntPtr((int)UnmanagedMethods.Cursor.IDC_ARROW));

        private static readonly Dictionary<WindowEdge, HitTestValues> s_edgeLookup =
            new Dictionary<WindowEdge, HitTestValues>
            {
                { WindowEdge.East, HitTestValues.HTRIGHT },
                { WindowEdge.North, HitTestValues.HTTOP },
                { WindowEdge.NorthEast, HitTestValues.HTTOPRIGHT },
                { WindowEdge.NorthWest, HitTestValues.HTTOPLEFT },
                { WindowEdge.South, HitTestValues.HTBOTTOM },
                { WindowEdge.SouthEast, HitTestValues.HTBOTTOMRIGHT },
                { WindowEdge.SouthWest, HitTestValues.HTBOTTOMLEFT },
                { WindowEdge.West, HitTestValues.HTLEFT }
            };

        private SavedWindowInfo _savedWindowInfo;
        private bool _isFullScreenActive;
        private bool _isClientAreaExtended;
        private Thickness _extendedMargins;
        private Thickness _offScreenMargin;
        private double _extendTitleBarHint = -1;
        private bool _isUsingComposition;
        private IBlurHost _blurHost;

#if USE_MANAGED_DRAG
        private readonly ManagedWindowResizeDragHelper _managedDrag;
#endif

        private const WindowStyles WindowStateMask = (WindowStyles.WS_MAXIMIZE | WindowStyles.WS_MINIMIZE);
        private readonly TouchDevice _touchDevice;
        private readonly MouseDevice _mouseDevice;
        private readonly ManagedDeferredRendererLock _rendererLock;
        private readonly FramebufferManager _framebuffer;
        private readonly IGlPlatformSurface _gl;

        private Win32NativeControlHost _nativeControlHost;
        private WndProc _wndProcDelegate;
        private string _className;
        private IntPtr _hwnd;
        private bool _multitouch;
        private IInputRoot _owner;
        private WindowProperties _windowProperties;
        private bool _trackingMouse;
        private bool _topmost;
        private double _scaling = 1;
        private WindowState _showWindowState;
        private WindowState _lastWindowState;
        private OleDropTarget _dropTarget;
        private Size _minSize;
        private Size _maxSize;
        private POINT _maxTrackSize;
        private WindowImpl _parent;        
        private ExtendClientAreaChromeHints _extendChromeHints = ExtendClientAreaChromeHints.Default;
        private bool _isCloseRequested;
        private bool _shown;
        private bool _hiddenWindowIsParent;

        public WindowImpl()
        {
            _touchDevice = new TouchDevice();
            _mouseDevice = new WindowsMouseDevice();

#if USE_MANAGED_DRAG
            _managedDrag = new ManagedWindowResizeDragHelper(this, capture =>
            {
                if (capture)
                    UnmanagedMethods.SetCapture(Handle.Handle);
                else
                    UnmanagedMethods.ReleaseCapture();
            });
#endif

            _windowProperties = new WindowProperties
            {
                ShowInTaskbar = false,
                IsResizable = true,
                Decorations = SystemDecorations.Full
            };
            _rendererLock = new ManagedDeferredRendererLock();

            var glPlatform = AvaloniaLocator.Current.GetService<IPlatformOpenGlInterface>();

            var compositionConnector = AvaloniaLocator.Current.GetService<WinUICompositorConnection>();

            _isUsingComposition = compositionConnector is { } &&
                glPlatform is EglPlatformOpenGlInterface egl &&
                    egl.Display is AngleWin32EglDisplay angleDisplay &&
                    angleDisplay.PlatformApi == AngleOptions.PlatformApi.DirectX11;

            CreateWindow();
            _framebuffer = new FramebufferManager(_hwnd);
            
            if (glPlatform != null)
            {
                if (_isUsingComposition)
                {
                    var cgl = new WinUiCompositedWindowSurface(compositionConnector, this);
                    _blurHost = cgl;

                    _gl = cgl;

                    _isUsingComposition = true;
                }
                else
                {
                    if (glPlatform is EglPlatformOpenGlInterface egl2)
                        _gl = new EglGlPlatformSurface(egl2, this);
                    else if (glPlatform is WglPlatformOpenGlInterface wgl)
                        _gl = new WglGlPlatformSurface(wgl.PrimaryContext, this);
                }
            }

            Screen = new ScreenImpl();

            _nativeControlHost = new Win32NativeControlHost(this);
            s_instances.Add(this);
        }

        public Action Activated { get; set; }

        public Func<bool> Closing { get; set; }

        public Action Closed { get; set; }

        public Action Deactivated { get; set; }

        public Action<RawInputEventArgs> Input { get; set; }

        public Action<Rect> Paint { get; set; }

        public Action<Size> Resized { get; set; }

        public Action<double> ScalingChanged { get; set; }

        public Action<PixelPoint> PositionChanged { get; set; }

        public Action<WindowState> WindowStateChanged { get; set; }
        
        public Action LostFocus { get; set; }

        public Action<WindowTransparencyLevel> TransparencyLevelChanged { get; set; }

        public Thickness BorderThickness
        {
            get
            {
                if (HasFullDecorations)
                {
                    var style = GetStyle();
                    var exStyle = GetExtendedStyle();

                    var padding = new RECT();

                    if (AdjustWindowRectEx(ref padding, (uint)style, false, (uint)exStyle))
                    {
                        return new Thickness(-padding.left, -padding.top, padding.right, padding.bottom);
                    }
                    else
                    {
                        throw new Win32Exception();
                    }
                }
                else
                {
                    return new Thickness();
                }
            }
        }

        public double RenderScaling => _scaling;

        public double DesktopScaling => RenderScaling;

        public Size ClientSize
        {
            get
            {
                GetClientRect(_hwnd, out var rect);

                return new Size(rect.right, rect.bottom) / RenderScaling;
            }
        }

        public Size? FrameSize
        {
            get
            {
                if (DwmIsCompositionEnabled(out var compositionEnabled) != 0 || !compositionEnabled)
                {
                    GetWindowRect(_hwnd, out var rcWindow);
                    return new Size(rcWindow.Width, rcWindow.Height) / RenderScaling;
                }

                DwmGetWindowAttribute(_hwnd, (int)DwmWindowAttribute.DWMWA_EXTENDED_FRAME_BOUNDS, out var rect, Marshal.SizeOf(typeof(RECT)));
                return new Size(rect.Width, rect.Height) / RenderScaling;
            }
        }

        public IScreenImpl Screen { get; }

        public IPlatformHandle Handle { get; private set; }

        public virtual Size MaxAutoSizeHint => new Size(_maxTrackSize.X / RenderScaling, _maxTrackSize.Y / RenderScaling);

        public IMouseDevice MouseDevice => _mouseDevice;

        public WindowState WindowState
        {
            get
            {
                if(_isFullScreenActive)
                {
                    return WindowState.FullScreen;
                }

                var placement = default(WINDOWPLACEMENT);
                GetWindowPlacement(_hwnd, ref placement);

                return placement.ShowCmd switch
                {
                    ShowWindowCommand.Maximize => WindowState.Maximized,
                    ShowWindowCommand.Minimize => WindowState.Minimized,
                    _ => WindowState.Normal
                };
            }

            set
            {
                if (IsWindowVisible(_hwnd))
                {
                    ShowWindow(value, true);
                }
                else
                {
                    _showWindowState = value;
                }
            }
        }

        public WindowTransparencyLevel TransparencyLevel { get; private set; }

        protected IntPtr Hwnd => _hwnd;

        public void SetTransparencyLevelHint (WindowTransparencyLevel transparencyLevel)
        {
            TransparencyLevel = EnableBlur(transparencyLevel);
        }

        private WindowTransparencyLevel EnableBlur(WindowTransparencyLevel transparencyLevel)
        {
            if (Win32Platform.WindowsVersion.Major >= 6)
            {
                if (DwmIsCompositionEnabled(out var compositionEnabled) != 0 || !compositionEnabled)
                {
                    return WindowTransparencyLevel.None;
                }
                else if (Win32Platform.WindowsVersion.Major >= 10)
                {
                    return Win10EnableBlur(transparencyLevel);
                }
                else if (Win32Platform.WindowsVersion.Minor >= 2)
                {
                    return Win8xEnableBlur(transparencyLevel);
                }
                else
                {
                    return Win7EnableBlur(transparencyLevel);
                }
            }
            else
            {
                return WindowTransparencyLevel.None;
            }
        }

        private WindowTransparencyLevel Win7EnableBlur(WindowTransparencyLevel transparencyLevel)
        {
            if (transparencyLevel == WindowTransparencyLevel.AcrylicBlur)
            {
                transparencyLevel = WindowTransparencyLevel.Blur;
            }

            var blurInfo = new DWM_BLURBEHIND(false);
            
            if (transparencyLevel == WindowTransparencyLevel.Blur)
            {
                blurInfo = new DWM_BLURBEHIND(true);
            }
            
            DwmEnableBlurBehindWindow(_hwnd, ref blurInfo);

            if (transparencyLevel == WindowTransparencyLevel.Transparent)
            {
                return WindowTransparencyLevel.None;
            }
            else
            {
                return transparencyLevel;
            }
        }

        private WindowTransparencyLevel Win8xEnableBlur(WindowTransparencyLevel transparencyLevel)
        {
            var accent = new AccentPolicy();
            var accentStructSize = Marshal.SizeOf(accent);

            if (transparencyLevel == WindowTransparencyLevel.AcrylicBlur)
            {
                transparencyLevel = WindowTransparencyLevel.Blur;
            }

            if (transparencyLevel == WindowTransparencyLevel.Transparent)
            {
                accent.AccentState = AccentState.ACCENT_ENABLE_BLURBEHIND;
            }
            else
            {
                accent.AccentState = AccentState.ACCENT_DISABLED;
            }

            var accentPtr = Marshal.AllocHGlobal(accentStructSize);
            Marshal.StructureToPtr(accent, accentPtr, false);

            var data = new WindowCompositionAttributeData();
            data.Attribute = WindowCompositionAttribute.WCA_ACCENT_POLICY;
            data.SizeOfData = accentStructSize;
            data.Data = accentPtr;

            SetWindowCompositionAttribute(_hwnd, ref data);

            Marshal.FreeHGlobal(accentPtr);

            if (transparencyLevel >= WindowTransparencyLevel.Blur)
            {
                Win7EnableBlur(transparencyLevel);
            }

            return transparencyLevel;
        }

        private WindowTransparencyLevel Win10EnableBlur(WindowTransparencyLevel transparencyLevel)
        {
            if (_isUsingComposition)
            {
                _blurHost?.SetBlur(transparencyLevel >= WindowTransparencyLevel.Blur);

                return transparencyLevel;
            }
            else
            {
                bool canUseAcrylic = Win32Platform.WindowsVersion.Major > 10 || Win32Platform.WindowsVersion.Build >= 19628;

                var accent = new AccentPolicy();
                var accentStructSize = Marshal.SizeOf(accent);

                if (transparencyLevel == WindowTransparencyLevel.AcrylicBlur && !canUseAcrylic)
                {
                    transparencyLevel = WindowTransparencyLevel.Blur;
                }

                switch (transparencyLevel)
                {
                    default:
                    case WindowTransparencyLevel.None:
                        accent.AccentState = AccentState.ACCENT_DISABLED;
                        break;

                    case WindowTransparencyLevel.Transparent:
                        accent.AccentState = AccentState.ACCENT_ENABLE_TRANSPARENTGRADIENT;
                        break;

                    case WindowTransparencyLevel.Blur:
                        accent.AccentState = AccentState.ACCENT_ENABLE_BLURBEHIND;
                        break;

                    case WindowTransparencyLevel.AcrylicBlur:
                    case (WindowTransparencyLevel.AcrylicBlur + 1): // hack-force acrylic.
                        accent.AccentState = AccentState.ACCENT_ENABLE_ACRYLIC;
                        transparencyLevel = WindowTransparencyLevel.AcrylicBlur;
                        break;
                }

                accent.AccentFlags = 2;
                accent.GradientColor = 0x01000000;

                var accentPtr = Marshal.AllocHGlobal(accentStructSize);
                Marshal.StructureToPtr(accent, accentPtr, false);

                var data = new WindowCompositionAttributeData();
                data.Attribute = WindowCompositionAttribute.WCA_ACCENT_POLICY;
                data.SizeOfData = accentStructSize;
                data.Data = accentPtr;

                SetWindowCompositionAttribute(_hwnd, ref data);

                Marshal.FreeHGlobal(accentPtr);

                return transparencyLevel;
            }
        }

        public IEnumerable<object> Surfaces => new object[] { Handle, _gl, _framebuffer };

        public PixelPoint Position
        {
            get
            {
                GetWindowRect(_hwnd, out var rc);

                return new PixelPoint(rc.left, rc.top);
            }
            set
            {
                SetWindowPos(
                    Handle.Handle,
                    IntPtr.Zero,
                    value.X,
                    value.Y,
                    0,
                    0,
                    SetWindowPosFlags.SWP_NOSIZE | SetWindowPosFlags.SWP_NOACTIVATE);
            }
        }

        private bool HasFullDecorations => _windowProperties.Decorations == SystemDecorations.Full;

        public void Move(PixelPoint point) => Position = point;

        public void SetMinMaxSize(Size minSize, Size maxSize)
        {
            _minSize = minSize;
            _maxSize = maxSize;
        }

        public IRenderer CreateRenderer(IRenderRoot root)
        {
            var loop = AvaloniaLocator.Current.GetService<IRenderLoop>();
            var customRendererFactory = AvaloniaLocator.Current.GetService<IRendererFactory>();

            if (customRendererFactory != null)
                return customRendererFactory.Create(root, loop);

            return Win32Platform.UseDeferredRendering 
                ?  _isUsingComposition 
                    ? new DeferredRenderer(root, loop)
                    {
                        RenderOnlyOnRenderThread = true
                    } 
                    : (IRenderer)new DeferredRenderer(root, loop, rendererLock: _rendererLock)
                : new ImmediateRenderer(root);
        }

        public void Resize(Size value)
        {
            int requestedClientWidth = (int)(value.Width * RenderScaling);
            int requestedClientHeight = (int)(value.Height * RenderScaling);

            GetClientRect(_hwnd, out var clientRect);

            // do comparison after scaling to avoid rounding issues
            if (requestedClientWidth != clientRect.Width || requestedClientHeight != clientRect.Height)
            {
                GetWindowRect(_hwnd, out var windowRect);

                SetWindowPos(
                    _hwnd,
                    IntPtr.Zero,
                    0,
                    0,
                    requestedClientWidth + (_isClientAreaExtended ? 0 : windowRect.Width - clientRect.Width),
                    requestedClientHeight + (_isClientAreaExtended ? 0 : windowRect.Height - clientRect.Height),
                    SetWindowPosFlags.SWP_RESIZE);
            }
        }

        public void Activate()
        {
            SetActiveWindow(_hwnd);
        }

        public IPopupImpl CreatePopup() => Win32Platform.UseOverlayPopups ? null : new PopupImpl(this);

        public void Dispose()
        {
            (_gl as IDisposable)?.Dispose();

            if (_dropTarget != null)
            {
                OleContext.Current?.UnregisterDragDrop(Handle);
                _dropTarget = null;
            }

            if (_hwnd != IntPtr.Zero)
            {
                // Detect if we are being closed programmatically - this would mean that WM_CLOSE was not called
                // and we didn't prepare this window for destruction.
                if (!_isCloseRequested)
                {
                    BeforeCloseCleanup(true);
                }
                
                DestroyWindow(_hwnd);
                _hwnd = IntPtr.Zero;
            }

            if (_className != null)
            {
                UnregisterClass(_className, GetModuleHandle(null));
                _className = null;
            }

            _framebuffer.Dispose();
        }

        public void Invalidate(Rect rect)
        {
            var scaling = RenderScaling;
            var r = new RECT
            {
                left = (int)Math.Floor(rect.X * scaling),
                top = (int)Math.Floor(rect.Y * scaling),
                right = (int)Math.Ceiling(rect.Right * scaling),
                bottom = (int)Math.Ceiling(rect.Bottom * scaling),
            };

            InvalidateRect(_hwnd, ref r, false);
        }

        public Point PointToClient(PixelPoint point)
        {
            var p = new POINT { X = point.X, Y = point.Y };
            UnmanagedMethods.ScreenToClient(_hwnd, ref p);
            return new Point(p.X, p.Y) / RenderScaling;
        }

        public PixelPoint PointToScreen(Point point)
        {
            point *= RenderScaling;
            var p = new POINT { X = (int)point.X, Y = (int)point.Y };
            ClientToScreen(_hwnd, ref p);
            return new PixelPoint(p.X, p.Y);
        }

        public void SetInputRoot(IInputRoot inputRoot)
        {
            _owner = inputRoot;
            CreateDropTarget();
        }

        public void Hide()
        {
            UnmanagedMethods.ShowWindow(_hwnd, ShowWindowCommand.Hide);
            _shown = false;
        }

        public virtual void Show(bool activate)
        {
            SetParent(_parent);
            ShowWindow(_showWindowState, activate);
        }

        public Action GotInputWhenDisabled { get; set; }

        public void SetParent(IWindowImpl parent)
        {
            _parent = (WindowImpl)parent;
            
            var parentHwnd = _parent?._hwnd ?? IntPtr.Zero;

            if (parentHwnd == IntPtr.Zero && !_windowProperties.ShowInTaskbar)
            {
                parentHwnd = OffscreenParentWindow.Handle;
                _hiddenWindowIsParent = true;
            }

            SetWindowLongPtr(_hwnd, (int)WindowLongParam.GWL_HWNDPARENT, parentHwnd);
        }

        public void SetEnabled(bool enable) => EnableWindow(_hwnd, enable);

        public void BeginMoveDrag(PointerPressedEventArgs e)
        {
            _mouseDevice.Capture(null);
            DefWindowProc(_hwnd, (int)WindowsMessage.WM_NCLBUTTONDOWN,
                new IntPtr((int)HitTestValues.HTCAPTION), IntPtr.Zero);
            e.Pointer.Capture(null);
        }

        public void BeginResizeDrag(WindowEdge edge, PointerPressedEventArgs e)
        {
#if USE_MANAGED_DRAG
            _managedDrag.BeginResizeDrag(edge, ScreenToClient(MouseDevice.Position.ToPoint(_scaling)));
#else
            _mouseDevice.Capture(null);
            DefWindowProc(_hwnd, (int)WindowsMessage.WM_NCLBUTTONDOWN,
                new IntPtr((int)s_edgeLookup[edge]), IntPtr.Zero);
#endif
        }

        public void SetTitle(string title)
        {
            SetWindowText(_hwnd, title);
        }

        public void SetCursor(ICursorImpl cursor)
        {
            var impl = cursor as CursorImpl;

            if (cursor is null || impl is object)
            {
                var hCursor = impl?.Handle ?? DefaultCursor;
                SetClassLong(_hwnd, ClassLongIndex.GCLP_HCURSOR, hCursor);

                if (_owner.IsPointerOver)
                {
                    UnmanagedMethods.SetCursor(hCursor);
                }
            }
        }

        public void SetIcon(IWindowIconImpl icon)
        {
            var impl = (IconImpl)icon;
            var hIcon = impl?.HIcon ?? IntPtr.Zero;
            PostMessage(_hwnd, (int)WindowsMessage.WM_SETICON,
                new IntPtr((int)Icons.ICON_BIG), hIcon);
        }

        public void ShowTaskbarIcon(bool value)
        {
            var newWindowProperties = _windowProperties;

            newWindowProperties.ShowInTaskbar = value;

            UpdateWindowProperties(newWindowProperties);
        }

        public void CanResize(bool value)
        {
            var newWindowProperties = _windowProperties;

            newWindowProperties.IsResizable = value;

            UpdateWindowProperties(newWindowProperties);
        }

        public void SetSystemDecorations(SystemDecorations value)
        {
            var newWindowProperties = _windowProperties;

            newWindowProperties.Decorations = value;

            UpdateWindowProperties(newWindowProperties);
        }

        public void SetTopmost(bool value)
        {
            if (value == _topmost)
            {
                return;
            }

            IntPtr hWndInsertAfter = value ? WindowPosZOrder.HWND_TOPMOST : WindowPosZOrder.HWND_NOTOPMOST;
            SetWindowPos(_hwnd,
                hWndInsertAfter,
                0, 0, 0, 0,
                SetWindowPosFlags.SWP_NOMOVE | SetWindowPosFlags.SWP_NOSIZE | SetWindowPosFlags.SWP_NOACTIVATE);

            _topmost = value;
        }

        protected virtual IntPtr CreateWindowOverride(ushort atom)
        {
            return CreateWindowEx(
                _isUsingComposition ? (int)WindowStyles.WS_EX_NOREDIRECTIONBITMAP : 0,
                atom,
                null,
                (int)WindowStyles.WS_OVERLAPPEDWINDOW | (int) WindowStyles.WS_CLIPCHILDREN,
                CW_USEDEFAULT,
                CW_USEDEFAULT,
                CW_USEDEFAULT,
                CW_USEDEFAULT,
                IntPtr.Zero,
                IntPtr.Zero,
                IntPtr.Zero,
                IntPtr.Zero);
        }

        private void CreateWindow()
        {
            // Ensure that the delegate doesn't get garbage collected by storing it as a field.
            _wndProcDelegate = WndProc;

            _className = $"Avalonia-{Guid.NewGuid().ToString()}";

            // Unique DC helps with performance when using Gpu based rendering
            const ClassStyles windowClassStyle = ClassStyles.CS_OWNDC | ClassStyles.CS_HREDRAW | ClassStyles.CS_VREDRAW;

            var wndClassEx = new WNDCLASSEX
            {
                cbSize = Marshal.SizeOf<WNDCLASSEX>(),
                style = (int)windowClassStyle,
                lpfnWndProc = _wndProcDelegate,
                hInstance = GetModuleHandle(null),
                hCursor = DefaultCursor,
                hbrBackground = IntPtr.Zero,
                lpszClassName = _className
            };

            ushort atom = RegisterClassEx(ref wndClassEx);

            if (atom == 0)
            {
                throw new Win32Exception();
            }

            _hwnd = CreateWindowOverride(atom);

            if (_hwnd == IntPtr.Zero)
            {
                throw new Win32Exception();
            }

            Handle = new PlatformHandle(_hwnd, PlatformConstants.WindowHandleType);

            _multitouch = Win32Platform.Options.EnableMultitouch ?? true;

            if (_multitouch)
            {
                RegisterTouchWindow(_hwnd, 0);
            }

            if (ShCoreAvailable && Win32Platform.WindowsVersion > PlatformConstants.Windows8)
			{
                var monitor = MonitorFromWindow(
                    _hwnd,
                    MONITOR.MONITOR_DEFAULTTONEAREST);

                if (GetDpiForMonitor(
                    monitor,
                    MONITOR_DPI_TYPE.MDT_EFFECTIVE_DPI,
                    out var dpix,
                    out var dpiy) == 0)
                {
                    _scaling = dpix / 96.0;
                }
            }
        }

        private void CreateDropTarget()
        {
            var odt = new OleDropTarget(this, _owner);

            if (OleContext.Current?.RegisterDragDrop(Handle, odt) ?? false)
            {
                _dropTarget = odt;
            }
        }

        /// <summary>
        /// Ported from https://github.com/chromium/chromium/blob/master/ui/views/win/fullscreen_handler.cc
        /// Method must only be called from inside UpdateWindowProperties.
        /// </summary>
        /// <param name="fullscreen"></param>
        private void SetFullScreen(bool fullscreen)
        {
            if (fullscreen)
            {
                GetWindowRect(_hwnd, out var windowRect);
                _savedWindowInfo.WindowRect = windowRect;

                var current = GetStyle();
                var currentEx = GetExtendedStyle();

                _savedWindowInfo.Style = current;
                _savedWindowInfo.ExStyle = currentEx;

                // Set new window style and size.
                SetStyle(current & ~(WindowStyles.WS_CAPTION | WindowStyles.WS_THICKFRAME), false);
                SetExtendedStyle(currentEx & ~(WindowStyles.WS_EX_DLGMODALFRAME | WindowStyles.WS_EX_WINDOWEDGE | WindowStyles.WS_EX_CLIENTEDGE | WindowStyles.WS_EX_STATICEDGE), false);

                // On expand, if we're given a window_rect, grow to it, otherwise do
                // not resize.
                MONITORINFO monitor_info = MONITORINFO.Create();
                GetMonitorInfo(MonitorFromWindow(_hwnd, MONITOR.MONITOR_DEFAULTTONEAREST), ref monitor_info);

                var window_rect = monitor_info.rcMonitor.ToPixelRect();

                SetWindowPos(_hwnd, IntPtr.Zero, window_rect.X, window_rect.Y,
                             window_rect.Width, window_rect.Height,
                             SetWindowPosFlags.SWP_NOZORDER | SetWindowPosFlags.SWP_NOACTIVATE | SetWindowPosFlags.SWP_FRAMECHANGED);

                _isFullScreenActive = true;
            }
            else
            {
                // Reset original window style and size.  The multiple window size/moves
                // here are ugly, but if SetWindowPos() doesn't redraw, the taskbar won't be
                // repainted.  Better-looking methods welcome.
                _isFullScreenActive = false;

                var windowStates = GetWindowStateStyles();
                SetStyle((_savedWindowInfo.Style & ~WindowStateMask) | windowStates, false);
                SetExtendedStyle(_savedWindowInfo.ExStyle, false);

                // On restore, resize to the previous saved rect size.
                var new_rect = _savedWindowInfo.WindowRect.ToPixelRect();

                SetWindowPos(_hwnd, IntPtr.Zero, new_rect.X, new_rect.Y, new_rect.Width,
                             new_rect.Height,
                            SetWindowPosFlags.SWP_NOZORDER | SetWindowPosFlags.SWP_NOACTIVATE | SetWindowPosFlags.SWP_FRAMECHANGED);

                UpdateWindowProperties(_windowProperties, true);
            }

            TaskBarList.MarkFullscreen(_hwnd, fullscreen);
            
            ExtendClientArea();
        }

        private MARGINS UpdateExtendMargins()
        {
            RECT borderThickness = new RECT();
            RECT borderCaptionThickness = new RECT();            

            AdjustWindowRectEx(ref borderCaptionThickness, (uint)(GetStyle()), false, 0);
            AdjustWindowRectEx(ref borderThickness, (uint)(GetStyle() & ~WindowStyles.WS_CAPTION), false, 0);
            borderThickness.left *= -1;
            borderThickness.top *= -1;
            borderCaptionThickness.left *= -1;
            borderCaptionThickness.top *= -1;

            bool wantsTitleBar = _extendChromeHints.HasAllFlags(ExtendClientAreaChromeHints.SystemChrome) || _extendTitleBarHint == -1;

            if (!wantsTitleBar)
            {
                borderCaptionThickness.top = 1;
            }

            MARGINS margins = new MARGINS();
            margins.cxLeftWidth = 1;
            margins.cxRightWidth = 1;
            margins.cyBottomHeight = 1;

            if (_extendTitleBarHint != -1)
            {
                borderCaptionThickness.top = (int)(_extendTitleBarHint * RenderScaling);                
            }

            margins.cyTopHeight = _extendChromeHints.HasAllFlags(ExtendClientAreaChromeHints.SystemChrome) && !_extendChromeHints.HasAllFlags(ExtendClientAreaChromeHints.PreferSystemChrome) ? borderCaptionThickness.top : 1;

            if (WindowState == WindowState.Maximized)
            {
                _extendedMargins = new Thickness(0, (borderCaptionThickness.top - borderThickness.top) / RenderScaling, 0, 0);
                _offScreenMargin = new Thickness(borderThickness.left / RenderScaling, borderThickness.top / RenderScaling, borderThickness.right / RenderScaling, borderThickness.bottom / RenderScaling);
            }
            else
            {
                _extendedMargins = new Thickness(0, (borderCaptionThickness.top) / RenderScaling, 0, 0);
                _offScreenMargin = new Thickness();
            }

            return margins;
        }

        private void ExtendClientArea()
        {
            if (!_shown)
            {
                return;
            }
            
            if (DwmIsCompositionEnabled(out bool compositionEnabled) < 0 || !compositionEnabled)
            {
                _isClientAreaExtended = false;
                return;
            }
            GetClientRect(_hwnd, out var rcClient);
            GetWindowRect(_hwnd, out var rcWindow);

            // Inform the application of the frame change.
            SetWindowPos(_hwnd,
                IntPtr.Zero,
                rcWindow.left, rcWindow.top,
                rcClient.Width, rcClient.Height,
                SetWindowPosFlags.SWP_FRAMECHANGED | SetWindowPosFlags.SWP_NOACTIVATE);

            if (_isClientAreaExtended && WindowState != WindowState.FullScreen)
            {
                var margins = UpdateExtendMargins();
                DwmExtendFrameIntoClientArea(_hwnd, ref margins);
            }
            else
            {
                var margins = new MARGINS();
                DwmExtendFrameIntoClientArea(_hwnd, ref margins);

                _offScreenMargin = new Thickness();
                _extendedMargins = new Thickness();
                
                Resize(new Size(rcWindow.Width/ RenderScaling, rcWindow.Height / RenderScaling));
            }

            if(!_isClientAreaExtended || (_extendChromeHints.HasAllFlags(ExtendClientAreaChromeHints.SystemChrome) &&
                !_extendChromeHints.HasAllFlags(ExtendClientAreaChromeHints.PreferSystemChrome)))
            {
                EnableCloseButton(_hwnd);
            }
            else
            {
                DisableCloseButton(_hwnd);
            }

            ExtendClientAreaToDecorationsChanged?.Invoke(_isClientAreaExtended);
        }

        private void ShowWindow(WindowState state, bool activate)
        {
            _shown = true;
            
            if (_isClientAreaExtended)
            {
                ExtendClientArea();
            }
            
            ShowWindowCommand? command;

            var newWindowProperties = _windowProperties;

            switch (state)
            {
                case WindowState.Minimized:
                    newWindowProperties.IsFullScreen = false;
                    command = activate ? ShowWindowCommand.Minimize : ShowWindowCommand.ShowMinNoActive;
                    break;
                case WindowState.Maximized:
                    newWindowProperties.IsFullScreen = false;
                    command = ShowWindowCommand.Maximize;
                    break;

                case WindowState.Normal:
                    newWindowProperties.IsFullScreen = false;
                    command = IsWindowVisible(_hwnd) ? ShowWindowCommand.Restore : 
                        activate ? ShowWindowCommand.Normal : ShowWindowCommand.ShowNoActivate;
                    break;

                case WindowState.FullScreen:
                    newWindowProperties.IsFullScreen = true;
                    command = IsWindowVisible(_hwnd) ? (ShowWindowCommand?)null : ShowWindowCommand.Restore;
                    break;

                default:
                    throw new ArgumentException("Invalid WindowState.");
            }

            UpdateWindowProperties(newWindowProperties);

            if (command.HasValue)
            {
                UnmanagedMethods.ShowWindow(_hwnd, command.Value);
            }

            if (state == WindowState.Maximized)
            {
                MaximizeWithoutCoveringTaskbar();
            }

            if (!Design.IsDesignMode && activate)
            {
                SetFocus(_hwnd);
            }
        }
        
        private void BeforeCloseCleanup(bool isDisposing)
        {
            // Based on https://github.com/dotnet/wpf/blob/master/src/Microsoft.DotNet.Wpf/src/PresentationFramework/System/Windows/Window.cs#L4270-L4337
            // We need to enable parent window before destroying child window to prevent OS from activating a random window behind us (or last active window).
            // This is described here: https://docs.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-enablewindow#remarks
            // We need to verify if parent is still alive (perhaps it got destroyed somehow).
            if (_parent != null && IsWindow(_parent._hwnd))
            {
                var wasActive = GetActiveWindow() == _hwnd;

                // We can only set enabled state if we are not disposing - generally Dispose happens after enabled state has been set.
                // Ignoring this would cause us to enable a window that might be disabled.
                if (!isDisposing)
                {
                    // Our window closed callback will set enabled state to a correct value after child window gets destroyed.
                    _parent.SetEnabled(true);
                }
                
                // We also need to activate our parent window since again OS might try to activate a window behind if it is not set.
                if (wasActive)
                {
                    SetActiveWindow(_parent._hwnd);
                }
            }
        }

        private void MaximizeWithoutCoveringTaskbar()
        {
            IntPtr monitor = MonitorFromWindow(_hwnd, MONITOR.MONITOR_DEFAULTTONEAREST);

            if (monitor != IntPtr.Zero)
            {
                var monitorInfo = MONITORINFO.Create();

                if (GetMonitorInfo(monitor, ref monitorInfo))
                {
                    var x = monitorInfo.rcWork.left;
                    var y = monitorInfo.rcWork.top;
                    var cx = Math.Abs(monitorInfo.rcWork.right - x);
                    var cy = Math.Abs(monitorInfo.rcWork.bottom - y);

                    SetWindowPos(_hwnd, WindowPosZOrder.HWND_NOTOPMOST, x, y, cx, cy, SetWindowPosFlags.SWP_SHOWWINDOW);
                }
            }
        }        

        private WindowStyles GetWindowStateStyles()
        {
            return GetStyle() & WindowStateMask;
        }

        private WindowStyles GetStyle()
        {
            if (_isFullScreenActive)
            {
                return _savedWindowInfo.Style;
            }
            else
            {
                return (WindowStyles)GetWindowLong(_hwnd, (int)WindowLongParam.GWL_STYLE);
            }
        }

        private WindowStyles GetExtendedStyle()
        {
            if (_isFullScreenActive)
            {
                return _savedWindowInfo.ExStyle;
            }
            else
            {
                return (WindowStyles)GetWindowLong(_hwnd, (int)WindowLongParam.GWL_EXSTYLE);
            }
        }

        private void SetStyle(WindowStyles style, bool save = true)
        {
            if (save)
            {
                _savedWindowInfo.Style = style;
            }

            if (!_isFullScreenActive)
            {
                SetWindowLong(_hwnd, (int)WindowLongParam.GWL_STYLE, (uint)style);
            }
        }

        private void SetExtendedStyle(WindowStyles style, bool save = true)
        {
            if (save)
            {
                _savedWindowInfo.ExStyle = style;
            }

            if (!_isFullScreenActive)
            {
                SetWindowLong(_hwnd, (int)WindowLongParam.GWL_EXSTYLE, (uint)style);
            }
        }

        private void UpdateWindowProperties(WindowProperties newProperties, bool forceChanges = false)
        {
            var oldProperties = _windowProperties;

            // Calling SetWindowPos will cause events to be sent and we need to respond
            // according to the new values already.
            _windowProperties = newProperties;

            if ((oldProperties.ShowInTaskbar != newProperties.ShowInTaskbar) || forceChanges)
            {
                var exStyle = GetExtendedStyle();

                if (newProperties.ShowInTaskbar)
                {
                    exStyle |= WindowStyles.WS_EX_APPWINDOW;

                    if (_hiddenWindowIsParent)
                    {
                        // Can't enable the taskbar icon by clearing the parent window unless the window
                        // is hidden. Hide the window and show it again with the same activation state
                        // when we've finished. Interestingly it seems to work fine the other way.
                        var shown = IsWindowVisible(_hwnd);
                        var activated = GetActiveWindow() == _hwnd;

                        if (shown)
                            Hide();

                        _hiddenWindowIsParent = false;
                        SetParent(null);

                        if (shown)
                            Show(activated);
                    }
                }
                else
                {
                    // To hide a non-owned window's taskbar icon we need to parent it to a hidden window.
                    if (_parent is null)
                    {
                        SetWindowLongPtr(_hwnd, (int)WindowLongParam.GWL_HWNDPARENT, OffscreenParentWindow.Handle);
                        _hiddenWindowIsParent = true;
                    }

                    exStyle &= ~WindowStyles.WS_EX_APPWINDOW;
                }

                SetExtendedStyle(exStyle);
            }

            WindowStyles style;
            if ((oldProperties.IsResizable != newProperties.IsResizable) || forceChanges)
            {
                style = GetStyle();

                if (newProperties.IsResizable)
                {
                    style |= WindowStyles.WS_SIZEFRAME;
                    style |= WindowStyles.WS_MAXIMIZEBOX;
                }
                else
                {
                    style &= ~WindowStyles.WS_SIZEFRAME;
                    style &= ~WindowStyles.WS_MAXIMIZEBOX;
                }

                SetStyle(style);
            }

            if (oldProperties.IsFullScreen != newProperties.IsFullScreen)
            {
                SetFullScreen(newProperties.IsFullScreen);
            }

            if ((oldProperties.Decorations != newProperties.Decorations) || forceChanges)
            {
                style = GetStyle();

                const WindowStyles fullDecorationFlags = WindowStyles.WS_CAPTION | WindowStyles.WS_SYSMENU;

                if (newProperties.Decorations == SystemDecorations.Full)
                {
                    style |= fullDecorationFlags;
                }
                else
                {
                    style &= ~fullDecorationFlags;
                }

                SetStyle(style);

                if (!_isFullScreenActive)
                {
                    var margin = newProperties.Decorations == SystemDecorations.BorderOnly ? 1 : 0;

                    var margins = new MARGINS
                    {
                        cyBottomHeight = margin,
                        cxRightWidth = margin,
                        cxLeftWidth = margin,
                        cyTopHeight = margin
                    };

                    DwmExtendFrameIntoClientArea(_hwnd, ref margins);

                    GetClientRect(_hwnd, out var oldClientRect);
                    var oldClientRectOrigin = new POINT();
                    ClientToScreen(_hwnd, ref oldClientRectOrigin);
                    oldClientRect.Offset(oldClientRectOrigin);

                    var newRect = oldClientRect;

                    if (newProperties.Decorations == SystemDecorations.Full)
                    {
                        AdjustWindowRectEx(ref newRect, (uint)style, false, (uint)GetExtendedStyle());
                    }

                    SetWindowPos(_hwnd, IntPtr.Zero, newRect.left, newRect.top, newRect.Width, newRect.Height,
                        SetWindowPosFlags.SWP_NOZORDER | SetWindowPosFlags.SWP_NOACTIVATE |
                        SetWindowPosFlags.SWP_FRAMECHANGED);
                }
            }            
        }

        private const int MF_BYCOMMAND = 0x0;
        private const int MF_BYPOSITION = 0x400;
        private const int MF_REMOVE = 0x1000;
        private const int MF_ENABLED = 0x0;
        private const int MF_GRAYED = 0x1;
        private const int MF_DISABLED = 0x2;
        private const int SC_CLOSE = 0xF060;

        void DisableCloseButton(IntPtr hwnd)
        {
            EnableMenuItem(GetSystemMenu(hwnd, false), SC_CLOSE,
                           MF_BYCOMMAND | MF_DISABLED | MF_GRAYED);
        }
        void EnableCloseButton(IntPtr hwnd)
        {
            EnableMenuItem(GetSystemMenu(hwnd, false), SC_CLOSE,
                           MF_BYCOMMAND | MF_ENABLED);
        }

#if USE_MANAGED_DRAG
        private Point ScreenToClient(Point point)
        {
            var p = new UnmanagedMethods.POINT { X = (int)point.X, Y = (int)point.Y };
            UnmanagedMethods.ScreenToClient(_hwnd, ref p);
            return new Point(p.X, p.Y);
        }
#endif

        PixelSize EglGlPlatformSurface.IEglWindowGlPlatformSurfaceInfo.Size
        {
            get
            {
                GetClientRect(_hwnd, out var rect);

                return new PixelSize(
                    Math.Max(1, rect.right - rect.left),
                    Math.Max(1, rect.bottom - rect.top));
            }
        }

        double EglGlPlatformSurface.IEglWindowGlPlatformSurfaceInfo.Scaling => RenderScaling;

        IntPtr EglGlPlatformSurface.IEglWindowGlPlatformSurfaceInfo.Handle => Handle.Handle;

        public void SetExtendClientAreaToDecorationsHint(bool hint)
        {
            _isClientAreaExtended = hint;
            
            ExtendClientArea();            
        }        

        public void SetExtendClientAreaChromeHints(ExtendClientAreaChromeHints hints)
        {
            _extendChromeHints = hints;

            ExtendClientArea();
        }
        
        /// <inheritdoc/>
        public void SetExtendClientAreaTitleBarHeightHint(double titleBarHeight)
        {
            _extendTitleBarHint = titleBarHeight;

            ExtendClientArea();
        }

        /// <inheritdoc/>
        public bool IsClientAreaExtendedToDecorations => _isClientAreaExtended;

        /// <inheritdoc/>
        public Action<bool> ExtendClientAreaToDecorationsChanged { get; set; }
        
        /// <inheritdoc/>
        public bool NeedsManagedDecorations => _isClientAreaExtended && _extendChromeHints.HasAllFlags(ExtendClientAreaChromeHints.PreferSystemChrome);

        /// <inheritdoc/>
        public Thickness ExtendedMargins => _extendedMargins;

        /// <inheritdoc/>
        public Thickness OffScreenMargin => _offScreenMargin;

        /// <inheritdoc/>
        public AcrylicPlatformCompensationLevels AcrylicCompensationLevels { get; } = new AcrylicPlatformCompensationLevels(1, 0.8, 0);

        private struct SavedWindowInfo
        {
            public WindowStyles Style { get; set; }
            public WindowStyles ExStyle { get; set; }
            public RECT WindowRect { get; set; }
        };

        private struct WindowProperties
        {
            public bool ShowInTaskbar;
            public bool IsResizable;
            public SystemDecorations Decorations;
            public bool IsFullScreen;
        }
    }
}
