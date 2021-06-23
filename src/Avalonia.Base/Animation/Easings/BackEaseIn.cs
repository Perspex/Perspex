using System;

namespace Avalonia.Animation.Easings
{
    /// <summary>
    /// Eases in a <see cref="double"/> value 
    /// using a overshooting cubic function.
    /// </summary>
    public class BackEaseIn : Easing
    {
        /// <inheritdoc/>
        public override double Ease(double p)
        {
            return p * (p * p - Math.Sin(p * Math.PI)); 
        }
    }
}
