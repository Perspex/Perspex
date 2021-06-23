using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Avalonia.Logging;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Avalonia.Platform;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Threading;
using Avalonia.Utilities;
using Avalonia.VisualTree;

namespace Avalonia.Rendering
{
    /// <summary>
    /// A renderer which renders the state of the visual tree to an intermediate scene graph
    /// representation which is then rendered on a rendering thread.
    /// </summary>
    public class DeferredRenderer : RendererBase, IRenderer, IRenderLoopTask, IVisualBrushRenderer
    {
        private readonly IDispatcher _dispatcher;
        private readonly IRenderLoop _renderLoop;
        private readonly IVisual _root;
        private readonly ISceneBuilder _sceneBuilder;

        private bool _running;
        private bool _disposed;
        private volatile IRef<Scene> _scene;
        private DirtyVisuals _dirty;
        private HashSet<IVisual> _recalculateChildren;
        private IRef<IRenderTargetBitmapImpl> _overlay;
        private int _lastSceneId = -1;
        private DisplayDirtyRects _dirtyRectsDisplay = new DisplayDirtyRects();
        private IRef<IDrawOperation> _currentDraw;
        private readonly IDeferredRendererLock _lock;
        private readonly object _sceneLock = new object();
        private readonly Action _updateSceneIfNeededDelegate;

