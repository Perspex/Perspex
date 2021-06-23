using System;
using Avalonia.Logging;
using Avalonia.Media;

namespace Avalonia.Animation.Animators
{
    /// <summary>
    /// Animator that handles <see cref="Point"/> properties.
    /// </summary>
    public class PointAnimator : Animator<Point>
    {
        public override Point Interpolate(double progress, Point oldValue, Point newValue)
        { 
            return ((newValue - oldValue) * progress) + oldValue;
        }
    }
}