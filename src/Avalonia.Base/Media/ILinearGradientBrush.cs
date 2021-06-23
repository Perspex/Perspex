namespace Avalonia.Media
{
    /// <summary>
    /// A brush that draws with a linear gradient.
    /// </summary>
    public interface ILinearGradientBrush : IGradientBrush
    {
        /// <summary>
        /// Gets or sets the start point for the gradient.
        /// </summary>
        RelativePoint StartPoint { get; }

        /// <summary>
        /// Gets or sets the end point for the gradient.
        /// </summary>
        RelativePoint EndPoint { get; }
    }
}