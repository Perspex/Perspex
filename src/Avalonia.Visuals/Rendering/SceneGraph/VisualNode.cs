using System;
using System.Collections.Generic;
using System.Reactive.Disposables;
using Avalonia.Collections;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Utilities;
using Avalonia.VisualTree;

namespace Avalonia.Rendering.SceneGraph
{
    /// <summary>
    /// A node in the low-level scene graph representing an <see cref="IVisual"/>.
    /// </summary>
    internal class VisualNode : IVisualNode
    {
        private static readonly IReadOnlyList<IVisualNode> EmptyChildren = Array.Empty<IVisualNode>();
        private static readonly IReadOnlyList<IRef<IDrawOperation>> EmptyDrawOperations = Array.Empty<IRef<IDrawOperation>>();

        private Rect? _bounds;
        private double _opacity;
        private List<IVisualNode> _children;
        private List<IRef<IDrawOperation>> _drawOperations;
        private IRef<IDisposable> _drawOperationsRefCounter;
        private bool _drawOperationsCloned;
        private Matrix transformRestore;

        /// <summary>
        /// Initializes a new instance of the <see cref="VisualNode"/> class.
        /// </summary>
        /// <param name="visual">The visual that this node represents.</param>
        /// <param name="parent">The parent scene graph node, if any.</param>
        public VisualNode(IVisual visual, IVisualNode parent)
        {
            Contract.Requires<ArgumentNullException>(visual != null);

            Visual = visual;
            Parent = parent;
            HasAncestorGeometryClip = parent != null && 
                (parent.HasAncestorGeometryClip || parent.GeometryClip != null);
        }

        /// <inheritdoc/>
        public IVisual Visual { get; }

        /// <inheritdoc/>
        public IVisualNode Parent { get; }

        /// <inheritdoc/>
        public CornerRadius ClipToBoundsRadius { get; set; }

        /// <inheritdoc/>
        public Matrix Transform { get; set; }

        /// <inheritdoc/>
        public Rect Bounds => _bounds ?? CalculateBounds();

        /// <inheritdoc/>
        public Rect ClipBounds { get; set; }

        /// <inheritdoc/>
        public Rect LayoutBounds { get; set; }

        /// <inheritdoc/>
        public bool ClipToBounds { get; set; }

        /// <inheritdoc/>
        public IGeometryImpl GeometryClip { get; set; }

        /// <inheritdoc/>
        public bool HasAncestorGeometryClip { get; }

        /// <inheritdoc/>
        public double Opacity
        {
            get { return _opacity; }
            set
            {
                if (_opacity != value)
                {
                    _opacity = value;
                    OpacityChanged = true;
                }
            }
        }

        /// <summary>
        /// Gets or sets the opacity mask for the scene graph node.
        /// </summary>
        public IBrush OpacityMask { get; set; }

        /// <summary>
        /// Gets a value indicating whether this node in the scene graph has already
        /// been updated in the current update pass.
        /// </summary>
        public bool SubTreeUpdated { get; set; }

        /// <summary>
        /// Gets a value indicating whether the <see cref="Opacity"/> property has changed.
        /// </summary>
        public bool OpacityChanged { get; private set; }

        public IVisual LayerRoot { get; set; }

        /// <inheritdoc/>
        public IReadOnlyList<IVisualNode> Children => _children ?? EmptyChildren;

        /// <inheritdoc/>
        public IReadOnlyList<IRef<IDrawOperation>> DrawOperations => _drawOperations ?? EmptyDrawOperations;

        /// <summary>
        /// Adds a child to the <see cref="Children"/> collection.
        /// </summary>
        /// <param name="child">The child to add.</param>
        public void AddChild(IVisualNode child)
        {
            if (child.Disposed)
            {
                throw new ObjectDisposedException("Visual node for {node.Visual}");
            }

            if (child.Parent != this)
            {
                throw new AvaloniaInternalException("VisualNode added to wrong parent.");
            }

            EnsureChildrenCreated();
            _children.Add(child);
        }

        /// <summary>
        /// Adds an operation to the <see cref="DrawOperations"/> collection.
        /// </summary>
        /// <param name="operation">The operation to add.</param>
        public void AddDrawOperation(IRef<IDrawOperation> operation)
        {
            EnsureDrawOperationsCreated();
            _drawOperations.Add(operation.Clone());
        }

