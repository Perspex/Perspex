using System;
using System.Collections.Generic;
using System.IO;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.UnitTests;
using Avalonia.Visuals.Media.Imaging;

namespace Avalonia.Benchmarks
{
    internal class NullRenderingPlatform : IPlatformRenderInterface
    {
        public IFormattedTextImpl CreateFormattedText(string text, Typeface typeface, double fontSize, TextAlignment textAlignment,
            TextWrapping wrapping, Size constraint, IReadOnlyList<FormattedTextStyleSpan> spans)
        {
            return new NullFormattedTextImpl();
        }

        public IGeometryImpl CreateEllipseGeometry(Rect rect)
        {
            return new MockStreamGeometryImpl();
        }

        public IGeometryImpl CreateLineGeometry(Point p1, Point p2)
        {
            return new MockStreamGeometryImpl();
        }

        public IGeometryImpl CreateRectangleGeometry(Rect rect)
        {
            return new MockStreamGeometryImpl();
        }

        public IStreamGeometryImpl CreateStreamGeometry()
        {
            return new MockStreamGeometryImpl();
        }

        public IRenderTarget CreateRenderTarget(IEnumerable<object> surfaces)
        {
            throw new NotImplementedException();
        }

        public IRenderTargetBitmapImpl CreateRenderTargetBitmap(PixelSize size, Vector dpi)
        {
            throw new NotImplementedException();
        }

        public IWriteableBitmapImpl CreateWriteableBitmap(PixelSize size, Vector dpi, PixelFormat format, AlphaFormat alphaFormat)
        {
            throw new NotImplementedException();
        }

        public IBitmapImpl LoadBitmap(string fileName)
        {
            throw new NotImplementedException();
        }

        public IBitmapImpl LoadBitmap(Stream stream)
        {
            throw new NotImplementedException();
        }

        public IWriteableBitmapImpl LoadWriteableBitmapToWidth(Stream stream, int width,
            BitmapInterpolationMode interpolationMode = BitmapInterpolationMode.HighQuality)
        {
            throw new NotImplementedException();
        }

        public IWriteableBitmapImpl LoadWriteableBitmapToHeight(Stream stream, int height,
            BitmapInterpolationMode interpolationMode = BitmapInterpolationMode.HighQuality)
        {
            throw new NotImplementedException();
        }

        public IWriteableBitmapImpl LoadWriteableBitmap(string fileName)
        {
            throw new NotImplementedException();
        }

        public IWriteableBitmapImpl LoadWriteableBitmap(Stream stream)
        {
            throw new NotImplementedException();
        }

        public IBitmapImpl LoadBitmap(PixelFormat format, AlphaFormat alphaFormat, IntPtr data, PixelSize size, Vector dpi, int stride)
        {
            throw new NotImplementedException();
        }

        public IBitmapImpl LoadBitmapToWidth(Stream stream, int width, BitmapInterpolationMode interpolationMode = BitmapInterpolationMode.HighQuality)
        {
            throw new NotImplementedException();
        }

        public IBitmapImpl LoadBitmapToHeight(Stream stream, int height, BitmapInterpolationMode interpolationMode = BitmapInterpolationMode.HighQuality)
        {
            throw new NotImplementedException();
        }

        public IBitmapImpl ResizeBitmap(IBitmapImpl bitmapImpl, PixelSize destinationSize, BitmapInterpolationMode interpolationMode = BitmapInterpolationMode.HighQuality)
        {
            throw new NotImplementedException();
        }

        public IFontManagerImpl CreateFontManager()
        {
            return new MockFontManagerImpl();
        }

        public IGlyphRunImpl CreateGlyphRun(GlyphRun glyphRun)
        {
            return new NullGlyphRun();
        }

        public bool SupportsIndividualRoundRects => true;

        public AlphaFormat DefaultAlphaFormat => AlphaFormat.Premul;

        public PixelFormat DefaultPixelFormat => PixelFormat.Rgba8888;
    }
}
