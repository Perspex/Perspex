using System;

namespace Avalonia.Animation.Easings
{
    /// <summary>
    /// Eases out a <see cref="double"/> value 
    /// using a exponential function.
    /// </summary>
    public class ExponentialEaseOut : Easing
    {
        /// <inheritdoc/>
        public override double Ease(double progress)
        {
            double p = progress;
            return (p == 1.0d) ? p : 1d - Math.Pow(2d, -10d * p);
        }
    }
}
