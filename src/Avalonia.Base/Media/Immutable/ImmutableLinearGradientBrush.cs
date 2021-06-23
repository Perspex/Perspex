using System.Collections.Generic;

namespace Avalonia.Media.Immutable
{
    /// <summary>
    /// A brush that draws with a linear gradient.
    /// </summary>
    public class ImmutableLinearGradientBrush : ImmutableGradientBrush, ILinearGradientBrush
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ImmutableLinearGradientBrush"/> class.
        /// </summary>
        /// <param name="gradientStops">The gradient stops.</param>
        /// <param name="opacity">The opacity of the brush.</param>
        /// <param name="spreadMethod">The spread method.</param>
        /// <param name="startPoint">The start point for the gradient.</param>
        /// <param name="endPoint">The end point for the gradient.</param>
        public ImmutableLinearGradientBrush(
            IReadOnlyList<ImmutableGradientStop> gradientStops,
            double opacity = 1,
            GradientSpreadMethod spreadMethod = GradientSpreadMethod.Pad,
            RelativePoint? startPoint = null,
            RelativePoint? endPoint = null)
            : base(gradientStops, opacity, spreadMethod)
        {
            StartPoint = startPoint ?? RelativePoint.TopLeft;
            EndPoint = endPoint ?? RelativePoint.BottomRight;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ImmutableLinearGradientBrush"/> class.
        /// </summary>
        /// <param name="source">The brush from which this brush's properties should be copied.</param>
        public ImmutableLinearGradientBrush(LinearGradientBrush source)
            : base(source)
        {
            StartPoint = source.StartPoint;
            EndPoint = source.EndPoint;
        }

        /// <inheritdoc/>
        public RelativePoint StartPoint { get; }

        /// <inheritdoc/>
        public RelativePoint EndPoint { get; }
    }
}
