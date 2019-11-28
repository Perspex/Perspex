// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.OpenGL;
using Avalonia.Platform;
using Avalonia.Rendering;
using Avalonia.Win32.Input;
using Avalonia.Win32.Interop;
using static Avalonia.Win32.Interop.UnmanagedMethods;

namespace Avalonia.Win32
{
    public class WindowImpl : IWindowImpl, EglGlPlatformSurface.IEglWindowGlPlatformSurfaceInfo
    {
        private static readonly List<WindowImpl> s_instances = new List<WindowImpl>();

        private static readonly IntPtr DefaultCursor = UnmanagedMethods.LoadCursor(
            IntPtr.Zero, new IntPtr((int)UnmanagedMethods.Cursor.IDC_ARROW));

        private UnmanagedMethods.WndProc _wndProcDelegate;
        private string _className;
        private IntPtr _hwnd;
        private bool _multitouch;
        private TouchDevice _touchDevice = new TouchDevice();
        private MouseDevice _mouseDevice = new WindowsMouseDevice();
        private IInputRoot _owner;
        private ManagedDeferredRendererLock _rendererLock = new ManagedDeferredRendererLock();
        private bool _trackingMouse;
        private bool _decorated = true;
        private bool _resizable = true;
        private bool _topmost = false;
        private bool _taskbarIcon = true;
        private double _scaling = 1;
        private WindowState _showWindowState;
        private WindowState _lastWindowState;
        private FramebufferManager _framebuffer;
        private IGlPlatformSurface _gl;
        private OleDropTarget _dropTarget;
        private Size _minSize;
        private Size _maxSize;
        private WindowImpl _parent;
        private readonly List<WindowImpl> _disabledBy = new List<WindowImpl>();

