using System;
using Avalonia.Utilities;
using Xunit;

namespace Avalonia.Base.UnitTests.Utilities
{
    public class UriExtensionsTests
    {
        [Fact]
        public void Assembly_Name_From_Query_Parsed()
        {
            const string key = "assembly";
            const string value = "Avalonia.Themes.Default";

            var uri = new Uri($"resm:Avalonia.Themes.Default.Accents.BaseLight.xaml?{key}={value}");
            var name = uri.GetAssemblyNameFromQuery();

            Assert.Equal(value, name);
        }

        [Fact]
        public void Assembly_Name_From_Empty_Query_Not_Parsed()
        {
            var uri = new Uri("resm:Avalonia.Themes.Default.Accents.BaseLight.xaml");
            var name = uri.GetAssemblyNameFromQuery();

            Assert.Equal(string.Empty, name);
        }
    }
}
