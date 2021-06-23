using System;

namespace Avalonia.Platform
{
    public interface ILockedFramebuffer : IDisposable
    {
        /// <summary>
        /// Address of the first pixel
        /// </summary>
        IntPtr Address { get; }

        /// <summary>
        /// Gets the framebuffer size in device pixels.
        /// </summary>
        PixelSize Size{ get; }
        
        /// <summary>
        /// Number of bytes per row
        /// </summary>
        int RowBytes { get; }
        
        /// <summary>
        /// DPI of underling screen
        /// </summary>
        Vector Dpi { get; }
        
        /// <summary>
        /// Pixel format
        /// </summary>
        PixelFormat Format { get; }
    }
}
