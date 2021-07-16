// This source file is adapted from the WinUI project.
// (https://github.com/microsoft/microsoft-ui-xaml)
//
// Licensed to The Avalonia Project under MIT License, courtesy of The .NET Foundation.

using System;
using System.Collections.Generic;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Avalonia.Controls.Presenters;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Logging;
using Avalonia.Media;
using Avalonia.Reactive;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace Avalonia.Controls
{
    internal class ViewportManager
    {
        private const double CacheBufferPerSideInflationPixelDelta = 40.0;
        private readonly ItemsRepeater _owner;
        private bool _ensuredScroller;
        private IScrollAnchorProvider _scroller;
        private IControl _makeAnchorElement;
        private bool _isAnchorOutsideRealizedRange;
        private Rect _visibleWindow;
        private Rect _layoutExtent;
        // This is the expected shift by the layout.
        private Point _expectedViewportShift;
        // This is what is pending and not been accounted for. 
        // Sometimes the scrolling surface cannot service a shift (for example
        // it is already at the top and cannot shift anymore.)
        private Point _pendingViewportShift;
        // Unshiftable shift amount that this view manager can
        // handle on its own to fake it to the layout as if the shift
        // actually happened. This can happen in cases where no scrollviewer
        // in the parent chain can scroll in the shift direction.
        private Point _unshiftableShift;
        private double _maximumHorizontalCacheLength = 2.0;
        private double _maximumVerticalCacheLength = 2.0;
        private double _horizontalCacheBufferPerSide;
        private double _verticalCacheBufferPerSide;
        private bool _isBringIntoViewInProgress;
        // For non-virtualizing layouts, we do not need to keep
        // updating viewports and invalidating measure often. So when
        // a non virtualizing layout is used, we stop doing all that work.
        private bool _managingViewportDisabled;
        private bool _effectiveViewportChangedSubscribed;
        private bool _layoutUpdatedSubscribed;

        public ViewportManager(ItemsRepeater owner)
        {
            _owner = owner;
        }

        public IControl SuggestedAnchor
        {
            get
            {
                // The element generated during the ItemsRepeater.MakeAnchor call has precedence over the next tick.
                var suggestedAnchor = _makeAnchorElement;
                var owner = _owner;

                if (suggestedAnchor == null)
                {
                    var anchorElement = _scroller?.CurrentAnchor;

                    if (anchorElement != null)
                    {
                        // We can't simply return anchorElement because, in case of nested Repeaters, it may not
                        // be a direct child of ours, or even an indirect child. We need to walk up the tree starting
                        // from anchorElement to figure out what child of ours (if any) to use as the suggested element.
                        var child = anchorElement;
                        var parent = child.VisualParent as IControl;

                        while (parent != null)
                        {
                            if (parent == owner)
                            {
                                suggestedAnchor = child;
                                break;
                            }

                            child = parent;
                            parent = parent.VisualParent as IControl;
                        }
                    }
                }

                return suggestedAnchor;
            }
        }

        public bool HasScroller => _scroller != null;

        public IControl MadeAnchor => _makeAnchorElement;

        public double HorizontalCacheLength
        {
            get => _maximumHorizontalCacheLength;
            set
            {
                if (_maximumHorizontalCacheLength != value)
                {
                    ValidateCacheLength(value);
                    _maximumHorizontalCacheLength = value;
                }
            }
        }

        public double VerticalCacheLength
        {
            get => _maximumVerticalCacheLength;
            set
            {
                if (_maximumVerticalCacheLength != value)
                {
                    ValidateCacheLength(value);
                    _maximumVerticalCacheLength = value;
                }
            }
        }
        
        private Rect GetLayoutVisibleWindowDiscardAnchor()
        {
            var visibleWindow = _visibleWindow;

            if (HasScroller)
            {
                visibleWindow = new Rect(
                    visibleWindow.X + _layoutExtent.X + _expectedViewportShift.X + _unshiftableShift.X,
                    visibleWindow.Y + _layoutExtent.Y + _expectedViewportShift.Y + _unshiftableShift.Y,
                    visibleWindow.Width,
                    visibleWindow.Height);
            }

            return visibleWindow;
        }

        public Rect GetLayoutVisibleWindow()
        {
            var visibleWindow = _visibleWindow;

            if (_makeAnchorElement != null)
            {
                // The anchor is not necessarily laid out yet. Its position should default
                // to zero and the layout origin is expected to change once layout is done.
                // Until then, we need a window that's going to protect the anchor from
                // getting recycled.
                visibleWindow = visibleWindow.WithX(0).WithY(0);
            }
            else if (HasScroller)
            {
                visibleWindow = new Rect(
                    visibleWindow.X + _layoutExtent.X + _expectedViewportShift.X + _unshiftableShift.X,
                    visibleWindow.Y + _layoutExtent.Y + _expectedViewportShift.Y + _unshiftableShift.Y,
                    visibleWindow.Width,
                    visibleWindow.Height);
            }

            return visibleWindow;
        }

        public Rect GetLayoutRealizationWindow()
        {
            var realizationWindow = GetLayoutVisibleWindow();
            if (HasScroller)
            {
                realizationWindow = new Rect(
                    realizationWindow.X - _horizontalCacheBufferPerSide,
                    realizationWindow.Y - _verticalCacheBufferPerSide,
                    realizationWindow.Width + _horizontalCacheBufferPerSide * 2.0,
                    realizationWindow.Height + _verticalCacheBufferPerSide * 2.0);
            }

            return realizationWindow;
        }

        public void SetLayoutExtent(Rect extent)
        {
            _expectedViewportShift = new Point(
                _expectedViewportShift.X + _layoutExtent.X - extent.X,
                _expectedViewportShift.Y + _layoutExtent.Y - extent.Y);

            // We tolerate viewport imprecisions up to 1 pixel to avoid invalidating layout too much.
            if (Math.Abs(_expectedViewportShift.X) > 1 || Math.Abs(_expectedViewportShift.Y) > 1)
            {
                Logger.TryGet(LogEventLevel.Verbose, "Repeater")?.Log(this, "{LayoutId}: Expecting viewport shift of ({Shift})",
                    _owner.Layout.LayoutId, _expectedViewportShift);

                // There are cases where we might be expecting a shift but not get it. We will
                // be waiting for the effective viewport event but if the scroll viewer is not able
                // to perform the shift (perhaps because it cannot scroll in negative offset),
                // then we will end up not realizing elements in the visible 
                // window. To avoid this, we register to layout updated for this layout pass. If we 
                // get an effective viewport, we know we have a new viewport and we unregister from
                // layout updated. If we get the layout updated handler, then we know that the 
                // scroller was unable to perform the shift and we invalidate measure and unregister
                // from the layout updated event.
                if (!_layoutUpdatedSubscribed)
                {
                    _owner.LayoutUpdated += OnLayoutUpdated;
                    _layoutUpdatedSubscribed = true;
                }
            }

            _layoutExtent = extent;
            _pendingViewportShift = _expectedViewportShift;

            // We just finished a measure pass and have a new extent.
            // Let's make sure the scrollers will run its arrange so that they track the anchor.
            ((IControl)_scroller)?.InvalidateArrange();
        }

        public Point GetOrigin() => _layoutExtent.TopLeft;

        public void OnLayoutChanged(bool isVirtualizing)
        {
            _managingViewportDisabled = !isVirtualizing;

            _layoutExtent = default;
            _expectedViewportShift = default;
            _pendingViewportShift = default;
            _unshiftableShift = default;

            if (_managingViewportDisabled && _effectiveViewportChangedSubscribed)
            {
                _owner.EffectiveViewportChanged -= OnEffectiveViewportChanged;
                _effectiveViewportChangedSubscribed = false;
            }
            else if (!_managingViewportDisabled && !_effectiveViewportChangedSubscribed)
            {
                _owner.EffectiveViewportChanged += OnEffectiveViewportChanged;
                _effectiveViewportChangedSubscribed = true;
            }
        }

        public void OnElementPrepared(IControl element, VirtualizationInfo virtInfo)
        {
            // WinUI registers the element as an anchor candidate here, but I feel that's in error:
            // at this point the element has not yet been positioned by the arrange pass so it will
            // have its previous position, meaning that when the arrange pass moves it into its new
            // position, an incorrect scroll anchoring will occur. Instead signal that it's not yet
            // registered as a scroll anchor candidate.
            virtInfo.IsRegisteredAsAnchorCandidate = false;
        }

        public void OnElementCleared(IControl element)
        {
            _scroller?.UnregisterAnchorCandidate(element);
        }

        public void OnOwnerMeasuring()
        {
            // This is because of a bug that causes effective viewport to not
            // fire if you register during arrange.
            // Bug 17411076: EffectiveViewport: registering for effective viewport in arrange should invalidate viewport
            EnsureScroller();
        }

        public void OnOwnerArranged()
        {
            _expectedViewportShift = default;

            if (!_managingViewportDisabled)
            {
                // This is because of a bug that causes effective viewport to not 
                // fire if you register during arrange.
                // Bug 17411076: EffectiveViewport: registering for effective viewport in arrange should invalidate viewport
                // EnsureScroller();

                if (HasScroller)
                {
                    double maximumHorizontalCacheBufferPerSide = _maximumHorizontalCacheLength * _visibleWindow.Width / 2.0;
                    double maximumVerticalCacheBufferPerSide = _maximumVerticalCacheLength * _visibleWindow.Height / 2.0;

                    bool continueBuildingCache =
                        _horizontalCacheBufferPerSide < maximumHorizontalCacheBufferPerSide ||
                        _verticalCacheBufferPerSide < maximumVerticalCacheBufferPerSide;

                    if (continueBuildingCache)
                    {
                        _horizontalCacheBufferPerSide += CacheBufferPerSideInflationPixelDelta;
                        _verticalCacheBufferPerSide += CacheBufferPerSideInflationPixelDelta;

                        _horizontalCacheBufferPerSide = Math.Min(_horizontalCacheBufferPerSide, maximumHorizontalCacheBufferPerSide);
                        _verticalCacheBufferPerSide = Math.Min(_verticalCacheBufferPerSide, maximumVerticalCacheBufferPerSide);
                    }
                }
            }
        }

        private void OnLayoutUpdated(object sender, EventArgs args)
        {
            _owner.LayoutUpdated -= OnLayoutUpdated;
            _layoutUpdatedSubscribed = false;
            if (_managingViewportDisabled)
            {
                return;
            }

            // We were expecting a viewport shift but we never got one and we are not going to in this
            // layout pass. We likely will never get this shift, so lets assume that we are never going to get it and
            // adjust our expected shift to track that. One case where this can happen is when there is no scrollviewer
            // that can scroll in the direction where the shift is expected.
            if (_pendingViewportShift.X != 0 || _pendingViewportShift.Y != 0)
            {
                Logger.TryGet(LogEventLevel.Verbose, "Repeater")?.Log(this, "{LayoutId}: Layout Updated with pending shift {Shift}- invalidating measure",
                    _owner.Layout.LayoutId,
                    _pendingViewportShift);

                // Assume this is never going to come.
                _unshiftableShift = new Point(
                    _unshiftableShift.X + _pendingViewportShift.X,
                    _unshiftableShift.Y + _pendingViewportShift.Y);
                _pendingViewportShift = default;
                _expectedViewportShift = default;

                TryInvalidateMeasure();
            }
        }

        public void OnMakeAnchor(IControl anchor, bool isAnchorOutsideRealizedRange)
        {
            if (_makeAnchorElement != anchor)
            {
                _makeAnchorElement = anchor;
                _isAnchorOutsideRealizedRange = isAnchorOutsideRealizedRange;
            }
        }

        public void OnBringIntoViewRequested(RequestBringIntoViewEventArgs args)
        {
            if (!_managingViewportDisabled)
            {
                // During the time between a bring into view request and the element coming into view we do not
                // want the anchor provider to pick some anchor and jump to it. Instead we want to anchor on the
                // element that is being brought into view. We can do this by making just that element as a potential
                // anchor candidate and ensure no other element of this repeater is an anchor candidate.
                // Once the layout pass is done and we render the frame, the element will be in frame and we can
                // switch back to letting the anchor provider pick a suitable anchor.

                // get the targetChild - i.e the immediate child of this repeater that is being brought into view.
                // Note that the element being brought into view could be a descendant.
                var targetChild = GetImmediateChildOfRepeater((IControl)args.TargetObject);

                if (targetChild is null)
                {
                    return;
                }

                // Make sure that only the target child can be the anchor during the bring into view operation.
                if (_scroller is object)
                {
                    foreach (var child in _owner.Children)
                    {
                        if (child != targetChild)
                        {
                            _scroller.UnregisterAnchorCandidate(child);
                        }
                    }
                }

                // Register action to go back to how things were before where any child can be the anchor. Here,
                // WinUI uses CompositionTarget.Rendering but we don't currently have that, so post an action to
                // run *after* rendering has completed (priority needs to be lower than Render as Transformed
                // bounds must have been set in order for OnEffectiveViewportChanged to trigger).
                if (!_isBringIntoViewInProgress)
                {
                    _isBringIntoViewInProgress = true;
                    Dispatcher.UIThread.Post(OnCompositionTargetRendering, DispatcherPriority.Loaded);
                }
            }
        }

        public void RegisterScrollAnchorCandidate(IControl element)
        {
            _scroller?.RegisterAnchorCandidate(element);
        }

        private IControl GetImmediateChildOfRepeater(IControl descendant)
        {
            var targetChild = descendant;
            var parent = (IControl)descendant.VisualParent;
            while (parent != null && parent != _owner)
            {
                targetChild = parent;
                parent = (IControl)parent.VisualParent;
            }

            if (parent == null)
            {
                return null;
            }

            return targetChild;
        }

        private void OnCompositionTargetRendering()
        {
            _isBringIntoViewInProgress = false;
            _makeAnchorElement = null;

            if (_scroller is object)
            {
                foreach (var child in _owner.Children)
                {
                    var info = ItemsRepeater.GetVirtualizationInfo(child);

                    if (info.IsRealized && info.IsHeldByLayout)
                    {
                        _scroller.RegisterAnchorCandidate(child);
                    }
                }
            }

            // HACK: Invalidate measure now that the anchor has been removed so that a layout can be
            // done with a proper realization rect. This is a hack not present upstream to try to fix
            // https://github.com/microsoft/microsoft-ui-xaml/issues/1422
            TryInvalidateMeasure();
        }

        public void ResetScrollers()
        {
            if (_scroller is object)
            {
                foreach (var child in _owner.Children)
                {
                    _scroller.UnregisterAnchorCandidate(child);
                }

                _scroller = null;
            }

            _owner.EffectiveViewportChanged -= OnEffectiveViewportChanged;
            _effectiveViewportChangedSubscribed = false;
            _ensuredScroller = false;
        }

        private void OnEffectiveViewportChanged(object sender, EffectiveViewportChangedEventArgs e)
        {
            Logger.TryGet(LogEventLevel.Verbose, "Repeater")?.Log(this, "{LayoutId}: EffectiveViewportChanged event callback", _owner.Layout.LayoutId);
            UpdateViewport(e.EffectiveViewport);

            _pendingViewportShift = default;
            _unshiftableShift = default;
            if (_visibleWindow.IsEmpty)
            {
                // We got cleared.
                _layoutExtent = default;
            }

            // We got a new viewport, we dont need to wait for layout updated anymore to 
            // see if our request for a pending shift was handled.
            if (_layoutUpdatedSubscribed)
            {
                _owner.LayoutUpdated -= OnLayoutUpdated;
                _layoutUpdatedSubscribed = false;
            }
        }

        private void EnsureScroller()
        {
            if (!_ensuredScroller)
            {
                ResetScrollers();

                var parent = _owner.GetVisualParent();
                while (parent != null)
                {
                    if (parent is IScrollAnchorProvider scroller)
                    {
                        _scroller = scroller;
                        break;
                    }

                    parent = parent.VisualParent;
                }

                if (!_managingViewportDisabled)
                {
                    _owner.EffectiveViewportChanged += OnEffectiveViewportChanged;
                    _effectiveViewportChangedSubscribed = true;
                }

                _ensuredScroller = true;
            }
        }

        private void UpdateViewport(Rect viewport)
        {
            var currentVisibleWindow = viewport;
            var previousVisibleWindow = _visibleWindow;

            Logger.TryGet(LogEventLevel.Verbose, "Repeater")?.Log(this, "{LayoutId}: Effective Viewport: ({Before})->({After})",
                _owner.Layout.LayoutId,
                previousVisibleWindow,
                viewport);

            if (-currentVisibleWindow.X <= ItemsRepeater.ClearedElementsArrangePosition.X &&
                -currentVisibleWindow.Y <= ItemsRepeater.ClearedElementsArrangePosition.Y)
            {
                Logger.TryGet(LogEventLevel.Verbose, "Repeater")?.Log(this, "{LayoutId}: Viewport is invalid. visible window cleared", _owner.Layout.LayoutId);
                // We got cleared.
                _visibleWindow = default;
            }
            else
            {
                _visibleWindow = currentVisibleWindow;
            }

            if (_visibleWindow != previousVisibleWindow)
            {
                Logger.TryGet(LogEventLevel.Verbose, "Repeater")?.Log(this, "{LayoutId}: Used Viewport: ({Before})->({After})",
                    _owner.Layout.LayoutId,
                    previousVisibleWindow,
                    currentVisibleWindow);
                TryInvalidateMeasure();
            }
        }

        private static void ValidateCacheLength(double cacheLength)
        {
            if (cacheLength < 0.0 || double.IsInfinity(cacheLength) || double.IsNaN(cacheLength))
            {
                throw new ArgumentException("The maximum cache length must be equal or superior to zero.");
            }
        }

        private void TryInvalidateMeasure()
        {
            // Don't invalidate measure if we have an invalid window.
            if (!_visibleWindow.IsEmpty)
            {
                // We invalidate measure instead of just invalidating arrange because
                // we don't invalidate measure in UpdateViewport if the view is changing to
                // avoid layout cycles.
                Logger.TryGet(LogEventLevel.Verbose, "Repeater")?.Log(this, "{LayoutId}: Invalidating measure due to viewport change", _owner.Layout.LayoutId);
                _owner.InvalidateMeasure();
            }
        }

        private class ScrollerInfo
        {
            public ScrollerInfo(ScrollViewer scroller)
            {
                Scroller = scroller;
            }

            public ScrollViewer Scroller { get; }
        }
    };
}
