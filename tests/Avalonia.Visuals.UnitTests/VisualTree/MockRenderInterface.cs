using System;
using System.Collections.Generic;
using System.IO;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.UnitTests;
using Avalonia.Visuals.Media.Imaging;

namespace Avalonia.Visuals.UnitTests.VisualTree
{
    class MockRenderInterface : IPlatformRenderInterface
    {
        public IRenderTarget CreateRenderTarget(IEnumerable<object> surfaces)
        {
            throw new NotImplementedException();
        }

        public IRenderTargetBitmapImpl CreateRenderTargetBitmap(PixelSize size, Vector dpi)
        {
            throw new NotImplementedException();
        }

        public IStreamGeometryImpl CreateStreamGeometry()
        {
            return new MockStreamGeometry();
        }

        public IBitmapImpl LoadBitmap(Stream stream)
        {
            throw new NotImplementedException();
        }

        public IBitmapImpl LoadBitmap(string fileName)
        {
            throw new NotImplementedException();
        }

        public IBitmapImpl LoadBitmap(PixelFormat format, AlphaFormat alphaFormat, IntPtr data, PixelSize size, Vector dpi, int stride)
        {
            throw new NotImplementedException();
        }

        public IGlyphRunImpl CreateGlyphRun(GlyphRun glyphRun)
        {
            throw new NotImplementedException();
        }

        public bool SupportsIndividualRoundRects { get; set; }
        public AlphaFormat DefaultAlphaFormat { get; }
        public PixelFormat DefaultPixelFormat { get; }

        public IFontManagerImpl CreateFontManager()
        {
            return new MockFontManagerImpl();
        }

        public IWriteableBitmapImpl CreateWriteableBitmap(PixelSize size, Vector dpi, PixelFormat fmt, AlphaFormat alphaFormat)
        {
            throw new NotImplementedException();
        }

        public IGeometryImpl CreateEllipseGeometry(Rect rect)
        {
            throw new NotImplementedException();
        }

        public IGeometryImpl CreateLineGeometry(Point p1, Point p2)
        {
            throw new NotImplementedException();
        }

        public IGeometryImpl CreateRectangleGeometry(Rect rect)
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

        class MockStreamGeometry : IStreamGeometryImpl
        {
            private MockStreamGeometryContext _impl = new MockStreamGeometryContext();
            public Rect Bounds
            {
                get
                {
                    throw new NotImplementedException();
                }
            }

            public double ContourLength { get; }

            public IStreamGeometryImpl Clone()
            {
                return this;
            }

            public void Dispose()
            {
            }

            public bool FillContains(Point point)
            {
                return _impl.FillContains(point);
            }

            public Rect GetRenderBounds(IPen pen)
            {
                throw new NotImplementedException();
            }

            public IGeometryImpl Intersect(IGeometryImpl geometry)
            {
                throw new NotImplementedException();
            }

            public IStreamGeometryContextImpl Open()
            {
                return _impl;
            }

            public bool StrokeContains(IPen pen, Point point)
            {
                throw new NotImplementedException();
            }

            public ITransformedGeometryImpl WithTransform(Matrix transform)
            {
                throw new NotImplementedException();
            }

            public bool TryGetPointAtDistance(double distance, out Point point)
            {
                throw new NotImplementedException();
            }

            public bool TryGetPointAndTangentAtDistance(double distance, out Point point, out Point tangent)
            {
                throw new NotImplementedException();
            }

            public bool TryGetSegment(double startDistance, double stopDistance, bool startOnBeginFigure, out IGeometryImpl segmentGeometry)
            {
                throw new NotImplementedException();
            }

            class MockStreamGeometryContext : IStreamGeometryContextImpl
            {
                private List<Point> points = new List<Point>();
                public void ArcTo(Point point, Size size, double rotationAngle, bool isLargeArc, SweepDirection sweepDirection)
                {
                    throw new NotImplementedException();
                }

                public void BeginFigure(Point startPoint, bool isFilled)
                {
                    points.Add(startPoint);
                }

                public void CubicBezierTo(Point point1, Point point2, Point point3)
                {
                    throw new NotImplementedException();
                }

                public void Dispose()
                {
                }

                public void EndFigure(bool isClosed)
                {
                }

                public void LineTo(Point point)
                {
                    points.Add(point);
                }

                public void QuadraticBezierTo(Point control, Point endPoint)
                {
                    throw new NotImplementedException();
                }

                public void SetFillRule(FillRule fillRule)
                {
                }

                public bool FillContains(Point point)
                {
                    // Use the algorithm from http://www.blackpawn.com/texts/pointinpoly/default.html
                    // to determine if the point is in the geometry (since it will always be convex in this situation)
                    for (int i = 0; i < points.Count; i++)
                    {
                        var a = points[i];
                        var b = points[(i + 1) % points.Count];
                        var c = points[(i + 2) % points.Count];

                        Vector v0 = c - a;
                        Vector v1 = b - a;
                        Vector v2 = point - a;

                        var dot00 = v0 * v0;
                        var dot01 = v0 * v1;
                        var dot02 = v0 * v2;
                        var dot11 = v1 * v1;
                        var dot12 = v1 * v2;


                        var invDenom = 1 / (dot00 * dot11 - dot01 * dot01);
                        var u = (dot11 * dot02 - dot01 * dot12) * invDenom;
                        var v = (dot00 * dot12 - dot01 * dot02) * invDenom;
                        if ((u >= 0) && (v >= 0) && (u + v < 1)) return true;
                    }
                    return false;
                }
            }
        }
    }

}
