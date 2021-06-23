using System;
using Avalonia.Media;
using Avalonia.Platform;
using Moq;
using Xunit;

namespace Avalonia.Base.UnitTests.Media
{
    public class GeometryTests
    {
        [Fact]
        public void Changing_AffectsGeometry_Property_Causes_PlatformImpl_To_Be_Updated()
        {
            var target = new TestGeometry();
            var platformImpl = target.PlatformImpl;

            target.Foo = true;

            Assert.NotSame(platformImpl, target.PlatformImpl);
        }

        [Fact]
        public void Changing_AffectsGeometry_Property_Causes_Changed_To_Be_Raised()
        {
            var target = new TestGeometry();
            var raised = false;

            target.Changed += (s, e) => raised = true;
            target.Foo = true;

            Assert.True(raised);
        }

        [Fact]
        public void Setting_Transform_Causes_Changed_To_Be_Raised()
        {
            var target = new TestGeometry();
            var raised = false;

            target.Changed += (s, e) => raised = true;
            target.Transform = new RotateTransform(45);

            Assert.True(raised);
        }

        [Fact]
        public void Changing_Transform_Causes_Changed_To_Be_Raised()
        {
            var transform = new RotateTransform(45);
            var target = new TestGeometry { Transform = transform };
            var raised = false;

            target.Changed += (s, e) => raised = true;
            transform.Angle = 90;

            Assert.True(raised);
        }

        [Fact]
        public void Removing_Transform_Causes_Changed_To_Be_Raised()
        {
            var transform = new RotateTransform(45);
            var target = new TestGeometry { Transform = transform };
            var raised = false;

            target.Changed += (s, e) => raised = true;
            target.Transform = null;

            Assert.True(raised);
        }

        [Fact]
        public void Transform_Produces_Transformed_PlatformImpl()
        {
            var target = new TestGeometry();
            var rotate = new RotateTransform(45);

            Assert.False(target.PlatformImpl is ITransformedGeometryImpl);
            target.Transform = rotate;
            Assert.True(target.PlatformImpl is ITransformedGeometryImpl);
            rotate.Angle = 0;
            Assert.False(target.PlatformImpl is ITransformedGeometryImpl);
        }

        private class TestGeometry : Geometry
        {
            public static readonly StyledProperty<bool> FooProperty =
                AvaloniaProperty.Register<TestGeometry, bool>(nameof(Foo));

            static TestGeometry()
            {
                AffectsGeometry(FooProperty);
            }

            public bool Foo
            {
                get => GetValue(FooProperty);
                set => SetValue(FooProperty, value);
            }

            public override Geometry Clone()
            {
                throw new NotImplementedException();
            }

            protected override IGeometryImpl CreateDefiningGeometry()
            {
                return Mock.Of<IGeometryImpl>(
                    x => x.WithTransform(It.IsAny<Matrix>()) == 
                        Mock.Of<ITransformedGeometryImpl>(y =>
                            y.SourceGeometry == x));
            }
        }
    }
}
