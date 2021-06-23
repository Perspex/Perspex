using System;
using Avalonia.VisualTree;

namespace Avalonia.Media
{
    /// <summary>
    /// Rotates an <see cref="IVisual"/>.
    /// </summary>
    public class RotateTransform : Transform
    {
        /// <summary>
        /// Defines the <see cref="Angle"/> property.
        /// </summary>
        public static readonly StyledProperty<double> AngleProperty =
            AvaloniaProperty.Register<RotateTransform, double>(nameof(Angle));

        /// <summary>
        /// Initializes a new instance of the <see cref="RotateTransform"/> class.
        /// </summary>
        public RotateTransform()
        {
            this.GetObservable(AngleProperty).Subscribe(_ => RaiseChanged());
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RotateTransform"/> class.
        /// </summary>
        /// <param name="angle">The angle, in degrees.</param>
        public RotateTransform(double angle)
            : this()
        {
            Angle = angle;
        }

        /// <summary>
        /// Gets or sets the angle of rotation, in degrees.
        /// </summary>
        public double Angle
        {
            get { return GetValue(AngleProperty); }
            set { SetValue(AngleProperty, value); }
        }

        /// <summary>
        /// Gets the transform's <see cref="Matrix"/>.
        /// </summary>
        public override Matrix Value => Matrix.CreateRotation(Matrix.ToRadians(Angle));
    }
}
