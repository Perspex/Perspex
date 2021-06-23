using System;
using Avalonia.Logging;
using Avalonia.Media;

namespace Avalonia.Animation.Animators
{
    /// <summary>
    /// Animator that handles <see cref="CornerRadius"/> properties.
    /// </summary>
    public class CornerRadiusAnimator : Animator<CornerRadius>
    {
        public override CornerRadius Interpolate(double progress, CornerRadius oldValue, CornerRadius newValue)
        {
            var deltaTL = newValue.TopLeft - oldValue.TopLeft;
            var deltaTR = newValue.TopRight - oldValue.TopRight;
            var deltaBR = newValue.BottomRight - oldValue.BottomRight;
            var deltaBL = newValue.BottomLeft - oldValue.BottomLeft;

            var nTL = progress * deltaTL + oldValue.TopLeft;
            var nTR = progress * deltaTR + oldValue.TopRight;
            var nBR = progress * deltaBR + oldValue.BottomRight;
            var nBL = progress * deltaBL + oldValue.BottomLeft;

            return new CornerRadius(nTL, nTR, nBR, nBL);
        }
    }
}