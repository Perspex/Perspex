using Avalonia.Media;

namespace Avalonia.Animation.Animators
{
    public class BoxShadowAnimator : Animator<BoxShadow>
    {
        static ColorAnimator s_colorAnimator = new ColorAnimator();
        static DoubleAnimator s_doubleAnimator = new DoubleAnimator();
        static BoolAnimator s_boolAnimator = new BoolAnimator();
        public override BoxShadow Interpolate(double progress, BoxShadow oldValue, BoxShadow newValue)
        {
            return new BoxShadow
            {
                OffsetX = s_doubleAnimator.Interpolate(progress, oldValue.OffsetX, newValue.OffsetX),
                OffsetY = s_doubleAnimator.Interpolate(progress, oldValue.OffsetY, newValue.OffsetY),
                Blur = s_doubleAnimator.Interpolate(progress, oldValue.Blur, newValue.Blur),
                Spread = s_doubleAnimator.Interpolate(progress, oldValue.Spread, newValue.Spread),
                Color = s_colorAnimator.Interpolate(progress, oldValue.Color, newValue.Color),
                IsInset = s_boolAnimator.Interpolate(progress, oldValue.IsInset, newValue.IsInset)
            };
        }
    }
}
