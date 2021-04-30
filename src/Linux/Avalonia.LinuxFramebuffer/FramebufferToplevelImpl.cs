using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.LinuxFramebuffer.Input;
using Avalonia.LinuxFramebuffer.Output;
using Avalonia.Platform;
using Avalonia.Rendering;

namespace Avalonia.LinuxFramebuffer
{
    class FramebufferToplevelImpl : ITopLevelImpl, IScreenInfoProvider
    {
        private readonly IOutputBackend _outputBackend;
        private readonly IInputBackend _inputBackend;

        public IInputRoot InputRoot { get; private set; }

        public FramebufferToplevelImpl(IOutputBackend outputBackend, IInputBackend inputBackend)
        {
            _outputBackend = outputBackend;
            _inputBackend = inputBackend;

            Surfaces = new object[] { _outputBackend };

            Invalidate(default(Rect));
            _inputBackend.Initialize(this, e => Input?.Invoke(e));
        }

        public IRenderer CreateRenderer(IRenderRoot root)
        {
            return new DeferredRenderer(root, AvaloniaLocator.Current.GetService<IRenderLoop>())
            {
                
            };
        }

        public void Dispose()
        {
            throw new NotSupportedException();
        }

        
        public void Invalidate(Rect rect)
        {
        }

        public void SetInputRoot(IInputRoot inputRoot)
        {
            InputRoot = inputRoot;
            _inputBackend.SetInputRoot(inputRoot);
        }

        public Point PointToClient(PixelPoint p) => p.ToPoint(1);

        public PixelPoint PointToScreen(Point p) => PixelPoint.FromPoint(p, 1);

        public void SetCursor(ICursorImpl cursor)
        {
        }

        public Size ClientSize => ScaledSize;
        public IMouseDevice MouseDevice => new MouseDevice();
        public IPopupImpl CreatePopup() => null;

        public double RenderScaling => _outputBackend.Scaling;
        public IEnumerable<object> Surfaces { get; }
        public Action<RawInputEventArgs> Input { get; set; }
        public Action<Rect> Paint { get; set; }
        public Action<Size> Resized { get; set; }
        public Action<double> ScalingChanged { get; set; }

        public Action<WindowTransparencyLevel> TransparencyLevelChanged { get; set; }

        public Action Closed { get; set; }
        public Action LostFocus { get; set; }

        public Size ScaledSize => _outputBackend.PixelSize.ToSize(RenderScaling);

        public void SetTransparencyLevelHint(WindowTransparencyLevel transparencyLevel) { }

        public WindowTransparencyLevel TransparencyLevel { get; private set; }

        public AcrylicPlatformCompensationLevels AcrylicCompensationLevels { get; } = new AcrylicPlatformCompensationLevels(1, 1, 1);
    }
}
