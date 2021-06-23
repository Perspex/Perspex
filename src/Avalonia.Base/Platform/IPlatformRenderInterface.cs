using System;
using System.Collections.Generic;
using System.IO;
using Avalonia.Media;
using Avalonia.Media.Imaging;

namespace Avalonia.Platform
{
    /// <summary>
    /// Defines the main platform-specific interface for the rendering subsystem.
    /// </summary>
    public interface IPlatformRenderInterface
    {
        /// <summary>
        /// Creates a formatted text implementation.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <param name="typeface">The base typeface.</param>
        /// <param name="fontSize">The font size.</param>
        /// <param name="textAlignment">The text alignment.</param>
        /// <param name="wrapping">The text wrapping mode.</param>
        /// <param name="constraint">The text layout constraints.</param>
        /// <param name="spans">The style spans.</param>
        /// <returns>An <see cref="IFormattedTextImpl"/>.</returns>
        IFormattedTextImpl CreateFormattedText(
            string text,
            Typeface typeface,
            double fontSize,
            TextAlignment textAlignment,
            TextWrapping wrapping,
            Size constraint,
            IReadOnlyList<FormattedTextStyleSpan> spans);

        /// <summary>
        /// Creates an ellipse geometry implementation.
        /// </summary>
        /// <param name="rect">The bounds of the ellipse.</param>
        /// <returns>An ellipse geometry..</returns>
        IGeometryImpl CreateEllipseGeometry(Rect rect);

        /// <summary>
        /// Creates a line geometry implementation.
        /// </summary>
        /// <param name="p1">The start of the line.</param>
        /// <param name="p2">The end of the line.</param>
        /// <returns>A line geometry.</returns>
        IGeometryImpl CreateLineGeometry(Point p1, Point p2);

        /// <summary>
        /// Creates a rectangle geometry implementation.
        /// </summary>
        /// <param name="rect">The bounds of the rectangle.</param>
        /// <returns>A rectangle.</returns>
        IGeometryImpl CreateRectangleGeometry(Rect rect);

        /// <summary>
        /// Creates a stream geometry implementation.
        /// </summary>
        /// <returns>An <see cref="IStreamGeometryImpl"/>.</returns>
        IStreamGeometryImpl CreateStreamGeometry();

        /// <summary>
        /// Creates a renderer.
        /// </summary>
        /// <param name="surfaces">
        /// The list of native platform surfaces that can be used for output.
        /// </param>
        /// <returns>An <see cref="IRenderTarget"/>.</returns>
        IRenderTarget CreateRenderTarget(IEnumerable<object> surfaces);

        /// <summary>
        /// Creates a render target bitmap implementation.
        /// </summary>
        /// <param name="size">The size of the bitmap in device pixels.</param>
        /// <param name="dpi">The DPI of the bitmap.</param>
        /// <returns>An <see cref="IRenderTargetBitmapImpl"/>.</returns>
        IRenderTargetBitmapImpl CreateRenderTargetBitmap(PixelSize size, Vector dpi);

        /// <summary>
        /// Creates a writeable bitmap implementation.
        /// </summary>
        /// <param name="size">The size of the bitmap in device pixels.</param>
        /// <param name="dpi">The DPI of the bitmap.</param>
        /// <param name="format">Pixel format.</param>
        /// <param name="alphaFormat">Alpha format .</param>
        /// <returns>An <see cref="IWriteableBitmapImpl"/>.</returns>
        IWriteableBitmapImpl CreateWriteableBitmap(PixelSize size, Vector dpi, PixelFormat format, AlphaFormat alphaFormat);

        /// <summary>
        /// Loads a bitmap implementation from a file..
        /// </summary>
        /// <param name="fileName">The filename of the bitmap.</param>        
        /// <returns>An <see cref="IBitmapImpl"/>.</returns>
        IBitmapImpl LoadBitmap(string fileName);

        /// <summary>
        /// Loads a bitmap implementation from a file..
        /// </summary>
        /// <param name="stream">The stream to read the bitmap from.</param>        
        /// <returns>An <see cref="IBitmapImpl"/>.</returns>
        IBitmapImpl LoadBitmap(Stream stream);

