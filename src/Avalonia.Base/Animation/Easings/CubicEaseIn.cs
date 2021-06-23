namespace Avalonia.Animation.Easings
{
    /// <summary>
    /// Eases in a <see cref="double"/> value 
    /// using a cubic equation.
    /// </summary>
    public class CubicEaseIn : Easing
    {
        /// <inheritdoc/>
        public override double Ease(double progress)
        {
            return progress * progress * progress;
        }
    }
}
