#nullable enable

namespace Avalonia.Media
{
    /// <summary>
    /// Draws an image within a region defined by a <see cref="Rect"/>.
    /// </summary>
    public class ImageDrawing : Drawing
    {
        /// <summary>
        /// Defines the <see cref="ImageSource"/> property.
        /// </summary>
        public static readonly StyledProperty<IImage?> ImageSourceProperty =
            AvaloniaProperty.Register<ImageDrawing, IImage?>(nameof(ImageSource));

        /// <summary>
        /// Defines the <see cref="Rect"/> property.
        /// </summary>
        public static readonly StyledProperty<Rect> RectProperty =
            AvaloniaProperty.Register<ImageDrawing, Rect>(nameof(Rect));

        /// <summary>
        /// Gets or sets the source of the image.
        /// </summary>
        public IImage? ImageSource
        {
            get => GetValue(ImageSourceProperty);
            set => SetValue(ImageSourceProperty, value);
        }

        /// <summary>
        /// Gets or sets region in which the image is drawn.
        /// </summary>
        public Rect Rect
        {
            get => GetValue(RectProperty);
            set => SetValue(RectProperty, value);
        }

        public override void Draw(DrawingContext context)
        {
            var imageSource = ImageSource;
            var rect = Rect;

            if (imageSource is object && !rect.IsEmpty)
            {
                context.DrawImage(imageSource, rect);
            }
        }

        public override Rect GetBounds() => Rect;
    }
}
