using Avalonia.Platform;

namespace Avalonia.Media
{
    /// <summary>
    /// Describes a geometry using drawing commands.
    /// </summary>
    /// <remarks>
    /// This class is used to define the geometry of a <see cref="StreamGeometry"/>. An instance
    /// of <see cref="StreamGeometryContext"/> is obtained by calling
    /// <see cref="StreamGeometry.Open"/>.
    /// </remarks>
    /// TODO: This class is just a wrapper around IStreamGeometryContextImpl: is it needed?
    public class StreamGeometryContext : IGeometryContext
    {
        private readonly IStreamGeometryContextImpl _impl;

        /// <summary>
        /// Initializes a new instance of the <see cref="StreamGeometryContext"/> class.
        /// </summary>
        /// <param name="impl">The platform-specific implementation.</param>
        public StreamGeometryContext(IStreamGeometryContextImpl impl)
        {
            _impl = impl;
        }

        /// <summary>
        /// Sets path's winding rule (default is EvenOdd). You should call this method before any calls to BeginFigure. If you wonder why, ask Direct2D guys about their design decisions.
        /// </summary>
        /// <param name="fillRule"></param>

        public void SetFillRule(FillRule fillRule)
        {
            _impl.SetFillRule(fillRule);
        }

        /// <summary>
        /// Draws an arc to the specified point.
        /// </summary>
        /// <param name="point">The destination point.</param>
        /// <param name="size">The radii of an oval whose perimeter is used to draw the angle.</param>
        /// <param name="rotationAngle">The rotation angle of the oval that specifies the curve.</param>
        /// <param name="isLargeArc">true to draw the arc greater than 180 degrees; otherwise, false.</param>
        /// <param name="sweepDirection">
        /// A value that indicates whether the arc is drawn in the Clockwise or Counterclockwise direction.
        /// </param>
        public void ArcTo(Point point, Size size, double rotationAngle, bool isLargeArc, SweepDirection sweepDirection)
        {
            _impl.ArcTo(point, size, rotationAngle, isLargeArc, sweepDirection);
        }

        /// <summary>
        /// Begins a new figure.
        /// </summary>
        /// <param name="startPoint">The starting point for the figure.</param>
        /// <param name="isFilled">Whether the figure is filled.</param>
        public void BeginFigure(Point startPoint, bool isFilled)
        {
            _impl.BeginFigure(startPoint, isFilled);
        }

        /// <summary>
        /// Draws a Bezier curve to the specified point.
        /// </summary>
        /// <param name="point1">The first control point used to specify the shape of the curve.</param>
        /// <param name="point2">The second control point used to specify the shape of the curve.</param>
        /// <param name="point3">The destination point for the end of the curve.</param>
        public void CubicBezierTo(Point point1, Point point2, Point point3)
        {
            _impl.CubicBezierTo(point1, point2, point3);
        }

        /// <summary>
        /// Draws a quadratic Bezier curve to the specified point
        /// </summary>
        /// <param name="control">The control point used to specify the shape of the curve.</param>
        /// <param name="endPoint">The destination point for the end of the curve.</param>
        public void QuadraticBezierTo(Point control, Point endPoint)
        {
            _impl.QuadraticBezierTo(control, endPoint);
        }

        /// <summary>
        /// Draws a line to the specified point.
        /// </summary>
        /// <param name="point">The destination point.</param>
        public void LineTo(Point point)
        {
            _impl.LineTo(point);
        }

        /// <summary>
        /// Ends the figure started by <see cref="BeginFigure(Point, bool)"/>.
        /// </summary>
        /// <param name="isClosed">Whether the figure is closed.</param>
        public void EndFigure(bool isClosed)
        {
            _impl.EndFigure(isClosed);
        }

        /// <summary>
        /// Finishes the drawing session.
        /// </summary>
        public void Dispose()
        {
            _impl.Dispose();
        }
    }
}
