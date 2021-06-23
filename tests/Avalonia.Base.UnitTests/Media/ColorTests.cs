using System;
using Avalonia.Media;
using Xunit;

namespace Avalonia.Base.UnitTests.Media
{
    public class ColorTests
    {
        [Fact]
        public void Parse_Parses_RGB_Hash_Color()
        {
            var result = Color.Parse("#ff8844");

            Assert.Equal(0xff, result.R);
            Assert.Equal(0x88, result.G);
            Assert.Equal(0x44, result.B);
            Assert.Equal(0xff, result.A);
        }

        [Fact]
        public void Try_Parse_Parses_RGB_Hash_Color()
        {
            var success = Color.TryParse("#ff8844", out Color result);

            Assert.True(success);
            Assert.Equal(0xff, result.R);
            Assert.Equal(0x88, result.G);
            Assert.Equal(0x44, result.B);
            Assert.Equal(0xff, result.A);
        }

        [Fact]
        public void Parse_Parses_RGB_Hash_Shorthand_Color()
        {
            var result = Color.Parse("#f84");

            Assert.Equal(0xff, result.R);
            Assert.Equal(0x88, result.G);
            Assert.Equal(0x44, result.B);
            Assert.Equal(0xff, result.A);
        }

        [Fact]
        public void Try_Parse_Parses_RGB_Hash_Shorthand_Color()
        {
            var success = Color.TryParse("#f84", out Color result);

            Assert.True(success);
            Assert.Equal(0xff, result.R);
            Assert.Equal(0x88, result.G);
            Assert.Equal(0x44, result.B);
            Assert.Equal(0xff, result.A);
        }

        [Fact]
        public void Parse_Parses_ARGB_Hash_Color()
        {
            var result = Color.Parse("#40ff8844");

            Assert.Equal(0xff, result.R);
            Assert.Equal(0x88, result.G);
            Assert.Equal(0x44, result.B);
            Assert.Equal(0x40, result.A);
        }

        [Fact]
        public void Try_Parse_Parses_ARGB_Hash_Color()
        {
            var success = Color.TryParse("#40ff8844", out Color result);

            Assert.True(success);
            Assert.Equal(0xff, result.R);
            Assert.Equal(0x88, result.G);
            Assert.Equal(0x44, result.B);
            Assert.Equal(0x40, result.A);
        }

        [Fact]
        public void Parse_Parses_ARGB_Hash_Shorthand_Color()
        {
            var result = Color.Parse("#4f84");

            Assert.Equal(0xff, result.R);
            Assert.Equal(0x88, result.G);
            Assert.Equal(0x44, result.B);
            Assert.Equal(0x44, result.A);
        }

        [Fact]
        public void Try_Parse_Parses_ARGB_Hash_Shorthand_Color()
        {
            var success = Color.TryParse("#4f84", out Color result);

            Assert.True(success);
            Assert.Equal(0xff, result.R);
            Assert.Equal(0x88, result.G);
            Assert.Equal(0x44, result.B);
            Assert.Equal(0x44, result.A);
        }

        [Fact]
        public void Parse_Parses_Named_Color_Lowercase()
        {
            var result = Color.Parse("red");

            Assert.Equal(0xff, result.R);
            Assert.Equal(0x00, result.G);
            Assert.Equal(0x00, result.B);
            Assert.Equal(0xff, result.A);
        }

        [Fact]
        public void TryParse_Parses_Named_Color_Lowercase()
        {
            var success = Color.TryParse("red", out Color result);

            Assert.True(success);
            Assert.Equal(0xff, result.R);
            Assert.Equal(0x00, result.G);
            Assert.Equal(0x00, result.B);
            Assert.Equal(0xff, result.A);
        }

        [Fact]
        public void Parse_Parses_Named_Color_Uppercase()
        {
            var result = Color.Parse("RED");

            Assert.Equal(0xff, result.R);
            Assert.Equal(0x00, result.G);
            Assert.Equal(0x00, result.B);
            Assert.Equal(0xff, result.A);
        }

        [Fact]
        public void TryParse_Parses_Named_Color_Uppercase()
        {
            var success = Color.TryParse("RED", out Color result);

            Assert.True(success);
            Assert.Equal(0xff, result.R);
            Assert.Equal(0x00, result.G);
            Assert.Equal(0x00, result.B);
            Assert.Equal(0xff, result.A);
        }

        [Fact]
        public void Parse_Hex_Value_Doesnt_Accept_Too_Few_Chars()
        {
            Assert.Throws<FormatException>(() => Color.Parse("#ff"));
        }

        [Fact]
        public void TryParse_Hex_Value_Doesnt_Accept_Too_Few_Chars()
        {
            Assert.False(Color.TryParse("#ff", out _));
        }

        [Fact]
        public void Parse_Hex_Value_Doesnt_Accept_Too_Many_Chars()
        {
            Assert.Throws<FormatException>(() => Color.Parse("#ff5555555"));
        }

        [Fact]
        public void TryParse_Hex_Value_Doesnt_Accept_Too_Many_Chars()
        {
            Assert.False(Color.TryParse("#ff5555555", out _));
        }

        [Fact]
        public void Parse_Hex_Value_Doesnt_Accept_Invalid_Number()
        {
            Assert.Throws<FormatException>(() => Color.Parse("#ff808g80"));
        }

        [Fact]
        public void TryParse_Hex_Value_Doesnt_Accept_Invalid_Number()
        {
            Assert.False(Color.TryParse("#ff808g80", out _));
        }

        [Fact]
        public void Parse_Throws_ArgumentNullException_For_Null_Input()
        {
            Assert.Throws<ArgumentNullException>(() => Color.Parse((string)null));
        }

        [Fact]
        public void Parse_Throws_FormatException_For_Invalid_Input()
        {
            Assert.Throws<FormatException>(() => Color.Parse(string.Empty));
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        public void TryParse_Returns_False_For_Invalid_Input(string input)
        {
            Assert.False(Color.TryParse(input, out _));
        }
    }
}
