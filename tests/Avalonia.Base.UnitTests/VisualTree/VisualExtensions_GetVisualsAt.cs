using System;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Rendering;
using Avalonia.UnitTests;
using Avalonia.VisualTree;
using Xunit;

namespace Avalonia.Base.UnitTests.VisualTree
{
    public class VisualExtensions_GetVisualsAt
    {
        [Fact]
        public void Should_Find_Control()
        {
            using (TestApplication())
            {
                Border target;
                var root = new TestRoot
                {
                    Width = 200,
                    Height = 200,
                    Child = new StackPanel
                    {
                        Background = Brushes.White,
                        Children =
                        {
                            (target = new Border
                            {
                                Width = 100,
                                Height = 200,
                                Background = Brushes.Red,
                            }),
                            new Border
                            {
                                Width = 100,
                                Height = 200,
                                Background = Brushes.Green,
                            }
                        },
                        Orientation = Orientation.Horizontal,
                    }
                };

                root.Renderer = new DeferredRenderer(root, null);
                root.Measure(Size.Infinity);
                root.Arrange(new Rect(root.DesiredSize));

                var result = target.GetVisualsAt(new Point(50, 50));

                Assert.Same(target, result.Single());
            }
        }

        [Fact]
        public void Should_Not_Find_Sibling_Control()
        {
            using (TestApplication())
            {
                Border target;
                var root = new TestRoot
                {
                    Width = 200,
                    Height = 200,
                    Child = new StackPanel
                    {
                        Background = Brushes.White,
                        Children =
                        {
                            (target = new Border
                            {
                                Width = 100,
                                Height = 200,
                                Background = Brushes.Red,
                            }),
                            new Border
                            {
                                Width = 100,
                                Height = 200,
                                Background = Brushes.Green,
                            }
                        },
                        Orientation = Orientation.Horizontal,
                    }
                };

                root.Renderer = new DeferredRenderer(root, null);
                root.Measure(Size.Infinity);
                root.Arrange(new Rect(root.DesiredSize));

                var result = target.GetVisualsAt(new Point(150, 50));

                Assert.Empty(result);
            }
        }

        private IDisposable TestApplication()
        {
            return UnitTestApplication.Start(TestServices.MockPlatformRenderInterface);
        }
    }
}