        /// <summary>
        /// Initializes a new instance of the <see cref="DeferredRenderer"/> class.
        /// </summary>
        /// <param name="root">The control to render.</param>
        /// <param name="renderLoop">The render loop.</param>
        /// <param name="sceneBuilder">The scene builder to use. Optional.</param>
        /// <param name="dispatcher">The dispatcher to use. Optional.</param>
        /// <param name="rendererLock">Lock object used before trying to access render target</param>
        public DeferredRenderer(
            IRenderRoot root,
            IRenderLoop renderLoop,
            ISceneBuilder sceneBuilder = null,
            IDispatcher dispatcher = null,
            IDeferredRendererLock rendererLock = null) : base(true)
        {
            Contract.Requires<ArgumentNullException>(root != null);

            _dispatcher = dispatcher ?? Dispatcher.UIThread;
            _root = root;
            _sceneBuilder = sceneBuilder ?? new SceneBuilder();
            Layers = new RenderLayers();
            _renderLoop = renderLoop;
            _lock = rendererLock ?? new ManagedDeferredRendererLock();
            _updateSceneIfNeededDelegate = UpdateSceneIfNeeded;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DeferredRenderer"/> class.
        /// </summary>
        /// <param name="root">The control to render.</param>
        /// <param name="renderTarget">The render target.</param>
        /// <param name="sceneBuilder">The scene builder to use. Optional.</param>
        /// <remarks>
        /// This constructor is intended to be used for unit testing.
        /// </remarks>
        public DeferredRenderer(
            IVisual root,
            IRenderTarget renderTarget,
            ISceneBuilder sceneBuilder = null) : base(true)
        {
            Contract.Requires<ArgumentNullException>(root != null);
            Contract.Requires<ArgumentNullException>(renderTarget != null);

            _root = root;
            RenderTarget = renderTarget;
            _sceneBuilder = sceneBuilder ?? new SceneBuilder();
            Layers = new RenderLayers();
            _lock = new ManagedDeferredRendererLock();
            _updateSceneIfNeededDelegate = UpdateSceneIfNeeded;
        }

        /// <inheritdoc/>
        public bool DrawFps { get; set; }

        /// <inheritdoc/>
        public bool DrawDirtyRects { get; set; }

        /// <summary>
        /// Gets or sets a path to which rendered frame should be rendered for debugging.
        /// </summary>
        public string DebugFramesPath { get; set; }

        /// <summary>
        /// Forces the renderer to only draw frames on the render thread. Makes Paint to wait until frame is rendered
        /// </summary>
        public bool RenderOnlyOnRenderThread { get; set; }

        /// <inheritdoc/>
        public event EventHandler<SceneInvalidatedEventArgs> SceneInvalidated;

        /// <summary>
        /// Gets the render layers.
        /// </summary>
        internal RenderLayers Layers { get; }

        /// <summary>
        /// Gets the current render target.
        /// </summary>
        internal IRenderTarget RenderTarget { get; private set; }

        /// <inheritdoc/>
        public void AddDirty(IVisual visual)
        {
            _dirty?.Add(visual);
        }

        /// <summary>
        /// Disposes of the renderer and detaches from the render loop.
        /// </summary>
        public void Dispose()
        {
            lock (_sceneLock)
            {
                if (_disposed)
                    return;
                _disposed = true;
                var scene = _scene;
                _scene = null;
                scene?.Dispose();
            }

            Stop();
            DisposeRenderTarget();
        }

        public void RecalculateChildren(IVisual visual) => _recalculateChildren?.Add(visual);

        void DisposeRenderTarget()
        {
            using (var l = _lock.TryLock())
            {
                if(l == null)
                {
                    // We are still trying to render on the render thread, try again a bit later
                    DispatcherTimer.RunOnce(DisposeRenderTarget, TimeSpan.FromMilliseconds(50),
                        DispatcherPriority.Background);
                    return;
                }

                Layers.Clear();
                RenderTarget?.Dispose();
                RenderTarget = null;
            }
        }

        /// <inheritdoc/>
        public IEnumerable<IVisual> HitTest(Point p, IVisual root, Func<IVisual, bool> filter)
        {
            EnsureCanHitTest();

            //It's safe to access _scene here without a lock since
            //it's only changed from UI thread which we are currently on
            return _scene?.Item.HitTest(p, root, filter) ?? Enumerable.Empty<IVisual>();
        }

        /// <inheritdoc/>
        public IVisual HitTestFirst(Point p, IVisual root, Func<IVisual, bool> filter)
        {
            EnsureCanHitTest();

            //It's safe to access _scene here without a lock since
            //it's only changed from UI thread which we are currently on
            return _scene?.Item.HitTestFirst(p, root, filter);
        }

        /// <inheritdoc/>
        public void Paint(Rect rect)
        {
            if (RenderOnlyOnRenderThread)
            {
                // Renderer is stopped and doesn't tick on the render thread
                // This indicates a bug somewhere in our code
                // (currently happens when a window gets minimized with Show desktop on Windows 10)
                if(!_running)
                    return;
                
                while (true)
                {
                    Scene scene;
                    bool? updated;
                    lock (_sceneLock)
                    {
                        updated = UpdateScene();
                        scene = _scene?.Item;
                    }
                    
                    // Renderer is in invalid state, skip drawing
                    if(updated == null)
                        return;

                    // Wait for the scene to be rendered or disposed
                    scene?.Rendered.Wait();
                    
                    // That was an up-to-date scene, we can return immediately
                    if (updated == true)
                        return;
                }
            }
            else
            {
                var t = (IRenderLoopTask)this;
                if (t.NeedsUpdate)
                    UpdateScene();
                if (_scene?.Item != null)
                    Render(true);
            }
        }

        /// <inheritdoc/>
        public void Resized(Size size)
        {
        }

        /// <inheritdoc/>
        public void Start()
        {
            if (!_running && _renderLoop != null)
            {
                _renderLoop.Add(this);
                _running = true;
            }
        }

        /// <inheritdoc/>
        public void Stop()
        {
            if (_running && _renderLoop != null)
            {
                _renderLoop.Remove(this);
                _running = false;
            }
        }

        bool NeedsUpdate => _dirty == null || _dirty.Count > 0;
        bool IRenderLoopTask.NeedsUpdate => NeedsUpdate;

        void IRenderLoopTask.Update(TimeSpan time) => UpdateScene();

        void IRenderLoopTask.Render() => Render(false);

        /// <inheritdoc/>
        Size IVisualBrushRenderer.GetRenderTargetSize(IVisualBrush brush)
        {
            return (_currentDraw.Item as BrushDrawOperation)?.ChildScenes?[brush.Visual]?.Size ?? Size.Empty;
        }

        /// <inheritdoc/>
        void IVisualBrushRenderer.RenderVisualBrush(IDrawingContextImpl context, IVisualBrush brush)
        {
            var childScene = (_currentDraw.Item as BrushDrawOperation)?.ChildScenes?[brush.Visual];

            if (childScene != null)
            {
                Render(context, (VisualNode)childScene.Root, null, new Rect(childScene.Size));
            }
        }

        internal void UnitTestUpdateScene() => UpdateScene();

        internal void UnitTestRender() => Render(false);

        internal Scene UnitTestScene() => _scene.Item;

        private void EnsureCanHitTest()
        {
            if (_renderLoop == null && (_dirty == null || _dirty.Count > 0))
            {
                // When unit testing the renderLoop may be null, so update the scene manually.
                UpdateScene();
            }
        }

        private void Render(bool forceComposite)
        {
            using (var l = _lock.TryLock())
            {
                if (l == null)
                    return;

                IDrawingContextImpl context = null;
                try
                {
                    try
                    {
                        var (scene, updated) = UpdateRenderLayersAndConsumeSceneIfNeeded(ref context);
                        if (updated)
                            FpsTick();
                        using (scene)
                        {
                            if (scene?.Item != null)
                            {
                                try
                                {
                                    var overlay = DrawDirtyRects || DrawFps;
                                    if (DrawDirtyRects)
                                        _dirtyRectsDisplay.Tick();
                                    if (overlay)
                                        RenderOverlay(scene.Item, ref context);
                                    if (updated || forceComposite || overlay)
                                        RenderComposite(scene.Item, ref context);
                                }
                                finally
                                {
                                    scene.Item.MarkAsRendered();
                                }
                            }
                        }
                    }
                    finally
                    {
                        context?.Dispose();
                    }
                }
                catch (RenderTargetCorruptedException ex)
                {
                    Logger.TryGet(LogEventLevel.Information, LogArea.Animations)?.Log(this, "Render target was corrupted. Exception: {0}", ex);
                    RenderTarget?.Dispose();
                    RenderTarget = null;
                }
            }
        }

        private (IRef<Scene> scene, bool updated) UpdateRenderLayersAndConsumeSceneIfNeeded(ref IDrawingContextImpl context,
            bool recursiveCall = false)
        {
            IRef<Scene> sceneRef;
            lock (_sceneLock)
                sceneRef = _scene?.Clone();
            if (sceneRef == null)
                return (null, false);
            using (sceneRef)
            {
                var scene = sceneRef.Item;
                if (scene.Generation != _lastSceneId)
                {
                    EnsureDrawingContext(ref context);

                    Layers.Update(scene, context);

                    RenderToLayers(scene);

                    if (DebugFramesPath != null)
                    {
                        SaveDebugFrames(scene.Generation);
                    }

                    lock (_sceneLock)
                        _lastSceneId = scene.Generation;


                    var isUiThread = Dispatcher.UIThread.CheckAccess();
                    // We have consumed the previously available scene, but there might be some dirty 
                    // rects since the last update. *If* we are on UI thread, we can force immediate scene
                    // rebuild before rendering anything on-screen
                    // We are calling the same method recursively here 
                    if (!recursiveCall && isUiThread && NeedsUpdate)
                    {
                        UpdateScene();
                        var (rs, _) = UpdateRenderLayersAndConsumeSceneIfNeeded(ref context, true);
                        return (rs, true);
                    }

                    // We are rendering a new scene version, so it's highly likely
                    // that there is already a pending update for animations
                    // So we are scheduling an update call so UI thread could prepare a scene before
                    // the next render timer tick
                    if (!recursiveCall && !isUiThread)
                        Dispatcher.UIThread.Post(_updateSceneIfNeededDelegate, DispatcherPriority.Render);

                    // Indicate that we have updated the layers
                    return (sceneRef.Clone(), true);
                }

                // Just return scene, layers weren't updated
                return (sceneRef.Clone(), false);
            }

        }


        private void Render(IDrawingContextImpl context, VisualNode node, IVisual layer, Rect clipBounds)
        {
            if (layer == null || node.LayerRoot == layer)
            {
                clipBounds = node.ClipBounds.Intersect(clipBounds);

                if (!clipBounds.IsEmpty && node.Opacity > 0)
                {
                    var isLayerRoot = node.Visual == layer;

                    node.BeginRender(context, isLayerRoot);

                    var drawOperations = node.DrawOperations;
                    var drawOperationsCount = drawOperations.Count;
                    for (int i = 0; i < drawOperationsCount; i++)
                    {
                        var operation = drawOperations[i];
                        _currentDraw = operation;
                        operation.Item.Render(context);
                        _currentDraw = null;
                    }

                    var children = node.Children;
                    var childrenCount = children.Count;
                    for (int i = 0; i < childrenCount; i++)
                    {
                        var child = children[i];
                        Render(context, (VisualNode)child, layer, clipBounds);
                    }

                    node.EndRender(context, isLayerRoot);
                }
            }
        }

        private void RenderToLayers(Scene scene)
        {
            foreach (var layer in scene.Layers)
            {
                var renderLayer = Layers[layer.LayerRoot];
                if (layer.Dirty.IsEmpty && !renderLayer.IsEmpty)
                    continue;
                var renderTarget = renderLayer.Bitmap;
                var node = (VisualNode)scene.FindNode(layer.LayerRoot);

                if (node != null)
                {
                    using (var context = renderTarget.Item.CreateDrawingContext(this))
                    {
                        if (renderLayer.IsEmpty)
                        {
                            // Render entire layer root node
                            context.Clear(Colors.Transparent);
                            context.Transform = Matrix.Identity;
                            context.PushClip(node.ClipBounds);
                            Render(context, node, layer.LayerRoot, node.ClipBounds);
                            context.PopClip();
                            if (DrawDirtyRects)
                            {
                                _dirtyRectsDisplay.Add(node.ClipBounds);
                            }

                            renderLayer.IsEmpty = false;
                        }
                        else
                        {
                            var scale = scene.Scaling;

                            foreach (var rect in layer.Dirty)
                            {
                                var snappedRect = SnapToDevicePixels(rect, scale);
                                context.Transform = Matrix.Identity;
                                context.PushClip(snappedRect);
                                context.Clear(Colors.Transparent);
                                Render(context, node, layer.LayerRoot, snappedRect);
                                context.PopClip();

                                if (DrawDirtyRects)
                                {
                                    _dirtyRectsDisplay.Add(snappedRect);
                                }
                            }
                        }
                    }
                }
            }
        }

        private static Rect SnapToDevicePixels(Rect rect, double scale)
        {
            return new Rect(
                new Point(
                    Math.Floor(rect.X * scale) / scale,
                    Math.Floor(rect.Y * scale) / scale),
                new Point(
                    Math.Ceiling(rect.Right * scale) / scale,
                    Math.Ceiling(rect.Bottom * scale) / scale));
        }

        private void RenderOverlay(Scene scene, ref IDrawingContextImpl parentContent)
        {
            EnsureDrawingContext(ref parentContent);

            if (DrawDirtyRects)
            {
                var overlay = GetOverlay(parentContent, scene.Size, scene.Scaling);

                using (var context = overlay.Item.CreateDrawingContext(this))
                {
                    context.Clear(Colors.Transparent);
                    RenderDirtyRects(context);
                }
            }
            else
            {
                _overlay?.Dispose();
                _overlay = null;
            }
        }

        private void RenderDirtyRects(IDrawingContextImpl context)
        {
            foreach (var r in _dirtyRectsDisplay)
            {
                var brush = new ImmutableSolidColorBrush(Colors.Magenta, r.Opacity);
                context.DrawRectangle(brush,null, r.Rect);
            }
        }

        private void RenderComposite(Scene scene, ref IDrawingContextImpl context)
        {
            EnsureDrawingContext(ref context);

            context.Clear(Colors.Transparent);

            var clientRect = new Rect(scene.Size);

            var firstLayer = true;
            foreach (var layer in scene.Layers)
            {
                var bitmap = Layers[layer.LayerRoot].Bitmap;
                var sourceRect = new Rect(0, 0, bitmap.Item.PixelSize.Width, bitmap.Item.PixelSize.Height);

                if (layer.GeometryClip != null)
                {
                    context.PushGeometryClip(layer.GeometryClip);
                }

                if (layer.OpacityMask == null)
                {
                    if (firstLayer && bitmap.Item.CanBlit)
                        bitmap.Item.Blit(context);
                    else
                        context.DrawBitmap(bitmap, layer.Opacity, sourceRect, clientRect);
                }
                else
                {
                    context.DrawBitmap(bitmap, layer.OpacityMask, layer.OpacityMaskRect, sourceRect);
                }

                if (layer.GeometryClip != null)
                {
                    context.PopGeometryClip();
                }

                firstLayer = false;
            }

            if (_overlay != null)
            {
                var sourceRect = new Rect(0, 0, _overlay.Item.PixelSize.Width, _overlay.Item.PixelSize.Height);
                context.DrawBitmap(_overlay, 0.5, sourceRect, clientRect);
            }

            if (DrawFps)
            {
                RenderFps(context, clientRect, scene.Layers.Count);
            }
        }

        private void EnsureDrawingContext(ref IDrawingContextImpl context)
        {
            if (context != null)
            {
                return;
            }

            if ((RenderTarget as IRenderTargetWithCorruptionInfo)?.IsCorrupted == true)
            {
                RenderTarget.Dispose();
                RenderTarget = null;
            }

            if (RenderTarget == null)
            {
                RenderTarget = ((IRenderRoot)_root).CreateRenderTarget();
            }

            context = RenderTarget.CreateDrawingContext(this);
        }

        private void UpdateSceneIfNeeded()
        {
            if(NeedsUpdate)
                UpdateScene();
        }
        
        private bool? UpdateScene()
        {
            Dispatcher.UIThread.VerifyAccess();
            using var noPump = NonPumpingLockHelper.Use();
            lock (_sceneLock)
            {
                if (_disposed)
                    return null;
                if (_scene?.Item.Generation > _lastSceneId)
                    return false;
            }
            if (_root.IsVisible)
            {
                var sceneRef = RefCountable.Create(_scene?.Item.CloneScene() ?? new Scene(_root));
                var scene = sceneRef.Item;

                if (_dirty == null)
                {
                    _dirty = new DirtyVisuals();
                    _recalculateChildren = new HashSet<IVisual>();
                    _sceneBuilder.UpdateAll(scene);
                }
                else
                {
                    foreach (var visual in _recalculateChildren)
                    {
                        var node = scene.FindNode(visual);
                        ((VisualNode)node)?.SortChildren(scene);
                    }

                    _recalculateChildren.Clear();

                    foreach (var visual in _dirty)
                    {
                        _sceneBuilder.Update(scene, visual);
                    }
                }

                lock (_sceneLock)
                {
                    var oldScene = _scene;
                    _scene = sceneRef;
                    oldScene?.Dispose();
                }

                _dirty.Clear();

                if (SceneInvalidated != null)
                {
                    var rect = new Rect();

                    foreach (var layer in scene.Layers)
                    {
                        foreach (var dirty in layer.Dirty)
                        {
                            rect = rect.Union(dirty);
                        }
                    }

                    SceneInvalidated(this, new SceneInvalidatedEventArgs((IRenderRoot)_root, rect));
                }

                return true;
            }
            else
            {
                lock (_sceneLock)
                {
                    var oldScene = _scene;
                    _scene = null;
                    oldScene?.Dispose();
                }

                return null;
            }
        }

        private IRef<IRenderTargetBitmapImpl> GetOverlay(
            IDrawingContextImpl parentContext,
            Size size,
            double scaling)
        {
            var pixelSize = size * scaling;

            if (_overlay == null ||
                _overlay.Item.PixelSize.Width != pixelSize.Width ||
                _overlay.Item.PixelSize.Height != pixelSize.Height)
            {
                _overlay?.Dispose();
                _overlay = RefCountable.Create(parentContext.CreateLayer(size));
            }

            return _overlay;
        }

        private void SaveDebugFrames(int id)
        {
            var index = 0;

            foreach (var layer in Layers)
            {
                var fileName = Path.Combine(DebugFramesPath, $"frame-{id}-layer-{index++}.png");
                layer.Bitmap.Item.Save(fileName);
            }
        }
    }
}
