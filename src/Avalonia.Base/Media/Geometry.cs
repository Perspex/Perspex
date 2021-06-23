using System;
using Avalonia.Platform;

namespace Avalonia.Media
{
    /// <summary>
    /// Defines a geometric shape.
    /// </summary>    
    public abstract class Geometry : AvaloniaObject
    {
        /// <summary>
        /// Defines the <see cref="Transform"/> property.
        /// </summary>
        public static readonly StyledProperty<Transform> TransformProperty =
            AvaloniaProperty.Register<Geometry, Transform>(nameof(Transform));

        private bool _isDirty = true;
        private IGeometryImpl _platformImpl;

        static Geometry()
        {
            TransformProperty.Changed.AddClassHandler<Geometry>((x,e) => x.TransformChanged(e));
        }

        /// <summary>
        /// Raised when the geometry changes.
        /// </summary>
        public event EventHandler Changed;

        /// <summary>
        /// Gets the geometry's bounding rectangle.
        /// </summary>
        public Rect Bounds => PlatformImpl?.Bounds ?? Rect.Empty;

        /// <summary>
        /// Gets the platform-specific implementation of the geometry.
        /// </summary>
        public IGeometryImpl PlatformImpl
        {
            get
            {
                if (_isDirty)
                {
                    var geometry = CreateDefiningGeometry();
                    var transform = Transform;

                    if (geometry != null && transform != null && transform.Value != Matrix.Identity)
                    {
                        geometry = geometry.WithTransform(transform.Value);
                    }

                    _platformImpl = geometry;
                    _isDirty = false;
                }

                return _platformImpl;
            }
        }

        /// <summary>
        /// Gets or sets a transform to apply to the geometry.
        /// </summary>
        public Transform Transform
        {
            get { return GetValue(TransformProperty); }
            set { SetValue(TransformProperty, value); }
        }

        /// <summary>
        /// Creates a <see cref="Geometry"/> from a string.
        /// </summary>
        /// <param name="s">The string.</param>
        /// <returns>A <see cref="StreamGeometry"/>.</returns>
        public static Geometry Parse(string s) => StreamGeometry.Parse(s);

        /// <summary>
        /// Clones the geometry.
        /// </summary>
        /// <returns>A cloned geometry.</returns>
        public abstract Geometry Clone();

        /// <summary>
        /// Gets the geometry's bounding rectangle with the specified pen.
        /// </summary>
        /// <param name="pen">The stroke thickness.</param>
        /// <returns>The bounding rectangle.</returns>
        public Rect GetRenderBounds(IPen pen) => PlatformImpl?.GetRenderBounds(pen) ?? Rect.Empty;

        /// <summary>
        /// Indicates whether the geometry's fill contains the specified point.
        /// </summary>
        /// <param name="point">The point.</param>
        /// <returns><c>true</c> if the geometry contains the point; otherwise, <c>false</c>.</returns>
        public bool FillContains(Point point)
        {
            return PlatformImpl?.FillContains(point) == true;
        }

        /// <summary>
        /// Indicates whether the geometry's stroke contains the specified point.
        /// </summary>
        /// <param name="pen">The pen to use.</param>
        /// <param name="point">The point.</param>
        /// <returns><c>true</c> if the geometry contains the point; otherwise, <c>false</c>.</returns>
        public bool StrokeContains(IPen pen, Point point)
        {
            return PlatformImpl?.StrokeContains(pen, point) == true;
        }

        /// <summary>
        /// Marks a property as affecting the geometry's <see cref="PlatformImpl"/>.
        /// </summary>
        /// <param name="properties">The properties.</param>
        /// <remarks>
        /// After a call to this method in a control's static constructor, any change to the
        /// property will cause <see cref="InvalidateGeometry"/> to be called on the element.
        /// </remarks>
        protected static void AffectsGeometry(params AvaloniaProperty[] properties)
        {
            foreach (var property in properties)
            {
                property.Changed.Subscribe(AffectsGeometryInvalidate);
            }
        }

        /// <summary>
        /// Creates the platform implementation of the geometry, without the transform applied.
        /// </summary>
        /// <returns></returns>
        protected abstract IGeometryImpl CreateDefiningGeometry();

        /// <summary>
        /// Invalidates the platform implementation of the geometry.
        /// </summary>
        protected void InvalidateGeometry()
        {
            _isDirty = true;
            _platformImpl = null;
            Changed?.Invoke(this, EventArgs.Empty);
        }

        private void TransformChanged(AvaloniaPropertyChangedEventArgs e)
        {
            var oldValue = (Transform)e.OldValue;
            var newValue = (Transform)e.NewValue;

            if (oldValue != null)
            {
                oldValue.Changed -= TransformChanged;
            }

            if (newValue != null)
            {
                newValue.Changed += TransformChanged;
            }

            TransformChanged(newValue, EventArgs.Empty);
        }

        private void TransformChanged(object sender, EventArgs e)
        {
            var transform = ((Transform)sender)?.Value;

            if (_platformImpl is ITransformedGeometryImpl t)
            {
                if (transform == null || transform == Matrix.Identity)
                {
                    _platformImpl = t.SourceGeometry;
                }
                else if (transform != t.Transform)
                {
                    _platformImpl = t.SourceGeometry.WithTransform(transform.Value);
                }
            }
            else if (_platformImpl != null && transform != null && transform != Matrix.Identity)
            {
                _platformImpl = PlatformImpl.WithTransform(transform.Value);
            }

            Changed?.Invoke(this, EventArgs.Empty);
        }

        private static void AffectsGeometryInvalidate(AvaloniaPropertyChangedEventArgs e)
        {
            var control = e.Sender as Geometry;
            control?.InvalidateGeometry();
        }
    }
}
