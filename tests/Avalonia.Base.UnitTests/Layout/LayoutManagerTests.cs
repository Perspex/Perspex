using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Layout;
using Xunit;

namespace Avalonia.Base.UnitTests.Layout
{
    public class LayoutManagerTests
    {
        [Fact]
        public void Measures_And_Arranges_InvalidateMeasured_Control()
        {
            var control = new LayoutTestControl();
            var root = new LayoutTestRoot { Child = control };

            root.LayoutManager.ExecuteInitialLayoutPass();
            control.Measured = control.Arranged = false;

            control.InvalidateMeasure();
            root.LayoutManager.ExecuteLayoutPass();

            Assert.True(control.Measured);
            Assert.True(control.Arranged);
        }

        [Fact]
        public void Doesnt_Measure_And_Arrange_InvalidateMeasured_Control_When_TopLevel_Is_Not_Visible()
        {
            var control = new LayoutTestControl();
            var root = new LayoutTestRoot { Child = control, IsVisible = false };

            root.LayoutManager.ExecuteInitialLayoutPass();
            control.Measured = control.Arranged = false;

            control.InvalidateMeasure();
            root.LayoutManager.ExecuteLayoutPass();

            Assert.False(control.Measured);
            Assert.False(control.Arranged);
        }

        [Fact]
        public void Arranges_InvalidateArranged_Control()
        {
            var control = new LayoutTestControl();
            var root = new LayoutTestRoot { Child = control };

            root.LayoutManager.ExecuteInitialLayoutPass();
            control.Measured = control.Arranged = false;

            control.InvalidateArrange();
            root.LayoutManager.ExecuteLayoutPass();

            Assert.False(control.Measured);
            Assert.True(control.Arranged);
        }

        [Fact]
        public void Measures_Parent_Of_Newly_Added_Control()
        {
            var control = new LayoutTestControl();
            var root = new LayoutTestRoot();

            root.LayoutManager.ExecuteInitialLayoutPass();
            root.Child = control;
            root.Measured = root.Arranged = false;

            root.LayoutManager.ExecuteLayoutPass();

            Assert.True(root.Measured);
            Assert.True(root.Arranged);
            Assert.True(control.Measured);
            Assert.True(control.Arranged);
        }

        [Fact]
        public void Measures_In_Correct_Order()
        {
            LayoutTestControl control1;
            LayoutTestControl control2;
            var root = new LayoutTestRoot
            {
                Child = control1 = new LayoutTestControl
                {
                    Child = control2 = new LayoutTestControl(),
                }
            };

            var order = new List<ILayoutable>();
            Size MeasureOverride(ILayoutable control, Size size)
            {
                order.Add(control);
                return new Size(10, 10);
            }

            root.DoMeasureOverride = MeasureOverride;
            control1.DoMeasureOverride = MeasureOverride;
            control2.DoMeasureOverride = MeasureOverride;
            root.LayoutManager.ExecuteInitialLayoutPass();

            control2.InvalidateMeasure();
            control1.InvalidateMeasure();
            root.InvalidateMeasure();

            order.Clear();
            root.LayoutManager.ExecuteLayoutPass();

            Assert.Equal(new ILayoutable[] { root, control1, control2 }, order);
        }

        [Fact]
        public void Measures_Root_And_Grandparent_In_Correct_Order()
        {
            LayoutTestControl control1;
            LayoutTestControl control2;
            var root = new LayoutTestRoot
            {
                Child = control1 = new LayoutTestControl
                {
                    Child = control2 = new LayoutTestControl(),
                }
            };

            var order = new List<ILayoutable>();
            Size MeasureOverride(ILayoutable control, Size size)
            {
                order.Add(control);
                return new Size(10, 10);
            }

            root.DoMeasureOverride = MeasureOverride;
            control1.DoMeasureOverride = MeasureOverride;
            control2.DoMeasureOverride = MeasureOverride;
            root.LayoutManager.ExecuteInitialLayoutPass();

            control2.InvalidateMeasure();
            root.InvalidateMeasure();

            order.Clear();
            root.LayoutManager.ExecuteLayoutPass();

            Assert.Equal(new ILayoutable[] { root, control2 }, order);
        }

