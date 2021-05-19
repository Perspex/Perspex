using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.VisualTree;

namespace Avalonia.Rendering.SceneGraph
{
    /// <summary>
    /// Represents a layer in a <see cref="Scene"/>.
    /// </summary>
    public class SceneLayer
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SceneLayer"/> class.
        /// </summary>
        /// <param name="layerRoot">The visual at the root of the layer.</param>
        /// <param name="distanceFromRoot">The distance from the scene root.</param>
        public SceneLayer(IVisual layerRoot, int distanceFromRoot)
        {
            LayerRoot = layerRoot;
            Dirty = new DirtyRects(layerRoot);
            DistanceFromRoot = distanceFromRoot;
        }

        /// <summary>
        /// Clones the layer.
        /// </summary>
        /// <returns>The cloned layer.</returns>
        public SceneLayer Clone()
        {
            return new SceneLayer(LayerRoot, DistanceFromRoot)
            {
                Opacity = Opacity,
                OpacityMask = OpacityMask,
                OpacityMaskRect = OpacityMaskRect,
                GeometryClip = GeometryClip,
            };
        }

        /// <summary>
        /// Gets the visual at the root of the layer.
        /// </summary>
        public IVisual LayerRoot { get; }

        /// <summary>
        /// Gets the distance of the layer root from the root of the scene.
        /// </summary>
        public int DistanceFromRoot { get; }

        /// <summary>
        /// Gets or sets the opacity of the layer.
        /// </summary>
        public double Opacity { get; set; } = 1;

        /// <summary>
        /// Gets or sets the opacity mask for the layer.
        /// </summary>
        public IBrush OpacityMask { get; set; }

        /// <summary>
        /// Gets or sets the target rectangle for the layer opacity mask.
        /// </summary>
        public Rect OpacityMaskRect { get; set; }

        /// <summary>
        /// Gets the layer's geometry clip.
        /// </summary>
        public IGeometryImpl GeometryClip { get; set; }

        /// <summary>
        /// Gets the dirty rectangles for the layer.
        /// </summary>
        internal DirtyRects Dirty { get; }
    }
}
