using Avalonia.Media.Imaging;
using Avalonia.Media.Immutable;

namespace Avalonia.Media
{
    /// <summary>
    /// Paints an area with an <see cref="IBitmap"/>.
    /// </summary>
    public class ImageBrush : TileBrush, IImageBrush
    {
        /// <summary>
        /// Defines the <see cref="Visual"/> property.
        /// </summary>
        public static readonly StyledProperty<IBitmap> SourceProperty =
            AvaloniaProperty.Register<ImageBrush, IBitmap>(nameof(Source));

        static ImageBrush()
        {
            AffectsRender<ImageBrush>(SourceProperty);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ImageBrush"/> class.
        /// </summary>
        public ImageBrush()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ImageBrush"/> class.
        /// </summary>
        /// <param name="source">The image to draw.</param>
        public ImageBrush(IBitmap source)
        {
            Source = source;
        }

        /// <summary>
        /// Gets or sets the image to draw.
        /// </summary>
        public IBitmap Source
        {
            get { return GetValue(SourceProperty); }
            set { SetValue(SourceProperty, value); }
        }

        /// <inheritdoc/>
        public override IBrush ToImmutable()
        {
            return new ImmutableImageBrush(this);
        }
    }
}
