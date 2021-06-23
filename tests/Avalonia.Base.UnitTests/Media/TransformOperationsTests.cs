using Avalonia.Media.Transformation;
using Avalonia.Utilities;
using Xunit;

namespace Avalonia.Base.UnitTests.Media
{
    public class TransformOperationsTests
    {
        [Theory]
        [InlineData("translate(10px)", 10d, 0d)]
        [InlineData("translate(10px, 10px)", 10d, 10d)]
        [InlineData("translate(0px, 10px)", 0d, 10d)]
        [InlineData("translate(10px, 0px)", 10d, 0d)]
        [InlineData("translateX(10px)", 10d, 0d)]
        [InlineData("translateY(10px)", 0d, 10d)]
        public void Can_Parse_Translation(string data, double x, double y)
        {
            var transform = TransformOperations.Parse(data);

            var operations = transform.Operations;

            Assert.Single(operations);
            Assert.Equal(TransformOperation.OperationType.Translate, operations[0].Type);
            Assert.Equal(x, operations[0].Data.Translate.X);
            Assert.Equal(y, operations[0].Data.Translate.Y);
        }

        [Theory]
        [InlineData("rotate(90deg)", 90d)]
        [InlineData("rotate(0.5turn)", 180d)]
        [InlineData("rotate(200grad)", 180d)]
        [InlineData("rotate(3.14159265rad)", 180d)]
        public void Can_Parse_Rotation(string data, double angleDeg)
        {
            var transform = TransformOperations.Parse(data);

            var operations = transform.Operations;

            Assert.Single(operations);
            Assert.Equal(TransformOperation.OperationType.Rotate, operations[0].Type);
            Assert.Equal(MathUtilities.Deg2Rad(angleDeg), operations[0].Data.Rotate.Angle, 4);
        }

        [Theory]
        [InlineData("scale(10)", 10d, 10d)]
        [InlineData("scale(10, 10)", 10d, 10d)]
        [InlineData("scale(0, 10)", 0d, 10d)]
        [InlineData("scale(10, 0)", 10d, 0d)]
        [InlineData("scaleX(10)", 10d, 1d)]
        [InlineData("scaleY(10)", 1d, 10d)]
        public void Can_Parse_Scale(string data, double x, double y)
        {
            var transform = TransformOperations.Parse(data);

            var operations = transform.Operations;

            Assert.Single(operations);
            Assert.Equal(TransformOperation.OperationType.Scale, operations[0].Type);
            Assert.Equal(x, operations[0].Data.Scale.X);
            Assert.Equal(y, operations[0].Data.Scale.Y);
        }

        [Theory]
        [InlineData("skew(90deg)", 90d, 0d)]
        [InlineData("skew(0.5turn)", 180d, 0d)]
        [InlineData("skew(200grad)", 180d, 0d)]
        [InlineData("skew(3.14159265rad)", 180d, 0d)]
        [InlineData("skewX(90deg)", 90d, 0d)]
        [InlineData("skewX(0.5turn)", 180d, 0d)]
        [InlineData("skewX(200grad)", 180d, 0d)]
        [InlineData("skewX(3.14159265rad)", 180d, 0d)]
        [InlineData("skew(0, 90deg)", 0d, 90d)]
        [InlineData("skew(0, 0.5turn)", 0d, 180d)]
        [InlineData("skew(0, 200grad)", 0d, 180d)]
        [InlineData("skew(0, 3.14159265rad)", 0d, 180d)]
        [InlineData("skewY(90deg)", 0d, 90d)]
        [InlineData("skewY(0.5turn)", 0d, 180d)]
        [InlineData("skewY(200grad)", 0d, 180d)]
        [InlineData("skewY(3.14159265rad)", 0d, 180d)]
        [InlineData("skew(90deg, 90deg)", 90d, 90d)]
        [InlineData("skew(0.5turn, 0.5turn)", 180d, 180d)]
        [InlineData("skew(200grad, 200grad)", 180d, 180d)]
        [InlineData("skew(3.14159265rad, 3.14159265rad)", 180d, 180d)]
        public void Can_Parse_Skew(string data, double x, double y)
        {
            var transform = TransformOperations.Parse(data);

            var operations = transform.Operations;

            Assert.Single(operations);
            Assert.Equal(TransformOperation.OperationType.Skew, operations[0].Type);
            Assert.Equal(MathUtilities.Deg2Rad(x), operations[0].Data.Skew.X, 4);
            Assert.Equal(MathUtilities.Deg2Rad(y), operations[0].Data.Skew.Y, 4);
        }

