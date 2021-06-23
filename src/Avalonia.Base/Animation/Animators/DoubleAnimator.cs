namespace Avalonia.Animation.Animators
{
    /// <summary>
    /// Animator that handles <see cref="double"/> properties.
    /// </summary>
    public class DoubleAnimator : Animator<double>
    {
        /// <inheritdocs/>
        public override double Interpolate(double progress, double oldValue, double newValue)
        {
            return ((newValue - oldValue) * progress) + oldValue;
        }
    }
}
