
namespace Avalonia.Rendering
{
    /// <summary>
    /// Defines the interface for a renderer factory.
    /// </summary>
    public interface IRendererFactory
    {
        /// <summary>
        /// Creates a renderer.
        /// </summary>
        /// <param name="root">The root visual.</param>
        /// <param name="renderLoop">The render loop.</param>
        IRenderer Create(IRenderRoot root, IRenderLoop renderLoop);
    }
}
