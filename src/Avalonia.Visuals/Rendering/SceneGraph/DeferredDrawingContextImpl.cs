using System;
using System.Collections.Generic;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Utilities;
using Avalonia.Visuals.Media.Imaging;
using Avalonia.VisualTree;

namespace Avalonia.Rendering.SceneGraph
{
    /// <summary>
    /// A drawing context which builds a scene graph.
    /// </summary>
    internal class DeferredDrawingContextImpl : IDrawingContextImpl, IDrawingContextWithAcrylicLikeSupport
    {
        private readonly ISceneBuilder _sceneBuilder;
        private VisualNode _node;
        private int _childIndex;
        private int _drawOperationindex;

        /// <summary>
        /// Initializes a new instance of the <see cref="DeferredDrawingContextImpl"/> class.
        /// </summary>
        /// <param name="sceneBuilder">
        /// A scene builder used for constructing child scenes for visual brushes.
        /// </param>
        /// <param name="layers">The scene layers.</param>
        public DeferredDrawingContextImpl(ISceneBuilder sceneBuilder, SceneLayers layers)
        {
            _sceneBuilder = sceneBuilder;
            Layers = layers;
        }

        /// <inheritdoc/>
        public Matrix Transform { get; set; } = Matrix.Identity;

        /// <summary>
        /// Gets the layers in the scene being built.
        /// </summary>
        public SceneLayers Layers { get; }

        /// <summary>
        /// Informs the drawing context of the visual node that is about to be rendered.
        /// </summary>
        /// <param name="node">The visual node.</param>
        /// <returns>
        /// An object which when disposed will commit the changes to visual node.
        /// </returns>
        public UpdateState BeginUpdate(VisualNode node)
        {
            Contract.Requires<ArgumentNullException>(node != null);

            if (_node != null)
            {
                if (_childIndex < _node.Children.Count)
                {
                    _node.ReplaceChild(_childIndex, node);
                }
                else
                {
                    _node.AddChild(node);
                }

                ++_childIndex;
            }

            var state = new UpdateState(this, _node, _childIndex, _drawOperationindex);
            _node = node;
            _childIndex = _drawOperationindex = 0;
            return state;
        }

        /// <inheritdoc/>
        public void Clear(Color color)
        {
            // Cannot clear a deferred scene.
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            // Nothing to do here since we allocate no unmanaged resources.
        }

        /// <summary>
        /// Removes any remaining drawing operations from the visual node.
        /// </summary>
        /// <remarks>
        /// Drawing operations are updated in place, overwriting existing drawing operations if
        /// they are different. Once drawing has completed for the current visual node, it is
        /// possible that there are stale drawing operations at the end of the list. This method
        /// trims these stale drawing operations.
        /// </remarks>
        public void TrimChildren()
        {
            _node.TrimChildren(_childIndex);
        }

        /// <inheritdoc/>
        public void DrawGeometry(IBrush brush, IPen pen, IGeometryImpl geometry)
        {
            var next = NextDrawAs<GeometryNode>();

            if (next == null || !next.Item.Equals(Transform, brush, pen, geometry))
            {
                Add(new GeometryNode(Transform, brush, pen, geometry, CreateChildScene(brush)));
            }
            else
            {
                ++_drawOperationindex;
            }
        }

        /// <inheritdoc/>
        public void DrawBitmap(IRef<IBitmapImpl> source, double opacity, Rect sourceRect, Rect destRect, BitmapInterpolationMode bitmapInterpolationMode)
        {
            var next = NextDrawAs<ImageNode>();

            if (next == null || !next.Item.Equals(Transform, source, opacity, sourceRect, destRect, bitmapInterpolationMode))
            {
                Add(new ImageNode(Transform, source, opacity, sourceRect, destRect, bitmapInterpolationMode));
            }
            else
            {
                ++_drawOperationindex;
            }
        }

        /// <inheritdoc/>
        public void DrawBitmap(IRef<IBitmapImpl> source, IBrush opacityMask, Rect opacityMaskRect, Rect sourceRect)
        {
            // This method is currently only used to composite layers so shouldn't be called here.
            throw new NotSupportedException();
        }

