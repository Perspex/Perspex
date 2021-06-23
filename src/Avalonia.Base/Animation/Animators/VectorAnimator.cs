using System;
using Avalonia.Logging;
using Avalonia.Media;

namespace Avalonia.Animation.Animators
{
    /// <summary>
    /// Animator that handles <see cref="Vector"/> properties.
    /// </summary>
    public class VectorAnimator : Animator<Vector>
    {
        public override Vector Interpolate(double progress, Vector oldValue, Vector newValue)
        {
            return ((newValue - oldValue) * progress) + oldValue;
        }
    }
}