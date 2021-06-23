using System;
using System.Collections.Generic;
using System.IO;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Media.Imaging;
using Moq;

namespace Avalonia.UnitTests
{
    public class MockPlatformRenderInterface : IPlatformRenderInterface
    {
        public IFormattedTextImpl CreateFormattedText(
            string text,
            Typeface typeface,
            double fontSize,
            TextAlignment textAlignment,
            TextWrapping wrapping,
            Size constraint,
            IReadOnlyList<FormattedTextStyleSpan> spans)
        {
            return Mock.Of<IFormattedTextImpl>();
        }

        public IGeometryImpl CreateEllipseGeometry(Rect rect)
        {
            return Mock.Of<IGeometryImpl>();
        }

        public IGeometryImpl CreateLineGeometry(Point p1, Point p2)
        {
            return Mock.Of<IGeometryImpl>();
        }

        public IGeometryImpl CreateRectangleGeometry(Rect rect)
        {
            return Mock.Of<IGeometryImpl>(x => x.Bounds == rect);
        }

        public IRenderTarget CreateRenderTarget(IEnumerable<object> surfaces)
        {
            return Mock.Of<IRenderTarget>();
        }

        public IRenderTargetBitmapImpl CreateRenderTargetBitmap(PixelSize size, Vector dpi)
        {
            return Mock.Of<IRenderTargetBitmapImpl>();
        }

        public IStreamGeometryImpl CreateStreamGeometry()
        {
            return new MockStreamGeometryImpl();
        }

        public IWriteableBitmapImpl CreateWriteableBitmap(
            PixelSize size,
            Vector dpi,
            PixelFormat format,
            AlphaFormat alphaFormat)
        {
            throw new NotImplementedException();
        }

        public IBitmapImpl LoadBitmap(Stream stream)
        {
            return Mock.Of<IBitmapImpl>();
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

        public IBitmapImpl LoadBitmap(string fileName)
        {
            return Mock.Of<IBitmapImpl>();
        }

        public IBitmapImpl LoadBitmapToWidth(Stream stream, int width, BitmapInterpolationMode interpolationMode = BitmapInterpolationMode.HighQuality)
        {
            return Mock.Of<IBitmapImpl>();
        }

        public IBitmapImpl LoadBitmapToHeight(Stream stream, int height, BitmapInterpolationMode interpolationMode = BitmapInterpolationMode.HighQuality)
        {
            return Mock.Of<IBitmapImpl>();
        }

        public IBitmapImpl ResizeBitmap(IBitmapImpl bitmapImpl, PixelSize destinationSize, BitmapInterpolationMode interpolationMode = BitmapInterpolationMode.HighQuality)
        {
            return Mock.Of<IBitmapImpl>();
        }

        public IBitmapImpl LoadBitmap(
            PixelFormat format,
            AlphaFormat alphaFormat,
            IntPtr data,
            PixelSize size,
            Vector dpi,
            int stride)
        {
            throw new NotImplementedException();
        }

        public IGlyphRunImpl CreateGlyphRun(GlyphRun glyphRun)
        {
            return Mock.Of<IGlyphRunImpl>();
        }

        public bool SupportsIndividualRoundRects { get; set; }

        public AlphaFormat DefaultAlphaFormat => AlphaFormat.Premul;

        public PixelFormat DefaultPixelFormat => PixelFormat.Rgba8888;
    }
}
