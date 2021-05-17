using System;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Platform;
using Avalonia.Visuals.Media.Imaging;

namespace Avalonia.Media.Imaging
{
    /// <summary>
    /// Holds a writeable bitmap image.
    /// </summary>
    public class WriteableBitmap : Bitmap
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="WriteableBitmap"/> class.
        /// </summary>
        /// <param name="size">The size of the bitmap in device pixels.</param>
        /// <param name="dpi">The DPI of the bitmap.</param>
        /// <param name="format">The pixel format (optional).</param>
        /// <returns>An <see cref="IWriteableBitmapImpl"/>.</returns>
        [Obsolete("Use overload taking an AlphaFormat.")]
        public WriteableBitmap(PixelSize size, Vector dpi, PixelFormat? format = null)
            : base(CreatePlatformImpl(size, dpi, format, null))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="WriteableBitmap"/> class.
        /// </summary>
        /// <param name="size">The size of the bitmap in device pixels.</param>
        /// <param name="dpi">The DPI of the bitmap.</param>
        /// <param name="format">The pixel format (optional).</param>
        /// <param name="alphaFormat">The alpha format (optional).</param>
        /// <returns>An <see cref="IWriteableBitmapImpl"/>.</returns>
        public WriteableBitmap(PixelSize size, Vector dpi, PixelFormat format, AlphaFormat alphaFormat) 
            : base(CreatePlatformImpl(size, dpi, format, alphaFormat))
        {
        }

        private WriteableBitmap(IWriteableBitmapImpl impl) : base(impl)
        {
            
        }

        public ILockedFramebuffer Lock() => ((IWriteableBitmapImpl) PlatformImpl.Item).Lock();

        public static WriteableBitmap Decode(Stream stream)
        {
            var ri = AvaloniaLocator.Current.GetService<IPlatformRenderInterface>();

            return new WriteableBitmap(ri.LoadWriteableBitmap(stream));
        }
        
        /// <summary>
        /// Loads a WriteableBitmap from a stream and decodes at the desired width. Aspect ratio is maintained.
        /// This is more efficient than loading and then resizing.
        /// </summary>
        /// <param name="stream">The stream to read the bitmap from. This can be any supported image format.</param>
        /// <param name="width">The desired width of the resulting bitmap.</param>
        /// <param name="interpolationMode">The <see cref="BitmapInterpolationMode"/> to use should any scaling be required.</param>
        /// <returns>An instance of the <see cref="WriteableBitmap"/> class.</returns>
        public new static WriteableBitmap DecodeToWidth(Stream stream, int width, BitmapInterpolationMode interpolationMode = BitmapInterpolationMode.HighQuality)
        {
            var ri = AvaloniaLocator.Current.GetService<IPlatformRenderInterface>();

            return new WriteableBitmap(ri.LoadWriteableBitmapToWidth(stream, width, interpolationMode));
        }

        /// <summary>
        /// Loads a Bitmap from a stream and decodes at the desired height. Aspect ratio is maintained.
        /// This is more efficient than loading and then resizing.
        /// </summary>
        /// <param name="stream">The stream to read the bitmap from. This can be any supported image format.</param>
        /// <param name="height">The desired height of the resulting bitmap.</param>
        /// <param name="interpolationMode">The <see cref="BitmapInterpolationMode"/> to use should any scaling be required.</param>
        /// <returns>An instance of the <see cref="WriteableBitmap"/> class.</returns>
        public new static WriteableBitmap DecodeToHeight(Stream stream, int height, BitmapInterpolationMode interpolationMode = BitmapInterpolationMode.HighQuality)
        {
            var ri = AvaloniaLocator.Current.GetService<IPlatformRenderInterface>();

            return new WriteableBitmap(ri.LoadWriteableBitmapToHeight(stream, height, interpolationMode));
        }

        private static IBitmapImpl CreatePlatformImpl(PixelSize size, in Vector dpi, PixelFormat? format, AlphaFormat? alphaFormat)
        {
            var ri = AvaloniaLocator.Current.GetService<IPlatformRenderInterface>();

            PixelFormat finalFormat = format ?? ri.DefaultPixelFormat;
            AlphaFormat finalAlphaFormat = alphaFormat ?? ri.DefaultAlphaFormat;

            return ri.CreateWriteableBitmap(size, dpi, finalFormat, finalAlphaFormat);
        }
    }
}
