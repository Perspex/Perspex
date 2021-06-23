using Avalonia.Media.Immutable;
using Avalonia.Metadata;

namespace Avalonia.Media
{
    /// <summary>
    /// Represents a drawing operation that combines 
    /// a geometry with and brush and/or pen to produce rendered content.
    /// </summary>
    public class GeometryDrawing : Drawing
    {
        // Adding the Pen's stroke thickness here could yield wrong results due to transforms.
        private static readonly IPen s_boundsPen = new ImmutablePen(Colors.Black.ToUint32(), 0);

        /// <summary>
        /// Defines the <see cref="Geometry"/> property.
        /// </summary>
        public static readonly StyledProperty<Geometry> GeometryProperty =
            AvaloniaProperty.Register<GeometryDrawing, Geometry>(nameof(Geometry));

        /// <summary>
        /// Defines the <see cref="Brush"/> property.
        /// </summary>
        public static readonly StyledProperty<IBrush> BrushProperty =
            AvaloniaProperty.Register<GeometryDrawing, IBrush>(nameof(Brush), Brushes.Transparent);

        /// <summary>
        /// Defines the <see cref="Pen"/> property.
        /// </summary>
        public static readonly StyledProperty<Pen> PenProperty =
            AvaloniaProperty.Register<GeometryDrawing, Pen>(nameof(Pen));

        /// <summary>
        /// Gets or sets the <see cref="Avalonia.Media.Geometry"/> that describes the shape of this <see cref="GeometryDrawing"/>.
        /// </summary>
        [Content]
        public Geometry Geometry
        {
            get => GetValue(GeometryProperty);
            set => SetValue(GeometryProperty, value);
        }

        /// <summary>
        /// Gets or sets the <see cref="Avalonia.Media.IBrush"/> used to fill the interior of the shape described by this <see cref="GeometryDrawing"/>.
        /// </summary>
        public IBrush Brush
        {
            get => GetValue(BrushProperty);
            set => SetValue(BrushProperty, value);
        }

        /// <summary>
        /// Gets or sets the <see cref="Avalonia.Media.IPen"/> used to stroke this <see cref="GeometryDrawing"/>.
        /// </summary>
        public IPen Pen
        {
            get => GetValue(PenProperty);
            set => SetValue(PenProperty, value);
        }

        public override void Draw(DrawingContext context)
        {
            if (Geometry != null)
            {
                context.DrawGeometry(Brush, Pen, Geometry);
            }
        }

        public override Rect GetBounds()
        {
            return Geometry?.GetRenderBounds(s_boundsPen) ?? Rect.Empty;
        }
    }
}
