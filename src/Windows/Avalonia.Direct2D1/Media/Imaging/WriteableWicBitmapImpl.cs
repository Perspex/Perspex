﻿// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using Avalonia.Platform;
using Avalonia.Rendering;
using SharpDX.WIC;
using PixelFormat = Avalonia.Platform.PixelFormat;

namespace Avalonia.Direct2D1.Media.Imaging
{
    class WriteableWicBitmapImpl : WicBitmapImpl, IWriteableBitmapImpl
    {
        public WriteableWicBitmapImpl(PixelSize size, Vector dpi, PixelFormat? pixelFormat) 
            : base(size, dpi, pixelFormat)
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

        public IDrawingContextImpl CreateDrawingContext(IVisualBrushRenderer visualBrushRenderer)
        {
            throw new NotImplementedException();
        }
    }
}
