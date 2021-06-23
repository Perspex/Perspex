using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Layout;
using Xunit;

namespace Avalonia.Base.UnitTests.Layout
{
    public class NonVirtualizingStackLayoutTests
    {
        [Fact]
        public void Lays_Out_Children_Vertically()
        {
            var target = new NonVirtualizingStackLayout { Orientation = Orientation.Vertical };
            var context = CreateContext(new[]
            {
                new Border { Height = 20, Width = 120 },
                new Border { Height = 30 },
                new Border { Height = 50 },
            });

            var desiredSize = target.Measure(context, Size.Infinity);
            var arrangeSize = target.Arrange(context, desiredSize);

            Assert.Equal(new Size(120, 100), desiredSize);
            Assert.Equal(new Size(120, 100), arrangeSize);
            Assert.Equal(new Rect(0, 0, 120, 20), context.Children[0].Bounds);
            Assert.Equal(new Rect(0, 20, 120, 30), context.Children[1].Bounds);
            Assert.Equal(new Rect(0, 50, 120, 50), context.Children[2].Bounds);
        }

        [Fact]
        public void Lays_Out_Children_Horizontally()
        {
            var target = new NonVirtualizingStackLayout { Orientation = Orientation.Horizontal };
            var context = CreateContext(new[]
            {
                new Border { Width = 20, Height = 120 },
                new Border { Width = 30 },
                new Border { Width = 50 },
            });

            var desiredSize = target.Measure(context, Size.Infinity);
            var arrangeSize = target.Arrange(context, desiredSize);

            Assert.Equal(new Size(100, 120), desiredSize);
            Assert.Equal(new Size(100, 120), arrangeSize);
            Assert.Equal(new Rect(0, 0, 20, 120), context.Children[0].Bounds);
            Assert.Equal(new Rect(20, 0, 30, 120), context.Children[1].Bounds);
            Assert.Equal(new Rect(50, 0, 50, 120), context.Children[2].Bounds);
        }

        [Fact]
        public void Lays_Out_Children_Vertically_With_Spacing()
        {
            var target = new NonVirtualizingStackLayout 
            { 
                Orientation = Orientation.Vertical,
                Spacing = 10,
            };

            var context = CreateContext(new[]
            {
                new Border { Height = 20, Width = 120 },
                new Border { Height = 30 },
                new Border { Height = 50 },
            });

            var desiredSize = target.Measure(context, Size.Infinity);
            var arrangeSize = target.Arrange(context, desiredSize);

            Assert.Equal(new Size(120, 120), desiredSize);
            Assert.Equal(new Size(120, 120), arrangeSize);
            Assert.Equal(new Rect(0, 0, 120, 20), context.Children[0].Bounds);
            Assert.Equal(new Rect(0, 30, 120, 30), context.Children[1].Bounds);
            Assert.Equal(new Rect(0, 70, 120, 50), context.Children[2].Bounds);
        }

        [Fact]
        public void Lays_Out_Children_Horizontally_With_Spacing()
        {
            var target = new NonVirtualizingStackLayout 
            { 
                Orientation = Orientation.Horizontal,
                Spacing = 10,
            };

            var context = CreateContext(new[]
            {
                new Border { Width = 20, Height = 120 },
                new Border { Width = 30 },
                new Border { Width = 50 },
            });

            var desiredSize = target.Measure(context, Size.Infinity);
            var arrangeSize = target.Arrange(context, desiredSize);

            Assert.Equal(new Size(120, 120), desiredSize);
            Assert.Equal(new Size(120, 120), arrangeSize);
            Assert.Equal(new Rect(0, 0, 20, 120), context.Children[0].Bounds);
            Assert.Equal(new Rect(30, 0, 30, 120), context.Children[1].Bounds);
            Assert.Equal(new Rect(70, 0, 50, 120), context.Children[2].Bounds);
        }

        [Fact]
        public void Arranges_Vertical_Children_With_Correct_Bounds()
        {
            var target = new NonVirtualizingStackLayout
            {
                Orientation = Orientation.Vertical
            };
            
            var context = CreateContext(new[]
            {
                new TestControl
                {
                    HorizontalAlignment = HorizontalAlignment.Left,
                    MeasureSize = new Size(50, 10),
                },
                new TestControl
                {
                    HorizontalAlignment = HorizontalAlignment.Left,
                    MeasureSize = new Size(150, 10),
                },
                new TestControl
                {
                    HorizontalAlignment = HorizontalAlignment.Center,
                    MeasureSize = new Size(50, 10),
                },
                new TestControl
                {
                    HorizontalAlignment = HorizontalAlignment.Center,
                    MeasureSize = new Size(150, 10),
                },
                new TestControl
                {
                    HorizontalAlignment = HorizontalAlignment.Right,
                    MeasureSize = new Size(50, 10),
                },
                new TestControl
                {
                    HorizontalAlignment = HorizontalAlignment.Right,
                    MeasureSize = new Size(150, 10),
                },
                new TestControl
                {
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    MeasureSize = new Size(50, 10),
                },
                new TestControl
                {
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    MeasureSize = new Size(150, 10),
                },
            });

            var desiredSize = target.Measure(context, new Size(100, 150));
            Assert.Equal(new Size(100, 80), desiredSize);

            target.Arrange(context, desiredSize);

            var bounds = context.Children.Select(x => x.Bounds).ToArray();

            Assert.Equal(
                new[]
                {
                    new Rect(0, 0, 50, 10),
                    new Rect(0, 10, 100, 10),
                    new Rect(25, 20, 50, 10),
                    new Rect(0, 30, 100, 10),
                    new Rect(50, 40, 50, 10),
                    new Rect(0, 50, 100, 10),
                    new Rect(0, 60, 100, 10),
                    new Rect(0, 70, 100, 10),

                }, bounds);
        }

