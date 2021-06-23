using System;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.UnitTests;
using Moq;
using Xunit;

namespace Avalonia.Base.UnitTests.Layout
{
    public class LayoutableTests
    {
        [Theory]
        [InlineData(0, 0, 0, 0, 100, 100)]
        [InlineData(10, 0, 0, 0, 90, 100)]
        [InlineData(10, 0, 5, 0, 85, 100)]
        [InlineData(0, 10, 0, 0, 100, 90)]
        [InlineData(0, 10, 0, 5, 100, 85)]
        [InlineData(4, 4, 6, 7, 90, 89)]
        public void Margin_Is_Applied_To_MeasureOverride_Size(
            double l,
            double t,
            double r,
            double b,
            double expectedWidth,
            double expectedHeight)
        {
            var target = new TestLayoutable
            {
                Margin = new Thickness(l, t, r, b),
            };

            target.Measure(new Size(100, 100));

            Assert.Equal(new Size(expectedWidth, expectedHeight), target.MeasureSize);
        }

        [Theory]
        [InlineData(HorizontalAlignment.Stretch, 100)]
        [InlineData(HorizontalAlignment.Left, 10)]
        [InlineData(HorizontalAlignment.Center, 10)]
        [InlineData(HorizontalAlignment.Right, 10)]
        public void HorizontalAlignment_Is_Applied_To_ArrangeOverride_Size(
            HorizontalAlignment h,
            double expectedWidth)
        {
            var target = new TestLayoutable
            {
                HorizontalAlignment = h,
            };

            target.Measure(Size.Infinity);
            target.Arrange(new Rect(0, 0, 100, 100));

            Assert.Equal(new Size(expectedWidth, 100), target.ArrangeSize);
        }

        [Theory]
        [InlineData(VerticalAlignment.Stretch, 100)]
        [InlineData(VerticalAlignment.Top, 10)]
        [InlineData(VerticalAlignment.Center, 10)]
        [InlineData(VerticalAlignment.Bottom, 10)]
        public void VerticalAlignment_Is_Applied_To_ArrangeOverride_Size(
            VerticalAlignment v,
            double expectedHeight)
        {
            var target = new TestLayoutable
            {
                VerticalAlignment = v,
            };

            target.Measure(Size.Infinity);
            target.Arrange(new Rect(0, 0, 100, 100));

            Assert.Equal(new Size(100, expectedHeight), target.ArrangeSize);
        }

        [Theory]
        [InlineData(0, 0, 0, 0, 100, 100)]
        [InlineData(10, 0, 0, 0, 90, 100)]
        [InlineData(10, 0, 5, 0, 85, 100)]
        [InlineData(0, 10, 0, 0, 100, 90)]
        [InlineData(0, 10, 0, 5, 100, 85)]
        [InlineData(4, 4, 6, 7, 90, 89)]
        public void Margin_Is_Applied_To_ArrangeOverride_Size(
            double l,
            double t,
            double r,
            double b,
            double expectedWidth,
            double expectedHeight)
        {
            var target = new TestLayoutable
            {
                Margin = new Thickness(l, t, r, b),
            };

            target.Measure(Size.Infinity);
            target.Arrange(new Rect(0, 0, 100, 100));

            Assert.Equal(new Size(expectedWidth, expectedHeight), target.ArrangeSize);
        }

        [Fact]
        public void Only_Calls_LayoutManager_InvalidateMeasure_Once()
        {
            var target = new Mock<ILayoutManager>();
            var control = new Decorator();
            var root = new LayoutTestRoot
            {
                Child = control,
                LayoutManager = target.Object,
            };

            root.Measure(Size.Infinity);
            root.Arrange(new Rect(root.DesiredSize));
            target.Invocations.Clear();

            control.InvalidateMeasure();
            control.InvalidateMeasure();

            target.Verify(x => x.InvalidateMeasure(control), Times.Once());
        }

        [Fact]
        public void Only_Calls_LayoutManager_InvalidateArrange_Once()
        {
            var target = new Mock<ILayoutManager>();
            var control = new Decorator();
            var root = new LayoutTestRoot
            {
                Child = control,
                LayoutManager = target.Object,
            };

            root.Measure(Size.Infinity);
            root.Arrange(new Rect(root.DesiredSize));
            target.Invocations.Clear();

            control.InvalidateArrange();
            control.InvalidateArrange();

            target.Verify(x => x.InvalidateArrange(control), Times.Once());
        }

        [Fact]
        public void Attaching_Control_To_Tree_Invalidates_Parent_Measure()
        {
            var target = new Mock<ILayoutManager>();
            var control = new Decorator();
            var root = new LayoutTestRoot
            {
                Child = control,
                LayoutManager = target.Object,
            };

            root.Measure(Size.Infinity);
            root.Arrange(new Rect(root.DesiredSize));
            Assert.True(control.IsMeasureValid);

            root.Child = null;
            root.Measure(Size.Infinity);
            root.Arrange(new Rect(root.DesiredSize));

            Assert.False(control.IsMeasureValid);
            Assert.True(root.IsMeasureValid);

            target.Invocations.Clear();

            root.Child = control;

            Assert.False(root.IsMeasureValid);
            Assert.False(control.IsMeasureValid);
            target.Verify(x => x.InvalidateMeasure(root), Times.Once());
        }

