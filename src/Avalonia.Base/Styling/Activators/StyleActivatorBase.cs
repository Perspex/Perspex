#nullable enable

namespace Avalonia.Styling.Activators
{
    /// <summary>
    /// Base class implementation of <see cref="IStyleActivator"/>.
    /// </summary>
    internal abstract class StyleActivatorBase : IStyleActivator
    {
        private IStyleActivatorSink? _sink;
        private int _tag;
        private bool? _value;

        public void Subscribe(IStyleActivatorSink sink, int tag = 0)
        {
            if (_sink is null)
            {
                _sink = sink;
                _tag = tag;
                _value = null;
                Initialize();
            }
            else
            {
                throw new AvaloniaInternalException("Cannot subscribe to a StyleActivator more than once.");
            }
        }

        public void Unsubscribe(IStyleActivatorSink sink)
        {
            if (_sink != sink)
            {
                throw new AvaloniaInternalException("StyleActivatorSink is not subscribed.");
            }

            _sink = null;
            Deinitialize();
        }

        public void PublishNext(bool value)
        {
            if (_value != value)
            {
                _value = value;
                _sink?.OnNext(value, _tag);
            }
        }

        public void Dispose()
        {
            _sink = null;
            Deinitialize();
        }

        protected abstract void Initialize();
        protected abstract void Deinitialize();
    }
}
