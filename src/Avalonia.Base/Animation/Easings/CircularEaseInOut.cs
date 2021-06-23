using System;

namespace Avalonia.Animation.Easings
{
    /// <summary>
    /// Eases a <see cref="double"/> value 
    /// using a piecewise unit circle function.
    /// </summary>
    public class CircularEaseInOut : Easing
    {
        /// <inheritdoc/>
        public override double Ease(double progress)
        {
            double p = progress;
            if (p < 0.5d)
            {
                return 0.5d * (1d - Math.Sqrt(1d - 4d * p * p));
            }
            else
            {
                double t = 2d * p;
                return 0.5d * (Math.Sqrt((3d - t) * (t - 1d)) + 1d);
            }
        }
    }
}
