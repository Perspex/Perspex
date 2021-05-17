using System;
using System.Reactive.Linq;

using Avalonia.Animation.Animators;

namespace Avalonia.Animation
{
    /// <summary>
    /// Transition class that handles <see cref="AvaloniaProperty"/> with <see cref="float"/> types.
    /// </summary>  
    public class FloatTransition : Transition<float>
    {
        private static readonly FloatAnimator s_animator = new FloatAnimator();

        /// <inheritdocs/>
        public override IObservable<float> DoTransition(IObservable<double> progress, float oldValue, float newValue)
        {
            return progress
                .Select(progress => s_animator.Interpolate(Easing.Ease(progress), oldValue, newValue));
        }
    }
}
