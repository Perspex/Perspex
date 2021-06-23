using Avalonia.Media.Imaging;

namespace Avalonia.Media
{
    /// <summary>
    /// Represents a raster or vector image.
    /// </summary>
    public interface IImage
    {
        /// <summary>
        /// Gets the size of the image, in device independent pixels.
        /// </summary>
        Size Size { get; }

        /// <summary>
        /// Draws the image to a <see cref="DrawingContext"/>.
        /// </summary>
        /// <param name="context">The drawing context.</param>
        /// <param name="sourceRect">The rect in the image to draw.</param>
        /// <param name="destRect">The rect in the output to draw to.</param>
        /// <param name="bitmapInterpolationMode">The bitmap interpolation mode.</param>
        void Draw(
            DrawingContext context,
            Rect sourceRect,
            Rect destRect,
            BitmapInterpolationMode bitmapInterpolationMode);
    }
}
