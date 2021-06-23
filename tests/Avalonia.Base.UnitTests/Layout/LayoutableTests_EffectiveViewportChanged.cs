using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Base.UnitTests.Layout
{
    public class LayoutableTests_EffectiveViewportChanged
    {
        [Fact]
        public async Task EffectiveViewportChanged_Not_Raised_When_Control_Added_To_Tree()
        {
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
            await RunOnUIThread.Execute(async () =>
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
            {
                var root = CreateRoot();
                var target = new Canvas();
                var raised = 0;

                target.EffectiveViewportChanged += (s, e) =>
                {
                    ++raised;
                };

                root.Child = target;

                Assert.Equal(0, raised);
            });
        }

        [Fact]
        public async Task EffectiveViewportChanged_Raised_Before_LayoutUpdated()
        {
            await RunOnUIThread.Execute(async () =>
            {
                var root = CreateRoot();
                var target = new Canvas();
                var raised = 0;

                target.EffectiveViewportChanged += (s, e) =>
                {
                    ++raised;
                };

                root.Child = target;

                await ExecuteInitialLayoutPass(root);

                Assert.Equal(1, raised);
            });
        }

        [Fact]
        public async Task Parent_Affects_EffectiveViewport()
        {
            await RunOnUIThread.Execute(async () =>
            {
                var root = CreateRoot();
                var target = new Canvas { Width = 100, Height = 100 };
                var parent = new Border { Width = 200, Height = 200, Child = target };
                var raised = 0;

                root.Child = parent;

                target.EffectiveViewportChanged += (s, e) =>
                {
                    Assert.Equal(new Rect(-550, -400, 1200, 900), e.EffectiveViewport);
                    ++raised;
                };

                await ExecuteInitialLayoutPass(root);
            });
        }

        [Fact]
        public async Task Invalidating_In_Handler_Causes_Layout_To_Be_Rerun_Before_LayoutUpdated_Raised()
        {
            await RunOnUIThread.Execute(async () =>
            {
                var root = CreateRoot();
                var target = new TestCanvas();
                var raised = 0;
                var layoutUpdatedRaised = 0;

                root.LayoutUpdated += (s, e) =>
                {
                    Assert.Equal(2, target.MeasureCount);
                    Assert.Equal(2, target.ArrangeCount);
                    ++layoutUpdatedRaised;
                };

                target.EffectiveViewportChanged += (s, e) =>
                {
                    target.InvalidateMeasure();
                    ++raised;
                };

                root.Child = target;

                await ExecuteInitialLayoutPass(root);

                Assert.Equal(1, raised);
                Assert.Equal(1, layoutUpdatedRaised);
            });
        }

        [Fact]
        public async Task Viewport_Extends_Beyond_Centered_Control()
        {
            await RunOnUIThread.Execute(async () =>
            {
                var root = CreateRoot();
                var target = new Canvas { Width = 52, Height = 52, };
                var raised = 0;

                target.EffectiveViewportChanged += (s, e) =>
                {
                    Assert.Equal(new Rect(-574, -424, 1200, 900), e.EffectiveViewport);
                    ++raised;
                };

                root.Child = target;

                await ExecuteInitialLayoutPass(root);
                Assert.Equal(1, raised);
            });
        }

        [Fact]
        public async Task Viewport_Extends_Beyond_Nested_Centered_Control()
        {
            await RunOnUIThread.Execute(async () =>
            {
                var root = CreateRoot();
                var target = new Canvas { Width = 52, Height = 52 };
                var parent = new Border { Width = 100, Height = 100, Child = target };
                var raised = 0;

                target.EffectiveViewportChanged += (s, e) =>
                {
                    Assert.Equal(new Rect(-574, -424, 1200, 900), e.EffectiveViewport);
                    ++raised;
                };

                root.Child = parent;

                await ExecuteInitialLayoutPass(root);
                Assert.Equal(1, raised);
            });
        }

        [Fact]
        public async Task ScrollViewer_Determines_EffectiveViewport()
        {
            await RunOnUIThread.Execute(async () =>
            {
                var root = CreateRoot();
                var target = new Canvas { Width = 200, Height = 200 };
                var scroller = new ScrollViewer { Width = 100, Height = 100, Content = target, Template = ScrollViewerTemplate(), HorizontalScrollBarVisibility = ScrollBarVisibility.Hidden };
                var raised = 0;

                target.EffectiveViewportChanged += (s, e) =>
                {
                    Assert.Equal(new Rect(0, 0, 100, 100), e.EffectiveViewport);
                    ++raised;
                };

                root.Child = scroller;

                await ExecuteInitialLayoutPass(root);
                Assert.Equal(1, raised);
            });
        }

        [Fact]
        public async Task Scrolled_ScrollViewer_Determines_EffectiveViewport()
        {
            await RunOnUIThread.Execute(async () =>
            {
                var root = CreateRoot();
                var target = new Canvas { Width = 200, Height = 200 };
                var scroller = new ScrollViewer { Width = 100, Height = 100, Content = target, Template = ScrollViewerTemplate(), HorizontalScrollBarVisibility = ScrollBarVisibility.Hidden };
                var raised = 0;

                root.Child = scroller;

                await ExecuteInitialLayoutPass(root);
                scroller.Offset = new Vector(0, 10);

                await ExecuteScrollerLayoutPass(root, scroller, target, (s, e) =>
                {
                    Assert.Equal(new Rect(0, 10, 100, 100), e.EffectiveViewport);
                    ++raised;
                });

                Assert.Equal(1, raised);
            });
        }

        [Fact]
        public async Task Moving_Parent_Updates_EffectiveViewport()
        {
            await RunOnUIThread.Execute(async () =>
            {
                var root = CreateRoot();
                var target = new Canvas { Width = 100, Height = 100 };
                var parent = new Border { Width = 200, Height = 200, Child = target };
                var raised = 0;

                root.Child = parent;

                await ExecuteInitialLayoutPass(root);

                target.EffectiveViewportChanged += (s, e) =>
                {
                    Assert.Equal(new Rect(-554, -400, 1200, 900), e.EffectiveViewport);
                    ++raised;
                };

                parent.Margin = new Thickness(8, 0, 0, 0);
                await ExecuteLayoutPass(root);

                Assert.Equal(1, raised);
            });
        }

        [Fact]
        public async Task Translate_Transform_Doesnt_Affect_EffectiveViewport()
        {
            await RunOnUIThread.Execute(async () =>
            {
                var root = CreateRoot();
                var target = new Canvas { Width = 100, Height = 100 };
                var parent = new Border { Width = 200, Height = 200, Child = target };
                var raised = 0;

                root.Child = parent;

                await ExecuteInitialLayoutPass(root);
                target.EffectiveViewportChanged += (s, e) => ++raised;
                target.RenderTransform = new TranslateTransform { X = 8 };
                target.InvalidateMeasure();
                await ExecuteLayoutPass(root);

                Assert.Equal(0, raised);
            });
        }

        [Fact]
        public async Task Translate_Transform_On_Parent_Affects_EffectiveViewport()
        {
            await RunOnUIThread.Execute(async () =>
            {
                var root = CreateRoot();
                var target = new Canvas { Width = 100, Height = 100 };
                var parent = new Border { Width = 200, Height = 200, Child = target };
                var raised = 0;

                root.Child = parent;

                await ExecuteInitialLayoutPass(root);

                target.EffectiveViewportChanged += (s, e) =>
                {
                    Assert.Equal(new Rect(-558, -400, 1200, 900), e.EffectiveViewport);
                    ++raised;
                };

                // Change the parent render transform to move it. A layout is then needed before
                // EffectiveViewportChanged is raised.
                parent.RenderTransform = new TranslateTransform { X = 8 };
                parent.InvalidateMeasure();
                await ExecuteLayoutPass(root);

                Assert.Equal(1, raised);
            });
        }

        [Fact]
        public async Task Rotate_Transform_On_Parent_Affects_EffectiveViewport()
        {
            await RunOnUIThread.Execute(async () =>
            {
                var root = CreateRoot();
                var target = new Canvas { Width = 100, Height = 100 };
                var parent = new Border { Width = 200, Height = 200, Child = target };
                var raised = 0;

                root.Child = parent;

                await ExecuteInitialLayoutPass(root);

                target.EffectiveViewportChanged += (s, e) =>
                {
                    AssertArePixelEqual(new Rect(-651, -792, 1484, 1484), e.EffectiveViewport);
                    ++raised;
                };

                parent.RenderTransformOrigin = new RelativePoint(0, 0, RelativeUnit.Absolute);
                parent.RenderTransform = new RotateTransform { Angle = 45 };
                parent.InvalidateMeasure();
                await ExecuteLayoutPass(root);

                Assert.Equal(1, raised);
            });
        }

        private TestRoot CreateRoot() => new TestRoot { Width = 1200, Height = 900 };

        private Task ExecuteInitialLayoutPass(TestRoot root)
        {
            root.LayoutManager.ExecuteInitialLayoutPass();
            return Task.CompletedTask;
        }

        private Task ExecuteLayoutPass(TestRoot root)
        {
            root.LayoutManager.ExecuteLayoutPass();
            return Task.CompletedTask;
        }

        private Task ExecuteScrollerLayoutPass(
            TestRoot root,
            ScrollViewer scroller,
            Control target,
            Action<object, EffectiveViewportChangedEventArgs> handler)
        {
            void ViewportChanged(object sender, EffectiveViewportChangedEventArgs e)
            {
                handler(sender, e);
            }

            target.EffectiveViewportChanged += ViewportChanged;
            root.LayoutManager.ExecuteLayoutPass();
            return Task.CompletedTask;
        }
        private IControlTemplate ScrollViewerTemplate()
        {
            return new FuncControlTemplate<ScrollViewer>((control, scope) => new Grid
            {
                ColumnDefinitions = new ColumnDefinitions
                {
                    new ColumnDefinition(1, GridUnitType.Star),
                    new ColumnDefinition(GridLength.Auto),
                },
                RowDefinitions = new RowDefinitions
                {
                    new RowDefinition(1, GridUnitType.Star),
                    new RowDefinition(GridLength.Auto),
                },
                Children =
                {
                    new ScrollContentPresenter
                    {
                        Name = "PART_ContentPresenter",
                        [~ContentPresenter.ContentProperty] = control[~ContentControl.ContentProperty],
                        [~~ScrollContentPresenter.ExtentProperty] = control[~~ScrollViewer.ExtentProperty],
                        [~~ScrollContentPresenter.OffsetProperty] = control[~~ScrollViewer.OffsetProperty],
                        [~~ScrollContentPresenter.ViewportProperty] = control[~~ScrollViewer.ViewportProperty],
                        [~ScrollContentPresenter.CanHorizontallyScrollProperty] = control[~ScrollViewer.CanHorizontallyScrollProperty],
                        [~ScrollContentPresenter.CanVerticallyScrollProperty] = control[~ScrollViewer.CanVerticallyScrollProperty],
                    }.RegisterInNameScope(scope),
                    new ScrollBar
                    {
                        Name = "horizontalScrollBar",
                        Orientation = Orientation.Horizontal,
                        [~RangeBase.MaximumProperty] = control[~ScrollViewer.HorizontalScrollBarMaximumProperty],
                        [~~RangeBase.ValueProperty] = control[~~ScrollViewer.HorizontalScrollBarValueProperty],
                        [~ScrollBar.ViewportSizeProperty] = control[~ScrollViewer.HorizontalScrollBarViewportSizeProperty],
                        [~ScrollBar.VisibilityProperty] = control[~ScrollViewer.HorizontalScrollBarVisibilityProperty],
                        [Grid.RowProperty] = 1,
                    }.RegisterInNameScope(scope),
                    new ScrollBar
                    {
                        Name = "verticalScrollBar",
                        Orientation = Orientation.Vertical,
                        [~RangeBase.MaximumProperty] = control[~ScrollViewer.VerticalScrollBarMaximumProperty],
                        [~~RangeBase.ValueProperty] = control[~~ScrollViewer.VerticalScrollBarValueProperty],
                        [~ScrollBar.ViewportSizeProperty] = control[~ScrollViewer.VerticalScrollBarViewportSizeProperty],
                        [~ScrollBar.VisibilityProperty] = control[~ScrollViewer.VerticalScrollBarVisibilityProperty],
                        [Grid.ColumnProperty] = 1,
                    }.RegisterInNameScope(scope),
                },
            });
        }

        private void AssertArePixelEqual(Rect expected, Rect actual)
        {
            var expectedRounded = new Rect((int)expected.X, (int)expected.Y, (int)expected.Width, (int)expected.Height);
            var actualRounded = new Rect((int)actual.X, (int)actual.Y, (int)actual.Width, (int)actual.Height);
            Assert.Equal(expectedRounded, actualRounded);
        }

        private class TestCanvas : Canvas
        {
            public int MeasureCount { get; private set; }
            public int ArrangeCount { get; private set; }

            protected override Size MeasureOverride(Size availableSize)
            {
                ++MeasureCount;
                return base.MeasureOverride(availableSize);
            }

            protected override Size ArrangeOverride(Size finalSize)
            {
                ++ArrangeCount;
                return base.ArrangeOverride(finalSize);
            }
        }

        private static class RunOnUIThread
        {
            public static async Task Execute(Func<Task> func)
            {
                await func();
            }
        }
    }
}
