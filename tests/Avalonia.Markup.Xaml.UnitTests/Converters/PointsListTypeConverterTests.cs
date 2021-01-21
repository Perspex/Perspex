using System.Collections.Generic;
using System.Reflection;
using Avalonia.Controls.Shapes;
using Avalonia.Data;
using Avalonia.Markup.Xaml.Converters;
using Xunit;

namespace Avalonia.Markup.Xaml.UnitTests.Converters
{
    public class PointsListTypeConverterTests
    {
        public PointsListTypeConverterTests()
        {
            // Ensure needed assemblies are loaded.
            Assembly.Load(typeof(RelativeSource).Assembly.GetName());
        }

        [Theory]
        [InlineData("1,2 3,4")]
        [InlineData("1 2 3 4")]
        [InlineData("1 2,3 4")]
        [InlineData("1,2,3,4")]
        public void TypeConverter_Should_Parse(string input)
        {
            var conv = new PointsListTypeConverter();

            var points = (IList<Point>)conv.ConvertFrom(input);

            Assert.Equal(2, points.Count);
            Assert.Equal(new Point(1, 2), points[0]);
            Assert.Equal(new Point(3, 4), points[1]);
        }

        [Theory]
        [InlineData("1,2 3,4")]
        [InlineData("1 2 3 4")]
        [InlineData("1 2,3 4")]
        [InlineData("1,2,3,4")]
        public void Should_Parse_Points_in_Xaml(string input)
        {
            var xaml = $"<Polygon xmlns='https://github.com/avaloniaui' Points='{input}' />";
            var polygon = (Polygon)AvaloniaRuntimeXamlLoader.Load(xaml);

            var points = polygon.Points;

            Assert.Equal(2, points.Count);
            Assert.Equal(new Point(1, 2), points[0]);
            Assert.Equal(new Point(3, 4), points[1]);
        }
    }
}
