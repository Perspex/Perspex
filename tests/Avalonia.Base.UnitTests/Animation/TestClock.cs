using System;
using Avalonia.Animation;

namespace Avalonia.Base.UnitTests.Animation
{
    internal class TestClock : IClock, IDisposable
    {
        private TimeSpan _curTime;

        private IObserver<TimeSpan> _observer;

        public PlayState PlayState { get; set; } = PlayState.Run;

        public void Dispose()
        {
            _observer?.OnCompleted();
        }

        public void Step(TimeSpan time)
        {
            _observer?.OnNext(time);
        }

        public void Pulse(TimeSpan time)
        {
            _curTime += time;
            _observer?.OnNext(_curTime);
        }

        public IDisposable Subscribe(IObserver<TimeSpan> observer)
        {
            _observer = observer;
            return this;
        }
    }
}
