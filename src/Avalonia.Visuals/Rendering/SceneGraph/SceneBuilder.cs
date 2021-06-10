using System;
using System.Collections.Generic;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace Avalonia.Rendering.SceneGraph
{
    /// <summary>
    /// Builds a scene graph from a visual tree.
    /// </summary>
    public class SceneBuilder : ISceneBuilder
    {
        /// <inheritdoc/>
        public void UpdateAll(Scene scene)
        {
            Contract.Requires<ArgumentNullException>(scene != null);
            Dispatcher.UIThread.VerifyAccess();

            UpdateSize(scene);
            scene.Layers.GetOrAdd(scene.Root.Visual);

            using (var impl = new DeferredDrawingContextImpl(this, scene.Layers))
            using (var context = new DrawingContext(impl))
            {
                var clip = new Rect(scene.Root.Visual.Bounds.Size);
                Update(context, scene, (VisualNode)scene.Root, clip, true);
            }
        }

        /// <inheritdoc/>
        public bool Update(Scene scene, IVisual visual)
        {
            Contract.Requires<ArgumentNullException>(scene != null);
            Contract.Requires<ArgumentNullException>(visual != null);

            Dispatcher.UIThread.VerifyAccess();

            if (!scene.Root.Visual.IsVisible)
            {
                throw new AvaloniaInternalException("Cannot update the scene for an invisible root visual.");
            }

            var node = (VisualNode)scene.FindNode(visual);

            if (visual == scene.Root.Visual)
            {
                UpdateSize(scene);
            }

            if (visual.VisualRoot == scene.Root.Visual)
            {
                if (node?.Parent != null &&
                    visual.VisualParent != null &&
                    node.Parent.Visual != visual.VisualParent)
                {
                    // The control has changed parents. Remove the node and recurse into the new parent node.
                    ((VisualNode)node.Parent).RemoveChild(node);
                    Deindex(scene, node);
                    node = (VisualNode)scene.FindNode(visual.VisualParent);
                }

                if (visual.IsVisible)
                {
                    // If the node isn't yet part of the scene, find the nearest ancestor that is.
                    node = node ?? FindExistingAncestor(scene, visual);

                    // We don't need to do anything if this part of the tree has already been fully
                    // updated.
                    if (node != null && !node.SubTreeUpdated)
                    {
                        // If the control we've been asked to update isn't part of the scene then
                        // we're carrying out an add operation, so recurse and add all the
                        // descendents too.
                        var recurse = node.Visual != visual;

                        using (var impl = new DeferredDrawingContextImpl(this, scene.Layers))
                        using (var context = new DrawingContext(impl))
                        {
                            var clip = new Rect(scene.Root.Visual.Bounds.Size);

                            if (node.Parent != null)
                            {
                                context.PushPostTransform(node.Parent.Transform);
                                clip = node.Parent.ClipBounds;
                            }

                            using (context.PushTransformContainer())
                            {
                                Update(context, scene, node, clip, recurse);
                            }
                        }

                        return true;
                    }
                }
                else
                {
                    if (node != null)
                    {
                        // The control has been hidden so remove it from its parent and deindex the
                        // node and its descendents.
                        ((VisualNode)node.Parent)?.RemoveChild(node);
                        Deindex(scene, node);
                        return true;
                    }
                }
            }
            else if (node != null)
            {
                // The control has been removed so remove it from its parent and deindex the
                // node and its descendents.
                var trim = FindFirstDeadAncestor(scene, node);
                ((VisualNode)trim.Parent).RemoveChild(trim);
                Deindex(scene, trim);
                return true;
            }

            return false;
        }

        private static VisualNode FindExistingAncestor(Scene scene, IVisual visual)
        {
            var node = scene.FindNode(visual);

            while (node == null && visual.IsVisible)
            {
                visual = visual.VisualParent;
                node = scene.FindNode(visual);
            }

            return visual.IsVisible ? (VisualNode)node : null;
        }

        private static VisualNode FindFirstDeadAncestor(Scene scene, IVisualNode node)
        {
            var parent = node.Parent;

            while (parent.Visual.VisualRoot == null)
            {
                node = parent;
                parent = node.Parent;
            }

            return (VisualNode)node;
        }

        private static object GetOrCreateChildNode(Scene scene, IVisual child, VisualNode parent)
        {
            var result = (VisualNode)scene.FindNode(child);

            if (result != null && result.Parent != parent)
            {
                Deindex(scene, result);
                result = null;
            }

            return result ?? CreateNode(scene, child, parent);
        }

        private static void Update(DrawingContext context, Scene scene, VisualNode node, Rect clip, bool forceRecurse)
        {
            var visual = node.Visual;
            var opacity = visual.Opacity;
            var clipToBounds = visual.ClipToBounds;
            var clipToBoundsRadius = visual is IVisualWithRoundRectClip roundRectClip ?
                roundRectClip.ClipToBoundsRadius :
                default;
            
            var bounds = new Rect(visual.Bounds.Size);
            var contextImpl = (DeferredDrawingContextImpl)context.PlatformImpl;

            contextImpl.Layers.Find(node.LayerRoot)?.Dirty.Add(node.Bounds);

            if (visual.IsVisible)
            {
                var m = node != scene.Root ? 
                    Matrix.CreateTranslation(visual.Bounds.Position) :
                    Matrix.Identity;

                var renderTransform = Matrix.Identity;

                if (visual.RenderTransform != null)
                {
                    var origin = visual.RenderTransformOrigin.ToPixels(new Size(visual.Bounds.Width, visual.Bounds.Height));
                    var offset = Matrix.CreateTranslation(origin);
                    renderTransform = (-offset) * visual.RenderTransform.Value * (offset);
                }

                m = renderTransform * m;

                using (contextImpl.BeginUpdate(node))
                using (context.PushPostTransform(m))
                using (context.PushTransformContainer())
                {
                    var globalBounds = bounds.TransformToAABB(contextImpl.Transform);
                    var clipBounds = clipToBounds ?
                        globalBounds.Intersect(clip) :
                        clip;

                    forceRecurse = forceRecurse ||
                        node.ClipBounds != clipBounds ||
                        node.Opacity != opacity ||
                        node.Transform != contextImpl.Transform;

                    node.Transform = contextImpl.Transform;
                    node.ClipBounds = clipBounds;
                    node.ClipToBounds = clipToBounds;
                    node.LayoutBounds = globalBounds;
                    node.ClipToBoundsRadius = clipToBoundsRadius;
                    node.GeometryClip = visual.Clip?.PlatformImpl;
                    node.Opacity = opacity;

                    // TODO: Check equality between node.OpacityMask and visual.OpacityMask before assigning.
                    node.OpacityMask = visual.OpacityMask?.ToImmutable();

                    if (ShouldStartLayer(visual))
                    {
                        if (node.LayerRoot != visual)
                        {
                            MakeLayer(scene, node);
                        }
                        else
                        {
                            UpdateLayer(node, scene.Layers[node.LayerRoot]);
                        }
                    }
                    else if (node.LayerRoot == node.Visual && node.Parent != null)
                    {
                        ClearLayer(scene, node);
                    }

                    if (node.ClipToBounds)
                    {
                        clip = clip.Intersect(node.ClipBounds);
                    }

                    try
                    {
                        visual.Render(context);
                    }
                    catch { }

                    var transformed = new TransformedBounds(new Rect(visual.Bounds.Size), clip, node.Transform);
                    visual.TransformedBounds = transformed;

                    if (forceRecurse)
                    {
                        var visualChildren = (IList<IVisual>) visual.VisualChildren;

                        node.TryPreallocateChildren(visualChildren.Count);

                        if (visualChildren.Count == 1)
                        {
                            var childNode = GetOrCreateChildNode(scene, visualChildren[0], node);
                            Update(context, scene, (VisualNode)childNode, clip, forceRecurse);
                        }
                        else if (visualChildren.Count > 1)
                        {
                            var sortedChildren = new IVisual[visualChildren.Count];
                            visualChildren.CopyTo(sortedChildren, 0);

                            Array.Sort(sortedChildren, ZIndexComparer.ComparisonInstance);

                            foreach (var child in sortedChildren)
                            {
                                var childNode = GetOrCreateChildNode(scene, child, node);
                                Update(context, scene, (VisualNode)childNode, clip, forceRecurse);
                            }
                        }

                        node.SubTreeUpdated = true;
                        contextImpl.TrimChildren();
                    }
                }
            }
            else
            {
                contextImpl.BeginUpdate(node).Dispose();
            }
        }

        private void UpdateSize(Scene scene)
        {
            var renderRoot = scene.Root.Visual as IRenderRoot;
            var newSize = renderRoot?.ClientSize ?? scene.Root.Visual.Bounds.Size;

            scene.Scaling = renderRoot?.RenderScaling ?? 1;

            if (scene.Size != newSize)
            {
                var oldSize = scene.Size;

                scene.Size = newSize;

                Rect horizontalDirtyRect = Rect.Empty;
                Rect verticalDirtyRect = Rect.Empty;

                if (newSize.Width > oldSize.Width)
                {
                    horizontalDirtyRect = new Rect(oldSize.Width, 0, newSize.Width - oldSize.Width, oldSize.Height);
                }

                if (newSize.Height > oldSize.Height)
                {
                    verticalDirtyRect = new Rect(0, oldSize.Height, newSize.Width, newSize.Height - oldSize.Height);
                }

                foreach (var layer in scene.Layers)
                {
                    layer.Dirty.Add(horizontalDirtyRect);
                    layer.Dirty.Add(verticalDirtyRect);
                }
            }
        }

        private static VisualNode CreateNode(Scene scene, IVisual visual, VisualNode parent)
        {
            var node = new VisualNode(visual, parent);
            node.LayerRoot = parent.LayerRoot;
            scene.Add(node);
            return node;
        }

        private static void Deindex(Scene scene, VisualNode node)
        {
            var nodeChildren = node.Children;
            var nodeChildrenCount = nodeChildren.Count;

            for (var i = 0; i < nodeChildrenCount; i++)
            {
                if (nodeChildren[i] is VisualNode visual)
                {
                    Deindex(scene, visual);
                }
            }

            scene.Remove(node);

            node.SubTreeUpdated = true;

            scene.Layers[node.LayerRoot].Dirty.Add(node.Bounds);

            node.Visual.TransformedBounds = null;

            if (node.LayerRoot == node.Visual && node.Visual != scene.Root.Visual)
            {
                scene.Layers.Remove(node.LayerRoot);
            }
        }

        private static void ClearLayer(Scene scene, VisualNode node)
        {
            var parent = (VisualNode)node.Parent;
            var oldLayerRoot = node.LayerRoot;
            var newLayerRoot = parent.LayerRoot;
            var existingDirtyRects = scene.Layers[node.LayerRoot].Dirty;
            var newDirtyRects = scene.Layers[newLayerRoot].Dirty;

            existingDirtyRects.Coalesce();

            foreach (var r in existingDirtyRects)
            {
                newDirtyRects.Add(r);
            }

            var oldLayer = scene.Layers[oldLayerRoot];
            PropagateLayer(node, scene.Layers[newLayerRoot], oldLayer);
            scene.Layers.Remove(oldLayer);
        }

        private static void MakeLayer(Scene scene, VisualNode node)
        {
            var oldLayerRoot = node.LayerRoot;
            var layer = scene.Layers.Add(node.Visual);
            var oldLayer = scene.Layers[oldLayerRoot];

            UpdateLayer(node, layer);
            PropagateLayer(node, layer, scene.Layers[oldLayerRoot]);
        }

        private static void UpdateLayer(VisualNode node, SceneLayer layer)
        {
            layer.Opacity = node.Visual.Opacity;

            if (node.Visual.OpacityMask != null)
            {
                layer.OpacityMask = node.Visual.OpacityMask?.ToImmutable();
                layer.OpacityMaskRect = node.ClipBounds;
            }
            else
            {
                layer.OpacityMask = null;
                layer.OpacityMaskRect = Rect.Empty;
            }

            layer.GeometryClip = node.HasAncestorGeometryClip ?
                CreateLayerGeometryClip(node) :
                null;
        }

        private static void PropagateLayer(VisualNode node, SceneLayer layer, SceneLayer oldLayer)
        {
            node.LayerRoot = layer.LayerRoot;

            layer.Dirty.Add(node.Bounds);
            oldLayer.Dirty.Add(node.Bounds);

            foreach (VisualNode child in node.Children)
            {
                // If the child is not the start of a new layer, recurse.
                if (child.LayerRoot != child.Visual)
                {
                    PropagateLayer(child, layer, oldLayer);
                }
            }
        }

        // HACK: Disabled layers because they're broken in current renderer. See #2244.
        private static bool ShouldStartLayer(IVisual visual) => false;

        private static IGeometryImpl CreateLayerGeometryClip(VisualNode node)
        {
            IGeometryImpl result = null;

            for (;;)
            {
                node = (VisualNode)node.Parent;

                if (node == null || (node.GeometryClip == null && !node.HasAncestorGeometryClip))
                {
                    break;
                }

                if (node?.GeometryClip != null)
                {
                    var transformed = node.GeometryClip.WithTransform(node.Transform);

                    result = result == null ? transformed : result.Intersect(transformed);
                }
            }

            return result;
        }
    }
}
