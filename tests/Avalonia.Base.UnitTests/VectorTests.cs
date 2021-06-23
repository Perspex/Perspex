using System;
using Xunit;

namespace Avalonia.Base.UnitTests
{
    public class VectorTests
    {
        [Fact]
        public void Length_Should_Return_Correct_Length_Of_Vector()
        {
            var vector = new Vector(2, 4);
            var length = Math.Sqrt(2 * 2 + 4 * 4);

            Assert.Equal(length, vector.Length);
        }

        [Fact]
        public void Length_Squared_Should_Return_Correct_Length_Of_Vector()
        {
            var vectorA = new Vector(2, 4);
            var squaredLengthA = 2 * 2 + 4 * 4;

            Assert.Equal(squaredLengthA, vectorA.SquaredLength);
        }

        [Fact]
        public void Normalize_Should_Return_Normalized_Vector()
        {
            // the length of a normalized vector must be 1

            var vectorA = new Vector(13, 84);
            var vectorB = new Vector(-34, 345);
            var vectorC = new Vector(-34, -84);

            Assert.Equal(1.0, vectorA.Normalize().Length);
            Assert.Equal(1.0, vectorB.Normalize().Length);
            Assert.Equal(1.0, vectorC.Normalize().Length);
        }

        [Fact]
        public void Negate_Should_Return_Negated_Vector()
        {
            var vector = new Vector(2, 4);
            var negated = new Vector(-2, -4);

            Assert.Equal(negated, vector.Negate());
        }

        [Fact]
        public void Dot_Should_Return_Correct_Value()
        {
            var a = new Vector(-6, 8.0);
            var b = new Vector(5, 12.0);

            Assert.Equal(66.0, Vector.Dot(a, b));
        }

        [Fact]
        public void Cross_Should_Return_Correct_Value()
        {
            var a = new Vector(-6, 8.0);
            var b = new Vector(5, 12.0);

            Assert.Equal(-112.0, Vector.Cross(a, b));
        }

        [Fact]
        public void Divied_By_Vector_Should_Return_Correct_Value()
        {
            var a = new Vector(10, 2);
            var b = new Vector(5, 2);

            var expected = new Vector(2, 1);

            Assert.Equal(expected, Vector.Divide(a, b));
        }

        [Fact]
        public void Divied_Should_Return_Correct_Value()
        {
            var vector = new Vector(10, 2);
            var expected = new Vector(5, 1);

            Assert.Equal(expected, Vector.Divide(vector, 2));
        }

        [Fact]
        public void Multiply_By_Vector_Should_Return_Correct_Value()
        {
            var a = new Vector(10, 2);
            var b = new Vector(2, 2);

            var expected = new Vector(20, 4);

            Assert.Equal(expected, Vector.Multiply(a, b));
        }

        [Fact]
        public void Multiply_Should_Return_Correct_Value()
        {
            var vector = new Vector(10, 2);

            var expected = new Vector(20, 4);

            Assert.Equal(expected, Vector.Multiply(vector, 2));
        }

        [Fact]
        public void Scale_Vector_Should_Be_Commutative()
        {
            var vector = new Vector(10, 2);

            var expected = vector * 2;

            Assert.Equal(expected, 2 * vector);
        }
    }
}
