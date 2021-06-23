using Avalonia.Media;
using Xunit;

namespace Avalonia.Base.UnitTests.Media
{
    public class BoxShadowTests
    {
        [Fact]
        public void BoxShadow_Should_Parse()
        {
            foreach (var extraSpaces in new[] { false, true })
            foreach (var inset in new[] { false, true })
                for (var componentCount = 2; componentCount < 5; componentCount++)
                {
                    var s = (inset ? "inset " : "") + "10 20";
                    double blur = 0;
                    double spread = 0;
                    if (componentCount > 2)
                    {
                        s += " 30";
                        blur = 30;
                    }

                    if (componentCount > 3)
                    {
                        s += " 40";
                        spread = 40;
                    }

                    s += " red";

                    if (extraSpaces)
                        s = " " + s.Replace(" ", "  ") + "   ";

                    var parsed = BoxShadow.Parse(s);
                    Assert.Equal(inset, parsed.IsInset);
                    Assert.Equal(10, parsed.OffsetX);
                    Assert.Equal(20, parsed.OffsetY);
                    Assert.Equal(blur, parsed.Blur);
                    Assert.Equal(spread, parsed.Spread);
                    Assert.Equal(Colors.Red, parsed.Color);
                }
        }
    }
}
