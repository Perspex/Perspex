using System;
using Avalonia.Collections;
using Avalonia.Media;
using Avalonia.Media.Immutable;

#nullable enable

namespace Avalonia.Controls.Shapes
{
    /// <summary>
    /// Provides a base class for shape elements, such as <see cref="Ellipse"/>, <see cref="Polygon"/> and <see cref="Rectangle"/>.
    /// </summary>
    public abstract class Shape : Control
    {
        /// <summary>
        /// Defines the <see cref="Fill"/> property.
        /// </summary>
        public static readonly StyledProperty<IBrush?> FillProperty =
            AvaloniaProperty.Register<Shape, IBrush?>(nameof(Fill));

        /// <summary>
        /// Defines the <see cref="Stretch"/> property.
        /// </summary>
        public static readonly StyledProperty<Stretch> StretchProperty =
            AvaloniaProperty.Register<Shape, Stretch>(nameof(Stretch));

        /// <summary>
        /// Defines the <see cref="Stroke"/> property.
        /// </summary>
        public static readonly StyledProperty<IBrush?> StrokeProperty =
            AvaloniaProperty.Register<Shape, IBrush?>(nameof(Stroke));

        /// <summary>
        /// Defines the <see cref="StrokeDashArray"/> property.
        /// </summary>
        public static readonly StyledProperty<AvaloniaList<double>?> StrokeDashArrayProperty =
            AvaloniaProperty.Register<Shape, AvaloniaList<double>?>(nameof(StrokeDashArray));

        /// <summary>
        /// Defines the <see cref="StrokeDashOffset"/> property.
        /// </summary>
        public static readonly StyledProperty<double> StrokeDashOffsetProperty =
            AvaloniaProperty.Register<Shape, double>(nameof(StrokeDashOffset));

        /// <summary>
        /// Defines the <see cref="StrokeThickness"/> property.
        /// </summary>
        public static readonly StyledProperty<double> StrokeThicknessProperty =
            AvaloniaProperty.Register<Shape, double>(nameof(StrokeThickness));

        /// <summary>
        /// Defines the <see cref="StrokeLineCap"/> property.
        /// </summary>
        public static readonly StyledProperty<PenLineCap> StrokeLineCapProperty =
            AvaloniaProperty.Register<Shape, PenLineCap>(nameof(StrokeLineCap), PenLineCap.Flat);

        /// <summary>
        /// Defines the <see cref="StrokeJoin"/> property.
        /// </summary>
        public static readonly StyledProperty<PenLineJoin> StrokeJoinProperty =
            AvaloniaProperty.Register<Shape, PenLineJoin>(nameof(StrokeJoin), PenLineJoin.Miter);

        private Matrix _transform = Matrix.Identity;
        private Geometry? _definingGeometry;
        private Geometry? _renderedGeometry;

        static Shape()
        {
            AffectsMeasure<Shape>(StretchProperty, StrokeThicknessProperty);

            AffectsRender<Shape>(FillProperty, StrokeProperty, StrokeDashArrayProperty, StrokeDashOffsetProperty,
                StrokeThicknessProperty, StrokeLineCapProperty, StrokeJoinProperty);
        }

        /// <summary>
        /// Gets a value that represents the <see cref="Geometry"/> of the shape.
        /// </summary>
        public Geometry? DefiningGeometry
        {
            get
            {
                if (_definingGeometry == null)
                {
                    _definingGeometry = CreateDefiningGeometry();
                }

                return _definingGeometry;
            }
        }

