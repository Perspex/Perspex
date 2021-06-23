using System;
using Avalonia.Media;
using Avalonia.Platform;

namespace Avalonia.Rendering.SceneGraph
{
    /// <summary>
    /// Base class for draw operations that have bounds.
    /// </summary>
    internal abstract class DrawOperation : IDrawOperation
    {
        public DrawOperation(Rect bounds, Matrix transform)
        {
            bounds = bounds.Normalize().TransformToAABB(transform);

            Bounds = new Rect(
                new Point(Math.Floor(bounds.X), Math.Floor(bounds.Y)),
                new Point(Math.Ceiling(bounds.Right), Math.Ceiling(bounds.Bottom)));
        }

        public Rect Bounds { get; }

        public abstract bool HitTest(Point p);

        public abstract void Render(IDrawingContextImpl context);

        public virtual void Dispose()
        {
        }
    }
}
