using Avalonia.Platform;

namespace Avalonia.Rendering.SceneGraph
{
    /// <summary>
    /// A node in the scene graph which represents a clip push or pop.
    /// </summary>
    internal class ClipNode : IDrawOperation
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ClipNode"/> class that represents a
        /// clip push.
        /// </summary>
        /// <param name="clip">The clip to push.</param>
        public ClipNode(Rect clip)
        {
            Clip = clip;
        }
        
        /// <summary>
        /// Initializes a new instance of the <see cref="ClipNode"/> class that represents a
        /// clip push.
        /// </summary>
        /// <param name="clip">The clip to push.</param>
        public ClipNode(RoundedRect clip)
        {
            Clip = clip;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ClipNode"/> class that represents a
        /// clip pop.
        /// </summary>
        public ClipNode()
        {
        }

        /// <inheritdoc/>
        public Rect Bounds => Rect.Empty;

        /// <summary>
        /// Gets the clip to be pushed or null if the operation represents a pop.
        /// </summary>
        public RoundedRect? Clip { get; }

        /// <inheritdoc/>
        public bool HitTest(Point p) => false;

        /// <summary>
        /// Determines if this draw operation equals another.
        /// </summary>
        /// <param name="clip">The clip of the other draw operation.</param>
        /// <returns>True if the draw operations are the same, otherwise false.</returns>
        /// <remarks>
        /// The properties of the other draw operation are passed in as arguments to prevent
        /// allocation of a not-yet-constructed draw operation object.
        /// </remarks>
        public bool Equals(RoundedRect? clip) => Clip == clip;

        /// <inheritdoc/>
        public void Render(IDrawingContextImpl context)
        {
            if (Clip.HasValue)
            {
                context.PushClip(Clip.Value);
            }
            else
            {
                context.PopClip();
            }
        }

        public void Dispose()
        {
        }
    }
}
