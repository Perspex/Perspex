using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reactive.Linq;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.LogicalTree;
using Avalonia.Styling;
using Avalonia.UnitTests;
using Moq;
using Xunit;

namespace Avalonia.Base.UnitTests.Styling
{
    public class StyledElementTests
    {
        [Fact]
        public void Classes_Should_Initially_Be_Empty()
        {
            var target = new StyledElement();

            Assert.Empty(target.Classes);
        }

        [Fact]
        public void Setting_Parent_Should_Also_Set_InheritanceParent()
        {
            var parent = new Decorator();
            var target = new TestControl();

            parent.Child = target;

            Assert.Equal(parent, target.Parent);
            Assert.Equal(parent, target.InheritanceParent);
        }

        [Fact]
        public void Setting_Parent_Should_Not_Set_InheritanceParent_If_Already_Set()
        {
            var parent = new Decorator();
            var inheritanceParent = new Decorator();
            var target = new TestControl();

            ((ISetInheritanceParent)target).SetParent(inheritanceParent);
            parent.Child = target;

            Assert.Equal(parent, target.Parent);
            Assert.Equal(inheritanceParent, target.InheritanceParent);
        }

        [Fact]
        public void InheritanceParent_Should_Be_Cleared_When_Removed_From_Parent()
        {
            var parent = new Decorator();
            var target = new TestControl();

            parent.Child = target;
            parent.Child = null;

            Assert.Null(target.InheritanceParent);
        }

        [Fact]
        public void InheritanceParent_Should_Be_Cleared_When_Removed_From_Parent_When_Has_Different_InheritanceParent()
        {
            var parent = new Decorator();
            var inheritanceParent = new Decorator();
            var target = new TestControl();

            ((ISetInheritanceParent)target).SetParent(inheritanceParent);
            parent.Child = target;
            parent.Child = null;

            Assert.Null(target.InheritanceParent);
        }

        [Fact]
        public void Adding_Element_With_Null_Parent_To_Logical_Tree_Should_Throw()
        {
            var target = new Border();
            var visualParent = new Panel();
            var logicalParent = new Panel();
            var root = new TestRoot();

            // Set the logical parent...
            ((ISetLogicalParent)target).SetParent(logicalParent);

            // ...so that when it's added to `visualParent`, the parent won't be set again.
            visualParent.Children.Add(target);

            // Clear the logical parent. It's now a logical child of `visualParent` but doesn't have
            // a logical parent itself.
            ((ISetLogicalParent)target).SetParent(null);

            // In this case, attaching the control to a logical tree should throw.
            logicalParent.Children.Add(visualParent);
            Assert.Throws<InvalidOperationException>(() => root.Child = logicalParent);
        }

        [Fact]
        public void AttachedToLogicalTree_Should_Be_Called_When_Added_To_Tree()
        {
            var root = new TestRoot();
            var parent = new Border();
            var child = new Border();
            var grandchild = new Border();
            var parentRaised = false;
            var childRaised = false;
            var grandchildRaised = false;

            parent.AttachedToLogicalTree += (s, e) => parentRaised = true;
            child.AttachedToLogicalTree += (s, e) => childRaised = true;
            grandchild.AttachedToLogicalTree += (s, e) => grandchildRaised = true;

            parent.Child = child;
            child.Child = grandchild;

            Assert.False(parentRaised);
            Assert.False(childRaised);
            Assert.False(grandchildRaised);

            root.Child = parent;

            Assert.True(parentRaised);
            Assert.True(childRaised);
            Assert.True(grandchildRaised);
        }
        
        [Fact]
        public void AttachedToLogicalTree_Should_Be_Called_Before_Parent_Change_Signalled()
        {
            var root = new TestRoot();
            var child = new Border();
            var raised = new List<string>();

            child.AttachedToLogicalTree += (s, e) =>
            {
                Assert.Equal(root, child.Parent);
                raised.Add("attached");
            };

            child.GetObservable(StyledElement.ParentProperty).Skip(1).Subscribe(_ => raised.Add("parent"));

            root.Child = child;

            Assert.Equal(new[] { "attached", "parent" }, raised);
        }

