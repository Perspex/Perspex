namespace Avalonia.Media
{
    /// <summary>
    /// Paints an area with a radial gradient.
    /// </summary>
    public interface IRadialGradientBrush : IGradientBrush
    {
        /// <summary>
        /// Gets the start point for the gradient.
        /// </summary>
        RelativePoint Center { get; }

        /// <summary>
        /// Gets the location of the two-dimensional focal point that defines the beginning of the
        /// gradient.
        /// </summary>
        RelativePoint GradientOrigin { get; }

        /// <summary>
        /// Gets the horizontal and vertical radius of the outermost circle of the radial gradient.
        /// </summary>
        double Radius { get; }
    }
}