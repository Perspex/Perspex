using System;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Rendering;
using Avalonia.UnitTests;
using Avalonia.VisualTree;
using Moq;
using Xunit;

namespace Avalonia.Visuals.UnitTests.Rendering
{
    public class DeferredRendererTests_HitTesting
    {
        [Fact]
        public void HitTest_Should_Find_Controls_At_Point()
        {
            using (TestApplication())
            {
                var root = new TestRoot
                {
                    Width = 200,
                    Height = 200,
                    Child = new Border
                    {
                        Width = 100,
                        Height = 100,
                        Background = Brushes.Red,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center
                    }
                };

                root.Renderer = new DeferredRenderer(root, null);
                root.Measure(Size.Infinity);
                root.Arrange(new Rect(root.DesiredSize));

                var result = root.Renderer.HitTest(new Point(100, 100), root, null);

                Assert.Equal(new[] { root.Child }, result);
            }
        }

        [Fact]
        public void HitTest_Should_Not_Find_Empty_Controls_At_Point()
        {
            using (TestApplication())
            {
                var root = new TestRoot
                {
                    Width = 200,
                    Height = 200,
                    Child = new Border
                    {
                        Width = 100,
                        Height = 100,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center
                    }
                };

                root.Renderer = new DeferredRenderer(root, null);
                root.Measure(Size.Infinity);
                root.Arrange(new Rect(root.DesiredSize));

                var result = root.Renderer.HitTest(new Point(100, 100), root, null);

                Assert.Empty(result);
            }
        }

        [Fact]
        public void HitTest_Should_Not_Find_Invisible_Controls_At_Point()
        {
            using (TestApplication())
            {
                Border visible;
                var root = new TestRoot
                {
                    Width = 200,
                    Height = 200,
                    Child = new Border
                    {
                        Width = 100,
                        Height = 100,
                        Background = Brushes.Red,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center,
                        IsVisible = false,
                        Child = visible = new Border
                        {
                            Background = Brushes.Red,
                            HorizontalAlignment = HorizontalAlignment.Stretch,
                            VerticalAlignment = VerticalAlignment.Stretch,
                        }
                    }
                };

                root.Renderer = new DeferredRenderer(root, null);
                root.Measure(Size.Infinity);
                root.Arrange(new Rect(root.DesiredSize));

                var result = root.Renderer.HitTest(new Point(100, 100), root, null);

                Assert.Empty(result);
            }
        }

        [Fact]
        public void HitTest_Should_Not_Find_Control_Outside_Point()
        {
            using (TestApplication())
            {
                var root = new TestRoot
                {
                    Width = 200,
                    Height = 200,
                    Child = new Border
                    {
                        Width = 100,
                        Height = 100,
                        Background = Brushes.Red,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center
                    }
                };

                root.Renderer = new DeferredRenderer(root, null);
                root.Measure(Size.Infinity);
                root.Arrange(new Rect(root.DesiredSize));

                var result = root.Renderer.HitTest(new Point(10, 10), root, null);

                Assert.Empty(result);
            }
        }

        [Fact]
        public void HitTest_Should_Return_Top_Controls_First()
        {
            using (TestApplication())
            {
                Panel container;
                var root = new TestRoot
                {
                    Child = container = new Panel
                    {
                        Width = 200,
                        Height = 200,
                        Children =
                        {
                            new Border
                            {
                                Width = 100,
                                Height = 100,
                                Background = Brushes.Red,
                                HorizontalAlignment = HorizontalAlignment.Center,
                                VerticalAlignment = VerticalAlignment.Center
                            },
                            new Border
                            {
                                Width = 50,
                                Height = 50,
                                Background = Brushes.Red,
                                HorizontalAlignment = HorizontalAlignment.Center,
                                VerticalAlignment = VerticalAlignment.Center
                            }
                        }
                    }
                };

                root.Renderer = new DeferredRenderer(root, null);
                root.Measure(Size.Infinity);
                root.Arrange(new Rect(container.DesiredSize));

                var result = root.Renderer.HitTest(new Point(100, 100), root, null);

                Assert.Equal(new[] { container.Children[1], container.Children[0] }, result);
            }
        }

