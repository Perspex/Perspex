using System;
using System.Diagnostics;
using System.Threading;

namespace Avalonia.Rendering
{
    public class SleepLoopRenderTimer : IRenderTimer
    {
        private Action<TimeSpan> _tick;
        private int _count;
        private readonly object _lock = new object();
        private bool _running;
        private readonly Stopwatch _st = Stopwatch.StartNew();
        private readonly TimeSpan _timeBetweenTicks;

        public SleepLoopRenderTimer(int fps)
        {
            _timeBetweenTicks = TimeSpan.FromSeconds(1d / fps);
        }
        
        public event Action<TimeSpan> Tick
        {
            add
            {
                lock (_lock)
                {
                    _tick += value;
                    _count++;
                    if (_running)
                        return;
                    _running = true;
                    new Thread(LoopProc) { IsBackground = true }.Start();
                }

            }
            remove
            {
                lock (_lock)
                {
                    _tick -= value;
                    _count--;
                }
            }
        }

        void LoopProc()
        {
            var lastTick = _st.Elapsed;
            while (true)
            {
                var now = _st.Elapsed;
                var timeTillNextTick = lastTick + _timeBetweenTicks - now;
                if (timeTillNextTick.TotalMilliseconds > 1) Thread.Sleep(timeTillNextTick);
                lastTick = now;
                lock (_lock)
                {
                    if (_count == 0)
                    {
                        _running = false;
                        return;
                    }
                }

                _tick?.Invoke(now);
                
            }
        }


    }
}