        public WindowImpl()
        {
            CreateWindow();
            _framebuffer = new FramebufferManager(_hwnd);
            if (Win32GlManager.EglFeature != null)
                _gl = new EglGlPlatformSurface((EglDisplay)Win32GlManager.EglFeature.Display,
                    Win32GlManager.EglFeature.DeferredContext, this);

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

        public Func<Point, WindowRegion> ClassifyWindowRegion { get; set; }

        public Thickness BorderThickness
        {
            get
            {
                if (_decorated)
                {
                    var style = UnmanagedMethods.GetWindowLong(_hwnd, (int)UnmanagedMethods.WindowLongParam.GWL_STYLE);
                    var exStyle = UnmanagedMethods.GetWindowLong(_hwnd, (int)UnmanagedMethods.WindowLongParam.GWL_EXSTYLE);

                    var padding = new RECT();

                    if (UnmanagedMethods.AdjustWindowRectEx(ref padding, style, false, exStyle))
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

        public Size ClientSize
        {
            get
            {
                UnmanagedMethods.RECT rect;
                UnmanagedMethods.GetClientRect(_hwnd, out rect);

                if (!_decorated)
                {
                    WINDOWPLACEMENT placement = default;
                    GetWindowPlacement(_hwnd, ref placement);

                    // For historical reasons, Windows will position a maximized decorated window slightly beyond
                    // the edges of the screen. We adjust the client size by this amount to prevent cutting off
                    // window content.
                    if (placement.ShowCmd == ShowWindowCommand.ShowMaximized)
                    {
                        MONITORINFO monitorInfo = MONITORINFO.Create();
                        POINT point = new POINT() { X = 0, Y = 0 };

                        GetMonitorInfo(MonitorFromWindow(_hwnd, MONITOR.MONITOR_DEFAULTTONEAREST), ref monitorInfo);
                        ClientToScreen(_hwnd, ref point);

                        return new Size(
                            rect.right - (monitorInfo.rcWork.left - point.X) * 2,
                            rect.bottom - (monitorInfo.rcWork.top - point.Y) * 2) / Scaling;
                    }
                }

                return new Size(rect.right, rect.bottom) / Scaling;
            }
        }

        public void Move(PixelPoint point) => Position = point;

        public void SetMinMaxSize(Size minSize, Size maxSize)
        {
            _minSize = minSize;
            _maxSize = maxSize;
        }

        public IScreenImpl Screen
        {
            get;
        } = new ScreenImpl();


        public IRenderer CreateRenderer(IRenderRoot root)
        {
            var loop = AvaloniaLocator.Current.GetService<IRenderLoop>();
            var customRendererFactory = AvaloniaLocator.Current.GetService<IRendererFactory>();

            if (customRendererFactory != null)
                return customRendererFactory.Create(root, loop);

            return Win32Platform.UseDeferredRendering ?
                (IRenderer)new DeferredRenderer(root, loop, rendererLock: _rendererLock) :
                new ImmediateRenderer(root);
        }

        public void Resize(Size value)
        {
            int requestedClientWidth = (int)(value.Width * Scaling);
            int requestedClientHeight = (int)(value.Height * Scaling);
            UnmanagedMethods.RECT clientRect;
            UnmanagedMethods.GetClientRect(_hwnd, out clientRect);
           
            // do comparison after scaling to avoid rounding issues
            if (requestedClientWidth != clientRect.Width || requestedClientHeight != clientRect.Height)
            {
                UnmanagedMethods.RECT windowRect;
                UnmanagedMethods.GetWindowRect(_hwnd, out windowRect);

                UnmanagedMethods.SetWindowPos(
                    _hwnd,
                    IntPtr.Zero,
                    0,
                    0,
                    requestedClientWidth + (windowRect.Width - clientRect.Width),
                    requestedClientHeight + (windowRect.Height - clientRect.Height),
                    UnmanagedMethods.SetWindowPosFlags.SWP_RESIZE);
            }
        }

        public double Scaling => _scaling;

        public IPlatformHandle Handle
        {
            get;
            private set;
        }


        void UpdateEnabled()
        {
            EnableWindow(_hwnd, _disabledBy.Count == 0);
        }

        public Size MaxClientSize
        {
            get
            {
                return (new Size(
                    UnmanagedMethods.GetSystemMetrics(UnmanagedMethods.SystemMetric.SM_CXMAXTRACK),
                    UnmanagedMethods.GetSystemMetrics(UnmanagedMethods.SystemMetric.SM_CYMAXTRACK))
                    - BorderThickness) / Scaling;
            }
        }

        public IMouseDevice MouseDevice => _mouseDevice;

        public WindowState WindowState
        {
            get
            {
                var placement = default(UnmanagedMethods.WINDOWPLACEMENT);
                UnmanagedMethods.GetWindowPlacement(_hwnd, ref placement);

                switch (placement.ShowCmd)
                {
                    case UnmanagedMethods.ShowWindowCommand.Maximize:
                        return WindowState.Maximized;
                    case UnmanagedMethods.ShowWindowCommand.Minimize:
                        return WindowState.Minimized;
                    default:
                        return WindowState.Normal;
                }
            }

            set
            {
                if (UnmanagedMethods.IsWindowVisible(_hwnd))
                {
                    ShowWindow(value);
                }
                else
                {
                    _showWindowState = value;
                }
            }
        }

        public IEnumerable<object> Surfaces => new object[]
        {
            Handle, _gl, _framebuffer
        };

        public void Activate()
        {
            UnmanagedMethods.SetActiveWindow(_hwnd);
        }

        public IPopupImpl CreatePopup() => Win32Platform.UseOverlayPopups ? null : new PopupImpl(this);

        public void Dispose()
        {
            if (_hwnd != IntPtr.Zero)
            {
                UnmanagedMethods.DestroyWindow(_hwnd);
                _hwnd = IntPtr.Zero;
            }
            if (_className != null)
            {
                UnmanagedMethods.UnregisterClass(_className, UnmanagedMethods.GetModuleHandle(null));
                _className = null;
            }
        }

        public void Hide()
        {
            if (_parent != null)
            {
                _parent._disabledBy.Remove(this);
                _parent.UpdateEnabled();
                _parent = null;
            }
            UnmanagedMethods.ShowWindow(_hwnd, UnmanagedMethods.ShowWindowCommand.Hide);
        }

        public void SetSystemDecorations(bool value)
        {
            if (value == _decorated)
            {
                return;
            }

            UpdateWMStyles(()=> _decorated = value);
        }

        public void Invalidate(Rect rect)
        {
            var f = Scaling;
            var r = new UnmanagedMethods.RECT
            {
                left = (int)Math.Floor(rect.X * f),
                top = (int)Math.Floor(rect.Y * f),
                right = (int)Math.Ceiling(rect.Right * f),
                bottom = (int)Math.Ceiling(rect.Bottom * f),
            };

            UnmanagedMethods.InvalidateRect(_hwnd, ref r, false);
        }

        public Point PointToClient(PixelPoint point)
        {
            var p = new UnmanagedMethods.POINT { X = (int)point.X, Y = (int)point.Y };
            UnmanagedMethods.ScreenToClient(_hwnd, ref p);
            return new Point(p.X, p.Y) / Scaling;
        }

        public PixelPoint PointToScreen(Point point)
        {
            point *= Scaling;
            var p = new UnmanagedMethods.POINT { X = (int)point.X, Y = (int)point.Y };
            UnmanagedMethods.ClientToScreen(_hwnd, ref p);
            return new PixelPoint(p.X, p.Y);
        }

        public void SetInputRoot(IInputRoot inputRoot)
        {
            _owner = inputRoot;
            CreateDropTarget();
        }

        public void SetTitle(string title)
        {
            UnmanagedMethods.SetWindowText(_hwnd, title);
        }

        public virtual void Show()
        {
            SetWindowLongPtr(_hwnd, (int)WindowLongParam.GWL_HWNDPARENT, IntPtr.Zero);
            ShowWindow(_showWindowState);
        }

        private static readonly Dictionary<WindowRegion, HitTestValues> HitTestValueMap = new Dictionary<WindowRegion, HitTestValues>
        {
            { WindowRegion.RightBorder, HitTestValues.HTRIGHT },
            { WindowRegion.TopBorder, HitTestValues.HTTOP },
            { WindowRegion.TopRightCorner, HitTestValues.HTTOPRIGHT },
            { WindowRegion.TopLeftCorner, HitTestValues.HTTOPLEFT },
            { WindowRegion.BottomBorder, HitTestValues.HTBOTTOM },
            { WindowRegion.BottomRightCorner, HitTestValues.HTBOTTOMRIGHT },
            { WindowRegion.BottomLeftCorner, HitTestValues.HTBOTTOMLEFT },
            { WindowRegion.LeftBorder, HitTestValues.HTLEFT },
            { WindowRegion.TitleBar, HitTestValues.HTCAPTION },
            { WindowRegion.ClientArea, HitTestValues.HTCLIENT }
        };

        public PixelPoint Position
        {
            get
            {
                UnmanagedMethods.RECT rc;
                UnmanagedMethods.GetWindowRect(_hwnd, out rc);
                return new PixelPoint(rc.left, rc.top);
            }
            set
            {
                UnmanagedMethods.SetWindowPos(
                    Handle.Handle,
                    IntPtr.Zero,
                    value.X,
                    value.Y,
                    0,
                    0,
                    UnmanagedMethods.SetWindowPosFlags.SWP_NOSIZE | UnmanagedMethods.SetWindowPosFlags.SWP_NOACTIVATE);

            }
        }

        public void ShowDialog(IWindowImpl parent)
        {
            _parent = (WindowImpl)parent;
            _parent._disabledBy.Add(this);
            _parent.UpdateEnabled();
            SetWindowLongPtr(_hwnd, (int)WindowLongParam.GWL_HWNDPARENT, ((WindowImpl)parent)._hwnd);
            ShowWindow(_showWindowState);
        }

        public void SetCursor(IPlatformHandle cursor)
        {
            var hCursor = cursor?.Handle ?? DefaultCursor;
            UnmanagedMethods.SetClassLong(_hwnd, UnmanagedMethods.ClassLongIndex.GCLP_HCURSOR, hCursor);

            if (_owner.IsPointerOver)
                UnmanagedMethods.SetCursor(hCursor);
        }

        protected virtual IntPtr CreateWindowOverride(ushort atom)
        {
            return UnmanagedMethods.CreateWindowEx(
                0,
                atom,
                null,
                (int)UnmanagedMethods.WindowStyles.WS_OVERLAPPEDWINDOW,
                UnmanagedMethods.CW_USEDEFAULT,
                UnmanagedMethods.CW_USEDEFAULT,
                UnmanagedMethods.CW_USEDEFAULT,
                UnmanagedMethods.CW_USEDEFAULT,
                IntPtr.Zero,
                IntPtr.Zero,
                IntPtr.Zero,
                IntPtr.Zero);
        }

        bool ShouldIgnoreTouchEmulatedMessage()
        {
            if (!_multitouch)
                return false;
            var marker = 0xFF515700L;
            var info = GetMessageExtraInfo().ToInt64();
            return (info & marker) == marker;
        }
        
        [SuppressMessage("Microsoft.StyleCop.CSharp.NamingRules", "SA1305:FieldNamesMustNotUseHungarianNotation", Justification = "Using Win32 naming for consistency.")]
        protected virtual unsafe IntPtr WndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
        {
            bool unicode = UnmanagedMethods.IsWindowUnicode(hWnd);

            const double wheelDelta = 120.0;
            uint timestamp = unchecked((uint)UnmanagedMethods.GetMessageTime());

            RawInputEventArgs e = null;
            
            switch ((UnmanagedMethods.WindowsMessage)msg)
            {
                case UnmanagedMethods.WindowsMessage.WM_ACTIVATE:
                    var wa = (UnmanagedMethods.WindowActivate)(ToInt32(wParam) & 0xffff);

                    switch (wa)
                    {
                        case UnmanagedMethods.WindowActivate.WA_ACTIVE:
                        case UnmanagedMethods.WindowActivate.WA_CLICKACTIVE:
                            Activated?.Invoke();
                            break;

                        case UnmanagedMethods.WindowActivate.WA_INACTIVE:
                            Deactivated?.Invoke();
                            break;
                    }

                    return IntPtr.Zero;

                case WindowsMessage.WM_NCCALCSIZE:
                    if (ToInt32(wParam) == 1 && !_decorated)
                    {
                        return IntPtr.Zero;
                    }
                    break;

                case UnmanagedMethods.WindowsMessage.WM_CLOSE:
                    bool? preventClosing = Closing?.Invoke();
                    if (preventClosing == true)
                    {
                        return IntPtr.Zero;
                    }
                    break;

                case UnmanagedMethods.WindowsMessage.WM_DESTROY:
                    //Window doesn't exist anymore
                    _hwnd = IntPtr.Zero;
                    //Remove root reference to this class, so unmanaged delegate can be collected
                    s_instances.Remove(this);
                    Closed?.Invoke();
                    if (_parent != null)
                    {
                        _parent._disabledBy.Remove(this);
                        _parent.UpdateEnabled();
                    }
                    _mouseDevice.Dispose();
                    _touchDevice?.Dispose();
                    //Free other resources
                    Dispose();
                    return IntPtr.Zero;

                case UnmanagedMethods.WindowsMessage.WM_DPICHANGED:
                    var dpi = ToInt32(wParam) & 0xffff;
                    var newDisplayRect = Marshal.PtrToStructure<UnmanagedMethods.RECT>(lParam);
                    _scaling = dpi / 96.0;
                    ScalingChanged?.Invoke(_scaling);
                    SetWindowPos(hWnd,
                        IntPtr.Zero,
                        newDisplayRect.left,
                        newDisplayRect.top,
                        newDisplayRect.right - newDisplayRect.left,
                        newDisplayRect.bottom - newDisplayRect.top,
                        SetWindowPosFlags.SWP_NOZORDER | SetWindowPosFlags.SWP_NOACTIVATE);
                    return IntPtr.Zero;

                case UnmanagedMethods.WindowsMessage.WM_KEYDOWN:
                case UnmanagedMethods.WindowsMessage.WM_SYSKEYDOWN:
                    e = new RawKeyEventArgs(
                            WindowsKeyboardDevice.Instance,
                            timestamp,
                            _owner,
                            RawKeyEventType.KeyDown,
                            KeyInterop.KeyFromVirtualKey(ToInt32(wParam)), WindowsKeyboardDevice.Instance.Modifiers);
                    break;

                case UnmanagedMethods.WindowsMessage.WM_MENUCHAR:
                    // mute the system beep
                    return (IntPtr)((Int32)UnmanagedMethods.MenuCharParam.MNC_CLOSE << 16);

                case UnmanagedMethods.WindowsMessage.WM_KEYUP:
                case UnmanagedMethods.WindowsMessage.WM_SYSKEYUP:
                    e = new RawKeyEventArgs(
                            WindowsKeyboardDevice.Instance,
                            timestamp,
                            _owner,
                            RawKeyEventType.KeyUp,
                            KeyInterop.KeyFromVirtualKey(ToInt32(wParam)), WindowsKeyboardDevice.Instance.Modifiers);
                    break;
                case UnmanagedMethods.WindowsMessage.WM_CHAR:
                    // Ignore control chars
                    if (ToInt32(wParam) >= 32)
                    {
                        e = new RawTextInputEventArgs(WindowsKeyboardDevice.Instance, timestamp, _owner,
                            new string((char)ToInt32(wParam), 1));
                    }

                    break;

                case UnmanagedMethods.WindowsMessage.WM_LBUTTONDOWN:
                case UnmanagedMethods.WindowsMessage.WM_RBUTTONDOWN:
                case UnmanagedMethods.WindowsMessage.WM_MBUTTONDOWN:
                    if(ShouldIgnoreTouchEmulatedMessage())
                        break;
                    e = new RawPointerEventArgs(
                        _mouseDevice,
                        timestamp,
                        _owner,
                        msg == (int)UnmanagedMethods.WindowsMessage.WM_LBUTTONDOWN
                            ? RawPointerEventType.LeftButtonDown
                            : msg == (int)UnmanagedMethods.WindowsMessage.WM_RBUTTONDOWN
                                ? RawPointerEventType.RightButtonDown
                                : RawPointerEventType.MiddleButtonDown,
                        DipFromLParam(lParam), GetMouseModifiers(wParam));
                    break;

                case UnmanagedMethods.WindowsMessage.WM_LBUTTONUP:
                case UnmanagedMethods.WindowsMessage.WM_RBUTTONUP:
                case UnmanagedMethods.WindowsMessage.WM_MBUTTONUP:
                    if(ShouldIgnoreTouchEmulatedMessage())
                        break;
                    e = new RawPointerEventArgs(
                        _mouseDevice,
                        timestamp,
                        _owner,
                        msg == (int)UnmanagedMethods.WindowsMessage.WM_LBUTTONUP
                            ? RawPointerEventType.LeftButtonUp
                            : msg == (int)UnmanagedMethods.WindowsMessage.WM_RBUTTONUP
                                ? RawPointerEventType.RightButtonUp
                                : RawPointerEventType.MiddleButtonUp,
                        DipFromLParam(lParam), GetMouseModifiers(wParam));
                    break;

                case UnmanagedMethods.WindowsMessage.WM_MOUSEMOVE:
                    if(ShouldIgnoreTouchEmulatedMessage())
                        break;
                    if (!_trackingMouse)
                    {
                        var tm = new UnmanagedMethods.TRACKMOUSEEVENT
                        {
                            cbSize = Marshal.SizeOf<UnmanagedMethods.TRACKMOUSEEVENT>(),
                            dwFlags = 2,
                            hwndTrack = _hwnd,
                            dwHoverTime = 0,
                        };

                        UnmanagedMethods.TrackMouseEvent(ref tm);
                    }

                    e = new RawPointerEventArgs(
                        _mouseDevice,
                        timestamp,
                        _owner,
                        RawPointerEventType.Move,
                        DipFromLParam(lParam), GetMouseModifiers(wParam));

                    break;

                case UnmanagedMethods.WindowsMessage.WM_MOUSEWHEEL:
                    e = new RawMouseWheelEventArgs(
                        _mouseDevice,
                        timestamp,
                        _owner,
                        PointToClient(PointFromLParam(lParam)),
                        new Vector(0, (ToInt32(wParam) >> 16) / wheelDelta), GetMouseModifiers(wParam));
                    break;

                case UnmanagedMethods.WindowsMessage.WM_MOUSEHWHEEL:
                    e = new RawMouseWheelEventArgs(
                        _mouseDevice,
                        timestamp,
                        _owner,
                        PointToClient(PointFromLParam(lParam)),
                        new Vector(-(ToInt32(wParam) >> 16) / wheelDelta, 0), GetMouseModifiers(wParam));
                    break;

                case UnmanagedMethods.WindowsMessage.WM_MOUSELEAVE:
                    _trackingMouse = false;
                    e = new RawPointerEventArgs(
                        _mouseDevice,
                        timestamp,
                        _owner,
                        RawPointerEventType.LeaveWindow,
                        new Point(-1,-1), WindowsKeyboardDevice.Instance.Modifiers);
                    break;

                case UnmanagedMethods.WindowsMessage.WM_NCLBUTTONDOWN:
                case UnmanagedMethods.WindowsMessage.WM_NCRBUTTONDOWN:
                case UnmanagedMethods.WindowsMessage.WM_NCMBUTTONDOWN:
                    e = new RawPointerEventArgs(
                        _mouseDevice,
                        timestamp,
                        _owner,
                        msg == (int)UnmanagedMethods.WindowsMessage.WM_NCLBUTTONDOWN
                            ? RawPointerEventType.NonClientLeftButtonDown
                            : msg == (int)UnmanagedMethods.WindowsMessage.WM_NCRBUTTONDOWN
                                ? RawPointerEventType.RightButtonDown
                                : RawPointerEventType.MiddleButtonDown,
                        PointToClient(PointFromLParam(lParam)), GetMouseModifiers(wParam));
                    break;

                case WindowsMessage.WM_TOUCH:
                    var touchInputCount = wParam.ToInt32();

                    var pTouchInputs = stackalloc TOUCHINPUT[touchInputCount];
                    var touchInputs = new Span<TOUCHINPUT>(pTouchInputs, touchInputCount);

                    if (GetTouchInputInfo(lParam, (uint)touchInputCount, pTouchInputs, Marshal.SizeOf<TOUCHINPUT>()))
                    {
                        foreach (var touchInput in touchInputs)
                        {
                            Input?.Invoke(new RawTouchEventArgs(_touchDevice, touchInput.Time,
                                _owner,
                                touchInput.Flags.HasFlagCustom(TouchInputFlags.TOUCHEVENTF_UP) ?
                                    RawPointerEventType.TouchEnd :
                                    touchInput.Flags.HasFlagCustom(TouchInputFlags.TOUCHEVENTF_DOWN) ?
                                        RawPointerEventType.TouchBegin :
                                        RawPointerEventType.TouchUpdate,
                                PointToClient(new PixelPoint(touchInput.X / 100, touchInput.Y / 100)),
                                WindowsKeyboardDevice.Instance.Modifiers,
                                touchInput.Id));
                        }
                        CloseTouchInputHandle(lParam);
                        return IntPtr.Zero;
                    }
                    break;

                case WindowsMessage.WM_NCACTIVATE:
                    if (!_decorated)
                    {
                        return new IntPtr(1);
                    }
                    break;

                case WindowsMessage.WM_NCHITTEST:
                    var classifyWindowRegion = ClassifyWindowRegion;
                    if (classifyWindowRegion != null)
                    {
                        long value = lParam.ToInt64();
                        int x = (short)(value & 0xffff);
                        int y = (short)(value >> 16);
                        var point = ScreenToClient(new Point(x, y));
                        var windowRegion = classifyWindowRegion(new Point(point.X / _scaling, point.Y / _scaling));
                        return new IntPtr((int)HitTestValueMap[windowRegion]);
                    }
                    break;

                case UnmanagedMethods.WindowsMessage.WM_PAINT:
                    using (_rendererLock.Lock())
                    {
                        UnmanagedMethods.PAINTSTRUCT ps;
                        if (UnmanagedMethods.BeginPaint(_hwnd, out ps) != IntPtr.Zero)
                        {
                            var f = Scaling;
                            var r = ps.rcPaint;
                            Paint?.Invoke(new Rect(r.left / f, r.top / f, (r.right - r.left) / f,
                                (r.bottom - r.top) / f));
                            UnmanagedMethods.EndPaint(_hwnd, ref ps);
                        }
                    }

                    return IntPtr.Zero;

                case UnmanagedMethods.WindowsMessage.WM_SIZE:
                    using (_rendererLock.Lock())
                    {
                        // Do nothing here, just block until the pending frame render is completed on the render thread
                    }

                    var size = (UnmanagedMethods.SizeCommand)wParam;

                    var windowState = size == SizeCommand.Maximized ? WindowState.Maximized
                        : (size == SizeCommand.Minimized ? WindowState.Minimized : WindowState.Normal);

                    // If a custom ClassifyWindowRegion method is provided, the window can potentially be resized while it
                    // is still maximized. In this case, the window doesn't immediately lose its maximized state. When this
                    // happens, let's just restore the window from the maximized state.
                    if (windowState == WindowState.Maximized && _lastWindowState == WindowState.Maximized)
                    {
                        UnmanagedMethods.ShowWindow(_hwnd, ShowWindowCommand.Restore);
                        break;
                    }

                    if (Resized != null &&
                        (size == UnmanagedMethods.SizeCommand.Restored ||
                         size == UnmanagedMethods.SizeCommand.Maximized))
                    {
                        var clientSize = new Size(ToInt32(lParam) & 0xffff, ToInt32(lParam) >> 16);
                        Resized(clientSize / Scaling);
                    }

                    if (windowState != _lastWindowState)
                    {
                        _lastWindowState = windowState;
                        WindowStateChanged?.Invoke(windowState);
                    }

                    return IntPtr.Zero;

                case UnmanagedMethods.WindowsMessage.WM_MOVE:
                    PositionChanged?.Invoke(new PixelPoint((short)(ToInt32(lParam) & 0xffff), (short)(ToInt32(lParam) >> 16)));
                    return IntPtr.Zero;

                case UnmanagedMethods.WindowsMessage.WM_GETMINMAXINFO:
                    MINMAXINFO mmi = Marshal.PtrToStructure<UnmanagedMethods.MINMAXINFO>(lParam);

                    if (_minSize.Width > 0)
                        mmi.ptMinTrackSize.X = (int)((_minSize.Width * Scaling) + BorderThickness.Left + BorderThickness.Right);

                    if (_minSize.Height > 0)
                        mmi.ptMinTrackSize.Y = (int)((_minSize.Height * Scaling) + BorderThickness.Top + BorderThickness.Bottom);

                    if (!Double.IsInfinity(_maxSize.Width) && _maxSize.Width > 0)
                        mmi.ptMaxTrackSize.X = (int)((_maxSize.Width * Scaling) + BorderThickness.Left + BorderThickness.Right);

                    if (!Double.IsInfinity(_maxSize.Height) && _maxSize.Height > 0)
                        mmi.ptMaxTrackSize.Y = (int)((_maxSize.Height * Scaling) + BorderThickness.Top + BorderThickness.Bottom);

                    Marshal.StructureToPtr(mmi, lParam, true);
                    return IntPtr.Zero;

                case UnmanagedMethods.WindowsMessage.WM_DISPLAYCHANGE:
                    (Screen as ScreenImpl)?.InvalidateScreensCache();
                    return IntPtr.Zero;

                case UnmanagedMethods.WindowsMessage.WM_DWMCOMPOSITIONCHANGED:
                {
                    var margins = new MARGINS();

                    if (!_decorated)
                    {
                        margins = new MARGINS()
                        {
                            topHeight = 1,
                            leftWidth = 1,
                            rightWidth = 1,
                            bottomHeight = 1
                        };
                    }

                    DwmExtendFrameIntoClientArea(_hwnd, ref margins);
                    return IntPtr.Zero;
                }
            }

            if (e != null && Input != null)
            {
                Input(e);

                if (e.Handled)
                {
                    return IntPtr.Zero;
                }
            }

            using (_rendererLock.Lock())
                return UnmanagedMethods.DefWindowProc(hWnd, msg, wParam, lParam);
        }

        static RawInputModifiers GetMouseModifiers(IntPtr wParam)
        {
            var keys = (UnmanagedMethods.ModifierKeys)ToInt32(wParam);
            var modifiers = WindowsKeyboardDevice.Instance.Modifiers;
            if (keys.HasFlagCustom(UnmanagedMethods.ModifierKeys.MK_LBUTTON))
                modifiers |= RawInputModifiers.LeftMouseButton;
            if (keys.HasFlagCustom(UnmanagedMethods.ModifierKeys.MK_RBUTTON))
                modifiers |= RawInputModifiers.RightMouseButton;
            if (keys.HasFlagCustom(UnmanagedMethods.ModifierKeys.MK_MBUTTON))
                modifiers |= RawInputModifiers.MiddleMouseButton;
            return modifiers;
        }

        private void CreateWindow()
        {
            // Ensure that the delegate doesn't get garbage collected by storing it as a field.
            _wndProcDelegate = new UnmanagedMethods.WndProc(WndProc);

            _className = $"Avalonia-{Guid.NewGuid().ToString()}";

            UnmanagedMethods.WNDCLASSEX wndClassEx = new UnmanagedMethods.WNDCLASSEX
            {
                cbSize = Marshal.SizeOf<UnmanagedMethods.WNDCLASSEX>(),
                style = (int)(ClassStyles.CS_OWNDC | ClassStyles.CS_HREDRAW | ClassStyles.CS_VREDRAW), // Unique DC helps with performance when using Gpu based rendering
                lpfnWndProc = _wndProcDelegate,
                hInstance = UnmanagedMethods.GetModuleHandle(null),
                hCursor = DefaultCursor,
                hbrBackground = IntPtr.Zero,
                lpszClassName = _className
            };

            ushort atom = UnmanagedMethods.RegisterClassEx(ref wndClassEx);

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

            _multitouch = Win32Platform.Options.EnableMultitouch ?? false;
            if (_multitouch)
                RegisterTouchWindow(_hwnd, 0);
            
            if (UnmanagedMethods.ShCoreAvailable)
            {
                uint dpix, dpiy;

                var monitor = UnmanagedMethods.MonitorFromWindow(
                    _hwnd,
                    UnmanagedMethods.MONITOR.MONITOR_DEFAULTTONEAREST);

                if (UnmanagedMethods.GetDpiForMonitor(
                        monitor,
                        UnmanagedMethods.MONITOR_DPI_TYPE.MDT_EFFECTIVE_DPI,
                        out dpix,
                        out dpiy) == 0)
                {
                    _scaling = dpix / 96.0;
                }
            }
        }

        private void CreateDropTarget()
        {
            OleDropTarget odt = new OleDropTarget(this, _owner);
            if (OleContext.Current?.RegisterDragDrop(Handle, odt) ?? false)
                _dropTarget = odt;
        }

        private Point DipFromLParam(IntPtr lParam)
        {
            return new Point((short)(ToInt32(lParam) & 0xffff), (short)(ToInt32(lParam) >> 16)) / Scaling;
        }

        private PixelPoint PointFromLParam(IntPtr lParam)
        {
            return new PixelPoint((short)(ToInt32(lParam) & 0xffff), (short)(ToInt32(lParam) >> 16));
        }

        private Point ScreenToClient(Point point)
        {
            var p = new UnmanagedMethods.POINT { X = (int)point.X, Y = (int)point.Y };
            UnmanagedMethods.ScreenToClient(_hwnd, ref p);
            return new Point(p.X, p.Y);
        }

        private void ShowWindow(WindowState state)
        {
            UnmanagedMethods.ShowWindowCommand command;

            switch (state)
            {
                case WindowState.Minimized:
                    command = ShowWindowCommand.Minimize;
                    break;
                case WindowState.Maximized:
                    command = ShowWindowCommand.Maximize;
                    break;

                case WindowState.Normal:
                    command = ShowWindowCommand.Restore;
                    break;

                default:
                    throw new ArgumentException("Invalid WindowState.");
            }

            UnmanagedMethods.ShowWindow(_hwnd, command);

            if (state == WindowState.Maximized)
            {
                MaximizeWithoutCoveringTaskbar();
            }

            if (!Design.IsDesignMode)
            {
                SetFocus(_hwnd);
            }
        }

        private void MaximizeWithoutCoveringTaskbar()
        {
            IntPtr monitor = MonitorFromWindow(_hwnd, MONITOR.MONITOR_DEFAULTTONEAREST);

            if (monitor != IntPtr.Zero)
            {
                MONITORINFO monitorInfo = MONITORINFO.Create();

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

        public void SetIcon(IWindowIconImpl icon)
        {
            var impl = (IconImpl)icon;
            var hIcon = impl.HIcon;
            UnmanagedMethods.PostMessage(_hwnd, (int)UnmanagedMethods.WindowsMessage.WM_SETICON,
                new IntPtr((int)UnmanagedMethods.Icons.ICON_BIG), hIcon);
        }

        private static int ToInt32(IntPtr ptr)
        {
            if (IntPtr.Size == 4) return ptr.ToInt32();

            return (int)(ptr.ToInt64() & 0xffffffff);
        }


        public void ShowTaskbarIcon(bool value)
        {
            if (_taskbarIcon == value)
            {
                return;
            }

            _taskbarIcon = value;

            var style = (UnmanagedMethods.WindowStyles)UnmanagedMethods.GetWindowLong(_hwnd, (int)UnmanagedMethods.WindowLongParam.GWL_EXSTYLE);

            style &= ~(UnmanagedMethods.WindowStyles.WS_VISIBLE);

            style |= UnmanagedMethods.WindowStyles.WS_EX_TOOLWINDOW;

            if (value)
                style |= UnmanagedMethods.WindowStyles.WS_EX_APPWINDOW;
            else
                style &= ~(UnmanagedMethods.WindowStyles.WS_EX_APPWINDOW);

            WINDOWPLACEMENT windowPlacement = UnmanagedMethods.WINDOWPLACEMENT.Default;
            if (UnmanagedMethods.GetWindowPlacement(_hwnd, ref windowPlacement))
            {
                //Toggle to make the styles stick
                UnmanagedMethods.ShowWindow(_hwnd, ShowWindowCommand.Hide);
                UnmanagedMethods.SetWindowLong(_hwnd, (int)UnmanagedMethods.WindowLongParam.GWL_EXSTYLE, (uint)style);
                UnmanagedMethods.ShowWindow(_hwnd, windowPlacement.ShowCmd);
            }
        }

        private void UpdateWMStyles(Action change)
        {
            var oldDecorated = _decorated;

            change();

            var style = (WindowStyles)GetWindowLong(_hwnd, (int)WindowLongParam.GWL_STYLE);

            style |= WindowStyles.WS_OVERLAPPEDWINDOW;

            if (!_resizable)
            {
                style &= ~(WindowStyles.WS_SIZEFRAME | WindowStyles.WS_MAXIMIZEBOX);
            }

            GetClientRect(_hwnd, out var oldClientRect);
            var oldClientRectOrigin = new UnmanagedMethods.POINT();
            ClientToScreen(_hwnd, ref oldClientRectOrigin);
            oldClientRect.Offset(oldClientRectOrigin);
            
            SetWindowLong(_hwnd, (int)WindowLongParam.GWL_STYLE, (uint)style);

            bool frameUpdated = false;
            if (oldDecorated != _decorated)
            {
                var newRect = oldClientRect;
                var margins = new MARGINS();

                if (_decorated)
                {
                    AdjustWindowRectEx(
                        ref newRect, (uint)style, false, GetWindowLong(_hwnd, (int)WindowLongParam.GWL_EXSTYLE));
                }
                else
                {
                    margins = new MARGINS()
                    {
                        topHeight = 1,
                        leftWidth = 1,
                        rightWidth = 1,
                        bottomHeight = 1
                    };
                }

                DwmExtendFrameIntoClientArea(_hwnd, ref margins);

                SetWindowPos(_hwnd, IntPtr.Zero, newRect.left, newRect.top, newRect.Width, newRect.Height,
                    SetWindowPosFlags.SWP_NOZORDER | SetWindowPosFlags.SWP_NOACTIVATE | SetWindowPosFlags.SWP_FRAMECHANGED);
                frameUpdated = true;
            }

            if (!frameUpdated)
                SetWindowPos(_hwnd, IntPtr.Zero, 0, 0, 0, 0,
                    SetWindowPosFlags.SWP_FRAMECHANGED | SetWindowPosFlags.SWP_NOZORDER |
                    SetWindowPosFlags.SWP_NOACTIVATE
                    | SetWindowPosFlags.SWP_NOMOVE | SetWindowPosFlags.SWP_NOSIZE);
        }

        public void CanResize(bool value)
        {
            if (value == _resizable)
            {
                return;
            }

            UpdateWMStyles(()=> _resizable = value);
        }

        public void SetTopmost(bool value)
        {
            if (value == _topmost)
            {
                return;
            }

            IntPtr hWndInsertAfter = value ? WindowPosZOrder.HWND_TOPMOST : WindowPosZOrder.HWND_NOTOPMOST;
            UnmanagedMethods.SetWindowPos(_hwnd,
                   hWndInsertAfter,
                   0, 0, 0, 0,
                   SetWindowPosFlags.SWP_NOMOVE | SetWindowPosFlags.SWP_NOSIZE | SetWindowPosFlags.SWP_NOACTIVATE);

            _topmost = value;
        }

        PixelSize EglGlPlatformSurface.IEglWindowGlPlatformSurfaceInfo.Size
        {
            get
            {
                RECT rect;
                GetClientRect(_hwnd, out rect);
                return new PixelSize(
                    Math.Max(1, rect.right - rect.left),
                    Math.Max(1, rect.bottom - rect.top));
            }
        }
        IntPtr EglGlPlatformSurface.IEglWindowGlPlatformSurfaceInfo.Handle => Handle.Handle;
    }
}