        [Theory]
        [InlineData(16, 6, 5.333333333333333)]
        [InlineData(18, 10, 4)]
        public void UseLayoutRounding_Arranges_Center_Alignment_Correctly_With_Fractional_Scaling(
            double containerWidth,
            double childWidth,
            double expectedX)
        {
            Border target;
            var root = new TestRoot
            {
                LayoutScaling = 1.5,
                UseLayoutRounding = true,
                Child = new Decorator
                {
                    Width = containerWidth,
                    Height = 100,
                    Child = target = new Border
                    {
                        Width = childWidth,
                        HorizontalAlignment = HorizontalAlignment.Center,
                    }
                }
            };

            root.Measure(new Size(100, 100));
            root.Arrange(new Rect(target.DesiredSize));

            Assert.Equal(new Rect(expectedX, 0, childWidth, 100), target.Bounds);
        }

        [Fact]
        public void LayoutUpdated_Is_Called_At_End_Of_Layout_Pass()
        {
            Border border1;
            Border border2;
            var root = new TestRoot
            {
                Child = border1 = new Border
                {
                    Child = border2 = new Border(),
                },
            };
            var raised = 0;

            void ValidateBounds(object sender, EventArgs e)
            {
                Assert.Equal(new Rect(0, 0, 100, 100), border1.Bounds);
                Assert.Equal(new Rect(0, 0, 100, 100), border2.Bounds);
                ++raised;
            }

            root.LayoutUpdated += ValidateBounds;
            border1.LayoutUpdated += ValidateBounds;
            border2.LayoutUpdated += ValidateBounds;

            root.Measure(new Size(100, 100));
            root.Arrange(new Rect(0, 0, 100, 100));
            
            root.LayoutManager.ExecuteLayoutPass();

            Assert.Equal(3, raised);
            Assert.Equal(new Rect(0, 0, 100, 100), border1.Bounds);
            Assert.Equal(new Rect(0, 0, 100, 100), border2.Bounds);
        }

        [Fact]
        public void LayoutUpdated_Subscribes_To_LayoutManager()
        {
            Border target;
            var layoutManager = new Mock<ILayoutManager>();
            layoutManager.SetupAdd(m => m.LayoutUpdated += (sender, args) => { });

            var root = new TestRoot
            {
                Child = new Border
                {
                    Child = target = new Border(),
                },
                LayoutManager = layoutManager.Object,
            };

            void Handler(object sender, EventArgs e) {}

            layoutManager.Invocations.Clear();
            target.LayoutUpdated += Handler;

            layoutManager.VerifyAdd(
                x => x.LayoutUpdated += It.IsAny<EventHandler>(),
                Times.Once);

            layoutManager.Invocations.Clear();
            target.LayoutUpdated -= Handler;

            layoutManager.VerifyRemove(
                x => x.LayoutUpdated -= It.IsAny<EventHandler>(),
                Times.Once);
        }

        [Fact]
        public void LayoutManager_LayoutUpdated_Is_Subscribed_When_Attached_To_Tree()
        {
            Border border1;
            var layoutManager = new Mock<ILayoutManager>();
            layoutManager.SetupAdd(m => m.LayoutUpdated += (sender, args) => { });

            var root = new TestRoot
            {
                Child = border1 = new Border(),
                LayoutManager = layoutManager.Object,
            };

            var border2 = new Border();
            border2.LayoutUpdated += (s, e) => { };

            layoutManager.Invocations.Clear();
            border1.Child = border2;

            layoutManager.VerifyAdd(
                x => x.LayoutUpdated += It.IsAny<EventHandler>(),
                Times.Once);
        }

        [Fact]
        public void LayoutManager_LayoutUpdated_Is_Unsubscribed_When_Detached_From_Tree()
        {
            Border border1;
            var layoutManager = new Mock<ILayoutManager>();
            layoutManager.SetupAdd(m => m.LayoutUpdated += (sender, args) => { });

            var root = new TestRoot
            {
                Child = border1 = new Border(),
                LayoutManager = layoutManager.Object,
            };

            var border2 = new Border();
            border2.LayoutUpdated += (s, e) => { };
            border1.Child = border2;

            layoutManager.Invocations.Clear();
            border1.Child = null;

            layoutManager.VerifyRemove(
                x => x.LayoutUpdated -= It.IsAny<EventHandler>(),
                Times.Once);
        }

        private class TestLayoutable : Layoutable
        {
            public Size ArrangeSize { get; private set; }
            public Size MeasureResult { get; set; } = new Size(10, 10);
            public Size MeasureSize { get; private set; }

            protected override Size MeasureOverride(Size availableSize)
            {
                MeasureSize = availableSize;
                return MeasureResult;
            }

            protected override Size ArrangeOverride(Size finalSize)
            {
                ArrangeSize = finalSize;
                return base.ArrangeOverride(finalSize);
            }
        }
    }
}
