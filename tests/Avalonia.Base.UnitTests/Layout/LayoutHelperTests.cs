using System;
using Avalonia.Layout;
using Xunit;

namespace Avalonia.Base.UnitTests.Layout
{
    public class LayoutHelperTests
    {
        [Fact]
        public void Round_Layout_Value_Without_DPI_Aware()
        {
            const double value = 42.5;
            var expectedValue = Math.Round(value);
            var actualValue = LayoutHelper.RoundLayoutValue(value, 1.0);
            Assert.Equal(expectedValue, actualValue);
        }

        [Fact]
        public void Round_Layout_Value_With_DPI_Aware()
        {
            const double dpiScale = 1.25;
            const double value = 42.5;
            var expectedValue = Math.Round(value * dpiScale) / dpiScale;
            var actualValue = LayoutHelper.RoundLayoutValue(value, dpiScale);
            Assert.Equal(expectedValue, actualValue);
        }
    }
}
