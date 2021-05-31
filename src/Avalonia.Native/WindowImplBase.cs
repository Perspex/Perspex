using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Platform;
using Avalonia.Controls;
using Avalonia.Controls.Platform;
using Avalonia.Controls.Platform.Surfaces;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.Native.Interop;
using Avalonia.OpenGL;
using Avalonia.Platform;
using Avalonia.Rendering;
using Avalonia.Threading;

namespace Avalonia.Native
{
    internal class MacOSTopLevelWindowHandle : IPlatformHandle, IMacOSTopLevelPlatformHandle
    {
        IAvnWindowBase _native;

        public MacOSTopLevelWindowHandle(IAvnWindowBase native)
        {
            _native = native;
        }

        public IntPtr Handle => NSWindow;

        public string HandleDescriptor => "NSWindow";

        public IntPtr NSView => _native.ObtainNSViewHandle();

        public IntPtr NSWindow => _native.ObtainNSWindowHandle();

        public IntPtr GetNSViewRetained()
        {
            return _native.ObtainNSViewHandleRetained();
        }

        public IntPtr GetNSWindowRetained()
        {
            return _native.ObtainNSWindowHandleRetained();
        }
    }

    internal abstract class WindowBaseImpl : IWindowBaseImpl,
        IFramebufferPlatformSurface, ITopLevelImplWithNativeControlHost
    {
        protected readonly IAvaloniaNativeFactory _factory;
        protected IInputRoot _inputRoot;
        IAvnWindowBase _native;
        private object _syncRoot = new object();
        private bool _deferredRendering = false;
        private bool _gpu = false;
        private readonly MouseDevice _mouse;
        private readonly IKeyboardDevice _keyboard;
        private readonly ICursorFactory _cursorFactory;
        private Size _savedLogicalSize;
        private Size _lastRenderedLogicalSize;
        private double _savedScaling;
        private GlPlatformSurface _glSurface;
        private NativeControlHostImpl _nativeControlHost;
        private IGlContext _glContext;
        private IAutomationNode _automationNode;
        private AvnAutomationPeer _automationPeer;
        private Func<IAutomationNode, AutomationPeer> _automationStarted;

        internal WindowBaseImpl(IAvaloniaNativeFactory factory, AvaloniaNativePlatformOptions opts,
            AvaloniaNativePlatformOpenGlInterface glFeature)
        {
            _factory = factory;
            _gpu = opts.UseGpu && glFeature != null;
            _deferredRendering = opts.UseDeferredRendering;

            _keyboard = AvaloniaLocator.Current.GetService<IKeyboardDevice>();
            _mouse = new MouseDevice();
            _cursorFactory = AvaloniaLocator.Current.GetService<ICursorFactory>();
        }

        protected void Init(IAvnWindowBase window, IAvnScreens screens, IGlContext glContext)
        {
            _native = window;
            _glContext = glContext;

            Handle = new MacOSTopLevelWindowHandle(window);
            if (_gpu)
                _glSurface = new GlPlatformSurface(window, _glContext);
            Screen = new ScreenImpl(screens);
            _savedLogicalSize = ClientSize;
            _savedScaling = RenderScaling;
            _nativeControlHost = new NativeControlHostImpl(_native.CreateNativeControlHost());

            var monitor = Screen.AllScreens.OrderBy(x => x.PixelDensity)
                    .FirstOrDefault(m => m.Bounds.Contains(Position));

            Resize(new Size(monitor.WorkingArea.Width * 0.75d, monitor.WorkingArea.Height * 0.7d));
        }

        public IAvnWindowBase Native => _native;

        public Size ClientSize 
        {
            get
            {
                if (_native != null)
                {
                    var s = _native.ClientSize;
                    return new Size(s.Width, s.Height);
                }

                return default;
            }
        }

        public IEnumerable<object> Surfaces => new[] {
            (_gpu ? _glSurface : (object)null),
            this 
        };

        public INativeControlHostImpl NativeControlHost => _nativeControlHost;

        public ILockedFramebuffer Lock()
        {
            var w = _savedLogicalSize.Width * _savedScaling;
            var h = _savedLogicalSize.Height * _savedScaling;
            var dpi = _savedScaling * 96;
            return new DeferredFramebuffer(cb =>
            {
                lock (_syncRoot)
                {
                    if (_native == null)
                        return false;
                    cb(_native);
                    _lastRenderedLogicalSize = _savedLogicalSize;
                    return true;
                }
            }, (int)w, (int)h, new Vector(dpi, dpi));
        }

        public Action LostFocus { get; set; }
        
        public Action<Rect> Paint { get; set; }
        public Action<Size> Resized { get; set; }
        public Action Closed { get; set; }
        public IMouseDevice MouseDevice => _mouse;
        public abstract IPopupImpl CreatePopup();

        protected unsafe class WindowBaseEvents : CallbackBase, IAvnWindowBaseEvents
        {
            private readonly WindowBaseImpl _parent;

            public WindowBaseEvents(WindowBaseImpl parent)
            {
                _parent = parent;
            }

            void IAvnWindowBaseEvents.Closed()
            {
                var n = _parent._native;
                try
                {
                    _parent?.Closed?.Invoke();
                }
                finally
                {
                    
                    _parent?.Dispose();
                    n?.Dispose();
                }
            }

            void IAvnWindowBaseEvents.Activated() => _parent.Activated?.Invoke();

            void IAvnWindowBaseEvents.Deactivated() => _parent.Deactivated?.Invoke();

            void IAvnWindowBaseEvents.Paint()
            {
                Dispatcher.UIThread.RunJobs(DispatcherPriority.Render);
                var s = _parent.ClientSize;
                _parent.Paint?.Invoke(new Rect(0, 0, s.Width, s.Height));
            }

            void IAvnWindowBaseEvents.Resized(AvnSize* size)
            {
                if (_parent?._native != null)
                {
                    var s = new Size(size->Width, size->Height);
                    _parent._savedLogicalSize = s;
                    _parent.Resized?.Invoke(s);
                }
            }

            void IAvnWindowBaseEvents.PositionChanged(AvnPoint position)
            {
                _parent.PositionChanged?.Invoke(position.ToAvaloniaPixelPoint());
            }

            void IAvnWindowBaseEvents.RawMouseEvent(AvnRawMouseEventType type, uint timeStamp, AvnInputModifiers modifiers, AvnPoint point, AvnVector delta)
            {
                _parent.RawMouseEvent(type, timeStamp, modifiers, point, delta);
            }

            int IAvnWindowBaseEvents.RawKeyEvent(AvnRawKeyEventType type, uint timeStamp, AvnInputModifiers modifiers, uint key)
            {
                return _parent.RawKeyEvent(type, timeStamp, modifiers, key).AsComBool();
            }

            int IAvnWindowBaseEvents.RawTextInputEvent(uint timeStamp, string text)
            {
                return _parent.RawTextInputEvent(timeStamp, text).AsComBool();
            }

            void IAvnWindowBaseEvents.ScalingChanged(double scaling)
            {
                _parent._savedScaling = scaling;
                _parent.ScalingChanged?.Invoke(scaling);
            }

            void IAvnWindowBaseEvents.RunRenderPriorityJobs()
            {
                Dispatcher.UIThread.RunJobs(DispatcherPriority.Render);
            }
            
            void IAvnWindowBaseEvents.LostFocus()
            {
                _parent.LostFocus?.Invoke();
            }

            public AvnDragDropEffects DragEvent(AvnDragEventType type, AvnPoint position,
                AvnInputModifiers modifiers,
                AvnDragDropEffects effects,
                IAvnClipboard clipboard, IntPtr dataObjectHandle)
            {
                var device = AvaloniaLocator.Current.GetService<IDragDropDevice>();

                IDataObject dataObject = null;
                if (dataObjectHandle != IntPtr.Zero)
                    dataObject = GCHandle.FromIntPtr(dataObjectHandle).Target as IDataObject;
                
                using(var clipboardDataObject = new ClipboardDataObject(clipboard))
                {
                    if (dataObject == null)
                        dataObject = clipboardDataObject;
                    
                    var args = new RawDragEvent(device, (RawDragEventType)type,
                        _parent._inputRoot, position.ToAvaloniaPoint(), dataObject, (DragDropEffects)effects,
                        (RawInputModifiers)modifiers);
                    _parent.Input(args);
                    return (AvnDragDropEffects)args.Effects;
                }
            }

            public IAvnAutomationPeer AutomationStarted(IAvnAutomationNode node) => _parent.HandleAutomationStarted(node);
        }
       
        public void Activate()
        {
            _native.Activate();
        }

        public bool RawTextInputEvent(uint timeStamp, string text)
        {
            if (_inputRoot is null) 
                return false;
            
            Dispatcher.UIThread.RunJobs(DispatcherPriority.Input + 1);

            var args = new RawTextInputEventArgs(_keyboard, timeStamp, _inputRoot, text);

            Input?.Invoke(args);

            return args.Handled;
        }

        public bool RawKeyEvent(AvnRawKeyEventType type, uint timeStamp, AvnInputModifiers modifiers, uint key)
        {
            if (_inputRoot is null) 
                return false;
            
            Dispatcher.UIThread.RunJobs(DispatcherPriority.Input + 1);

            var args = new RawKeyEventArgs(_keyboard, timeStamp, _inputRoot, (RawKeyEventType)type, (Key)key, (RawInputModifiers)modifiers);

            Input?.Invoke(args);

            return args.Handled;
        }

        protected virtual bool ChromeHitTest(RawPointerEventArgs e)
        {
            return false;
        }

        public void RawMouseEvent(AvnRawMouseEventType type, uint timeStamp, AvnInputModifiers modifiers, AvnPoint point, AvnVector delta)
        {
            if (_inputRoot is null) 
                return;
            
            Dispatcher.UIThread.RunJobs(DispatcherPriority.Input + 1);

            switch (type)
            {
                case AvnRawMouseEventType.Wheel:
                    Input?.Invoke(new RawMouseWheelEventArgs(_mouse, timeStamp, _inputRoot, point.ToAvaloniaPoint(), new Vector(delta.X, delta.Y), (RawInputModifiers)modifiers));
                    break;

                default:
                    var e = new RawPointerEventArgs(_mouse, timeStamp, _inputRoot, (RawPointerEventType)type, point.ToAvaloniaPoint(), (RawInputModifiers)modifiers);
                    
                    if(!ChromeHitTest(e))
                    {
                        Input?.Invoke(e);
                    }
                    break;
            }
        }

        public void Resize(Size clientSize)
        {
            _native.Resize(clientSize.Width, clientSize.Height);
        }

        public IRenderer CreateRenderer(IRenderRoot root)
        {
            if (_deferredRendering)
            {
                var loop = AvaloniaLocator.Current.GetService<IRenderLoop>();
                var customRendererFactory = AvaloniaLocator.Current.GetService<IRendererFactory>();

                if (customRendererFactory != null)
                    return customRendererFactory.Create(root, loop);
                return new DeferredRenderer(root, loop);
            }

            return new ImmediateRenderer(root);
        }

        public virtual void Dispose()
        {
            _native?.Close();
            _native?.Dispose();
            _native = null;

            _nativeControlHost?.Dispose();
            _nativeControlHost = null;
            
            (Screen as ScreenImpl)?.Dispose();
            _mouse.Dispose();
        }


        public void Invalidate(Rect rect)
        {
            _native?.Invalidate(new AvnRect { Height = rect.Height, Width = rect.Width, X = rect.X, Y = rect.Y });
        }

        public void SetInputRoot(IInputRoot inputRoot)
        {
            _inputRoot = inputRoot;
        }


        public virtual void Show(bool activate)
        {
            _native.Show(activate.AsComBool());
        }


        public PixelPoint Position
        {
            get => _native.Position.ToAvaloniaPixelPoint();
            set => _native.SetPosition(value.ToAvnPoint());
        }

        public Point PointToClient(PixelPoint point)
        {
            return _native?.PointToClient(point.ToAvnPoint()).ToAvaloniaPoint() ?? default;
        }

        public PixelPoint PointToScreen(Point point)
        {
            return _native?.PointToScreen(point.ToAvnPoint()).ToAvaloniaPixelPoint() ?? default;
        }

        public void Hide()
        {
            _native.Hide();
        }

        public void BeginMoveDrag(PointerPressedEventArgs e)
        {
            _native.BeginMoveDrag();
        }

        public Size MaxAutoSizeHint => Screen.AllScreens.Select(s => s.Bounds.Size.ToSize(1))
            .OrderByDescending(x => x.Width + x.Height).FirstOrDefault();

        public void SetTopmost(bool value)
        {
            _native.SetTopMost(value.AsComBool());
        }

        public double RenderScaling => _native?.Scaling ?? 1;

        public double DesktopScaling => 1;

        public Action Deactivated { get; set; }
        public Action Activated { get; set; }

        public void SetCursor(ICursorImpl cursor)
        {
            if (_native == null)
            {
                return;
            }
            
            var newCursor = cursor as AvaloniaNativeCursor;
            newCursor = newCursor ?? (_cursorFactory.GetCursor(StandardCursorType.Arrow) as AvaloniaNativeCursor);
            _native.SetCursor(newCursor.Cursor);
        }

        public Action<PixelPoint> PositionChanged { get; set; }

        public Action<RawInputEventArgs> Input { get; set; }

        public Action<double> ScalingChanged { get; set; }

        public Action<WindowTransparencyLevel> TransparencyLevelChanged { get; set; }

        public IScreenImpl Screen { get; private set; }

        // TODO

        public void SetMinMaxSize(Size minSize, Size maxSize)
        {
            _native.SetMinMaxSize(minSize.ToAvnSize(), maxSize.ToAvnSize());
        }

        public void BeginResizeDrag(WindowEdge edge, PointerPressedEventArgs e)
        {

        }

        internal void BeginDraggingSession(AvnDragDropEffects effects, AvnPoint point, IAvnClipboard clipboard,
            IAvnDndResultCallback callback, IntPtr sourceHandle)
        {
            _native.BeginDragAndDropOperation(effects, point, clipboard, callback, sourceHandle);
        }

        public void SetTransparencyLevelHint(WindowTransparencyLevel transparencyLevel) 
        {
            if (TransparencyLevel != transparencyLevel)
            {
                if (transparencyLevel >= WindowTransparencyLevel.Blur)
                {
                    transparencyLevel = WindowTransparencyLevel.AcrylicBlur;
                }

                if(transparencyLevel == WindowTransparencyLevel.None)
                {
                    transparencyLevel = WindowTransparencyLevel.Transparent;
                }

                TransparencyLevel = transparencyLevel;

                _native?.SetBlurEnabled((TransparencyLevel >= WindowTransparencyLevel.Blur).AsComBool());
                TransparencyLevelChanged?.Invoke(TransparencyLevel);
            }
        }

        public WindowTransparencyLevel TransparencyLevel { get; private set; } = WindowTransparencyLevel.Transparent;

        public AcrylicPlatformCompensationLevels AcrylicCompensationLevels { get; } = new AcrylicPlatformCompensationLevels(1, 0, 0);

        public IPlatformHandle Handle { get; private set; }

        public Func<IAutomationNode, AutomationPeer> AutomationStarted
        {
            get => _automationStarted;
            set
            {
                _automationStarted = value;
                
                // We've already received an AutomationStarted event, but the Window/PopupRoot wasn't initialized.
                // Now it is, so notify it and store the automation peer for the next time the OS invokes an a11y
                // query.
                if (value is object && _automationNode is object) 
                    _automationPeer = new AvnAutomationPeer(_automationStarted.Invoke(_automationNode));
            }
        }

        private AvnAutomationPeer HandleAutomationStarted(IAvnAutomationNode node)
        {
            if (_automationPeer is object)
                return _automationPeer;
            
            var factory = AutomationNodeFactory.GetInstance(_factory);
            _automationNode = new AutomationNode(factory, node);

            // If automation is started during platform window creation we don't yet have a Window/PopupRoot
            // control to notify. In this case we'll notify them when AutomationStarted gets set. We can safely
            // return null here because the peer isn't actually needed at this point and will be re-queried the next
            // time it's needed. 
            if (AutomationStarted is null)
                return null;
                
            return _automationPeer = new AvnAutomationPeer(AutomationStarted(_automationNode));
        }
    }
}