        /// <summary>
        /// Removes a child from the <see cref="Children"/> collection.
        /// </summary>
        /// <param name="child">The child to remove.</param>
        public void RemoveChild(IVisualNode child)
        {
            EnsureChildrenCreated();
            _children.Remove(child);
        }

        /// <summary>
        /// Replaces a child in the <see cref="Children"/> collection.
        /// </summary>
        /// <param name="index">The child to be replaced.</param>
        /// <param name="node">The child to add.</param>
        public void ReplaceChild(int index, IVisualNode node)
        {
            if (node.Disposed)
            {
                throw new ObjectDisposedException("Visual node for {node.Visual}");
            }

            if (node.Parent != this)
            {
                throw new AvaloniaInternalException("VisualNode added to wrong parent.");
            }

            EnsureChildrenCreated();
            _children[index] = node;
        }

        /// <summary>
        /// Replaces an item in the <see cref="DrawOperations"/> collection.
        /// </summary>
        /// <param name="index">The operation to be replaced.</param>
        /// <param name="operation">The operation to add.</param>
        public void ReplaceDrawOperation(int index, IRef<IDrawOperation> operation)
        {
            EnsureDrawOperationsCreated();
            var old = _drawOperations[index];
            _drawOperations[index] = operation.Clone();
            old.Dispose();
        }

        /// <summary>
        /// Sorts the <see cref="Children"/> collection according to the order of the visual's
        /// children and their z-index.
        /// </summary>
        /// <param name="scene">The scene that the node is a part of.</param>
        public void SortChildren(Scene scene)
        {
            if (_children == null || _children.Count <= 1)
            {
                return;
            }

            var keys = new List<long>(Visual.VisualChildren.Count);

            for (var i = 0; i < Visual.VisualChildren.Count; ++i)
            {
                var child = Visual.VisualChildren[i];
                var zIndex = child.ZIndex;
                keys.Add(((long)zIndex << 32) + i);
            }

            keys.Sort();
            _children.Clear();

            foreach (var i in keys)
            {
                var child = Visual.VisualChildren[(int)(i & 0xffffffff)];
                var node = scene.FindNode(child);

                if (node != null)
                {
                    _children.Add(node);
                }
            }
        }

        /// <summary>
        /// Removes items in the <see cref="Children"/> collection from the specified index
        /// to the end.
        /// </summary>
        /// <param name="first">The index of the first child to be removed.</param>
        public void TrimChildren(int first)
        {
            if (first < _children?.Count)
            {
                EnsureChildrenCreated();
                for (int i = first; i < _children.Count; i++)
                {
                    _children[i].Dispose();
                }
                _children.RemoveRange(first, _children.Count - first);
            }
        }

        /// <summary>
        /// Removes items in the <see cref="DrawOperations"/> collection from the specified index
        /// to the end.
        /// </summary>
        /// <param name="first">The index of the first operation to be removed.</param>
        public void TrimDrawOperations(int first)
        {
            if (first < _drawOperations?.Count)
            {
                EnsureDrawOperationsCreated();
                for (int i = first; i < _drawOperations.Count; i++)
                {
                    _drawOperations[i].Dispose();
                }
                _drawOperations.RemoveRange(first, _drawOperations.Count - first);
            }
        }

        /// <summary>
        /// Makes a copy of the node
        /// </summary>
        /// <param name="parent">The new parent node.</param>
        /// <returns>A cloned node.</returns>
        public VisualNode Clone(IVisualNode parent)
        {
            return new VisualNode(Visual, parent)
            {
                Transform = Transform,
                ClipBounds = ClipBounds,
                ClipToBoundsRadius =  ClipToBoundsRadius,
                ClipToBounds = ClipToBounds,
                LayoutBounds = LayoutBounds,
                GeometryClip = GeometryClip,
                _opacity = Opacity,
                OpacityMask = OpacityMask,
                _drawOperations = _drawOperations,
                _drawOperationsRefCounter = _drawOperationsRefCounter?.Clone(),
                _drawOperationsCloned = true,
                LayerRoot= LayerRoot,
            };
        }

