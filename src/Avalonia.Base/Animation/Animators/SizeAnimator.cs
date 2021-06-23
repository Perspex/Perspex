using System;
using Avalonia.Logging;
using Avalonia.Media;

namespace Avalonia.Animation.Animators
{
    /// <summary>
    /// Animator that handles <see cref="Size"/> properties.
    /// </summary>
    public class SizeAnimator : Animator<Size>
    {
        public override Size Interpolate(double progress, Size oldValue, Size newValue)
        {
            return ((newValue - oldValue) * progress) + oldValue;
        }
    }
}