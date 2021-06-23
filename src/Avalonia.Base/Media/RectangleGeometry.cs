using Avalonia.Platform;

namespace Avalonia.Media
{
    /// <summary>
    /// Represents the geometry of a rectangle.
    /// </summary>
    public class RectangleGeometry : Geometry
    {
        /// <summary>
        /// Defines the <see cref="Rect"/> property.
        /// </summary>
        public static readonly StyledProperty<Rect> RectProperty =
            AvaloniaProperty.Register<RectangleGeometry, Rect>(nameof(Rect));

        static RectangleGeometry()
        {
            AffectsGeometry(RectProperty);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RectangleGeometry"/> class.
        /// </summary>
        public RectangleGeometry()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RectangleGeometry"/> class.
        /// </summary>
        /// <param name="rect">The rectangle bounds.</param>
        public RectangleGeometry(Rect rect)
        {
            Rect = rect;
        }

        /// <summary>
        /// Gets or sets the bounds of the rectangle.
        /// </summary>
        public Rect Rect
        {
            get => GetValue(RectProperty);
            set => SetValue(RectProperty, value);
        }

        /// <inheritdoc/>
        public override Geometry Clone() => new RectangleGeometry(Rect);

        protected override IGeometryImpl CreateDefiningGeometry()
        {
            var factory = AvaloniaLocator.Current.GetService<IPlatformRenderInterface>();

            return factory.CreateRectangleGeometry(Rect);
        }
    }
}
