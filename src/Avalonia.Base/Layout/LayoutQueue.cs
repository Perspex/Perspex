using System;
using System.Collections;
using System.Collections.Generic;

namespace Avalonia.Layout
{
    internal class LayoutQueue<T> : IReadOnlyCollection<T>, IDisposable
    {
        private struct Info
        {
            public bool Active;
            public int Count;
        }

        public LayoutQueue(Func<T, bool> shouldEnqueue)
        {
            _shouldEnqueue = shouldEnqueue;
        }

        private readonly Func<T, bool> _shouldEnqueue;
        private readonly Queue<T> _inner = new Queue<T>();
        private readonly Dictionary<T, Info> _loopQueueInfo = new Dictionary<T, Info>();
        private readonly List<KeyValuePair<T, Info>> _notFinalizedBuffer = new List<KeyValuePair<T, Info>>();

        private int _maxEnqueueCountPerLoop = 1;

        public int Count => _inner.Count;

        public IEnumerator<T> GetEnumerator() => (_inner as IEnumerable<T>).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => _inner.GetEnumerator();

        public T Dequeue()
        {
            var result = _inner.Dequeue();

            if (_loopQueueInfo.TryGetValue(result, out var info))
            {
                info.Active = false;
                _loopQueueInfo[result] = info;
            }

            return result;
        }

        public void Enqueue(T item)
        {
            _loopQueueInfo.TryGetValue(item, out var info);

            if (!info.Active && info.Count < _maxEnqueueCountPerLoop)
            {
                _inner.Enqueue(item);
                _loopQueueInfo[item] = new Info() { Active = true, Count = info.Count + 1 };
            }
        }

        public void BeginLoop(int maxEnqueueCountPerLoop)
        {
            _maxEnqueueCountPerLoop = maxEnqueueCountPerLoop;
        }

        public void EndLoop()
        {
            foreach (KeyValuePair<T, Info> info in _loopQueueInfo)
            {
                if (info.Value.Count >= _maxEnqueueCountPerLoop)
                {
                    _notFinalizedBuffer.Add(info);
                }
            }

            _loopQueueInfo.Clear();

            // Prevent layout cycle but add to next layout the non arranged/measured items that might have caused cycle
            // one more time as a final attempt.
            foreach (var item in _notFinalizedBuffer)
            {
                if (_shouldEnqueue(item.Key))
                {
                    _loopQueueInfo[item.Key] = new Info() { Active = true, Count = 0 };
                    _inner.Enqueue(item.Key);
                }
            }

            _notFinalizedBuffer.Clear();
        }

        public void Dispose()
        {
            _inner.Clear();
            _loopQueueInfo.Clear();
            _notFinalizedBuffer.Clear();
        }
    }
}
