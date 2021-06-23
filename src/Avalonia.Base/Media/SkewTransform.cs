using System;
using Avalonia.VisualTree;

namespace Avalonia.Media
{
    /// <summary>
    /// Skews an <see cref="IVisual"/>.
    /// </summary>
    public class SkewTransform : Transform
    {
        /// <summary>
        /// Defines the <see cref="AngleX"/> property.
        /// </summary>
        public static readonly StyledProperty<double> AngleXProperty =
                    AvaloniaProperty.Register<SkewTransform, double>(nameof(AngleX));

        /// <summary>
        /// Defines the <see cref="AngleY"/> property.
        /// </summary>
        public static readonly StyledProperty<double> AngleYProperty =
                    AvaloniaProperty.Register<SkewTransform, double>(nameof(AngleY));

        /// <summary>
        /// Initializes a new instance of the <see cref="SkewTransform"/> class.
        /// </summary>
        public SkewTransform()
        {
            this.GetObservable(AngleXProperty).Subscribe(_ => RaiseChanged());
            this.GetObservable(AngleYProperty).Subscribe(_ => RaiseChanged());
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SkewTransform"/> class.
        /// </summary>
        /// <param name="angleX">The skew angle of X-axis, in degrees.</param>
        /// <param name="angleY">The skew angle of Y-axis, in degrees.</param>
        public SkewTransform(double angleX, double angleY) : this()
        {
            AngleX = angleX;
            AngleY = angleY;
        }

        /// <summary>
        /// Gets or sets the AngleX property.
        /// </summary>
        public double AngleX
        {
            get { return GetValue(AngleXProperty); }
            set { SetValue(AngleXProperty, value); }
        }

        /// <summary>
        /// Gets or sets the AngleY property.
        /// </summary>
        public double AngleY
        {
            get { return GetValue(AngleYProperty); }
            set { SetValue(AngleYProperty, value); }
        }

        /// <summary>
        /// Gets the transform's <see cref="Matrix"/>.
        /// </summary>
        public override Matrix Value => Matrix.CreateSkew(Matrix.ToRadians(AngleX), Matrix.ToRadians(AngleY));
    }
}
