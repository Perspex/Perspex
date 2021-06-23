using System;

namespace Avalonia.Animation.Easings
{
    /// <summary>
    /// Eases in a <see cref="double"/> value 
    /// using a exponential function.
    /// </summary>
    public class ExponentialEaseIn : Easing
    {
        /// <inheritdoc/>
        public override double Ease(double progress)
        {
            double p = progress;
            return (p == 0.0d) ? p : Math.Pow(2d, 10d * (p - 1d));
        }
    }
}
