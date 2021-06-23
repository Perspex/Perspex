using System;

namespace Avalonia.Animation.Easings
{
    /// <summary>
    /// Eases out a <see cref="double"/> value 
    /// using a overshooting cubic function.
    /// </summary>
    public class BackEaseOut : Easing
    {
        /// <inheritdoc/>
        public override double Ease(double progress)
        {
            double p = 1d - progress;
            return 1 - p * (p * p - Math.Sin(p * Math.PI));
        }
    }
}
