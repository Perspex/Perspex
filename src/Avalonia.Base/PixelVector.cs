using System;
using System.Globalization;
using Avalonia.Animation.Animators;
using JetBrains.Annotations;

namespace Avalonia
{
    /// <summary>
    /// Defines a vector.
    /// </summary>
    public readonly struct PixelVector
    {
        /// <summary>
        /// The X vector.
        /// </summary>
        private readonly int _x;

        /// <summary>
        /// The Y vector.
        /// </summary>
        private readonly int _y;

        /// <summary>
        /// Initializes a new instance of the <see cref="PixelVector"/> structure.
        /// </summary>
        /// <param name="x">The X vector.</param>
        /// <param name="y">The Y vector.</param>
        public PixelVector(int x, int y)
        {
            _x = x;
            _y = y;
        }

        /// <summary>
        /// Gets the X vector.
        /// </summary>
        public int X => _x;

        /// <summary>
        /// Gets the Y vector.
        /// </summary>
        public int Y => _y;

        /// <summary>
        /// Converts the <see cref="PixelVector"/> to a <see cref="PixelPoint"/>.
        /// </summary>
        /// <param name="a">The vector.</param>
        public static explicit operator PixelPoint(PixelVector a)
        {
            return new PixelPoint(a._x, a._y);
        }

        /// <summary>
        /// Calculates the dot product of two vectors
        /// </summary>
        /// <param name="a">First vector</param>
        /// <param name="b">Second vector</param>
        /// <returns>The dot product</returns>
        public static int operator *(PixelVector a, PixelVector b)
        {
            return a.X * b.X + a.Y * b.Y;
        }

        /// <summary>
        /// Scales a vector.
        /// </summary>
        /// <param name="vector">The vector</param>
        /// <param name="scale">The scaling factor.</param>
        /// <returns>The scaled vector.</returns>
        public static PixelVector operator *(PixelVector vector, int scale)
        {
            return new PixelVector(vector._x * scale, vector._y * scale);
        }

        /// <summary>
        /// Scales a vector.
        /// </summary>
        /// <param name="vector">The vector</param>
        /// <param name="scale">The divisor.</param>
        /// <returns>The scaled vector.</returns>
        public static PixelVector operator /(PixelVector vector, int scale)
        {
            return new PixelVector(vector._x / scale, vector._y / scale);
        }

        /// <summary>
        /// Length of the vector
        /// </summary>
        public double Length => Math.Sqrt(X * X + Y * Y);

        /// <summary>
        /// Negates a vector.
        /// </summary>
        /// <param name="a">The vector.</param>
        /// <returns>The negated vector.</returns>
        public static PixelVector operator -(PixelVector a)
        {
            return new PixelVector(-a._x, -a._y);
        }

        /// <summary>
        /// Adds two vectors.
        /// </summary>
        /// <param name="a">The first vector.</param>
        /// <param name="b">The second vector.</param>
        /// <returns>A vector that is the result of the addition.</returns>
        public static PixelVector operator +(PixelVector a, PixelVector b)
        {
            return new PixelVector(a._x + b._x, a._y + b._y);
        }

        /// <summary>
        /// Subtracts two vectors.
        /// </summary>
        /// <param name="a">The first vector.</param>
        /// <param name="b">The second vector.</param>
        /// <returns>A vector that is the result of the subtraction.</returns>
        public static PixelVector operator -(PixelVector a, PixelVector b)
        {
            return new PixelVector(a._x - b._x, a._y - b._y);
        }

        /// <summary>
        /// Check if two vectors are equal (bitwise).
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool Equals(PixelVector other)
        {
            return _x == other._x && _y == other._y;
        }

        /// <summary>
        /// Check if two vectors are nearly equal (numerically).
        /// </summary>
        /// <param name="other">The other vector.</param>
        /// <returns>True if vectors are nearly equal.</returns>
        [Pure]
        public bool NearlyEquals(PixelVector other)
        {
            const float tolerance = float.Epsilon;

            return Math.Abs(_x - other._x) < tolerance && Math.Abs(_y - other._y) < tolerance;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;

            return obj is PixelVector vector && Equals(vector);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (_x.GetHashCode() * 397) ^ _y.GetHashCode();
            }
        }

        public static bool operator ==(PixelVector left, PixelVector right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(PixelVector left, PixelVector right)
        {
            return !left.Equals(right);
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
        /// Returns a new vector with the specified X coordinate.
        /// </summary>
        /// <param name="x">The X coordinate.</param>
        /// <returns>The new vector.</returns>
        public PixelVector WithX(int x)
        {
            return new PixelVector(x, _y);
        }

        /// <summary>
        /// Returns a new vector with the specified Y coordinate.
        /// </summary>
        /// <param name="y">The Y coordinate.</param>
        /// <returns>The new vector.</returns>
        public PixelVector WithY(int y)
        {
            return new PixelVector(_x, y);
        }
    }
}
