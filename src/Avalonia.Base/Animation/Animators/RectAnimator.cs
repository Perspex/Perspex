using System;
using Avalonia.Logging;
using Avalonia.Media;

namespace Avalonia.Animation.Animators
{
    /// <summary>
    /// Animator that handles <see cref="Rect"/> properties.
    /// </summary>
    public class RectAnimator : Animator<Rect>
    {
        public override Rect Interpolate(double progress, Rect oldValue, Rect newValue)
        {
            var deltaPos = newValue.Position - oldValue.Position;
            var deltaSize = newValue.Size - oldValue.Size;

            var newPos = (deltaPos * progress) + oldValue.Position;
            var newSize = (deltaSize * progress) + oldValue.Size;

            return new Rect(newPos, newSize);
        }
    }
}