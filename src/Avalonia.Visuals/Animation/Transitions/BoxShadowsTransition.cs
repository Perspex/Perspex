using System;
using System.Reactive.Linq;

using Avalonia.Animation.Animators;
using Avalonia.Media;

namespace Avalonia.Animation
{
    /// <summary>
    /// Transition class that handles <see cref="AvaloniaProperty"/> with <see cref="BoxShadows"/> type.
    /// </summary>  
    public class BoxShadowsTransition : Transition<BoxShadows>
    {
        private static readonly BoxShadowsAnimator s_animator = new BoxShadowsAnimator();

        /// <inheritdocs/>
        public override IObservable<BoxShadows> DoTransition(IObservable<double> progress, BoxShadows oldValue, BoxShadows newValue)
        {
            return progress
                .Select(progress => s_animator.Interpolate(Easing.Ease(progress), oldValue, newValue));
        }
    }
}
