using System;

namespace Avalonia.Rendering
{
    /// <summary>
    /// Provides data for the <see cref="IRenderer.SceneInvalidated"/> event.
    /// </summary>
    public class SceneInvalidatedEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SceneInvalidatedEventArgs"/> class.
        /// </summary>
        /// <param name="root">The render root that has been updated.</param>
        /// <param name="dirtyRect">The updated area.</param>
        public SceneInvalidatedEventArgs(
            IRenderRoot root,
            Rect dirtyRect)
        {
            RenderRoot = root;
            DirtyRect = dirtyRect;
        }

        /// <summary>
        /// Gets the invalidated area.
        /// </summary>
        public Rect DirtyRect { get; }

        /// <summary>
        /// Gets the render root that has been invalidated.
        /// </summary>
        public IRenderRoot RenderRoot { get; }
    }
}
