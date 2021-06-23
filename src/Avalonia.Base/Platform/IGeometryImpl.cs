using Avalonia.Media;

namespace Avalonia.Platform
{
    /// <summary>
    /// Defines the platform-specific interface for a <see cref="Geometry"/>.
    /// </summary>
    public interface IGeometryImpl
    {
        /// <summary>
        /// Gets the geometry's bounding rectangle.
        /// </summary>
        Rect Bounds { get; }
        
        /// <summary>
        /// Gets the geometry's total length as if all its contours are placed
        /// in a straight line.
        /// </summary>
        double ContourLength { get; }

        /// <summary>
        /// Gets the geometry's bounding rectangle with the specified pen.
        /// </summary>
        /// <param name="pen">The pen to use. May be null.</param>
        /// <returns>The bounding rectangle.</returns>
        Rect GetRenderBounds(IPen pen);

        /// <summary>
        /// Indicates whether the geometry's fill contains the specified point.
        /// </summary>
        /// <param name="point">The point.</param>
        /// <returns><c>true</c> if the geometry contains the point; otherwise, <c>false</c>.</returns>
        bool FillContains(Point point);

        /// <summary>
        /// Intersects the geometry with another geometry.
        /// </summary>
        /// <param name="geometry">The other geometry.</param>
        /// <returns>A new <see cref="IGeometryImpl"/> representing the intersection.</returns>
        IGeometryImpl Intersect(IGeometryImpl geometry);

        /// <summary>
        /// Indicates whether the geometry's stroke contains the specified point.
        /// </summary>
        /// <param name="pen">The stroke to use.</param>
        /// <param name="point">The point.</param>
        /// <returns><c>true</c> if the geometry contains the point; otherwise, <c>false</c>.</returns>
        bool StrokeContains(IPen pen, Point point);

        /// <summary>
        /// Makes a clone of the geometry with the specified transform.
        /// </summary>
        /// <param name="transform">The transform.</param>
        /// <returns>The cloned geometry.</returns>
        ITransformedGeometryImpl WithTransform(Matrix transform);

        /// <summary>
        /// Attempts to get the corresponding point at the
        /// specified distance
        /// </summary>
        /// <param name="distance">The contour distance to get from.</param>
        /// <param name="point">The point in the specified distance.</param>
        /// <returns>If there's valid point at the specified distance.</returns>
        bool TryGetPointAtDistance(double distance, out Point point);

        /// <summary>
        /// Attempts to get the corresponding point and
        /// tangent from the specified distance along the
        /// contour of the geometry.
        /// </summary>
        /// <param name="distance">The contour distance to get from.</param>
        /// <param name="point">The point in the specified distance.</param>
        /// <param name="tangent">The tangent in the specified distance.</param>
        /// <returns>If there's valid point and tangent at the specified distance.</returns>
        bool TryGetPointAndTangentAtDistance(double distance, out Point point, out Point tangent);
        
        /// <summary>
        /// Attempts to get the corresponding path segment
        /// given by the two distances specified.
        /// Imagine it like snipping a part of the current
        /// geometry.
        /// </summary>
        /// <param name="startDistance">The contour distance to start snipping from.</param>
        /// <param name="stopDistance">The contour distance to stop snipping to.</param>
        /// <param name="startOnBeginFigure">If ture, the resulting snipped path will start with a BeginFigure call.</param>
        /// <param name="segmentGeometry">The resulting snipped path.</param>
        /// <returns>If the snipping operation is successful.</returns>
        bool TryGetSegment(double startDistance, double stopDistance, bool startOnBeginFigure, out IGeometryImpl segmentGeometry);
    }
}
