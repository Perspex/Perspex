using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;
using Avalonia.Platform;
using Avalonia.Utilities;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

#if AVALONIA_SKIA
namespace Avalonia.Skia.RenderTests
#else

using Avalonia.Direct2D1.RenderTests;

namespace Avalonia.Direct2D1.RenderTests.Media
#endif
{
    public class TextLayoutTests : TestBase
    {
        public TextLayoutTests()
            : base(@"Media\TextFormatting\TextLayout")
        {
        }

        [Fact]
        public async Task TextLayout_Basic()
        {
            var t = new TextLayout(
                "Avalonia!",
                new Typeface(TestFontFamily),
                24,
                Brushes.Black);

            var target = new Border
            {
                Width = 200,
                Height = 200,
                Background = Brushes.White,
                Child = new DrawnControl(c =>
                {
                    var textRect = new Rect(t.Size);
                    var bounds = new Rect(0, 0, 200, 200);
                    var rect = bounds.CenterRect(textRect);
                    c.DrawRectangle(Brushes.Yellow, null, rect);
                    t.Draw(c, rect.Position);
                }),
            };

            await RenderToFile(target);
            CompareImages();
        }

        [Fact]
        public async Task TextLayout_Rotated()
        {
            var t = new TextLayout(
                "Avalonia!",
                new Typeface(TestFontFamily),
                24,
                Brushes.Black);

            var target = new Border
            {
                Width = 200,
                Height = 200,
                Background = Brushes.White,
                Child = new DrawnControl(c =>
                {
                    var textRect = new Rect(t.Size);
                    var bounds = new Rect(0, 0, 200, 200);
                    var rect = bounds.CenterRect(textRect);
                    var rotate = Matrix.CreateTranslation(-100, -100) *
                        Matrix.CreateRotation(MathUtilities.Deg2Rad(90)) *
                        Matrix.CreateTranslation(100, 100);
                    using var transform = c.PushPreTransform(rotate);
                    c.DrawRectangle(Brushes.Yellow, null, rect);
                    t.Draw(c, rect.Position);
                }),
            };

            await RenderToFile(target);
            CompareImages();
        }

        private class DrawnControl : Control
        {
            private readonly Action<DrawingContext> _render;
            public DrawnControl(Action<DrawingContext> render) => _render = render;
            public override void Render(DrawingContext context) => _render(context);
        }
    }
}
