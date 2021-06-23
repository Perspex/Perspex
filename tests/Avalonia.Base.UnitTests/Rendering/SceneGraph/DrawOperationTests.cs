using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Utilities;
using Avalonia.Media.Imaging;
using Moq;
using Xunit;

namespace Avalonia.Base.UnitTests.Rendering.SceneGraph
{  
    public class DrawOperationTests
    {
        [Fact]
        public void Empty_Bounds_Remain_Empty()
        {
            var target = new TestDrawOperation(Rect.Empty, Matrix.Identity, null);

            Assert.Equal(Rect.Empty, target.Bounds);
        }

        [Theory]
        [InlineData(10, 10, 10, 10, 1, 1, 1, 9, 9, 12, 12)]
        [InlineData(10, 10, 10, 10, 1, 1, 2, 9, 9, 12, 12)]
        [InlineData(10, 10, 10, 10, 1.5, 1.5, 1, 14, 14, 17, 17)]
        public void Rectangle_Bounds_Are_Snapped_To_Pixels(
            double x,
            double y,
            double width,
            double height,
            double scaleX,
            double scaleY,
            double? penThickness,
            double expectedX,
            double expectedY,
            double expectedWidth,
            double expectedHeight)
        {
            var target = new TestRectangleDrawOperation(
                new Rect(x, y, width, height),
                Matrix.CreateScale(scaleX, scaleY),
                penThickness.HasValue ? new Pen(Brushes.Black, penThickness.Value) : null);
            Assert.Equal(new Rect(expectedX, expectedY, expectedWidth, expectedHeight), target.Bounds);
        }

        [Fact]
        public void Image_Node_Releases_Reference_To_Bitmap_On_Dispose()
        {
            var bitmap = RefCountable.Create(Mock.Of<IBitmapImpl>());
            var imageNode = new ImageNode(
                Matrix.Identity,
                bitmap,
                1,
                new Rect(1, 1, 1, 1),
                new Rect(1, 1, 1, 1),
                BitmapInterpolationMode.Default);

            Assert.Equal(2, bitmap.RefCount);

            imageNode.Dispose();

            Assert.Equal(1, bitmap.RefCount);
        }

        [Fact]
        public void HitTest_On_Geometry_Node_With_Zero_Transform_Does_Not_Throw()
        {
            var geometry = Mock.Of<IGeometryImpl>();
            var geometryNode = new GeometryNode(
                new Matrix(),
                Brushes.Black,
                null,
                geometry, default);

            geometryNode.HitTest(new Point());
        }

        private class TestRectangleDrawOperation : RectangleNode
        {
            public TestRectangleDrawOperation(Rect bounds, Matrix transform, Pen pen) 
                : base(transform, pen.Brush, pen, bounds, new BoxShadows())
            {

            }

            public override bool HitTest(Point p) => false;

            public override void Render(IDrawingContextImpl context) { }
        }

        private class TestDrawOperation : DrawOperation
        {
            public TestDrawOperation(Rect bounds, Matrix transform, Pen pen)
                :base(bounds, transform)
            {
            }

            public override bool HitTest(Point p) => false;

            public override void Render(IDrawingContextImpl context) { }
        }
    }
}
