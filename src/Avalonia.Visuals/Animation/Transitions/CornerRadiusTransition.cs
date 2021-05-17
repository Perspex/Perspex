using System;
using System.Reactive.Linq;

using Avalonia.Animation.Animators;

namespace Avalonia.Animation
{
    /// <summary>
    /// Transition class that handles <see cref="AvaloniaProperty"/> with <see cref="CornerRadius"/> type.
    /// </summary>  
    public class CornerRadiusTransition : Transition<CornerRadius>
    {
        private static readonly CornerRadiusAnimator s_animator = new CornerRadiusAnimator();

        /// <inheritdocs/>
        public override IObservable<CornerRadius> DoTransition(IObservable<double> progress, CornerRadius oldValue, CornerRadius newValue)
        {
            return progress
                .Select(progress => s_animator.Interpolate(Easing.Ease(progress), oldValue, newValue));
        }
    }
}
