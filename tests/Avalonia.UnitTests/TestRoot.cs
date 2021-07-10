using System;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.LogicalTree;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Rendering;
using Avalonia.Styling;
using Avalonia.VisualTree;
using Moq;

namespace Avalonia.UnitTests
{
    public class TestRoot : Decorator, IFocusScope, ILayoutRoot, IInputRoot, IRenderRoot, IStyleHost, ILogicalRoot
    {
        private readonly NameScope _nameScope = new NameScope();

        public TestRoot()
        {
            Renderer = Mock.Of<IRenderer>();
            LayoutManager = new LayoutManager(this);
            IsVisible = true;
        }

        public TestRoot(IControl child)
            : this(false, child)
        {
            Child = child;
        }

        public TestRoot(bool useGlobalStyles, IControl child)
            : this()
        {
            if (useGlobalStyles)
            {
                StylingParent = UnitTestApplication.Current;
            }

            Child = child;
        }

        public Size ClientSize { get; set; } = new Size(100, 100);

        public Size MaxClientSize { get; set; } = Size.Infinity;

        public double LayoutScaling { get; set; } = 1;

        public ILayoutManager LayoutManager { get; set; }

        public double RenderScaling => 1;

        public IRenderer Renderer { get; set; }

        public IAccessKeyHandler AccessKeyHandler => null;

        public IKeyboardNavigationHandler KeyboardNavigationHandler => null;

        public IInputElement PointerOverElement { get; set; }

        public IMouseDevice MouseDevice { get; set; }

        public bool ShowAccessKeys { get; set; }

        public bool? IsPlatformEnabled { get; }

        public IStyleHost StylingParent { get; set; }

        IStyleHost IStyleHost.StylingParent => StylingParent;

        public IRenderTarget CreateRenderTarget()
        {
            var dc = new Mock<IDrawingContextImpl>();
            dc.Setup(x => x.CreateLayer(It.IsAny<Size>())).Returns(() =>
            {
                var layerDc = new Mock<IDrawingContextImpl>();
                var layer = new Mock<IDrawingContextLayerImpl>();
                layer.Setup(x => x.CreateDrawingContext(It.IsAny<IVisualBrushRenderer>())).Returns(layerDc.Object);
                return layer.Object;
            });

            var result = new Mock<IRenderTarget>();
            result.Setup(x => x.CreateDrawingContext(It.IsAny<IVisualBrushRenderer>())).Returns(dc.Object);
            return result.Object;
        }

        public void Invalidate(Rect rect)
        {
        }

        public Point PointToClient(PixelPoint p) => p.ToPoint(1);

        public PixelPoint PointToScreen(Point p) => PixelPoint.FromPoint(p, 1);

        public void RegisterChildrenNames()
        {
            var scope = NameScope.GetNameScope(this) ?? new NameScope();
            NameScope.SetNameScope(this, scope);
            void Visit(StyledElement element, bool force = false)
            {
                if (element.Name != null)
                {
                    if (scope.Find(element.Name) != element)
                        scope.Register(element.Name, element);
                }

                if(element is IVisual visual && (force || NameScope.GetNameScope(element) == null))
                    foreach(var child in visual.GetVisualChildren())
                        if (child is StyledElement styledChild)
                            Visit(styledChild);
            }
            Visit(this, true);
        }
    }
}
