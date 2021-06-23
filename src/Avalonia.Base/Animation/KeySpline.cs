using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Text;
using Avalonia;
using Avalonia.Utilities;

// Ported from WPF open-source code.
// https://github.com/dotnet/wpf/blob/ae1790531c3b993b56eba8b1f0dd395a3ed7de75/src/Microsoft.DotNet.Wpf/src/PresentationCore/System/Windows/Media/Animation/KeySpline.cs

namespace Avalonia.Animation
{
    /// <summary>
    /// Determines how an animation is used based on a cubic bezier curve.
    /// X1 and X2 must be between 0.0 and 1.0, inclusive.
    /// See https://docs.microsoft.com/en-us/dotnet/api/system.windows.media.animation.keyspline
    /// </summary>
    [TypeConverter(typeof(KeySplineTypeConverter))]
    public class KeySpline : AvaloniaObject
    {
        // Control points
        private double _controlPointX1;
        private double _controlPointY1;
        private double _controlPointX2;
        private double _controlPointY2;
        private bool _isSpecified;
        private bool _isDirty;

        // The parameter that corresponds to the most recent time
        private double _parameter;

        // Cached coefficients
        private double _Bx;        // 3*points[0].X
        private double _Cx;        // 3*points[1].X
        private double _Cx_Bx;     // 2*(Cx - Bx)
        private double _three_Cx;  // 3 - Cx

        private double _By;        // 3*points[0].Y
        private double _Cy;        // 3*points[1].Y

        // constants
        private const double _accuracy = .001;   // 1/3 the desired accuracy in X
        private const double _fuzz = .000001;    // computational zero

        /// <summary>
        /// Create a <see cref="KeySpline"/> with X1 = Y1 = 0 and X2 = Y2 = 1.
        /// </summary>
        public KeySpline()
        {
            _controlPointX1 = 0.0;
            _controlPointY1 = 0.0;
            _controlPointX2 = 1.0;
            _controlPointY2 = 1.0;
            _isDirty = true;
        }

        /// <summary>
        /// Create a <see cref="KeySpline"/> with the given parameters
        /// </summary>
        /// <param name="x1">X coordinate for the first control point</param>
        /// <param name="y1">Y coordinate for the first control point</param>
        /// <param name="x2">X coordinate for the second control point</param>
        /// <param name="y2">Y coordinate for the second control point</param>
        public KeySpline(double x1, double y1, double x2, double y2)
        {
            _controlPointX1 = x1;
            _controlPointY1 = y1;
            _controlPointX2 = x2;
            _controlPointY2 = y2;
            _isDirty = true;
        }

        /// <summary>
        /// Parse a <see cref="KeySpline"/> from a string. The string
        /// needs to contain 4 values in it for the 2 control points.
        /// </summary>
        /// <param name="value">string with 4 values in it</param>
        /// <param name="culture">culture of the string</param>
        /// <exception cref="FormatException">Thrown if the string does not have 4 values</exception>
        /// <returns>A <see cref="KeySpline"/> with the appropriate values set</returns>
        public static KeySpline Parse(string value, CultureInfo culture)
        {
            if (culture is null)
                culture = CultureInfo.InvariantCulture;

            using (var tokenizer = new StringTokenizer((string)value, culture, exceptionMessage: $"Invalid KeySpline string: \"{value}\"."))
            {
                return new KeySpline(tokenizer.ReadDouble(), tokenizer.ReadDouble(), tokenizer.ReadDouble(), tokenizer.ReadDouble());
            }
        }

        /// <summary>
        /// X coordinate of the first control point
        /// </summary>
        public double ControlPointX1
        {
            get => _controlPointX1;
            set
            {
                if (IsValidXValue(value))
                {
                    _controlPointX1 = value;
                    _isDirty = true;
                }
                else
                {
                    throw new ArgumentException("Invalid KeySpline X1 value. Must be >= 0.0 and <= 1.0.");
                }
            }
        }

        /// <summary>
        /// Y coordinate of the first control point
        /// </summary>
        public double ControlPointY1
        {
            get => _controlPointY1;
            set
            {
                _controlPointY1 = value;
                _isDirty = true;
            }
        }

        /// <summary>
        /// X coordinate of the second control point
        /// </summary>
        public double ControlPointX2
        {
            get => _controlPointX2;
            set
            {
                if (IsValidXValue(value))
                {
                    _controlPointX2 = value;
                    _isDirty = true;
                }
                else
                {
                    throw new ArgumentException("Invalid KeySpline X2 value. Must be >= 0.0 and <= 1.0.");
                }
            }
        }

        /// <summary>
        /// Y coordinate of the second control point
        /// </summary>
        public double ControlPointY2
        {
            get => _controlPointY2;
            set
            {
                _controlPointY2 = value;
                _isDirty = true;
            }
        }

