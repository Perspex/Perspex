using Xunit;

namespace Avalonia.Base.UnitTests.Media
{
    public class RectTests
    {
        [Fact]
        public void Parse_Parses()
        {
            var rect = Rect.Parse("1,2 3,-4");
            var expected = new Rect(1, 2, 3, -4);
            Assert.Equal(expected, rect);
        }
    }
}
