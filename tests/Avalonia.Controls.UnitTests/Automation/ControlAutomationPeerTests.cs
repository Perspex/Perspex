using System.Linq;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Platform;
using Avalonia.Automation.Provider;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Platform;
using Avalonia.UnitTests;
using Avalonia.VisualTree;
using Moq;
using Xunit;

namespace Avalonia.Controls.UnitTests.Automation
{
    public class ControlAutomationPeerTests
    {
        private static Mock<IAutomationNodeFactory> _factory;

        public ControlAutomationPeerTests()
        {
            _factory = new Mock<IAutomationNodeFactory>();
            _factory.Setup(x => x.CreateNode(It.IsAny<AutomationPeer>()))
                .Returns(() => Mock.Of<IAutomationNode>(x => x.Factory == _factory));
        }

        public class Children
        {
            [Fact]
            public void Creates_Children_For_Controls_In_Visual_Tree()
            {
                var panel = new Panel
                {
                    Children =
                    {
                        new Border(),
                        new Border(),
                    },
                };

                var factory = CreateFactory();
                var target = CreatePeer(factory, panel);

                Assert.Equal(
                    panel.GetVisualChildren(),
                    target.GetChildren().Cast<ControlAutomationPeer>().Select(x => x.Owner));
            }

            [Fact]
            public void Creates_Children_when_Controls_Attached_To_Visual_Tree()
            {
                var contentControl = new ContentControl
                {
                    Template = new FuncControlTemplate<ContentControl>((o, ns) =>
                        new ContentPresenter
                        {
                            Name = "PART_ContentPresenter",
                            [!ContentPresenter.ContentProperty] = o[!ContentControl.ContentProperty],
                        }),
                    Content = new Border(),
                };

                var factory = CreateFactory();
                var target = CreatePeer(factory, contentControl);

                Assert.Empty(target.GetChildren());

                contentControl.Measure(Size.Infinity);

                Assert.Equal(1, target.GetChildren().Count);
            }

            [Fact]
            public void Updates_Children_When_VisualChildren_Added()
            {
                var panel = new Panel
                {
                    Children =
                    {
                        new Border(),
                        new Border(),
                    },
                };

                var factory = CreateFactory();
                var target = CreatePeer(factory, panel);
                var children = target.GetChildren();

                Assert.Equal(2, children.Count);

                panel.Children.Add(new Decorator());

                children = target.GetChildren();
                Assert.Equal(3, children.Count);
            }

            [Fact]
            public void Updates_Children_When_VisualChildren_Removed()
            {
                var panel = new Panel
                {
                    Children =
                    {
                        new Border(),
                        new Border(),
                    },
                };

                var factory = CreateFactory();
                var target = CreatePeer(factory, panel);
                var children = target.GetChildren();

                Assert.Equal(2, children.Count);

                panel.Children.RemoveAt(1);

                children = target.GetChildren();
                Assert.Equal(1, children.Count);
            }

            [Fact]
            public void Updates_Children_When_Visibility_Changes()
            {
                var panel = new Panel
                {
                    Children =
                    {
                        new Border(),
                        new Border(),
                    },
                };

                var factory = CreateFactory();
                var target = CreatePeer(factory, panel);
                var children = target.GetChildren();

                Assert.Equal(2, children.Count);

                panel.Children[1].IsVisible = false;
                children = target.GetChildren();
                Assert.Equal(1, children.Count);

                panel.Children[1].IsVisible = true;
                children = target.GetChildren();
                Assert.Equal(2, children.Count);
            }
        }

        public class Parent
        {
            [Fact]
            public void Connects_Peer_To_Tree_When_GetParent_Called()
            {
                var border = new Border();
                var tree = new Decorator
                {
                    Child = new Decorator
                    {
                        Child = border,
                    }
                };

                var factory = CreateFactory();

                // We're accessing Border directly without going via its ancestors. Because the tree
                // is built lazily, ensure that calling GetParent causes the ancestor tree to be built.
                var target = CreatePeer(factory, border);

                var parentPeer = Assert.IsAssignableFrom<ControlAutomationPeer>(target.GetParent());
                Assert.Same(border.GetVisualParent(), parentPeer.Owner);
            }

            [Fact]
            public void Parent_Updated_When_Moved_To_Separate_Visual_Tree()
            {
                var border = new Border();
                var root1 = new Decorator { Child = border };
                var root2 = new Decorator();
                var factory = CreateFactory();
                var target = CreatePeer(factory, border);

                var parentPeer = Assert.IsAssignableFrom<ControlAutomationPeer>(target.GetParent());
                Assert.Same(root1, parentPeer.Owner);

                root1.Child = null;

                Assert.Null(target.GetParent());

                root2.Child = border;

                parentPeer = Assert.IsAssignableFrom<ControlAutomationPeer>(target.GetParent());
                Assert.Same(root2, parentPeer.Owner);
            }
        }

        private static IAutomationNodeFactory CreateFactory()
        {
            var factory = new Mock<IAutomationNodeFactory>();
            factory.Setup(x => x.CreateNode(It.IsAny<AutomationPeer>()))
                .Returns(() => Mock.Of<IAutomationNode>(x => x.Factory == factory.Object));
            return factory.Object;
        }

        private static AutomationPeer CreatePeer(IAutomationNodeFactory factory, Control control)
        {
            return ControlAutomationPeer.GetOrCreatePeer(factory, control);
        }

        private class TestControl : Control
        {
            protected override AutomationPeer OnCreateAutomationPeer(IAutomationNodeFactory factory)
            {
                return new TestAutomationPeer(factory, this);
            }
        }

        private class AutomationTestRoot : TestRoot
        {
            protected override AutomationPeer OnCreateAutomationPeer(IAutomationNodeFactory factory)
            {
                return new TestRootAutomationPeer(factory, this);
            }
        }

        private class TestAutomationPeer : ControlAutomationPeer
        {
            public TestAutomationPeer(IAutomationNodeFactory factory, Control owner)
                : base(factory, owner) 
            {
            }
        }

        private class TestRootAutomationPeer : ControlAutomationPeer, IRootProvider
        {
            public TestRootAutomationPeer(IAutomationNodeFactory factory, Control owner)
                : base(factory, owner)
            {
            }

            public ITopLevelImpl PlatformImpl => throw new System.NotImplementedException();

            public AutomationPeer GetFocus()
            {
                throw new System.NotImplementedException();
            }

            public AutomationPeer GetPeerFromPoint(Point p)
            {
                throw new System.NotImplementedException();
            }
        }
    }
}
