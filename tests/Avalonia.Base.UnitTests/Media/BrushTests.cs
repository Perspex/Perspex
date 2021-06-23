using System;
using Avalonia.Media;
using Xunit;

namespace Avalonia.Base.UnitTests.Media
{
    public class BrushTests
    {
        [Fact]
        public void Parse_Parses_RGB_Hash_Brush()
        {
            var result = (ISolidColorBrush)Brush.Parse("#ff8844");

            Assert.Equal(0xff, result.Color.R);
            Assert.Equal(0x88, result.Color.G);
            Assert.Equal(0x44, result.Color.B);
            Assert.Equal(0xff, result.Color.A);
        }

        [Fact]
        public void Parse_Parses_ARGB_Hash_Brush()
        {
            var result = (ISolidColorBrush)Brush.Parse("#40ff8844");

            Assert.Equal(0xff, result.Color.R);
            Assert.Equal(0x88, result.Color.G);
            Assert.Equal(0x44, result.Color.B);
            Assert.Equal(0x40, result.Color.A);
        }

        [Fact]
        public void Parse_Parses_Named_Brush_Lowercase()
        {
            var result = (ISolidColorBrush)Brush.Parse("red");

            Assert.Equal(0xff, result.Color.R);
            Assert.Equal(0x00, result.Color.G);
            Assert.Equal(0x00, result.Color.B);
            Assert.Equal(0xff, result.Color.A);
        }

        [Fact]
        public void Parse_Parses_Named_Brush_Uppercase()
        {
            var result = (ISolidColorBrush)Brush.Parse("RED");

            Assert.Equal(0xff, result.Color.R);
            Assert.Equal(0x00, result.Color.G);
            Assert.Equal(0x00, result.Color.B);
            Assert.Equal(0xff, result.Color.A);
        }

        [Fact]
        public void Parse_ToString_Named_Brush_Roundtrip()
        {
            const string expectedName = "Red";
            var brush = (ISolidColorBrush)Brush.Parse(expectedName);
            var name = brush.ToString();

            Assert.Equal(expectedName, name);
        }

        [Fact]
        public void Parse_Hex_Value_Doesnt_Accept_Too_Few_Chars()
        {
            Assert.Throws<FormatException>(() => Brush.Parse("#ff"));
        }

        [Fact]
        public void Parse_Hex_Value_Doesnt_Accept_Too_Many_Chars()
        {
            Assert.Throws<FormatException>(() => Brush.Parse("#ff5555555"));
        }

        [Fact]
        public void Parse_Hex_Value_Doesnt_Accept_Invalid_Number()
        {
            Assert.Throws<FormatException>(() => Brush.Parse("#ff808g80"));
        }

        [Fact]
        public void Changing_Opacity_Raises_Invalidated()
        {
            var target = new SolidColorBrush();
            var raised = false;

            target.Invalidated += (s, e) => raised = true;
            target.Opacity = 0.5;

            Assert.True(raised);
        }
    }
}
