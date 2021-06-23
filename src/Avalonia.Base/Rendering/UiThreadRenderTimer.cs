using System;
using System.Diagnostics;
using System.Reactive.Disposables;
using Avalonia.Threading;

namespace Avalonia.Rendering
{
    /// <summary>
    /// Render timer that ticks on UI thread. Useful for debugging or bootstrapping on new platforms 
    /// </summary>
    
    public class UiThreadRenderTimer : DefaultRenderTimer
    {
        public UiThreadRenderTimer(int framesPerSecond) : base(framesPerSecond)
        {
        }

        protected override IDisposable StartCore(Action<TimeSpan> tick)
        {
            bool cancelled = false;
            var st = Stopwatch.StartNew();
            DispatcherTimer.Run(() =>
            {
                if (cancelled)
                    return false;
                tick(st.Elapsed);
                return !cancelled;
            }, TimeSpan.FromSeconds(1.0 / FramesPerSecond), DispatcherPriority.Render);
            return Disposable.Create(() => cancelled = true);
        }
    }
}
