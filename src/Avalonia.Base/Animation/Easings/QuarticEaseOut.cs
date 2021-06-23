namespace Avalonia.Animation.Easings
{
    /// <summary>
    /// Eases out a <see cref="double"/> value 
    /// using a quartic equation.
    /// </summary>
    public class QuarticEaseOut : Easing
    {
        /// <inheritdoc/>
        public override double Ease(double progress)
        {
            double f = progress - 1d;
            double f2 = f * f;
            return -f2 * f2 + 1d;
        }
    }
}
