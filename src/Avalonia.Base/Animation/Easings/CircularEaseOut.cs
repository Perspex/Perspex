using System;

namespace Avalonia.Animation.Easings
{
    /// <summary>
    /// Eases out a <see cref="double"/> value 
    /// using the shifted second quadrant of
    /// the unit circle.
    /// </summary>
    public class CircularEaseOut : Easing
    {
        /// <inheritdoc/>
        public override double Ease(double progress)
        {
            double p = progress;
            return Math.Sqrt((2d - p) * p);
         }
    }
}
