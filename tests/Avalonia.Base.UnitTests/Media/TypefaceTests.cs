using System;
using Avalonia.Media;
using Xunit;

namespace Avalonia.Base.UnitTests.Media
{
    public class TypefaceTests
    {
        [Fact]
        public void Exception_Should_Be_Thrown_If_FontWeight_LessThanEqualTo_Zero()
        {
            Assert.Throws<ArgumentException>(() => new Typeface("foo", (FontStyle)12, 0));
        }

        [Fact]
        public void Should_Be_Equal()
        {
            Assert.Equal(new Typeface("Font A"), new Typeface("Font A"));
        }

        [Fact]
        public void Should_Have_Equal_Hash()
        {
            Assert.Equal(new Typeface("Font A").GetHashCode(), new Typeface("Font A").GetHashCode());
        }
    }
}
