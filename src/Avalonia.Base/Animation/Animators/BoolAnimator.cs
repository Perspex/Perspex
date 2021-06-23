namespace Avalonia.Animation.Animators
{
    /// <summary>
    /// Animator that handles <see cref="bool"/> properties.
    /// </summary>
    public class BoolAnimator : Animator<bool>
    {
        /// <inheritdocs/>
        public override bool Interpolate(double progress, bool oldValue, bool newValue)
        {
            if(progress >= 1d)
                return newValue;
            if(progress >= 0)
                return oldValue;
            return oldValue;
        }
    }
}
