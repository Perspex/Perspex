using System;
using Avalonia.Media;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Base.UnitTests.Media
{
    public class FontManagerTests
    {
        [Fact]
        public void Should_Create_Single_Instance_Typeface()
        {
            using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface))
            {
                var fontFamily = new FontFamily("MyFont");

                var typeface = new Typeface(fontFamily);

                var glyphTypeface = FontManager.Current.GetOrAddGlyphTypeface(typeface);

                Assert.Same(glyphTypeface, FontManager.Current.GetOrAddGlyphTypeface(typeface));
            }
        }

        [Fact]
        public void Should_Throw_When_Default_FamilyName_Is_Null()
        {
            using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface.With(fontManagerImpl: new MockFontManagerImpl(null))))
            {
                Assert.Throws<InvalidOperationException>(() => FontManager.Current);
            }
        }
    }
}
