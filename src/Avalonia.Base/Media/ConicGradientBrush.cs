using Avalonia.Media.Immutable;

namespace Avalonia.Media
{
    /// <summary>
    /// Paints an area with a swept circular gradient.
    /// </summary>
    public sealed class ConicGradientBrush : GradientBrush, IConicGradientBrush
    {
        /// <summary>
        /// Defines the <see cref="Center"/> property.
        /// </summary>
        public static readonly StyledProperty<RelativePoint> CenterProperty =
            AvaloniaProperty.Register<RadialGradientBrush, RelativePoint>(
                nameof(Center),
                RelativePoint.Center);

        /// <summary>
        /// Defines the <see cref="Angle"/> property.
        /// </summary>
        public static readonly StyledProperty<double> AngleProperty =
            AvaloniaProperty.Register<RadialGradientBrush, double>(
                nameof(Angle),
                0);
        
        static ConicGradientBrush()
        {
            AffectsRender<ConicGradientBrush>(CenterProperty, AngleProperty);
        }

        /// <summary>
        /// Gets or sets the center point of the gradient.
        /// </summary>
        public RelativePoint Center
        {
            get { return GetValue(CenterProperty); }
            set { SetValue(CenterProperty, value); }
        }

        /// <summary>
        /// Gets or sets the angle of the start and end of the sweep, measured from above the center point.
        /// </summary>
        public double Angle
        {
            get { return GetValue(AngleProperty); }
            set { SetValue(AngleProperty, value); }
        }

        /// <inheritdoc/>
        public override IBrush ToImmutable()
        {
            return new ImmutableConicGradientBrush(this);
        }
    }
}