        /// <summary>
        /// Gets a value that represents the final rendered <see cref="Geometry"/> of the shape.
        /// </summary>
        public Geometry? RenderedGeometry
        {
            get
            {
                if (_renderedGeometry == null && DefiningGeometry != null)
                {
                    if (_transform == Matrix.Identity)
                    {
                        _renderedGeometry = DefiningGeometry;
                    }
                    else
                    {
                        _renderedGeometry = DefiningGeometry.Clone();

                        if (_renderedGeometry.Transform == null ||
                            _renderedGeometry.Transform.Value == Matrix.Identity)
                        {
                            _renderedGeometry.Transform = new MatrixTransform(_transform);
                        }
                        else
                        {
                            _renderedGeometry.Transform = new MatrixTransform(
                                _renderedGeometry.Transform.Value * _transform);
                        }
                    }
                }

                return _renderedGeometry;
            }
        }

        /// <summary>
        /// Gets or sets the <see cref="IBrush"/> that specifies how the shape's interior is painted.
        /// </summary>
        public IBrush? Fill
        {
            get { return GetValue(FillProperty); }
            set { SetValue(FillProperty, value); }
        }

        /// <summary>
        /// Gets or sets a <see cref="Stretch"/> enumeration value that describes how the shape fills its allocated space.
        /// </summary>
        public Stretch Stretch
        {
            get { return GetValue(StretchProperty); }
            set { SetValue(StretchProperty, value); }
        }

        /// <summary>
        /// Gets or sets the <see cref="IBrush"/> that specifies how the shape's outline is painted.
        /// </summary>
        public IBrush? Stroke
        {
            get { return GetValue(StrokeProperty); }
            set { SetValue(StrokeProperty, value); }
        }

        /// <summary>
        /// Gets or sets a collection of <see cref="double"/> values that indicate the pattern of dashes and gaps that is used to outline shapes.
        /// </summary>
        public AvaloniaList<double>? StrokeDashArray
        {
            get { return GetValue(StrokeDashArrayProperty); }
            set { SetValue(StrokeDashArrayProperty, value); }
        }

        /// <summary>
        /// Gets or sets a value that specifies the distance within the dash pattern where a dash begins.
        /// </summary>
        public double StrokeDashOffset
        {
            get { return GetValue(StrokeDashOffsetProperty); }
            set { SetValue(StrokeDashOffsetProperty, value); }
        }

        /// <summary>
        /// Gets or sets the width of the shape outline.
        /// </summary>
        public double StrokeThickness
        {
            get { return GetValue(StrokeThicknessProperty); }
            set { SetValue(StrokeThicknessProperty, value); }
        }

        /// <summary>
        /// Gets or sets a <see cref="PenLineCap"/> enumeration value that describes the shape at the ends of a line.
        /// </summary>
        public PenLineCap StrokeLineCap
        {
            get { return GetValue(StrokeLineCapProperty); }
            set { SetValue(StrokeLineCapProperty, value); }
        }

        /// <summary>
        /// Gets or sets a <see cref="PenLineJoin"/> enumeration value that specifies the type of join that is used at the vertices of a Shape.
        /// </summary>
        public PenLineJoin StrokeJoin
        {
            get { return GetValue(StrokeJoinProperty); }
            set { SetValue(StrokeJoinProperty, value); }
        }

        public override void Render(DrawingContext context)
        {
            var geometry = RenderedGeometry;

            if (geometry != null)
            {
                var stroke = Stroke;

                ImmutablePen? pen = null;

                if (stroke != null)
                {
                    var strokeDashArray = StrokeDashArray;

                    ImmutableDashStyle? dashStyle = null;

                    if (strokeDashArray != null && strokeDashArray.Count > 0)
                    {
                        dashStyle = new ImmutableDashStyle(strokeDashArray, StrokeDashOffset);
                    }

                    pen = new ImmutablePen(
                        stroke.ToImmutable(),
                        StrokeThickness,
                        dashStyle,
                        StrokeLineCap,
                        StrokeJoin);
                }

                context.DrawGeometry(Fill, pen, geometry);
            }
        }