        [Fact]
        public void HitTest_Should_Return_Top_Controls_First_With_ZIndex()
        {
            using (TestApplication())
            {
                Panel container;
                var root = new TestRoot
                {
                    Child = container = new Panel
                    {
                        Width = 200,
                        Height = 200,
                        Children =
                        {
                            new Border
                            {
                                Width = 100,
                                Height = 100,
                                ZIndex = 1,
                                Background = Brushes.Red,
                                HorizontalAlignment = HorizontalAlignment.Center,
                                VerticalAlignment = VerticalAlignment.Center
                            },
                            new Border
                            {
                                Width = 50,
                                Height = 50,
                                Background = Brushes.Red,
                                HorizontalAlignment = HorizontalAlignment.Center,
                                VerticalAlignment = VerticalAlignment.Center
                            },
                            new Border
                            {
                                Width = 75,
                                Height = 75,
                                ZIndex = 2,
                                Background = Brushes.Red,
                                HorizontalAlignment = HorizontalAlignment.Center,
                                VerticalAlignment = VerticalAlignment.Center
                            }
                        }
                    }
                };

                root.Renderer = new DeferredRenderer(root, null);
                root.Measure(Size.Infinity);
                root.Arrange(new Rect(container.DesiredSize));

                var result = root.Renderer.HitTest(new Point(100, 100), root, null);

                Assert.Equal(new[] { container.Children[2], container.Children[0], container.Children[1] }, result);
            }
        }

        [Fact]
        public void HitTest_Should_Find_Control_Translated_Outside_Parent_Bounds()
        {
            using (TestApplication())
            {
                Border target;
                Panel container;
                var root = new TestRoot
                {
                    Child = container = new Panel
                    {
                        Width = 200,
                        Height = 200,
                        Background = Brushes.Red,
                        ClipToBounds = false,
                        Children =
                        {
                            new Border
                            {
                                Width = 100,
                                Height = 100,
                                ZIndex = 1,
                                Background = Brushes.Red,
                                HorizontalAlignment = HorizontalAlignment.Left,
                                VerticalAlignment = VerticalAlignment.Top,
                                Child = target = new Border
                                {
                                    Width = 50,
                                    Height = 50,
                                    Background = Brushes.Red,
                                    HorizontalAlignment = HorizontalAlignment.Left,
                                    VerticalAlignment = VerticalAlignment.Top,
                                    RenderTransform = new TranslateTransform(110, 110),
                                }
                            },
                        }
                    }
                };

                root.Renderer = new DeferredRenderer(root, null);
                container.Measure(Size.Infinity);
                container.Arrange(new Rect(container.DesiredSize));

                var result = root.Renderer.HitTest(new Point(120, 120), root, null);

                Assert.Equal(new IVisual[] { target, container }, result);
            }
        }

        [Fact]
        public void HitTest_Should_Not_Find_Control_Outside_Parent_Bounds_When_Clipped()
        {
            using (TestApplication())
            {
                Border target;
                Panel container;
                var root = new TestRoot
                {
                    Child = container = new Panel
                    {
                        Width = 100,
                        Height = 200,
                        Background = Brushes.Red,
                        Children =
                        {
                            new Panel()
                            {
                                Width = 100,
                                Height = 100,
                                Background = Brushes.Red,
                                Margin = new Thickness(0, 100, 0, 0),
                                ClipToBounds = true,
                                Children =
                                {
                                    (target = new Border()
                                    {
                                        Width = 100,
                                        Height = 100,
                                        Background = Brushes.Red,
                                        Margin = new Thickness(0, -100, 0, 0)
                                    })
                                }
                            }
                        }
                    }
                };

                root.Renderer = new DeferredRenderer(root, null);
                root.Measure(Size.Infinity);
                root.Arrange(new Rect(container.DesiredSize));

                var result = root.Renderer.HitTest(new Point(50, 50), root, null);

                Assert.Equal(new[] { container }, result);
            }
        }

