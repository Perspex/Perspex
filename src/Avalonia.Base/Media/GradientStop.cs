namespace Avalonia.Media
{
    /// <summary>
    /// Describes the location and color of a transition point in a gradient.
    /// </summary>
    public sealed class GradientStop : AvaloniaObject, IGradientStop
    {
        /// <summary>
        /// Describes the <see cref="Offset"/> property.
        /// </summary>
        public static readonly StyledProperty<double> OffsetProperty =
            AvaloniaProperty.Register<GradientStop, double>(nameof(Offset));

        /// <summary>
        /// Describes the <see cref="Color"/> property.
        /// </summary>
        public static readonly StyledProperty<Color> ColorProperty =
            AvaloniaProperty.Register<GradientStop, Color>(nameof(Color));

        /// <summary>
        /// Initializes a new instance of the <see cref="GradientStop"/> class.
        /// </summary>
        public GradientStop() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="GradientStop"/> class.
        /// </summary>
        /// <param name="color">The color</param>
        /// <param name="offset">The offset</param>
        public GradientStop(Color color, double offset)
        {
            Color = color;
            Offset = offset;
        }

        /// <inheritdoc/>
        public double Offset
        {
            get => GetValue(OffsetProperty);
            set => SetValue(OffsetProperty, value);
        }

        /// <inheritdoc/>
        public Color Color
        {
            get => GetValue(ColorProperty);
            set => SetValue(ColorProperty, value);
        }
    }
}
