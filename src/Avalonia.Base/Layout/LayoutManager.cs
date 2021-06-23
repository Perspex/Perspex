using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using Avalonia.Logging;
using Avalonia.Threading;
using Avalonia.VisualTree;

#nullable enable

namespace Avalonia.Layout
{
    /// <summary>
    /// Manages measuring and arranging of controls.
    /// </summary>
    public class LayoutManager : ILayoutManager, IDisposable
    {
        private const int MaxPasses = 3;
        private readonly ILayoutRoot _owner;
        private readonly LayoutQueue<ILayoutable> _toMeasure = new LayoutQueue<ILayoutable>(v => !v.IsMeasureValid);
        private readonly LayoutQueue<ILayoutable> _toArrange = new LayoutQueue<ILayoutable>(v => !v.IsArrangeValid);
        private readonly Action _executeLayoutPass;
        private List<EffectiveViewportChangedListener>? _effectiveViewportChangedListeners;
        private bool _disposed;
        private bool _queued;
        private bool _running;

        public LayoutManager(ILayoutRoot owner)
        {
            _owner = owner ?? throw new ArgumentNullException(nameof(owner));
            _executeLayoutPass = ExecuteLayoutPass;
        }

        public virtual event EventHandler? LayoutUpdated;

        /// <inheritdoc/>
        public virtual void InvalidateMeasure(ILayoutable control)
        {
            control = control ?? throw new ArgumentNullException(nameof(control));
            Dispatcher.UIThread.VerifyAccess();

            if (_disposed)
            {
                return;
            }

            if (!control.IsAttachedToVisualTree)
            {
#if DEBUG
                throw new AvaloniaInternalException(
                    "LayoutManager.InvalidateMeasure called on a control that is detached from the visual tree.");
#else
                return;
#endif
            }

            if (control.VisualRoot != _owner)
            {
                throw new ArgumentException("Attempt to call InvalidateMeasure on wrong LayoutManager.");
            }

            _toMeasure.Enqueue(control);
            _toArrange.Enqueue(control);
            QueueLayoutPass();
        }

        /// <inheritdoc/>
        public virtual void InvalidateArrange(ILayoutable control)
        {
            control = control ?? throw new ArgumentNullException(nameof(control));
            Dispatcher.UIThread.VerifyAccess();

            if (_disposed)
            {
                return;
            }

            if (!control.IsAttachedToVisualTree)
            {
#if DEBUG
                throw new AvaloniaInternalException(
                    "LayoutManager.InvalidateArrange called on a control that is detached from the visual tree.");
#else
                return;
#endif
            }

            if (control.VisualRoot != _owner)
            {
                throw new ArgumentException("Attempt to call InvalidateArrange on wrong LayoutManager.");
            }

            _toArrange.Enqueue(control);
            QueueLayoutPass();
        }

        /// <inheritdoc/>
        public virtual void ExecuteLayoutPass()
        {
            Dispatcher.UIThread.VerifyAccess();

            if (_disposed)
            {
                return;
            }

            if (!_running)
            {
                Stopwatch? stopwatch = null;

                const LogEventLevel timingLogLevel = LogEventLevel.Information;
                bool captureTiming = Logger.IsEnabled(timingLogLevel, LogArea.Layout);

                if (captureTiming)
                {
                    Logger.TryGet(timingLogLevel, LogArea.Layout)?.Log(
                        this,
                        "Started layout pass. To measure: {Measure} To arrange: {Arrange}",
                        _toMeasure.Count,
                        _toArrange.Count);

                    stopwatch = new Stopwatch();
                    stopwatch.Start();
                }

                _toMeasure.BeginLoop(MaxPasses);
                _toArrange.BeginLoop(MaxPasses);

                try
                {
                    _running = true;

                    for (var pass = 0; pass < MaxPasses; ++pass)
                    {
                        InnerLayoutPass();

                        if (!RaiseEffectiveViewportChanged())
                        {
                            break;
                        }
                    }
                }
                finally
                {
                    _running = false;
                }

                _toMeasure.EndLoop();
                _toArrange.EndLoop();

                if (captureTiming)
                {
                    stopwatch!.Stop();

                    Logger.TryGet(timingLogLevel, LogArea.Layout)?.Log(this, "Layout pass finished in {Time}", stopwatch.Elapsed);
                }
            }

            _queued = false;
            LayoutUpdated?.Invoke(this, EventArgs.Empty);
        }

