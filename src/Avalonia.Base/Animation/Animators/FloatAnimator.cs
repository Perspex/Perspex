namespace Avalonia.Animation.Animators
{
    /// <summary>
    /// Animator that handles <see cref="float"/> properties.
    /// </summary>
    public class FloatAnimator : Animator<float>
    {
        /// <inheritdocs/>
        public override float Interpolate(double progress, float oldValue, float newValue)
        {
            return (float)(((newValue - oldValue) * progress) + oldValue);
        }
    }
}