        [Fact]
        public void AttachedToLogicalTree_Should_Not_Be_Called_With_GlobalStyles_As_Root()
        {
            var globalStyles = Mock.Of<IGlobalStyles>();
            var root = new TestRoot { StylingParent = globalStyles };
            var child = new Border();
            var raised = false;

            child.AttachedToLogicalTree += (s, e) =>
            {
                Assert.Equal(root, e.Root);
                raised = true;
            };

            root.Child = child;

            Assert.True(raised);
        }

        [Fact]
        public void AttachedToLogicalTree_Should_Have_Source_Set()
        {
            var root = new TestRoot();
            var canvas = new Canvas();
            var border = new Border { Child = canvas };
            var raised = 0;

            void Attached(object sender, LogicalTreeAttachmentEventArgs e)
            {
                Assert.Same(border, e.Source);
                ++raised;
            }

            border.AttachedToLogicalTree += Attached;
            canvas.AttachedToLogicalTree += Attached;

            root.Child = border;

            Assert.Equal(2, raised);
        }

        [Fact]
        public void AttachedToLogicalTree_Should_Have_Parent_Set()
        {
            var root = new TestRoot();
            var canvas = new Canvas();
            var border = new Border { Child = canvas };
            var raised = 0;

            void Attached(object sender, LogicalTreeAttachmentEventArgs e)
            {
                Assert.Same(root, e.Parent);
                ++raised;
            }

            border.AttachedToLogicalTree += Attached;
            canvas.AttachedToLogicalTree += Attached;

            root.Child = border;

            Assert.Equal(2, raised);
        }

        [Fact]
        public void DetachedFromLogicalTree_Should_Be_Called_When_Removed_From_Tree()
        {
            var root = new TestRoot();
            var parent = new Border();
            var child = new Border();
            var grandchild = new Border();
            var parentRaised = false;
            var childRaised = false;
            var grandchildRaised = false;

            parent.Child = child;
            child.Child = grandchild;
            root.Child = parent;

            parent.DetachedFromLogicalTree += (s, e) => parentRaised = true;
            child.DetachedFromLogicalTree += (s, e) => childRaised = true;
            grandchild.DetachedFromLogicalTree += (s, e) => grandchildRaised = true;

            root.Child = null;

            Assert.True(parentRaised);
            Assert.True(childRaised);
            Assert.True(grandchildRaised);
        }

        [Fact]
        public void DetachedFromLogicalTree_Should_Not_Be_Called_With_GlobalStyles_As_Root()
        {
            var globalStyles = Mock.Of<IGlobalStyles>();
            var root = new TestRoot { StylingParent = globalStyles };
            var child = new Border();
            var raised = false;

            child.DetachedFromLogicalTree += (s, e) =>
            {
                Assert.Equal(root, e.Root);
                raised = true;
            };

            root.Child = child;
            root.Child = null;

            Assert.True(raised);
        }

        [Fact]
        public void Parent_Should_Be_Null_When_DetachedFromLogicalTree_Called()
        {
            var target = new TestControl();
            var root = new TestRoot(target);
            var called = 0;

            target.DetachedFromLogicalTree += (s, e) =>
            {
                Assert.Null(target.Parent);
                Assert.Null(target.InheritanceParent);
                ++called;
            };

            root.Child = null;

            Assert.Equal(1, called);
        }

        [Fact]
        public void Adding_Tree_To_IStyleRoot_Should_Style_Controls()
        {
            using (AvaloniaLocator.EnterScope())
            {
                var root = new TestRoot();
                var parent = new Border();
                var child = new Border();
                var grandchild = new Control();
                var styler = new Mock<IStyler>();

                AvaloniaLocator.CurrentMutable.Bind<IStyler>().ToConstant(styler.Object);

                parent.Child = child;
                child.Child = grandchild;

                styler.Verify(x => x.ApplyStyles(It.IsAny<IStyleable>()), Times.Never());

                root.Child = parent;

                styler.Verify(x => x.ApplyStyles(parent), Times.Once());
                styler.Verify(x => x.ApplyStyles(child), Times.Once());
                styler.Verify(x => x.ApplyStyles(grandchild), Times.Once());
            }
        }