        [Fact]
        public void HitTest_Should_Not_Find_Control_Outside_Scroll_Viewport()
        {
            using (TestApplication())
            {
                Border target;
                Border item1;
                Border item2;
                ScrollContentPresenter scroll;
                Panel container;
                var root = new TestRoot
                {
                    Child = container = new Panel
                    {
                        Width = 100,
                        Height = 200,
                        Background = Brushes.Red,
                        Children =
                        {
                            (target = new Border()
                            {
                                Name = "b1",
                                Width = 100,
                                Height = 100,
                                Background = Brushes.Red,
                            }),
                            new Border()
                            {
                                Name = "b2",
                                Width = 100,
                                Height = 100,
                                Background = Brushes.Red,
                                Margin = new Thickness(0, 100, 0, 0),
                                Child = scroll = new ScrollContentPresenter()
                                {
                                    CanHorizontallyScroll = true,
                                    CanVerticallyScroll = true,
                                    Content = new StackPanel()
                                    {
                                        Children =
                                        {
                                            (item1 = new Border()
                                            {
                                                Name = "b3",
                                                Width = 100,
                                                Height = 100,
                                                Background = Brushes.Red,
                                            }),
                                            (item2 = new Border()
                                            {
                                                Name = "b4",
                                                Width = 100,
                                                Height = 100,
                                                Background = Brushes.Red,
                                            }),
                                        }
                                    }
                                }
                            }
                        }
                    }
                };

                scroll.UpdateChild();

                root.Renderer = new DeferredRenderer(root, null);
                root.Measure(Size.Infinity);
                root.Arrange(new Rect(container.DesiredSize));
                
                root.Renderer.Paint(Rect.Empty);
                var result = root.Renderer.HitTest(new Point(50, 150), root, null).First();

                Assert.Equal(item1, result);

                result = root.Renderer.HitTest(new Point(50, 50), root, null).First();

                Assert.Equal(target, result);

                scroll.Offset = new Vector(0, 100);

                // We don't have LayoutManager set up so do the layout pass manually.
                scroll.Parent.InvalidateArrange();
                container.InvalidateArrange();
                container.Arrange(new Rect(container.DesiredSize));

                root.Renderer.Paint(Rect.Empty);
                result = root.Renderer.HitTest(new Point(50, 150), root, null).First();
                Assert.Equal(item2, result);

                result = root.Renderer.HitTest(new Point(50, 50), root, null).First();
                Assert.Equal(target, result);
            }
        }

        [Fact]
        public void HitTest_Should_Not_Find_Path_When_Outside_Fill()
        {
            using (TestApplication())
            {
                Path path;
                var root = new TestRoot
                {
                    Width = 200,
                    Height = 200,
                    Child = path = new Path
                    {
                        Width = 200,
                        Height = 200,
                        Fill = Brushes.Red,
                        Data = StreamGeometry.Parse("M100,0 L0,100 100,100")
                    }
                };

                root.Renderer = new DeferredRenderer(root, null);
                root.Measure(Size.Infinity);
                root.Arrange(new Rect(root.DesiredSize));

                var context = new DrawingContext(Mock.Of<IDrawingContextImpl>());

                var result = root.Renderer.HitTest(new Point(100, 100), root, null);
                Assert.Equal(new[] { path }, result);

                result = root.Renderer.HitTest(new Point(10, 10), root, null);
                Assert.Empty(result);
            }
        }

        [Fact]
        public void HitTest_Should_Respect_Geometry_Clip()
        {
            using (TestApplication())
            {
                Border border;
                Canvas canvas;
                var root = new TestRoot
                {
                    Width = 400,
                    Height = 400,
                    Child = border = new Border
                    {
                        Background = Brushes.Red,
                        Clip = StreamGeometry.Parse("M100,0 L0,100 100,100"),
                        Width = 200,
                        Height = 200,
                        Child = canvas = new Canvas
                        {
                            Background = Brushes.Yellow,
                            Margin = new Thickness(10),
                        }
                    }
                };

                root.Renderer = new DeferredRenderer(root, null);
                root.Measure(Size.Infinity);
                root.Arrange(new Rect(root.DesiredSize));
                Assert.Equal(new Rect(100, 100, 200, 200), border.Bounds);

                var context = new DrawingContext(Mock.Of<IDrawingContextImpl>());

                var result = root.Renderer.HitTest(new Point(200, 200), root, null);
                Assert.Equal(new IVisual[] { canvas, border }, result);

                result = root.Renderer.HitTest(new Point(110, 110), root, null);
                Assert.Empty(result);
            }
        }

        [Fact]
        public void HitTest_Should_Accommodate_ICustomHitTest()
        {
            using (TestApplication())
            {
                Border border;

                var root = new TestRoot
                {
                    Width = 300,
                    Height = 200,
                    Child = border = new CustomHitTestBorder
                    {
                        Width = 100,
                        Height = 100,
                        Background = Brushes.Red,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center
                    }
                }; 

                root.Renderer = new DeferredRenderer(root, null);
                root.Measure(Size.Infinity);
                root.Arrange(new Rect(root.DesiredSize));

                var result = root.Renderer.HitTest(new Point(75, 100), root, null);
                Assert.Equal(new[] { border }, result);

                result = root.Renderer.HitTest(new Point(125, 100), root, null);
                Assert.Equal(new[] { border }, result);

                result = root.Renderer.HitTest(new Point(175, 100), root, null);
                Assert.Empty(result);
            }
        }

        private IDisposable TestApplication()
        {
            return UnitTestApplication.Start(TestServices.MockPlatformRenderInterface);
        }
    }
}
