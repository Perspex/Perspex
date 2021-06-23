using Avalonia.Platform;
using Avalonia.Media.Imaging;

namespace Avalonia.Rendering.SceneGraph
{
    /// <summary>
    /// A node in the scene graph which represents an bitmap blending mode push or pop.
    /// </summary>
    internal class BitmapBlendModeNode : IDrawOperation
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BitmapBlendModeNode"/> class that represents an
        /// <see cref="BitmapBlendingMode"/> push.
        /// </summary>
        /// <param name="bitmapBlend">The <see cref="BitmapBlendingMode"/> to push.</param>
        public BitmapBlendModeNode(BitmapBlendingMode bitmapBlend)
        {
            BlendingMode = bitmapBlend;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BitmapBlendNode"/> class that represents an
        /// <see cref="BitmapBlendingMode"/> pop.
        /// </summary>
        public BitmapBlendModeNode()
        {
        }

        /// <inheritdoc/>
        public Rect Bounds => Rect.Empty;

        /// <summary>
        /// Gets the BitmapBlend to be pushed or null if the operation represents a pop.
        /// </summary>
        public BitmapBlendingMode? BlendingMode { get; }

        /// <inheritdoc/>
        public bool HitTest(Point p) => false;

        /// <summary>
        /// Determines if this draw operation equals another.
        /// </summary>
        /// <param name="opacity">The opacity of the other draw operation.</param>
        /// <returns>True if the draw operations are the same, otherwise false.</returns>
        /// <remarks>
        /// The properties of the other draw operation are passed in as arguments to prevent
        /// allocation of a not-yet-constructed draw operation object.
        /// </remarks>
        public bool Equals(BitmapBlendingMode? blendingMode) => BlendingMode == blendingMode;

        /// <inheritdoc/>
        public void Render(IDrawingContextImpl context)
        {
            if (BlendingMode.HasValue)
            {
                context.PushBitmapBlendMode(BlendingMode.Value);
            }
            else
            {
                context.PopBitmapBlendMode();
            }
        }

        public void Dispose()
        {
        }
    }
}
