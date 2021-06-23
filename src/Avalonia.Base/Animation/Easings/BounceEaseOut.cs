using Avalonia.Animation.Utils;

namespace Avalonia.Animation.Easings
{
    /// <summary>
    /// Eases out a <see cref="double"/> value 
    /// using a simulated bounce function.
    /// </summary>
    public class BounceEaseOut : Easing
    {
        /// <inheritdoc/>
        public override double Ease(double progress)
        {
            return BounceEaseUtils.Bounce(progress);
        }
    }
}
