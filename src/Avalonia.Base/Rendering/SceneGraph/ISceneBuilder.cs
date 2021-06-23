using Avalonia.VisualTree;

namespace Avalonia.Rendering.SceneGraph
{
    /// <summary>
    /// Builds a scene graph from a visual tree.
    /// </summary>
    public interface ISceneBuilder
    {
        /// <summary>
        /// Builds the initial scene graph for a visual tree.
        /// </summary>
        /// <param name="scene">The scene to build.</param>
        void UpdateAll(Scene scene);

        /// <summary>
        /// Updates the visual (and potentially its children) in a scene.
        /// </summary>
        /// <param name="scene">The scene.</param>
        /// <param name="visual">The visual to update.</param>
        /// <returns>True if changes were made, otherwise false.</returns>
        bool Update(Scene scene, IVisual visual);
    }
}