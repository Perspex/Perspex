using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Controls.Embedding;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.Platform;
using Avalonia.Rendering;
using CoreAnimation;
using CoreGraphics;
using Foundation;
using ObjCRuntime;
using OpenGLES;
using UIKit;

namespace Avalonia.iOS
{
    public partial class AvaloniaView : UIView
    {
        internal IInputRoot InputRoot { get; private set; }
        private TopLevelImpl _topLevelImpl;
        private EmbeddableControlRoot _topLevel;
        private TouchHandler _touches;

        public AvaloniaView()
        {
            _topLevelImpl = new TopLevelImpl(this);
            _touches = new TouchHandler(this, _topLevelImpl);
            _topLevel = new EmbeddableControlRoot(_topLevelImpl);
            _topLevel.Prepare();
            
            _topLevel.Renderer.Start();
            
            var l = (CAEAGLLayer) Layer;
            l.ContentsScale = UIScreen.MainScreen.Scale;
            l.Opaque = true;
            l.DrawableProperties = new NSDictionary(
                EAGLDrawableProperty.RetainedBacking, false,
                EAGLDrawableProperty.ColorFormat, EAGLColorFormat.RGBA8
            );
            _topLevelImpl.Surfaces = new[] {new EaglLayerSurface(l)};
            MultipleTouchEnabled = true;
        }

        internal class TopLevelImpl : ITopLevelImpl
        {
            private readonly AvaloniaView _view;
            public AvaloniaView View => _view;

            public TopLevelImpl(AvaloniaView view)
            {
                _view = view;
            }

            public void Dispose()
            {
                // No-op
            }

            public IRenderer CreateRenderer(IRenderRoot root) => new DeferredRenderer(root,
                AvaloniaLocator.Current.GetService<IRenderLoop>());

            public void Invalidate(Rect rect)
            {
                // No-op
            }

            public void SetInputRoot(IInputRoot inputRoot)
            {
                _view.InputRoot = inputRoot;
            }

            public Point PointToClient(PixelPoint point) => new Point(point.X, point.Y);

            public PixelPoint PointToScreen(Point point) => new PixelPoint((int) point.X, (int) point.Y);

            public void SetCursor(ICursorImpl _)
            {
                // no-op
            }

            public IPopupImpl CreatePopup()
            {
                // In-window popups
                return null;
            }

            public void SetTransparencyLevelHint(WindowTransparencyLevel transparencyLevel)
            {
                // No-op
            }

            public Size ClientSize => new Size(_view.Bounds.Width, _view.Bounds.Height);
            public Size? FrameSize => null;
            public double RenderScaling => _view.ContentScaleFactor;
            public IEnumerable<object> Surfaces { get; set; }
            public Action<RawInputEventArgs> Input { get; set; }
            public Action<Rect> Paint { get; set; }
            public Action<Size, PlatformResizeReason> Resized { get; set; }
            public Action<double> ScalingChanged { get; set; }
            public Action<WindowTransparencyLevel> TransparencyLevelChanged { get; set; }
            public Action Closed { get; set; }

            public Action LostFocus { get; set; }

            // legacy no-op
            public IMouseDevice MouseDevice { get; } = new MouseDevice();
            public WindowTransparencyLevel TransparencyLevel { get; }

            public AcrylicPlatformCompensationLevels AcrylicCompensationLevels { get; } =
                new AcrylicPlatformCompensationLevels();
        }

        [Export("layerClass")]
        public static Class LayerClass()
        {
            return new Class(typeof(CAEAGLLayer));
        }

        public override void TouchesBegan(NSSet touches, UIEvent evt) => _touches.Handle(touches, evt);

        public override void TouchesMoved(NSSet touches, UIEvent evt) => _touches.Handle(touches, evt);

        public override void TouchesEnded(NSSet touches, UIEvent evt) => _touches.Handle(touches, evt);

        public override void TouchesCancelled(NSSet touches, UIEvent evt) => _touches.Handle(touches, evt);

        public override void LayoutSubviews()
        {
            _topLevelImpl.Resized?.Invoke(_topLevelImpl.ClientSize, PlatformResizeReason.Layout);
            base.LayoutSubviews();
        }

        public Control Content
        {
            get => (Control)_topLevel.Content;
            set => _topLevel.Content = value;
        }
    }
}
