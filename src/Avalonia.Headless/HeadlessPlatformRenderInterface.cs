using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Rendering;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Utilities;
using Avalonia.Visuals.Media.Imaging;

namespace Avalonia.Headless
{
    internal class HeadlessPlatformRenderInterface : IPlatformRenderInterface
    {
        public static void Initialize()
        {
            AvaloniaLocator.CurrentMutable
                .Bind<IPlatformRenderInterface>().ToConstant(new HeadlessPlatformRenderInterface());
        }

        public IEnumerable<string> InstalledFontNames { get; } = new[] { "Tahoma" };

        public bool SupportsIndividualRoundRects => false;

        public AlphaFormat DefaultAlphaFormat => AlphaFormat.Premul;

        public PixelFormat DefaultPixelFormat => PixelFormat.Rgba8888;

        public IGeometryImpl CreateEllipseGeometry(Rect rect) => new HeadlessGeometryStub(rect);

        public IGeometryImpl CreateLineGeometry(Point p1, Point p2)
        {
            var tl = new Point(Math.Min(p1.X, p2.X), Math.Min(p1.Y, p2.Y));
            var br = new Point(Math.Max(p1.X, p2.X), Math.Max(p1.Y, p2.Y));
            return new HeadlessGeometryStub(new Rect(tl, br));
        }

        public IGeometryImpl CreateRectangleGeometry(Rect rect)
        {
            return new HeadlessGeometryStub(rect);
        }

        public IStreamGeometryImpl CreateStreamGeometry() => new HeadlessStreamingGeometryStub();

        public IRenderTarget CreateRenderTarget(IEnumerable<object> surfaces) => new HeadlessRenderTarget();

        public IRenderTargetBitmapImpl CreateRenderTargetBitmap(PixelSize size, Vector dpi)
        {
            return new HeadlessBitmapStub(size, dpi);
        }

        public IWriteableBitmapImpl CreateWriteableBitmap(PixelSize size, Vector dpi, PixelFormat format, AlphaFormat alphaFormat)
        {
            return new HeadlessBitmapStub(size, dpi);
        }

        public IBitmapImpl LoadBitmap(string fileName)
        {
            return new HeadlessBitmapStub(new Size(1, 1), new Vector(96, 96));
        }

        public IBitmapImpl LoadBitmap(Stream stream)
        {
            return new HeadlessBitmapStub(new Size(1, 1), new Vector(96, 96));
        }

        public IBitmapImpl LoadBitmap(PixelFormat format, AlphaFormat alphaFormat, IntPtr data, PixelSize size, Vector dpi, int stride)
        {
            return new HeadlessBitmapStub(new Size(1, 1), new Vector(96, 96));
        }        

        public IBitmapImpl LoadBitmapToWidth(Stream stream, int width, BitmapInterpolationMode interpolationMode = BitmapInterpolationMode.HighQuality)
        {
            return new HeadlessBitmapStub(new Size(width, width), new Vector(96, 96));
        }

        public IBitmapImpl LoadBitmapToHeight(Stream stream, int height, BitmapInterpolationMode interpolationMode = BitmapInterpolationMode.HighQuality)
        {
            return new HeadlessBitmapStub(new Size(height, height), new Vector(96, 96));
        }

        public IBitmapImpl ResizeBitmap(IBitmapImpl bitmapImpl, PixelSize destinationSize, BitmapInterpolationMode interpolationMode = BitmapInterpolationMode.HighQuality)
        {
            return new HeadlessBitmapStub(destinationSize, new Vector(96, 96));
        }

        public IGlyphRunImpl CreateGlyphRun(GlyphRun glyphRun)
        {
            return new HeadlessGlyphRunStub();
        }

        class HeadlessGeometryStub : IGeometryImpl
        {
            public HeadlessGeometryStub(Rect bounds)
            {
                Bounds = bounds;
            }

            public Rect Bounds { get; set; }
            
            public double ContourLength { get; } = 0;
            
            public virtual bool FillContains(Point point) => Bounds.Contains(point);

