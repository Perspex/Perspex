using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.LogicalTree;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;

namespace RenderDemo.Pages
{
    public class RenderTargetBitmapPage : Control
    {
        private RenderTargetBitmap _bitmap;

        protected override void OnAttachedToLogicalTree(LogicalTreeAttachmentEventArgs e)
        {
            _bitmap = new RenderTargetBitmap(new PixelSize(200, 200), new Vector(96, 96));
            base.OnAttachedToLogicalTree(e);
        }

        protected override void OnDetachedFromLogicalTree(LogicalTreeAttachmentEventArgs e)
        {
            _bitmap.Dispose();
            _bitmap = null;
            base.OnDetachedFromLogicalTree(e);
        }

        readonly Stopwatch _st = Stopwatch.StartNew();
        public override void Render(DrawingContext context)
        {
            using (var ctxi = _bitmap.CreateDrawingContext(null))
            using(var ctx = new DrawingContext(ctxi, false))
            using (ctx.PushPostTransform(Matrix.CreateTranslation(-100, -100)
                                         * Matrix.CreateRotation(_st.Elapsed.TotalSeconds)
                                         * Matrix.CreateTranslation(100, 100)))
            {
                ctxi.Clear(default);
                ctx.FillRectangle(Brushes.Fuchsia, new Rect(50, 50, 100, 100));
            }

            context.DrawImage(_bitmap,
                new Rect(0, 0, 200, 200), 
                new Rect(0, 0, 200, 200));
            Dispatcher.UIThread.Post(InvalidateVisual, DispatcherPriority.Background);
            base.Render(context);
        }
    }
}
