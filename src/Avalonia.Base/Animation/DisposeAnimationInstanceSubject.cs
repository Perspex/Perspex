using System;
using Avalonia.Animation.Animators;

namespace Avalonia.Animation
{
    /// <summary>
    /// Manages the lifetime of animation instances as determined by its selector state.
    /// </summary>
    internal class DisposeAnimationInstanceSubject<T> : IObserver<bool>, IDisposable
    {
        private IDisposable _lastInstance;
        private bool _lastMatch;
        private Animator<T> _animator;
        private Animation _animation;
        private Animatable _control;
        private Action _onComplete;
        private IClock _clock;

        public DisposeAnimationInstanceSubject(Animator<T> animator, Animation animation, Animatable control, IClock clock, Action onComplete)
        {
            this._animator = animator;
            this._animation = animation;
            this._control = control;
            this._onComplete = onComplete;
            this._clock = clock;
        }
        public void Dispose()
        {
            _lastInstance?.Dispose();
        }

        public void OnCompleted()
        {
        }

        public void OnError(Exception error)
        {
            _lastInstance?.Dispose();
            _lastInstance = null;
        }

        void IObserver<bool>.OnNext(bool matchVal)
        {
            if (matchVal != _lastMatch)
            {
                _lastInstance?.Dispose();

                if (matchVal)
                {
                    _lastInstance = _animator.Run(_animation, _control, _clock, _onComplete);
                }
                else
                {
                    _lastInstance = null;
                }

                _lastMatch = matchVal;
            }
        }
    }
}
