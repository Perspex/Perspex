using System;
using System.Globalization;
#if !BUILDTASK
using Avalonia.Animation.Animators;
#endif
using Avalonia.Utilities;

namespace Avalonia
{
    /// <summary>
    /// Defines a point.
    /// </summary>
#if !BUILDTASK
    public
#endif
    readonly struct Point : IEquatable<Point>
    {
        static Point()
        {
#if !BUILDTASK
            Animation.Animation.RegisterAnimator<PointAnimator>(prop => typeof(Point).IsAssignableFrom(prop.PropertyType));
#endif
        }

        /// <summary>
        /// The X position.
        /// </summary>
        private readonly double _x;

        /// <summary>
        /// The Y position.
        /// </summary>
        private readonly double _y;

        /// <summary>
        /// Initializes a new instance of the <see cref="Point"/> structure.
        /// </summary>
        /// <param name="x">The X position.</param>
        /// <param name="y">The Y position.</param>
        public Point(double x, double y)
        {
            _x = x;
            _y = y;
        }

        /// <summary>
        /// Gets the X position.
        /// </summary>
        public double X => _x;

        /// <summary>
        /// Gets the Y position.
        /// </summary>
        public double Y => _y;

        /// <summary>
        /// Converts the <see cref="Point"/> to a <see cref="Vector"/>.
        /// </summary>
        /// <param name="p">The point.</param>
        public static implicit operator Vector(Point p)
        {
            return new Vector(p._x, p._y);
        }

        /// <summary>
        /// Negates a point.
        /// </summary>
        /// <param name="a">The point.</param>
        /// <returns>The negated point.</returns>
        public static Point operator -(Point a)
        {
            return new Point(-a._x, -a._y);
        }

        /// <summary>
        /// Checks for equality between two <see cref="Point"/>s.
        /// </summary>
        /// <param name="left">The first point.</param>
        /// <param name="right">The second point.</param>
        /// <returns>True if the points are equal; otherwise false.</returns>
        public static bool operator ==(Point left, Point right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Checks for inequality between two <see cref="Point"/>s.
        /// </summary>
        /// <param name="left">The first point.</param>
        /// <param name="right">The second point.</param>
        /// <returns>True if the points are unequal; otherwise false.</returns>
        public static bool operator !=(Point left, Point right)
        {
            return !(left == right);
        }

        /// <summary>
        /// Adds two points.
        /// </summary>
        /// <param name="a">The first point.</param>
        /// <param name="b">The second point.</param>
        /// <returns>A point that is the result of the addition.</returns>
        public static Point operator +(Point a, Point b)
        {
            return new Point(a._x + b._x, a._y + b._y);
        }

        /// <summary>
        /// Adds a vector to a point.
        /// </summary>
        /// <param name="a">The point.</param>
        /// <param name="b">The vector.</param>
        /// <returns>A point that is the result of the addition.</returns>
        public static Point operator +(Point a, Vector b)
        {
            return new Point(a._x + b.X, a._y + b.Y);
        }

        /// <summary>
        /// Subtracts two points.
        /// </summary>
        /// <param name="a">The first point.</param>
        /// <param name="b">The second point.</param>
        /// <returns>A point that is the result of the subtraction.</returns>
        public static Point operator -(Point a, Point b)
        {
            return new Point(a._x - b._x, a._y - b._y);
        }

        /// <summary>
        /// Subtracts a vector from a point.
        /// </summary>
        /// <param name="a">The point.</param>
        /// <param name="b">The vector.</param>
        /// <returns>A point that is the result of the subtraction.</returns>
        public static Point operator -(Point a, Vector b)
        {
            return new Point(a._x - b.X, a._y - b.Y);
        }

        /// <summary>
        /// Multiplies a point by a factor coordinate-wise
        /// </summary>
        /// <param name="p">Point to multiply</param>
        /// <param name="k">Factor</param>
        /// <returns>Points having its coordinates multiplied</returns>
        public static Point operator *(Point p, double k) => new Point(p.X * k, p.Y * k);

        /// <summary>
        /// Multiplies a point by a factor coordinate-wise
        /// </summary>
        /// <param name="p">Point to multiply</param>
        /// <param name="k">Factor</param>
        /// <returns>Points having its coordinates multiplied</returns>
        public static Point operator *(double k, Point p) => new Point(p.X * k, p.Y * k);

        /// <summary>
        /// Divides a point by a factor coordinate-wise
        /// </summary>
        /// <param name="p">Point to divide by</param>
        /// <param name="k">Factor</param>
        /// <returns>Points having its coordinates divided</returns>
        public static Point operator /(Point p, double k) => new Point(p.X / k, p.Y / k);

        /// <summary>
        /// Applies a matrix to a point.
        /// </summary>
        /// <param name="point">The point.</param>
        /// <param name="matrix">The matrix.</param>
        /// <returns>The resulting point.</returns>
        public static Point operator *(Point point, Matrix matrix)
        {
            return new Point(
                (point.X * matrix.M11) + (point.Y * matrix.M21) + matrix.M31,
                (point.X * matrix.M12) + (point.Y * matrix.M22) + matrix.M32);
        }

        /// <summary>
        /// Parses a <see cref="Point"/> string.
        /// </summary>
        /// <param name="s">The string.</param>
        /// <returns>The <see cref="Point"/>.</returns>
        public static Point Parse(string s)
        {
            using (var tokenizer = new StringTokenizer(s, CultureInfo.InvariantCulture, exceptionMessage: "Invalid Point."))
            {
                return new Point(
                    tokenizer.ReadDouble(),
                    tokenizer.ReadDouble()
                );
            }
        }

        /// <summary>
        /// Returns a boolean indicating whether the point is equal to the other given point.
        /// </summary>
        /// <param name="other">The other point to test equality against.</param>
        /// <returns>True if this point is equal to other; False otherwise.</returns>
        public bool Equals(Point other)
        {
            // ReSharper disable CompareOfFloatsByEqualityOperator
            return _x == other._x &&
                   _y == other._y;
            // ReSharper enable CompareOfFloatsByEqualityOperator
        }

        /// <summary>
        /// Checks for equality between a point and an object.
        /// </summary>
        /// <param name="obj">The object.</param>
        /// <returns>
        /// True if <paramref name="obj"/> is a point that equals the current point.
        /// </returns>
        public override bool Equals(object obj) => obj is Point other && Equals(other);

        /// <summary>
        /// Returns a hash code for a <see cref="Point"/>.
        /// </summary>
        /// <returns>The hash code.</returns>
        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = (hash * 23) + _x.GetHashCode();
                hash = (hash * 23) + _y.GetHashCode();
                return hash;
            }
        }

