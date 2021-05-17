using System;
using System.IO;
using Avalonia.Platform;
using SharpDX.WIC;
using PixelFormat = Avalonia.Platform.PixelFormat;

namespace Avalonia.Direct2D1.Media.Imaging
{
    class WriteableWicBitmapImpl : WicBitmapImpl, IWriteableBitmapImpl
    {
        public WriteableWicBitmapImpl(Stream stream, int decodeSize, bool horizontal,
            Avalonia.Visuals.Media.Imaging.BitmapInterpolationMode interpolationMode)
        : base(stream, decodeSize, horizontal, interpolationMode)
        {
        }
        
        public WriteableWicBitmapImpl(PixelSize size, Vector dpi, PixelFormat? pixelFormat, AlphaFormat? alphaFormat) 
            : base(size, dpi, pixelFormat, alphaFormat)
        {
        }

        public WriteableWicBitmapImpl(Stream stream)
            : base(stream)
        {
        }

        public WriteableWicBitmapImpl(string fileName)
            : base(fileName)
        {
        }

        class LockedBitmap : ILockedFramebuffer
        {
            private readonly WriteableWicBitmapImpl _parent;
            private readonly BitmapLock _lock;
            private readonly PixelFormat _format;

            public LockedBitmap(WriteableWicBitmapImpl parent, BitmapLock l, PixelFormat format)
            {
                _parent = parent;
                _lock = l;
                _format = format;
            }


            public void Dispose()
            {
                _lock.Dispose();
                _parent.Version++;
            }

            public IntPtr Address => _lock.Data.DataPointer;
            public PixelSize Size => _lock.Size.ToAvalonia();
            public int RowBytes => _lock.Stride;
            public Vector Dpi { get; } = new Vector(96, 96);
            public PixelFormat Format => _format;

        }

        public ILockedFramebuffer Lock() =>
            new LockedBitmap(this, WicImpl.Lock(BitmapLockFlags.Write), PixelFormat.Value);
    }
}
