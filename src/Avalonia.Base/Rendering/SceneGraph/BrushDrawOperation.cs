using System.Collections.Generic;
using Avalonia.Media;
using Avalonia.VisualTree;

namespace Avalonia.Rendering.SceneGraph
{
    /// <summary>
    /// Base class for draw operations that can use a brush.
    /// </summary>
    internal abstract class BrushDrawOperation : DrawOperation
    {
        public BrushDrawOperation(Rect bounds, Matrix transform)
            : base(bounds, transform)
        {
        }

        /// <summary>
        /// Gets a collection of child scenes that are needed to draw visual brushes.
        /// </summary>
        public abstract IDictionary<IVisual, Scene> ChildScenes { get; }
    }
}
