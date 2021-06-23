using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Utilities;
using Avalonia.Media.Imaging;

namespace Avalonia.Benchmarks
{
    internal class NullDrawingContextImpl : IDrawingContextImpl
    {
        public void Dispose()
        {
        }

        public Matrix Transform { get; set; }

        public void Clear(Color color)
        {
        }

        public void DrawBitmap(IRef<IBitmapImpl> source, double opacity, Rect sourceRect, Rect destRect,
            BitmapInterpolationMode bitmapInterpolationMode = BitmapInterpolationMode.Default)
        {
        }

        public void DrawBitmap(IRef<IBitmapImpl> source, IBrush opacityMask, Rect opacityMaskRect, Rect destRect)
        {
        }

        public void DrawLine(IPen pen, Point p1, Point p2)
        {
        }

        public void DrawGeometry(IBrush brush, IPen pen, IGeometryImpl geometry)
        {
        }

        public void DrawRectangle(IBrush brush, IPen pen, RoundedRect rect, BoxShadows boxShadows = default)
        {
        }

        public void DrawText(IBrush foreground, Point origin, IFormattedTextImpl text)
        {
        }

        public void DrawGlyphRun(IBrush foreground, GlyphRun glyphRun)
        {
        }

        public IDrawingContextLayerImpl CreateLayer(Size size)
        {
            return null;
        }

        public void PushClip(Rect clip)
        {
        }

        public void PushClip(RoundedRect clip)
        {
        }

        public void PopClip()
        {
        }

        public void PushOpacity(double opacity)
        {
        }

        public void PopOpacity()
        {
        }

        public void PushOpacityMask(IBrush mask, Rect bounds)
        {
        }

        public void PopOpacityMask()
        {
        }

        public void PushGeometryClip(IGeometryImpl clip)
        {
        }

        public void PopGeometryClip()
        {
        }

        public void PushBitmapBlendMode(BitmapBlendingMode blendingMode)
        {
        }

        public void PopBitmapBlendMode()
        {
        }

        public void Custom(ICustomDrawOperation custom)
        {
        }
    }
}
