using Avalonia.Media.Imaging;

namespace Avalonia.Media
{
    /// <summary>
    /// Paints an area with an <see cref="IBitmap"/>.
    /// </summary>
    public interface IImageBrush : ITileBrush
    {
        /// <summary>
        /// Gets the image to draw.
        /// </summary>
        IBitmap Source { get; }
    }
}