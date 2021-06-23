using System;
using Avalonia.Media.Fonts;
using Xunit;

namespace Avalonia.Base.UnitTests.Media.Fonts
{
    public class FamilyNameCollectionTests
    {
        [Fact]
        public void Exception_Should_Be_Thrown_If_Names_Is_Null()
        {
            Assert.Throws<ArgumentNullException>(() => new FamilyNameCollection(null));
        }

        [Fact]
        public void Should_Be_Equal()
        {
            var familyNames = new FamilyNameCollection("Arial, Times New Roman");

            Assert.Equal(new FamilyNameCollection("Arial, Times New Roman"), familyNames);
        }
    }
}
