using System;
using System.Reactive.Linq;

using Avalonia.Animation.Animators;

namespace Avalonia.Animation
{
    /// <summary>
    /// Transition class that handles <see cref="AvaloniaProperty"/> with <see cref="Point"/> type.
    /// </summary>  
    public class PointTransition : Transition<Point>
    {
        private static readonly PointAnimator s_animator = new PointAnimator();

        /// <inheritdocs/>
        public override IObservable<Point> DoTransition(IObservable<double> progress, Point oldValue, Point newValue)
        {
            return progress
                .Select(progress => s_animator.Interpolate(Easing.Ease(progress), oldValue, newValue));
        }
    }
}
