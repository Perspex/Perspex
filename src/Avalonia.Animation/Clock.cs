using System;

namespace Avalonia.Animation
{
    public class Clock : ClockBase
    {
        public static IClock GlobalClock => AvaloniaLocator.Current.GetService<IGlobalClock>();

        private readonly IDisposable _parentSubscription;

        public Clock() : this(GlobalClock)
        {
        }
        
        public Clock(IClock parent)
        {
            _parentSubscription = parent.Subscribe(Pulse);
        }

        protected override void Stop()
        {
            _parentSubscription?.Dispose();
        }
    }
}