            public Rect GetRenderBounds(IPen pen)
            {
                if(pen is null)
                {
                    return Bounds;
                }

                return Bounds.Inflate(pen.Thickness / 2);
            }

            public bool StrokeContains(IPen pen, Point point)
            {
                return false;
            }

            public IGeometryImpl Intersect(IGeometryImpl geometry)
                => new HeadlessGeometryStub(geometry.Bounds.Intersect(Bounds));

            public ITransformedGeometryImpl WithTransform(Matrix transform) =>
                new HeadlessTransformedGeometryStub(this, transform);

            public bool TryGetPointAtDistance(double distance, out Point point)
            {
                point = new Point();
                return false;
            }

            public bool TryGetPointAndTangentAtDistance(double distance, out Point point, out Point tangent)
            {
                point = new Point();
                tangent = new Point();
                return false;
            }

            public bool TryGetSegment(double startDistance, double stopDistance, bool startOnBeginFigure, out IGeometryImpl segmentGeometry)
            {
                segmentGeometry = null;
                return false;
            }
        }

        class HeadlessTransformedGeometryStub : HeadlessGeometryStub, ITransformedGeometryImpl
        {
            public HeadlessTransformedGeometryStub(IGeometryImpl b, Matrix transform) : this(Fix(b, transform))
            {

            }

            static (IGeometryImpl, Matrix, Rect) Fix(IGeometryImpl b, Matrix transform)
            {
                if (b is HeadlessTransformedGeometryStub transformed)
                {
                    b = transformed.SourceGeometry;
                    transform = transformed.Transform * transform;
                }

                return (b, transform, b.Bounds.TransformToAABB(transform));
            }

            private HeadlessTransformedGeometryStub((IGeometryImpl b, Matrix transform, Rect bounds) fix) : base(fix.bounds)
            {
                SourceGeometry = fix.b;
                Transform = fix.transform;
            }


            public IGeometryImpl SourceGeometry { get; }
            public Matrix Transform { get; }
        }

        class HeadlessGlyphRunStub : IGlyphRunImpl
        {
            public void Dispose()
            {
            }
        }

        class HeadlessStreamingGeometryStub : HeadlessGeometryStub, IStreamGeometryImpl
        {
            public HeadlessStreamingGeometryStub() : base(Rect.Empty)
            {
            }

            public IStreamGeometryImpl Clone()
            {
                return this;
            }

            public IStreamGeometryContextImpl Open()
            {
                return new HeadlessStreamingGeometryContextStub(this);
            }

            class HeadlessStreamingGeometryContextStub : IStreamGeometryContextImpl
            {
                private readonly HeadlessStreamingGeometryStub _parent;
                private double _x1, _y1, _x2, _y2;
                public HeadlessStreamingGeometryContextStub(HeadlessStreamingGeometryStub parent)
                {
                    _parent = parent;
                }

                void Track(Point pt)
                {
                    if (_x1 > pt.X)
                        _x1 = pt.X;
                    if (_x2 < pt.X)
                        _x2 = pt.X;
                    if (_y1 > pt.Y)
                        _y1 = pt.Y;
                    if (_y2 < pt.Y)
                        _y2 = pt.Y;
                }

                public void Dispose()
                {
                    _parent.Bounds = new Rect(_x1, _y1, _x2 - _x1, _y2 - _y1);
                }

                public void ArcTo(Point point, Size size, double rotationAngle, bool isLargeArc, SweepDirection sweepDirection)
                    => Track(point);

                public void BeginFigure(Point startPoint, bool isFilled = true) => Track(startPoint);

                public void CubicBezierTo(Point point1, Point point2, Point point3)
                {
                    Track(point1);
                    Track(point2);
                    Track(point3);
                }

                public void QuadraticBezierTo(Point control, Point endPoint)
                {
                    Track(control);
                    Track(endPoint);
                }

                public void LineTo(Point point) => Track(point);

                public void EndFigure(bool isClosed)
                {
                    Dispose();
                }

