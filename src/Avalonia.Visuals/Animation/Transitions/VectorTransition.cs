using System;
using System.Reactive.Linq;

using Avalonia.Animation.Animators;

namespace Avalonia.Animation
{
    /// <summary>
    /// Transition class that handles <see cref="AvaloniaProperty"/> with <see cref="Vector"/> type.
    /// </summary>  
    public class VectorTransition : Transition<Vector>
    {
        private static readonly VectorAnimator s_animator = new VectorAnimator();

        /// <inheritdocs/>
        public override IObservable<Vector> DoTransition(IObservable<double> progress, Vector oldValue, Vector newValue)
        {
            return progress
                .Select(progress => s_animator.Interpolate(Easing.Ease(progress), oldValue, newValue));
        }
    }
}
