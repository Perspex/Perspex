using System.Collections.Generic;

namespace Avalonia.Media
{
    /// <summary>
    /// A brush that draws with a gradient.
    /// </summary>
    public interface IGradientBrush : IBrush
    {
        /// <summary>
        /// Gets the brush's gradient stops.
        /// </summary>
        IReadOnlyList<IGradientStop> GradientStops { get; }

        /// <summary>
        /// Gets the brush's spread method that defines how to draw a gradient that doesn't fill
        /// the bounds of the destination control.
        /// </summary>
        GradientSpreadMethod SpreadMethod { get; }
    }
}
