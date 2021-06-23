using System.Collections.Generic;

namespace Avalonia.Media.Immutable
{
    /// <summary>
    /// A brush that draws with a radial gradient.
    /// </summary>
    public class ImmutableRadialGradientBrush : ImmutableGradientBrush, IRadialGradientBrush
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ImmutableRadialGradientBrush"/> class.
        /// </summary>
        /// <param name="gradientStops">The gradient stops.</param>
        /// <param name="opacity">The opacity of the brush.</param>
        /// <param name="spreadMethod">The spread method.</param>
        /// <param name="center">The start point for the gradient.</param>
        /// <param name="gradientOrigin">
        /// The location of the two-dimensional focal point that defines the beginning of the gradient.
        /// </param>
        /// <param name="radius">
        /// The horizontal and vertical radius of the outermost circle of the radial gradient.
        /// </param>
        public ImmutableRadialGradientBrush(
            IReadOnlyList<ImmutableGradientStop> gradientStops,
            double opacity = 1,
            GradientSpreadMethod spreadMethod = GradientSpreadMethod.Pad,
            RelativePoint? center = null,
            RelativePoint? gradientOrigin = null,
            double radius = 0.5)
            : base(gradientStops, opacity, spreadMethod)
        {
            Center = center ?? RelativePoint.Center;
            GradientOrigin = gradientOrigin ?? RelativePoint.Center;
            Radius = radius;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ImmutableRadialGradientBrush"/> class.
        /// </summary>
        /// <param name="source">The brush from which this brush's properties should be copied.</param>
        public ImmutableRadialGradientBrush(RadialGradientBrush source)
            : base(source)
        {
            Center = source.Center;
            GradientOrigin = source.GradientOrigin;
            Radius = source.Radius;
        }

        /// <inheritdoc/>
        public RelativePoint Center { get; }

        /// <inheritdoc/>
        public RelativePoint GradientOrigin { get; }

        /// <inheritdoc/>
        public double Radius { get; }
    }
}
