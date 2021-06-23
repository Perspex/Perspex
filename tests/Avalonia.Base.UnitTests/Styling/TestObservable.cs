using System;
using System.Reactive.Disposables;

namespace Avalonia.Base.UnitTests.Styling
{
    public class TestObservable : IObservable<bool>
    {
        public int SubscribedCount { get; private set; }

        public IDisposable Subscribe(IObserver<bool> observer)
        {
            ++SubscribedCount;
            observer.OnNext(true);
            return Disposable.Create(() => --SubscribedCount);
        }
    }
}
