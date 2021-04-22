using System;
using System.Diagnostics;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Skia;
using Avalonia.Threading;
using SkiaSharp;

namespace RenderDemo.Pages
{
    public class CustomSkiaPage : Control
    {
        public CustomSkiaPage()
        {
            ClipToBounds = true;
        }

        class CustomDrawOp : ICustomDrawOperation
        {
            private readonly FormattedText _noSkia;

            public CustomDrawOp(Rect bounds, FormattedText noSkia)
            {
                _noSkia = noSkia;
                Bounds = bounds;
            }

            public void Dispose()
            {
                // No-op
            }

            public Rect Bounds { get; }
            public bool HitTest(Point p) => false;
            public bool Equals(ICustomDrawOperation other) => false;
            static Stopwatch St = Stopwatch.StartNew();
            public void Render(IDrawingContextImpl context)
            {
                var canvas = (context as ISkiaDrawingContextImpl)?.SkCanvas;
                if (canvas == null)
                    using (var c = new DrawingContext(context, false))
                    {
                        c.DrawText(_noSkia, new Point());
                    }
                else
                {
                    canvas.Save();
                    // create the first shader
                    var colors = new SKColor[] {
                        new SKColor(0, 255, 255),
                        new SKColor(255, 0, 255),
                        new SKColor(255, 255, 0),
                        new SKColor(0, 255, 255)
                    };

                    var sx = Animate(100, 2, 10);
                    var sy = Animate(1000, 5, 15);
                    var lightPosition = new SKPoint(
                        (float)(Bounds.Width / 2 + Math.Cos(St.Elapsed.TotalSeconds) * Bounds.Width / 4),
                        (float)(Bounds.Height / 2 + Math.Sin(St.Elapsed.TotalSeconds) * Bounds.Height / 4));
                    using (var sweep =
                        SKShader.CreateSweepGradient(new SKPoint((int)Bounds.Width / 2, (int)Bounds.Height / 2), colors,
                            null))
                    using (var turbulence = SKShader.CreatePerlinNoiseFractalNoise(0.05f, 0.05f, 4, 0))
                    using (var shader = SKShader.CreateCompose(sweep, turbulence, SKBlendMode.SrcATop))
                    using (var blur = SKImageFilter.CreateBlur(Animate(100, 2, 10), Animate(100, 5, 15)))
                    using (var paint = new SKPaint
                    {
                        Shader = shader,
                        ImageFilter = blur
                    })
                        canvas.DrawPaint(paint);

                    using (var pseudoLight = SKShader.CreateRadialGradient(
                        lightPosition,
                        (float)(Bounds.Width / 3),
                        new[] {
                            new SKColor(255, 200, 200, 100),
                            SKColors.Transparent,
                            new SKColor(40,40,40, 220),
                            new SKColor(20,20,20, (byte)Animate(100, 200,220)) },
                        new float[] { 0.3f, 0.3f, 0.8f, 1 },
                        SKShaderTileMode.Clamp))
                    using (var paint = new SKPaint
                    {
                        Shader = pseudoLight
                    })
                        canvas.DrawPaint(paint);
                    canvas.Restore();
                }
            }
            static int Animate(int d, int from, int to)
            {
                var ms = (int)(St.ElapsedMilliseconds / d);
                var diff = to - from;
                var range = diff * 2;
                var v = ms % range;
                if (v > diff)
                    v = range - v;
                var rv = v + from;
                if (rv < from || rv > to)
                    throw new Exception("WTF");
                return rv;
            }
        }

        public override void Render(DrawingContext context)
        {
            var noSkia = new FormattedText("Current rendering API is not Skia", CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight, Typeface.Default, 12, Brushes.Black);

            context.Custom(new CustomDrawOp(new Rect(0, 0, Bounds.Width, Bounds.Height), noSkia));

            Dispatcher.UIThread.InvokeAsync(InvalidateVisual, DispatcherPriority.Background);
        }
    }
}
