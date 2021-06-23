namespace Avalonia.Animation.Easings
{
    /// <summary>
    /// Eases out a <see cref="double"/> value 
    /// using a quadratic function.
    /// </summary>
    public class QuadraticEaseOut : Easing
    {
        /// <inheritdoc/>
        public override double Ease(double progress)
        {
            return -(progress * (progress - 2d));
        }
    }
}