        /// <inheritdoc/>
        public void DrawLine(IPen pen, Point p1, Point p2)
        {
            var next = NextDrawAs<LineNode>();

            if (next == null || !next.Item.Equals(Transform, pen, p1, p2))
            {
                Add(new LineNode(Transform, pen, p1, p2, CreateChildScene(pen.Brush)));
            }
            else
            {
                ++_drawOperationindex;
            }
        }

        /// <inheritdoc/>
        public void DrawRectangle(IBrush brush, IPen pen, RoundedRect rect,
            BoxShadows boxShadows = default)
        {
            var next = NextDrawAs<RectangleNode>();

            if (next == null || !next.Item.Equals(Transform, brush, pen, rect, boxShadows))
            {
                Add(new RectangleNode(Transform, brush, pen, rect, boxShadows, CreateChildScene(brush)));
            }
            else
            {
                ++_drawOperationindex;
            }
        }

        /// <inheritdoc/>
        public void DrawRectangle(IExperimentalAcrylicMaterial material, RoundedRect rect)
        {
            var next = NextDrawAs<ExperimentalAcrylicNode>();

            if (next == null || !next.Item.Equals(Transform, material, rect))
            {
                Add(new ExperimentalAcrylicNode(Transform, material, rect));
            }
            else
            {
                ++_drawOperationindex;
            }
        }

        public void Custom(ICustomDrawOperation custom)
        {
            var next = NextDrawAs<CustomDrawOperation>();
            if (next == null || !next.Item.Equals(Transform, custom))
                Add(new CustomDrawOperation(custom, Transform));
            else
                ++_drawOperationindex;
        }

        /// <inheritdoc/>
        public void DrawGlyphRun(IBrush foreground, GlyphRun glyphRun)
        {
            var next = NextDrawAs<GlyphRunNode>();

            if (next == null || !next.Item.Equals(Transform, foreground, glyphRun))
            {
                Add(new GlyphRunNode(Transform, foreground, glyphRun, CreateChildScene(foreground)));
            }

            else
            {
                ++_drawOperationindex;
            }
        }
        public IDrawingContextLayerImpl CreateLayer(Size size)
        {
            throw new NotSupportedException("Creating layers on a deferred drawing context not supported");
        }

        /// <inheritdoc/>
        public void PopClip()
        {
            var next = NextDrawAs<ClipNode>();

            if (next == null || !next.Item.Equals(null))
            {
                Add(new ClipNode());
            }
            else
            {
                ++_drawOperationindex;
            }
        }

        /// <inheritdoc/>
        public void PopGeometryClip()
        {
            var next = NextDrawAs<GeometryClipNode>();

            if (next == null || !next.Item.Equals(null))
            {
                Add((new GeometryClipNode()));
            }
            else
            {
                ++_drawOperationindex;
            }
        }

        /// <inheritdoc/>
        public void PopBitmapBlendMode()
        {
            var next = NextDrawAs<BitmapBlendModeNode>();

            if (next == null || !next.Item.Equals(null))
            {
                Add(new BitmapBlendModeNode());
            }
            else
            {
                ++_drawOperationindex;
            }
        }
        
        /// <inheritdoc/>
        public void PopOpacity()
        {
            var next = NextDrawAs<OpacityNode>();

            if (next == null || !next.Item.Equals(null))
            {
                Add(new OpacityNode());
            }
            else
            {
                ++_drawOperationindex;
            }
        }

        /// <inheritdoc/>
        public void PopOpacityMask()
        {
            var next = NextDrawAs<OpacityMaskNode>();

            if (next == null || !next.Item.Equals(null, null))
            {
                Add(new OpacityMaskNode());
            }
            else
            {
                ++_drawOperationindex;
            }
        }

        /// <inheritdoc/>
        public void PushClip(Rect clip)
        {
            var next = NextDrawAs<ClipNode>();

            if (next == null || !next.Item.Equals(clip))
            {
                Add(new ClipNode(clip));
            }
            else
            {
                ++_drawOperationindex;
            }
        }

