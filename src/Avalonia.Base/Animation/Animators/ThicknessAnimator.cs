using System;
using Avalonia.Logging;
using Avalonia.Media;

namespace Avalonia.Animation.Animators
{
    /// <summary>
    /// Animator that handles <see cref="Thickness"/> properties.
    /// </summary>
    public class ThicknessAnimator : Animator<Thickness>
    {
        public override Thickness Interpolate(double progress, Thickness oldValue, Thickness newValue)
        {
            return ((newValue - oldValue) * progress) + oldValue;
        }
    }
}