using Avalonia.Media;
using Avalonia.Platform;

namespace Avalonia.Rendering
{
    /// <summary>
    /// Defines a renderer used to render a visual brush to a bitmap.
    /// </summary>
    public interface IVisualBrushRenderer
    {
        /// <summary>
        /// Gets the size of the intermediate render target to which the visual brush should be
        /// drawn.
        /// </summary>
        /// <param name="brush">The visual brush.</param>
        /// <returns>The size of the intermediate render target to create.</returns>
        Size GetRenderTargetSize(IVisualBrush brush);

        /// <summary>
        /// Renders a visual brush to a bitmap.
        /// </summary>
        /// <param name="context">The drawing context to render to.</param>
        /// <param name="brush">The visual brush.</param>
        /// <returns>A bitmap containing the rendered brush.</returns>
        void RenderVisualBrush(IDrawingContextImpl context, IVisualBrush brush);
    }
}
