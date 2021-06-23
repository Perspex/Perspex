using System;
using System.Collections.Generic;
using System.Reactive.Disposables;
using Avalonia.Logging;
using Avalonia.Media;

namespace Avalonia.Animation.Animators
{
    /// <summary>
    /// Animator that handles all animations on properties
    /// with <see cref="IBrush"/> as their type and
    /// redirect them to the properly registered
    /// animators in this class.
    /// </summary>
    public class BaseBrushAnimator : Animator<IBrush>
    {
        private IAnimator _targetAnimator;

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
            where TAnimator : IAnimator
        {
            _brushAnimators.Insert(0, (condition, typeof(TAnimator)));
        }

        /// <inheritdoc/>
        public override IDisposable Apply(Animation animation, Animatable control, IClock clock,
            IObservable<bool> match, Action onComplete)
        {
            foreach (var valueType in _brushAnimators)
            {
                if (!valueType.Match(this[0].Value.GetType())) continue;

                _targetAnimator = (IAnimator)Activator.CreateInstance(valueType.AnimatorType);

                foreach (var keyframe in this)
                {
                    _targetAnimator.Add(keyframe);
                }

                _targetAnimator.Property = this.Property;
                
               return _targetAnimator.Apply(animation, control, clock, match, onComplete);
            }

            Logger.TryGet(LogEventLevel.Error, LogArea.Animations)?.Log(
                this,
                "The animation's keyframe values didn't match any brush animators registered in BaseBrushAnimator.");
            
            return Disposable.Empty;
        }

        /// <inheritdoc/>
        public override IBrush Interpolate(double progress, IBrush oldValue, IBrush newValue) => null;
    }
}