        /// <inheritdoc/>
        public virtual void ExecuteInitialLayoutPass()
        {
            if (_disposed)
            {
                return;
            }

            try
            {
                _running = true;
                Measure(_owner);
                Arrange(_owner);
            }
            finally
            {
                _running = false;
            }

            // Running the initial layout pass may have caused some control to be invalidated
            // so run a full layout pass now (this usually due to scrollbars; its not known
            // whether they will need to be shown until the layout pass has run and if the
            // first guess was incorrect the layout will need to be updated).
            ExecuteLayoutPass();
        }

        [Obsolete("Call ExecuteInitialLayoutPass without parameter")]
        public void ExecuteInitialLayoutPass(ILayoutRoot root)
        {
            if (root != _owner)
            {
                throw new ArgumentException("ExecuteInitialLayoutPass called with incorrect root.");
            }

            ExecuteInitialLayoutPass();
        }

        public void Dispose()
        {
            _disposed = true;
            _toMeasure.Dispose();
            _toArrange.Dispose();
        }

        void ILayoutManager.RegisterEffectiveViewportListener(ILayoutable control)
        {
            _effectiveViewportChangedListeners ??= new List<EffectiveViewportChangedListener>();
            _effectiveViewportChangedListeners.Add(new EffectiveViewportChangedListener(
                control,
                CalculateEffectiveViewport(control)));
        }

        void ILayoutManager.UnregisterEffectiveViewportListener(ILayoutable control)
        {
            if (_effectiveViewportChangedListeners is object)
            {
                for (var i = _effectiveViewportChangedListeners.Count - 1; i >= 0; --i)
                {
                    if (_effectiveViewportChangedListeners[i].Listener == control)
                    {
                        _effectiveViewportChangedListeners.RemoveAt(i);
                    }
                }
            }
        }

        private void InnerLayoutPass()
        {
            for (var pass = 0; pass < MaxPasses; ++pass)
            {
                ExecuteMeasurePass();
                ExecuteArrangePass();

                if (_toMeasure.Count == 0)
                {
                    break;
                }
            }
        }

        private void ExecuteMeasurePass()
        {
            while (_toMeasure.Count > 0)
            {
                var control = _toMeasure.Dequeue();

                if (!control.IsMeasureValid && control.IsAttachedToVisualTree)
                {
                    Measure(control);
                }
            }
        }

        private void ExecuteArrangePass()
        {
            while (_toArrange.Count > 0)
            {
                var control = _toArrange.Dequeue();

                if (!control.IsArrangeValid && control.IsAttachedToVisualTree)
                {
                    Arrange(control);
                }
            }
        }

        private void Measure(ILayoutable control)
        {
            // Controls closest to the visual root need to be arranged first. We don't try to store
            // ordered invalidation lists, instead we traverse the tree upwards, measuring the
            // controls closest to the root first. This has been shown by benchmarks to be the
            // fastest and most memory-efficient algorithm.
            if (control.VisualParent is ILayoutable parent)
            {
                Measure(parent);
            }

            // If the control being measured has IsMeasureValid == true here then its measure was
            // handed by an ancestor and can be ignored. The measure may have also caused the
            // control to be removed.
            if (!control.IsMeasureValid && control.IsAttachedToVisualTree)
            {
                if (control is ILayoutRoot root)
                {
                    root.Measure(Size.Infinity);
                }
                else if (control.PreviousMeasure.HasValue)
                {
                    control.Measure(control.PreviousMeasure.Value);
                }
            }
        }