        [Fact]
        public void Doesnt_Measure_Non_Invalidated_Root()
        {
            var control = new LayoutTestControl();
            var root = new LayoutTestRoot { Child = control };

            root.LayoutManager.ExecuteInitialLayoutPass();
            root.Measured = root.Arranged = false;
            control.Measured = control.Arranged = false;

            control.InvalidateMeasure();
            root.LayoutManager.ExecuteLayoutPass();

            Assert.False(root.Measured);
            Assert.False(root.Arranged);
            Assert.True(control.Measured);
            Assert.True(control.Arranged);
        }

        [Fact]
        public void Doesnt_Measure_Removed_Control()
        {
            var control = new LayoutTestControl();
            var root = new LayoutTestRoot { Child = control };

            root.LayoutManager.ExecuteInitialLayoutPass();
            control.Measured = control.Arranged = false;

            control.InvalidateMeasure();
            root.Child = null;
            root.LayoutManager.ExecuteLayoutPass();

            Assert.False(control.Measured);
            Assert.False(control.Arranged);
        }

        [Fact]
        public void Measures_Root_With_Infinity()
        {
            var root = new LayoutTestRoot();
            var availableSize = default(Size);

            // Should not measure with this size.
            root.MaxClientSize = new Size(123, 456);

            root.DoMeasureOverride = (_, s) =>
            {
                availableSize = s;
                return new Size(100, 100);
            };

            root.LayoutManager.ExecuteInitialLayoutPass();

            Assert.Equal(Size.Infinity, availableSize);
        }

        [Fact]
        public void Arranges_Root_With_DesiredSize()
        {
            var root = new LayoutTestRoot
            {
                Width = 100,
                Height = 100,
            };

            var arrangeSize = default(Size);

            root.DoArrangeOverride = (_, s) =>
            {
                arrangeSize = s;
                return s;
            };

            root.LayoutManager.ExecuteInitialLayoutPass();
            Assert.Equal(new Size(100, 100), arrangeSize);

            root.Width = 120;

            root.LayoutManager.ExecuteLayoutPass();
            Assert.Equal(new Size(120, 100), arrangeSize);
        }

        [Fact]
        public void Invalidating_Child_Remeasures_Parent()
        {
            Border border;
            StackPanel panel;

            var root = new LayoutTestRoot
            {
                Child = panel = new StackPanel
                {
                    Children =
                    {
                        (border = new Border())
                    }
                }
            };

            root.LayoutManager.ExecuteInitialLayoutPass();
            Assert.Equal(new Size(0, 0), root.DesiredSize);

            border.Width = 100;
            border.Height = 100;

            root.LayoutManager.ExecuteLayoutPass();
            Assert.Equal(new Size(100, 100), panel.DesiredSize);
        }

        [Fact]
        public void LayoutManager_Should_Prevent_Infinite_Loop_On_Measure()
        {
            var control = new LayoutTestControl();
            var root = new LayoutTestRoot { Child = control };

            root.LayoutManager.ExecuteInitialLayoutPass();
            control.Measured = false;

            int cnt = 0;
            int maxcnt = 100;
            control.DoMeasureOverride = (l, s) =>
            {
                //emulate a problem in the logic of a control that triggers
                //invalidate measure during measure
                //it can lead to an infinite loop in layoutmanager
                if (++cnt < maxcnt)
                {
                    control.InvalidateMeasure();
                }

                return new Size(100, 100);
            };

            control.InvalidateMeasure();

            root.LayoutManager.ExecuteLayoutPass();

            Assert.True(cnt < 100);
        }

