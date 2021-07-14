using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using Avalonia.Controls;
using Avalonia.Controls.Embedding;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.Layout;
using Avalonia.Platform;
using Avalonia.Rendering;
using Key = Avalonia.Input.Key;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using MouseButton = System.Windows.Input.MouseButton;

namespace Avalonia.Win32.Interop.Wpf
{
    class WpfTopLevelImpl : FrameworkElement, ITopLevelImpl
    {
        private HwndSource _currentHwndSource;
        private readonly HwndSourceHook _hook;
        private readonly ITopLevelImpl _ttl;
        private IInputRoot _inputRoot;
        private readonly IEnumerable<object> _surfaces;
        private readonly IMouseDevice _mouse;
        private readonly IKeyboardDevice _keyboard;
        private Size _finalSize;

        public EmbeddableControlRoot ControlRoot { get; }
        internal ImageSource ImageSource { get; set; }

        public class CustomControlRoot : EmbeddableControlRoot, IEmbeddedLayoutRoot
        {
            public CustomControlRoot(WpfTopLevelImpl impl) : base(impl)
            {
                EnforceClientSize = false;
            }

            protected override void OnMeasureInvalidated()
            {
                ((FrameworkElement)PlatformImpl)?.InvalidateMeasure();
            }

            protected override void HandleResized(Size clientSize, PlatformResizeReason reason)
            {
                ClientSize = clientSize;
                LayoutManager.ExecuteLayoutPass();
                Renderer?.Resized(clientSize);
            }

            public Size AllocatedSize => ClientSize;
        }

        public WpfTopLevelImpl()
        {
            PresentationSource.AddSourceChangedHandler(this, OnSourceChanged);
            _hook = WndProc;
            _ttl = this;
            _surfaces = new object[] {new WritableBitmapSurface(this), new Direct2DImageSurface(this)};
            _mouse = new WpfMouseDevice(this);
            _keyboard = AvaloniaLocator.Current.GetService<IKeyboardDevice>();

            ControlRoot = new CustomControlRoot(this);
            SnapsToDevicePixels = true;
            Focusable = true;
            DataContextChanged += delegate
            {
                ControlRoot.DataContext = DataContext;
            };
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wparam, IntPtr lparam, ref bool handled)
        {
            if (msg == (int)UnmanagedMethods.WindowsMessage.WM_DPICHANGED)
                _ttl.ScalingChanged?.Invoke(_ttl.RenderScaling);
            return IntPtr.Zero;
        }

        private void OnSourceChanged(object sender, SourceChangedEventArgs e)
        {
            _currentHwndSource?.RemoveHook(_hook);
            _currentHwndSource = e.NewSource as HwndSource;
            _currentHwndSource?.AddHook(_hook);
            _ttl.ScalingChanged?.Invoke(_ttl.RenderScaling);
        }

        public IRenderer CreateRenderer(IRenderRoot root)
        {
            return new ImmediateRenderer(root);
        }

        public void Dispose()
        {
            _ttl.Closed?.Invoke();
            foreach(var d in _surfaces.OfType<IDisposable>())
                d.Dispose();
        }

        Size ITopLevelImpl.ClientSize => _finalSize;
        Size? ITopLevelImpl.FrameSize => null;
        IMouseDevice ITopLevelImpl.MouseDevice => _mouse;

        double ITopLevelImpl.RenderScaling => PresentationSource.FromVisual(this)?.CompositionTarget?.TransformToDevice.M11 ?? 1;

        IEnumerable<object> ITopLevelImpl.Surfaces => _surfaces;

        private Size _previousSize;
        protected override System.Windows.Size ArrangeOverride(System.Windows.Size finalSize)
        {
            _finalSize = finalSize.ToAvaloniaSize();
            if (_finalSize == _previousSize)
                return finalSize;
            _previousSize = _finalSize;
            _ttl.Resized?.Invoke(finalSize.ToAvaloniaSize(), PlatformResizeReason.Unspecified);
            return base.ArrangeOverride(finalSize);
        }

        protected override System.Windows.Size MeasureOverride(System.Windows.Size availableSize)
        {
            ControlRoot.Measure(availableSize.ToAvaloniaSize());
            return ControlRoot.DesiredSize.ToWpfSize();
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            if(ActualHeight == 0 || ActualWidth == 0)
                return;
            _ttl.Paint?.Invoke(new Rect(0, 0, ActualWidth, ActualHeight));
            if (ImageSource != null)
                drawingContext.DrawImage(ImageSource, new System.Windows.Rect(0, 0, ActualWidth, ActualHeight));
        }

        void ITopLevelImpl.Invalidate(Rect rect) => InvalidateVisual();

        void ITopLevelImpl.SetInputRoot(IInputRoot inputRoot) => _inputRoot = inputRoot;

        Point ITopLevelImpl.PointToClient(PixelPoint point) => PointFromScreen(point.ToWpfPoint()).ToAvaloniaPoint();

        PixelPoint ITopLevelImpl.PointToScreen(Point point) => PointToScreen(point.ToWpfPoint()).ToAvaloniaPixelPoint();

        protected override void OnLostFocus(RoutedEventArgs e) => LostFocus?.Invoke();


