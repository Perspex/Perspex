using System;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Avalonia.Reactive;
using Avalonia.Styling.Activators;

namespace Avalonia.Base.UnitTests.Styling
{
    internal static class StyleActivatorExtensions
    {
        public static IDisposable Subscribe(this IStyleActivator activator, Action<bool> action)
        {
            return activator.ToObservable().Subscribe(action);
        }

        public static async Task<bool> Take(this IStyleActivator activator, int value)
        {
            return await activator.ToObservable().Take(value);
        }

        public static IObservable<bool> ToObservable(this IStyleActivator activator)
        {
            return new ObservableAdapter(activator);
        }

        private class ObservableAdapter : LightweightObservableBase<bool>, IStyleActivatorSink
        {
            private readonly IStyleActivator _source;
            private bool _value;
            
            public ObservableAdapter(IStyleActivator source) => _source = source;
            protected override void Initialize() => _source.Subscribe(this);
            protected override void Deinitialize() => _source.Unsubscribe(this);

            void IStyleActivatorSink.OnNext(bool value, int tag)
            {
                _value = value;
                PublishNext(value);
            }
        }
    }
}
