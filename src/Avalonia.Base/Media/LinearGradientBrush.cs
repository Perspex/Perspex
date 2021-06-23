using Avalonia.Media.Immutable;

namespace Avalonia.Media
{
    /// <summary>
    /// A brush that draws with a linear gradient.
    /// </summary>
    public sealed class LinearGradientBrush : GradientBrush, ILinearGradientBrush
    {
        /// <summary>
        /// Defines the <see cref="StartPoint"/> property.
        /// </summary>
        public static readonly StyledProperty<RelativePoint> StartPointProperty =
            AvaloniaProperty.Register<LinearGradientBrush, RelativePoint>(
                nameof(StartPoint),
                RelativePoint.TopLeft);

        /// <summary>
        /// Defines the <see cref="EndPoint"/> property.
        /// </summary>
        public static readonly StyledProperty<RelativePoint> EndPointProperty =
            AvaloniaProperty.Register<LinearGradientBrush, RelativePoint>(
                nameof(EndPoint), 
                RelativePoint.BottomRight);

        static LinearGradientBrush()
        {
            AffectsRender<LinearGradientBrush>(StartPointProperty, EndPointProperty);
        }

        /// <summary>
        /// Gets or sets the start point for the gradient.
        /// </summary>
        public RelativePoint StartPoint
        {
            get { return GetValue(StartPointProperty); }
            set { SetValue(StartPointProperty, value); }
        }

        /// <summary>
        /// Gets or sets the end point for the gradient.
        /// </summary>
        public RelativePoint EndPoint
        {
            get { return GetValue(EndPointProperty); }
            set { SetValue(EndPointProperty, value); }
        }

        /// <inheritdoc/>
        public override IBrush ToImmutable()
        {
            return new ImmutableLinearGradientBrush(this);
        }
    }
}
