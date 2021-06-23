using System;
using Avalonia.VisualTree;
using System.Collections.Generic;

namespace Avalonia.Rendering
{
    /// <summary>
    /// Defines the interface for a renderer.
    /// </summary>
    public interface IRenderer : IDisposable
    {
        /// <summary>
        /// Gets or sets a value indicating whether the renderer should draw an FPS counter.
        /// </summary>
        bool DrawFps { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the renderer should draw a visual representation
        /// of its dirty rectangles.
        /// </summary>
        bool DrawDirtyRects { get; set; }

        /// <summary>
        /// Raised when a portion of the scene has been invalidated.
        /// </summary>
        /// <remarks>
        /// Indicates that the underlying low-level scene information has been updated. Used to
        /// signal that an update to the current pointer-over state may be required.
        /// </remarks>
        event EventHandler<SceneInvalidatedEventArgs> SceneInvalidated;

        /// <summary>
        /// Mark a visual as dirty and needing re-rendering.
        /// </summary>
        /// <param name="visual">The visual.</param>
        void AddDirty(IVisual visual);

        /// <summary>
        /// Hit tests a location to find the visuals at the specified point.
        /// </summary>
        /// <param name="p">The point, in client coordinates.</param>
        /// <param name="root">The root of the subtree to search.</param>
        /// <param name="filter">
        /// A filter predicate. If the predicate returns false then the visual and all its
        /// children will be excluded from the results.
        /// </param>
        /// <returns>The visuals at the specified point, topmost first.</returns>
        IEnumerable<IVisual> HitTest(Point p, IVisual root, Func<IVisual, bool> filter);

        /// <summary>
        /// Hit tests a location to find first visual at the specified point.
        /// </summary>
        /// <param name="p">The point, in client coordinates.</param>
        /// <param name="root">The root of the subtree to search.</param>
        /// <param name="filter">
        /// A filter predicate. If the predicate returns false then the visual and all its
        /// children will be excluded from the results.
        /// </param>
        /// <returns>The visual at the specified point, topmost first.</returns>
        IVisual HitTestFirst(Point p, IVisual root, Func<IVisual, bool> filter);

        /// <summary>
        /// Informs the renderer that the z-ordering of a visual's children has changed.
        /// </summary>
        /// <param name="visual">The visual.</param>
        void RecalculateChildren(IVisual visual);

        /// <summary>
        /// Called when a resize notification is received by the control being rendered.
        /// </summary>
        /// <param name="size">The new size of the window.</param>
        void Resized(Size size);

        /// <summary>
        /// Called when a paint notification is received by the control being rendered.
        /// </summary>
        /// <param name="rect">The dirty rectangle.</param>
        void Paint(Rect rect);

        /// <summary>
        /// Starts the renderer.
        /// </summary>
        void Start();

        /// <summary>
        /// Stops the renderer.
        /// </summary>
        void Stop();
    }
}