        [Fact]
        public void LayoutManager_Should_Prevent_Infinite_Loop_On_Arrange()
        {
            var control = new LayoutTestControl();
            var root = new LayoutTestRoot { Child = control };

            root.LayoutManager.ExecuteInitialLayoutPass();
            control.Arranged = false;

            int cnt = 0;
            int maxcnt = 100;
            control.DoArrangeOverride = (l, s) =>
            {
                //emulate a problem in the logic of a control that triggers
                //invalidate measure during arrange
                //it can lead to infinity loop in layoutmanager
                if (++cnt < maxcnt)
                {
                    control.InvalidateArrange();
                }

                return new Size(100, 100);
            };

            control.InvalidateArrange();

            root.LayoutManager.ExecuteLayoutPass();

            Assert.True(cnt < 100);
        }

        [Fact]
        public void LayoutManager_Should_Properly_Arrange_Visuals_Even_When_There_Are_Issues_With_Previous_Arranged()
        {
            var nonArrageableTargets = Enumerable.Range(1, 10).Select(_ => new LayoutTestControl()).ToArray();
            var targets = Enumerable.Range(1, 10).Select(_ => new LayoutTestControl()).ToArray();

            StackPanel panel;

            var root = new LayoutTestRoot
            {
                Child = panel = new StackPanel()
            };

            panel.Children.AddRange(nonArrageableTargets);
            panel.Children.AddRange(targets);

            root.LayoutManager.ExecuteInitialLayoutPass();

            foreach (var c in panel.Children.OfType<LayoutTestControl>())
            {
                c.Measured = c.Arranged = false;
                c.InvalidateMeasure();
            }

            foreach (var c in nonArrageableTargets)
            {
                c.DoArrangeOverride = (l, s) =>
                {
                    //emulate a problem in the logic of a control that triggers
                    //invalidate measure during arrange
                    c.InvalidateMeasure();
                    return new Size(100, 100);
                };
            }

            root.LayoutManager.ExecuteLayoutPass();

            //altough nonArrageableTargets has rubbish logic and can't be measured/arranged properly
            //layoutmanager should process properly other visuals
            Assert.All(targets, c => Assert.True(c.Arranged));
        }


        [Fact]
        public void LayoutManager_Should_Recover_From_Infinite_Loop_On_Measure()
        {
            // Test for issue #3041.
            var control = new LayoutTestControl();
            var root = new LayoutTestRoot { Child = control };

            root.LayoutManager.ExecuteInitialLayoutPass();
            control.Measured = false;

            control.DoMeasureOverride = (l, s) =>
            {
                control.InvalidateMeasure();
                return new Size(100, 100);
            };

            control.InvalidateMeasure();
            root.LayoutManager.ExecuteLayoutPass();

            // This is the important part: running a second layout pass in which we exceed the maximum
            // retries causes LayoutQueue<T>.Info.Count to exceed _maxEnqueueCountPerLoop.
            root.LayoutManager.ExecuteLayoutPass();

            control.Measured = false;
            control.DoMeasureOverride = null;

            root.LayoutManager.ExecuteLayoutPass();

            Assert.True(control.Measured);
            Assert.True(control.IsMeasureValid);
        }

        [Fact]
        public void Calling_ExecuteLayoutPass_From_ExecuteInitialLayoutPass_Does_Not_Break_Measure()
        {
            // Test for issue #3550.
            var control = new LayoutTestControl();
            var root = new LayoutTestRoot { Child = control };
            var count = 0;

            root.LayoutManager.ExecuteInitialLayoutPass();
            control.Measured = false;

            control.DoMeasureOverride = (l, s) =>
            {
                if (count++ == 0)
                {
                    control.InvalidateMeasure();
                    root.LayoutManager.ExecuteLayoutPass();
                    return new Size(100, 100);
                }
                else
                {
                    return new Size(200, 200);
                }
            };

            root.InvalidateMeasure();
            control.InvalidateMeasure();
            root.LayoutManager.ExecuteInitialLayoutPass();

            Assert.Equal(new Size(200, 200), control.Bounds.Size);
            Assert.Equal(new Size(200, 200), control.DesiredSize);
        }
    }
}
