using System;
using System.Reactive.Linq;

using Avalonia.Animation.Animators;

namespace Avalonia.Animation
{
    /// <summary>
    /// Transition class that handles <see cref="AvaloniaProperty"/> with <see cref="double"/> types.
    /// </summary>  
    public class DoubleTransition : Transition<double>
    {
        private static readonly DoubleAnimator s_animator = new DoubleAnimator();

        /// <inheritdocs/>
        public override IObservable<double> DoTransition(IObservable<double> progress, double oldValue, double newValue)
        {
            return progress
                .Select(progress => s_animator.Interpolate(Easing.Ease(progress), oldValue, newValue));
        }
    }
}