        /// <summary>
        /// Returns the string representation of the point.
        /// </summary>
        /// <returns>The string representation of the point.</returns>
        public override string ToString()
        {
            return string.Format(CultureInfo.InvariantCulture, "{0}, {1}", _x, _y);
        }

        /// <summary>
        /// Transforms the point by a matrix.
        /// </summary>
        /// <param name="transform">The transform.</param>
        /// <returns>The transformed point.</returns>
        public Point Transform(Matrix transform)
        {
            var x = X;
            var y = Y;
            var xadd = y * transform.M21 + transform.M31;
            var yadd = x * transform.M12 + transform.M32;
            x *= transform.M11;
            x += xadd;
            y *= transform.M22;
            y += yadd;
            return new Point(x, y);
        }

        /// <summary>
        /// Returns a new point with the specified X coordinate.
        /// </summary>
        /// <param name="x">The X coordinate.</param>
        /// <returns>The new point.</returns>
        public Point WithX(double x)
        {
            return new Point(x, _y);
        }

        /// <summary>
        /// Returns a new point with the specified Y coordinate.
        /// </summary>
        /// <param name="y">The Y coordinate.</param>
        /// <returns>The new point.</returns>
        public Point WithY(double y)
        {
            return new Point(_x, y);
        }

        /// <summary>
        /// Deconstructs the point into its X and Y coordinates.
        /// </summary>
        /// <param name="x">The X coordinate.</param>
        /// <param name="y">The Y coordinate.</param>
        public void Deconstruct(out double x, out double y)
        {
            x = this._x;
            y = this._y;
        }

        /// <summary>
        /// Gets a value indicating whether the X and Y coordinates are zero.
        /// </summary>
        public bool IsDefault
        {
            get { return (_x == 0) && (_y == 0); }
        }
    }
}
