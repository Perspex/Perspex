using System;
using Avalonia.VisualTree;

namespace Avalonia.Media
{
    /// <summary>
    /// Scale an <see cref="IVisual"/>.
    /// </summary>
    public class ScaleTransform : Transform
    {
        /// <summary>
        /// Defines the <see cref="ScaleX"/> property.
        /// </summary>
        public static readonly StyledProperty<double> ScaleXProperty =
                    AvaloniaProperty.Register<ScaleTransform, double>(nameof(ScaleX), 1);

        /// <summary>
        /// Defines the <see cref="ScaleY"/> property.
        /// </summary>
        public static readonly StyledProperty<double> ScaleYProperty =
                    AvaloniaProperty.Register<ScaleTransform, double>(nameof(ScaleY), 1);

        /// <summary>
        /// Initializes a new instance of the <see cref="ScaleTransform"/> class.
        /// </summary>
        public ScaleTransform()
        {
            this.GetObservable(ScaleXProperty).Subscribe(_ => RaiseChanged());
            this.GetObservable(ScaleYProperty).Subscribe(_ => RaiseChanged());
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ScaleTransform"/> class.
        /// </summary>
        /// <param name="scaleX">ScaleX</param>
        /// <param name="scaleY">ScaleY</param>
        public ScaleTransform(double scaleX, double scaleY)
            : this()
        {
            ScaleX = scaleX;
            ScaleY = scaleY;
        }

        /// <summary>
        /// Gets or sets the ScaleX property.
        /// </summary>
        public double ScaleX
        {
            get { return GetValue(ScaleXProperty); }
            set { SetValue(ScaleXProperty, value); }
        }

        /// <summary>
        /// Gets or sets the ScaleY property.
        /// </summary>
        public double ScaleY
        {
            get { return GetValue(ScaleYProperty); }
            set { SetValue(ScaleYProperty, value); }
        }

        /// <summary>
        /// Gets the transform's <see cref="Matrix"/>.
        /// </summary>
        public override Matrix Value => Matrix.CreateScale(ScaleX, ScaleY);
    }
}
