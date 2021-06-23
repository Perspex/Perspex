using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Collections.Pooled;
using Avalonia.VisualTree;

namespace Avalonia.Rendering.SceneGraph
{
    /// <summary>
    /// Represents a scene graph used by the <see cref="DeferredRenderer"/>.
    /// </summary>
    public class Scene : IDisposable
    {
        private readonly Dictionary<IVisual, IVisualNode> _index;
        private readonly TaskCompletionSource<bool> _rendered = new TaskCompletionSource<bool>();

        /// <summary>
        /// Initializes a new instance of the <see cref="Scene"/> class.
        /// </summary>
        /// <param name="rootVisual">The root visual to draw.</param>
        public Scene(IVisual rootVisual)
            : this(
                new VisualNode(rootVisual, null),
                new Dictionary<IVisual, IVisualNode>(),
                new SceneLayers(rootVisual),
                0)
        {
            _index.Add(rootVisual, Root);
        }

        private Scene(VisualNode root, Dictionary<IVisual, IVisualNode> index, SceneLayers layers, int generation)
        {
            Contract.Requires<ArgumentNullException>(root != null);

            var renderRoot = root.Visual as IRenderRoot;

            _index = index;
            Root = root;
            Layers = layers;
            Generation = generation;
            root.LayerRoot = root.Visual;
        }

        public Task Rendered => _rendered.Task;

        /// <summary>
        /// Gets a value identifying the scene's generation. This is incremented each time the scene is cloned.
        /// </summary>
        public int Generation { get; }

        /// <summary>
        /// Gets the layers for the scene.
        /// </summary>
        public SceneLayers Layers { get; }

        /// <summary>
        /// Gets the root node of the scene graph.
        /// </summary>
        public IVisualNode Root { get; }

        /// <summary>
        /// Gets or sets the size of the scene in device independent pixels.
        /// </summary>
        public Size Size { get; set; }

        /// <summary>
        /// Gets or sets the scene scaling.
        /// </summary>
        public double Scaling { get; set; } = 1;

        /// <summary>
        /// Adds a node to the scene index.
        /// </summary>
        /// <param name="node">The node.</param>
        public void Add(IVisualNode node)
        {
            Contract.Requires<ArgumentNullException>(node != null);

            _index.Add(node.Visual, node);
        }

        /// <summary>
        /// Clones the scene.
        /// </summary>
        /// <returns>The cloned scene.</returns>
        public Scene CloneScene()
        {
            var index = new Dictionary<IVisual, IVisualNode>(_index.Count);
            var root = Clone((VisualNode)Root, null, index);

            var result = new Scene(root, index, Layers.Clone(), Generation + 1)
            {
                Size = Size,
                Scaling = Scaling,
            };

            return result;
        }

        public void Dispose()
        {
            _rendered.TrySetResult(false);
            foreach (var node in _index.Values)
            {
                node.Dispose();
            }
        }

        /// <summary>
        /// Tries to find a node in the scene graph representing the specified visual.
        /// </summary>
        /// <param name="visual">The visual.</param>
        /// <returns>
        /// The node representing the visual or null if it could not be found.
        /// </returns>
        public IVisualNode FindNode(IVisual visual)
        {
            IVisualNode node;
            _index.TryGetValue(visual, out node);
            return node;
        }

        /// <summary>
        /// Gets the visuals at a point in the scene.
        /// </summary>
        /// <param name="p">The point.</param>
        /// <param name="root">The root of the subtree to search.</param>
        /// <param name="filter">A filter. May be null.</param>
        /// <returns>The visuals at the specified point.</returns>
        public IEnumerable<IVisual> HitTest(Point p, IVisual root, Func<IVisual, bool> filter)
        {
            var node = FindNode(root);
            return (node != null) ? new HitTestEnumerable(node, filter, p, Root) : Enumerable.Empty<IVisual>();
        }

        /// <summary>
        /// Gets the visual at a point in the scene.
        /// </summary>
        /// <param name="p">The point.</param>
        /// <param name="root">The root of the subtree to search.</param>
        /// <param name="filter">A filter. May be null.</param>
        /// <returns>The visual at the specified point.</returns>
        public IVisual HitTestFirst(Point p, IVisual root, Func<IVisual, bool> filter)
        {
            var node = FindNode(root);
            return (node != null) ? HitTestFirst(node, p, filter) : null;
        }

        /// <summary>
        /// Removes a node from the scene index.
        /// </summary>
        /// <param name="node">The node.</param>
        public void Remove(IVisualNode node)
        {
            Contract.Requires<ArgumentNullException>(node != null);

            _index.Remove(node.Visual);

            node.Dispose();
        }

