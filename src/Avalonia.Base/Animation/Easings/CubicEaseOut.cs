namespace Avalonia.Animation.Easings
{
    /// <summary>
    /// Eases out a <see cref="double"/> value 
    /// using a cubic equation.
    /// </summary>
    public class CubicEaseOut : Easing
    {
        /// <inheritdoc/>
        public override double Ease(double progress)
        {
            double f = (progress - 1d);
            return f * f * f + 1d;
        }
    }
}
