using System;
using Avalonia.Animation.Easings;
using Avalonia.Reactive;

#nullable enable

namespace Avalonia.Animation
{
    /// <summary>
    /// Provides base for observables implementing transitions.
    /// </summary>
    /// <typeparam name="T">Type of the transitioned value.</typeparam>
    public abstract class TransitionObservableBase<T> : SingleSubscriberObservableBase<T>, IObserver<double>
    {
        private readonly Easing _easing;
        private readonly IObservable<double> _progress;
        private IDisposable? _progressSubscription;

        protected TransitionObservableBase(IObservable<double> progress, Easing easing)
        {
            _progress = progress;
            _easing = easing;
        }

        /// <summary>
        /// Produces value at given progress time point.
        /// </summary>
        /// <param name="progress">Transition progress.</param>
        protected abstract T ProduceValue(double progress);

        protected override void Subscribed()
        {
            _progressSubscription = _progress.Subscribe(this);
        }

        protected override void Unsubscribed()
        {
            _progressSubscription?.Dispose();
        }

        void IObserver<double>.OnCompleted()
        {
            PublishCompleted();
        }

        void IObserver<double>.OnError(Exception error)
        {
            PublishError(error);
        }

        void IObserver<double>.OnNext(double value)
        {
            double progress = _easing.Ease(value);

            PublishNext(ProduceValue(progress));
        }
    }
}