        private void Arrange(ILayoutable control)
        {
            if (control.VisualParent is ILayoutable parent)
            {
                Arrange(parent);
            }

            if (!control.IsArrangeValid && control.IsAttachedToVisualTree)
            {
                if (control is IEmbeddedLayoutRoot embeddedRoot)
                    control.Arrange(new Rect(embeddedRoot.AllocatedSize));
                else if (control is ILayoutRoot root)
                    control.Arrange(new Rect(root.DesiredSize));
                else if (control.PreviousArrange != null)
                {
                    // Has been observed that PreviousArrange sometimes is null, probably a bug somewhere else.
                    // Condition observed: control.VisualParent is Scrollbar, control is Border.
                    control.Arrange(control.PreviousArrange.Value);
                }
            }
        }

        private void QueueLayoutPass()
        {
            if (!_queued && !_running)
            {
                Dispatcher.UIThread.Post(_executeLayoutPass, DispatcherPriority.Layout);
                _queued = true;
            }
        }

        private bool RaiseEffectiveViewportChanged()
        {
            var startCount = _toMeasure.Count + _toArrange.Count;

            if (_effectiveViewportChangedListeners is object)
            {
                var count = _effectiveViewportChangedListeners.Count;
                var pool = ArrayPool<EffectiveViewportChangedListener>.Shared;
                var listeners = pool.Rent(count);

                _effectiveViewportChangedListeners.CopyTo(listeners);

                try
                {
                    for (var i = 0; i < count; ++i)
                    {
                        var l = _effectiveViewportChangedListeners[i];

                        if (!l.Listener.IsAttachedToVisualTree)
                        {
                            continue;
                        }

                        var viewport = CalculateEffectiveViewport(l.Listener);

                        if (viewport != l.Viewport)
                        {
                            l.Listener.EffectiveViewportChanged(new EffectiveViewportChangedEventArgs(viewport));
                            _effectiveViewportChangedListeners[i] = new EffectiveViewportChangedListener(l.Listener, viewport);
                        }
                    }
                }
                finally
                {
                    pool.Return(listeners, clearArray: true);
                }
            }

            return startCount != _toMeasure.Count + _toArrange.Count;
        }

        private Rect CalculateEffectiveViewport(IVisual control)
        {
            var viewport = new Rect(0, 0, double.PositiveInfinity, double.PositiveInfinity);
            CalculateEffectiveViewport(control, control, ref viewport);
            return viewport;
        }

        private void CalculateEffectiveViewport(IVisual target, IVisual control, ref Rect viewport)
        {
            // Recurse until the top level control.
            if (control.VisualParent is object)
            {
                CalculateEffectiveViewport(target, control.VisualParent, ref viewport);
            }
            else
            {
                viewport = new Rect(control.Bounds.Size);
            }

            // Apply the control clip bounds if it's not the target control. We don't apply it to
            // the target control because it may itself be clipped to bounds and if so the viewport
            // we calculate would be of no use.
            if (control != target && control.ClipToBounds)
            {
                viewport = control.Bounds.Intersect(viewport);
            }

            // Translate the viewport into this control's coordinate space.
            viewport = viewport.Translate(-control.Bounds.Position);

            if (control != target && control.RenderTransform is object)
            {
                var origin = control.RenderTransformOrigin.ToPixels(control.Bounds.Size);
                var offset = Matrix.CreateTranslation(origin);
                var renderTransform = (-offset) * control.RenderTransform.Value.Invert() * (offset);
                viewport = viewport.TransformToAABB(renderTransform);
            }
        }

        private readonly struct EffectiveViewportChangedListener
        {
            public EffectiveViewportChangedListener(ILayoutable listener, Rect viewport)
            {
                Listener = listener;
                Viewport = viewport;
            }

            public ILayoutable Listener { get; }
            public Rect Viewport { get; }
        }
    }
}
