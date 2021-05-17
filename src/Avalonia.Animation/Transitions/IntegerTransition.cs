using System;
using System.Reactive.Linq;

using Avalonia.Animation.Animators;

namespace Avalonia.Animation
{
    /// <summary>
    /// Transition class that handles <see cref="AvaloniaProperty"/> with <see cref="int"/> types.
    /// </summary>  
    public class IntegerTransition : Transition<int>
    {
        private static readonly Int32Animator s_animator = new Int32Animator();

        /// <inheritdocs/>
        public override IObservable<int> DoTransition(IObservable<double> progress, int oldValue, int newValue)
        {
            return progress
                .Select(progress => s_animator.Interpolate(Easing.Ease(progress), oldValue, newValue));
        }
    }
}
