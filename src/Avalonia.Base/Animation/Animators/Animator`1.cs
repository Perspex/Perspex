using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using Avalonia.Animation.Utils;
using Avalonia.Collections;
using Avalonia.Data;
using Avalonia.Reactive;

namespace Avalonia.Animation.Animators
{
    /// <summary>
    /// Base class for <see cref="Animator{T}"/> objects
    /// </summary>
    public abstract class Animator<T> : AvaloniaList<AnimatorKeyFrame>, IAnimator
    {
        /// <summary>
        /// List of type-converted keyframes.
        /// </summary>
        private readonly List<AnimatorKeyFrame> _convertedKeyframes = new List<AnimatorKeyFrame>();

        private bool _isVerifiedAndConverted;

        /// <summary>
        /// Gets or sets the target property for the keyframe.
        /// </summary>
        public AvaloniaProperty Property { get; set; }

        public Animator()
        {
            // Invalidate keyframes when changed.
            this.CollectionChanged += delegate { _isVerifiedAndConverted = false; };
        }

        /// <inheritdoc/>
        public virtual IDisposable Apply(Animation animation, Animatable control, IClock clock, IObservable<bool> match, Action onComplete)
        {
            if (!_isVerifiedAndConverted)
                VerifyConvertKeyFrames();

            var subject = new DisposeAnimationInstanceSubject<T>(this, animation, control, clock, onComplete);
            return match.Subscribe(subject);
        }

        protected T InterpolationHandler(double animationTime, T neutralValue)
        {
            AnimatorKeyFrame firstKeyframe, lastKeyframe;

            int kvCount = _convertedKeyframes.Count;
            if (kvCount > 2)
            {
                if (animationTime <= 0.0)
                {
                    firstKeyframe = _convertedKeyframes[0];
                    lastKeyframe = _convertedKeyframes[1];
                }
                else if (animationTime >= 1.0)
                {
                    firstKeyframe = _convertedKeyframes[_convertedKeyframes.Count - 2];
                    lastKeyframe = _convertedKeyframes[_convertedKeyframes.Count - 1];
                }
                else
                {
                    int index = FindClosestBeforeKeyFrame(animationTime);
                    firstKeyframe = _convertedKeyframes[index];
                    lastKeyframe = _convertedKeyframes[index + 1];
                }
            }
            else
            {
                firstKeyframe = _convertedKeyframes[0];
                lastKeyframe = _convertedKeyframes[1];
            }

            double t0 = firstKeyframe.Cue.CueValue;
            double t1 = lastKeyframe.Cue.CueValue;

            double progress = (animationTime - t0) / (t1 - t0);

            T oldValue, newValue;

            if (firstKeyframe.isNeutral)
                oldValue = neutralValue;
            else
                oldValue = (T)firstKeyframe.Value;

            if (lastKeyframe.isNeutral)
                newValue = neutralValue;
            else
                newValue = (T)lastKeyframe.Value;

            if (lastKeyframe.KeySpline != null)
                progress = lastKeyframe.KeySpline.GetSplineProgress(progress);

            return Interpolate(progress, oldValue, newValue);
        }

        private int FindClosestBeforeKeyFrame(double time)
        {
            for (int i = 0; i < _convertedKeyframes.Count; i++)
                if (_convertedKeyframes[i].Cue.CueValue > time)
                    return i - 1;

            throw new Exception("Index time is out of keyframe time range.");
        }

        public virtual IDisposable BindAnimation(Animatable control, IObservable<T> instance)
        {
            return control.Bind((AvaloniaProperty<T>)Property, instance, BindingPriority.Animation);
        }

        /// <summary>
        /// Runs the KeyFrames Animation.
        /// </summary>
        internal IDisposable Run(Animation animation, Animatable control, IClock clock, Action onComplete)
        {
            var instance = new AnimationInstance<T>(
                animation,
                control,
                this,
                clock ?? control.Clock ?? Clock.GlobalClock,
                onComplete,
                InterpolationHandler);
            
            return BindAnimation(control, instance);
        }

        /// <summary>
        /// Interpolates in-between two key values given the desired progress time.
        /// </summary>
        public abstract T Interpolate(double progress, T oldValue, T newValue);

        private void VerifyConvertKeyFrames()
        {
            foreach (AnimatorKeyFrame keyframe in this)
            {
                _convertedKeyframes.Add(keyframe);
            }

            AddNeutralKeyFramesIfNeeded();

            _isVerifiedAndConverted = true;
        }

        private void AddNeutralKeyFramesIfNeeded()
        {
            bool hasStartKey, hasEndKey;
            hasStartKey = hasEndKey = false;

            // Check if there's start and end keyframes.
            foreach (var frame in _convertedKeyframes)
            {
                if (frame.Cue.CueValue == 0.0d)
                {
                    hasStartKey = true;
                }
                else if (frame.Cue.CueValue == 1.0d)
                {
                    hasEndKey = true;
                }
            }

            if (!hasStartKey || !hasEndKey)
                AddNeutralKeyFrames(hasStartKey, hasEndKey);
        }

        private void AddNeutralKeyFrames(bool hasStartKey, bool hasEndKey)
        {
            if (!hasStartKey)
            {
                _convertedKeyframes.Insert(0, new AnimatorKeyFrame(null, new Cue(0.0d)) { Value = default(T), isNeutral = true });
            }

            if (!hasEndKey)
            {
                _convertedKeyframes.Add(new AnimatorKeyFrame(null, new Cue(1.0d)) { Value = default(T), isNeutral = true });
            }
        }
    }
}
