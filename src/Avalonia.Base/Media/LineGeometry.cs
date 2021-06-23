using Avalonia.Platform;

namespace Avalonia.Media
{
    /// <summary>
    /// Represents the geometry of a line.
    /// </summary>
    public class LineGeometry : Geometry
    {
        /// <summary>
        /// Defines the <see cref="StartPoint"/> property.
        /// </summary>
        public static readonly StyledProperty<Point> StartPointProperty =
            AvaloniaProperty.Register<LineGeometry, Point>(nameof(StartPoint));

        /// <summary>
        /// Defines the <see cref="EndPoint"/> property.
        /// </summary>
        public static readonly StyledProperty<Point> EndPointProperty =
            AvaloniaProperty.Register<LineGeometry, Point>(nameof(EndPoint));

        static LineGeometry()
        {
            AffectsGeometry(StartPointProperty, EndPointProperty);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LineGeometry"/> class.
        /// </summary>
        public LineGeometry()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LineGeometry"/> class.
        /// </summary>
        /// <param name="startPoint">The start point.</param>
        /// <param name="endPoint">The end point.</param>
        public LineGeometry(Point startPoint, Point endPoint) : this()
        {
            StartPoint = startPoint;
            EndPoint = endPoint;
        }

        /// <summary>
        /// Gets or sets the start point of the line.
        /// </summary>
        public Point StartPoint
        {
            get => GetValue(StartPointProperty);
            set => SetValue(StartPointProperty, value);
        }

        /// <summary>
        /// Gets or sets the end point of the line.
        /// </summary>
        public Point EndPoint
        {
            get => GetValue(EndPointProperty);
            set => SetValue(EndPointProperty, value);
        }

        /// <inheritdoc/>
        public override Geometry Clone()
        {
            return new LineGeometry(StartPoint, EndPoint);
        }

        /// <inheritdoc/>
        protected override IGeometryImpl CreateDefiningGeometry()
        {
            var factory = AvaloniaLocator.Current.GetService<IPlatformRenderInterface>();

            return factory.CreateLineGeometry(StartPoint, EndPoint);
        }
    }
}
