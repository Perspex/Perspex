using System;
using Avalonia.Animation.Utils;

namespace Avalonia.Animation.Easings
{
    /// <summary>
    /// Eases out a <see cref="double"/> value 
    /// using the quarter-wave of sine function
    /// with shifted phase.
    /// </summary>
    public class SineEaseOut : Easing
    {
        /// <inheritdoc/>
        public override double Ease(double progress)
        {
            return Math.Sin(progress * EasingUtils.HALFPI);
        }
    }
}
