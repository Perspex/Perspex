namespace Avalonia.Media
{
    /// <summary>
    /// Fills an area with a solid color.
    /// </summary>
#if !BUILDTASK
    public
#endif
    interface ISolidColorBrush : IBrush
    {
        /// <summary>
        /// Gets the color of the brush.
        /// </summary>
        Color Color { get; }
    }
}
