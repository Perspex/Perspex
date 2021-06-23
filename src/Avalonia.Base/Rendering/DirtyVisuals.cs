using System;
using System.Collections;
using System.Collections.Generic;
using Avalonia.VisualTree;

namespace Avalonia.Rendering
{
    /// <summary>
    /// Stores a list of dirty visuals for an <see cref="IRenderer"/>.
    /// </summary>
    /// <remarks>
    /// This class stores the dirty visuals for a scene, ordered by their distance to the root
    /// visual. TODO: We probably want to put an upper limit on the number of visuals that can be
    /// stored and if we reach that limit, assume all visuals are dirty.
    /// </remarks>
    internal class DirtyVisuals : IEnumerable<IVisual>
    {
        private SortedDictionary<int, List<IVisual>> _inner = new SortedDictionary<int, List<IVisual>>();
        private Dictionary<IVisual, int> _index = new Dictionary<IVisual, int>();
        private List<IVisual> _deferredChanges = new List<IVisual>();
        private int _deferring;

        /// <summary>
        /// Gets the number of dirty visuals.
        /// </summary>
        public int Count => _index.Count;

        /// <summary>
        /// Adds a visual to the dirty list.
        /// </summary>
        /// <param name="visual">The dirty visual.</param>
        public void Add(IVisual visual)
        {
            if (_deferring > 0)
            {
                _deferredChanges.Add(visual);
                return;
            }

            var distance = visual.CalculateDistanceFromAncestor(visual.VisualRoot);

            if (_index.TryGetValue(visual, out var existingDistance))
            {
                if (distance == existingDistance)
                {
                    return;
                }

                _inner[existingDistance].Remove(visual);
                _index.Remove(visual);
            }

            if (!_inner.TryGetValue(distance, out var list))
            {
                list = new List<IVisual>();
                _inner.Add(distance, list);
            }

            list.Add(visual);
            _index.Add(visual, distance);
        }

        /// <summary>
        /// Clears the list.
        /// </summary>
        public void Clear()
        {
            if (_deferring > 0)
            {
                throw new InvalidOperationException("Cannot clear while enumerating");
            }

            _inner.Clear();
            _index.Clear();
        }

        /// <summary>
        /// Gets the dirty visuals, in ascending order of distance to their root.
        /// </summary>
        /// <returns>A collection of visuals.</returns>
        public IEnumerator<IVisual> GetEnumerator()
        {
            BeginDefer();
            try
            {
                foreach (var i in _inner)
                {
                    foreach (var j in i.Value)
                    {
                        yield return j;
                    }
                }
            }
            finally
            {
                EndDefer();
            }
        }

        private void BeginDefer()
        {
            ++_deferring;
        }

        private void EndDefer()
        {
            if (--_deferring > 0) return;

            foreach (var visual in _deferredChanges)
            {
                Add(visual);
            }

            _deferredChanges.Clear();
        }

        /// <summary>
        /// Gets the dirty visuals, in ascending order of distance to their root.
        /// </summary>
        /// <returns>A collection of visuals.</returns>
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
