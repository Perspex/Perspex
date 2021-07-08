using System;
using System.Linq;
using System.Reflection;
using Avalonia.Shared.PlatformSupport;
using Avalonia.Platform.Internal;
using Moq;
using Xunit;

namespace Avalonia.Base.UnitTests.Platform
{
    public class AssetLoaderTests
    {
        public class MockAssembly : Assembly {}

        private const string AssemblyNameWithWhitespace = "Awesome Library";

        private const string AssemblyNameWithNonAscii = "Какое-то-название";

        static AssetLoaderTests()
        {
            var resolver = Mock.Of<IAssemblyDescriptorResolver>();

            var descriptor = CreateAssemblyDescriptor(AssemblyNameWithWhitespace);
            Mock.Get(resolver).Setup(x => x.Get(AssemblyNameWithWhitespace)).Returns(descriptor);

            descriptor = CreateAssemblyDescriptor(AssemblyNameWithNonAscii);
            Mock.Get(resolver).Setup(x => x.Get(AssemblyNameWithNonAscii)).Returns(descriptor);

            AssetLoader.SetAssemblyDescriptorResolver(resolver);
        }

        [Fact]
        public void AssemblyName_With_Whitespace_Should_Load_Resm()
        {
            var uri = new Uri($"resm:Avalonia.Base.UnitTests.Assets.something?assembly={AssemblyNameWithWhitespace}");
            var loader = new AssetLoader();

            var assemblyActual = loader.GetAssembly(uri, null);

            Assert.Equal(AssemblyNameWithWhitespace, assemblyActual.FullName);
        }

        [Fact]
        public void AssemblyName_With_Non_ASCII_Should_Load_Avares()
        {
            var uri = new Uri($"avares://{AssemblyNameWithNonAscii}/Assets/something");
            var loader = new AssetLoader();

            var assemblyActual = loader.GetAssembly(uri, null);

            Assert.Equal(AssemblyNameWithNonAscii, assemblyActual.FullName);
        }

        private static IAssemblyDescriptor CreateAssemblyDescriptor(string assemblyName)
        {
            var assembly = Mock.Of<MockAssembly>();
            Mock.Get(assembly).Setup(x => x.GetName()).Returns(new AssemblyName(assemblyName));
            Mock.Get(assembly).Setup(x => x.FullName).Returns(assemblyName);

            var descriptor = Mock.Of<IAssemblyDescriptor>();
            Mock.Get(descriptor).Setup(x => x.Assembly).Returns(assembly);
            return descriptor;
        }
    }
}
