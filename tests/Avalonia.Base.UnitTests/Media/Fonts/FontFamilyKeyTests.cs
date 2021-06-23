using System;
using Avalonia.Media.Fonts;
using Xunit;

namespace Avalonia.Base.UnitTests.Media.Fonts
{
    public class FontFamilyKeyTests
    {
        [Fact]
        public void Exception_Should_Be_Thrown_If_Source_Is_Null()
        {
            Assert.Throws<ArgumentNullException>(() => new FontFamilyKey(null));
        }

        [Fact]
        public void Should_Initialize_With_Location()
        {
            var source = new Uri("resm:Avalonia.Visuals.UnitTests#MyFont");

            var fontFamilyKey = new FontFamilyKey(source);

            Assert.Equal(new Uri("resm:Avalonia.Visuals.UnitTests"), fontFamilyKey.Source);
        }

        [Fact]
        public void Should_Initialize_With_Location_And_Filename()
        {
            var source = new Uri("resm:Avalonia.Visuals.UnitTests.MyFont.ttf#MyFont");

            var fontFamilyKey = new FontFamilyKey(source);

            Assert.Equal(new Uri("resm:Avalonia.Visuals.UnitTests.MyFont.ttf"), fontFamilyKey.Source);
        }
    }
}
