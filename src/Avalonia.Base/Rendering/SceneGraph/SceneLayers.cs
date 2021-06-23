using System;
using System.Collections;
using System.Collections.Generic;
using Avalonia.VisualTree;

namespace Avalonia.Rendering.SceneGraph
{
    /// <summary>
    /// Holds a collection of layers for a <see cref="Scene"/>.
    /// </summary>
    public class SceneLayers : IEnumerable<SceneLayer>
    {
        private readonly IVisual _root;
        private readonly List<SceneLayer> _inner;
        private readonly Dictionary<IVisual, SceneLayer> _index;

        /// <summary>
        /// Initializes a new instance of the <see cref="SceneLayers"/> class.
        /// </summary>
        /// <param name="root">The scene's root visual.</param>
        public SceneLayers(IVisual root) : this(root, 0)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SceneLayers"/> class.
        /// </summary>
        /// <param name="root">The scene's root visual.</param>
        /// <param name="capacity">Initial layer capacity.</param>
        public SceneLayers(IVisual root, int capacity)
        {
            _root = root;

            _inner = new List<SceneLayer>(capacity);
            _index = new Dictionary<IVisual, SceneLayer>(capacity);
        }

        /// <summary>
        /// Gets the number of layers in the scene.
        /// </summary>
        public int Count => _inner.Count;

        /// <summary>
        /// Gets a value indicating whether any of the layers have a dirty region.
        /// </summary>
        public bool HasDirty
        {
            get
            {
                foreach (var layer in _inner)
                {
                    if (!layer.Dirty.IsEmpty)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        /// <summary>
        /// Gets a layer by index.
        /// </summary>
        /// <param name="index">The index of the layer.</param>
        /// <returns>The layer.</returns>
        public SceneLayer this[int index] => _inner[index];

        /// <summary>
        /// Gets a layer by its root visual.
        /// </summary>
        /// <param name="visual">The layer's root visual.</param>
        /// <returns>The layer.</returns>
        public SceneLayer this[IVisual visual] => _index[visual];

        /// <summary>
        /// Adds a layer to the scene.
        /// </summary>
        /// <param name="layerRoot">The root visual of the layer.</param>
        /// <returns>The created layer.</returns>
        public SceneLayer Add(IVisual layerRoot)
        {
            Contract.Requires<ArgumentNullException>(layerRoot != null);

            var distance = layerRoot.CalculateDistanceFromAncestor(_root);
            var layer = new SceneLayer(layerRoot, distance);
            var insert = FindInsertIndex(layer);
            _index.Add(layerRoot, layer);
            _inner.Insert(insert, layer);
            return layer;
        }

        /// <summary>
        /// Makes a deep clone of the layers.
        /// </summary>
        /// <returns>The cloned layers.</returns>
        public SceneLayers Clone()
        {
            var result = new SceneLayers(_root, Count);

            foreach (var src in _inner)
            {
                var dest = src.Clone();
                result._index.Add(dest.LayerRoot, dest);
                result._inner.Add(dest);
            }

            return result;
        }

        /// <summary>
        /// Tests whether a layer exists with the specified root visual.
        /// </summary>
        /// <param name="layerRoot">The root visual.</param>
        /// <returns>
        /// True if a layer exists with the specified root visual, otherwise false.
        /// </returns>
        public bool Exists(IVisual layerRoot)
        {
            Contract.Requires<ArgumentNullException>(layerRoot != null);

            return _index.ContainsKey(layerRoot);
        }

        /// <summary>
        /// Tries to find a layer with the specified root visual.
        /// </summary>
        /// <param name="layerRoot">The root visual.</param>
        /// <returns>The layer if found, otherwise null.</returns>
        public SceneLayer Find(IVisual layerRoot)
        {
            SceneLayer result;
            _index.TryGetValue(layerRoot, out result);
            return result;
        }

        /// <summary>
        /// Gets an existing layer or creates a new one if no existing layer is found.
        /// </summary>
        /// <param name="layerRoot">The root visual.</param>
        /// <returns>The layer.</returns>
        public SceneLayer GetOrAdd(IVisual layerRoot)
        {
            Contract.Requires<ArgumentNullException>(layerRoot != null);

            SceneLayer result;

            if (!_index.TryGetValue(layerRoot, out result))
            {
                result = Add(layerRoot);
            }

            return result;
        }

        /// <summary>
        /// Removes a layer from the scene.
        /// </summary>
        /// <param name="layerRoot">The root visual.</param>
        /// <returns>True if a matching layer was removed, otherwise false.</returns>
        public bool Remove(IVisual layerRoot)
        {
            Contract.Requires<ArgumentNullException>(layerRoot != null);

            SceneLayer layer;

            if (_index.TryGetValue(layerRoot, out layer))
            {
                Remove(layer);
            }

            return layer != null;
        }

        /// <summary>
        /// Removes a layer from the scene.
        /// </summary>
        /// <param name="layer">The layer.</param>
        /// <returns>True if the layer was part of the scene, otherwise false.</returns>
        public bool Remove(SceneLayer layer)
        {
            Contract.Requires<ArgumentNullException>(layer != null);

            _index.Remove(layer.LayerRoot);
            return _inner.Remove(layer);
        }

        /// <inheritdoc/>
        public IEnumerator<SceneLayer> GetEnumerator() => _inner.GetEnumerator();

        /// <inheritdoc/>
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        private int FindInsertIndex(SceneLayer insert)
        {
            var index = 0;

            foreach (var layer in _inner)
            {
                if (layer.DistanceFromRoot > insert.DistanceFromRoot)
                {
                    break;
                }

                ++index;
            }

            return index;
        }
    }
}
