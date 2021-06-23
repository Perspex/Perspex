using Avalonia.Media.Immutable;
using Avalonia.VisualTree;

namespace Avalonia.Media
{
    /// <summary>
    /// Paints an area with an <see cref="IVisual"/>.
    /// </summary>
    public class VisualBrush : TileBrush, IVisualBrush
    {
        /// <summary>
        /// Defines the <see cref="Visual"/> property.
        /// </summary>
        public static readonly StyledProperty<IVisual> VisualProperty =
            AvaloniaProperty.Register<VisualBrush, IVisual>(nameof(Visual));

        static VisualBrush()
        {
            AffectsRender<VisualBrush>(VisualProperty);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="VisualBrush"/> class.
        /// </summary>
        public VisualBrush()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="VisualBrush"/> class.
        /// </summary>
        /// <param name="visual">The visual to draw.</param>
        public VisualBrush(IVisual visual)
        {
            Visual = visual;
        }

        /// <summary>
        /// Gets or sets the visual to draw.
        /// </summary>
        public IVisual Visual
        {
            get { return GetValue(VisualProperty); }
            set { SetValue(VisualProperty, value); }
        }

        /// <inheritdoc/>
        public override IBrush ToImmutable()
        {
            return new ImmutableVisualBrush(this);
        }
    }
}