                public void SetFillRule(FillRule fillRule)
                {

                }
            }
        }

        class HeadlessBitmapStub : IBitmapImpl, IDrawingContextLayerImpl, IWriteableBitmapImpl
        {
            public Size Size { get; }

            public HeadlessBitmapStub(Size size, Vector dpi)
            {
                Size = size;
                Dpi = dpi;
                var pixel = Size * (Dpi / 96);
                PixelSize = new PixelSize(Math.Max(1, (int)pixel.Width), Math.Max(1, (int)pixel.Height));
            }

            public HeadlessBitmapStub(PixelSize size, Vector dpi)
            {
                PixelSize = size;
                Dpi = dpi;
                Size = PixelSize.ToSizeWithDpi(dpi);
            }

            public void Dispose()
            {

            }

            public IDrawingContextImpl CreateDrawingContext(IVisualBrushRenderer visualBrushRenderer)
            {
                return new HeadlessDrawingContextStub();
            }

            public void Blit(IDrawingContextImpl context)
            {
                
            }

            public bool CanBlit => false;

            public Vector Dpi { get; }
            public PixelSize PixelSize { get; }
            public int Version { get; set; }
            public void Save(string fileName)
            {

            }

            public void Save(Stream stream)
            {

            }

            public ILockedFramebuffer Lock()
            {
                Version++;
                var mem = Marshal.AllocHGlobal(PixelSize.Width * PixelSize.Height * 4);
                return new LockedFramebuffer(mem, PixelSize, PixelSize.Width * 4, Dpi, PixelFormat.Rgba8888,
                    () => Marshal.FreeHGlobal(mem));
            }
        }

        class HeadlessDrawingContextStub : IDrawingContextImpl
        {
            public void Dispose()
            {

            }

            public Matrix Transform { get; set; }
            public void Clear(Color color)
            {

            }

            public IDrawingContextLayerImpl CreateLayer(Size size)
            {
                return new HeadlessBitmapStub(size, new Vector(96, 96));
            }

            public void PushClip(Rect clip)
            {

            }

            public void PopClip()
            {

            }

            public void PushOpacity(double opacity)
            {

            }

            public void PopOpacity()
            {

            }

            public void PushOpacityMask(IBrush mask, Rect bounds)
            {

            }

            public void PopOpacityMask()
            {

            }

            public void PushGeometryClip(IGeometryImpl clip)
            {

            }

            public void PopGeometryClip()
            {

            }

            public void PushBitmapBlendMode(BitmapBlendingMode blendingMode)
            {
                
            }

            public void PopBitmapBlendMode()
            {
                
            }

            public void Custom(ICustomDrawOperation custom)
            {

            }

            public void DrawLine(IPen pen, Point p1, Point p2)
            {
                throw new NotImplementedException();
            }

            public void DrawGeometry(IBrush brush, IPen pen, IGeometryImpl geometry)
            {
            }

            public void DrawRectangle(IPen pen, Rect rect, float cornerRadius = 0)
            {
            }

            public void DrawBitmap(IRef<IBitmapImpl> source, double opacity, Rect sourceRect, Rect destRect, BitmapInterpolationMode bitmapInterpolationMode = BitmapInterpolationMode.Default)
            {
                
            }

            public void DrawBitmap(IRef<IBitmapImpl> source, IBrush opacityMask, Rect opacityMaskRect, Rect destRect)
            {
                
            }

            public void DrawRectangle(IBrush brush, IPen pen, RoundedRect rect, BoxShadows boxShadow = default)
            {
                
            }

            public void DrawGlyphRun(IBrush foreground, GlyphRun glyphRun)
            {
                
            }

            public void PushClip(RoundedRect clip)
            {
                
            }
        }

        class HeadlessRenderTarget : IRenderTarget
        {
            public void Dispose()
            {

            }

            public IDrawingContextImpl CreateDrawingContext(IVisualBrushRenderer visualBrushRenderer)
            {
                return new HeadlessDrawingContextStub();
            }
        }
    }
}
