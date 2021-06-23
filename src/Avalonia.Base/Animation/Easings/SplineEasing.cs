namespace Avalonia.Animation.Easings
{
    /// <summary>
    /// Eases a <see cref="double"/> value
    /// using a user-defined cubic bezier curve.
    /// Good for custom easing functions that doesn't quite
    /// fit with the built-in ones. 
    /// </summary>
    public class SplineEasing : Easing
    {
        /// <summary>
        /// X coordinate of the first control point
        /// </summary>
        public double X1
        {
            get => _internalKeySpline.ControlPointX1;
            set
            {
                _internalKeySpline.ControlPointX1 = value;
            }
        }

        /// <summary>
        /// Y coordinate of the first control point
        /// </summary>
        public double Y1
        {
            get => _internalKeySpline.ControlPointY1;
            set
            {
                _internalKeySpline.ControlPointY1 = value;
            }
        }

        /// <summary>
        /// X coordinate of the second control point
        /// </summary> 
        public double X2
        {
            get => _internalKeySpline.ControlPointX2;
            set
            {
                _internalKeySpline.ControlPointX2 = value;
            }
        }

        /// <summary>
        /// Y coordinate of the second control point
        /// </summary>
        public double Y2
        {
            get => _internalKeySpline.ControlPointY2;
            set
            {
                _internalKeySpline.ControlPointY2 = value;
            }
        }

        private readonly KeySpline _internalKeySpline;

        public SplineEasing(double x1 = 0d, double y1 = 0d, double x2 = 1d, double y2 = 1d)
        {
            _internalKeySpline = new KeySpline();

            this.X1 = x1;
            this.Y1 = y1;
            this.X2 = x2;
            this.Y1 = y2;
        }

        public SplineEasing(KeySpline keySpline)
        {
            _internalKeySpline = keySpline;
        }
        
        public SplineEasing()
        {
            _internalKeySpline = new KeySpline();
        }

        /// <inheritdoc/>
        public override double Ease(double progress) =>
            _internalKeySpline.GetSplineProgress(progress);
    }
}
