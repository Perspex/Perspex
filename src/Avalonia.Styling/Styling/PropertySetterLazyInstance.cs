using System;
using Avalonia.Data;
using Avalonia.Reactive;

#nullable enable

namespace Avalonia.Styling
{
    /// <summary>
    /// A <see cref="Setter"/> which has been instanced on a control and whose value is lazily
    /// evaluated.
    /// </summary>
    /// <typeparam name="T">The target property type.</typeparam>
    internal class PropertySetterLazyInstance<T> : SingleSubscriberObservableBase<BindingValue<T>>,
        ISetterInstance
    {
        private readonly IStyleable _target;
        private readonly StyledPropertyBase<T>? _styledProperty;
        private readonly DirectPropertyBase<T>? _directProperty;
        private readonly Func<T> _valueFactory;
        private BindingValue<T> _value;
        private IDisposable? _subscription;
        private bool _isActive;

        public PropertySetterLazyInstance(
            IStyleable target,
            StyledPropertyBase<T> property,
            Func<T> valueFactory)
        {
            _target = target;
            _styledProperty = property;
            _valueFactory = valueFactory;
        }

        public PropertySetterLazyInstance(
            IStyleable target,
            DirectPropertyBase<T> property,
            Func<T> valueFactory)
        {
            _target = target;
            _directProperty = property;
            _valueFactory = valueFactory;
        }

        public void Start(bool hasActivator)
        {
            _isActive = !hasActivator;

            if (_styledProperty is object)
            {
                var priority = hasActivator ? BindingPriority.StyleTrigger : BindingPriority.Style;
                _subscription = _target.Bind(_styledProperty, this, priority);
            }
            else
            {
                _subscription = _target.Bind(_directProperty!, this);
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

        private T GetValue()
        {
            if (_value.HasValue)
            {
                return _value.Value;
            }

            _value = _valueFactory();
            return _value.Value;
        }

        private void PublishNext()
        {
            if (_isActive)
            {
                GetValue();
                PublishNext(_value);
            }
            else
            {
                PublishNext(default);
            }
        }
    }
}
