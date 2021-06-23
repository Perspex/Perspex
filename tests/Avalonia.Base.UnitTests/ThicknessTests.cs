using Xunit;

namespace Avalonia.Base.UnitTests
{
    public class ThicknessTests
    {
        [Fact]
        public void Parse_Parses_Single_Uniform_Size()
        {
            var result = Thickness.Parse("1.2");

            Assert.Equal(new Thickness(1.2), result);
        }

        [Fact]
        public void Parse_Parses_Horizontal_Vertical()
        {
            var result = Thickness.Parse("1.2,3.4");

            Assert.Equal(new Thickness(1.2, 3.4), result);
        }

        [Fact]
        public void Parse_Parses_Left_Top_Right_Bottom()
        {
            var result = Thickness.Parse("1.2, 3.4, 5, 6");

            Assert.Equal(new Thickness(1.2, 3.4, 5, 6), result);
        }

        [Fact]
        public void Parse_Accepts_Spaces()
        {
            var result = Thickness.Parse("1.2 3.4 5 6");

            Assert.Equal(new Thickness(1.2, 3.4, 5, 6), result);
        }
    }
}
