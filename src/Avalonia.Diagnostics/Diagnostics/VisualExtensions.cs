using System;
using System.IO;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Media.Imaging;
using Avalonia.VisualTree;

namespace Avalonia.Diagnostics
{
    static class VisualExtensions
    {
        /// <summary>
        /// Rendered control to stream
        /// </summary>
        /// <param name="source">the control I want to render in the stream</param>
        /// <param name="destination">destination destina</param>
        /// <param name="dpi">(optional) dpi quality default 96</param>
        public static void RenderTo(this IControl source, Stream destination, double dpi = 96)
        {
            if (source.TransformedBounds == null)
            {
                return;
            }
            var rect = source.TransformedBounds.Value.Clip;
            var top = rect.TopLeft;
            var pixelSize = new PixelSize((int)rect.Width, (int)rect.Height);
            var dpiVector = new Vector(dpi, dpi);

            // get Visual root
            var root = (source.VisualRoot
                ?? source.GetVisualRoot())
                as IControl ?? source;


            IDisposable? clipSetter = default;
            IDisposable? clipToBoundsSetter = default;
            IDisposable? renderTransformOriginSetter = default;
            IDisposable? renderTransformSetter = default;
            try
            {                
                // Set clip region
                var clipRegion = new Media.RectangleGeometry(rect);
                clipToBoundsSetter = root.SetValue(Visual.ClipToBoundsProperty, true, BindingPriority.Animation);
                clipSetter = root.SetValue(Visual.ClipProperty, clipRegion, BindingPriority.Animation);

                // Translate origin
                renderTransformOriginSetter = root.SetValue(Visual.RenderTransformOriginProperty,
                    new RelativePoint(top, RelativeUnit.Absolute),
                    BindingPriority.Animation);

                renderTransformSetter = root.SetValue(Visual.RenderTransformProperty,
                    new Media.TranslateTransform(-top.X, -top.Y),
                    BindingPriority.Animation);

                using (var bitmap = new RenderTargetBitmap(pixelSize, dpiVector))
                {                   
                    bitmap.Render(root);
                    bitmap.Save(destination);
                }

            }
            finally
            {
                // Restore values before trasformation
                renderTransformSetter?.Dispose();
                renderTransformOriginSetter?.Dispose();
                clipSetter?.Dispose();
                clipToBoundsSetter?.Dispose();
                source?.InvalidateVisual();
            }
        }
    }
}
