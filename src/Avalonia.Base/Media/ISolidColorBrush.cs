namespace Avalonia.Media
{
    /// <summary>
    /// Fills an area with a solid color.
    /// </summary>
    public interface ISolidColorBrush : IBrush
    {
        /// <summary>
        /// Gets the color of the brush.
        /// </summary>
        Color Color { get; }
    }
}