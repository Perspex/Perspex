using System;
using System.Reactive.Linq;

using Avalonia.Animation.Animators;

namespace Avalonia.Animation
{
    /// <summary>
    /// Transition class that handles <see cref="AvaloniaProperty"/> with <see cref="Size"/> type.
    /// </summary>  
    public class SizeTransition : Transition<Size>
    {
        private static readonly SizeAnimator s_animator = new SizeAnimator();

        /// <inheritdocs/>
        public override IObservable<Size> DoTransition(IObservable<double> progress, Size oldValue, Size newValue)
        {
            return progress
                .Select(progress => s_animator.Interpolate(Easing.Ease(progress), oldValue, newValue));
        }
    }
}
