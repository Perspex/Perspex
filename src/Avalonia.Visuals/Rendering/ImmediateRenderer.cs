using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Logging;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.VisualTree;

namespace Avalonia.Rendering
{
    /// <summary>
    /// A renderer which renders the state of the visual tree without an intermediate scene graph
    /// representation.
    /// </summary>
    /// <remarks>
    /// The immediate renderer supports only clip-bound-based hit testing; a control's geometry is
    /// not taken into account.
    /// </remarks>
    public class ImmediateRenderer : RendererBase, IRenderer, IVisualBrushRenderer
    {
        private readonly IVisual _root;
        private readonly IRenderRoot _renderRoot;
        private IRenderTarget _renderTarget;

        /// <summary>
        /// Initializes a new instance of the <see cref="ImmediateRenderer"/> class.
        /// </summary>
        /// <param name="root">The control to render.</param>
        public ImmediateRenderer(IVisual root)
        {
            Contract.Requires<ArgumentNullException>(root != null);

            _root = root;
            _renderRoot = root as IRenderRoot;
        }

        /// <inheritdoc/>
        public bool DrawFps { get; set; }

        /// <inheritdoc/>
        public bool DrawDirtyRects { get; set; }

        /// <inheritdoc/>
        public event EventHandler<SceneInvalidatedEventArgs> SceneInvalidated;

        /// <inheritdoc/>
        public void Paint(Rect rect)
        {
            if (_renderTarget == null)
            {
                _renderTarget = ((IRenderRoot)_root).CreateRenderTarget();
            }

            try
            {
                using (var context = new DrawingContext(_renderTarget.CreateDrawingContext(this)))
                {
                    context.PlatformImpl.Clear(Colors.Transparent);

                    using (context.PushTransformContainer())
                    {
                        Render(context, _root, _root.Bounds);
                    }

                    if (DrawDirtyRects)
                    {
                        var color = (uint)new Random().Next(0xffffff) | 0x44000000;
                        context.FillRectangle(
                            new SolidColorBrush(color),
                            rect);
                    }

                    if (DrawFps)
                    {
                        RenderFps(context, _root.Bounds, null);
                    }
                }
            }
            catch (RenderTargetCorruptedException ex)
            {
                Logger.TryGet(LogEventLevel.Information, LogArea.Animations)?.Log(this, "Render target was corrupted. Exception: {0}", ex);
                _renderTarget.Dispose();
                _renderTarget = null;
            }

            SceneInvalidated?.Invoke(this, new SceneInvalidatedEventArgs((IRenderRoot)_root, rect));
        }

        /// <inheritdoc/>
        public void Resized(Size size)
        {
        }

        /// <summary>
        /// Renders a visual to a render target.
        /// </summary>
        /// <param name="visual">The visual.</param>
        /// <param name="target">The render target.</param>
        public static void Render(IVisual visual, IRenderTarget target)
        {
            using (var renderer = new ImmediateRenderer(visual))
            using (var context = new DrawingContext(target.CreateDrawingContext(renderer)))
            {
                renderer.Render(context, visual, visual.Bounds);
            }
        }

        /// <summary>
        /// Renders a visual to a drawing context.
        /// </summary>
        /// <param name="visual">The visual.</param>
        /// <param name="context">The drawing context.</param>
        public static void Render(IVisual visual, DrawingContext context)
        {
            using (var renderer = new ImmediateRenderer(visual))
            {
                renderer.Render(context, visual, visual.Bounds);
            }
        }

        /// <inheritdoc/>
        public void AddDirty(IVisual visual)
        {
            if (visual.Bounds != Rect.Empty)
            {
                var m = visual.TransformToVisual(_root);

                if (m.HasValue)
                {
                    var bounds = new Rect(visual.Bounds.Size).TransformToAABB(m.Value);

                    //use transformedbounds as previous render state of the visual bounds
                    //so we can invalidate old and new bounds of a control in case it moved/shrinked
                    if (visual.TransformedBounds.HasValue)
                    {
                        var trb = visual.TransformedBounds.Value;
                        var trBounds = trb.Bounds.TransformToAABB(trb.Transform);

                        if (trBounds != bounds)
                        {
                            _renderRoot?.Invalidate(trBounds);
                        }
                    }

                    _renderRoot?.Invalidate(bounds);
                }
            }
        }

        /// <summary>
        /// Ends the operation of the renderer.
        /// </summary>
        public void Dispose()
        {
            _renderTarget?.Dispose();
        }

        /// <inheritdoc/>
        public IEnumerable<IVisual> HitTest(Point p, IVisual root, Func<IVisual, bool> filter)
        {
            return HitTest(root, p, filter);
        }

        public IVisual HitTestFirst(Point p, IVisual root, Func<IVisual, bool> filter)
        {
            return HitTest(root, p, filter).FirstOrDefault();
        }

        /// <inheritdoc/>
        public void RecalculateChildren(IVisual visual) => AddDirty(visual);