        /// <summary>
        /// Loads a WriteableBitmap implementation from a stream to a specified width maintaining aspect ratio.
        /// </summary>
        /// <param name="stream">The stream to read the bitmap from.</param> 
        /// <param name="width">The desired width of the resulting bitmap.</param>
        /// <param name="interpolationMode">The <see cref="BitmapInterpolationMode"/> to use should resizing be required.</param>
        /// <returns>An <see cref="IWriteableBitmapImpl"/>.</returns>
        IWriteableBitmapImpl LoadWriteableBitmapToWidth(Stream stream, int width, BitmapInterpolationMode interpolationMode = BitmapInterpolationMode.HighQuality);

        /// <summary>
        /// Loads a WriteableBitmap implementation from a stream to a specified height maintaining aspect ratio.
        /// </summary>
        /// <param name="stream">The stream to read the bitmap from.</param> 
        /// <param name="height">The desired height of the resulting bitmap.</param>
        /// <param name="interpolationMode">The <see cref="BitmapInterpolationMode"/> to use should resizing be required.</param>
        /// <returns>An <see cref="IBitmapImpl"/>.</returns>
        IWriteableBitmapImpl LoadWriteableBitmapToHeight(Stream stream, int height, BitmapInterpolationMode interpolationMode = BitmapInterpolationMode.HighQuality);
        
        /// <summary>
        /// Loads a WriteableBitmap implementation from a file.
        /// </summary>
        /// <param name="fileName">The filename of the bitmap.</param>        
        /// <returns>An <see cref="IBitmapImpl"/>.</returns>
        IWriteableBitmapImpl LoadWriteableBitmap(string fileName);

        /// <summary>
        /// Loads a WriteableBitmap implementation from a file.
        /// </summary>
        /// <param name="stream">The stream to read the bitmap from.</param>        
        /// <returns>An <see cref="IBitmapImpl"/>.</returns>
        IWriteableBitmapImpl LoadWriteableBitmap(Stream stream);

        /// <summary>
        /// Loads a bitmap implementation from a stream to a specified width maintaining aspect ratio.
        /// </summary>
        /// <param name="stream">The stream to read the bitmap from.</param> 
        /// <param name="width">The desired width of the resulting bitmap.</param>
        /// <param name="interpolationMode">The <see cref="BitmapInterpolationMode"/> to use should resizing be required.</param>
        /// <returns>An <see cref="IBitmapImpl"/>.</returns>
        IBitmapImpl LoadBitmapToWidth(Stream stream, int width, BitmapInterpolationMode interpolationMode = BitmapInterpolationMode.HighQuality);

        /// <summary>
        /// Loads a bitmap implementation from a stream to a specified height maintaining aspect ratio.
        /// </summary>
        /// <param name="stream">The stream to read the bitmap from.</param> 
        /// <param name="height">The desired height of the resulting bitmap.</param>
        /// <param name="interpolationMode">The <see cref="BitmapInterpolationMode"/> to use should resizing be required.</param>
        /// <returns>An <see cref="IBitmapImpl"/>.</returns>
        IBitmapImpl LoadBitmapToHeight(Stream stream, int height, BitmapInterpolationMode interpolationMode = BitmapInterpolationMode.HighQuality);

        IBitmapImpl ResizeBitmap(IBitmapImpl bitmapImpl, PixelSize destinationSize, BitmapInterpolationMode interpolationMode = BitmapInterpolationMode.HighQuality);

        /// <summary>
        /// Loads a bitmap implementation from a pixels in memory.
        /// </summary>
        /// <param name="format">The pixel format.</param>
        /// <param name="alphaFormat">The alpha format.</param>
        /// <param name="data">The pointer to source bytes.</param>
        /// <param name="size">The size of the bitmap in device pixels.</param>
        /// <param name="dpi">The DPI of the bitmap.</param>
        /// <param name="stride">The number of bytes per row.</param>
        /// <returns>An <see cref="IBitmapImpl"/>.</returns>
        IBitmapImpl LoadBitmap(PixelFormat format, AlphaFormat alphaFormat, IntPtr data, PixelSize size, Vector dpi, int stride);

        /// <summary>
        /// Creates a platform implementation of a glyph run.
        /// </summary>
        /// <param name="glyphRun">The glyph run.</param>
        /// <returns></returns>
        IGlyphRunImpl CreateGlyphRun(GlyphRun glyphRun);

        bool SupportsIndividualRoundRects { get; }

        /// <summary>
        /// Default <see cref="AlphaFormat"/> used on this platform.
        /// </summary>
        public AlphaFormat DefaultAlphaFormat { get; }

        /// <summary>
        /// Default <see cref="PixelFormat"/> used on this platform.
        /// </summary>
        public PixelFormat DefaultPixelFormat { get; }
    }
}
