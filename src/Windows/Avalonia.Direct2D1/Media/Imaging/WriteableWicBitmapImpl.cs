﻿using System;
using Avalonia.Platform;
using Vortice.WIC;
using PixelFormat = Avalonia.Platform.PixelFormat;

namespace Avalonia.Direct2D1.Media.Imaging
{
    class WriteableWicBitmapImpl : WicBitmapImpl, IWriteableBitmapImpl
    {
        public WriteableWicBitmapImpl(PixelSize size, Vector dpi, PixelFormat? pixelFormat, AlphaFormat? alphaFormat) 
            : base(size, dpi, pixelFormat, alphaFormat)
        {
        }

        class LockedBitmap : ILockedFramebuffer
        {
            private readonly WriteableWicBitmapImpl _parent;
            private readonly IWICBitmapLock _lock;
            private readonly PixelFormat _format;

            public LockedBitmap(WriteableWicBitmapImpl parent, IWICBitmapLock l, PixelFormat format)
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
            new LockedBitmap(this, WicImpl.Lock(BitmapLockFlags.LockWrite), PixelFormat.Value);
    }
}
