using System;
using Avalonia.Data;
using Avalonia.Reactive;

#nullable enable

namespace Avalonia.Styling
{
    /// <summary>
    /// A <see cref="Setter"/> which has been instanced on a control.
    /// </summary>
    /// <typeparam name="T">The target property type.</typeparam>
    internal class PropertySetterInstance<T> : SingleSubscriberObservableBase<BindingValue<T>>,
        ISetterInstance
    {
        private readonly IStyleable _target;
        private readonly StyledPropertyBase<T>? _styledProperty;
        private readonly DirectPropertyBase<T>? _directProperty;
        private readonly T _value;
        private IDisposable? _subscription;
        private bool _isActive;

        public PropertySetterInstance(
            IStyleable target,
            StyledPropertyBase<T> property,
            T value)
        {
            _target = target;
            _styledProperty = property;
            _value = value;
        }

        public PropertySetterInstance(
            IStyleable target,
            DirectPropertyBase<T> property,
            T value)
        {
            _target = target;
            _directProperty = property;
            _value = value;
        }

        public void Start(bool hasActivator)
        {
            if (hasActivator)
            {
                if (_styledProperty is object)
                {
                    _subscription = _target.Bind(_styledProperty, this, BindingPriority.StyleTrigger);
                }
                else
                {
                    _subscription = _target.Bind(_directProperty!, this);
                }
            }
            else
            {
                if (_styledProperty is object)
                {
                    _subscription = _target.SetValue(_styledProperty, _value, BindingPriority.Style);
                }
                else
                {
                    _target.SetValue(_directProperty!, _value);
                }
            }
        }

        public void Activate()
        {
            if (!_isActive)
            {
                _isActive = true;
                PublishNext();
            }
        }

        public void Deactivate()
        {
            if (_isActive)
            {
                _isActive = false;
                PublishNext();
            }
        }

        public override void Dispose()
        {
            if (_subscription is object)
            {
                var sub = _subscription;
                _subscription = null;
                sub.Dispose();
            }
            else if (_isActive)
            {
                if (_styledProperty is object)
                {
                    _target.ClearValue(_styledProperty);
                }
                else
                {
                    _target.ClearValue(_directProperty!);
                }
            }

            base.Dispose();
        }

        protected override void Subscribed() => PublishNext();
        protected override void Unsubscribed() { }

        private void PublishNext()
        {
            PublishNext(_isActive ? new BindingValue<T>(_value) : default);
        }
    }
}
