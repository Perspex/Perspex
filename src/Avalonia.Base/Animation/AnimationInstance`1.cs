using System;
using System.Linq;
using System.Reactive.Linq;
using Avalonia.Animation.Animators;
using Avalonia.Animation.Utils;
using Avalonia.Data;
using Avalonia.Reactive;
using JetBrains.Annotations;

namespace Avalonia.Animation
{
    /// <summary>
    /// Handles interpolation and time-related functions 
    /// for keyframe animations.
    /// </summary>
    internal class AnimationInstance<T> : SingleSubscriberObservableBase<T>
    {
        private T _lastInterpValue;
        private T _firstKFValue;
        private ulong? _iterationCount;
        private ulong _currentIteration;
        private bool _gotFirstKFValue;
        private bool _playbackReversed;
        private FillMode _fillMode;
        private PlaybackDirection _playbackDirection;
        private Animator<T> _animator;
        private Animation _animation;
        private Animatable _targetControl;
        private T _neutralValue;
        private double _speedRatioConv;
        private TimeSpan _initialDelay;
        private TimeSpan _iterationDelay;
        private TimeSpan _duration;
        private Easings.Easing _easeFunc;
        private Action _onCompleteAction;
        private Func<double, T, T> _interpolator;
        private IDisposable _timerSub;
        private readonly IClock _baseClock;
        private IClock _clock;
        private EventHandler<AvaloniaPropertyChangedEventArgs> _propertyChangedDelegate;

        public AnimationInstance(Animation animation, Animatable control, Animator<T> animator, IClock baseClock, Action OnComplete, Func<double, T, T> Interpolator)
        {
            _animator = animator;
            _animation = animation;
            _targetControl = control;
            _onCompleteAction = OnComplete;
            _interpolator = Interpolator;
            _baseClock = baseClock;
            FetchProperties();
        }

        private void FetchProperties()
        {
            if (_animation.SpeedRatio < 0d)
                throw new ArgumentOutOfRangeException("SpeedRatio value should not be negative.");

            if (_animation.Duration.TotalSeconds <= 0)
                throw new InvalidOperationException("Duration value cannot be negative or zero.");

            _easeFunc = _animation.Easing;

            _speedRatioConv = 1d / _animation.SpeedRatio;

            _initialDelay = _animation.Delay;
            _duration = _animation.Duration;
            _iterationDelay = _animation.DelayBetweenIterations;

            if (_animation.IterationCount.RepeatType == IterationType.Many)
                _iterationCount = _animation.IterationCount.Value;
            else
                _iterationCount = null;

            _playbackDirection = _animation.PlaybackDirection;
            _fillMode = _animation.FillMode;
        }

        protected override void Unsubscribed()
        {
            // Animation may have been stopped before it has finished.
            ApplyFinalFill();

            _targetControl.PropertyChanged -= _propertyChangedDelegate;
            _timerSub?.Dispose();
            _clock.PlayState = PlayState.Stop;
        }

        protected override void Subscribed()
        {
            _clock = new Clock(_baseClock);
            _timerSub = _clock.Subscribe(Step);
            _propertyChangedDelegate ??= ControlPropertyChanged;
            _targetControl.PropertyChanged += _propertyChangedDelegate;
            UpdateNeutralValue();
        }

        public void Step(TimeSpan frameTick)
        {
            try
            {
                InternalStep(frameTick);
            }
            catch (Exception e)
            {
                PublishError(e);
            }
        }

        private void ApplyFinalFill()
        {
            if (_fillMode == FillMode.Forward || _fillMode == FillMode.Both)
                _targetControl.SetValue(_animator.Property, _lastInterpValue, BindingPriority.LocalValue);
        }

        private void DoComplete()
        {
            ApplyFinalFill();
            _onCompleteAction?.Invoke();
            PublishCompleted();
        }

        private void DoDelay()
        {
            if (_fillMode == FillMode.Backward || _fillMode == FillMode.Both)
                if (_currentIteration == 0)
                    PublishNext(_firstKFValue);
                else
                    PublishNext(_lastInterpValue);
        }

        private void DoPlayStates()
        {
            if (_clock.PlayState == PlayState.Stop || _baseClock.PlayState == PlayState.Stop)
                DoComplete();

            if (!_gotFirstKFValue)
            {
                _firstKFValue = (T)_animator.First().Value;
                _gotFirstKFValue = true;
            }
        }

        private void InternalStep(TimeSpan time)
        {
            DoPlayStates();

            FetchProperties();

            // Scale timebases according to speedratio.
            var indexTime = time.Ticks;
            var iterDuration = _duration.Ticks * _speedRatioConv;
            var iterDelay = _iterationDelay.Ticks * _speedRatioConv;
            var initDelay = _initialDelay.Ticks * _speedRatioConv;

            if (indexTime > 0 & indexTime <= initDelay)
            {
                DoDelay();
            }
            else
            {
                // Calculate timebases.
                var iterationTime = iterDuration + iterDelay;
                var opsTime = indexTime - initDelay;
                var playbackTime = opsTime % iterationTime;

                _currentIteration = (ulong)(opsTime / iterationTime);

                // Stop animation when the current iteration is beyond the iteration count
                // and snap the last iteration value to exact values.
                if ((_currentIteration + 1) > _iterationCount)
                {
                    var easedTime = _easeFunc.Ease(_playbackReversed ? 0.0 : 1.0);
                    _lastInterpValue = _interpolator(easedTime, _neutralValue);
                    DoComplete();
                }

                if (playbackTime <= iterDuration)
                {
                    // Normalize time for interpolation.
                    var normalizedTime = playbackTime / iterDuration;

                    // Check if normalized time needs to be reversed according to PlaybackDirection

                    switch (_playbackDirection)
                    {
                        case PlaybackDirection.Normal:
                            _playbackReversed = false;
                            break;
                        case PlaybackDirection.Reverse:
                            _playbackReversed = true;
                            break;
                        case PlaybackDirection.Alternate:
                            _playbackReversed = (_currentIteration % 2 == 0) ? false : true;
                            break;
                        case PlaybackDirection.AlternateReverse:
                            _playbackReversed = (_currentIteration % 2 == 0) ? true : false;
                            break;
                        default:
                            throw new InvalidOperationException($"Animation direction value is unknown: {_playbackDirection}");
                    }

                    if (_playbackReversed)
                        normalizedTime = 1 - normalizedTime;

                    // Ease and interpolate
                    var easedTime = _easeFunc.Ease(normalizedTime);
                    _lastInterpValue = _interpolator(easedTime, _neutralValue);

                    PublishNext(_lastInterpValue);
                }
                else if (playbackTime > iterDuration &
                         playbackTime <= iterationTime &
                         iterDelay > 0)
                {
                    // The last iteration's trailing delay should be skipped.
                    if ((_currentIteration + 1) < _iterationCount)
                        DoDelay();
                    else
                        DoComplete();
                }
            }
        }

        private void UpdateNeutralValue()
        {
            var property = _animator.Property;
            var baseValue = _targetControl.GetBaseValue(property, BindingPriority.LocalValue);

            _neutralValue = baseValue != AvaloniaProperty.UnsetValue ?
                (T)baseValue : (T)_targetControl.GetValue(property);
        }

        private void ControlPropertyChanged(object sender, AvaloniaPropertyChangedEventArgs e)
        {
            if (e.Property == _animator.Property && e.Priority > BindingPriority.Animation)
            {
                UpdateNeutralValue();
            }
        }
    }
}
