using System;
using Avalonia.Animation.Utils;

namespace Avalonia.Animation.Easings
{
    /// <summary>
    /// Eases a <see cref="double"/> value 
    /// using a piecewise damped sine function.
    /// </summary>
    public class ElasticEaseInOut : Easing
    {
        /// <inheritdoc/>
        public override double Ease(double progress)
        {
            double p = progress;

            if (p < 0.5d)
            {
                double t = 2d * p;
                return 0.5d * Math.Sin(13d * EasingUtils.HALFPI * t) * Math.Pow(2d, 10d * (t - 1d));
            }
            else
            {
                return 0.5d * (Math.Sin(-13d * EasingUtils.HALFPI * ((2d * p - 1d) + 1d)) * Math.Pow(2d, -10d * (2d * p - 1d)) + 2d);
            }
        }
    }
}
