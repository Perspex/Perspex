using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

using Avalonia.Logging;
using Avalonia.Media;

#nullable enable

namespace Avalonia.Animation.Animators
{
    /// <summary>
    /// Animator that handles all animations on properties
    /// with <see cref="IBrush"/> as their type and
    /// redirect them to the properly registered
    /// animators in this class.
    /// </summary>
    public class BaseBrushAnimator : Animator<IBrush?>
    {
        private static readonly List<(Func<Type, bool> Match, Type AnimatorType)> _brushAnimators =
            new List<(Func<Type, bool> Match, Type AnimatorType)>();

        /// <summary>
        /// Register an <see cref="Animator{T}"/> that handles a specific
        /// <see cref="IBrush"/>'s descendant value type.
        /// </summary>
        /// <param name="condition">
        /// The condition to which the <see cref="Animator{T}"/>
        /// is to be activated and used.
        /// </param>
        /// <typeparam name="TAnimator">
        /// The type of the animator to instantiate.
        /// </typeparam>
        public static void RegisterBrushAnimator<TAnimator>(Func<Type, bool> condition)
            where TAnimator : IAnimator, new()
        {
            _brushAnimators.Insert(0, (condition, typeof(TAnimator)));
        }

        /// <inheritdoc/>
        public override IDisposable Apply(Animation animation, Animatable control, IClock clock,
            IObservable<bool> match, Action onComplete)
        {
            if (TryCreateCustomRegisteredAnimator(out var animator)
                || TryCreateGradientAnimator(out animator)
                || TryCreateSolidColorBrushAnimator(out animator))
            {
                return animator.Apply(animation, control, clock, match, onComplete);
            }

            Logger.TryGet(LogEventLevel.Error, LogArea.Animations)?.Log(
                this,
                "The animation's keyframe value types set is not supported.");

            return base.Apply(animation, control, clock, match, onComplete);
        }

        /// <summary>
        /// Fallback implementation of <see cref="IBrush"/> animation.
        /// </summary>
        public override IBrush? Interpolate(double progress, IBrush? oldValue, IBrush? newValue) => progress >= 0.5 ? newValue : oldValue;

        private bool TryCreateGradientAnimator([NotNullWhen(true)] out IAnimator? animator)
        {
            IGradientBrush? firstGradient = null;
            foreach (var keyframe in this)
            {
                if (keyframe.Value is IGradientBrush gradientBrush)
                {
                    firstGradient = gradientBrush;
                    break;
                }
            }

            if (firstGradient is null)
            {
                animator = null;
                return false;
            }

            var gradientAnimator = new GradientBrushAnimator();
            gradientAnimator.Property = Property;

            foreach (var keyframe in this)
            {
                if (keyframe.Value is ISolidColorBrush solidColorBrush)
                {
                    gradientAnimator.Add(new AnimatorKeyFrame(typeof(GradientBrushAnimator), keyframe.Cue, keyframe.KeySpline)
                    {
                        Value = GradientBrushAnimator.ConvertSolidColorBrushToGradient(firstGradient, solidColorBrush)
                    });
                }
                else if (keyframe.Value is IGradientBrush)
                {
                    gradientAnimator.Add(new AnimatorKeyFrame(typeof(GradientBrushAnimator), keyframe.Cue, keyframe.KeySpline)
                    {
                        Value = keyframe.Value
                    });
                }
                else
                {
                    animator = null;
                    return false;
                }
            }

            animator = gradientAnimator;
            return true;
        }

        private bool TryCreateSolidColorBrushAnimator([NotNullWhen(true)] out IAnimator? animator)
        {
            var solidColorBrushAnimator = new ISolidColorBrushAnimator();
            solidColorBrushAnimator.Property = Property;

            foreach (var keyframe in this)
            {
                if (keyframe.Value is ISolidColorBrush)
                {
                    solidColorBrushAnimator.Add(new AnimatorKeyFrame(typeof(ISolidColorBrushAnimator), keyframe.Cue, keyframe.KeySpline)
                    {
                        Value = keyframe.Value
                    });
                }
                else
                {
                    animator = null;
                    return false;
                }
            }

            animator = solidColorBrushAnimator;
            return true;
        }

        private bool TryCreateCustomRegisteredAnimator([NotNullWhen(true)] out IAnimator? animator)
        {
            if (_brushAnimators.Count > 0)
            {
                var firstKeyType = this[0].Value.GetType();
                foreach (var (match, animatorType) in _brushAnimators)
                {
                    if (!match(firstKeyType))
                        continue;

                    animator = (IAnimator)Activator.CreateInstance(animatorType);
                    if (animator != null)
                    {
                        animator.Property = Property;
                        foreach (var keyframe in this)
                        {
                            animator.Add(new AnimatorKeyFrame(animatorType, keyframe.Cue, keyframe.KeySpline)
                            {
                                Value = keyframe.Value
                            });
                        }

                        return true;
                    }
                }
            }

            animator = null;
            return false;
        }
    }
}
