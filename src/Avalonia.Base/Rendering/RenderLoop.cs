using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Avalonia.Logging;
using Avalonia.Threading;

namespace Avalonia.Rendering
{
    /// <summary>
    /// The application render loop.
    /// </summary>
    /// <remarks>
    /// The render loop is responsible for advancing the animation timer and updating the scene
    /// graph for visible windows.
    /// </remarks>
    public class RenderLoop : IRenderLoop
    {
        private readonly IDispatcher _dispatcher;
        private List<IRenderLoopTask> _items = new List<IRenderLoopTask>();
        private List<IRenderLoopTask> _itemsCopy = new List<IRenderLoopTask>();
        private IRenderTimer _timer;
        private int _inTick;
        private int _inUpdate;

        /// <summary>
        /// Initializes a new instance of the <see cref="RenderLoop"/> class.
        /// </summary>
        public RenderLoop()
        {
            _dispatcher = Dispatcher.UIThread;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RenderLoop"/> class.
        /// </summary>
        /// <param name="timer">The render timer.</param>
        /// <param name="dispatcher">The UI thread dispatcher.</param>
        public RenderLoop(IRenderTimer timer, IDispatcher dispatcher)
        {
            _timer = timer;
            _dispatcher = dispatcher;
        }

        /// <summary>
        /// Gets the render timer.
        /// </summary>
        protected IRenderTimer Timer
        {
            get
            {
                if (_timer == null)
                {
                    _timer = AvaloniaLocator.Current.GetService<IRenderTimer>();
                }

                return _timer;
            }
        }

        /// <inheritdoc/>
        public void Add(IRenderLoopTask i)
        {
            Contract.Requires<ArgumentNullException>(i != null);
            Dispatcher.UIThread.VerifyAccess();

            lock (_items)
            {
                _items.Add(i);

                if (_items.Count == 1)
                {
                    Timer.Tick += TimerTick;
                }
            }
        }

        /// <inheritdoc/>
        public void Remove(IRenderLoopTask i)
        {
            Contract.Requires<ArgumentNullException>(i != null);
            Dispatcher.UIThread.VerifyAccess();
            lock (_items)
            {
                _items.Remove(i);

                if (_items.Count == 0)
                {
                    Timer.Tick -= TimerTick;
                }
            }
        }

        private void TimerTick(TimeSpan time)
        {
            if (Interlocked.CompareExchange(ref _inTick, 1, 0) == 0)
            {
                try
                {
                    bool needsUpdate = false;

                    foreach (IRenderLoopTask item in _items)
                    {
                        if (item.NeedsUpdate)
                        {
                            needsUpdate = true;

                            break;
                        }
                    }

                    if (needsUpdate &&
                        Interlocked.CompareExchange(ref _inUpdate, 1, 0) == 0)
                    {
                        _dispatcher.Post(() =>
                        {
                            for (var i = 0; i < _items.Count; ++i)
                            {
                                var item = _items[i];

                                if (item.NeedsUpdate)
                                {
                                    try
                                    {
                                        item.Update(time);
                                    }
                                    catch (Exception ex)
                                    {
                                        Logger.TryGet(LogEventLevel.Error, LogArea.Visual)?.Log(this, "Exception in render update: {Error}", ex);
                                    }
                                }
                            }

                            Interlocked.Exchange(ref _inUpdate, 0);
                        }, DispatcherPriority.Render);
                    }

                    lock (_items)
                    {
                        _itemsCopy.Clear();
                        foreach (var i in _items)
                            _itemsCopy.Add(i);
                    }

                    for (int i = 0; i < _itemsCopy.Count; i++)
                    {
                        _itemsCopy[i].Render();
                    }
                    
                    _itemsCopy.Clear();

                }
                catch (Exception ex)
                {
                    Logger.TryGet(LogEventLevel.Error, LogArea.Visual)?.Log(this, "Exception in render loop: {Error}", ex);
                }
                finally
                {
                    Interlocked.Exchange(ref _inTick, 0);
                }
            }
        }
    }
}