        /// <inheritdoc />
        public void PushClip(RoundedRect clip)
        {
            var next = NextDrawAs<ClipNode>();

            if (next == null || !next.Item.Equals(clip))
            {
                Add(new ClipNode(clip));
            }
            else
            {
                ++_drawOperationindex;
            }
        }

        /// <inheritdoc/>
        public void PushGeometryClip(IGeometryImpl clip)
        {
            var next = NextDrawAs<GeometryClipNode>();

            if (next == null || !next.Item.Equals(clip))
            {
                Add(new GeometryClipNode(clip));
            }
            else
            {
                ++_drawOperationindex;
            }
        }

        /// <inheritdoc/>
        public void PushOpacity(double opacity)
        {
            var next = NextDrawAs<OpacityNode>();

            if (next == null || !next.Item.Equals(opacity))
            {
                Add(new OpacityNode(opacity));
            }
            else
            {
                ++_drawOperationindex;
            }
        }

        /// <inheritdoc/>
        public void PushOpacityMask(IBrush mask, Rect bounds)
        {
            var next = NextDrawAs<OpacityMaskNode>();

            if (next == null || !next.Item.Equals(mask, bounds))
            {
                Add(new OpacityMaskNode(mask, bounds, CreateChildScene(mask)));
            }
            else
            {
                ++_drawOperationindex;
            }
        }

        /// <inheritdoc/>
        public void PushBitmapBlendMode(BitmapBlendingMode blendingMode)
        {
            var next = NextDrawAs<BitmapBlendModeNode>();

            if (next == null || !next.Item.Equals(blendingMode))
            {
                Add(new BitmapBlendModeNode(blendingMode));
            }
            else
            {
                ++_drawOperationindex;
            }
        }
        
        public readonly struct UpdateState : IDisposable
        {
            public UpdateState(
                DeferredDrawingContextImpl owner,
                VisualNode node,
                int childIndex,
                int drawOperationIndex)
            {
                Owner = owner;
                Node = node;
                ChildIndex = childIndex;
                DrawOperationIndex = drawOperationIndex;
            }

            public void Dispose()
            {
                Owner._node.TrimDrawOperations(Owner._drawOperationindex);

                var dirty = Owner.Layers.GetOrAdd(Owner._node.LayerRoot).Dirty;

                foreach (var operation in Owner._node.DrawOperations)
                {
                    dirty.Add(operation.Item.Bounds);
                }

                Owner._node = Node;
                Owner._childIndex = ChildIndex;
                Owner._drawOperationindex = DrawOperationIndex;
            }

            public DeferredDrawingContextImpl Owner { get; }
            public VisualNode Node { get; }
            public int ChildIndex { get; }
            public int DrawOperationIndex { get; }
        }

        private void Add<T>(T node) where T : class, IDrawOperation
        {
            using (var refCounted = RefCountable.Create(node))
            {
                Add(refCounted);
            }
        }

        private void Add(IRef<IDrawOperation> node)
        {
            if (_drawOperationindex < _node.DrawOperations.Count)
            {
                _node.ReplaceDrawOperation(_drawOperationindex, node);
            }
            else
            {
                _node.AddDrawOperation(node);
            }

            ++_drawOperationindex;
        }

        private IRef<T> NextDrawAs<T>() where T : class, IDrawOperation
        {
            return _drawOperationindex < _node.DrawOperations.Count ? _node.DrawOperations[_drawOperationindex] as IRef<T> : null;
        }

        private IDictionary<IVisual, Scene> CreateChildScene(IBrush brush)
        {
            var visualBrush = brush as VisualBrush;

            if (visualBrush != null)
            {
                var visual = visualBrush.Visual;

                if (visual != null)
                {
                    (visual as IVisualBrushInitialize)?.EnsureInitialized();
                    var scene = new Scene(visual);
                    _sceneBuilder.UpdateAll(scene);
                    return new Dictionary<IVisual, Scene> { { visualBrush.Visual, scene } };
                }
            }

            return null;
        }
    }
}
