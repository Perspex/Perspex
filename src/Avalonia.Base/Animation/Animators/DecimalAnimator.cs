namespace Avalonia.Animation.Animators
{
    /// <summary>
    /// Animator that handles <see cref="decimal"/> properties.
    /// </summary>
    public class DecimalAnimator : Animator<decimal>
    {
        /// <inheritdocs/>
        public override decimal Interpolate(double progress, decimal oldValue, decimal newValue)
        {
            return ((newValue - oldValue) * (decimal)progress) + oldValue;
        }
    }
}
