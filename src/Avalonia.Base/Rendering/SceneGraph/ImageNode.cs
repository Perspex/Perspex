using Avalonia.Platform;
using Avalonia.Utilities;
using Avalonia.Media.Imaging;

namespace Avalonia.Rendering.SceneGraph
{
    /// <summary>
    /// A node in the scene graph which represents an image draw.
    /// </summary>
    internal class ImageNode : DrawOperation
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ImageNode"/> class.
        /// </summary>
        /// <param name="transform">The transform.</param>
        /// <param name="source">The image to draw.</param>
        /// <param name="opacity">The draw opacity.</param>
        /// <param name="sourceRect">The source rect.</param>
        /// <param name="destRect">The destination rect.</param>
        /// <param name="bitmapInterpolationMode">The bitmap interpolation mode.</param>
        public ImageNode(Matrix transform, IRef<IBitmapImpl> source, double opacity, Rect sourceRect, Rect destRect, BitmapInterpolationMode bitmapInterpolationMode)
            : base(destRect, transform)
        {
            Transform = transform;
            Source = source.Clone();
            Opacity = opacity;
            SourceRect = sourceRect;
            DestRect = destRect;
            BitmapInterpolationMode = bitmapInterpolationMode;
            SourceVersion = Source.Item.Version;
        }        

        /// <summary>
        /// Gets the transform with which the node will be drawn.
        /// </summary>
        public Matrix Transform { get; }

        /// <summary>
        /// Gets the image to draw.
        /// </summary>
        public IRef<IBitmapImpl> Source { get; }

        /// <summary>
        /// Source bitmap Version
        /// </summary>
        public int SourceVersion { get; }

        /// <summary>
        /// Gets the draw opacity.
        /// </summary>
        public double Opacity { get; }

        /// <summary>
        /// Gets the source rect.
        /// </summary>
        public Rect SourceRect { get; }

        /// <summary>
        /// Gets the destination rect.
        /// </summary>
        public Rect DestRect { get; }

        /// <summary>
        /// Gets the bitmap interpolation mode.
        /// </summary>
        /// <value>
        /// The scaling mode.
        /// </value>
        public BitmapInterpolationMode BitmapInterpolationMode { get; }
        
        /// <summary>
        /// The bitmap blending mode.
        /// </summary>
        /// <value>
        /// The blending mode.
        /// </value>
        public BitmapBlendingMode BitmapBlendingMode { get; }

        /// <summary>
        /// Determines if this draw operation equals another.
        /// </summary>
        /// <param name="transform">The transform of the other draw operation.</param>
        /// <param name="source">The image of the other draw operation.</param>
        /// <param name="opacity">The opacity of the other draw operation.</param>
        /// <param name="sourceRect">The source rect of the other draw operation.</param>
        /// <param name="destRect">The dest rect of the other draw operation.</param>
        /// <param name="bitmapInterpolationMode">The bitmap interpolation mode.</param>
        /// <returns>True if the draw operations are the same, otherwise false.</returns>
        /// <remarks>
        /// The properties of the other draw operation are passed in as arguments to prevent
        /// allocation of a not-yet-constructed draw operation object.
        /// </remarks>
        public bool Equals(Matrix transform, IRef<IBitmapImpl> source, double opacity, Rect sourceRect, Rect destRect, BitmapInterpolationMode bitmapInterpolationMode)
        {
            return transform == Transform &&
                   Equals(source.Item, Source.Item) &&
                   source.Item.Version == SourceVersion &&
                   opacity == Opacity &&
                   sourceRect == SourceRect &&
                   destRect == DestRect &&
                   bitmapInterpolationMode == BitmapInterpolationMode;
        }

        /// <inheritdoc/>
        public override void Render(IDrawingContextImpl context)
        {
            context.Transform = Transform;
            context.DrawBitmap(Source, Opacity, SourceRect, DestRect, BitmapInterpolationMode);
        }

        /// <inheritdoc/>
        public override bool HitTest(Point p) => Bounds.Contains(p);

        public override void Dispose()
        {
            Source?.Dispose();
        }
    }
}