        [Fact]
        public void Arranges_Horizontal_Children_With_Correct_Bounds()
        {
            var target = new NonVirtualizingStackLayout
            {
                Orientation = Orientation.Horizontal
            };

            var context = CreateContext(new[]
            {
                new TestControl
                {
                    VerticalAlignment = VerticalAlignment.Top,
                    MeasureSize = new Size(10, 50),
                },
                new TestControl
                {
                    VerticalAlignment = VerticalAlignment.Top,
                    MeasureSize = new Size(10, 150),
                },
                new TestControl
                {
                    VerticalAlignment = VerticalAlignment.Center,
                    MeasureSize = new Size(10, 50),
                },
                new TestControl
                {
                    VerticalAlignment = VerticalAlignment.Center,
                    MeasureSize = new Size(10, 150),
                },
                new TestControl
                {
                    VerticalAlignment = VerticalAlignment.Bottom,
                    MeasureSize = new Size(10, 50),
                },
                new TestControl
                {
                    VerticalAlignment = VerticalAlignment.Bottom,
                    MeasureSize = new Size(10, 150),
                },
                new TestControl
                {
                    VerticalAlignment = VerticalAlignment.Stretch,
                    MeasureSize = new Size(10, 50),
                },
                new TestControl
                {
                    VerticalAlignment = VerticalAlignment.Stretch,
                    MeasureSize = new Size(10, 150),
                },
            });

            var desiredSize = target.Measure(context, new Size(150, 100));
            Assert.Equal(new Size(80, 100), desiredSize);

            target.Arrange(context, desiredSize);

            var bounds = context.Children.Select(x => x.Bounds).ToArray();

            Assert.Equal(
                new[]
                {
                    new Rect(0, 0, 10, 50),
                    new Rect(10, 0, 10, 100),
                    new Rect(20, 25, 10, 50),
                    new Rect(30, 0, 10, 100),
                    new Rect(40, 50, 10, 50),
                    new Rect(50, 0, 10, 100),
                    new Rect(60, 0, 10, 100),
                    new Rect(70, 0, 10, 100),
                }, bounds);
        }

        [Theory]
        [InlineData(Orientation.Horizontal)]
        [InlineData(Orientation.Vertical)]
        public void Spacing_Not_Added_For_Invisible_Children(Orientation orientation)
        {
            var targetThreeChildrenOneInvisble = new NonVirtualizingStackLayout
            {
                Orientation = orientation,
                Spacing = 40,
            };

            var contextThreeChildrenOneInvisble = CreateContext(new[]
            {
                new StackPanel { Width = 10, Height= 10, IsVisible = false },
                new StackPanel { Width = 10, Height= 10 },
                new StackPanel { Width = 10, Height= 10 },
            });

            var targetTwoChildrenNoneInvisible = new NonVirtualizingStackLayout
            {
                Spacing = 40,
                Orientation = orientation,
            };

            var contextTwoChildrenNoneInvisible = CreateContext(new[]
            {
                new StackPanel { Width = 10, Height = 10 },
                new StackPanel { Width = 10, Height = 10 }
            });

            var desiredSize1 = targetThreeChildrenOneInvisble.Measure(contextThreeChildrenOneInvisble, Size.Infinity);
            var desiredSize2 = targetTwoChildrenNoneInvisible.Measure(contextTwoChildrenNoneInvisible, Size.Infinity);
 
            Assert.Equal(desiredSize2, desiredSize1);
        }

        [Theory]
        [InlineData(Orientation.Horizontal)]
        [InlineData(Orientation.Vertical)]
        public void Only_Arrange_Visible_Children(Orientation orientation)
        {
            var hiddenPanel = new Panel { Width = 10, Height = 10, IsVisible = false };
            var panel = new Panel { Width = 10, Height = 10 };

            var target = new NonVirtualizingStackLayout
            {
                Spacing = 40,
                Orientation = orientation,
            };

            var context = CreateContext(new[]
            {
                hiddenPanel,
                panel
            });

            var desiredSize = target.Measure(context, Size.Infinity);
            var arrangeSize = target.Arrange(context, desiredSize);
            Assert.Equal(new Size(10, 10), arrangeSize);
        }

        private NonVirtualizingLayoutContext CreateContext(Control[] children)
        {
            return new TestLayoutContext(children);
        }

        private class TestLayoutContext : NonVirtualizingLayoutContext
        {
            public TestLayoutContext(Control[] children) => ChildrenCore = children;
            protected override IReadOnlyList<ILayoutable> ChildrenCore { get; }
        }

        private class TestControl : Control
        {
            public Size MeasureConstraint { get; private set; }
            public Size MeasureSize { get; set; }

            protected override Size MeasureOverride(Size availableSize)
            {
                MeasureConstraint = availableSize;
                return MeasureSize;
            }
        }
    }
}
