namespace Avalonia.Animation.Easings
{
    /// <summary>
    /// Eases in a <see cref="double"/> value 
    /// using a quadratic function.
    /// </summary>
    public class QuadraticEaseIn : Easing
    {
        /// <inheritdoc/>
        public override double Ease(double progress)
        {
            return progress * progress;
        }
    }
}
