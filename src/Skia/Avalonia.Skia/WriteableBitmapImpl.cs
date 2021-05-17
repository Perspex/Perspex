using System;
using System.IO;
using System.Threading;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Skia.Helpers;
using Avalonia.Visuals.Media.Imaging;
using SkiaSharp;

namespace Avalonia.Skia
{
    /// <summary>
    /// Skia based writeable bitmap.
    /// </summary>
    internal class WriteableBitmapImpl : IWriteableBitmapImpl, IDrawableBitmapImpl
    {
        private static readonly SKBitmapReleaseDelegate s_releaseDelegate = ReleaseProc;
        private readonly SKBitmap _bitmap;
        private readonly object _lock = new object();
        
        /// <summary>
        /// Create a WriteableBitmap from given stream.
        /// </summary>
        /// <param name="stream">Stream containing encoded data.</param>
        public WriteableBitmapImpl(Stream stream)
        {
            using (var skiaStream = new SKManagedStream(stream))
            {
                _bitmap = SKBitmap.Decode(skiaStream);

                if (_bitmap == null)
                {
                    throw new ArgumentException("Unable to load bitmap from provided data");
                }

                PixelSize = new PixelSize(_bitmap.Width, _bitmap.Height);
                Dpi = SkiaPlatform.DefaultDpi;
            }
        }

        public WriteableBitmapImpl(Stream stream, int decodeSize, bool horizontal, BitmapInterpolationMode interpolationMode)
        {
            using (var skStream = new SKManagedStream(stream))
            using (var codec = SKCodec.Create(skStream))
            {
                var info = codec.Info;

                // get the scale that is nearest to what we want (eg: jpg returned 512)
                var supportedScale = codec.GetScaledDimensions(horizontal ? ((float)decodeSize / info.Width) : ((float)decodeSize / info.Height));

                // decode the bitmap at the nearest size
                var nearest = new SKImageInfo(supportedScale.Width, supportedScale.Height);
                var bmp = SKBitmap.Decode(codec, nearest);

                // now scale that to the size that we want
                var realScale = horizontal ? ((double)info.Height / info.Width) : ((double)info.Width / info.Height);

                SKImageInfo desired;


                if (horizontal)
                {
                    desired = new SKImageInfo(decodeSize, (int)(realScale * decodeSize));
                }
                else
                {
                    desired = new SKImageInfo((int)(realScale * decodeSize), decodeSize);
                }

                if (bmp.Width != desired.Width || bmp.Height != desired.Height)
                {
                    var scaledBmp = bmp.Resize(desired, interpolationMode.ToSKFilterQuality());
                    bmp.Dispose();
                    bmp = scaledBmp;
                }

                _bitmap = bmp;

                PixelSize = new PixelSize(bmp.Width, bmp.Height);
                Dpi = SkiaPlatform.DefaultDpi;
            }
        }
        
        /// <summary>
        /// Create new writeable bitmap.
        /// </summary>
        /// <param name="size">The size of the bitmap in device pixels.</param>
        /// <param name="dpi">The DPI of the bitmap.</param>
        /// <param name="format">The pixel format.</param>
        /// <param name="alphaFormat">The alpha format.</param>
        public WriteableBitmapImpl(PixelSize size, Vector dpi, PixelFormat format, AlphaFormat alphaFormat)
        {
            PixelSize = size;
            Dpi = dpi;

            SKColorType colorType = format.ToSkColorType();
            SKAlphaType alphaType = alphaFormat.ToSkAlphaType();
            
            var runtimePlatform = AvaloniaLocator.Current?.GetService<IRuntimePlatform>();
            
            if (runtimePlatform != null)
            {
                _bitmap = new SKBitmap();

                var nfo = new SKImageInfo(size.Width, size.Height, colorType, alphaType);
                var blob = runtimePlatform.AllocBlob(nfo.BytesSize);

                _bitmap.InstallPixels(nfo, blob.Address, nfo.RowBytes, s_releaseDelegate, blob);
            }
            else
            {
                _bitmap = new SKBitmap(size.Width, size.Height, colorType, alphaType);
            }

            _bitmap.Erase(SKColor.Empty);
        }

        public Vector Dpi { get; }

        /// <inheritdoc />
        public PixelSize PixelSize { get; }

        public int Version { get; private set; } = 1;

        /// <inheritdoc />
        public void Draw(DrawingContextImpl context, SKRect sourceRect, SKRect destRect, SKPaint paint)
        {
            lock (_lock)
                context.Canvas.DrawBitmap(_bitmap, sourceRect, destRect, paint);
        }

        /// <inheritdoc />
        public void Dispose()
        {
            _bitmap.Dispose();
        }

        /// <inheritdoc />
        public void Save(Stream stream)
        {
            using (var image = GetSnapshot())
            {
                ImageSavingHelper.SaveImage(image, stream);
            }
        }

        /// <inheritdoc />
        public void Save(string fileName)
        {
            using (var image = GetSnapshot())
            {
                ImageSavingHelper.SaveImage(image, fileName);
            }
        }

        /// <inheritdoc />
        public ILockedFramebuffer Lock() => new BitmapFramebuffer(this, _bitmap);

        /// <summary>
        /// Get snapshot as image.
        /// </summary>
        /// <returns>Image snapshot.</returns>
        public SKImage GetSnapshot()
        {
            lock (_lock)
                return SKImage.FromPixels(_bitmap.Info, _bitmap.GetPixels(), _bitmap.RowBytes);
        }

        /// <summary>
        /// Release given unmanaged blob.
        /// </summary>
        /// <param name="address">Blob address.</param>
        /// <param name="ctx">Blob.</param>
        private static void ReleaseProc(IntPtr address, object ctx)
        {
            ((IUnmanagedBlob)ctx).Dispose();
        }

        /// <summary>
        /// Framebuffer for bitmap.
        /// </summary>
        private class BitmapFramebuffer : ILockedFramebuffer
        {
            private WriteableBitmapImpl _parent;
            private SKBitmap _bitmap;

            /// <summary>
            /// Create framebuffer from given bitmap.
            /// </summary>
            /// <param name="parent">Parent bitmap impl.</param>
            /// <param name="bitmap">Bitmap.</param>
            public BitmapFramebuffer(WriteableBitmapImpl parent, SKBitmap bitmap)
            {
                _parent = parent;
                _bitmap = bitmap;
                Monitor.Enter(parent._lock);
            }

            /// <inheritdoc />
            public void Dispose()
            {
                _bitmap.NotifyPixelsChanged();
                _parent.Version++;
                Monitor.Exit(_parent._lock);
                _bitmap = null;
                _parent = null;
            }
            
            /// <inheritdoc />
            public IntPtr Address => _bitmap.GetPixels();

            /// <inheritdoc />
            public PixelSize Size => new PixelSize(_bitmap.Width, _bitmap.Height);

            /// <inheritdoc />
            public int RowBytes => _bitmap.RowBytes;

            /// <inheritdoc />
            public Vector Dpi { get; } = SkiaPlatform.DefaultDpi;

            /// <inheritdoc />
            public PixelFormat Format => _bitmap.ColorType.ToPixelFormat();
        }
    }
}