        /// <inheritdoc/>
        public void Start()
        {
        }

        /// <inheritdoc/>
        public void Stop()
        {
        }

        /// <inheritdoc/>
        Size IVisualBrushRenderer.GetRenderTargetSize(IVisualBrush brush)
        {
            (brush.Visual as IVisualBrushInitialize)?.EnsureInitialized();
            return brush.Visual?.Bounds.Size ?? Size.Empty;
        }

        /// <inheritdoc/>
        void IVisualBrushRenderer.RenderVisualBrush(IDrawingContextImpl context, IVisualBrush brush)
        {
            var visual = brush.Visual;
            Render(new DrawingContext(context), visual, visual.Bounds);
        }

        private static void ClearTransformedBounds(IVisual visual)
        {
            foreach (var e in visual.GetSelfAndVisualDescendants())
            {
                visual.TransformedBounds = null;
            }
        }

        private static Rect GetTransformedBounds(IVisual visual)
        {
            if (visual.RenderTransform == null)
            {
                return visual.Bounds;
            }
            else
            {
                var origin = visual.RenderTransformOrigin.ToPixels(new Size(visual.Bounds.Width, visual.Bounds.Height));
                var offset = Matrix.CreateTranslation(visual.Bounds.Position + origin);
                var m = (-offset) * visual.RenderTransform.Value * (offset);
                return visual.Bounds.TransformToAABB(m);
            }
        }

        private static IEnumerable<IVisual> HitTest(
           IVisual visual,
           Point p,
           Func<IVisual, bool> filter)
        {
            Contract.Requires<ArgumentNullException>(visual != null);

            if (filter?.Invoke(visual) != false)
            {
                bool containsPoint = false;

                if (visual is ICustomSimpleHitTest custom)
                {
                    containsPoint = custom.HitTest(p);
                }
                else
                {
                    containsPoint = visual.TransformedBounds?.Contains(p) == true;
                }

                if ((containsPoint || !visual.ClipToBounds) && visual.VisualChildren.Count > 0)
                {
                    foreach (var child in visual.VisualChildren.SortByZIndex())
                    {
                        foreach (var result in HitTest(child, p, filter))
                        {
                            yield return result;
                        }
                    }
                }

                if (containsPoint)
                {
                    yield return visual;
                }
            }
        }

        private void Render(DrawingContext context, IVisual visual, Rect clipRect)
        {
            var opacity = visual.Opacity;
            var clipToBounds = visual.ClipToBounds;
            var bounds = new Rect(visual.Bounds.Size);

            if (visual.IsVisible && opacity > 0)
            {
                var m = Matrix.CreateTranslation(visual.Bounds.Position);

                var renderTransform = Matrix.Identity;

                if (visual.RenderTransform != null)
                {
                    var origin = visual.RenderTransformOrigin.ToPixels(new Size(visual.Bounds.Width, visual.Bounds.Height));
                    var offset = Matrix.CreateTranslation(origin);
                    renderTransform = (-offset) * visual.RenderTransform.Value * (offset);
                }

                m = renderTransform * m;

                if (clipToBounds)
                {
                    if (visual.RenderTransform != null)
                    {
                        clipRect = new Rect(visual.Bounds.Size);
                    }
                    else
                    {
                        clipRect = clipRect.Intersect(new Rect(visual.Bounds.Size));
                    }
                }

                using (context.PushPostTransform(m))
                using (context.PushOpacity(opacity))
                using (clipToBounds 
                    ? visual is IVisualWithRoundRectClip roundClipVisual 
                        ? context.PushClip(new RoundedRect(bounds, roundClipVisual.ClipToBoundsRadius))
                        : context.PushClip(bounds) 
                    : default(DrawingContext.PushedState))
                using (visual.Clip != null ? context.PushGeometryClip(visual.Clip) : default(DrawingContext.PushedState))
                using (visual.OpacityMask != null ? context.PushOpacityMask(visual.OpacityMask, bounds) : default(DrawingContext.PushedState))
                using (context.PushTransformContainer())
                {
                    visual.Render(context);

#pragma warning disable 0618
                    var transformed =
                        new TransformedBounds(bounds, new Rect(), context.CurrentContainerTransform);
#pragma warning restore 0618

                    visual.TransformedBounds = transformed;

                    foreach (var child in visual.VisualChildren.OrderBy(x => x, ZIndexComparer.Instance))
                    {
                        var childBounds = GetTransformedBounds(child);

                        if (!child.ClipToBounds || clipRect.Intersects(childBounds))
                        {
                            var childClipRect = child.RenderTransform == null
                                ? clipRect.Translate(-childBounds.Position)
                                : clipRect;
                            Render(context, child, childClipRect);
                        }
                        else
                        {
                            ClearTransformedBounds(child);
                        }
                    }
                }
            }

            if (!visual.IsVisible)
            {
                ClearTransformedBounds(visual);
            }
        }
    }
}
