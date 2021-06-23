using Xunit;

namespace Avalonia.Base.UnitTests
{
    public class RelativePointTests
    {
        [Fact]
        public void Parse_Should_Accept_Absolute_Value()
        {
            var result = RelativePoint.Parse("4,5");

            Assert.Equal(new RelativePoint(4, 5, RelativeUnit.Absolute), result);
        }

        [Fact]
        public void Parse_Should_Accept_Relative_Value()
        {
            var result = RelativePoint.Parse("25%, 50%");

            Assert.Equal(new RelativePoint(0.25, 0.5, RelativeUnit.Relative), result);
        }
    }
}
