using System.Collections.Generic;

using Avalonia.Media;
using Avalonia.Media.Immutable;
using Avalonia.Platform;
using Avalonia.VisualTree;

namespace Avalonia.Rendering.SceneGraph
{
    /// <summary>
    /// A node in the scene graph which represents a glyph run draw.
    /// </summary>
    internal class GlyphRunNode : BrushDrawOperation
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GlyphRunNode"/> class.
        /// </summary>
        /// <param name="transform">The transform.</param>
        /// <param name="foreground">The foreground brush.</param>
        /// <param name="glyphRun">The glyph run to draw.</param>
        /// <param name="childScenes">Child scenes for drawing visual brushes.</param>
        public GlyphRunNode(
            Matrix transform,
            IBrush foreground,
            GlyphRun glyphRun,
            IDictionary<IVisual, Scene> childScenes = null)
            : base(new Rect(glyphRun.Size), transform)
        {
            Transform = transform;
            Foreground = foreground?.ToImmutable();
            GlyphRun = glyphRun;
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
        /// Gets the glyph run to draw.
        /// </summary>
        public GlyphRun GlyphRun { get; }

        /// <inheritdoc/>
        public override IDictionary<IVisual, Scene> ChildScenes { get; }

        /// <inheritdoc/>
        public override void Render(IDrawingContextImpl context)
        {
            context.Transform = Transform;
            context.DrawGlyphRun(Foreground, GlyphRun);
        }

        /// <summary>
        /// Determines if this draw operation equals another.
        /// </summary>
        /// <param name="transform">The transform of the other draw operation.</param>
        /// <param name="foreground">The foreground of the other draw operation.</param>
        /// <param name="glyphRun">The glyph run of the other draw operation.</param>
        /// <returns>True if the draw operations are the same, otherwise false.</returns>
        /// <remarks>
        /// The properties of the other draw operation are passed in as arguments to prevent
        /// allocation of a not-yet-constructed draw operation object.
        /// </remarks>
        internal bool Equals(Matrix transform, IBrush foreground, GlyphRun glyphRun)
        {
            return transform == Transform &&
                   Equals(foreground, Foreground) &&
                   Equals(glyphRun, GlyphRun);
        }

        /// <inheritdoc/>
        public override bool HitTest(Point p) => Bounds.Contains(p);
    }
}
