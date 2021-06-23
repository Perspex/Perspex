using System;
using Avalonia.Controls;
using Moq;
using Xunit;

namespace Avalonia.Base.UnitTests.Styling
{
    public class ResourceDictionaryTests
    {
        [Fact]
        public void Cannot_Add_Null_Key()
        {
            var target = new ResourceDictionary();
            Assert.Throws<ArgumentNullException>(() => target.Add(null, "null"));
        }

        [Fact]
        public void Can_Add_Null_Value()
        {
            var target = new ResourceDictionary();
            target.Add("null", null);
        }

        [Fact]
        public void TryGetResource_Should_Find_Resource()
        {
            var target = new ResourceDictionary
            {
                { "foo", "bar" },
            };

            Assert.True(target.TryGetResource("foo", out var result));
            Assert.Equal("bar", result);
        }

        [Fact]
        public void TryGetResource_Should_Find_Resource_From_Merged_Dictionary()
        {
            var target = new ResourceDictionary
            {
                MergedDictionaries =
                {
                    new ResourceDictionary
                    {
                        { "foo", "bar" },
                    }
                }
            };

            Assert.True(target.TryGetResource("foo", out var result));
            Assert.Equal("bar", result);
        }

        [Fact]
        public void TryGetResource_Should_Find_Resource_From_Itself_Before_Merged_Dictionary()
        {
            var target = new ResourceDictionary
            {
                { "foo", "bar" },
            };

            target.MergedDictionaries.Add(new ResourceDictionary
            {
                { "foo", "baz" },
            });

            Assert.True(target.TryGetResource("foo", out var result));
            Assert.Equal("bar", result);
        }

        [Fact]
        public void TryGetResource_Should_Find_Resource_From_Later_Merged_Dictionary()
        {
            var target = new ResourceDictionary
            {
                MergedDictionaries =
                {
                    new ResourceDictionary
                    {
                        { "foo", "bar" },
                    },
                    new ResourceDictionary
                    {
                        { "foo", "baz" },
                    }
                }
            };

            Assert.True(target.TryGetResource("foo", out var result));
            Assert.Equal("baz", result);
        }

        [Fact]
        public void NotifyHostedResourcesChanged_Should_Be_Called_On_AddOwner()
        {
            var host = new Mock<IResourceHost>();
            var target = new ResourceDictionary { { "foo", "bar" } };

            ((IResourceProvider)target).AddOwner(host.Object);

            host.Verify(x => x.NotifyHostedResourcesChanged(It.IsAny<ResourcesChangedEventArgs>()));
        }

        [Fact]
        public void NotifyHostedResourcesChanged_Should_Be_Called_On_RemoveOwner()
        {
            var host = new Mock<IResourceHost>();
            var target = new ResourceDictionary { { "foo", "bar" } };

            ((IResourceProvider)target).AddOwner(host.Object);
            host.Invocations.Clear();
            ((IResourceProvider)target).RemoveOwner(host.Object);

            host.Verify(x => x.NotifyHostedResourcesChanged(It.IsAny<ResourcesChangedEventArgs>()));
        }

        [Fact]
        public void NotifyHostedResourcesChanged_Should_Be_Called_On_Resource_Add()
        {
            var host = new Mock<IResourceHost>();
            var target = new ResourceDictionary(host.Object);

            host.Invocations.Clear();
            target.Add("foo", "bar");

            host.Verify(x => x.NotifyHostedResourcesChanged(It.IsAny<ResourcesChangedEventArgs>()));
        }

        [Fact]
        public void NotifyHostedResourcesChanged_Should_Be_Called_On_MergedDictionary_Add()
        {
            var host = new Mock<IResourceHost>();
            var target = new ResourceDictionary(host.Object);

            host.Invocations.Clear();
            target.MergedDictionaries.Add(new ResourceDictionary
            {
                { "foo", "bar" },
            });

            host.Verify(
                x => x.NotifyHostedResourcesChanged(It.IsAny<ResourcesChangedEventArgs>()),
                Times.Once);
        }

        [Fact]
        public void NotifyHostedResourcesChanged_Should_Not_Be_Called_On_Empty_MergedDictionary_Add()
        {
            var host = new Mock<IResourceHost>();
            var target = new ResourceDictionary(host.Object);

            host.Invocations.Clear();
            target.MergedDictionaries.Add(new ResourceDictionary());

            host.Verify(
                x => x.NotifyHostedResourcesChanged(It.IsAny<ResourcesChangedEventArgs>()),
                Times.Never);
        }

        [Fact]
        public void NotifyHostedResourcesChanged_Should_Be_Called_On_MergedDictionary_Remove()
        {
            var host = new Mock<IResourceHost>();
            var target = new ResourceDictionary(host.Object)
            {
                MergedDictionaries =
                {
                    new ResourceDictionary { { "foo", "bar" } },
                }
            };

            host.Invocations.Clear();
            target.MergedDictionaries.RemoveAt(0);

            host.Verify(
                x => x.NotifyHostedResourcesChanged(It.IsAny<ResourcesChangedEventArgs>()),
                Times.Once);
        }

        [Fact]
        public void NotifyHostedResourcesChanged_Should_Be_Called_On_MergedDictionary_Resource_Add()
        {
            var host = new Mock<IResourceHost>();
            var target = new ResourceDictionary(host.Object)
            {
                MergedDictionaries =
                {
                    new ResourceDictionary(),
                }
            };

            host.Invocations.Clear();
            ((IResourceDictionary)target.MergedDictionaries[0]).Add("foo", "bar");

            host.Verify(
                x => x.NotifyHostedResourcesChanged(It.IsAny<ResourcesChangedEventArgs>()),
                Times.Once);
        }

        [Fact]
        public void Sets_Added_MergedDictionary_Owner()
        {
            var host = new Mock<IResourceHost>();

            var target = new ResourceDictionary(host.Object);
            target.MergedDictionaries.Add(new ResourceDictionary());

            Assert.Same(host.Object, target.Owner);
            Assert.Same(host.Object, ((ResourceDictionary)target.MergedDictionaries[0]).Owner);
        }

        [Fact]
        public void AddOwner_Sets_MergedDictionary_Owner()
        {
            var host = new Mock<IResourceHost>();

            var target = new ResourceDictionary
            {
                MergedDictionaries =
                {
                    new ResourceDictionary(),
                }
            };

            ((IResourceProvider)target).AddOwner(host.Object);

            Assert.Same(host.Object, target.Owner);
            Assert.Same(host.Object, ((ResourceDictionary)target.MergedDictionaries[0]).Owner);
        }

        [Fact]
        public void RemoveOwner_Clears_MergedDictionary_Owner()
        {
            var host = new Mock<IResourceHost>();

            var target = new ResourceDictionary(host.Object)
            {
                MergedDictionaries =
                {
                    new ResourceDictionary(),
                }
            };

            ((IResourceProvider)target).RemoveOwner(host.Object);

            Assert.Null(target.Owner);
            Assert.Null(((ResourceDictionary)target.MergedDictionaries[0]).Owner);
        }

    }
}
