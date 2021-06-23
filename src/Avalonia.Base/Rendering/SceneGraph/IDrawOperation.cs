using System;
using Avalonia.Platform;

namespace Avalonia.Rendering.SceneGraph
{
    /// <summary>
    /// Represents a node in the low-level scene graph that represents geometry.
    /// </summary>
    public interface IDrawOperation : IDisposable
    {
        /// <summary>
        /// Gets the bounds of the visible content in the node in global coordinates.
        /// </summary>
        Rect Bounds { get; }

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

        /// <summary>
        /// Renders the node to a drawing context.
        /// </summary>
        /// <param name="context">The drawing context.</param>
        void Render(IDrawingContextImpl context);
    }
}
