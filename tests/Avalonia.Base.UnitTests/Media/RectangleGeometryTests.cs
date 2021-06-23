using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.UnitTests;
using Moq;
using Xunit;

namespace Avalonia.Base.UnitTests.Media
{
    public class RectangleGeometryTests
    {
        [Fact]
        public void Rectangle_With_Transform_Can_Be_Changed()
        {
            using (UnitTestApplication.Start(GetServices()))
            {
                var target = new RectangleGeometry
                {
                    Rect = new Rect(0, 0, 100, 100),
                    Transform = new RotateTransform(45),
                };

                target.Rect = new Rect(50, 50, 150, 150);
            }
        }

        private TestServices GetServices()
        {
            var context = Mock.Of<IStreamGeometryContextImpl>();
            var transformedGeometry = new Mock<ITransformedGeometryImpl>();
            var streamGeometry = Mock.Of<IStreamGeometryImpl>(x => 
                x.Open() == context &&
                x.WithTransform(It.IsAny<Matrix>()) == transformedGeometry.Object);
            var renderInterface = Mock.Of<IPlatformRenderInterface>(x =>
                x.CreateStreamGeometry() == streamGeometry);
            return new TestServices(renderInterface: renderInterface);
        }
    }
}
