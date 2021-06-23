using System.Collections.Generic;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.VisualTree;

namespace Avalonia.Rendering.SceneGraph
{
    /// <summary>
    /// A node in the scene graph which represents a text draw.
    /// </summary>
    internal class TextNode : BrushDrawOperation
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TextNode"/> class.
        /// </summary>
        /// <param name="transform">The transform.</param>
        /// <param name="foreground">The foreground brush.</param>
        /// <param name="origin">The draw origin.</param>
        /// <param name="text">The text to draw.</param>
        /// <param name="childScenes">Child scenes for drawing visual brushes.</param>
        public TextNode(
            Matrix transform,
            IBrush foreground,
            Point origin,
            IFormattedTextImpl text,
            IDictionary<IVisual, Scene> childScenes = null)
            : base(text.Bounds.Translate(origin), transform)
        {
            Transform = transform;
            Foreground = foreground?.ToImmutable();
            Origin = origin;
            Text = text;
            ChildScenes = childScenes;
        }

        /// <summary>
        /// Gets the transform with which the node will be drawn.
        /// </summary>
        public Matrix Transform { get; }

        /// <summary>
        /// Gets the foreground brush.
        /// </summary>
        public IBrush Foreground { get; }

        /// <summary>
        /// Gets the draw origin.
        /// </summary>
        public Point Origin { get; }

        /// <summary>
        /// Gets the text to draw.
        /// </summary>
        public IFormattedTextImpl Text { get; }

        /// <inheritdoc/>
        public override IDictionary<IVisual, Scene> ChildScenes { get; }

        /// <inheritdoc/>
        public override void Render(IDrawingContextImpl context)
        {
            context.Transform = Transform;
            context.DrawText(Foreground, Origin, Text);
        }

        /// <summary>
        /// Determines if this draw operation equals another.
        /// </summary>
        /// <param name="transform">The transform of the other draw operation.</param>
        /// <param name="foreground">The foreground of the other draw operation.</param>
        /// <param name="origin">The draw origin of the other draw operation.</param>
        /// <param name="text">The text of the other draw operation.</param>
        /// <returns>True if the draw operations are the same, otherwise false.</returns>
        /// <remarks>
        /// The properties of the other draw operation are passed in as arguments to prevent
        /// allocation of a not-yet-constructed draw operation object.
        /// </remarks>
        internal bool Equals(Matrix transform, IBrush foreground, Point origin, IFormattedTextImpl text)
        {
            return transform == Transform &&
                Equals(foreground, Foreground) &&
                origin == Origin &&
                Equals(text, Text);
        }

        /// <inheritdoc/>
        public override bool HitTest(Point p) => Bounds.Contains(p);
    }
}
