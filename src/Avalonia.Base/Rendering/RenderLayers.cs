using System.Collections;
using System.Collections.Generic;
using Avalonia.Platform;
using Avalonia.Rendering.SceneGraph;
using Avalonia.VisualTree;

namespace Avalonia.Rendering
{
    public class RenderLayers : IEnumerable<RenderLayer>
    {
        private readonly List<RenderLayer> _inner = new List<RenderLayer>();
        private readonly Dictionary<IVisual, RenderLayer> _index = new Dictionary<IVisual, RenderLayer>();
        
        public int Count => _inner.Count;
        public RenderLayer this[IVisual layerRoot] => _index[layerRoot];

        public void Update(Scene scene, IDrawingContextImpl context)
        {
            for (var i = scene.Layers.Count - 1; i >= 0; --i)
            {
                var src = scene.Layers[i];
                RenderLayer layer;

                if (!_index.TryGetValue(src.LayerRoot, out layer))
                {
                    layer = new RenderLayer(context, scene.Size, scene.Scaling, src.LayerRoot);
                    _inner.Add(layer);
                    _index.Add(src.LayerRoot, layer);
                }
                else
                {
                    layer.RecreateBitmap(context, scene.Size, scene.Scaling);
                }
            }

            for (var i = 0; i < _inner.Count;)
            {
                var layer = _inner[i];

                if (!scene.Layers.Exists(layer.LayerRoot))
                {
                    layer.Bitmap.Dispose();
                    _inner.RemoveAt(i);
                    _index.Remove(layer.LayerRoot);
                }
                else
                    i++;
            }
        }

        public void Clear()
        {
            foreach (var layer in _index.Values)
            {
                layer.Bitmap.Dispose();
            }

            _index.Clear();
            _inner.Clear();
        }

        public bool TryGetValue(IVisual layerRoot, out RenderLayer value)
        {
            return _index.TryGetValue(layerRoot, out value);
        }

        public IEnumerator<RenderLayer> GetEnumerator() => _inner.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
