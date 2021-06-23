using Avalonia.Platform;

namespace Avalonia.Media
{
    /// <summary>
    /// Represents the geometry of an ellipse or circle.
    /// </summary>
    public class EllipseGeometry : Geometry
    {
        /// <summary>
        /// Defines the <see cref="Rect"/> property.
        /// </summary>
        public static readonly StyledProperty<Rect> RectProperty =
            AvaloniaProperty.Register<EllipseGeometry, Rect>(nameof(Rect));
        
        /// <summary>
        /// Defines the <see cref="RadiusX"/> property.
        /// </summary>
        public static readonly StyledProperty<double> RadiusXProperty =
            AvaloniaProperty.Register<EllipseGeometry, double>(nameof(RadiusX));
        
        /// <summary>
        /// Defines the <see cref="RadiusY"/> property.
        /// </summary>
        public static readonly StyledProperty<double> RadiusYProperty =
            AvaloniaProperty.Register<EllipseGeometry, double>(nameof(RadiusY));

        /// <summary>
        /// Defines the <see cref="Center"/> property.
        /// </summary>
        public static readonly StyledProperty<Point> CenterProperty =
            AvaloniaProperty.Register<EllipseGeometry, Point>(nameof(Center));

        static EllipseGeometry()
        {
            AffectsGeometry(RectProperty, RadiusXProperty, RadiusYProperty, CenterProperty);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="EllipseGeometry"/> class.
        /// </summary>
        public EllipseGeometry()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="EllipseGeometry"/> class.
        /// </summary>
        /// <param name="rect">The rectangle that the ellipse should fill.</param>
        public EllipseGeometry(Rect rect) : this()
        {
            Rect = rect;
        }

        /// <summary>
        /// Gets or sets a rect that defines the bounds of the ellipse.
        /// </summary>
        public Rect Rect
        {
            get => GetValue(RectProperty);
            set => SetValue(RectProperty, value);
        }

        /// <summary>
        /// Gets or sets a double that defines the radius in the X-axis of the ellipse.
        /// </summary>
        public double RadiusX
        {
            get => GetValue(RadiusXProperty);
            set => SetValue(RadiusXProperty, value);
        }

        /// <summary>
        /// Gets or sets a double that defines the radius in the Y-axis of the ellipse.
        /// </summary>
        public double RadiusY
        {
            get => GetValue(RadiusYProperty);
            set => SetValue(RadiusYProperty, value);
        }

        /// <summary>
        /// Gets or sets a point that defines the center of the ellipse.
        /// </summary>
        public Point Center
        {
            get => GetValue(CenterProperty);
            set => SetValue(CenterProperty, value);
        }

        /// <inheritdoc/>
        public override Geometry Clone()
        {
            return new EllipseGeometry(Rect);
        }

        /// <inheritdoc/>
        protected override IGeometryImpl CreateDefiningGeometry()
        {
            var factory = AvaloniaLocator.Current.GetService<IPlatformRenderInterface>();

            if (Rect != default) return factory.CreateEllipseGeometry(Rect);
            
            var originX = Center.X - RadiusX;
            var originY = Center.Y - RadiusY;
            var width = RadiusX * 2;
            var height = RadiusY * 2;
            
            return factory.CreateEllipseGeometry(new Rect(originX, originY, width, height));
        }
    }
}
