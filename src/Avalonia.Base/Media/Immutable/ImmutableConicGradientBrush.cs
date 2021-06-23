using System.Collections.Generic;

namespace Avalonia.Media.Immutable
{
    /// <summary>
    /// A brush that draws with a sweep gradient.
    /// </summary>
    public class ImmutableConicGradientBrush : ImmutableGradientBrush, IConicGradientBrush
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ImmutableConicGradientBrush"/> class.
        /// </summary>
        /// <param name="gradientStops">The gradient stops.</param>
        /// <param name="opacity">The opacity of the brush.</param>
        /// <param name="spreadMethod">The spread method.</param>
        /// <param name="center">The center point for the gradient.</param>
        /// <param name="angle">The starting angle for the gradient.</param>
        public ImmutableConicGradientBrush(
            IReadOnlyList<ImmutableGradientStop> gradientStops,
            double opacity = 1,
            GradientSpreadMethod spreadMethod = GradientSpreadMethod.Pad,
            RelativePoint? center = null,
            double angle = 0)
            : base(gradientStops, opacity, spreadMethod)
        {
            Center = center ?? RelativePoint.Center;
            Angle = angle;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ImmutableConicGradientBrush"/> class.
        /// </summary>
        /// <param name="source">The brush from which this brush's properties should be copied.</param>
        public ImmutableConicGradientBrush(ConicGradientBrush source)
            : base(source)
        {
            Center = source.Center;
            Angle = source.Angle;
        }

        /// <inheritdoc/>
        public RelativePoint Center { get; }

        /// <inheritdoc/>
        public double Angle { get; }
    }
}
