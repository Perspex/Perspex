namespace Avalonia.Media
{
    /// <summary>
    /// Paints an area with a conic gradient.
    /// </summary>
    public interface IConicGradientBrush : IGradientBrush
    {
        /// <summary>
        /// Gets the center point for the gradient.
        /// </summary>
        RelativePoint Center { get; }

        /// <summary>
        /// Gets the starting angle for the gradient in degrees, measured from
        /// the point above the center point.
        /// </summary>
        double Angle { get; }
    }
}
