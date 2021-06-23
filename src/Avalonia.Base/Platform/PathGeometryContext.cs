using System;
using Avalonia.Media;
using Avalonia.Platform;

namespace Avalonia.Visuals.Platform
{
    public class PathGeometryContext : IGeometryContext
    {
        private PathFigure _currentFigure;
        private PathGeometry _pathGeometry;

        public PathGeometryContext(PathGeometry pathGeometry)
        {
            _pathGeometry = pathGeometry ?? throw new ArgumentNullException(nameof(pathGeometry));
        }

        public void Dispose()
        {
            _pathGeometry = null;
        }

        public void ArcTo(Point point, Size size, double rotationAngle, bool isLargeArc, SweepDirection sweepDirection)
        {
            var arcSegment = new ArcSegment
            {
                Size = size,
                RotationAngle = rotationAngle,
                IsLargeArc = isLargeArc,
                SweepDirection = sweepDirection,
                Point = point
            };

            _currentFigure.Segments.Add(arcSegment);
        }

        public void BeginFigure(Point startPoint, bool isFilled)
        {
            _currentFigure = new PathFigure { StartPoint = startPoint, IsClosed = false, IsFilled = isFilled };

            _pathGeometry.Figures.Add(_currentFigure);
        }

        public void CubicBezierTo(Point point1, Point point2, Point point3)
        {
            var bezierSegment = new BezierSegment { Point1 = point1, Point2 = point2, Point3 = point3 };

            _currentFigure.Segments.Add(bezierSegment);
        }

        public void QuadraticBezierTo(Point control, Point endPoint)
        {
            var quadraticBezierSegment = new QuadraticBezierSegment { Point1 = control, Point2 = endPoint };

            _currentFigure.Segments.Add(quadraticBezierSegment);
        }

        public void LineTo(Point point)
        {
            var lineSegment = new LineSegment
            {
                Point = point
            };

            _currentFigure.Segments.Add(lineSegment);
        }

        public void EndFigure(bool isClosed)
        {
            if (_currentFigure != null)
            {
                _currentFigure.IsClosed = isClosed;
            }

            _currentFigure = null;
        }

        public void SetFillRule(FillRule fillRule)
        {
            _pathGeometry.FillRule = fillRule;
        }
    }
}
