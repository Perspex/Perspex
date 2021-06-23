using System.Collections.Generic;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Avalonia.Platform;
using Avalonia.VisualTree;

namespace Avalonia.Rendering.SceneGraph
{
    /// <summary>
    /// A node in the scene graph which represents a geometry draw.
    /// </summary>
    internal class GeometryNode : BrushDrawOperation
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GeometryNode"/> class.
        /// </summary>
        /// <param name="transform">The transform.</param>
        /// <param name="brush">The fill brush.</param>
        /// <param name="pen">The stroke pen.</param>
        /// <param name="geometry">The geometry.</param>
        /// <param name="childScenes">Child scenes for drawing visual brushes.</param>
        public GeometryNode(Matrix transform,
            IBrush brush,
            IPen pen,
            IGeometryImpl geometry,
            IDictionary<IVisual, Scene> childScenes = null)
            : base(geometry.GetRenderBounds(pen), transform)
        {
            Transform = transform;
            Brush = brush?.ToImmutable();
            Pen = pen?.ToImmutable();
            Geometry = geometry;
            ChildScenes = childScenes;
        }

        /// <summary>
        /// Gets the transform with which the node will be drawn.
        /// </summary>
        public Matrix Transform { get; }

        /// <summary>
        /// Gets the fill brush.
        /// </summary>
        public IBrush Brush { get; }

        /// <summary>
        /// Gets the stroke pen.
        /// </summary>
        public ImmutablePen Pen { get; }

        /// <summary>
        /// Gets the geometry to draw.
        /// </summary>
        public IGeometryImpl Geometry { get; }

        /// <inheritdoc/>
        public override IDictionary<IVisual, Scene> ChildScenes { get; }

        /// <summary>
        /// Determines if this draw operation equals another.
        /// </summary>
        /// <param name="transform">The transform of the other draw operation.</param>
        /// <param name="brush">The fill of the other draw operation.</param>
        /// <param name="pen">The stroke of the other draw operation.</param>
        /// <param name="geometry">The geometry of the other draw operation.</param>
        /// <returns>True if the draw operations are the same, otherwise false.</returns>
        /// <remarks>
        /// The properties of the other draw operation are passed in as arguments to prevent
        /// allocation of a not-yet-constructed draw operation object.
        /// </remarks>
        public bool Equals(Matrix transform, IBrush brush, IPen pen, IGeometryImpl geometry)
        {
            return transform == Transform &&
                   Equals(brush, Brush) &&
                   Equals(Pen, pen) &&
                   Equals(geometry, Geometry);
        }

        /// <inheritdoc/>
        public override void Render(IDrawingContextImpl context)
        {
            context.Transform = Transform;
            context.DrawGeometry(Brush, Pen, Geometry);
        }

        /// <inheritdoc/>
        public override bool HitTest(Point p)
        {
            if (Transform.HasInverse)
            {
                p *= Transform.Invert();
                return (Brush != null && Geometry.FillContains(p)) ||
                    (Pen != null && Geometry.StrokeContains(Pen, p));
            }

            return false;
        }
    }
}