        private VisualNode Clone(VisualNode source, IVisualNode parent, Dictionary<IVisual, IVisualNode> index)
        {
            var result = source.Clone(parent);

            index.Add(result.Visual, result);

            var children = source.Children;
            var childrenCount = children.Count;

            if (childrenCount > 0)
            {
                result.TryPreallocateChildren(childrenCount);

                for (var i = 0; i < childrenCount; i++)
                {
                    var child = children[i];

                    result.AddChild(Clone((VisualNode)child, result, index));
                }
            }

            return result;
        }

        private IVisual HitTestFirst(IVisualNode root, Point p, Func<IVisual, bool> filter)
        {
            using var enumerator = new HitTestEnumerator(root, filter, p, Root);

            enumerator.MoveNext();

            return enumerator.Current;
        }

        private class HitTestEnumerable : IEnumerable<IVisual>
        {
            private readonly IVisualNode _root;
            private readonly Func<IVisual, bool> _filter;
            private readonly IVisualNode _sceneRoot;
            private readonly Point _point;
            
            public HitTestEnumerable(IVisualNode root, Func<IVisual, bool> filter, Point point, IVisualNode sceneRoot)
            {
                _root = root;
                _filter = filter;
                _point = point;
                _sceneRoot = sceneRoot;
            }

            public IEnumerator<IVisual> GetEnumerator()
            {
                return new HitTestEnumerator(_root, _filter, _point, _sceneRoot);
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }

        private struct HitTestEnumerator : IEnumerator<IVisual>
        {
            private readonly PooledStack<Entry> _nodeStack;
            private readonly Func<IVisual, bool> _filter;
            private readonly IVisualNode _sceneRoot;
            private IVisual _current;
            private readonly Point _point;

            public HitTestEnumerator(IVisualNode root, Func<IVisual, bool> filter, Point point, IVisualNode sceneRoot)
            {
                _nodeStack = new PooledStack<Entry>();
                _nodeStack.Push(new Entry(root, false, null, true));

                _filter = filter;
                _point = point;
                _sceneRoot = sceneRoot;

                _current = null;
            }

            public bool MoveNext()
            {
                while (_nodeStack.Count > 0)
                {
                    (var wasVisited, var isRoot, IVisualNode node, Rect? clip) = _nodeStack.Pop();

                    if (wasVisited && isRoot)
                    {
                        break;
                    }

                    var children = node.Children;
                    int childCount = children.Count;

                    if (childCount == 0 || wasVisited)
                    {
                        if ((wasVisited || FilterAndClip(node, ref clip)) &&
                            (node.Visual is ICustomHitTest custom ? custom.HitTest(_point) : node.HitTest(_point)))
                        {
                            _current = node.Visual;

                            return true;
                        }
                    }
                    else if (FilterAndClip(node, ref clip))
                    {
                        _nodeStack.Push(new Entry(node, true, null));

                        for (var i = 0; i < childCount; i++)
                        {
                            _nodeStack.Push(new Entry(children[i], false, clip));
                        }
                    }
                }

                return false;
            }

            public void Reset()
            {
                throw new NotSupportedException();
            }

            public IVisual Current => _current;

            object IEnumerator.Current => Current;

            public void Dispose()
            {
                _nodeStack.Dispose();
            }

            private bool FilterAndClip(IVisualNode node, ref Rect? clip)
            {
                if (_filter?.Invoke(node.Visual) != false && node.Visual.IsAttachedToVisualTree)
                {
                    var clipped = false;

                    if (node.ClipToBounds)
                    {
                        clip = clip == null ? node.ClipBounds : clip.Value.Intersect(node.ClipBounds);
                        clipped = !clip.Value.Contains(_point);
                    }

                    if (node.GeometryClip != null)
                    {
                        var controlPoint = _sceneRoot.Visual.TranslatePoint(_point, node.Visual);
                        clipped = !node.GeometryClip.FillContains(controlPoint.Value);
                    }

                    if (!clipped && node.Visual is ICustomHitTest custom)
                    {
                        clipped = !custom.HitTest(_point);
                    }

                    return !clipped;
                }

                return false;
            }

            private readonly struct Entry
            {
                public readonly bool WasVisited;
                public readonly bool IsRoot;
                public readonly IVisualNode Node;
                public readonly Rect? Clip;

                public Entry(IVisualNode node, bool wasVisited, Rect? clip, bool isRoot = false)
                {
                    Node = node;
                    WasVisited = wasVisited;
                    IsRoot = isRoot;
                    Clip = clip;
                }

                public void Deconstruct(out bool wasVisited, out bool isRoot, out IVisualNode node, out Rect? clip)
                {
                    wasVisited = WasVisited;
                    isRoot = IsRoot;
                    node = Node;
                    clip = Clip;
                }
            }
        }

        public void MarkAsRendered() => _rendered.TrySetResult(true);
    }
}
