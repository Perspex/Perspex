namespace Avalonia.Media.Immutable
{
    /// <summary>
    /// Describes the location and color of a transition point in a gradient.
    /// </summary>
    public class ImmutableGradientStop : IGradientStop
    {
        public ImmutableGradientStop(double offset, Color color)
        {
            Offset = offset;
            Color = color;
        }

        /// <inheritdoc/>
        public double Offset { get; }

        /// <inheritdoc/>
        public Color Color { get; }
    }
}
