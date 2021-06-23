#nullable enable

using System.Collections.Generic;

namespace Avalonia.Styling.Activators
{
    /// <summary>
    /// An aggregate <see cref="IStyleActivator"/> which is active when any of its inputs are
    /// active.
    /// </summary>
    internal class OrActivator : StyleActivatorBase, IStyleActivatorSink
    {
        private List<IStyleActivator>? _sources;
        private ulong _flags;
        private bool _initializing;

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

            if (!_initializing)
            {
                PublishNext(_flags != 0);
            }
        }

        protected override void Initialize()
        {
            if (_sources is object)
            {
                var i = 0;

                _initializing = true;

                foreach (var source in _sources)
                {
                    source.Subscribe(this, i++);
                }

                _initializing = false;
                PublishNext(_flags != 0);
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
        }
    }
}
