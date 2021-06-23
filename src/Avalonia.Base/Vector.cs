using System;
using System.Globalization;
#if !BUILDTASK
using Avalonia.Animation.Animators;
#endif
using Avalonia.Utilities;

#nullable enable

namespace Avalonia
{
    /// <summary>
    /// Defines a vector.
    /// </summary>
#if !BUILDTASK
    public
#endif
    readonly struct Vector : IEquatable<Vector>
    {
        static Vector()
        {
#if !BUILDTASK
            Animation.Animation.RegisterAnimator<VectorAnimator>(prop => typeof(Vector).IsAssignableFrom(prop.PropertyType));
#endif
        }

        /// <summary>
        /// The X component.
        /// </summary>
        private readonly double _x;

        /// <summary>
        /// The Y component.
        /// </summary>
        private readonly double _y;

        /// <summary>
        /// Initializes a new instance of the <see cref="Vector"/> structure.
        /// </summary>
        /// <param name="x">The X component.</param>
        /// <param name="y">The Y component.</param>
        public Vector(double x, double y)
        {
            _x = x;
            _y = y;
        }

        /// <summary>
        /// Gets the X component.
        /// </summary>
        public double X => _x;

        /// <summary>
        /// Gets the Y component.
        /// </summary>
        public double Y => _y;

        /// <summary>
        /// Converts the <see cref="Vector"/> to a <see cref="Point"/>.
        /// </summary>
        /// <param name="a">The vector.</param>
        public static explicit operator Point(Vector a)
        {
            return new Point(a._x, a._y);
        }

        /// <summary>
        /// Calculates the dot product of two vectors.
        /// </summary>
        /// <param name="a">First vector.</param>
        /// <param name="b">Second vector.</param>
        /// <returns>The dot product.</returns>
        public static double operator *(Vector a, Vector b)
            => Dot(a, b);

        /// <summary>
        /// Scales a vector.
        /// </summary>
        /// <param name="vector">The vector.</param>
        /// <param name="scale">The scaling factor.</param>
        /// <returns>The scaled vector.</returns>
        public static Vector operator *(Vector vector, double scale)
            => Multiply(vector, scale);

        /// <summary>
        /// Scales a vector.
        /// </summary>
        /// <param name="vector">The vector.</param>
        /// <param name="scale">The scaling factor.</param>
        /// <returns>The scaled vector.</returns>
        public static Vector operator *(double scale, Vector vector)
            => Multiply(vector, scale);

        /// <summary>
        /// Scales a vector.
        /// </summary>
        /// <param name="vector">The vector.</param>
        /// <param name="scale">The divisor.</param>
        /// <returns>The scaled vector.</returns>
        public static Vector operator /(Vector vector, double scale)
            => Divide(vector, scale);

        /// <summary>
        /// Parses a <see cref="Vector"/> string.
        /// </summary>
        /// <param name="s">The string.</param>
        /// <returns>The <see cref="Vector"/>.</returns>
        public static Vector Parse(string s)
        {
            using (var tokenizer = new StringTokenizer(s, CultureInfo.InvariantCulture, exceptionMessage: "Invalid Vector."))
            {
                return new Vector(
                    tokenizer.ReadDouble(),
                    tokenizer.ReadDouble()
                );
            }
        }

        /// <summary>
        /// Length of the vector.
        /// </summary>
        public double Length => Math.Sqrt(SquaredLength);

        /// <summary>
        /// Squared Length of the vector.
        /// </summary>
        public double SquaredLength => _x * _x + _y * _y;

        /// <summary>
        /// Negates a vector.
        /// </summary>
        /// <param name="a">The vector.</param>
        /// <returns>The negated vector.</returns>
        public static Vector operator -(Vector a)
            => Negate(a);

        /// <summary>
        /// Adds two vectors.
        /// </summary>
        /// <param name="a">The first vector.</param>
        /// <param name="b">The second vector.</param>
        /// <returns>A vector that is the result of the addition.</returns>
        public static Vector operator +(Vector a, Vector b)
            => Add(a, b);

        /// <summary>
        /// Subtracts two vectors.
        /// </summary>
        /// <param name="a">The first vector.</param>
        /// <param name="b">The second vector.</param>
        /// <returns>A vector that is the result of the subtraction.</returns>
        public static Vector operator -(Vector a, Vector b)
            => Subtract(a, b);

        /// <summary>
        /// Check if two vectors are equal (bitwise).
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool Equals(Vector other)
        {
            // ReSharper disable CompareOfFloatsByEqualityOperator
            return _x == other._x && _y == other._y;
            // ReSharper restore CompareOfFloatsByEqualityOperator
        }

        /// <summary>
        /// Check if two vectors are nearly equal (numerically).
        /// </summary>
        /// <param name="other">The other vector.</param>
        /// <returns>True if vectors are nearly equal.</returns>
        public bool NearlyEquals(Vector other)
        {
            return MathUtilities.AreClose(_x, other._x) && 
                   MathUtilities.AreClose(_y, other._y);
        }

        public override bool Equals(object obj) => obj is Vector other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                return (_x.GetHashCode() * 397) ^ _y.GetHashCode();
            }
        }

        public static bool operator ==(Vector left, Vector right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Vector left, Vector right)
        {
            return !left.Equals(right);
        }

        /// <summary>
        /// Returns the string representation of the vector.
        /// </summary>
        /// <returns>The string representation of the vector.</returns>
        public override string ToString()
        {
            return string.Format(CultureInfo.InvariantCulture, "{0}, {1}", _x, _y);
        }

        /// <summary>
        /// Returns a new vector with the specified X component.
        /// </summary>
        /// <param name="x">The X component.</param>
        /// <returns>The new vector.</returns>
        public Vector WithX(double x)
        {
            return new Vector(x, _y);
        }

        /// <summary>
        /// Returns a new vector with the specified Y component.
        /// </summary>
        /// <param name="y">The Y component.</param>
        /// <returns>The new vector.</returns>
        public Vector WithY(double y)
        {
            return new Vector(_x, y);
        }

        /// <summary>
        /// Returns a normalized version of this vector.
        /// </summary>
        /// <returns>The normalized vector.</returns>
        public Vector Normalize()
            => Normalize(this);

        /// <summary>
        /// Returns a negated version of this vector.
        /// </summary>
        /// <returns>The negated vector.</returns>
        public Vector Negate()
            => Negate(this);

        /// <summary>
        /// Returns the dot product of two vectors.
        /// </summary>
        /// <param name="a">The first vector.</param>
        /// <param name="b">The second vector.</param>
        /// <returns>The dot product.</returns>
        public static double Dot(Vector a, Vector b)
            => a._x * b._x + a._y * b._y;

        /// <summary>
        /// Returns the cross product of two vectors.
        /// </summary>
        /// <param name="a">The first vector.</param>
        /// <param name="b">The second vector.</param>
        /// <returns>The cross product.</returns>
        public static double Cross(Vector a, Vector b)
            => a._x * b._y - a._y * b._x;

        /// <summary>
        /// Normalizes the given vector.
        /// </summary>
        /// <param name="vector">The vector</param>
        /// <returns>The normalized vector.</returns>
        public static Vector Normalize(Vector vector)
            => Divide(vector, vector.Length);
        
        /// <summary>
        /// Divides the first vector by the second.
        /// </summary>
        /// <param name="a">The first vector.</param>
        /// <param name="b">The second vector.</param>
        /// <returns>The scaled vector.</returns>
        public static Vector Divide(Vector a, Vector b)
            => new Vector(a._x / b._x, a._y / b._y);

        /// <summary>
        /// Divides the vector by the given scalar.
        /// </summary>
        /// <param name="vector">The vector</param>
        /// <param name="scalar">The scalar value</param>
        /// <returns>The scaled vector.</returns>
        public static Vector Divide(Vector vector, double scalar)
            => new Vector(vector._x / scalar, vector._y / scalar);

        /// <summary>
        /// Multiplies the first vector by the second.
        /// </summary>
        /// <param name="a">The first vector.</param>
        /// <param name="b">The second vector.</param>
        /// <returns>The scaled vector.</returns>
        public static Vector Multiply(Vector a, Vector b)
            => new Vector(a._x * b._x, a._y * b._y);

        /// <summary>
        /// Multiplies the vector by the given scalar.
        /// </summary>
        /// <param name="vector">The vector</param>
        /// <param name="scalar">The scalar value</param>
        /// <returns>The scaled vector.</returns>
        public static Vector Multiply(Vector vector, double scalar)
            => new Vector(vector._x * scalar, vector._y * scalar);

        /// <summary>
        /// Adds the second to the first vector
        /// </summary>
        /// <param name="a">The first vector.</param>
        /// <param name="b">The second vector.</param>
        /// <returns>The summed vector.</returns>
        public static Vector Add(Vector a, Vector b)
            => new Vector(a._x + b._x, a._y + b._y);

        /// <summary>
        /// Subtracts the second from the first vector
        /// </summary>
        /// <param name="a">The first vector.</param>
        /// <param name="b">The second vector.</param>
        /// <returns>The difference vector.</returns>
        public static Vector Subtract(Vector a, Vector b)
            => new Vector(a._x - b._x, a._y - b._y);

        /// <summary>
        /// Negates the vector
        /// </summary>
        /// <param name="vector">The vector to negate.</param>
        /// <returns>The scaled vector.</returns>
        public static Vector Negate(Vector vector)
            => new Vector(-vector._x, -vector._y);

        /// <summary>
        /// Returns the vector (0.0, 0.0).
        /// </summary>
        public static Vector Zero
            => new Vector(0, 0);

        /// <summary>
        /// Returns the vector (1.0, 1.0).
        /// </summary>
        public static Vector One
            => new Vector(1, 1);

        /// <summary>
        /// Returns the vector (1.0, 0.0).
        /// </summary>
        public static Vector UnitX
            => new Vector(1, 0);

        /// <summary>
        /// Returns the vector (0.0, 1.0).
        /// </summary>
        public static Vector UnitY
            => new Vector(0, 1);

        /// <summary>
        /// Deconstructs the vector into its X and Y components.
        /// </summary>
        /// <param name="x">The X component.</param>
        /// <param name="y">The Y component.</param>
        public void Deconstruct(out double x, out double y)
        {
            x = this._x;
            y = this._y;
        }

        /// <summary>
        /// Gets a value indicating whether the X and Y components are zero.
        /// </summary>
        public bool IsDefault
        {
            get { return (_x == 0) && (_y == 0); }
        }
    }
}
