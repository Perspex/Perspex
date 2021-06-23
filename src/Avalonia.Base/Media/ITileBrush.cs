using Avalonia.Media.Imaging;

namespace Avalonia.Media
{  
    /// <summary>
    /// A brush which displays a repeating image.
    /// </summary>
    public interface ITileBrush : IBrush
    {
        /// <summary>
        /// Gets the horizontal alignment of a tile in the destination.
        /// </summary>
        AlignmentX AlignmentX { get; }

        /// <summary>
        /// Gets the horizontal alignment of a tile in the destination.
        /// </summary>
        AlignmentY AlignmentY { get; }

        /// <summary>
        /// Gets the rectangle on the destination in which to paint a tile.
        /// </summary>
        RelativeRect DestinationRect { get; }

        /// <summary>
        /// Gets the rectangle of the source image that will be displayed.
        /// </summary>
        RelativeRect SourceRect { get; }

        /// <summary>
        /// Gets a value indicating how the source rectangle will be stretched to fill the
        /// destination rect.
        /// </summary>
        Stretch Stretch { get; }

        /// <summary>
        /// Gets the brush's tile mode.
        /// </summary>
        TileMode TileMode { get; }

        /// <summary>
        /// Gets the bitmap interpolation mode.
        /// </summary>
        /// <value>
        /// The bitmap interpolation mode.
        /// </value>
        BitmapInterpolationMode BitmapInterpolationMode { get; }
    }
}