        [Fact]
        public void Can_Parse_Compound_Operations()
        {
            var data = "scale(1,2) translate(3px,4px) rotate(5deg) skew(6deg,7deg)";

            var transform = TransformOperations.Parse(data);

            var operations = transform.Operations;

            Assert.Equal(TransformOperation.OperationType.Scale, operations[0].Type);
            Assert.Equal(1, operations[0].Data.Scale.X);
            Assert.Equal(2, operations[0].Data.Scale.Y);

            Assert.Equal(TransformOperation.OperationType.Translate, operations[1].Type);
            Assert.Equal(3, operations[1].Data.Translate.X);
            Assert.Equal(4, operations[1].Data.Translate.Y);

            Assert.Equal(TransformOperation.OperationType.Rotate, operations[2].Type);
            Assert.Equal(MathUtilities.Deg2Rad(5), operations[2].Data.Rotate.Angle);

            Assert.Equal(TransformOperation.OperationType.Skew, operations[3].Type);
            Assert.Equal(MathUtilities.Deg2Rad(6), operations[3].Data.Skew.X);
            Assert.Equal(MathUtilities.Deg2Rad(7), operations[3].Data.Skew.Y);
        }

        [Fact]
        public void Can_Parse_Matrix_Operation()
        {
            var data = "matrix(1,2,3,4,5,6)";

            var transform = TransformOperations.Parse(data);

            var operations = transform.Operations;

            Assert.Single(operations);
            Assert.Equal(TransformOperation.OperationType.Matrix, operations[0].Type);
            
            var expectedMatrix = new Matrix(1, 2, 3, 4, 5, 6);

            Assert.Equal(expectedMatrix, operations[0].Matrix);
        }

        [Theory]
        [InlineData(0d, 10d, 0d)]
        [InlineData(0.5d, 5d, 10d)]
        [InlineData(1d, 0d, 20d)]
        public void Can_Interpolate_Translation(double progress, double x, double y)
        {
            var from = TransformOperations.Parse("translateX(10px)");
            var to = TransformOperations.Parse("translateY(20px)");

            var interpolated = TransformOperations.Interpolate(from, to, progress);

            var operations = interpolated.Operations;

            Assert.Single(operations);
            Assert.Equal(TransformOperation.OperationType.Translate, operations[0].Type);
            Assert.Equal(x, operations[0].Data.Translate.X);
            Assert.Equal(y, operations[0].Data.Translate.Y);
        }

        [Theory]
        [InlineData(0d, 10d, 1d)]
        [InlineData(0.5d, 5.5d, 10.5d)]
        [InlineData(1d, 1d, 20d)]
        public void Can_Interpolate_Scale(double progress, double x, double y)
        {
            var from = TransformOperations.Parse("scaleX(10)");
            var to = TransformOperations.Parse("scaleY(20)");

            var interpolated = TransformOperations.Interpolate(from, to, progress);

            var operations = interpolated.Operations;

            Assert.Single(operations);
            Assert.Equal(TransformOperation.OperationType.Scale, operations[0].Type);
            Assert.Equal(x, operations[0].Data.Scale.X);
            Assert.Equal(y, operations[0].Data.Scale.Y);
        }

        [Theory]
        [InlineData(0d, 10d, 0d)]
        [InlineData(0.5d, 5d, 10d)]
        [InlineData(1d, 0d, 20d)]
        public void Can_Interpolate_Skew(double progress, double x, double y)
        {
            var from = TransformOperations.Parse("skewX(10deg)");
            var to = TransformOperations.Parse("skewY(20deg)");

            var interpolated = TransformOperations.Interpolate(from, to, progress);

            var operations = interpolated.Operations;

            Assert.Single(operations);
            Assert.Equal(TransformOperation.OperationType.Skew, operations[0].Type);
            Assert.Equal(MathUtilities.Deg2Rad(x), operations[0].Data.Skew.X);
            Assert.Equal(MathUtilities.Deg2Rad(y), operations[0].Data.Skew.Y);
        }

        [Theory]
        [InlineData(0d, 10d)]
        [InlineData(0.5d, 15d)]
        [InlineData(1d,20d)]
        public void Can_Interpolate_Rotation(double progress, double angle)
        {
            var from = TransformOperations.Parse("rotate(10deg)");
            var to = TransformOperations.Parse("rotate(20deg)");

            var interpolated = TransformOperations.Interpolate(from, to, progress);

            var operations = interpolated.Operations;

            Assert.Single(operations);
            Assert.Equal(TransformOperation.OperationType.Rotate, operations[0].Type);
            Assert.Equal(MathUtilities.Deg2Rad(angle), operations[0].Data.Rotate.Angle);
        }

        [Fact]
        public void Interpolation_Fallback_To_Matrix()
        {
            double progress = 0.5d;

            var from = TransformOperations.Parse("rotate(45deg)");
            var to = TransformOperations.Parse("translate(100px, 100px) rotate(1215deg)");

            var interpolated = TransformOperations.Interpolate(from, to, progress);

            var operations = interpolated.Operations;

            Assert.Single(operations);
            Assert.Equal(TransformOperation.OperationType.Matrix, operations[0].Type);
        }
    }
}
