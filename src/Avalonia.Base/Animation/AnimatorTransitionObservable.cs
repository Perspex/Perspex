using System;
using Avalonia.Animation.Animators;
using Avalonia.Animation.Easings;

namespace Avalonia.Animation
{
    /// <summary>
    /// Transition observable based on an <see cref="Animator{T}"/> producing a value.
    /// </summary>
    /// <typeparam name="T">Type of the transitioned value.</typeparam>
    /// <typeparam name="TAnimator">Type of the animator.</typeparam>
    public class AnimatorTransitionObservable<T, TAnimator> : TransitionObservableBase<T> where TAnimator : Animator<T>
    {
        private readonly TAnimator _animator;
        private readonly Easing _easing;
        private readonly T _oldValue;
        private readonly T _newValue;

        public AnimatorTransitionObservable(TAnimator animator, IObservable<double> progress, Easing easing, T oldValue, T newValue) : base(progress, easing)
        {
            _animator = animator;
            _easing = easing;
            _oldValue = oldValue;
            _newValue = newValue;
        }

        protected override T ProduceValue(double progress)
        {
            return _animator.Interpolate(progress, _oldValue, _newValue);
        }
    }
}
