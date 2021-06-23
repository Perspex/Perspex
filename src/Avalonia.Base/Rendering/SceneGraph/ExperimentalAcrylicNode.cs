using System;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Avalonia.Platform;

namespace Avalonia.Rendering.SceneGraph
{
    /// <summary>
    /// A node in the scene graph which represents a rectangle draw.
    /// </summary>
    internal class ExperimentalAcrylicNode : DrawOperation
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RectangleNode"/> class.
        /// </summary>
        /// <param name="transform">The transform.</param>
        /// <param name="material"></param>        
        /// <param name="rect">The rectangle to draw.</param>
        public ExperimentalAcrylicNode(
            Matrix transform,
            IExperimentalAcrylicMaterial material,
            RoundedRect rect)
            : base(rect.Rect, transform)
        {
            Transform = transform;
            Material = material?.ToImmutable();
            Rect = rect;
        }

        /// <summary>
        /// Gets the transform with which the node will be drawn.
        /// </summary>
        public Matrix Transform { get; }

        public IExperimentalAcrylicMaterial Material { get; }        

        /// <summary>
        /// Gets the rectangle to draw.
        /// </summary>
        public RoundedRect Rect { get; }

        /// <summary>
        /// Determines if this draw operation equals another.
        /// </summary>
        /// <param name="transform">The transform of the other draw operation.</param>
        /// <param name="material">The fill of the other draw operation.</param>
        /// <param name="rect">The rectangle of the other draw operation.</param>        
        /// <returns>True if the draw operations are the same, otherwise false.</returns>
        /// <remarks>
        /// The properties of the other draw operation are passed in as arguments to prevent
        /// allocation of a not-yet-constructed draw operation object.
        /// </remarks>
        public bool Equals(Matrix transform, IExperimentalAcrylicMaterial material, RoundedRect rect)
        {
            return transform == Transform &&
                   Equals(material, Material) &&
                   rect.Equals(Rect);
        }

        /// <inheritdoc/>
        public override void Render(IDrawingContextImpl context)
        {
            context.Transform = Transform;

            if(context is IDrawingContextWithAcrylicLikeSupport idc)
            {
                idc.DrawRectangle(Material, Rect);
            }
            else
            {
                context.DrawRectangle(new ImmutableSolidColorBrush(Material.FallbackColor), null, Rect);
            }            
        }

        /// <inheritdoc/>
        public override bool HitTest(Point p)
        {
            // TODO: This doesn't respect CornerRadius yet.
            if (Transform.HasInverse)
            {
                p *= Transform.Invert();

                if (Material != null)
                {
                    var rect = Rect.Rect;
                    return rect.Contains(p);
                }
            }

            return false;
        }
    }
}
