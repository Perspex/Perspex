using System;
using System.Collections.Generic;
using Avalonia.Collections;
using Avalonia.Metadata;
using Avalonia.Platform;

namespace Avalonia.Media
{
    /// <summary>
    /// Represents the geometry of an polyline or polygon.
    /// </summary>
    public class PolylineGeometry : Geometry
    {
        /// <summary>
        /// Defines the <see cref="Points"/> property.
        /// </summary>
        public static readonly DirectProperty<PolylineGeometry, Points> PointsProperty =
            AvaloniaProperty.RegisterDirect<PolylineGeometry, Points>(nameof(Points), g => g.Points, (g, f) => g.Points = f);

        /// <summary>
        /// Defines the <see cref="IsFilled"/> property.
        /// </summary>
        public static readonly StyledProperty<bool> IsFilledProperty =
            AvaloniaProperty.Register<PolylineGeometry, bool>(nameof(IsFilled));

        private Points _points;
        private IDisposable _pointsObserver;

        static PolylineGeometry()
        {
            AffectsGeometry(IsFilledProperty);
            PointsProperty.Changed.AddClassHandler<PolylineGeometry>((s, e) => s.OnPointsChanged(e.NewValue as Points));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PolylineGeometry"/> class.
        /// </summary>
        public PolylineGeometry()
        {
            Points = new Points();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PolylineGeometry"/> class.
        /// </summary>
        public PolylineGeometry(IEnumerable<Point> points, bool isFilled) : this()
        {
            Points.AddRange(points);
            IsFilled = isFilled;
        }

        /// <summary>
        /// Gets or sets the figures.
        /// </summary>
        /// <value>
        /// The points.
        /// </value>
        [Content]
        public Points Points
        {
            get => _points;
            set => SetAndRaise(PointsProperty, ref _points, value);
        }

        public bool IsFilled
        {
            get => GetValue(IsFilledProperty);
            set => SetValue(IsFilledProperty, value);
        }

        /// <inheritdoc/>
        public override Geometry Clone()
        {
            return new PolylineGeometry(Points, IsFilled);
        }

        protected override IGeometryImpl CreateDefiningGeometry()
        {
            var factory = AvaloniaLocator.Current.GetService<IPlatformRenderInterface>();
            var geometry = factory.CreateStreamGeometry();

            using (var context = geometry.Open())
            {
                var points = Points;
                var isFilled = IsFilled;
                if (points.Count > 0)
                {
                    context.BeginFigure(points[0], isFilled);
                    for (int i = 1; i < points.Count; i++)
                    {
                        context.LineTo(points[i]);
                    }
                    context.EndFigure(isFilled);
                }
            }

            return geometry;
        }

        private void OnPointsChanged(Points newValue)
        {
            _pointsObserver?.Dispose();
            _pointsObserver = newValue?.ForEachItem(
                _ => InvalidateGeometry(),
                _ => InvalidateGeometry(),
                InvalidateGeometry);
        }
    }
}
