namespace Avalonia.Animation.Easings
{
    /// <summary>
    /// Eases a <see cref="double"/> value 
    /// using a piece-wise quartic equation.
    /// </summary>
    public class QuarticEaseInOut : Easing
    {
        /// <inheritdoc/>
        public override double Ease(double progress)
        {
            double p = progress;

            if (p < 0.5d)
            {
                double p2 = p * p;
                return 8d * p2 * p2;
            }
            else
            {
                double f = p - 1d;
                double f2 = f * f;
                return -8d * f2 * f2 + 1d;
            }
        }
    }
}