        RawInputModifiers GetModifiers(MouseEventArgs e)
        {
            var state = Keyboard.Modifiers;
            var rv = default(RawInputModifiers);
            if (state.HasAllFlags(ModifierKeys.Windows))
                rv |= RawInputModifiers.Meta;
            if (state.HasAllFlags(ModifierKeys.Alt))
                rv |= RawInputModifiers.Alt;
            if (state.HasAllFlags(ModifierKeys.Control))
                rv |= RawInputModifiers.Control;
            if (state.HasAllFlags(ModifierKeys.Shift))
                rv |= RawInputModifiers.Shift;
            if (e != null)
            {
                if (e.LeftButton == MouseButtonState.Pressed)
                    rv |= RawInputModifiers.LeftMouseButton;
                if (e.RightButton == MouseButtonState.Pressed)
                    rv |= RawInputModifiers.RightMouseButton;
                if (e.MiddleButton == MouseButtonState.Pressed)
                    rv |= RawInputModifiers.MiddleMouseButton;
            }
            return rv;
        }

        void MouseEvent(RawPointerEventType type, MouseEventArgs e)
            => _ttl.Input?.Invoke(new RawPointerEventArgs(_mouse, (uint)e.Timestamp, _inputRoot, type,
            e.GetPosition(this).ToAvaloniaPoint(), GetModifiers(e)));

        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            RawPointerEventType type;
            if(e.ChangedButton == MouseButton.Left)
                type = RawPointerEventType.LeftButtonDown;
            else if (e.ChangedButton == MouseButton.Middle)
                type = RawPointerEventType.MiddleButtonDown;
            else if (e.ChangedButton == MouseButton.Right)
                type = RawPointerEventType.RightButtonDown;
            else
                return;
            MouseEvent(type, e);
            Focus();
        }

        protected override void OnMouseUp(MouseButtonEventArgs e)
        {
            RawPointerEventType type;
            if (e.ChangedButton == MouseButton.Left)
                type = RawPointerEventType.LeftButtonUp;
            else if (e.ChangedButton == MouseButton.Middle)
                type = RawPointerEventType.MiddleButtonUp;
            else if (e.ChangedButton == MouseButton.Right)
                type = RawPointerEventType.RightButtonUp;
            else
                return;
            MouseEvent(type, e);
            Focus();
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            MouseEvent(RawPointerEventType.Move, e);
        }

        protected override void OnMouseWheel(MouseWheelEventArgs e) =>
            _ttl.Input?.Invoke(new RawMouseWheelEventArgs(_mouse, (uint) e.Timestamp, _inputRoot,
                e.GetPosition(this).ToAvaloniaPoint(), new Vector(0, e.Delta), GetModifiers(e)));

        protected override void OnMouseLeave(MouseEventArgs e) => MouseEvent(RawPointerEventType.LeaveWindow, e);

        protected override void OnKeyDown(KeyEventArgs e)
            => _ttl.Input?.Invoke(new RawKeyEventArgs(_keyboard, (uint) e.Timestamp, _inputRoot, RawKeyEventType.KeyDown,
                (Key) e.Key,
                GetModifiers(null)));

        protected override void OnKeyUp(KeyEventArgs e)
            => _ttl.Input?.Invoke(new RawKeyEventArgs(_keyboard, (uint)e.Timestamp, _inputRoot, RawKeyEventType.KeyUp,
                (Key)e.Key,
                GetModifiers(null)));

        protected override void OnTextInput(TextCompositionEventArgs e) 
            => _ttl.Input?.Invoke(new RawTextInputEventArgs(_keyboard, (uint) e.Timestamp, _inputRoot, e.Text));

        void ITopLevelImpl.SetCursor(ICursorImpl cursor)
        {
            if (cursor == null)
                Cursor = Cursors.Arrow;
            else if (cursor is IPlatformHandle handle)
                Cursor = CursorShim.FromHCursor(handle.Handle);
        }

        Action<RawInputEventArgs> ITopLevelImpl.Input { get; set; } //TODO
        Action<Rect> ITopLevelImpl.Paint { get; set; }
        Action<Size, PlatformResizeReason> ITopLevelImpl.Resized { get; set; }
        Action<double> ITopLevelImpl.ScalingChanged { get; set; }

        Action<WindowTransparencyLevel> ITopLevelImpl.TransparencyLevelChanged { get; set; }

        Action ITopLevelImpl.Closed { get; set; }
        public new Action LostFocus { get; set; }

        internal Vector GetScaling()
        {
            var src = PresentationSource.FromVisual(this)?.CompositionTarget;
            if (src == null)
                return new Vector(1, 1);
            return new Vector(src.TransformToDevice.M11, src.TransformToDevice.M22);
        }

        public IPopupImpl CreatePopup() => null;

        public void SetTransparencyLevelHint(WindowTransparencyLevel transparencyLevel) { }

        public WindowTransparencyLevel TransparencyLevel { get; private set; }

        public AcrylicPlatformCompensationLevels AcrylicCompensationLevels { get; } = new AcrylicPlatformCompensationLevels(1, 1, 1);
    }
}
