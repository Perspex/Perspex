using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Disposables;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Controls.Platform;
using Avalonia.Controls.Primitives.PopupPositioning;
using Avalonia.FreeDesktop;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.Input.TextInput;
using Avalonia.OpenGL;
using Avalonia.OpenGL.Egl;
using Avalonia.Platform;
using Avalonia.Rendering;
using Avalonia.Threading;
using Avalonia.X11.Glx;
using static Avalonia.X11.XLib;
// ReSharper disable IdentifierTypo
// ReSharper disable StringLiteralTypo
namespace Avalonia.X11
{
    unsafe partial class X11Window : IWindowImpl, IPopupImpl, IXI2Client,
        ITopLevelImplWithNativeMenuExporter,
        ITopLevelImplWithNativeControlHost,
        ITopLevelImplWithTextInputMethod
    {
        private readonly AvaloniaX11Platform _platform;
        private readonly bool _popup;
        private readonly X11Info _x11;
        private XConfigureEvent? _configure;
        private PixelPoint? _configurePoint;
        private bool _triggeredExpose;
        private IInputRoot _inputRoot;
        private readonly MouseDevice _mouse;
        private readonly TouchDevice _touch;
        private readonly IKeyboardDevice _keyboard;
        private PixelPoint? _position;
        private PixelSize _realSize;
        private IntPtr _handle;
        private IntPtr _xic;
        private IntPtr _renderHandle;
        private bool _mapped;
        private bool _wasMappedAtLeastOnce = false;
        private double? _scalingOverride;
        private bool _disabled;
        private TransparencyHelper _transparencyHelper;

        public object SyncRoot { get; } = new object();

        class InputEventContainer
        {
            public RawInputEventArgs Event;
        }
        private readonly Queue<InputEventContainer> _inputQueue = new Queue<InputEventContainer>();
        private InputEventContainer _lastEvent;
        private bool _useRenderWindow = false;
        public X11Window(AvaloniaX11Platform platform, IWindowImpl popupParent)
        {
            _platform = platform;
            _popup = popupParent != null;
            _x11 = platform.Info;
            _mouse = new MouseDevice();
            _touch = new TouchDevice();
            _keyboard = platform.KeyboardDevice;

            var glfeature = AvaloniaLocator.Current.GetService<IPlatformOpenGlInterface>();
            XSetWindowAttributes attr = new XSetWindowAttributes();
            var valueMask = default(SetWindowValuemask);

            attr.backing_store = 1;
            attr.bit_gravity = Gravity.NorthWestGravity;
            attr.win_gravity = Gravity.NorthWestGravity;
            valueMask |= SetWindowValuemask.BackPixel | SetWindowValuemask.BorderPixel
                         | SetWindowValuemask.BackPixmap | SetWindowValuemask.BackingStore
                         | SetWindowValuemask.BitGravity | SetWindowValuemask.WinGravity;

            if (_popup)
            {
                attr.override_redirect = 1;
                valueMask |= SetWindowValuemask.OverrideRedirect;
            }

            XVisualInfo? visualInfo = null;

            // OpenGL seems to be do weird things to it's current window which breaks resize sometimes
            _useRenderWindow = glfeature != null;
            
            var glx = glfeature as GlxPlatformOpenGlInterface;
            if (glx != null)
                visualInfo = *glx.Display.VisualInfo;
            else if (glfeature == null)
                visualInfo = _x11.TransparentVisualInfo;

            var egl = glfeature as EglPlatformOpenGlInterface;
            
            var visual = IntPtr.Zero;
            var depth = 24;
            if (visualInfo != null)
            {
                visual = visualInfo.Value.visual;
                depth = (int)visualInfo.Value.depth;
                attr.colormap = XCreateColormap(_x11.Display, _x11.RootWindow, visualInfo.Value.visual, 0);
                valueMask |= SetWindowValuemask.ColorMap;   
            }

            int defaultWidth = 0, defaultHeight = 0;

            if (!_popup && Screen != null)
            {
                var monitor = Screen.AllScreens.OrderBy(x => x.PixelDensity)
                   .FirstOrDefault(m => m.Bounds.Contains(Position));

                if (monitor != null)
                {
                    // Emulate Window 7+'s default window size behavior.
                    defaultWidth = (int)(monitor.WorkingArea.Width * 0.75d);
                    defaultHeight = (int)(monitor.WorkingArea.Height * 0.7d);
                }
            }

            // check if the calculated size is zero then compensate to hardcoded resolution
            defaultWidth = Math.Max(defaultWidth, 300);
            defaultHeight = Math.Max(defaultHeight, 200);

            _handle = XCreateWindow(_x11.Display, _x11.RootWindow, 10, 10, defaultWidth, defaultHeight, 0,
                depth,
                (int)CreateWindowArgs.InputOutput, 
                visual,
                new UIntPtr((uint)valueMask), ref attr);

            if (_useRenderWindow)
                _renderHandle = XCreateWindow(_x11.Display, _handle, 0, 0, defaultWidth, defaultHeight, 0, depth,
                    (int)CreateWindowArgs.InputOutput,
                    visual,
                    new UIntPtr((uint)(SetWindowValuemask.BorderPixel | SetWindowValuemask.BitGravity |
                                       SetWindowValuemask.WinGravity | SetWindowValuemask.BackingStore)), ref attr);
            else
                _renderHandle = _handle;
                
            Handle = new PlatformHandle(_handle, "XID");
            _realSize = new PixelSize(defaultWidth, defaultHeight);
            platform.Windows[_handle] = OnEvent;
            XEventMask ignoredMask = XEventMask.SubstructureRedirectMask
                                     | XEventMask.ResizeRedirectMask
                                     | XEventMask.PointerMotionHintMask;
            if (platform.XI2 != null)
                ignoredMask |= platform.XI2.AddWindow(_handle, this);
            var mask = new IntPtr(0xffffff ^ (int)ignoredMask);
            XSelectInput(_x11.Display, _handle, mask);
            var protocols = new[]
            {
                _x11.Atoms.WM_DELETE_WINDOW
            };
            XSetWMProtocols(_x11.Display, _handle, protocols, protocols.Length);
            XChangeProperty(_x11.Display, _handle, _x11.Atoms._NET_WM_WINDOW_TYPE, _x11.Atoms.XA_ATOM,
                32, PropertyMode.Replace, new[] {_x11.Atoms._NET_WM_WINDOW_TYPE_NORMAL}, 1);

            if (platform.Options.WmClass != null)
                SetWmClass(platform.Options.WmClass);

            var surfaces = new List<object>
            {
                new X11FramebufferSurface(_x11.DeferredDisplay, _renderHandle, 
                   depth, () => RenderScaling)
            };
            
            if (egl != null)
                surfaces.Insert(0,
                    new EglGlPlatformSurface(egl,
                        new SurfaceInfo(this, _x11.DeferredDisplay, _handle, _renderHandle)));
            if (glx != null)
                surfaces.Insert(0, new GlxGlPlatformSurface(glx.Display, glx.DeferredContext,
                    new SurfaceInfo(this, _x11.Display, _handle, _renderHandle)));
            
            Surfaces = surfaces.ToArray();
            UpdateMotifHints();
            UpdateSizeHints(null);

            _transparencyHelper = new TransparencyHelper(_x11, _handle, platform.Globals);
            _transparencyHelper.SetTransparencyRequest(WindowTransparencyLevel.None);

            CreateIC();

            XFlush(_x11.Display);
            if(_popup)
                PopupPositioner = new ManagedPopupPositioner(new ManagedPopupPositionerPopupImplHelper(popupParent, MoveResize));
            if (platform.Options.UseDBusMenu)
                NativeMenuExporter = DBusMenuExporter.TryCreate(_handle);
            NativeControlHost = new X11NativeControlHost(_platform, this);
            DispatcherTimer.Run(() =>
            {
                Paint?.Invoke(default);
                return _handle != IntPtr.Zero;
            }, TimeSpan.FromMilliseconds(100));
            InitializeIme();
        }

        class SurfaceInfo  : EglGlPlatformSurface.IEglWindowGlPlatformSurfaceInfo
        {
            private readonly X11Window _window;
            private readonly IntPtr _display;
            private readonly IntPtr _parent;

            public SurfaceInfo(X11Window window, IntPtr display, IntPtr parent, IntPtr xid)
            {
                _window = window;
                _display = display;
                _parent = parent;
                Handle = xid;
            }
            public IntPtr Handle { get; }

            public PixelSize Size
            {
                get
                {
                    XLockDisplay(_display);
                    XGetGeometry(_display, _parent, out var geo);
                    XResizeWindow(_display, Handle, geo.width, geo.height);
                    XUnlockDisplay(_display);
                    return new PixelSize(geo.width, geo.height);
                }
            }

            public double Scaling => _window.RenderScaling;
        }

        void UpdateMotifHints()
        {
            var functions = MotifFunctions.Move | MotifFunctions.Close | MotifFunctions.Resize |
                            MotifFunctions.Minimize | MotifFunctions.Maximize;
            var decorations = MotifDecorations.Menu | MotifDecorations.Title | MotifDecorations.Border |
                              MotifDecorations.Maximize | MotifDecorations.Minimize | MotifDecorations.ResizeH;

            if (_popup 
                || _systemDecorations == SystemDecorations.None) 
                decorations = 0;

            if (!_canResize)
            {
                functions &= ~(MotifFunctions.Resize | MotifFunctions.Maximize);
                decorations &= ~(MotifDecorations.Maximize | MotifDecorations.ResizeH);
            }

            var hints = new MotifWmHints
            {
                flags = new IntPtr((int)(MotifFlags.Decorations | MotifFlags.Functions)),
                decorations = new IntPtr((int)decorations),
                functions = new IntPtr((int)functions)
            };

            XChangeProperty(_x11.Display, _handle,
                _x11.Atoms._MOTIF_WM_HINTS, _x11.Atoms._MOTIF_WM_HINTS, 32,
                PropertyMode.Replace, ref hints, 5);
        }

        void UpdateSizeHints(PixelSize? preResize)
        {
            var min = _minMaxSize.minSize;
            var max = _minMaxSize.maxSize;

            if (!_canResize)
                max = min = _realSize;
            
            if (preResize.HasValue)
            {
                var desired = preResize.Value;
                max = new PixelSize(Math.Max(desired.Width, max.Width), Math.Max(desired.Height, max.Height));
                min = new PixelSize(Math.Min(desired.Width, min.Width), Math.Min(desired.Height, min.Height));
            }

            var hints = new XSizeHints
            {
                min_width = min.Width,
                min_height = min.Height
            };
            hints.height_inc = hints.width_inc = 1;
            var flags = XSizeHintsFlags.PMinSize | XSizeHintsFlags.PResizeInc;
            // People might be passing double.MaxValue
            if (max.Width < 100000 && max.Height < 100000)
            {
                hints.max_width = max.Width;
                hints.max_height = max.Height;
                flags |= XSizeHintsFlags.PMaxSize;
            }

            hints.flags = (IntPtr)flags;

            XSetWMNormalHints(_x11.Display, _handle, ref hints);
        }

        public Size ClientSize => new Size(_realSize.Width / RenderScaling, _realSize.Height / RenderScaling);

        public Size? FrameSize
        {
            get
            {
                XGetWindowProperty(_x11.Display, _handle, _x11.Atoms._NET_FRAME_EXTENTS, IntPtr.Zero,
                    new IntPtr(4), false, (IntPtr)Atom.AnyPropertyType, out var _,
                    out var _, out var nitems, out var _, out var prop);

                if (nitems.ToInt64() != 4)
                {
                    // Window hasn't been mapped by the WM yet, so can't get the extents.
                    return null;
                }

                var data = (IntPtr*)prop.ToPointer();
                var extents = new Thickness(data[0].ToInt32(), data[2].ToInt32(), data[1].ToInt32(), data[3].ToInt32());
                XFree(prop);
                
                return new Size(
                    (_realSize.Width + extents.Left + extents.Right) / RenderScaling,
                    (_realSize.Height + extents.Top + extents.Bottom) / RenderScaling);
            }
        }

        public double RenderScaling
        {
            get
            {
                lock (SyncRoot)
                    return _scaling;

            }
            private set => _scaling = value;
        }
        
        public double DesktopScaling => RenderScaling;

        public IEnumerable<object> Surfaces { get; }
        public Action<RawInputEventArgs> Input { get; set; }
        public Action<Rect> Paint { get; set; }
        public Action<Size> Resized { get; set; }
        //TODO
        public Action<double> ScalingChanged { get; set; }
        public Action Deactivated { get; set; }
        public Action Activated { get; set; }
        public Func<bool> Closing { get; set; }
        public Action<WindowState> WindowStateChanged { get; set; }

        public Action<WindowTransparencyLevel> TransparencyLevelChanged
        {
            get => _transparencyHelper.TransparencyLevelChanged;
            set => _transparencyHelper.TransparencyLevelChanged = value;
        }        

        public Action<bool> ExtendClientAreaToDecorationsChanged { get; set; }

        public Thickness ExtendedMargins { get; } = new Thickness();

        public Thickness OffScreenMargin { get; } = new Thickness();

        public bool IsClientAreaExtendedToDecorations { get; }

        public Action Closed { get; set; }
        public Action<PixelPoint> PositionChanged { get; set; }
        public Action LostFocus { get; set; }

        public IRenderer CreateRenderer(IRenderRoot root)
        {
            var loop = AvaloniaLocator.Current.GetService<IRenderLoop>();
            var customRendererFactory = AvaloniaLocator.Current.GetService<IRendererFactory>();

            if (customRendererFactory != null)
                return customRendererFactory.Create(root, loop);
            
            return _platform.Options.UseDeferredRendering ?
                new DeferredRenderer(root, loop)
                {
                    RenderOnlyOnRenderThread = true
                } :
                (IRenderer)new X11ImmediateRendererProxy(root, loop);
        }

        void OnEvent(ref XEvent ev)
        {
            lock (SyncRoot)
                OnEventSync(ref ev);
        }
        void OnEventSync(ref XEvent ev)
        {
            if (ev.type == XEventName.MapNotify)
            {
                _mapped = true;
                if (_useRenderWindow)
                    XMapWindow(_x11.Display, _renderHandle);
            }
            else if (ev.type == XEventName.UnmapNotify)
                _mapped = false;
            else if (ev.type == XEventName.Expose ||
                     (ev.type == XEventName.VisibilityNotify &&
                      ev.VisibilityEvent.state < 2))
            {
                if (!_triggeredExpose)
                {
                    _triggeredExpose = true;
                    Dispatcher.UIThread.Post(() =>
                    {
                        _triggeredExpose = false;
                        DoPaint();
                    }, DispatcherPriority.Render);
                }
            }
            else if (ev.type == XEventName.FocusIn)
            {
                if (ActivateTransientChildIfNeeded())
                    return;
                Activated?.Invoke();
                _imeControl?.SetWindowActive(true);
            }
            else if (ev.type == XEventName.FocusOut)
            {
                _imeControl?.SetWindowActive(false);
                Deactivated?.Invoke();
            }
            else if (ev.type == XEventName.MotionNotify)
                MouseEvent(RawPointerEventType.Move, ref ev, ev.MotionEvent.state);
            else if (ev.type == XEventName.LeaveNotify)
                MouseEvent(RawPointerEventType.LeaveWindow, ref ev, ev.CrossingEvent.state);
            else if (ev.type == XEventName.PropertyNotify)
            {
                OnPropertyChange(ev.PropertyEvent.atom, ev.PropertyEvent.state == 0);
            }
            else if (ev.type == XEventName.ButtonPress)
            {
                if (ActivateTransientChildIfNeeded())
                    return;
                if (ev.ButtonEvent.button < 4 || ev.ButtonEvent.button == 8 || ev.ButtonEvent.button == 9)
                    MouseEvent(
                        ev.ButtonEvent.button switch
                        {
                            1 => RawPointerEventType.LeftButtonDown,
                            2 => RawPointerEventType.MiddleButtonDown,
                            3 => RawPointerEventType.RightButtonDown,
                            8 => RawPointerEventType.XButton1Down,
                            9 => RawPointerEventType.XButton2Down
                        },
                        ref ev, ev.ButtonEvent.state);
                else
                {
                    var delta = ev.ButtonEvent.button == 4
                        ? new Vector(0, 1)
                        : ev.ButtonEvent.button == 5
                            ? new Vector(0, -1)
                            : ev.ButtonEvent.button == 6
                                ? new Vector(1, 0)
                                : new Vector(-1, 0);
                    ScheduleInput(new RawMouseWheelEventArgs(_mouse, (ulong)ev.ButtonEvent.time.ToInt64(),
                        _inputRoot, new Point(ev.ButtonEvent.x, ev.ButtonEvent.y), delta,
                        TranslateModifiers(ev.ButtonEvent.state)), ref ev);
                }
                
            }
            else if (ev.type == XEventName.ButtonRelease)
            {
                if (ev.ButtonEvent.button < 4 || ev.ButtonEvent.button == 8 || ev.ButtonEvent.button == 9)
                    MouseEvent(
                        ev.ButtonEvent.button switch
                        {
                            1 => RawPointerEventType.LeftButtonUp,
                            2 => RawPointerEventType.MiddleButtonUp,
                            3 => RawPointerEventType.RightButtonUp,
                            8 => RawPointerEventType.XButton1Up,
                            9 => RawPointerEventType.XButton2Up
                        },
                        ref ev, ev.ButtonEvent.state);
            }
            else if (ev.type == XEventName.ConfigureNotify)
            {
                if (ev.ConfigureEvent.window != _handle)
                    return;
                var needEnqueue = (_configure == null);
                _configure = ev.ConfigureEvent;
                if (ev.ConfigureEvent.override_redirect != 0  || ev.ConfigureEvent.send_event != 0)
                    _configurePoint = new PixelPoint(ev.ConfigureEvent.x, ev.ConfigureEvent.y);
                else
                {
                    XTranslateCoordinates(_x11.Display, _handle, _x11.RootWindow,
                        0, 0,
                        out var tx, out var ty, out _);
                    _configurePoint = new PixelPoint(tx, ty);
                }
                if (needEnqueue)
                    Dispatcher.UIThread.Post(() =>
                    {
                        if (_configure == null)
                            return;
                        var cev = _configure.Value;
                        var npos = _configurePoint.Value;
                        _configure = null;
                        _configurePoint = null;
                        
                        var nsize = new PixelSize(cev.width, cev.height);
                        var changedSize = _realSize != nsize;
                        var changedPos = _position == null || npos != _position;
                        _realSize = nsize;
                        _position = npos;
                        bool updatedSizeViaScaling = false;
                        if (changedPos)
                        {
                            PositionChanged?.Invoke(npos);
                            updatedSizeViaScaling = UpdateScaling();
                        }
                        UpdateImePosition();

                        if (changedSize && !updatedSizeViaScaling && !_popup)
                            Resized?.Invoke(ClientSize);

                        Dispatcher.UIThread.RunJobs(DispatcherPriority.Layout);
                    }, DispatcherPriority.Layout);
                if (_useRenderWindow)
                    XConfigureResizeWindow(_x11.Display, _renderHandle, ev.ConfigureEvent.width,
                        ev.ConfigureEvent.height);
            }
            else if (ev.type == XEventName.DestroyNotify 
                     && ev.DestroyWindowEvent.window == _handle)
            {
                Cleanup();
            }
            else if (ev.type == XEventName.ClientMessage)
            {
                if (ev.ClientMessageEvent.message_type == _x11.Atoms.WM_PROTOCOLS)
                {
                    if (ev.ClientMessageEvent.ptr1 == _x11.Atoms.WM_DELETE_WINDOW)
                    {
                        if (Closing?.Invoke() != true)
                            Dispose();
                    }

                }
            }
            else if (ev.type == XEventName.KeyPress || ev.type == XEventName.KeyRelease)
            {
                if (ActivateTransientChildIfNeeded())
                    return;
                HandleKeyEvent(ref ev);
            }
        }

        private bool UpdateScaling(bool skipResize = false)
        {
            lock (SyncRoot)
            {
                double newScaling;
                if (_scalingOverride.HasValue)
                    newScaling = _scalingOverride.Value;
                else
                {
                    var monitor = _platform.X11Screens.Screens.OrderBy(x => x.PixelDensity)
                        .FirstOrDefault(m => m.Bounds.Contains(Position));
                    newScaling = monitor?.PixelDensity ?? RenderScaling;
                }

                if (RenderScaling != newScaling)
                {
                    var oldScaledSize = ClientSize;
                    RenderScaling = newScaling;
                    ScalingChanged?.Invoke(RenderScaling);
                    UpdateImePosition();
                    SetMinMaxSize(_scaledMinMaxSize.minSize, _scaledMinMaxSize.maxSize);
                    if(!skipResize)
                        Resize(oldScaledSize, true);
                    return true;
                }

                return false;
            }
        }

        private WindowState _lastWindowState;
        public WindowState WindowState
        {
            get => _lastWindowState;
            set
            {
                if(_lastWindowState == value)
                    return;
                _lastWindowState = value;
                if (value == WindowState.Minimized)
                {
                    XIconifyWindow(_x11.Display, _handle, _x11.DefaultScreen);
                }
                else if (value == WindowState.Maximized)
                {
                    ChangeWMAtoms(false, _x11.Atoms._NET_WM_STATE_HIDDEN);
                    ChangeWMAtoms(false, _x11.Atoms._NET_WM_STATE_FULLSCREEN);
                    ChangeWMAtoms(true, _x11.Atoms._NET_WM_STATE_MAXIMIZED_VERT,
                        _x11.Atoms._NET_WM_STATE_MAXIMIZED_HORZ);
                }
                else if (value == WindowState.FullScreen)
                {
                    ChangeWMAtoms(false, _x11.Atoms._NET_WM_STATE_HIDDEN);
                    ChangeWMAtoms(true, _x11.Atoms._NET_WM_STATE_FULLSCREEN);
                    ChangeWMAtoms(false, _x11.Atoms._NET_WM_STATE_MAXIMIZED_VERT,
                        _x11.Atoms._NET_WM_STATE_MAXIMIZED_HORZ);
                }
                else
                {
                    ChangeWMAtoms(false, _x11.Atoms._NET_WM_STATE_HIDDEN);
                    ChangeWMAtoms(false, _x11.Atoms._NET_WM_STATE_FULLSCREEN);
                    ChangeWMAtoms(false, _x11.Atoms._NET_WM_STATE_MAXIMIZED_VERT,
                        _x11.Atoms._NET_WM_STATE_MAXIMIZED_HORZ);
                }
            }
        }

        private void OnPropertyChange(IntPtr atom, bool hasValue)
        {
            if (atom == _x11.Atoms._NET_FRAME_EXTENTS)
            {
                // Occurs once the window has been mapped, which is the earliest the extents
                // can be retrieved, so invoke event to force update of TopLevel.FrameSize.
                Resized.Invoke(ClientSize);
            }

            if (atom == _x11.Atoms._NET_WM_STATE)
            {
                WindowState state = WindowState.Normal;
                if(hasValue)
                {

                    XGetWindowProperty(_x11.Display, _handle, _x11.Atoms._NET_WM_STATE, IntPtr.Zero, new IntPtr(256),
                        false, (IntPtr)Atom.XA_ATOM, out _, out _, out var nitems, out _,
                        out var prop);
                    int maximized = 0;
                    var pitems = (IntPtr*)prop.ToPointer();
                    for (var c = 0; c < nitems.ToInt32(); c++)
                    {
                        if (pitems[c] == _x11.Atoms._NET_WM_STATE_HIDDEN)
                        {
                            state = WindowState.Minimized;
                            break;
                        }

                        if(pitems[c] == _x11.Atoms._NET_WM_STATE_FULLSCREEN)
                        {
                            state = WindowState.FullScreen;
                            break;
                        }

                        if (pitems[c] == _x11.Atoms._NET_WM_STATE_MAXIMIZED_HORZ ||
                            pitems[c] == _x11.Atoms._NET_WM_STATE_MAXIMIZED_VERT)
                        {
                            maximized++;
                            if (maximized == 2)
                            {
                                state = WindowState.Maximized;
                                break;
                            }
                        }
                    }
                    XFree(prop);
                }
                if (_lastWindowState != state)
                {
                    _lastWindowState = state;
                    WindowStateChanged?.Invoke(state);
                }
            }

        }

        RawInputModifiers TranslateModifiers(XModifierMask state)
        {
            var rv = default(RawInputModifiers);
            if (state.HasAllFlags(XModifierMask.Button1Mask))
                rv |= RawInputModifiers.LeftMouseButton;
            if (state.HasAllFlags(XModifierMask.Button2Mask))
                rv |= RawInputModifiers.RightMouseButton;
            if (state.HasAllFlags(XModifierMask.Button3Mask))
                rv |= RawInputModifiers.MiddleMouseButton;
            if (state.HasAllFlags(XModifierMask.Button4Mask))
                rv |= RawInputModifiers.XButton1MouseButton;
            if (state.HasAllFlags(XModifierMask.Button5Mask))
                rv |= RawInputModifiers.XButton2MouseButton;
            if (state.HasAllFlags(XModifierMask.ShiftMask))
                rv |= RawInputModifiers.Shift;
            if (state.HasAllFlags(XModifierMask.ControlMask))
                rv |= RawInputModifiers.Control;
            if (state.HasAllFlags(XModifierMask.Mod1Mask))
                rv |= RawInputModifiers.Alt;
            if (state.HasAllFlags(XModifierMask.Mod4Mask))
                rv |= RawInputModifiers.Meta;
            return rv;
        }
        
        private SystemDecorations _systemDecorations = SystemDecorations.Full;
        private bool _canResize = true;
        private const int MaxWindowDimension = 100000;

        private (Size minSize, Size maxSize) _scaledMinMaxSize =
            (new Size(1, 1), new Size(double.PositiveInfinity, double.PositiveInfinity));

        private (PixelSize minSize, PixelSize maxSize) _minMaxSize = (new PixelSize(1, 1),
            new PixelSize(MaxWindowDimension, MaxWindowDimension));
        
        private double _scaling = 1;

        void ScheduleInput(RawInputEventArgs args, ref XEvent xev)
        {
            _x11.LastActivityTimestamp = xev.ButtonEvent.time;
            ScheduleInput(args);
        }
        

        public void ScheduleXI2Input(RawInputEventArgs args)
        {
            if (args is RawPointerEventArgs pargs)
            {
                if ((pargs.Type == RawPointerEventType.TouchBegin
                     || pargs.Type == RawPointerEventType.TouchUpdate
                     || pargs.Type == RawPointerEventType.LeftButtonDown
                     || pargs.Type == RawPointerEventType.RightButtonDown
                     || pargs.Type == RawPointerEventType.MiddleButtonDown
                     || pargs.Type == RawPointerEventType.NonClientLeftButtonDown)
                    && ActivateTransientChildIfNeeded())
                    return;
                if (pargs.Type == RawPointerEventType.TouchEnd
                    && ActivateTransientChildIfNeeded())
                    pargs.Type = RawPointerEventType.TouchCancel;
            }

            ScheduleInput(args);
        }
        
        private void ScheduleInput(RawInputEventArgs args)
        {
            if (args is RawPointerEventArgs mouse)
                mouse.Position = mouse.Position / RenderScaling;
            if (args is RawDragEvent drag)
                drag.Location = drag.Location / RenderScaling;
            
            _lastEvent = new InputEventContainer() {Event = args};
            _inputQueue.Enqueue(_lastEvent);
            if (_inputQueue.Count == 1)
            {
                Dispatcher.UIThread.Post(() =>
                {
                    while (_inputQueue.Count > 0)
                    {
                        Dispatcher.UIThread.RunJobs(DispatcherPriority.Input + 1);
                        var ev = _inputQueue.Dequeue();
                        Input?.Invoke(ev.Event);
                    }
                }, DispatcherPriority.Input);
            }
        }
        
        void MouseEvent(RawPointerEventType type, ref XEvent ev, XModifierMask mods)
        {
            var mev = new RawPointerEventArgs(
                _mouse, (ulong)ev.ButtonEvent.time.ToInt64(), _inputRoot,
                type, new Point(ev.ButtonEvent.x, ev.ButtonEvent.y), TranslateModifiers(mods)); 
            if(type == RawPointerEventType.Move && _inputQueue.Count>0 && _lastEvent.Event is RawPointerEventArgs ma)
                if (ma.Type == RawPointerEventType.Move)
                {
                    _lastEvent.Event = mev;
                    return;
                }
            ScheduleInput(mev, ref ev);
        }

        void DoPaint()
        {
            Paint?.Invoke(new Rect());
        }
        
        public void Invalidate(Rect rect)
        {

        }

        public IInputRoot InputRoot => _inputRoot;
        
        public void SetInputRoot(IInputRoot inputRoot)
        {
            _inputRoot = inputRoot;
        }

        public void Dispose()
        {
            Cleanup();            
        }

        void Cleanup()
        {
            if (_imeControl != null)
            {
                _imeControl.Dispose();
                _imeControl = null;
                _ime = null;
            }
            
            if (_xic != IntPtr.Zero)
            {
                XDestroyIC(_xic);
                _xic = IntPtr.Zero;
            }
            
            if (_handle != IntPtr.Zero)
            {
                XDestroyWindow(_x11.Display, _handle);
                _platform.Windows.Remove(_handle);
                _platform.XI2?.OnWindowDestroyed(_handle);
                _handle = IntPtr.Zero;
                Closed?.Invoke();
                _mouse.Dispose();
                _touch.Dispose();
            }
            
            if (_useRenderWindow && _renderHandle != IntPtr.Zero)
            {                
                _renderHandle = IntPtr.Zero;
            }
        }

        bool ActivateTransientChildIfNeeded()
        {
            if (_disabled)
            {
                GotInputWhenDisabled?.Invoke();
                return true;
            }

            return false;
        }

        public void SetParent(IWindowImpl parent)
        {
            if (parent == null || parent.Handle == null || parent.Handle.Handle == IntPtr.Zero)
                XDeleteProperty(_x11.Display, _handle, _x11.Atoms.XA_WM_TRANSIENT_FOR);
            else
                XSetTransientForHint(_x11.Display, _handle, parent.Handle.Handle);
        }

        public void Show(bool activate)
        {
            _wasMappedAtLeastOnce = true;
            XMapWindow(_x11.Display, _handle);
            XFlush(_x11.Display);
        }

        public void Hide() => XUnmapWindow(_x11.Display, _handle);
        
        public Point PointToClient(PixelPoint point) => new Point((point.X - Position.X) / RenderScaling, (point.Y - Position.Y) / RenderScaling);

        public PixelPoint PointToScreen(Point point) => new PixelPoint(
            (int)(point.X * RenderScaling + Position.X),
            (int)(point.Y * RenderScaling + Position.Y));
        
        public void SetSystemDecorations(SystemDecorations enabled)
        {
            _systemDecorations = enabled == SystemDecorations.Full ? SystemDecorations.Full : SystemDecorations.None;
            UpdateMotifHints();
            UpdateSizeHints(null);
        }


        public void Resize(Size clientSize) => Resize(clientSize, false);
        public void Move(PixelPoint point) => Position = point;
        private void MoveResize(PixelPoint position, Size size, double scaling)
        {
            Move(position);
            _scalingOverride = scaling;
            UpdateScaling(true);
            Resize(size, true);
        }

        PixelSize ToPixelSize(Size size) => new PixelSize((int)(size.Width * RenderScaling), (int)(size.Height * RenderScaling));
        
        void Resize(Size clientSize, bool force)
        {
            if (!force && clientSize == ClientSize)
                return;
            
            var needImmediatePopupResize = clientSize != ClientSize;

            var pixelSize = ToPixelSize(clientSize);
            UpdateSizeHints(pixelSize);
            XConfigureResizeWindow(_x11.Display, _handle, pixelSize);
            if (_useRenderWindow)
                XConfigureResizeWindow(_x11.Display, _renderHandle, pixelSize);
            XFlush(_x11.Display);

            if (force || !_wasMappedAtLeastOnce || (_popup && needImmediatePopupResize))
            {
                _realSize = pixelSize;
                Resized?.Invoke(ClientSize);
            }
        }
        
        public void CanResize(bool value)
        {
            _canResize = value;
            UpdateMotifHints();
            UpdateSizeHints(null);
        }

        public void SetCursor(ICursorImpl cursor)
        {
            if (cursor == null)
                XDefineCursor(_x11.Display, _handle, _x11.DefaultCursor);
            else if (cursor is CursorImpl impl)
            {
                XDefineCursor(_x11.Display, _handle, impl.Handle);
            }
        }

        public IPlatformHandle Handle { get; }
        
        public PixelPoint Position
        {
            get => _position ?? default;
            set
            {
                var changes = new XWindowChanges
                {
                    x = (int)value.X,
                    y = (int)value.Y
                };
                XConfigureWindow(_x11.Display, _handle, ChangeWindowFlags.CWX | ChangeWindowFlags.CWY,
                    ref changes);
                XFlush(_x11.Display);
                if (!_wasMappedAtLeastOnce)
                {
                    _position = value;
                    PositionChanged?.Invoke(value);
                }

            }
        }

        public IMouseDevice MouseDevice => _mouse;
        public TouchDevice TouchDevice => _touch;

        public IPopupImpl CreatePopup() 
            => _platform.Options.OverlayPopups ? null : new X11Window(_platform, this);

        public void Activate()
        {
            if (_x11.Atoms._NET_ACTIVE_WINDOW != IntPtr.Zero)
            {
                SendNetWMMessage(_x11.Atoms._NET_ACTIVE_WINDOW, (IntPtr)1, _x11.LastActivityTimestamp,
                    IntPtr.Zero);
            }
            else
            {
                XRaiseWindow(_x11.Display, _handle);
                XSetInputFocus(_x11.Display, _handle, 0, IntPtr.Zero);
            }
        }


        public IScreenImpl Screen => _platform.Screens;

        public Size MaxAutoSizeHint => _platform.X11Screens.Screens.Select(s => s.Bounds.Size.ToSize(s.PixelDensity))
            .OrderByDescending(x => x.Width + x.Height).FirstOrDefault();


        void SendNetWMMessage(IntPtr message_type, IntPtr l0,
            IntPtr? l1 = null, IntPtr? l2 = null, IntPtr? l3 = null, IntPtr? l4 = null)
        {
            var xev = new XEvent
            {
                ClientMessageEvent =
                {
                    type = XEventName.ClientMessage,
                    send_event = 1,
                    window = _handle,
                    message_type = message_type,
                    format = 32,
                    ptr1 = l0,
                    ptr2 = l1 ?? IntPtr.Zero,
                    ptr3 = l2 ?? IntPtr.Zero,
                    ptr4 = l3 ?? IntPtr.Zero
                }
            };
            xev.ClientMessageEvent.ptr4 = l4 ?? IntPtr.Zero;
            XSendEvent(_x11.Display, _x11.RootWindow, false,
                new IntPtr((int)(EventMask.SubstructureRedirectMask | EventMask.SubstructureNotifyMask)), ref xev);

        }

        void BeginMoveResize(NetWmMoveResize side, PointerPressedEventArgs e)
        {
            var pos = GetCursorPos(_x11);
            XUngrabPointer(_x11.Display, new IntPtr(0));
            SendNetWMMessage (_x11.Atoms._NET_WM_MOVERESIZE, (IntPtr) pos.x, (IntPtr) pos.y,
                (IntPtr) side,
                (IntPtr) 1, (IntPtr)1); // left button
                
            e.Pointer.Capture(null);
        }

        public void BeginMoveDrag(PointerPressedEventArgs e)
        {
            BeginMoveResize(NetWmMoveResize._NET_WM_MOVERESIZE_MOVE, e);
        }

        public void BeginResizeDrag(WindowEdge edge, PointerPressedEventArgs e)
        {
            var side = NetWmMoveResize._NET_WM_MOVERESIZE_CANCEL;
            if (edge == WindowEdge.East)
                side = NetWmMoveResize._NET_WM_MOVERESIZE_SIZE_RIGHT;
            if (edge == WindowEdge.North)
                side = NetWmMoveResize._NET_WM_MOVERESIZE_SIZE_TOP;
            if (edge == WindowEdge.South)
                side = NetWmMoveResize._NET_WM_MOVERESIZE_SIZE_BOTTOM;
            if (edge == WindowEdge.West)
                side = NetWmMoveResize._NET_WM_MOVERESIZE_SIZE_LEFT;
            if (edge == WindowEdge.NorthEast)
                side = NetWmMoveResize._NET_WM_MOVERESIZE_SIZE_TOPRIGHT;
            if (edge == WindowEdge.NorthWest)
                side = NetWmMoveResize._NET_WM_MOVERESIZE_SIZE_TOPLEFT;
            if (edge == WindowEdge.SouthEast)
                side = NetWmMoveResize._NET_WM_MOVERESIZE_SIZE_BOTTOMRIGHT;
            if (edge == WindowEdge.SouthWest)
                side = NetWmMoveResize._NET_WM_MOVERESIZE_SIZE_BOTTOMLEFT;
            BeginMoveResize(side, e);
        }
        
        public void SetTitle(string title)
        {
            var data = Encoding.UTF8.GetBytes(title);
            fixed (void* pdata = data)
            {
                XChangeProperty(_x11.Display, _handle, _x11.Atoms._NET_WM_NAME, _x11.Atoms.UTF8_STRING, 8,
                    PropertyMode.Replace, pdata, data.Length);
                XStoreName(_x11.Display, _handle, title);
            }
        }

        public void SetWmClass(string wmClass)
        {
            var data = Encoding.ASCII.GetBytes(wmClass);
            fixed (void* pdata = data)
            {
                XChangeProperty(_x11.Display, _handle, _x11.Atoms.XA_WM_CLASS, _x11.Atoms.XA_STRING, 8,
                    PropertyMode.Replace, pdata, data.Length);
            }
        }

        public void SetMinMaxSize(Size minSize, Size maxSize)
        {
            _scaledMinMaxSize = (minSize, maxSize);
            var min = new PixelSize(
                (int)(minSize.Width < 1 ? 1 : minSize.Width * RenderScaling),
                (int)(minSize.Height < 1 ? 1 : minSize.Height * RenderScaling));

            const int maxDim = MaxWindowDimension;
            var max = new PixelSize(
                (int)(maxSize.Width > maxDim ? maxDim : Math.Max(min.Width, maxSize.Width * RenderScaling)),
                (int)(maxSize.Height > maxDim ? maxDim : Math.Max(min.Height, maxSize.Height * RenderScaling)));
            
            _minMaxSize = (min, max);
            UpdateSizeHints(null);
        }

        public void SetTopmost(bool value)
        {
            ChangeWMAtoms(value, _x11.Atoms._NET_WM_STATE_ABOVE);
        }
        
        public void SetEnabled(bool enable)
        {
            _disabled = !enable;
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

        public Action GotInputWhenDisabled { get; set; }

        public void SetIcon(IWindowIconImpl icon)
        {
            if (icon != null)
            {
                var data = ((X11IconData)icon).Data;
                fixed (void* pdata = data)
                    XChangeProperty(_x11.Display, _handle, _x11.Atoms._NET_WM_ICON,
                        new IntPtr((int)Atom.XA_CARDINAL), 32, PropertyMode.Replace,
                        pdata, data.Length);
            }
            else
            {
                XDeleteProperty(_x11.Display, _handle, _x11.Atoms._NET_WM_ICON);
            }
        }

        public void ShowTaskbarIcon(bool value)
        {
            ChangeWMAtoms(!value, _x11.Atoms._NET_WM_STATE_SKIP_TASKBAR);
        }

        void ChangeWMAtoms(bool enable, params IntPtr[] atoms)
        {
            if (atoms.Length != 1 && atoms.Length != 2)
                throw new ArgumentException();

            if (!_mapped)
            {
                XGetWindowProperty(_x11.Display, _handle, _x11.Atoms._NET_WM_STATE, IntPtr.Zero, new IntPtr(256),
                    false, (IntPtr)Atom.XA_ATOM, out _, out _, out var nitems, out _,
                    out var prop);
                var ptr = (IntPtr*)prop.ToPointer();
                var newAtoms = new HashSet<IntPtr>();
                for (var c = 0; c < nitems.ToInt64(); c++) 
                    newAtoms.Add(*ptr);
                XFree(prop);
                foreach(var atom in atoms)
                    if (enable)
                        newAtoms.Add(atom);
                    else
                        newAtoms.Remove(atom);

                XChangeProperty(_x11.Display, _handle, _x11.Atoms._NET_WM_STATE, (IntPtr)Atom.XA_ATOM, 32,
                    PropertyMode.Replace, newAtoms.ToArray(), newAtoms.Count);
            }
            
            SendNetWMMessage(_x11.Atoms._NET_WM_STATE,
                (IntPtr)(enable ? 1 : 0),
                atoms[0],
                atoms.Length > 1 ? atoms[1] : IntPtr.Zero,
                atoms.Length > 2 ? atoms[2] : IntPtr.Zero,
                atoms.Length > 3 ? atoms[3] : IntPtr.Zero
            );
        }

        public IPopupPositioner PopupPositioner { get; }
        public ITopLevelNativeMenuExporter NativeMenuExporter { get; }
        public INativeControlHostImpl NativeControlHost { get; }
        public ITextInputMethodImpl TextInputMethod => _ime;

        public void SetTransparencyLevelHint(WindowTransparencyLevel transparencyLevel) =>
            _transparencyHelper.SetTransparencyRequest(transparencyLevel);

        public void SetWindowManagerAddShadowHint(bool enabled)
        {
        }

        public WindowTransparencyLevel TransparencyLevel => _transparencyHelper.CurrentLevel;

        public AcrylicPlatformCompensationLevels AcrylicCompensationLevels { get; } = new AcrylicPlatformCompensationLevels(1, 0.8, 0.8);

        public bool NeedsManagedDecorations => false;

        public Action InputMethodUpdated { get; set; }
    }
}
