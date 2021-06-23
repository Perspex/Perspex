using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Avalonia.Platform;

namespace Avalonia.Rendering
{
    /// <summary>
    /// Defines a default render timer that uses a standard timer.
    /// </summary>
    /// <remarks>
    /// This class may be overridden by platform implementations to use a specialized timer
    /// implementation.
    /// </remarks>
    public class DefaultRenderTimer : IRenderTimer
    {
        private IRuntimePlatform _runtime;
        private int _subscriberCount;
        private Action<TimeSpan> _tick;
        private IDisposable _subscription;

        /// <summary>
        /// Initializes a new instance of the <see cref="DefaultRenderTimer"/> class.
        /// </summary>
        /// <param name="framesPerSecond">
        /// The number of frames per second at which the loop should run.
        /// </param>
        public DefaultRenderTimer(int framesPerSecond)
        {
            FramesPerSecond = framesPerSecond;
        }

        /// <summary>
        /// Gets the number of frames per second at which the loop runs.
        /// </summary>
        public int FramesPerSecond { get; }

        /// <inheritdoc/>
        public event Action<TimeSpan> Tick
        {
            add
            {
                _tick += value;

                if (_subscriberCount++ == 0)
                {
                    Start();
                }
            }

            remove
            {
                if (--_subscriberCount == 0)
                {
                    Stop();
                }

                _tick -= value;
            }
        }

        /// <summary>
        /// Starts the timer.
        /// </summary>
        protected void Start()
        {
            _subscription = StartCore(InternalTick);
        }

        /// <summary>
        /// Provides the implementation of starting the timer.
        /// </summary>
        /// <param name="tick">The method to call on each tick.</param>
        /// <remarks>
        /// This can be overridden by platform implementations to use a specialized timer
        /// implementation.
        /// </remarks>
        protected virtual IDisposable StartCore(Action<TimeSpan> tick)
        {
            if (_runtime == null)
            {
                _runtime = AvaloniaLocator.Current.GetService<IRuntimePlatform>();
            }

            return _runtime.StartSystemTimer(
                TimeSpan.FromSeconds(1.0 / FramesPerSecond),
                () => tick(TimeSpan.FromMilliseconds(Environment.TickCount)));
        }

        /// <summary>
        /// Stops the timer.
        /// </summary>
        protected void Stop()
        {
            _subscription.Dispose();
            _subscription = null;
        }

        private void InternalTick(TimeSpan tickCount)
        {
            _tick(tickCount);
        }
    }
}
