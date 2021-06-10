using System;
using Avalonia.Animation.Animators;
using Avalonia.Animation.Easings;
using Avalonia.Media;

#nullable enable

namespace Avalonia.Animation
{
    /// <summary>
    /// Transition class that handles <see cref="AvaloniaProperty"/> with <see cref="IBrush"/> type.
    /// Only values of <see cref="ISolidColorBrush"/> will transition correctly at the moment.
    /// </summary>
    public class BrushTransition : Transition<IBrush?>
    {
        private static readonly ISolidColorBrushAnimator s_animator = new ISolidColorBrushAnimator();

        public override IObservable<IBrush?> DoTransition(IObservable<double> progress, IBrush? oldValue, IBrush? newValue)
        {
            var oldSolidColorBrush = TryGetSolidColorBrush(oldValue);
            var newSolidColorBrush = TryGetSolidColorBrush(newValue);

            if (oldSolidColorBrush != null && newSolidColorBrush != null)
            {
                return new AnimatorTransitionObservable<ISolidColorBrush, ISolidColorBrushAnimator>(
                    s_animator, progress, Easing, oldSolidColorBrush, newSolidColorBrush);
            }

            return new IncompatibleTransitionObservable(progress, Easing, oldValue, newValue);
        }

        private static ISolidColorBrush? TryGetSolidColorBrush(IBrush? brush)
        {
            if (brush is null)
            {
                return Brushes.Transparent;
            }

            return brush as ISolidColorBrush;
        }

        private class IncompatibleTransitionObservable : TransitionObservableBase<IBrush?>
        {
            private readonly IBrush? _from;
            private readonly IBrush? _to;

            public IncompatibleTransitionObservable(IObservable<double> progress, Easing easing, IBrush? from, IBrush? to) : base(progress, easing)
            {
                _from = from;
                _to = to;
            }

            protected override IBrush? ProduceValue(double progress)
            {
                return progress < 0.5 ? _from : _to;
            }
        }
    }
}
