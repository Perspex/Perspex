using System;
using Avalonia.Animation.Animators;

namespace Avalonia.Animation
{
    /// <summary>
    /// <see cref="Transition{T}"/> using an <see cref="Animator{T}"/> to transition between values.
    /// </summary>
    /// <typeparam name="T">Type of the transitioned value.</typeparam>
    /// <typeparam name="TAnimator">Type of the animator.</typeparam>
    public abstract class AnimatorDrivenTransition<T, TAnimator> : Transition<T> where TAnimator : Animator<T>, new()
    {
        private static readonly TAnimator s_animator = new TAnimator();

        public override IObservable<T> DoTransition(IObservable<double> progress, T oldValue, T newValue)
        {
            return new AnimatorTransitionObservable<T, TAnimator>(s_animator, progress, Easing, oldValue, newValue);
        }
    }
}
