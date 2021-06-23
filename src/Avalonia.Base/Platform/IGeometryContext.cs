using System;
using Avalonia.Media;

namespace Avalonia.Platform
{
    /// <summary>
    /// Describes a geometry using drawing commands.
    /// </summary>
    public interface IGeometryContext : IDisposable
    {
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
        void ArcTo(Point point, Size size, double rotationAngle, bool isLargeArc, SweepDirection sweepDirection);

        /// <summary>
        /// Begins a new figure.
        /// </summary>
        /// <param name="startPoint">The starting point for the figure.</param>
        /// <param name="isFilled">Whether the figure is filled.</param>
        void BeginFigure(Point startPoint, bool isFilled = true);

        /// <summary>
        /// Draws a Bezier curve to the specified point.
        /// </summary>
        /// <param name="point1">The first control point used to specify the shape of the curve.</param>
        /// <param name="point2">The second control point used to specify the shape of the curve.</param>
        /// <param name="point3">The destination point for the end of the curve.</param>
        void CubicBezierTo(Point point1, Point point2, Point point3);

        /// <summary>
        /// Draws a quadratic Bezier curve to the specified point
        /// </summary>
        /// <param name="control">Control point</param>
        /// <param name="endPoint">DestinationPoint</param>
        void QuadraticBezierTo(Point control, Point endPoint);

        /// <summary>
        /// Draws a line to the specified point.
        /// </summary>
        /// <param name="point">The destination point.</param>
        void LineTo(Point point);

        /// <summary>
        /// Ends the figure started by <see cref="BeginFigure(Point, bool)"/>.
        /// </summary>
        /// <param name="isClosed">Whether the figure is closed.</param>
        void EndFigure(bool isClosed);

        /// <summary>
        /// Sets the fill rule.
        /// </summary>
        /// <param name="fillRule">The fill rule.</param>
        void SetFillRule(FillRule fillRule);
    }
}