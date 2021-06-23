namespace Avalonia.Media
{
    /// <summary>
    /// Describes the location and color of a transition point in a gradient.
    /// </summary>
    public interface IGradientStop
    {
        /// <summary>
        /// Gets the gradient stop color.
        /// </summary>
        Color Color { get; }

        /// <summary>
        /// Gets the gradient stop offset.
        /// </summary>
        double Offset { get; }
    }
}
