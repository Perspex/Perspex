using System.Collections;
using System.Collections.Generic;
using Avalonia.VisualTree;

namespace Avalonia.Rendering
{
    /// <summary>
    /// Tracks dirty rectangles.
    /// </summary>
    internal class DirtyRects : IEnumerable<Rect>
    {
        private List<Rect> _rects = new List<Rect>();
        private IVisual _root;

        public DirtyRects(IVisual root = null)
        {
            _root = root;
        }

        public bool IsEmpty => _rects.Count == 0;

        /// <summary>
        /// Adds a dirty rectangle, extending an existing dirty rectangle if it intersects.
        /// </summary>
        /// <param name="rect">The dirt rectangle.</param>
        /// <remarks>
        /// We probably want to do this more intelligently because:
        /// - Adding e.g. the top left quarter of a scene and the bottom left quarter of a scene
        ///   will cause the whole scene to be invalidated if they overlap by a single pixel
        /// - Adding two adjacent rectangles that don't overlap will not cause them to be 
        /// coalesced
        /// - It only coalesces the first intersecting rectangle found - one needs to
        ///  call <see cref="Coalesce"/> at the end of the draw cycle to coalesce the rest.
        /// </remarks>
        public void Add(Rect rect)
        {
            if (_root != null)
            {
                rect = _root.Bounds;
                _rects.Add(rect);
                return;
            }

            if (!rect.IsEmpty)
            {
                for (var i = 0; i < _rects.Count; ++i)
                {
                    var r = _rects[i];

                    if (r.Intersects(rect))
                    {
                        _rects[i] = r.Union(rect);
                        return;
                    }
                }

                _rects.Add(rect);
            }
        }

        /// <summary>
        /// Works around our flimsy dirt-rect coalescing algorithm.
        /// </summary>
        /// <remarks>
        /// See the comments in <see cref="Add(Rect)"/>.
        /// </remarks>
        public void Coalesce()
        {
            for (var i = _rects.Count - 1; i >= 0; --i)
            {
                var a = _rects[i];

                for (var j = 0; j < i; ++j)
                {
                    var b = _rects[j];

                    if (i < _rects.Count && a.Intersects(b))
                    {
                        _rects[i] = _rects[i].Union(b);
                        _rects.RemoveAt(i);
                    }
                }
            }
        }

        /// <summary>
        /// Gets the dirty rectangles.
        /// </summary>
        /// <returns>A collection of dirty rectangles</returns>
        public IEnumerator<Rect> GetEnumerator() => _rects.GetEnumerator();

        /// <summary>
        /// Gets the dirty rectangles.
        /// </summary>
        /// <returns>A collection of dirty rectangles</returns>
        IEnumerator IEnumerable.GetEnumerator() => _rects.GetEnumerator();
    }
}
