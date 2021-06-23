using System;
using System.Collections;
using System.Collections.Generic;

namespace Avalonia.Rendering
{
    /// <summary>
    /// Holds a collection of <see cref="DisplayDirtyRect"/> objects and manages their aging.
    /// </summary>
    internal class DisplayDirtyRects : IEnumerable<DisplayDirtyRect>
    {
        private List<DisplayDirtyRect> _inner = new List<DisplayDirtyRect>();

        /// <summary>
        /// Adds new new dirty rect to the collection.
        /// </summary>
        /// <param name="rect"></param>
        public void Add(Rect rect)
        {
            foreach (var r in _inner)
            {
                if (r.Rect == rect)
                {
                    r.ResetLifetime();
                    return;
                }
            }

            _inner.Add(new DisplayDirtyRect(rect));
        }

        /// <summary>
        /// Removes dirty rects one they are no longer active.
        /// </summary>
        public void Tick()
        {
            var now = DateTimeOffset.UtcNow;

            for (var i = _inner.Count - 1; i >= 0; --i)
            {
                var r = _inner[i];

                if (now > r.Dies)
                {
                    _inner.RemoveAt(i);
                }
            }
        }

        /// <summary>
        /// Gets the dirty rects.
        /// </summary>
        /// <returns>A collection of <see cref="DisplayDirtyRect"/> objects.</returns>
        public IEnumerator<DisplayDirtyRect> GetEnumerator() => _inner.GetEnumerator();

        /// <summary>
        /// Gets the dirty rects.
        /// </summary>
        /// <returns>A collection of <see cref="DisplayDirtyRect"/> objects.</returns>
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
