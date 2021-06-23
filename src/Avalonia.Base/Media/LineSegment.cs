namespace Avalonia.Media
{
    public sealed class LineSegment : PathSegment
    {
        /// <summary>
        /// Defines the <see cref="Point"/> property.
        /// </summary>
        public static readonly StyledProperty<Point> PointProperty
                        = AvaloniaProperty.Register<LineSegment, Point>(nameof(Point));

        /// <summary>
        /// Gets or sets the point.
        /// </summary>
        /// <value>
        /// The point.
        /// </value>
        public Point Point
        {
            get { return GetValue(PointProperty); }
            set { SetValue(PointProperty, value); }
        }

        protected internal override void ApplyTo(StreamGeometryContext ctx)
        {
            ctx.LineTo(Point);
        }

        public override string ToString()
            => $"L {Point}";
    }
}