        [Fact]
        public void Styles_Not_Applied_Until_Initialization_Finished()
        {
            using (AvaloniaLocator.EnterScope())
            {
                var root = new TestRoot();
                var child = new Border();
                var styler = new Mock<IStyler>();

                AvaloniaLocator.CurrentMutable.Bind<IStyler>().ToConstant(styler.Object);

                ((ISupportInitialize)child).BeginInit();
                root.Child = child;
                styler.Verify(x => x.ApplyStyles(It.IsAny<IStyleable>()), Times.Never());

                ((ISupportInitialize)child).EndInit();
                styler.Verify(x => x.ApplyStyles(child), Times.Once());
            }
        }

        [Fact]
        public void Name_Cannot_Be_Set_After_Added_To_Logical_Tree()
        {
            using (AvaloniaLocator.EnterScope())
            {
                var root = new TestRoot();
                var child = new Border();

                root.Child = child;

                Assert.Throws<InvalidOperationException>(() => child.Name = "foo");
            }
        }

        [Fact]
        public void Name_Can_Be_Set_While_Initializing()
        {
            using (AvaloniaLocator.EnterScope())
            {
                var root = new TestRoot();
                var child = new Border();

                child.BeginInit();
                root.Child = child;
                child.Name = "foo";
                child.EndInit();
            }
        }

        [Fact]
        public void StyleInstance_Is_Disposed_When_Control_Removed_From_Logical_Tree()
        {
            using (AvaloniaLocator.EnterScope())
            {
                var root = new TestRoot();
                var child = new Border();

                root.Child = child;

                var styleInstance = new Mock<IStyleInstance>();
                ((IStyleable)child).StyleApplied(styleInstance.Object);

                root.Child = null;

                styleInstance.Verify(x => x.Dispose(), Times.Once);
            }
        }

        [Fact]
        public void EndInit_Should_Raise_Initialized()
        {
            var root = new TestRoot();
            var target = new Border();
            var called = false;

            target.Initialized += (s, e) => called = true;
            ((ISupportInitialize)target).BeginInit();
            root.Child = target;
            ((ISupportInitialize)target).EndInit();

            Assert.True(called);
            Assert.True(target.IsInitialized);
        }

        [Fact]
        public void Attaching_To_Visual_Tree_Should_Raise_Initialized()
        {
            var root = new TestRoot();
            var target = new Border();
            var called = false;

            target.Initialized += (s, e) => called = true;
            root.Child = target;

            Assert.True(called);
            Assert.True(target.IsInitialized);
        }

        [Fact]
        public void DataContextChanged_Should_Be_Called()
        {
            var root = new TestStackPanel
            {
                Name = "root",
                Children =
                {
                    new TestControl
                    {
                        Name = "a1",
                        Child = new TestControl
                        {
                            Name = "b1",
                        }
                    },
                    new TestControl
                    {
                        Name = "a2",
                        DataContext = "foo",
                    },
                }
            };

            var called = new List<string>();
            void Record(object sender, EventArgs e) => called.Add(((StyledElement)sender).Name);

            root.DataContextChanged += Record;

            foreach (TestControl c in root.GetLogicalDescendants())
            {
                c.DataContextChanged += Record;
            }

            root.DataContext = "foo";

            Assert.Equal(new[] { "root", "a1", "b1", }, called);
        }

        [Fact]
        public void DataContext_Notifications_Should_Be_Called_In_Correct_Order()
        {
            var root = new TestStackPanel
            {
                Name = "root",
                Children =
                {
                    new TestControl
                    {
                        Name = "a1",
                        Child = new TestControl
                        {
                            Name = "b1",
                        }
                    },
                    new TestControl
                    {
                        Name = "a2",
                        DataContext = "foo",
                    },
                }
            };

            var called = new List<string>();

            foreach (IDataContextEvents c in root.GetSelfAndLogicalDescendants())
            {
                c.DataContextBeginUpdate += (s, e) => called.Add("begin " + ((StyledElement)s).Name);
                c.DataContextChanged += (s, e) => called.Add("changed " + ((StyledElement)s).Name);
                c.DataContextEndUpdate += (s, e) => called.Add("end " + ((StyledElement)s).Name);
            }

            root.DataContext = "foo";

            Assert.Equal(
                new[] 
                {
                    "begin root",
                    "begin a1",
                    "begin b1",
                    "changed root",
                    "changed a1",
                    "changed b1",
                    "end b1",
                    "end a1",
                    "end root",
                },
                called);
        }

