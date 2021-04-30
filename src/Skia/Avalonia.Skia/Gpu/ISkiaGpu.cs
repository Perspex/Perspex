using System;
using System.Collections.Generic;
using Avalonia.OpenGL.Imaging;
using SkiaSharp;

namespace Avalonia.Skia
{
    /// <summary>
    /// Custom Skia gpu instance.
    /// </summary>
    public interface ISkiaGpu
    {
        /// <summary>
        /// Attempts to create custom render target from given surfaces.
        /// </summary>
        /// <param name="surfaces">Surfaces.</param>
        /// <returns>Created render target or <see langword="null"/> if it fails.</returns>
        ISkiaGpuRenderTarget TryCreateRenderTarget(IEnumerable<object> surfaces);
        
        /// <summary>
        /// Creates an offscreen render target surface
        /// </summary>
        /// <param name="size">size in pixels</param>
        /// <param name="session">An optional custom render session.</param>
        ISkiaSurface TryCreateSurface(PixelSize size, ISkiaGpuRenderSession session);
    }
    
    public interface ISkiaSurface : IDisposable
    {
        SKSurface Surface { get; }
        bool CanBlit { get; }
        void Blit(SKCanvas canvas);
    }

    public interface IOpenGlAwareSkiaGpu : ISkiaGpu
    {
        IOpenGlBitmapImpl CreateOpenGlBitmap(PixelSize size, Vector dpi);
    }
}
