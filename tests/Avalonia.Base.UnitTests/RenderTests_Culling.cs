using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Rendering;
using Avalonia.UnitTests;
using Moq;
using Xunit;

namespace Avalonia.Base.UnitTests
{
    public class RenderTests_Culling
    {
        [Fact]
        public void In_Bounds_Control_Should_Be_Rendered()
        {
            using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface))
            {
                TestControl target;

                var container = new Canvas
                {
                    Width = 100,
                    Height = 100,
                    ClipToBounds = true,
                    Children =
                    {
                        (target = new TestControl
                        {
                            Width = 10, Height = 10, [Canvas.LeftProperty] = 98, [Canvas.TopProperty] = 98,
                        })
                    }
                };

                Render(container);

                Assert.True(target.Rendered);
            }
        }

        [Fact]
        public void Out_Of_Bounds_Control_Should_Not_Be_Rendered()
        {
            using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface))
            {
                TestControl target;

                var container = new Canvas
                {
                    Width = 100,
                    Height = 100,
                    ClipToBounds = true,
                    Children =
                    {
                        (target = new TestControl
                        {
                            Width = 10,
                            Height = 10,
                            ClipToBounds = true,
                            [Canvas.LeftProperty] = 110,
                            [Canvas.TopProperty] = 110,
                        })
                    }
                };

                Render(container);

                Assert.False(target.Rendered);
            }
        }

        [Fact]
        public void Out_Of_Bounds_Child_Control_Should_Not_Be_Rendered()
        {
            using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface))
            {
                TestControl target;

                var container = new Canvas
                {
                    Width = 100,
                    Height = 100,
                    ClipToBounds = true,
                    Children =
                    {
                        new Canvas
                        {
                            Width = 100,
                            Height = 100,
                            [Canvas.LeftProperty] = 50,
                            [Canvas.TopProperty] = 50,
                            Children =
                            {
                                (target = new TestControl
                                {
                                    Width = 10,
                                    Height = 10,
                                    ClipToBounds = true,
                                    [Canvas.LeftProperty] = 50,
                                    [Canvas.TopProperty] = 50,
                                })
                            }
                        }
                    }
                };

                Render(container);

                Assert.False(target.Rendered);
            }
        }

        [Fact]
        public void RenderTransform_Should_Be_Respected()
        {
            using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface))
            {
                TestControl target;

                var container = new Canvas
                {
                    Width = 100,
                    Height = 100,
                    ClipToBounds = true,
                    Children =
                    {
                        (target = new TestControl
                        {
                            Width = 10,
                            Height = 10,
                            [Canvas.LeftProperty] = 110,
                            [Canvas.TopProperty] = 110,
                            RenderTransform = new TranslateTransform(-100, -100),
                        })
                    }
                };

                Render(container);

                Assert.True(target.Rendered);
            }
        }

        [Fact]
        public void Negative_Margin_Should_Be_Respected()
        {
            using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface))
            {
                TestControl target;

                var container = new Canvas
                {
                    Width = 100,
                    Height = 100,
                    ClipToBounds = true,
                    Children =
                    {
                        new Border
                        {
                            Margin = new Thickness(100, 100, 0, 0),
                            Child = target = new TestControl
                            {
                                Width = 10, Height = 10, Margin = new Thickness(-100, -100, 0, 0),
                            }
                        }
                    }
                };

                Render(container);

                Assert.True(target.Rendered);
            }
        }

        private void Render(IControl control)
        {
            var ctx = CreateDrawingContext();
            control.Measure(Size.Infinity);
            control.Arrange(new Rect(control.DesiredSize));
            ImmediateRenderer.Render(control, ctx);
        }

        private DrawingContext CreateDrawingContext()
        {
            return new DrawingContext(Mock.Of<IDrawingContextImpl>());
        }

        private class TestControl : Control
        {
            public bool Rendered { get; private set; }

            public override void Render(DrawingContext context)
            {
                Rendered = true;
            }
        }
    }
}
