namespace Avalonia.Animation.Animators
{
    /// <summary>
    /// Animator that handles <see cref="RelativePoint"/> properties.
    /// </summary>
    public class RelativePointAnimator : Animator<RelativePoint>
    {
        private static readonly PointAnimator s_pointAnimator = new PointAnimator();

        public override RelativePoint Interpolate(double progress, RelativePoint oldValue, RelativePoint newValue)
        {
            if (oldValue.Unit != newValue.Unit)
            {
                return progress >= 0.5 ? newValue : oldValue;
            }

            return new RelativePoint(s_pointAnimator.Interpolate(progress, oldValue.Point, newValue.Point), oldValue.Unit);
        }
    }
}
