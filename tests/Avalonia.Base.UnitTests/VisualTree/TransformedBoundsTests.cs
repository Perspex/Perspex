using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Rendering;
using Avalonia.UnitTests;
using Avalonia.VisualTree;
using Moq;
using Xunit;

namespace Avalonia.Base.UnitTests.VisualTree
{
    public class TransformedBoundsTests
    {
        [Fact]
        public void Should_Track_Bounds()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var control = default(Rectangle);
                var tree = new Decorator
                {
                    Padding = new Thickness(10),
                    Child = new Decorator
                    {
                        Padding = new Thickness(5),
                        Child = control = new Rectangle
                        {
                            Width = 15,
                            Height = 15,
                        },
                    }
                };

                var context = new DrawingContext(Mock.Of<IDrawingContextImpl>());

                tree.Measure(Size.Infinity);
                tree.Arrange(new Rect(0, 0, 100, 100));
                ImmediateRenderer.Render(tree, context);

                var track = control.GetObservable(Visual.TransformedBoundsProperty);
                var results = new List<TransformedBounds?>();
                track.Subscribe(results.Add);

                Assert.Equal(new Rect(0, 0, 15, 15), results[0].Value.Bounds);
                Assert.Equal(Matrix.CreateTranslation(42, 42), results[0].Value.Transform);
            }
        }
    }
}
