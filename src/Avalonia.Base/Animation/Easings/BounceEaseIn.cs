using Avalonia.Animation.Utils;

namespace Avalonia.Animation.Easings
{
    /// <summary>
    /// Eases in a <see cref="double"/> value 
    /// using a simulated bounce function.
    /// </summary>
    public class BounceEaseIn : Easing
    {
        /// <inheritdoc/>
        public override double Ease(double progress)
        {
            return 1 - BounceEaseUtils.Bounce(1 - progress);
        }
    }
}