        /// <inheritdoc/>
        public bool HitTest(Point p)
        {
            var drawOperations = DrawOperations;
            var drawOperationsCount = drawOperations.Count;

            for (var i = 0; i < drawOperationsCount; i++)
            {
                var operation = drawOperations[i];

                if (operation?.Item?.HitTest(p) == true)
                {
                    return true;
                }
            }

            return false;
        }

        /// <inheritdoc/>
        public void BeginRender(IDrawingContextImpl context, bool skipOpacity)
        {
            transformRestore = context.Transform;

            if (ClipToBounds)
            {
                context.Transform = Matrix.Identity;
                if (ClipToBoundsRadius.IsEmpty)
                    context.PushClip(ClipBounds);
                else
                    context.PushClip(new RoundedRect(ClipBounds, ClipToBoundsRadius));
            }

            context.Transform = Transform;

            if (Opacity != 1 && !skipOpacity)
            {
                context.PushOpacity(Opacity);
            }

            if (GeometryClip != null)
            {
                context.PushGeometryClip(GeometryClip);
            }

            if (OpacityMask != null)
            {
                context.PushOpacityMask(OpacityMask, LayoutBounds);
            }
        }

        /// <inheritdoc/>
        public void EndRender(IDrawingContextImpl context, bool skipOpacity)
        {
            if (OpacityMask != null)
            {
                context.PopOpacityMask();
            }

            if (GeometryClip != null)
            {
                context.PopGeometryClip();
            }

            if (Opacity != 1 && !skipOpacity)
            {
                context.PopOpacity();
            }

            if (ClipToBounds)
            {
                context.Transform = Matrix.Identity;
                context.PopClip();
            }

            context.Transform = transformRestore;
        }

        internal void TryPreallocateChildren(int count)
        {
            if (count == 0)
            {
                return;
            }

            EnsureChildrenCreated(count);
        }

        private Rect CalculateBounds()
        {
            var result = new Rect();

            if (_drawOperations != null)
            {
                foreach (var operation in _drawOperations)
                {
                    result = result.Union(operation.Item.Bounds);
                }
            }

            _bounds = result;
            return result;
        }

        private void EnsureChildrenCreated(int capacity = 0)
        {
            if (_children == null)
            {
                _children = new List<IVisualNode>(capacity);
            }
        }

        /// <summary>
        /// Ensures that this node draw operations have been created and are mutable (in case we are using cloned operations).
        /// </summary>
        private void EnsureDrawOperationsCreated()
        {
            if (_drawOperations == null)
            {
                _drawOperations = new List<IRef<IDrawOperation>>();
                _drawOperationsRefCounter = RefCountable.Create(CreateDisposeDrawOperations(_drawOperations));
                _drawOperationsCloned = false;
            }
            else if (_drawOperationsCloned)
            {
                var oldDrawOperations = _drawOperations;

                _drawOperations = new List<IRef<IDrawOperation>>(oldDrawOperations.Count);

                foreach (var drawOperation in oldDrawOperations)
                {
                    _drawOperations.Add(drawOperation.Clone());
                }

                _drawOperationsRefCounter.Dispose();
                _drawOperationsRefCounter = RefCountable.Create(CreateDisposeDrawOperations(_drawOperations));
                _drawOperationsCloned = false;
            }
        }

        /// <summary>
        /// Creates disposable that will dispose all items in passed draw operations after being disposed.
        /// It is crucial that we don't capture current <see cref="VisualNode"/> instance
        /// as draw operations can be cloned and may persist across subsequent scenes.
        /// </summary>
        /// <param name="drawOperations">Draw operations that need to be disposed.</param>
        /// <returns>Disposable for given draw operations.</returns>
        private static IDisposable CreateDisposeDrawOperations(List<IRef<IDrawOperation>> drawOperations)
        {
            return Disposable.Create(drawOperations, operations =>
            {
                foreach (var operation in operations)
                {
                    operation.Dispose();
                }
            });
        }

        public bool Disposed { get; private set; }

        public void Dispose()
        {
            _drawOperationsRefCounter?.Dispose();

            Disposed = true;
        }
    }
}
