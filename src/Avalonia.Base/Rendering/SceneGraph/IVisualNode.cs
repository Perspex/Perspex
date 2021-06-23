using System;
using System.Collections.Generic;
using Avalonia.Platform;
using Avalonia.Utilities;
using Avalonia.VisualTree;

namespace Avalonia.Rendering.SceneGraph
{
    /// <summary>
    /// Represents a node in the low-level scene graph representing an <see cref="IVisual"/>.
    /// </summary>
    public interface IVisualNode : IDisposable
    {
        /// <summary>
        /// Gets the visual to which the node relates.
        /// </summary>
        IVisual Visual { get; }

        /// <summary>
        /// Gets the parent scene graph node.
        /// </summary>
        IVisualNode Parent { get; }

        /// <summary>
        /// Gets the transform for the node from global to control coordinates.
        /// </summary>
        Matrix Transform { get; }

        /// <summary>
        /// Gets the corner radius of visual. Contents are clipped to this radius.
        /// </summary>
        CornerRadius ClipToBoundsRadius { get; }

        /// <summary>
        /// Gets the bounds of the node's geometry in global coordinates.
        /// </summary>
        Rect Bounds { get; }

        /// <summary>
        /// Gets the clip bounds for the node in global coordinates.
        /// </summary>
        Rect ClipBounds { get; }

        /// <summary>
        /// Gets the layout bounds for the node in global coordinates.
        /// </summary>
        Rect LayoutBounds { get; }

        /// <summary>
        /// Whether the node is clipped to <see cref="ClipBounds"/>.
        /// </summary>
        bool ClipToBounds { get; }

        /// <summary>
        /// Gets the node's clip geometry, if any.
        /// </summary>
        IGeometryImpl GeometryClip { get; set; }

        /// <summary>
        /// Gets a value indicating whether one of the node's ancestors has a geometry clip.
        /// </summary>
        bool HasAncestorGeometryClip { get; }

        /// <summary>
        /// Gets the child scene graph nodes.
        /// </summary>
        IReadOnlyList<IVisualNode> Children { get; }

        /// <summary>
        /// Gets the drawing operations for the visual.
        /// </summary>
        IReadOnlyList<IRef<IDrawOperation>> DrawOperations { get; }

        /// <summary>
        /// Gets the opacity of the scene graph node.
        /// </summary>
        double Opacity { get; }

        /// <summary>
        /// Sets up the drawing context for rendering the node's geometry.
        /// </summary>
        /// <param name="context">The drawing context.</param>
        /// <param name="skipOpacity">Whether to skip pushing the control's opacity.</param>
        void BeginRender(IDrawingContextImpl context, bool skipOpacity);

        /// <summary>
        /// Resets the drawing context after rendering the node's geometry.
        /// </summary>
        /// <param name="context">The drawing context.</param>
        /// <param name="skipOpacity">Whether to skip popping the control's opacity.</param>
        void EndRender(IDrawingContextImpl context, bool skipOpacity);

        /// <summary>
        /// Hit test the geometry in this node.
        /// </summary>
        /// <param name="p">The point in global coordinates.</param>
        /// <returns>True if the point hits the node's geometry; otherwise false.</returns>
        /// <remarks>
        /// This method does not recurse to child <see cref="IVisualNode"/>s, if you want
        /// to hit test children they must be hit tested manually.
        /// </remarks>
        bool HitTest(Point p);

        bool Disposed { get; }
    }
}
