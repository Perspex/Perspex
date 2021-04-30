using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Avalonia.Logging;
using Avalonia.OpenGL;
using Avalonia.OpenGL.Imaging;
using Avalonia.OpenGL.Surfaces;
using SkiaSharp;

namespace Avalonia.Skia
{
    class GlSkiaGpu : IOpenGlAwareSkiaGpu
    {
        private GRContext _grContext;
        private IGlContext _glContext;
        private bool? _canCreateSurfaces;

        public GlSkiaGpu(IPlatformOpenGlInterface openGl, long? maxResourceBytes)
        {
            var context = openGl.PrimaryContext;
            _glContext = context;
            using (context.MakeCurrent())
            {
                using (var iface = context.Version.Type == GlProfileType.OpenGL ?
                    GRGlInterface.CreateOpenGl(proc => context.GlInterface.GetProcAddress(proc)) :
                    GRGlInterface.CreateGles(proc => context.GlInterface.GetProcAddress(proc)))
                {
                    _grContext = GRContext.CreateGl(iface);
                    if (maxResourceBytes.HasValue)
                    {
                        _grContext.SetResourceCacheLimit(maxResourceBytes.Value);
                    }
                }
            }
        }

        public ISkiaGpuRenderTarget TryCreateRenderTarget(IEnumerable<object> surfaces)
        {
            foreach (var surface in surfaces)
            {
                if (surface is IGlPlatformSurface glSurface)
                {
                    return new GlRenderTarget(_grContext, glSurface);
                }
            }

            return null;
        }

        public ISkiaSurface TryCreateSurface(PixelSize size, ISkiaGpuRenderSession session)
        {
            // Only windows platform needs our FBO trickery
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return null;

            // Blit feature requires glBlitFramebuffer
            if (_glContext.GlInterface.BlitFramebuffer == null)
                return null;
            
            size = new PixelSize(Math.Max(size.Width, 1), Math.Max(size.Height, 1));
            if (_canCreateSurfaces == false)
                return null;
            try
            {
                var surface = new FboSkiaSurface(_grContext, _glContext, size, session?.SurfaceOrigin ?? GRSurfaceOrigin.TopLeft);
                _canCreateSurfaces = true;
                return surface;
            }
            catch (Exception)
            {
                Logger.TryGet(LogEventLevel.Error, "OpenGL")
                    ?.Log(this, "Unable to create a Skia-compatible FBO manually");
                _canCreateSurfaces ??= false;
                return null;
            }
        }

        public IOpenGlBitmapImpl CreateOpenGlBitmap(PixelSize size, Vector dpi) => new GlOpenGlBitmapImpl(_glContext, size, dpi);
    }
}
