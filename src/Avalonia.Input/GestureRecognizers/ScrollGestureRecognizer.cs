using System;
using System.Diagnostics;
using Avalonia.Threading;

namespace Avalonia.Input.GestureRecognizers
{
    public class ScrollGestureRecognizer 
        : StyledElement, // It's not an "element" in any way, shape or form, but TemplateBinding refuse to work otherwise
            IGestureRecognizer
    {
        private bool _scrolling;
        private Point _trackedRootPoint;
        private IPointer? _tracking;
        private IInputElement? _target;
        private IGestureRecognizerActionsDispatcher? _actions;
        private bool _canHorizontallyScroll;
        private bool _canVerticallyScroll;
        private int _gestureId;
        
        // Movement per second
        private Vector _inertia;
        private ulong? _lastMoveTimestamp;
        
        /// <summary>
        /// Defines the <see cref="CanHorizontallyScroll"/> property.
        /// </summary>
        public static readonly DirectProperty<ScrollGestureRecognizer, bool> CanHorizontallyScrollProperty =
            AvaloniaProperty.RegisterDirect<ScrollGestureRecognizer, bool>(
                nameof(CanHorizontallyScroll),
                o => o.CanHorizontallyScroll,
                (o, v) => o.CanHorizontallyScroll = v);

        /// <summary>
        /// Defines the <see cref="CanVerticallyScroll"/> property.
        /// </summary>
        public static readonly DirectProperty<ScrollGestureRecognizer, bool> CanVerticallyScrollProperty =
            AvaloniaProperty.RegisterDirect<ScrollGestureRecognizer, bool>(
                nameof(CanVerticallyScroll),
                o => o.CanVerticallyScroll,
                (o, v) => o.CanVerticallyScroll = v);
        
        /// <summary>
        /// Gets or sets a value indicating whether the content can be scrolled horizontally.
        /// </summary>
        public bool CanHorizontallyScroll
        {
            get => _canHorizontallyScroll;
            set => SetAndRaise(CanHorizontallyScrollProperty, ref _canHorizontallyScroll, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether the content can be scrolled horizontally.
        /// </summary>
        public bool CanVerticallyScroll
        {
            get => _canVerticallyScroll;
            set => SetAndRaise(CanVerticallyScrollProperty, ref _canVerticallyScroll, value);
        }
        

        public void Initialize(IInputElement target, IGestureRecognizerActionsDispatcher actions)
        {
            _target = target;
            _actions = actions;
        }
        
        public void PointerPressed(PointerPressedEventArgs e)
        {
            if (e.Pointer.IsPrimary && e.Pointer.Type == PointerType.Touch)
            {
                EndGesture();
                _tracking = e.Pointer;
                _gestureId = ScrollGestureEventArgs.GetNextFreeId();;
                _trackedRootPoint = e.GetPosition(null);
            }
        }

        // Arbitrary chosen value, probably need to move that to platform settings or something
        private const double ScrollStartDistance = 30;
        
        // Pixels per second speed that is considered to be the stop of inertial scroll
        private const double InertialScrollSpeedEnd = 5;
        
        public void PointerMoved(PointerEventArgs e)
        {
            if (e.Pointer == _tracking)
            {
                var rootPoint = e.GetPosition(null);
                if (!_scrolling)
                {
                    if (CanHorizontallyScroll && Math.Abs(_trackedRootPoint.X - rootPoint.X) > ScrollStartDistance)
                        _scrolling = true;
                    if (CanVerticallyScroll && Math.Abs(_trackedRootPoint.Y - rootPoint.Y) > ScrollStartDistance)
                        _scrolling = true;
                    if (_scrolling)
                    {
                        _actions!.Capture(e.Pointer, this);
                    }
                }

                if (_scrolling)
                {
                    var vector = _trackedRootPoint - rootPoint;
                    var elapsed = _lastMoveTimestamp.HasValue ?
                        TimeSpan.FromMilliseconds(e.Timestamp - _lastMoveTimestamp.Value) :
                        TimeSpan.Zero;
                    
                    _lastMoveTimestamp = e.Timestamp;
                    _trackedRootPoint = rootPoint;
                    if (elapsed.TotalSeconds > 0)
                        _inertia = vector / elapsed.TotalSeconds;
                    _target!.RaiseEvent(new ScrollGestureEventArgs(_gestureId, vector));
                    e.Handled = true;
                }
            }
        }

        public void PointerCaptureLost(IPointer pointer)
        {
            if (pointer == _tracking) EndGesture();
        }

        void EndGesture()
        {
            _tracking = null;
            if (_scrolling)
            {
                _inertia = default;
                _scrolling = false;
                _target!.RaiseEvent(new ScrollGestureEndedEventArgs(_gestureId));
                _gestureId = 0;
                _lastMoveTimestamp = null;
            }
            
        }
        
        
        public void PointerReleased(PointerReleasedEventArgs e)
        {
            if (e.Pointer == _tracking && _scrolling)
            {
                e.Handled = true;
                if (_inertia == default
                    || e.Timestamp == 0
                    || _lastMoveTimestamp == 0
                    || e.Timestamp - _lastMoveTimestamp > 200)
                    EndGesture();
                else
                {
                    _tracking = null;
                    var savedGestureId = _gestureId;
                    var st = Stopwatch.StartNew();
                    var lastTime = TimeSpan.Zero;
                    DispatcherTimer.Run(() =>
                    {
                        // Another gesture has started, finish the current one
                        if (_gestureId != savedGestureId)
                        {
                            return false;
                        }

                        var elapsedSinceLastTick = st.Elapsed - lastTime;
                        lastTime = st.Elapsed;

                        var speed = _inertia * Math.Pow(0.15, st.Elapsed.TotalSeconds);
                        var distance = speed * elapsedSinceLastTick.TotalSeconds;
                        _target!.RaiseEvent(new ScrollGestureEventArgs(_gestureId, distance));



                        if (Math.Abs(speed.X) < InertialScrollSpeedEnd || Math.Abs(speed.Y) <= InertialScrollSpeedEnd)
                        {
                            EndGesture();
                            return false;
                        }

                        return true;
                    }, TimeSpan.FromMilliseconds(16), DispatcherPriority.Background);
                }
            }
        }
    }
}
