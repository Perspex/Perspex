using Xunit;

namespace Avalonia.Base.UnitTests
{
    public class RectTests
    {
        [Fact]
        public void Union_Should_Return_Correct_Value_For_Intersecting_Rects()
        {
            var result = new Rect(0, 0, 100, 100).Union(new Rect(50, 50, 100, 100));

            Assert.Equal(new Rect(0, 0, 150, 150), result);
        }

        [Fact]
        public void Union_Should_Return_Correct_Value_For_NonIntersecting_Rects()
        {
            var result = new Rect(0, 0, 100, 100).Union(new Rect(150, 150, 100, 100));

            Assert.Equal(new Rect(0, 0, 250, 250), result);
        }

        [Fact]
        public void Union_Should_Ignore_Empty_This_rect()
        {
            var result = new Rect(0, 0, 0, 0).Union(new Rect(150, 150, 100, 100));

            Assert.Equal(new Rect(150, 150, 100, 100), result);
        }

        [Fact]
        public void Union_Should_Ignore_Empty_Other_rect()
        {
            var result = new Rect(0, 0, 100, 100).Union(new Rect(150, 150, 0, 0));

            Assert.Equal(new Rect(0, 0, 100, 100), result);
        }

        [Fact]
        public void Normalize_Should_Reverse_Negative_Size()
        {
            var result = new Rect(new Point(100, 100), new Point(0, 0)).Normalize();

            Assert.Equal(new Rect(0, 0, 100, 100), result);
        }

        [Fact]
        public void Normalize_Should_Make_Invalid_Rects_Empty()
        {
            var result = new Rect(
                double.NegativeInfinity, double.PositiveInfinity, 
                double.PositiveInfinity, double.PositiveInfinity)
                .Normalize();

            Assert.Equal(Rect.Empty, result);
        }
    }
}
