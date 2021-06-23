using Avalonia.Media.Immutable;

namespace Avalonia.Media
{
    /// <summary>
    /// Paints an area with a radial gradient.
    /// </summary>
    public sealed class RadialGradientBrush : GradientBrush, IRadialGradientBrush
    {
        /// <summary>
        /// Defines the <see cref="Center"/> property.
        /// </summary>
        public static readonly StyledProperty<RelativePoint> CenterProperty =
            AvaloniaProperty.Register<RadialGradientBrush, RelativePoint>(
                nameof(Center),
                RelativePoint.Center);

        /// <summary>
        /// Defines the <see cref="GradientOrigin"/> property.
        /// </summary>
        public static readonly StyledProperty<RelativePoint> GradientOriginProperty =
            AvaloniaProperty.Register<RadialGradientBrush, RelativePoint>(
                nameof(GradientOrigin), 
                RelativePoint.Center);

        /// <summary>
        /// Defines the <see cref="Radius"/> property.
        /// </summary>
        public static readonly StyledProperty<double> RadiusProperty =
            AvaloniaProperty.Register<RadialGradientBrush, double>(
                nameof(Radius),
                0.5);
        
        static RadialGradientBrush()
        {
            AffectsRender<RadialGradientBrush>(CenterProperty, GradientOriginProperty, RadiusProperty);
        }

        /// <summary>
        /// Gets or sets the start point for the gradient.
        /// </summary>
        public RelativePoint Center
        {
            get { return GetValue(CenterProperty); }
            set { SetValue(CenterProperty, value); }
        }

        /// <summary>
        /// Gets or sets the location of the two-dimensional focal point that defines the beginning
        /// of the gradient.
        /// </summary>
        public RelativePoint GradientOrigin
        {
            get { return GetValue(GradientOriginProperty); }
            set { SetValue(GradientOriginProperty, value); }
        }

        /// <summary>
        /// Gets or sets the horizontal and vertical radius of the outermost circle of the radial
        /// gradient.
        /// </summary>
        // TODO: This appears to always be relative so should use a RelativeSize struct or something.
        public double Radius
        {
            get { return GetValue(RadiusProperty); }
            set { SetValue(RadiusProperty, value); }
        }

        /// <inheritdoc/>
        public override IBrush ToImmutable()
        {
            return new ImmutableRadialGradientBrush(this);
        }
    }
}
