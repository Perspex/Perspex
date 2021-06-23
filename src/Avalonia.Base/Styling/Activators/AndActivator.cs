#nullable enable

using System.Collections.Generic;

namespace Avalonia.Styling.Activators
{
    /// <summary>
    /// An aggregate <see cref="IStyleActivator"/> which is active when all of its inputs are
    /// active.
    /// </summary>
    internal class AndActivator : StyleActivatorBase, IStyleActivatorSink
    {
        private List<IStyleActivator>? _sources;
        private ulong _flags;
        private ulong _mask;

        public int Count => _sources?.Count ?? 0;

        public void Add(IStyleActivator activator)
        {
            _sources ??= new List<IStyleActivator>();
            _sources.Add(activator);
        }

        void IStyleActivatorSink.OnNext(bool value, int tag)
        {
            if (value)
            {
                _flags |= 1ul << tag;
            }
            else
            {
                _flags &= ~(1ul << tag);
            }

            if (_mask != 0)
            {
                PublishNext(_flags == _mask);
            }
        }

        protected override void Initialize()
        {
            if (_sources is object)
            {
                var i = 0;

                foreach (var source in _sources)
                {
                    source.Subscribe(this, i++);
                }

                _mask = (1ul << Count) - 1;
                PublishNext(_flags == _mask);
            }
        }

        protected override void Deinitialize()
        {
            if (_sources is object)
            {
                foreach (var source in _sources)
                {
                    source.Unsubscribe(this);
                }
            }

            _mask = 0;
        }
    }
}
