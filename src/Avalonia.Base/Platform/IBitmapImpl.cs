using System;
using System.IO;

namespace Avalonia.Platform
{
    /// <summary>
    /// Defines the platform-specific interface for a <see cref="Avalonia.Media.Imaging.Bitmap"/>.
    /// </summary>
    public interface IBitmapImpl : IDisposable
    {
        /// <summary>
        /// Gets the dots per inch (DPI) of the image.
        /// </summary>
        Vector Dpi { get; }

        /// <summary>
        /// Gets the size of the bitmap, in device pixels.
        /// </summary>
        PixelSize PixelSize { get; }
        
        /// <summary>
        /// Version of the pixel data
        /// </summary>
        int Version { get; }

        /// <summary>
        /// Saves the bitmap to a file.
        /// </summary>
        /// <param name="fileName">The filename.</param>
        void Save(string fileName);

        /// <summary>
        /// Saves the bitmap to a stream in png format.
        /// </summary>
        /// <param name="stream">The stream.</param>
        void Save(Stream stream);
    }
}
