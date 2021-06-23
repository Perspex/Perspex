using System;
using System.Collections.Generic;
using System.Reactive.Subjects;
using Avalonia.Animation;
using Avalonia.Styling.Activators;

#nullable enable

namespace Avalonia.Styling
{
    /// <summary>
    /// A <see cref="Style"/> which has been instanced on a control.
    /// </summary>
    internal class StyleInstance : IStyleInstance, IStyleActivatorSink
    {
        private readonly List<ISetterInstance>? _setters;
        private readonly List<IDisposable>? _animations;
        private readonly IStyleActivator? _activator;
        private readonly Subject<bool>? _animationTrigger;

        public StyleInstance(
            IStyle source,
            IStyleable target,
            IReadOnlyList<ISetter>? setters,
            IReadOnlyList<IAnimation>? animations,
            IStyleActivator? activator = null)
        {
            Source = source ?? throw new ArgumentNullException(nameof(source));
            Target = target ?? throw new ArgumentNullException(nameof(target));
            _activator = activator;
            IsActive = _activator is null;

            if (setters is object)
            {
                var setterCount = setters.Count;

                _setters = new List<ISetterInstance>(setterCount);

                for (var i = 0; i < setterCount; ++i)
                {
                    _setters.Add(setters[i].Instance(Target));
                }
            }

            if (animations is object && target is Animatable animatable)
            {
                var animationsCount = animations.Count;

                _animations = new List<IDisposable>(animationsCount);
                _animationTrigger = new Subject<bool>();

                for (var i = 0; i < animationsCount; ++i)
                {
                    _animations.Add(animations[i].Apply(animatable, null, _animationTrigger));
                }
            }
        }

        public bool IsActive { get; private set; }
        public IStyle Source { get; }
        public IStyleable Target { get; }

        public void Start()
        {
            var hasActivator = _activator is object;

            if (_setters is object)
            {
                foreach (var setter in _setters)
                {
                    setter.Start(hasActivator);
                }
            }

            if (hasActivator)
            {
                _activator!.Subscribe(this, 0);
            }
            else if (_animationTrigger != null)
            {
                _animationTrigger.OnNext(true);
            }
        }

        public void Dispose()
        {
            if (_setters is object)
            {
                foreach (var setter in _setters)
                {
                    setter.Dispose();
                }
            }

            if (_animations is object)
            {
                foreach (var subscripion in _animations)
                {
                    subscripion.Dispose();
                }
            }

            _activator?.Dispose();
        }

        private void ActivatorChanged(bool value)
        {
            if (IsActive != value)
            {
                IsActive = value;

                _animationTrigger?.OnNext(value);

                if (_setters is object)
                {
                    if (IsActive)
                    {
                        foreach (var setter in _setters)
                        {
                            setter.Activate();
                        }
                    }
                    else
                    {
                        foreach (var setter in _setters)
                        {
                            setter.Deactivate();
                        }
                    }
                }
            }
        }

        void IStyleActivatorSink.OnNext(bool value, int tag) => ActivatorChanged(value);
    }
}
