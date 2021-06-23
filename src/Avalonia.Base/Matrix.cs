using System;
using System.Globalization;
using Avalonia.Utilities;

namespace Avalonia
{
    /// <summary>
    /// A 2x3 matrix.
    /// </summary>
#if !BUILDTASK
    public
#endif
    readonly struct Matrix : IEquatable<Matrix>
    {
        private readonly double _m11;
        private readonly double _m12;
        private readonly double _m21;
        private readonly double _m22;
        private readonly double _m31;
        private readonly double _m32;

        /// <summary>
        /// Initializes a new instance of the <see cref="Matrix"/> struct.
        /// </summary>
        /// <param name="m11">The first element of the first row.</param>
        /// <param name="m12">The second element of the first row.</param>
        /// <param name="m21">The first element of the second row.</param>
        /// <param name="m22">The second element of the second row.</param>
        /// <param name="offsetX">The first element of the third row.</param>
        /// <param name="offsetY">The second element of the third row.</param>
        public Matrix(
            double m11,
            double m12,
            double m21,
            double m22,
            double offsetX,
            double offsetY)
        {
            _m11 = m11;
            _m12 = m12;
            _m21 = m21;
            _m22 = m22;
            _m31 = offsetX;
            _m32 = offsetY;
        }

        /// <summary>
        /// Returns the multiplicative identity matrix.
        /// </summary>
        public static Matrix Identity { get; } = new Matrix(1.0, 0.0, 0.0, 1.0, 0.0, 0.0);

        /// <summary>
        /// Returns whether the matrix is the identity matrix.
        /// </summary>
        public bool IsIdentity => Equals(Identity);

        /// <summary>
        /// HasInverse Property - returns true if this matrix is invertible, false otherwise.
        /// </summary>
        public bool HasInverse => !MathUtilities.IsZero(GetDeterminant());

        /// <summary>
        /// The first element of the first row
        /// </summary>
        public double M11 => _m11;

        /// <summary>
        /// The second element of the first row
        /// </summary>
        public double M12 => _m12;

        /// <summary>
        /// The first element of the second row
        /// </summary>
        public double M21 => _m21;

        /// <summary>
        /// The second element of the second row
        /// </summary>
        public double M22 => _m22;

        /// <summary>
        /// The first element of the third row
        /// </summary>
        public double M31 => _m31;

        /// <summary>
        /// The second element of the third row
        /// </summary>
        public double M32 => _m32;

        /// <summary>
        /// Multiplies two matrices together and returns the resulting matrix.
        /// </summary>
        /// <param name="value1">The first source matrix.</param>
        /// <param name="value2">The second source matrix.</param>
        /// <returns>The product matrix.</returns>
        public static Matrix operator *(Matrix value1, Matrix value2)
        {
            return new Matrix(
                (value1.M11 * value2.M11) + (value1.M12 * value2.M21),
                (value1.M11 * value2.M12) + (value1.M12 * value2.M22),
                (value1.M21 * value2.M11) + (value1.M22 * value2.M21),
                (value1.M21 * value2.M12) + (value1.M22 * value2.M22),
                (value1._m31 * value2.M11) + (value1._m32 * value2.M21) + value2._m31,
                (value1._m31 * value2.M12) + (value1._m32 * value2.M22) + value2._m32);
        }

        /// <summary>
        /// Negates the given matrix by multiplying all values by -1.
        /// </summary>
        /// <param name="value">The source matrix.</param>
        /// <returns>The negated matrix.</returns>
        public static Matrix operator -(Matrix value)
        {
            return value.Invert();
        }

        /// <summary>
        /// Returns a boolean indicating whether the given matrices are equal.
        /// </summary>
        /// <param name="value1">The first source matrix.</param>
        /// <param name="value2">The second source matrix.</param>
        /// <returns>True if the matrices are equal; False otherwise.</returns>
        public static bool operator ==(Matrix value1, Matrix value2)
        {
            return value1.Equals(value2);
        }

        /// <summary>
        /// Returns a boolean indicating whether the given matrices are not equal.
        /// </summary>
        /// <param name="value1">The first source matrix.</param>
        /// <param name="value2">The second source matrix.</param>
        /// <returns>True if the matrices are not equal; False if they are equal.</returns>
        public static bool operator !=(Matrix value1, Matrix value2)
        {
            return !value1.Equals(value2);
        }

        /// <summary>
        /// Creates a rotation matrix using the given rotation in radians.
        /// </summary>
        /// <param name="radians">The amount of rotation, in radians.</param>
        /// <returns>A rotation matrix.</returns>
        public static Matrix CreateRotation(double radians)
        {
            double cos = Math.Cos(radians);
            double sin = Math.Sin(radians);
            return new Matrix(cos, sin, -sin, cos, 0, 0);
        }

        /// <summary>
        /// Creates a skew matrix from the given axis skew angles in radians.
        /// </summary>
        /// <param name="xAngle">The amount of skew along the X-axis, in radians.</param>
        /// <param name="yAngle">The amount of skew along the Y-axis, in radians.</param>
        /// <returns>A rotation matrix.</returns>
        public static Matrix CreateSkew(double xAngle, double yAngle)
        {
            double tanX = Math.Tan(xAngle);
            double tanY = Math.Tan(yAngle);
            return new Matrix(1.0, tanY, tanX, 1.0, 0.0, 0.0);
        }

        /// <summary>
        /// Creates a scale matrix from the given X and Y components.
        /// </summary>
        /// <param name="xScale">Value to scale by on the X-axis.</param>
        /// <param name="yScale">Value to scale by on the Y-axis.</param>
        /// <returns>A scaling matrix.</returns>
        public static Matrix CreateScale(double xScale, double yScale)
        {
            return CreateScale(new Vector(xScale, yScale));
        }

        /// <summary>
        /// Creates a scale matrix from the given vector scale.
        /// </summary>
        /// <param name="scales">The scale to use.</param>
        /// <returns>A scaling matrix.</returns>
        public static Matrix CreateScale(Vector scales)
        {
            return new Matrix(scales.X, 0, 0, scales.Y, 0, 0);
        }

        /// <summary>
        /// Creates a translation matrix from the given vector.
        /// </summary>
        /// <param name="position">The translation position.</param>
        /// <returns>A translation matrix.</returns>
        public static Matrix CreateTranslation(Vector position)
        {
            return CreateTranslation(position.X, position.Y);
        }

        /// <summary>
        /// Creates a translation matrix from the given X and Y components.
        /// </summary>
        /// <param name="xPosition">The X position.</param>
        /// <param name="yPosition">The Y position.</param>
        /// <returns>A translation matrix.</returns>
        public static Matrix CreateTranslation(double xPosition, double yPosition)
        {
            return new Matrix(1.0, 0.0, 0.0, 1.0, xPosition, yPosition);
        }

        /// <summary>
        /// Converts an angle in degrees to radians.
        /// </summary>
        /// <param name="angle">The angle in degrees.</param>
        /// <returns>The angle in radians.</returns>
        public static double ToRadians(double angle)
        {
            return angle * 0.0174532925;
        }

        /// <summary>
        /// Calculates the determinant for this matrix.
        /// </summary>
        /// <returns>The determinant.</returns>
        /// <remarks>
        /// The determinant is calculated by expanding the matrix with a third column whose
        /// values are (0,0,1).
        /// </remarks>
        public double GetDeterminant()
        {
            return (_m11 * _m22) - (_m12 * _m21);
        }

        /// <summary>
        /// Returns a boolean indicating whether the matrix is equal to the other given matrix.
        /// </summary>
        /// <param name="other">The other matrix to test equality against.</param>
        /// <returns>True if this matrix is equal to other; False otherwise.</returns>
        public bool Equals(Matrix other)
        {
            // ReSharper disable CompareOfFloatsByEqualityOperator
            return _m11 == other.M11 &&
                   _m12 == other.M12 &&
                   _m21 == other.M21 &&
                   _m22 == other.M22 &&
                   _m31 == other.M31 &&
                   _m32 == other.M32;
            // ReSharper restore CompareOfFloatsByEqualityOperator
        }

        /// <summary>
        /// Returns a boolean indicating whether the given Object is equal to this matrix instance.
        /// </summary>
        /// <param name="obj">The Object to compare against.</param>
        /// <returns>True if the Object is equal to this matrix; False otherwise.</returns>
        public override bool Equals(object obj) => obj is Matrix other && Equals(other);

        /// <summary>
        /// Returns the hash code for this instance.
        /// </summary>
        /// <returns>The hash code.</returns>
        public override int GetHashCode()
        {
            return M11.GetHashCode() + M12.GetHashCode() +
                   M21.GetHashCode() + M22.GetHashCode() +
                   M31.GetHashCode() + M32.GetHashCode();
        }

        /// <summary>
        /// Returns a String representing this matrix instance.
        /// </summary>
        /// <returns>The string representation.</returns>
        public override string ToString()
        {
            CultureInfo ci = CultureInfo.CurrentCulture;
            return string.Format(
                ci,
                "{{ {{M11:{0} M12:{1}}} {{M21:{2} M22:{3}}} {{M31:{4} M32:{5}}} }}",
                M11.ToString(ci),
                M12.ToString(ci),
                M21.ToString(ci),
                M22.ToString(ci),
                M31.ToString(ci),
                M32.ToString(ci));
        }

        /// <summary>
        /// Attempts to invert the Matrix.
        /// </summary>
        /// <returns>The inverted matrix or <see langword="null"/> when matrix is not invertible.</returns>
        public bool TryInvert(out Matrix inverted)
        {
            double d = GetDeterminant();

            if (MathUtilities.IsZero(d))
            {
                inverted = default;
                
                return false;
            }

            inverted = new Matrix(
                _m22 / d,
                -_m12 / d,
                -_m21 / d,
                _m11 / d,
                ((_m21 * _m32) - (_m22 * _m31)) / d,
                ((_m12 * _m31) - (_m11 * _m32)) / d);

            return true;
        }

        /// <summary>
        /// Inverts the Matrix.
        /// </summary>
        /// <exception cref="InvalidOperationException">Matrix is not invertible.</exception>
        /// <returns>The inverted matrix.</returns>
        public Matrix Invert()
        {
            if (!TryInvert(out Matrix inverted))
            {
                throw new InvalidOperationException("Transform is not invertible.");
            }

            return inverted;
        }

        /// <summary>
        /// Parses a <see cref="Matrix"/> string.
        /// </summary>
        /// <param name="s">Six comma-delimited double values (m11, m12, m21, m22, offsetX, offsetY) that describe the new <see cref="Matrix"/></param>
        /// <returns>The <see cref="Matrix"/>.</returns>
        public static Matrix Parse(string s)
        {
            using (var tokenizer = new StringTokenizer(s, CultureInfo.InvariantCulture, exceptionMessage: "Invalid Matrix."))
            {
                return new Matrix(
                    tokenizer.ReadDouble(),
                    tokenizer.ReadDouble(),
                    tokenizer.ReadDouble(),
                    tokenizer.ReadDouble(),
                    tokenizer.ReadDouble(),
                    tokenizer.ReadDouble()
                );
            }
        }

        /// <summary>
        /// Decomposes given matrix into transform operations.
        /// </summary>
        /// <param name="matrix">Matrix to decompose.</param>
        /// <param name="decomposed">Decomposed matrix.</param>
        /// <returns>The status of the operation.</returns>
        public static bool TryDecomposeTransform(Matrix matrix, out Decomposed decomposed)
        {
            decomposed = default;

            var determinant = matrix.GetDeterminant();
            
            if (MathUtilities.IsZero(determinant))
            {
                return false;
            }

            var m11 = matrix.M11;
            var m21 = matrix.M21;
            var m12 = matrix.M12;
            var m22 = matrix.M22;

            // Translation.
            decomposed.Translate = new Vector(matrix.M31, matrix.M32);

            // Scale sign.
            var scaleX = 1d;
            var scaleY = 1d;

            if (determinant < 0)
            {
                if (m11 < m22)
                {
                    scaleX *= -1d;
                }
                else
                {
                    scaleY *= -1d;
                }
            }

            // X Scale.
            scaleX *= Math.Sqrt(m11 * m11 + m12 * m12);

            m11 /= scaleX;
            m12 /= scaleX;

            // XY Shear.
            double scaledShear = m11 * m21 + m12 * m22;

            m21 -= m11 * scaledShear;
            m22 -= m12 * scaledShear;

            // Y Scale.
            scaleY *= Math.Sqrt(m21 * m21 + m22 * m22);

            decomposed.Scale = new Vector(scaleX, scaleY);
            decomposed.Skew = new Vector(scaledShear / scaleY, 0d);
            decomposed.Angle = Math.Atan2(m12, m11);

            return true;
        }

        public struct Decomposed
        {
            public Vector Translate;
            public Vector Scale;
            public Vector Skew;
            public double Angle;
        }
    }
}
