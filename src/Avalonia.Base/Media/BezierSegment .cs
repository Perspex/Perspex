namespace Avalonia.Media
{
    public sealed class BezierSegment : PathSegment
    {
        /// <summary>
        /// Defines the <see cref="Point1"/> property.
        /// </summary>
        public static readonly StyledProperty<Point> Point1Property
                        = AvaloniaProperty.Register<BezierSegment, Point>(nameof(Point1));
        /// <summary>
        /// Defines the <see cref="Point2"/> property.
        /// </summary>
        public static readonly StyledProperty<Point> Point2Property
                        = AvaloniaProperty.Register<BezierSegment, Point>(nameof(Point2));
        /// <summary>
        /// Defines the <see cref="Point3"/> property.
        /// </summary>
        public static readonly StyledProperty<Point> Point3Property
                        = AvaloniaProperty.Register<BezierSegment, Point>(nameof(Point3));

        /// <summary>
        /// Gets or sets the point1.
        /// </summary>
        /// <value>
        /// The point1.
        /// </value>
        public Point Point1
        {
            get { return GetValue(Point1Property); }
            set { SetValue(Point1Property, value); }
        }

        /// <summary>
        /// Gets or sets the point2.
        /// </summary>
        /// <value>
        /// The point2.
        /// </value>
        public Point Point2
        {
            get { return GetValue(Point2Property); }
            set { SetValue(Point2Property, value); }
        }

        /// <summary>
        /// Gets or sets the point3.
        /// </summary>
        /// <value>
        /// The point3.
        /// </value>
        public Point Point3
        {
            get { return GetValue(Point3Property); }
            set { SetValue(Point3Property, value); }
        }

        protected internal override void ApplyTo(StreamGeometryContext ctx)
        {
            ctx.CubicBezierTo(Point1, Point2, Point3);
        }

        public override string ToString()
            => $"C {Point1} {Point2} {Point3}";
    }
}