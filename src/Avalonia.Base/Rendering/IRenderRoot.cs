using Avalonia.Platform;
using Avalonia.VisualTree;

namespace Avalonia.Rendering
{
    /// <summary>
    /// Represents the root of a renderable tree.
    /// </summary>
    public interface IRenderRoot : IVisual
    {
        /// <summary>
        /// Gets the client size of the window.
        /// </summary>
        Size ClientSize { get; }

        /// <summary>
        /// Gets the renderer for the window.
        /// </summary>
        IRenderer Renderer { get; }

        /// <summary>
        /// The scaling factor to use in rendering.
        /// </summary>
        double RenderScaling { get; }

        /// <summary>
        /// Creates a render target for the window.
        /// </summary>
        /// <returns>An <see cref="IRenderTarget"/>.</returns>
        IRenderTarget CreateRenderTarget();

        /// <summary>
        /// Adds a rectangle to the window's dirty region.
        /// </summary>
        /// <param name="rect">The rectangle.</param>
        void Invalidate(Rect rect);

        /// <summary>
        /// Converts a point from screen to client coordinates.
        /// </summary>
        /// <param name="point">The point in screen device coordinates.</param>
        /// <returns>The point in client coordinates.</returns>
        Point PointToClient(PixelPoint point);

        /// <summary>
        /// Converts a point from client to screen coordinates.
        /// </summary>
        /// <param name="point">The point in client coordinates.</param>
        /// <returns>The point in screen device coordinates.</returns>
        PixelPoint PointToScreen(Point point);
    }
}
