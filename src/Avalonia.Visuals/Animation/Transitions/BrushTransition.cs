using System;

using Avalonia.Animation.Animators;
using Avalonia.Animation.Easings;
using Avalonia.Media;

#nullable enable

namespace Avalonia.Animation
{
    /// <summary>
    /// Transition class that handles <see cref="AvaloniaProperty"/> with <see cref="IBrush"/> type.
    /// </summary>
    public class BrushTransition : Transition<IBrush?>
    {
        private static readonly GradientBrushAnimator s_gradientAnimator = new GradientBrushAnimator();
        private static readonly ISolidColorBrushAnimator s_solidColorBrushAnimator = new ISolidColorBrushAnimator();

        public override IObservable<IBrush?> DoTransition(IObservable<double> progress, IBrush? oldValue, IBrush? newValue)
        {
            if (oldValue is null || newValue is null)
            {
                return new IncompatibleTransitionObservable(progress, Easing, oldValue, newValue);
            }

            if (oldValue is IGradientBrush oldGradient)
            {
                if (newValue is IGradientBrush newGradient)
                {
                    return new AnimatorTransitionObservable<IGradientBrush?, GradientBrushAnimator>(s_gradientAnimator, progress, Easing, oldGradient, newGradient);
                }
                else if (newValue is ISolidColorBrush newSolidColorBrushToConvert)
                {
                    var convertedSolidColorBrush = GradientBrushAnimator.ConvertSolidColorBrushToGradient(oldGradient, newSolidColorBrushToConvert);
                    return new AnimatorTransitionObservable<IGradientBrush?, GradientBrushAnimator>(s_gradientAnimator, progress, Easing, oldGradient, convertedSolidColorBrush);
                }
            }
            else if (newValue is IGradientBrush newGradient && oldValue is ISolidColorBrush oldSolidColorBrushToConvert)
            {
                var convertedSolidColorBrush = GradientBrushAnimator.ConvertSolidColorBrushToGradient(newGradient, oldSolidColorBrushToConvert);
                return new AnimatorTransitionObservable<IGradientBrush?, GradientBrushAnimator>(s_gradientAnimator, progress, Easing, convertedSolidColorBrush, newGradient);
            }

            if (oldValue is ISolidColorBrush oldSolidColorBrush && newValue is ISolidColorBrush newSolidColorBrush)
            {
                return new AnimatorTransitionObservable<ISolidColorBrush?, ISolidColorBrushAnimator>(s_solidColorBrushAnimator, progress, Easing, oldSolidColorBrush, newSolidColorBrush);
            }

            return new IncompatibleTransitionObservable(progress, Easing, oldValue, newValue);
        }

        private sealed class IncompatibleTransitionObservable : TransitionObservableBase<IBrush?>
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
                return progress >= 0.5 ? _to : _from;
            }
        }
    }
}
