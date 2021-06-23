using System;
using System.Runtime.ExceptionServices;
using Avalonia.Reactive;
using Avalonia.Utilities;

namespace Avalonia.Animation
{
    /// <summary>
    /// Handles the timing and lifetime of a <see cref="Transition{T}"/>.
    /// </summary>
    internal class TransitionInstance : SingleSubscriberObservableBase<double>, IObserver<TimeSpan>
    {
        private IDisposable _timerSubscription;
        private TimeSpan _delay;
        private TimeSpan _duration;
        private readonly IClock _baseClock;
        private TransitionClock _clock;

        public TransitionInstance(IClock clock, TimeSpan delay, TimeSpan duration)
        {
            clock = clock ?? throw new ArgumentNullException(nameof(clock));

            _delay = delay;
            _duration = duration;
            _baseClock = clock;
        }

        private void TimerTick(TimeSpan t)
        {

            // [<------------- normalizedTotalDur ------------------>]
            // [<---- Delay ---->][<---------- Duration ------------>]
            //                   ^- normalizedDelayEnd
            //                    [<----   normalizedInterpVal   --->]

            var normalizedInterpVal = 1d;

            if (!MathUtilities.AreClose(_duration.TotalSeconds, 0d))
            {
                var normalizedTotalDur = _delay + _duration;
                var normalizedDelayEnd = _delay.TotalSeconds / normalizedTotalDur.TotalSeconds;
                var normalizedPresentationTime = t.TotalSeconds / normalizedTotalDur.TotalSeconds;

                if (normalizedPresentationTime < normalizedDelayEnd
                    || MathUtilities.AreClose(normalizedPresentationTime, normalizedDelayEnd))
                {
                    normalizedInterpVal = 0d;
                }
                else
                {
                    normalizedInterpVal = (t.TotalSeconds - _delay.TotalSeconds) / _duration.TotalSeconds;
                }
            }

            // Clamp interpolation value.
            if (normalizedInterpVal >= 1d || normalizedInterpVal < 0d)
            {
                PublishNext(1d);
                PublishCompleted();
            }
            else
            {
                PublishNext(normalizedInterpVal);
            }
        }

        protected override void Unsubscribed()
        {
            _timerSubscription?.Dispose();
            _clock.PlayState = PlayState.Stop;
        }

        protected override void Subscribed()
        {
            _clock = new TransitionClock(_baseClock);
            _timerSubscription = _clock.Subscribe(this);
            PublishNext(0.0d);
        }

        void IObserver<TimeSpan>.OnCompleted()
        {
            PublishCompleted();
        }

        void IObserver<TimeSpan>.OnError(Exception error)
        {
            PublishError(error);
        }

        void IObserver<TimeSpan>.OnNext(TimeSpan value)
        {
            TimerTick(value);
        }

        /// <summary>
        /// TODO: This clock is still fairly expensive due to <see cref="ClockBase"/> implementation.
        /// </summary>
        private sealed class TransitionClock : ClockBase, IObserver<TimeSpan>
        {
            private readonly IDisposable _parentSubscription;

            public TransitionClock(IClock parent)
            {
                _parentSubscription = parent.Subscribe(this);
            }

            protected override void Stop()
            {
                _parentSubscription.Dispose();
            }

            void IObserver<TimeSpan>.OnNext(TimeSpan value)
            {
                Pulse(value);
            }

            void IObserver<TimeSpan>.OnCompleted()
            {
            }

            void IObserver<TimeSpan>.OnError(Exception error)
            {
                ExceptionDispatchInfo.Capture(error).Throw();
            }
        }
    }
}
