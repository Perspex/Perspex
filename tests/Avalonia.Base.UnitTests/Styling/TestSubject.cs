using System;
using System.Collections.Generic;
using System.Reactive.Disposables;

namespace Avalonia.Base.UnitTests.Styling
{
    internal class TestSubject<T> : IObserver<T>, IObservable<T>
    {
        private readonly T _initial;

        private readonly List<IObserver<T>> _subscribers = new List<IObserver<T>>();

        public TestSubject(T initial)
        {
            _initial = initial;
        }

        public int SubscriberCount => _subscribers.Count;

        public void OnCompleted()
        {
            foreach (IObserver<T> subscriber in _subscribers.ToArray())
            {
                subscriber.OnCompleted();
            }
        }

        public void OnError(Exception error)
        {
            foreach (IObserver<T> subscriber in _subscribers.ToArray())
            {
                subscriber.OnError(error);
            }
        }

        public void OnNext(T value)
        {
            foreach (IObserver<T> subscriber in _subscribers.ToArray())
            {
                subscriber.OnNext(value);
            }
        }

        public IDisposable Subscribe(IObserver<T> observer)
        {
            _subscribers.Add(observer);
            observer.OnNext(_initial);
            return Disposable.Create(() => _subscribers.Remove(observer));
        }
    }
}
