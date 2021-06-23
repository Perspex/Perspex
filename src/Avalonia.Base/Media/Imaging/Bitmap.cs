using System;
using System.IO;
using Avalonia.Platform;
using Avalonia.Utilities;

namespace Avalonia.Media.Imaging
{
    /// <summary>
    /// Holds a bitmap image.
    /// </summary>
    public class Bitmap : IBitmap
    {
        /// <summary>
        /// Loads a Bitmap from a stream and decodes at the desired width. Aspect ratio is maintained.
        /// This is more efficient than loading and then resizing.
        /// </summary>
        /// <param name="stream">The stream to read the bitmap from. This can be any supported image format.</param>
        /// <param name="width">The desired width of the resulting bitmap.</param>
        /// <param name="interpolationMode">The <see cref="BitmapInterpolationMode"/> to use should any scaling be required.</param>
        /// <returns>An instance of the <see cref="Bitmap"/> class.</returns>
        public static Bitmap DecodeToWidth(Stream stream, int width, BitmapInterpolationMode interpolationMode = BitmapInterpolationMode.HighQuality)
        {
            IPlatformRenderInterface factory = AvaloniaLocator.Current.GetService<IPlatformRenderInterface>();
            return new Bitmap(factory.LoadBitmapToWidth(stream, width, interpolationMode));
        }

        /// <summary>
        /// Loads a Bitmap from a stream and decodes at the desired height. Aspect ratio is maintained.
        /// This is more efficient than loading and then resizing.
        /// </summary>
        /// <param name="stream">The stream to read the bitmap from. This can be any supported image format.</param>
        /// <param name="height">The desired height of the resulting bitmap.</param>
        /// <param name="interpolationMode">The <see cref="BitmapInterpolationMode"/> to use should any scaling be required.</param>
        /// <returns>An instance of the <see cref="Bitmap"/> class.</returns>
        public static Bitmap DecodeToHeight(Stream stream, int height, BitmapInterpolationMode interpolationMode = BitmapInterpolationMode.HighQuality)
        {
            IPlatformRenderInterface factory = AvaloniaLocator.Current.GetService<IPlatformRenderInterface>();
            return new Bitmap(factory.LoadBitmapToHeight(stream, height, interpolationMode));
        }

        /// <summary>
        /// Creates a Bitmap scaled to a specified size from the current bitmap.
        /// </summary>        
        /// <param name="destinationSize">The destination size.</param>
        /// <param name="interpolationMode">The <see cref="BitmapInterpolationMode"/> to use should any scaling be required.</param>
        /// <returns>An instance of the <see cref="Bitmap"/> class.</returns>
        public Bitmap CreateScaledBitmap(PixelSize destinationSize, BitmapInterpolationMode interpolationMode = BitmapInterpolationMode.HighQuality)
        {
            IPlatformRenderInterface factory = AvaloniaLocator.Current.GetService<IPlatformRenderInterface>();
            return new Bitmap(factory.ResizeBitmap(PlatformImpl.Item, destinationSize, interpolationMode));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Bitmap"/> class.
        /// </summary>
        /// <param name="fileName">The filename of the bitmap.</param>
        public Bitmap(string fileName)
        {
            IPlatformRenderInterface factory = AvaloniaLocator.Current.GetService<IPlatformRenderInterface>();
            PlatformImpl = RefCountable.Create(factory.LoadBitmap(fileName));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Bitmap"/> class.
        /// </summary>
        /// <param name="stream">The stream to read the bitmap from.</param>
        public Bitmap(Stream stream)
        {
            IPlatformRenderInterface factory = AvaloniaLocator.Current.GetService<IPlatformRenderInterface>();
            PlatformImpl = RefCountable.Create(factory.LoadBitmap(stream));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Bitmap"/> class.
        /// </summary>
        /// <param name="impl">A platform-specific bitmap implementation.</param>
        public Bitmap(IRef<IBitmapImpl> impl)
        {
            PlatformImpl = impl.Clone();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Bitmap"/> class.
        /// </summary>
        /// <param name="impl">A platform-specific bitmap implementation. Bitmap class takes the ownership.</param>
        protected Bitmap(IBitmapImpl impl)
        {
            PlatformImpl = RefCountable.Create(impl);
        }

        /// <inheritdoc/>
        public virtual void Dispose()
        {
            PlatformImpl.Dispose();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Bitmap"/> class.
        /// </summary>
        /// <param name="format">The pixel format.</param>
        /// <param name="data">The pointer to the source bytes.</param>
        /// <param name="size">The size of the bitmap in device pixels.</param>
        /// <param name="dpi">The DPI of the bitmap.</param>
        /// <param name="stride">The number of bytes per row.</param>
        [Obsolete("Use overload taking an AlphaFormat.")]
        public Bitmap(PixelFormat format, IntPtr data, PixelSize size, Vector dpi, int stride)
        {
            var ri = AvaloniaLocator.Current.GetService<IPlatformRenderInterface>();

            PlatformImpl = RefCountable.Create(AvaloniaLocator.Current.GetService<IPlatformRenderInterface>()
                .LoadBitmap(format, ri.DefaultAlphaFormat, data, size, dpi, stride));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Bitmap"/> class.
        /// </summary>
        /// <param name="format">The pixel format.</param>
        /// <param name="alphaFormat">The alpha format.</param>
        /// <param name="data">The pointer to the source bytes.</param>
        /// <param name="size">The size of the bitmap in device pixels.</param>
        /// <param name="dpi">The DPI of the bitmap.</param>
        /// <param name="stride">The number of bytes per row.</param>
        public Bitmap(PixelFormat format, AlphaFormat alphaFormat, IntPtr data, PixelSize size, Vector dpi, int stride)
        {
            PlatformImpl = RefCountable.Create(AvaloniaLocator.Current.GetService<IPlatformRenderInterface>()
                .LoadBitmap(format, alphaFormat, data, size, dpi, stride));
        }

        /// <inheritdoc/>
        public Vector Dpi => PlatformImpl.Item.Dpi;

        /// <inheritdoc/>
        public Size Size => PlatformImpl.Item.PixelSize.ToSizeWithDpi(Dpi);

        /// <inheritdoc/>
        public PixelSize PixelSize => PlatformImpl.Item.PixelSize;

        /// <summary>
        /// Gets the platform-specific bitmap implementation.
        /// </summary>
        public IRef<IBitmapImpl> PlatformImpl { get; }

        /// <summary>
        /// Saves the bitmap to a file.
        /// </summary>
        /// <param name="fileName">The filename.</param>
        public void Save(string fileName)
        {
            PlatformImpl.Item.Save(fileName);
        }

        /// <summary>
        /// Saves the bitmap to a stream.
        /// </summary>
        /// <param name="stream">The stream.</param>
        public void Save(Stream stream)
        {
            PlatformImpl.Item.Save(stream);
        }

        /// <inheritdoc/>
        void IImage.Draw(
            DrawingContext context,
            Rect sourceRect,
            Rect destRect,
            BitmapInterpolationMode bitmapInterpolationMode)
        {
            context.PlatformImpl.DrawBitmap(
                PlatformImpl,
                1,
                sourceRect,
                destRect,
                bitmapInterpolationMode);
        }
    }
}
