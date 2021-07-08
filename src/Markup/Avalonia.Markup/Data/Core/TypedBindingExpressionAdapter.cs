using System;
using System.Reactive.Subjects;
using Avalonia.Reactive;

#nullable enable

namespace Avalonia.Data.Core
{
    internal class TypedBindingExpressionAdapter<T> : SingleSubscriberObservableBase<object?>,
        ISubject<object?>
    {
        private readonly ISubject<BindingValue<T>> _source;
        private readonly Inner _inner;
        private readonly bool _enableDataValidation;
        private IDisposable? _subscription;

        public TypedBindingExpressionAdapter(
            ISubject<BindingValue<T>> source,
            bool enableDataValidation)
        {
            _source = source;
            _inner = new Inner(this);
            _enableDataValidation = enableDataValidation;
        }

        public void OnCompleted() => _source.OnCompleted();
        public void OnError(Exception error) => _source.OnError(error);
#pragma warning disable CS8600
        public void OnNext(object? value) => _source.OnNext(new BindingValue<T>((T)value));
#pragma warning restore CS8600

        protected override void Subscribed()
        {
            _subscription = _source.Subscribe(_inner);
        }

        protected override void Unsubscribed()
        {
            _subscription?.Dispose();
            _subscription = null;
        }

        private class Inner : IObserver<BindingValue<T>>
        {
            private TypedBindingExpressionAdapter<T> _owner;

            public Inner(TypedBindingExpressionAdapter<T> owner)
            {
                _owner = owner;
            }

            public void OnCompleted() => _owner.OnCompleted();
            public void OnError(Exception error) => _owner.OnError(error);

            public void OnNext(BindingValue<T> value)
            {
                var finalValue = value.HasValue ? value.Value : AvaloniaProperty.UnsetValue;

                if (!_owner._enableDataValidation)
                {
                    _owner.PublishNext(finalValue);
                }
                else
                {
                    BindingNotification notification;

                    if (value.Error != null)
                    {
                        notification = new BindingNotification(
                            value.Error,
                            BindingErrorType.Error,
                            finalValue);
                    }
                    else
                    {
                        notification = new BindingNotification(value.Value);
                    }

                    _owner.PublishNext(notification);
                }
            }
        }
    }
}
