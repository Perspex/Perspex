using Avalonia;
using Avalonia.Controls;
using Avalonia.LogicalTree;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Media.Immutable;

namespace RenderDemo.Pages
{
    public class PathMeasurementPage : Control
    {
        static PathMeasurementPage()
        {
            AffectsRender<PathMeasurementPage>(BoundsProperty);
        }

        private RenderTargetBitmap _bitmap;

        protected override void OnAttachedToLogicalTree(LogicalTreeAttachmentEventArgs e)
        {
            _bitmap = new RenderTargetBitmap(new PixelSize(500, 500), new Vector(96, 96));
            base.OnAttachedToLogicalTree(e);
        }

        protected override void OnDetachedFromLogicalTree(LogicalTreeAttachmentEventArgs e)
        {
            _bitmap.Dispose();
            _bitmap = null;
            base.OnDetachedFromLogicalTree(e);
        }

        readonly IPen strokePen = new ImmutablePen(Brushes.DarkBlue, 10d, null, PenLineCap.Round, PenLineJoin.Round);
        readonly IPen strokePen1 = new ImmutablePen(Brushes.Purple, 10d, null, PenLineCap.Round, PenLineJoin.Round);
        readonly IPen strokePen2 = new ImmutablePen(Brushes.Green, 10d, null, PenLineCap.Round, PenLineJoin.Round);
        readonly IPen strokePen3 = new ImmutablePen(Brushes.LightBlue, 10d, null, PenLineCap.Round, PenLineJoin.Round);
        readonly IPen strokePen4 = new ImmutablePen(Brushes.Red, 1d, null, PenLineCap.Round, PenLineJoin.Round);

        public override void Render(DrawingContext context)
        {
            using (var ctxi = _bitmap.CreateDrawingContext(null))
            using (var bitmapCtx = new DrawingContext(ctxi, false))
            {
                ctxi.Clear(default);

                var basePath = new PathGeometry();

                using (var basePathCtx = basePath.Open())
                {
                    basePathCtx.BeginFigure(new Point(20, 20), false);
                    basePathCtx.LineTo(new Point(400, 50));
                    basePathCtx.LineTo(new Point(80, 100));
                    basePathCtx.LineTo(new Point(300, 150));
                    basePathCtx.EndFigure(false);
                }

                bitmapCtx.DrawGeometry(null, strokePen, basePath);


                var length = basePath.PlatformImpl.ContourLength;

                if (basePath.PlatformImpl.TryGetSegment(length * 0.05, length * 0.2, true, out var dst1))
                    bitmapCtx.DrawGeometry(null, strokePen1, dst1);

                if (basePath.PlatformImpl.TryGetSegment(length * 0.2, length * 0.8, true, out var dst2))
                    bitmapCtx.DrawGeometry(null, strokePen2, dst2);

                if (basePath.PlatformImpl.TryGetSegment(length * 0.8, length * 0.95, true, out var dst3))
                    bitmapCtx.DrawGeometry(null, strokePen3, dst3);
                
                var pathBounds = basePath.GetRenderBounds(strokePen);
                
                bitmapCtx.DrawRectangle(null, strokePen4, pathBounds);
            }


            context.DrawImage(_bitmap,
                new Rect(0, 0, 500, 500),
                new Rect(0, 0, 500, 500));
             
            base.Render(context);
        }
    }
}
