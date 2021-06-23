#nullable enable
using System;
using System.Linq;
using Avalonia.Collections;
using Avalonia.Metadata;

namespace Avalonia.Media
{
    public sealed class PathFigure : AvaloniaObject
    {
        /// <summary>
        /// Defines the <see cref="IsClosed"/> property.
        /// </summary>
        public static readonly StyledProperty<bool> IsClosedProperty
            = AvaloniaProperty.Register<PathFigure, bool>(nameof(IsClosed), true);

        /// <summary>
        /// Defines the <see cref="IsFilled"/> property.
        /// </summary>
        public static readonly StyledProperty<bool> IsFilledProperty
            = AvaloniaProperty.Register<PathFigure, bool>(nameof(IsFilled), true);

        /// <summary>
        /// Defines the <see cref="Segments"/> property.
        /// </summary>
        public static readonly DirectProperty<PathFigure, PathSegments?> SegmentsProperty
            = AvaloniaProperty.RegisterDirect<PathFigure, PathSegments?>(
                nameof(Segments), 
                f => f.Segments,
                (f, s) => f.Segments = s);

        /// <summary>
        /// Defines the <see cref="StartPoint"/> property.
        /// </summary>
        public static readonly StyledProperty<Point> StartPointProperty
            = AvaloniaProperty.Register<PathFigure, Point>(nameof(StartPoint));

        internal event EventHandler? SegmentsInvalidated;

        private PathSegments? _segments;

        private IDisposable? _segmentsDisposable;

        private IDisposable? _segmentsPropertiesDisposable;

        /// <summary>
        /// Initializes a new instance of the <see cref="PathFigure"/> class.
        /// </summary>
        public PathFigure()
        {
            Segments = new PathSegments();
        }

        static PathFigure()
        {
            SegmentsProperty.Changed.AddClassHandler<PathFigure>(
                (s, e) =>
                s.OnSegmentsChanged());
        }

        private void OnSegmentsChanged()
        {
            _segmentsDisposable?.Dispose();
            _segmentsPropertiesDisposable?.Dispose();

            _segmentsDisposable = _segments?.ForEachItem(
                _ => InvalidateSegments(),
                _ => InvalidateSegments(),
                InvalidateSegments);

            _segmentsPropertiesDisposable = _segments?.TrackItemPropertyChanged(_ => InvalidateSegments());
        }

        private void InvalidateSegments()
        {
            SegmentsInvalidated?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is closed.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance is closed; otherwise, <c>false</c>.
        /// </value>
        public bool IsClosed
        {
            get { return GetValue(IsClosedProperty); }
            set { SetValue(IsClosedProperty, value); }
        }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is filled.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance is filled; otherwise, <c>false</c>.
        /// </value>
        public bool IsFilled
        {
            get { return GetValue(IsFilledProperty); }
            set { SetValue(IsFilledProperty, value); }
        }

        /// <summary>
        /// Gets or sets the segments.
        /// </summary>
        /// <value>
        /// The segments.
        /// </value>
        [Content]
        public PathSegments? Segments
        {
            get { return _segments; }
            set { SetAndRaise(SegmentsProperty, ref _segments, value); }
        }

        /// <summary>
        /// Gets or sets the start point.
        /// </summary>
        /// <value>
        /// The start point.
        /// </value>
        public Point StartPoint
        {
            get { return GetValue(StartPointProperty); }
            set { SetValue(StartPointProperty, value); }
        }
        
        public override string ToString()
            => $"M {StartPoint} {string.Join(" ", _segments ?? Enumerable.Empty<PathSegment>())}{(IsClosed ? "Z" : "")}";

        internal void ApplyTo(StreamGeometryContext ctx)
        {
            ctx.BeginFigure(StartPoint, IsFilled);

            if (Segments != null)
            {
                foreach (var segment in Segments)
                {
                    segment.ApplyTo(ctx);
                }
            }

            ctx.EndFigure(IsClosed);
        }
    }
}
