using System;
using System.Reactive.Linq;

using Avalonia.Animation.Animators;

namespace Avalonia.Animation
{
    /// <summary>
    /// Transition class that handles <see cref="AvaloniaProperty"/> with <see cref="Thickness"/> type.
    /// </summary>  
    public class ThicknessTransition : Transition<Thickness>
    {
        private static readonly ThicknessAnimator s_animator = new ThicknessAnimator();

        /// <inheritdocs/>
        public override IObservable<Thickness> DoTransition(IObservable<double> progress, Thickness oldValue, Thickness newValue)
        {
            return progress
                .Select(progress => s_animator.Interpolate(Easing.Ease(progress), oldValue, newValue));
        }
    }
}
