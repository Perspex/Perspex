using Avalonia.Controls;
using Avalonia.Styling;
using Moq;
using Xunit;

namespace Avalonia.Base.UnitTests.Styling
{
    public class StylesTests
    {
        [Fact]
        public void Adding_Style_Should_Set_Owner()
        {
            var host = new Mock<IResourceHost>();
            var target = new Styles(host.Object);
            var style = new Mock<IStyle>();
            var rp = style.As<IResourceProvider>();

            host.Invocations.Clear();
            target.Add(style.Object);

            rp.Verify(x => x.AddOwner(host.Object));
        }

        [Fact]
        public void Removing_Style_Should_Clear_Owner()
        {
            var host = new Mock<IResourceHost>();
            var target = new Styles(host.Object);
            var style = new Mock<IStyle>();
            var rp = style.As<IResourceProvider>();

            host.Invocations.Clear();
            target.Add(style.Object);
            target.Remove(style.Object);

            rp.Verify(x => x.RemoveOwner(host.Object));
        }

        [Fact]
        public void Should_Set_Owner_On_Assigned_Resources()
        {
            var host = new Mock<IResourceHost>();
            var target = new Styles();
            ((IResourceProvider)target).AddOwner(host.Object);

            var resources = new Mock<IResourceDictionary>();
            target.Resources = resources.Object;

            resources.Verify(x => x.AddOwner(host.Object), Times.Once);
        }

        [Fact]
        public void Should_Set_Owner_On_Assigned_Resources_2()
        {
            var host = new Mock<IResourceHost>();
            var target = new Styles();

            var resources = new Mock<IResourceDictionary>();
            target.Resources = resources.Object;

            host.Invocations.Clear();
            ((IResourceProvider)target).AddOwner(host.Object);
            resources.Verify(x => x.AddOwner(host.Object), Times.Once);
        }

        [Fact]
        public void Should_Set_Owner_On_Child_Style()
        {
            var host = new Mock<IResourceHost>();
            var target = new Styles();
            ((IResourceProvider)target).AddOwner(host.Object);

            var style = new Mock<IStyle>();
            var resourceProvider = style.As<IResourceProvider>();
            target.Add(style.Object);

            resourceProvider.Verify(x => x.AddOwner(host.Object), Times.Once);
        }

        [Fact]
        public void Should_Set_Owner_On_Child_Style_2()
        {
            var host = new Mock<IResourceHost>();
            var target = new Styles();

            var style = new Mock<IStyle>();
            var resourceProvider = style.As<IResourceProvider>();
            target.Add(style.Object);

            host.Invocations.Clear();
            ((IResourceProvider)target).AddOwner(host.Object);
            resourceProvider.Verify(x => x.AddOwner(host.Object), Times.Once);
        }
        [Fact]
        public void Finds_Resource_In_Merged_Dictionary()
        {
            var target = new Styles
            {
                Resources = new ResourceDictionary
                {
                    MergedDictionaries =
                    {
                        new ResourceDictionary
                        {
                            { "foo", "bar" },
                        }
                    }
                }
            };

            Assert.True(target.TryGetResource("foo", out var result));
            Assert.Equal("bar", result);
        }
    }
}