        [Fact]
        public void Resources_Owner_Is_Set()
        {
            var target = new TestControl();

            Assert.Same(target, ((ResourceDictionary)target.Resources).Owner);
        }

        [Fact]
        public void Assigned_Resources_Parent_Is_Set()
        {
            var resources = new Mock<IResourceDictionary>();
            var target = new TestControl { Resources = resources.Object };

            resources.Verify(x => x.AddOwner(target));
        }

        [Fact]
        public void Assigning_Resources_Raises_ResourcesChanged()
        {
            var resources = new ResourceDictionary { { "foo", "bar" } };
            var target = new TestControl();
            var raised = 0;

            target.ResourcesChanged += (s, e) => ++raised;
            target.Resources = resources;

            Assert.Equal(1, raised);
        }

        [Fact]
        public void Styles_Owner_Is_Set()
        {
            var target = new TestControl();

            Assert.Same(target, target.Styles.Owner);
        }

        [Fact]
        public void Adding_To_Logical_Tree_Raises_ResourcesChanged()
        {
            var target = new TestRoot();
            var parent = new Decorator { Resources = { { "foo", "bar" } } };
            var raised = 0;

            target.ResourcesChanged += (s, e) => ++raised;

            parent.Child = target;

            Assert.Equal(1, raised);
        }

        [Fact]
        public void SetParent_Does_Not_Crash_Due_To_Reentrancy()
        {
            // Issue #3708
            var app = UnitTestApplication.Start(TestServices.StyledWindow);

            ContentControl target;
            var root = new TestRoot
            {
                DataContext = false,
                Child = target = new ContentControl
                {
                    Styles =
                    {
                        new Style(x => x.OfType<ContentControl>())
                        {
                            Setters =
                            {
                                new Setter(
                                    ContentControl.ContentProperty,
                                    new FuncTemplate<IControl>(() => new TextBlock { Text = "Enabled" })),
                            },
                        },
                        new Style(x => x.OfType<ContentControl>().Class(":disabled"))
                        {
                            Setters =
                            {
                                new Setter(
                                    ContentControl.ContentProperty,
                                    new FuncTemplate<IControl>(() => new TextBlock { Text = "Disabled" })),
                            },
                        },
                    },
                    [!ContentControl.IsEnabledProperty] = new Binding(),
                }
            };

            root.Measure(Size.Infinity);
            root.Arrange(new Rect(0, 0, 100, 100));

            var textBlock = Assert.IsType<TextBlock>(target.Content);
            Assert.Equal("Disabled", textBlock.Text);

            // #3708 was crashing here with AvaloniaInternalException.
            root.Child = null;
        }

        private interface IDataContextEvents
        {
            event EventHandler DataContextBeginUpdate;
            event EventHandler DataContextChanged;
            event EventHandler DataContextEndUpdate;
        }

        private class TestControl : Decorator, IDataContextEvents
        {
            public event EventHandler DataContextBeginUpdate;
            public event EventHandler DataContextEndUpdate;

            public new IAvaloniaObject InheritanceParent => base.InheritanceParent;

            protected override void OnDataContextBeginUpdate()
            {
                DataContextBeginUpdate?.Invoke(this, EventArgs.Empty);
                base.OnDataContextBeginUpdate();
            }

            protected override void OnDataContextEndUpdate()
            {
                DataContextEndUpdate?.Invoke(this, EventArgs.Empty);
                base.OnDataContextEndUpdate();
            }
        }

        private class TestStackPanel : StackPanel, IDataContextEvents
        {
            public event EventHandler DataContextBeginUpdate;
            public event EventHandler DataContextEndUpdate;

            protected override void OnDataContextBeginUpdate()
            {
                DataContextBeginUpdate?.Invoke(this, EventArgs.Empty);
                base.OnDataContextBeginUpdate();
            }

            protected override void OnDataContextEndUpdate()
            {
                DataContextEndUpdate?.Invoke(this, EventArgs.Empty);
                base.OnDataContextEndUpdate();
            }
        }
    }
}