        /// <summary>
        /// Calculates spline progress from a linear progress.
        /// </summary>
        /// <param name="linearProgress">the linear progress</param>
        /// <returns>the spline progress</returns>
        public double GetSplineProgress(double linearProgress)
        {
            if (_isDirty)
            {
                Build();
            }

            if (!_isSpecified)
            {
                return linearProgress;
            }
            else
            {
                SetParameterFromX(linearProgress);

                return GetBezierValue(_By, _Cy, _parameter);
            }
        }

        /// <summary>
        /// Check to see whether the <see cref="KeySpline"/> is valid by looking
        /// at its X values.
        /// </summary>
        /// <returns>true if the X values for this <see cref="KeySpline"/> fall in 
        /// acceptable range; false otherwise.</returns>
        public bool IsValid()
        {
            return IsValidXValue(_controlPointX1) && IsValidXValue(_controlPointX2);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        private bool IsValidXValue(double value)
        {
            return value >= 0.0 && value <= 1.0;
        }

        /// <summary>
        /// Compute cached coefficients.
        /// </summary>
        private void Build()
        {
            if (_controlPointX1 == 0 && _controlPointY1 == 0 && _controlPointX2 == 1 && _controlPointY2 == 1)
            {
                // This KeySpline would have no effect on the progress.
                _isSpecified = false;
            }
            else
            {
                _isSpecified = true;

                _parameter = 0;

                // X coefficients
                _Bx = 3 * _controlPointX1;
                _Cx = 3 * _controlPointX2;
                _Cx_Bx = 2 * (_Cx - _Bx);
                _three_Cx = 3 - _Cx;

                // Y coefficients
                _By = 3 * _controlPointY1;
                _Cy = 3 * _controlPointY2;
            }

            _isDirty = false;
        }

        /// <summary>
        /// Get an X or Y value with the Bezier formula.
        /// </summary>
        /// <param name="b">the second Bezier coefficient</param>
        /// <param name="c">the third Bezier coefficient</param>
        /// <param name="t">the parameter value to evaluate at</param>
        /// <returns>the value of the Bezier function at the given parameter</returns>
        static private double GetBezierValue(double b, double c, double t)
        {
            double s = 1.0 - t;
            double t2 = t * t;

            return b * t * s * s + c * t2 * s + t2 * t;
        }

        /// <summary>
        /// Get X and dX/dt at a given parameter
        /// </summary>
        /// <param name="t">the parameter value to evaluate at</param>
        /// <param name="x">the value of x there</param>
        /// <param name="dx">the value of dx/dt there</param>
        private void GetXAndDx(double t, out double x, out double dx)
        {
            double s = 1.0 - t;
            double t2 = t * t;
            double s2 = s * s;

            x = _Bx * t * s2 + _Cx * t2 * s + t2 * t;
            dx = _Bx * s2 + _Cx_Bx * s * t + _three_Cx * t2;
        }

        /// <summary>
        /// Compute the parameter value that corresponds to a given X value, using a modified
        /// clamped Newton-Raphson algorithm to solve the equation X(t) - time = 0. We make 
        /// use of some known properties of this particular function:
        /// * We are only interested in solutions in the interval [0,1]
        /// * X(t) is increasing, so we can assume that if X(t) > time t > solution.  We use
        ///   that to clamp down the search interval with every probe.
        /// * The derivative of X and Y are between 0 and 3.
        /// </summary>
        /// <param name="time">the time, scaled to fit in [0,1]</param>
        private void SetParameterFromX(double time)
        {
            // Dynamic search interval to clamp with
            double bottom = 0;
            double top = 1;

            if (time == 0)
            {
                _parameter = 0;
            }
            else if (time == 1)
            {
                _parameter = 1;
            }
            else
            {
                // Loop while improving the guess
                while (top - bottom > _fuzz)
                {
                    double x, dx, absdx;

                    // Get x and dx/dt at the current parameter
                    GetXAndDx(_parameter, out x, out dx);
                    absdx = Math.Abs(dx);

                    // Clamp down the search interval, relying on the monotonicity of X(t)
                    if (x > time)
                    {
                        top = _parameter;      // because parameter > solution
                    }
                    else
                    {
                        bottom = _parameter;  // because parameter < solution
                    }

                    // The desired accuracy is in ultimately in y, not in x, so the
                    // accuracy needs to be multiplied by dx/dy = (dx/dt) / (dy/dt).
                    // But dy/dt <=3, so we omit that
                    if (Math.Abs(x - time) < _accuracy * absdx)
                    {
                        break; // We're there
                    }

                    if (absdx > _fuzz)
                    {
                        // Nonzero derivative, use Newton-Raphson to obtain the next guess
                        double next = _parameter - (x - time) / dx;

                        // If next guess is out of the search interval then clamp it in
                        if (next >= top)
                        {
                            _parameter = (_parameter + top) / 2;
                        }
                        else if (next <= bottom)
                        {
                            _parameter = (_parameter + bottom) / 2;
                        }
                        else
                        {
                            // Next guess is inside the search interval, accept it
                            _parameter = next;
                        }
                    }
                    else    // Zero derivative, halve the search interval
                    {
                        _parameter = (bottom + top) / 2;
                    }
                }
            }
        }
    }
}