        /// <summary>
        /// Marks a property as affecting the shape's geometry.
        /// </summary>
        /// <param name="properties">The properties.</param>
        /// <remarks>
        /// After a call to this method in a control's static constructor, any change to the
        /// property will cause <see cref="InvalidateGeometry"/> to be called on the element.
        /// </remarks>
        protected static void AffectsGeometry<TShape>(params AvaloniaProperty[] properties)
            where TShape : Shape
        {
            foreach (var property in properties)
            {
                property.Changed.Subscribe(e =>
                {
                    if (e.Sender is TShape shape)
                    {
                        AffectsGeometryInvalidate(shape, e);
                    }
                });
            }
        }

        /// <summary>
        /// Creates the shape's defining geometry.
        /// </summary>
        /// <returns>Defining <see cref="Geometry"/> of the shape.</returns>
        protected abstract Geometry? CreateDefiningGeometry();

        /// <summary>
        /// Invalidates the geometry of this shape.
        /// </summary>
        protected void InvalidateGeometry()
        {
            _renderedGeometry = null;
            _definingGeometry = null;

            InvalidateMeasure();
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            if (DefiningGeometry is null)
            {
                return default;
            }

            return CalculateSizeAndTransform(availableSize, DefiningGeometry.Bounds, Stretch).size;
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            if (DefiningGeometry != null)
            {
                // This should probably use GetRenderBounds(strokeThickness) but then the calculations
                // will multiply the stroke thickness as well, which isn't correct.
                var (_, transform) = CalculateSizeAndTransform(finalSize, DefiningGeometry.Bounds, Stretch);

                if (_transform != transform)
                {
                    _transform = transform;
                    _renderedGeometry = null;
                }

                return finalSize;
            }

            return Size.Empty;
        }

        internal static (Size size, Matrix transform) CalculateSizeAndTransform(Size availableSize, Rect shapeBounds, Stretch Stretch)
        {
            Size shapeSize = new Size(shapeBounds.Right, shapeBounds.Bottom);
            Matrix translate = Matrix.Identity;
            double desiredX = availableSize.Width;
            double desiredY = availableSize.Height;
            double sx = 0.0;
            double sy = 0.0;

            if (Stretch != Stretch.None)
            {
                shapeSize = shapeBounds.Size;
                translate = Matrix.CreateTranslation(-(Vector)shapeBounds.Position);
            }

            if (double.IsInfinity(availableSize.Width))
            {
                desiredX = shapeSize.Width;
            }

            if (double.IsInfinity(availableSize.Height))
            {
                desiredY = shapeSize.Height;
            }

            if (shapeBounds.Width > 0)
            {
                sx = desiredX / shapeSize.Width;
            }

            if (shapeBounds.Height > 0)
            {
                sy = desiredY / shapeSize.Height;
            }

            if (double.IsInfinity(availableSize.Width))
            {
                sx = sy;
            }

            if (double.IsInfinity(availableSize.Height))
            {
                sy = sx;
            }

            switch (Stretch)
            {
                case Stretch.Uniform:
                    sx = sy = Math.Min(sx, sy);
                    break;
                case Stretch.UniformToFill:
                    sx = sy = Math.Max(sx, sy);
                    break;
                case Stretch.Fill:
                    if (double.IsInfinity(availableSize.Width))
                    {
                        sx = 1.0;
                    }

                    if (double.IsInfinity(availableSize.Height))
                    {
                        sy = 1.0;
                    }

                    break;
                default:
                    sx = sy = 1;
                    break;
            }

            var transform = translate * Matrix.CreateScale(sx, sy);
            var size = new Size(shapeSize.Width * sx, shapeSize.Height * sy);
            return (size, transform);
        }

        private static void AffectsGeometryInvalidate(Shape control, AvaloniaPropertyChangedEventArgs e)
        {
            // If the geometry is invalidated when Bounds changes, only invalidate when the Size
            // portion changes.
            if (e.Property == BoundsProperty)
            {
                var oldBounds = (Rect)e.OldValue!;
                var newBounds = (Rect)e.NewValue!;

                if (oldBounds.Size == newBounds.Size)
                {
                    return;
                }
            }

            control.InvalidateGeometry();
        }
    }
}
