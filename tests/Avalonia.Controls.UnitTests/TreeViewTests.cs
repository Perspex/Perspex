using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using Avalonia.Collections;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Data.Core;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Styling;
using Avalonia.UnitTests;
using JetBrains.Annotations;
using Xunit;

namespace Avalonia.Controls.UnitTests
{
    public class TreeViewTests
    {
        MouseTestHelper _mouse = new MouseTestHelper();
        
        [Fact]
        public void Items_Should_Be_Created()
        {
            var target = new TreeView
            {
                Template = CreateTreeViewTemplate(),
                Items = CreateTestTreeData(),
            };

            var root = new TestRoot(target);

            CreateNodeDataTemplate(target);
            ApplyTemplates(target);

            Assert.Equal(new[] { "Root" }, ExtractItemHeader(target, 0));
            Assert.Equal(new[] { "Child1", "Child2", "Child3" }, ExtractItemHeader(target, 1));
            Assert.Equal(new[] { "Grandchild2a" }, ExtractItemHeader(target, 2));
        }

        [Fact]
        public void Items_Should_Be_Created_Using_ItemTemplate_If_Present()
        {
            TreeView target;

            var root = new TestRoot
            {
                Child = target = new TreeView
                {
                    Template = CreateTreeViewTemplate(),
                    Items = CreateTestTreeData(),
                    ItemTemplate = new FuncTreeDataTemplate<Node>(
                        (_, __) => new Canvas(),
                        x => x.Children),
                }
            };

            ApplyTemplates(target);

            var items = target.ItemContainerGenerator.Index.Containers
                .OfType<TreeViewItem>()
                .ToList();

            Assert.Equal(5, items.Count);
            Assert.All(items, x => Assert.IsType<Canvas>(x.HeaderPresenter.Child));
        }

        [Fact]
        public void Root_ItemContainerGenerator_Containers_Should_Be_Root_Containers()
        {
            var target = new TreeView
            {
                Template = CreateTreeViewTemplate(),
                Items = CreateTestTreeData(),
            };

            var root = new TestRoot(target);

            CreateNodeDataTemplate(target);
            ApplyTemplates(target);

            var container = (TreeViewItem)target.ItemContainerGenerator.Containers.Single().ContainerControl;
            var header = (TextBlock)container.Header;
            Assert.Equal("Root", header.Text);
        }

        [Fact]
        public void Root_TreeContainerFromItem_Should_Return_Descendant_Item()
        {
            var tree = CreateTestTreeData();
            var target = new TreeView
            {
                Template = CreateTreeViewTemplate(),
                Items = tree,
            };

            // For TreeViewItem to find its parent TreeView, OnAttachedToLogicalTree needs
            // to be called, which requires an IStyleRoot.
            var root = new TestRoot();
            root.Child = target;

            CreateNodeDataTemplate(target);
            ApplyTemplates(target);

            var container = target.ItemContainerGenerator.Index.ContainerFromItem(
                tree[0].Children[1].Children[0]);

            Assert.NotNull(container);

            var header = ((TreeViewItem)container).Header;
            var headerContent = ((TextBlock)header).Text;

            Assert.Equal("Grandchild2a", headerContent);
        }

        [Fact]
        public void Clicking_Item_Should_Select_It()
        {
            using (Application())
            {
                var tree = CreateTestTreeData();
                var target = new TreeView
                {
                    Template = CreateTreeViewTemplate(),
                    Items = tree,
                };

                var visualRoot = new TestRoot();
                visualRoot.Child = target;

                CreateNodeDataTemplate(target);
                ApplyTemplates(target);
                ExpandAll(target);

                var item = tree[0].Children[1].Children[0];
                var container = (TreeViewItem)target.ItemContainerGenerator.Index.ContainerFromItem(item);

                Assert.NotNull(container);

                _mouse.Click(container);

                Assert.Equal(item, target.SelectedItem);
                Assert.True(container.IsSelected);
            }
        }

        [Fact]
        public void Clicking_WithControlModifier_Selected_Item_Should_Deselect_It()
        {
            using (Application())
            {
                var tree = CreateTestTreeData();
                var target = new TreeView
                {
                    Template = CreateTreeViewTemplate(),
                    Items = tree
                };

                var visualRoot = new TestRoot();
                visualRoot.Child = target;

                CreateNodeDataTemplate(target);
                ApplyTemplates(target);
                ExpandAll(target);

                var item = tree[0].Children[1].Children[0];
                var container = (TreeViewItem)target.ItemContainerGenerator.Index.ContainerFromItem(item);

                Assert.NotNull(container);

                target.SelectedItem = item;

                Assert.True(container.IsSelected);

                _mouse.Click(container, modifiers: KeyModifiers.Control);

                Assert.Null(target.SelectedItem);
                Assert.False(container.IsSelected);
            }
        }

        [Fact]
        public void Clicking_WithControlModifier_Not_Selected_Item_Should_Select_It()
        {
            using (Application())
            {
                var tree = CreateTestTreeData();
                var target = new TreeView
                {
                    Template = CreateTreeViewTemplate(),
                    Items = tree
                };

                var visualRoot = new TestRoot();
                visualRoot.Child = target;

                CreateNodeDataTemplate(target);
                ApplyTemplates(target);
                ExpandAll(target);

                var item1 = tree[0].Children[1].Children[0];
                var container1 = (TreeViewItem)target.ItemContainerGenerator.Index.ContainerFromItem(item1);

                var item2 = tree[0].Children[1];
                var container2 = (TreeViewItem)target.ItemContainerGenerator.Index.ContainerFromItem(item2);

                Assert.NotNull(container1);
                Assert.NotNull(container2);

                target.SelectedItem = item1;

                Assert.True(container1.IsSelected);

                _mouse.Click(container2, modifiers: KeyModifiers.Control);

                Assert.Equal(item2, target.SelectedItem);
                Assert.False(container1.IsSelected);
                Assert.True(container2.IsSelected);
            }
        }

        [Fact]
        public void Clicking_WithControlModifier_Selected_Item_Should_Deselect_And_Remove_From_SelectedItems()
        {
            using (Application())
            {
                var tree = CreateTestTreeData();
                var target = new TreeView
                {
                    Template = CreateTreeViewTemplate(),
                    Items = tree,
                    SelectionMode = SelectionMode.Multiple
                };

                var visualRoot = new TestRoot();
                visualRoot.Child = target;

                CreateNodeDataTemplate(target);
                ApplyTemplates(target);
                ExpandAll(target);

                var rootNode = tree[0];

                var item1 = rootNode.Children[0];
                var item2 = rootNode.Children.Last();

                var item1Container = (TreeViewItem)target.ItemContainerGenerator.Index.ContainerFromItem(item1);
                var item2Container = (TreeViewItem)target.ItemContainerGenerator.Index.ContainerFromItem(item2);

                ClickContainer(item1Container, KeyModifiers.Control);
                Assert.True(item1Container.IsSelected);

                ClickContainer(item2Container, KeyModifiers.Control);
                Assert.True(item2Container.IsSelected);

                Assert.Equal(new[] { item1, item2 }, target.SelectedItems.OfType<Node>());

                ClickContainer(item1Container, KeyModifiers.Control);
                Assert.False(item1Container.IsSelected);

                Assert.DoesNotContain(item1, target.SelectedItems.OfType<Node>());
            }
        }

        [Fact]
        public void Clicking_WithShiftModifier_DownDirection_Should_Select_Range_Of_Items()
        {
            using (Application())
            {
                var tree = CreateTestTreeData();
                var target = new TreeView
                {
                    Template = CreateTreeViewTemplate(),
                    Items = tree,
                    SelectionMode = SelectionMode.Multiple
                };

                var visualRoot = new TestRoot();
                visualRoot.Child = target;

                CreateNodeDataTemplate(target);
                ApplyTemplates(target);
                ExpandAll(target);

                var rootNode = tree[0];

                var from = rootNode.Children[0];
                var to = rootNode.Children.Last();

                var fromContainer = (TreeViewItem)target.ItemContainerGenerator.Index.ContainerFromItem(from);
                var toContainer = (TreeViewItem)target.ItemContainerGenerator.Index.ContainerFromItem(to);

                ClickContainer(fromContainer, KeyModifiers.None);

                Assert.True(fromContainer.IsSelected);

                ClickContainer(toContainer, KeyModifiers.Shift);
                AssertChildrenSelected(target, rootNode);
            }
        }

        [Fact]
        public void Clicking_WithShiftModifier_UpDirection_Should_Select_Range_Of_Items()
        {
            using (Application())
            {
                var tree = CreateTestTreeData();
                var target = new TreeView
                {
                    Template = CreateTreeViewTemplate(),
                    Items = tree,
                    SelectionMode = SelectionMode.Multiple
                };

                var visualRoot = new TestRoot();
                visualRoot.Child = target;

                CreateNodeDataTemplate(target);
                ApplyTemplates(target);
                ExpandAll(target);

                var rootNode = tree[0];

                var from = rootNode.Children.Last();
                var to = rootNode.Children[0];

                var fromContainer = (TreeViewItem)target.ItemContainerGenerator.Index.ContainerFromItem(from);
                var toContainer = (TreeViewItem)target.ItemContainerGenerator.Index.ContainerFromItem(to);

                ClickContainer(fromContainer, KeyModifiers.None);

                Assert.True(fromContainer.IsSelected);

                ClickContainer(toContainer, KeyModifiers.Shift);
                AssertChildrenSelected(target, rootNode);
            }
        }

        [Fact]
        public void Clicking_First_Item_Of_SelectedItems_Should_Select_Only_It()
        {
            using (Application())
            {
                var tree = CreateTestTreeData();
                var target = new TreeView
                {
                    Template = CreateTreeViewTemplate(),
                    Items = tree,
                    SelectionMode = SelectionMode.Multiple
                };

                var visualRoot = new TestRoot();
                visualRoot.Child = target;

                CreateNodeDataTemplate(target);
                ApplyTemplates(target);
                ExpandAll(target);

                var rootNode = tree[0];

                var from = rootNode.Children.Last();
                var to = rootNode.Children[0];

                var fromContainer = (TreeViewItem)target.ItemContainerGenerator.Index.ContainerFromItem(from);
                var toContainer = (TreeViewItem)target.ItemContainerGenerator.Index.ContainerFromItem(to);

                ClickContainer(fromContainer, KeyModifiers.None);

                ClickContainer(toContainer, KeyModifiers.Shift);
                AssertChildrenSelected(target, rootNode);

                ClickContainer(fromContainer, KeyModifiers.None);

                Assert.True(fromContainer.IsSelected);

                foreach (var child in rootNode.Children)
                {
                    if (child == from)
                    {
                        continue;
                    }

                    var container = (TreeViewItem)target.ItemContainerGenerator.Index.ContainerFromItem(child);

                    Assert.False(container.IsSelected);
                }
            }
        }

        [Fact]
        public void Setting_SelectedItem_Should_Set_Container_Selected()
        {
            using (Application())
            {
                var tree = CreateTestTreeData();
                var target = new TreeView
                {
                    Template = CreateTreeViewTemplate(),
                    Items = tree,
                };

                var visualRoot = new TestRoot();
                visualRoot.Child = target;

                CreateNodeDataTemplate(target);
                ApplyTemplates(target);
                ExpandAll(target);

                var item = tree[0].Children[1].Children[0];
                var container = (TreeViewItem)target.ItemContainerGenerator.Index.ContainerFromItem(item);

                Assert.NotNull(container);

                target.SelectedItem = item;

                Assert.True(container.IsSelected);
            }
        }

        [Fact]
        public void Setting_SelectedItem_Should_Raise_SelectedItemChanged_Event()
        {
            using (Application())
            {
                var tree = CreateTestTreeData();
                var target = new TreeView
                {
                    Template = CreateTreeViewTemplate(),
                    Items = tree,
                };

                var visualRoot = new TestRoot();
                visualRoot.Child = target;

                CreateNodeDataTemplate(target);
                ApplyTemplates(target);
                ExpandAll(target);

                var item = tree[0].Children[1].Children[0];

                var called = false;
                target.SelectionChanged += (s, e) =>
                {
                    Assert.Empty(e.RemovedItems);
                    Assert.Equal(1, e.AddedItems.Count);
                    Assert.Same(item, e.AddedItems[0]);
                    called = true;
                };

                target.SelectedItem = item;
                Assert.True(called);
            }
        }

        [Fact]
        public void Bound_SelectedItem_Should_Not_Be_Cleared_when_Changing_Selection()
        {
            using (Application())
            {
                var dataContext = new TestDataContext();

                var target = new TreeView
                {
                    Template = CreateTreeViewTemplate(),
                    DataContext = dataContext
                };

                target.Bind(TreeView.ItemsProperty, new Binding("Items"));
                target.Bind(TreeView.SelectedItemProperty, new Binding("SelectedItem"));

                var visualRoot = new TestRoot();
                visualRoot.Child = target;

                CreateNodeDataTemplate(target);
                ApplyTemplates(target);

                var selectedValues = new List<object>();

                dataContext.PropertyChanged += (_, e) =>
                {
                    if (e.PropertyName == nameof(TestDataContext.SelectedItem))
                        selectedValues.Add(dataContext.SelectedItem);
                };
                selectedValues.Add(dataContext.SelectedItem);

                _mouse.Click((Interactive)target.Presenter.Panel.Children[0], MouseButton.Left);
                _mouse.Click((Interactive)target.Presenter.Panel.Children[2], MouseButton.Left);

                Assert.Equal(3, selectedValues.Count);
                Assert.Equal(new[] { null, "Item 0", "Item 2" }, selectedValues.ToArray());
            }
        }

        [Fact]
        public void LogicalChildren_Should_Be_Set()
        {
            var target = new TreeView
            {
                Template = CreateTreeViewTemplate(),
                Items = new[] { "Foo", "Bar", "Baz " },
            };

            ApplyTemplates(target);

            var result = target.GetLogicalChildren()
                .OfType<TreeViewItem>()
                .Select(x => x.Header)
                .OfType<TextBlock>()
                .Select(x => x.Text)
                .ToList();

            Assert.Equal(new[] { "Foo", "Bar", "Baz " }, result);
        }

        [Fact]
        public void Removing_Item_Should_Remove_Itself_And_Children_From_Index()
        {
            var tree = CreateTestTreeData();
            var target = new TreeView
            {
                Template = CreateTreeViewTemplate(),
                Items = tree,
            };

            var root = new TestRoot();
            root.Child = target;

            CreateNodeDataTemplate(target);
            ApplyTemplates(target);

            Assert.Equal(5, target.ItemContainerGenerator.Index.Containers.Count());

            tree[0].Children.RemoveAt(1);

            Assert.Equal(3, target.ItemContainerGenerator.Index.Containers.Count());
        }

        [Fact]
        public void DataContexts_Should_Be_Correctly_Set()
        {
            var items = new object[]
            {
                "Foo",
                new Node { Value = "Bar" },
                new TextBlock { Text = "Baz" },
                new TreeViewItem { Header = "Qux" },
            };

            var target = new TreeView
            {
                Template = CreateTreeViewTemplate(),
                DataContext = "Base",
                DataTemplates =
                {
                    new FuncDataTemplate<Node>((x, _) => new Button { Content = x })
                },
                Items = items,
            };

            ApplyTemplates(target);

            var dataContexts = target.Presenter.Panel.Children
                .Cast<Control>()
                .Select(x => x.DataContext)
                .ToList();

            Assert.Equal(
                new object[] { items[0], items[1], "Base", "Base" },
                dataContexts);
        }

        [Fact]
        public void Control_Item_Should_Not_Be_NameScope()
        {
            var items = new object[]
            {
                new TreeViewItem(),
            };

            var target = new TreeView
            {
                Template = CreateTreeViewTemplate(),
                Items = items,
            };

            target.ApplyTemplate();
            target.Presenter.ApplyTemplate();

            var item = target.Presenter.Panel.LogicalChildren[0];
            Assert.Null(NameScope.GetNameScope((TreeViewItem)item));
        }

        [Fact]
        public void Should_React_To_Children_Changing()
        {
            var data = CreateTestTreeData();

            var target = new TreeView
            {
                Template = CreateTreeViewTemplate(),
                Items = data,
            };

            var root = new TestRoot(target);

            CreateNodeDataTemplate(target);
            ApplyTemplates(target);

            Assert.Equal(new[] { "Root" }, ExtractItemHeader(target, 0));
            Assert.Equal(new[] { "Child1", "Child2", "Child3" }, ExtractItemHeader(target, 1));
            Assert.Equal(new[] { "Grandchild2a" }, ExtractItemHeader(target, 2));

            // Make sure that the binding to Node.Children does not get collected.
            GC.Collect();

            data[0].Children = new AvaloniaList<Node>
            {
                new Node
                {
                    Value = "NewChild1",
                }
            };

            Assert.Equal(new[] { "Root" }, ExtractItemHeader(target, 0));
            Assert.Equal(new[] { "NewChild1" }, ExtractItemHeader(target, 1));
        }

        [Fact]
        public void Keyboard_Navigation_Should_Move_To_Last_Selected_Node()
        {
            using (Application())
            {
                var focus = FocusManager.Instance;
                var navigation = AvaloniaLocator.Current.GetService<IKeyboardNavigationHandler>();
                var data = CreateTestTreeData();

                var target = new TreeView
                {
                    Template = CreateTreeViewTemplate(),
                    Items = data,
                };

                var button = new Button();

                var root = new TestRoot
                {
                    Child = new StackPanel
                    {
                        Children = { target, button },
                    }
                };

                CreateNodeDataTemplate(target);
                ApplyTemplates(target);
                ExpandAll(target);

                var item = data[0].Children[0];
                var node = target.ItemContainerGenerator.Index.ContainerFromItem(item);
                Assert.NotNull(node);

                target.SelectedItem = item;
                node.Focus();
                Assert.Same(node, focus.Current);

                navigation.Move(focus.Current, NavigationDirection.Next);
                Assert.Same(button, focus.Current);

                navigation.Move(focus.Current, NavigationDirection.Next);
                Assert.Same(node, focus.Current);
            }
        }
        
        [Fact]
        public void Keyboard_Navigation_Should_Not_Crash_If_Selected_Item_Is_not_In_Tree()
        {
            using (Application())
            {
                var focus = FocusManager.Instance;
                var navigation = AvaloniaLocator.Current.GetService<IKeyboardNavigationHandler>();
                var data = CreateTestTreeData();

                var selectedNode = new Node { Value = "Out of Tree Selected Item" };

                var target = new TreeView
                {
                    Template = CreateTreeViewTemplate(),
                    Items = data,
                    SelectedItem = selectedNode
                };

                var button = new Button();

                var root = new TestRoot
                {
                    Child = new StackPanel
                    {
                        Children = { target, button },
                    }
                };

                CreateNodeDataTemplate(target);
                ApplyTemplates(target);
                ExpandAll(target);

                var item = data[0].Children[0];
                var node = target.ItemContainerGenerator.Index.ContainerFromItem(item);
                Assert.NotNull(node);

                target.SelectedItem = selectedNode;
                node.Focus();
                Assert.Same(node, focus.Current);

                var next = KeyboardNavigationHandler.GetNext(node, NavigationDirection.Previous);
            }
        }

        [Fact]
        public void Pressing_SelectAll_Gesture_Should_Select_All_Nodes()
        {
            using (UnitTestApplication.Start())
            {
                var tree = CreateTestTreeData();
                var target = new TreeView
                {
                    Template = CreateTreeViewTemplate(),
                    Items = tree,
                    SelectionMode = SelectionMode.Multiple
                };

                var visualRoot = new TestRoot();
                visualRoot.Child = target;

                CreateNodeDataTemplate(target);
                ApplyTemplates(target);
                ExpandAll(target);

                var rootNode = tree[0];

                var keymap = AvaloniaLocator.Current.GetService<PlatformHotkeyConfiguration>();
                var selectAllGesture = keymap.SelectAll.First();

                var keyEvent = new KeyEventArgs
                {
                    RoutedEvent = InputElement.KeyDownEvent,
                    Key = selectAllGesture.Key,
                    KeyModifiers = selectAllGesture.KeyModifiers
                };

                target.RaiseEvent(keyEvent);

                AssertChildrenSelected(target, rootNode);
            }
        }

        [Fact]
        public void Pressing_SelectAll_Gesture_With_Downward_Range_Selected_Should_Select_All_Nodes()
        {
            using (Application())
            {
                var tree = CreateTestTreeData();
                var target = new TreeView
                {
                    Template = CreateTreeViewTemplate(),
                    Items = tree,
                    SelectionMode = SelectionMode.Multiple
                };

                var visualRoot = new TestRoot();
                visualRoot.Child = target;

                CreateNodeDataTemplate(target);
                ApplyTemplates(target);
                ExpandAll(target);

                var rootNode = tree[0];

                var from = rootNode.Children[0];
                var to = rootNode.Children.Last();

                var fromContainer = (TreeViewItem)target.ItemContainerGenerator.Index.ContainerFromItem(from);
                var toContainer = (TreeViewItem)target.ItemContainerGenerator.Index.ContainerFromItem(to);

                ClickContainer(fromContainer, KeyModifiers.None);
                ClickContainer(toContainer, KeyModifiers.Shift);

                var keymap = AvaloniaLocator.Current.GetService<PlatformHotkeyConfiguration>();
                var selectAllGesture = keymap.SelectAll.First();

                var keyEvent = new KeyEventArgs
                {
                    RoutedEvent = InputElement.KeyDownEvent,
                    Key = selectAllGesture.Key,
                    KeyModifiers = selectAllGesture.KeyModifiers
                };

                target.RaiseEvent(keyEvent);

                AssertChildrenSelected(target, rootNode);
            }
        }

        [Fact]
        public void Pressing_SelectAll_Gesture_With_Upward_Range_Selected_Should_Select_All_Nodes()
        {
            using (Application())
            {
                var tree = CreateTestTreeData();
                var target = new TreeView
                {
                    Template = CreateTreeViewTemplate(),
                    Items = tree,
                    SelectionMode = SelectionMode.Multiple
                };

                var visualRoot = new TestRoot();
                visualRoot.Child = target;

                CreateNodeDataTemplate(target);
                ApplyTemplates(target);
                ExpandAll(target);

                var rootNode = tree[0];

                var from = rootNode.Children.Last();
                var to = rootNode.Children[0];

                var fromContainer = (TreeViewItem)target.ItemContainerGenerator.Index.ContainerFromItem(from);
                var toContainer = (TreeViewItem)target.ItemContainerGenerator.Index.ContainerFromItem(to);

                ClickContainer(fromContainer, KeyModifiers.None);
                ClickContainer(toContainer, KeyModifiers.Shift);

                var keymap = AvaloniaLocator.Current.GetService<PlatformHotkeyConfiguration>();
                var selectAllGesture = keymap.SelectAll.First();

                var keyEvent = new KeyEventArgs
                {
                    RoutedEvent = InputElement.KeyDownEvent,
                    Key = selectAllGesture.Key,
                    KeyModifiers = selectAllGesture.KeyModifiers
                };

                target.RaiseEvent(keyEvent);

                AssertChildrenSelected(target, rootNode);
            }
        }

        [Fact]
        public void Right_Click_On_SelectedItem_Should_Not_Clear_Existing_Selection()
        {
            var tree = CreateTestTreeData();
            var target = new TreeView
            {
                Template = CreateTreeViewTemplate(),
                Items = tree,
                SelectionMode = SelectionMode.Multiple,
            };

            var visualRoot = new TestRoot();
            visualRoot.Child = target;

            CreateNodeDataTemplate(target);
            ApplyTemplates(target);
            target.ExpandSubTree((TreeViewItem)target.Presenter.Panel.Children[0]);
            target.SelectAll();

            AssertChildrenSelected(target, tree[0]);
            Assert.Equal(5, target.SelectedItems.Count);

            _mouse.Click((Interactive)target.Presenter.Panel.Children[0], MouseButton.Right);

            Assert.Equal(5, target.SelectedItems.Count);
        }

        [Fact]
        public void Right_Click_On_UnselectedItem_Should_Clear_Existing_Selection()
        {
            using (Application())
            {
                var tree = CreateTestTreeData();
                var target = new TreeView
                {
                    Template = CreateTreeViewTemplate(),
                    Items = tree,
                    SelectionMode = SelectionMode.Multiple,
                };

                var visualRoot = new TestRoot();
                visualRoot.Child = target;

                CreateNodeDataTemplate(target);
                ApplyTemplates(target);
                target.ExpandSubTree((TreeViewItem)target.Presenter.Panel.Children[0]);

                var rootNode = tree[0];
                var to = rootNode.Children[0];
                var then = rootNode.Children[1];

                var fromContainer = (TreeViewItem)target.ItemContainerGenerator.Index.ContainerFromItem(rootNode);
                var toContainer = (TreeViewItem)target.ItemContainerGenerator.Index.ContainerFromItem(to);
                var thenContainer = (TreeViewItem)target.ItemContainerGenerator.Index.ContainerFromItem(then);

                ClickContainer(fromContainer, KeyModifiers.None);
                ClickContainer(toContainer, KeyModifiers.Shift);

                Assert.Equal(2, target.SelectedItems.Count);

                _mouse.Click(thenContainer, MouseButton.Right);

                Assert.Equal(1, target.SelectedItems.Count);
            }
        }

        [Fact]
        public void Shift_Right_Click_Should_Not_Select_Multiple()
        {
            using (Application())
            {
                var tree = CreateTestTreeData();
                var target = new TreeView
                {
                    Template = CreateTreeViewTemplate(),
                    Items = tree,
                    SelectionMode = SelectionMode.Multiple,
                };

                var visualRoot = new TestRoot();
                visualRoot.Child = target;

                CreateNodeDataTemplate(target);
                ApplyTemplates(target);
                target.ExpandSubTree((TreeViewItem)target.Presenter.Panel.Children[0]);

                var rootNode = tree[0];
                var from = rootNode.Children[0];
                var to = rootNode.Children[1];
                var fromContainer = (TreeViewItem)target.ItemContainerGenerator.Index.ContainerFromItem(from);
                var toContainer = (TreeViewItem)target.ItemContainerGenerator.Index.ContainerFromItem(to);

                _mouse.Click(fromContainer);
                _mouse.Click(toContainer, MouseButton.Right, modifiers: KeyModifiers.Shift);

                Assert.Equal(1, target.SelectedItems.Count);
            }
        }

        [Fact]
        public void Ctrl_Right_Click_Should_Not_Select_Multiple()
        {
            using (Application())
            {
                var tree = CreateTestTreeData();
                var target = new TreeView
                {
                    Template = CreateTreeViewTemplate(),
                    Items = tree,
                    SelectionMode = SelectionMode.Multiple,
                };

                var visualRoot = new TestRoot();
                visualRoot.Child = target;

                CreateNodeDataTemplate(target);
                ApplyTemplates(target);
                target.ExpandSubTree((TreeViewItem)target.Presenter.Panel.Children[0]);

                var rootNode = tree[0];
                var from = rootNode.Children[0];
                var to = rootNode.Children[1];
                var fromContainer = (TreeViewItem)target.ItemContainerGenerator.Index.ContainerFromItem(from);
                var toContainer = (TreeViewItem)target.ItemContainerGenerator.Index.ContainerFromItem(to);

                _mouse.Click(fromContainer);
                _mouse.Click(toContainer, MouseButton.Right, modifiers: KeyModifiers.Control);

                Assert.Equal(1, target.SelectedItems.Count);
            }
        }

        [Fact]
        public void TreeViewItems_Level_Should_Be_Set()
        {
            var tree = CreateTestTreeData();
            var target = new TreeView
            {
                Template = CreateTreeViewTemplate(),
                Items = tree,
            };

            var visualRoot = new TestRoot();
            visualRoot.Child = target;

            CreateNodeDataTemplate(target);
            ApplyTemplates(target);
            ExpandAll(target);

            Assert.Equal(0, GetItem(target, 0).Level);
            Assert.Equal(1, GetItem(target, 0, 0).Level);
            Assert.Equal(1, GetItem(target, 0, 1).Level);
            Assert.Equal(1, GetItem(target, 0, 2).Level);
            Assert.Equal(2, GetItem(target, 0, 1, 0).Level);
        }

        [Fact]
        public void TreeViewItems_Level_Should_Be_Set_For_Derived_TreeView()
        {
            var tree = CreateTestTreeData();
            var target = new DerivedTreeView
            {
                Template = CreateTreeViewTemplate(),
                Items = tree,
            };

            var visualRoot = new TestRoot();
            visualRoot.Child = target;

            CreateNodeDataTemplate(target);
            ApplyTemplates(target);
            ExpandAll(target);

            Assert.Equal(0, GetItem(target, 0).Level);
            Assert.Equal(1, GetItem(target, 0, 0).Level);
            Assert.Equal(1, GetItem(target, 0, 1).Level);
            Assert.Equal(1, GetItem(target, 0, 2).Level);
            Assert.Equal(2, GetItem(target, 0, 1, 0).Level);
        }

        [Fact]
        public void Adding_Node_To_Removed_And_ReAdded_Parent_Should_Not_Crash()
        {
            // Issue #2985
            var tree = CreateTestTreeData();
            var target = new TreeView
            {
                Template = CreateTreeViewTemplate(),
                Items = tree,
            };

            var visualRoot = new TestRoot();
            visualRoot.Child = target;

            CreateNodeDataTemplate(target);
            ApplyTemplates(target);
            ExpandAll(target);

            var parent = tree[0];
            var node = parent.Children[1];

            parent.Children.Remove(node);
            parent.Children.Add(node);

            var item = target.ItemContainerGenerator.Index.ContainerFromItem(node);
            ApplyTemplates(new[] { item });

            // #2985 causes ArgumentException here.
            node.Children.Add(new Node());
        }

        [Fact]
        public void Auto_Expanding_In_Style_Should_Not_Break_Range_Selection()
        {
            // Issue #2980.
            using (Application())
            {
                var target = new DerivedTreeView
                {
                    Template = CreateTreeViewTemplate(),
                    SelectionMode = SelectionMode.Multiple,
                    Items = new List<Node>
                {
                    new Node { Value = "Root1", },
                    new Node { Value = "Root2", },
                },
                };

                var visualRoot = new TestRoot
                {
                    Styles =
                    {
                        new Style(x => x.OfType<TreeViewItem>())
                        {
                            Setters =
                            {
                                new Setter(TreeViewItem.IsExpandedProperty, true),
                            },
                        },
                    },
                    Child = target,
                };

                CreateNodeDataTemplate(target);
                ApplyTemplates(target);

                _mouse.Click(GetItem(target, 0));
                _mouse.Click(GetItem(target, 1), modifiers: KeyModifiers.Shift);
            }
        }

        [Fact]
        public void Removing_TreeView_From_Root_Should_Preserve_TreeViewItems()
        {
            // Issue #3328
            var tree = CreateTestTreeData();
            var target = new TreeView
            {
                Template = CreateTreeViewTemplate(),
                Items = tree,
            };

            var root = new TestRoot();
            root.Child = target;

            CreateNodeDataTemplate(target);
            ApplyTemplates(target);
            ExpandAll(target);

            Assert.Equal(5, target.ItemContainerGenerator.Index.Containers.Count());

            root.Child = null;

            Assert.Equal(5, target.ItemContainerGenerator.Index.Containers.Count());
            Assert.Equal(1, target.Presenter.Panel.Children.Count);

            var rootNode = Assert.IsType<TreeViewItem>(target.Presenter.Panel.Children[0]);
            Assert.Equal(3, rootNode.ItemContainerGenerator.Containers.Count());
            Assert.Equal(3, rootNode.Presenter.Panel.Children.Count);

            var child2Node = Assert.IsType<TreeViewItem>(rootNode.Presenter.Panel.Children[1]);
            Assert.Equal(1, child2Node.ItemContainerGenerator.Containers.Count());
            Assert.Equal(1, child2Node.Presenter.Panel.Children.Count);
        }

        [Fact]
        public void Clearing_TreeView_Items_Clears_Index()
        {
            // Issue #3551
            var tree = CreateTestTreeData();
            var target = new TreeView
            {
                Template = CreateTreeViewTemplate(),
                Items = tree,
            };

            var root = new TestRoot();
            root.Child = target;

            CreateNodeDataTemplate(target);
            ApplyTemplates(target);

            var rootNode = tree[0];
            var container = (TreeViewItem)target.ItemContainerGenerator.Index.ContainerFromItem(rootNode);

            Assert.NotNull(container);

            root.Child = null;

            tree.Clear();

            Assert.Empty(target.ItemContainerGenerator.Index.Containers);
        }

        private void ApplyTemplates(TreeView tree)
        {
            tree.ApplyTemplate();
            tree.Presenter.ApplyTemplate();
            ApplyTemplates(tree.Presenter.Panel.Children);
        }

        private void ApplyTemplates(IEnumerable<IControl> controls)
        {
            foreach (TreeViewItem control in controls)
            {
                control.Template = CreateTreeViewItemTemplate();
                control.ApplyTemplate();
                control.Presenter.ApplyTemplate();
                control.HeaderPresenter.ApplyTemplate();
                ApplyTemplates(control.Presenter.Panel.Children);
            }
        }

        private TreeViewItem GetItem(TreeView target, params int[] indexes)
        {
            var c = (ItemsControl)target;

            foreach (var index in indexes)
            {
                var item = ((IList)c.Items)[index];
                c = (ItemsControl)target.ItemContainerGenerator.Index.ContainerFromItem(item);
            }

            return (TreeViewItem)c;
        }

        private IList<Node> CreateTestTreeData()
        {
            return new AvaloniaList<Node>
            {
                new Node
                {
                    Value = "Root",
                    Children = new AvaloniaList<Node>
                    {
                        new Node
                        {
                            Value = "Child1",
                        },
                        new Node
                        {
                            Value = "Child2",
                            Children = new AvaloniaList<Node>
                            {
                                new Node
                                {
                                    Value = "Grandchild2a",
                                },
                            },
                        },
                        new Node
                        {
                            Value = "Child3",
                        }
                    }
                }
            };
        }

        private void CreateNodeDataTemplate(IControl control)
        {
            control.DataTemplates.Add(new TestTreeDataTemplate());
        }

        private IControlTemplate CreateTreeViewTemplate()
        {
            return new FuncControlTemplate<TreeView>((parent, scope) => new ItemsPresenter
            {
                Name = "PART_ItemsPresenter",
                [~ItemsPresenter.ItemsProperty] = parent[~ItemsControl.ItemsProperty],
            }.RegisterInNameScope(scope));
        }

        private IControlTemplate CreateTreeViewItemTemplate()
        {
            return new FuncControlTemplate<TreeViewItem>((parent, scope) => new Panel
            {
                Children =
                {
                    new ContentPresenter
                    {
                        Name = "PART_HeaderPresenter",
                        [~ContentPresenter.ContentProperty] = parent[~TreeViewItem.HeaderProperty],
                    }.RegisterInNameScope(scope),
                    new ItemsPresenter
                    {
                        Name = "PART_ItemsPresenter",
                        [~ItemsPresenter.ItemsProperty] = parent[~ItemsControl.ItemsProperty],
                    }.RegisterInNameScope(scope)
                }
            });
        }

        private void ExpandAll(TreeView tree)
        {
            foreach (var i in tree.ItemContainerGenerator.Containers)
            {
                tree.ExpandSubTree((TreeViewItem)i.ContainerControl);
            }
        }

        private List<string> ExtractItemHeader(TreeView tree, int level)
        {
            return ExtractItemContent(tree.Presenter.Panel, 0, level)
                .Select(x => x.Header)
                .OfType<TextBlock>()
                .Select(x => x.Text)
                .ToList();
        }

        private IEnumerable<TreeViewItem> ExtractItemContent(IPanel panel, int currentLevel, int level)
        {
            foreach (TreeViewItem container in panel.Children)
            {
                if (container.Template == null)
                {
                    container.Template = CreateTreeViewItemTemplate();
                    container.ApplyTemplate();
                }

                if (currentLevel == level)
                {
                    yield return container;
                }
                else
                {
                    foreach (var child in ExtractItemContent(container.Presenter.Panel, currentLevel + 1, level))
                    {
                        yield return child;
                    }
                }
            }
        }

        private void ClickContainer(IControl container, KeyModifiers modifiers)
        {
            _mouse.Click(container, modifiers: modifiers);
        }

        private void AssertChildrenSelected(TreeView treeView, Node rootNode)
        {
            foreach (var child in rootNode.Children)
            {
                var container = (TreeViewItem)treeView.ItemContainerGenerator.Index.ContainerFromItem(child);

                Assert.True(container.IsSelected);
            }
        }

        private IDisposable Application()
        {
            return UnitTestApplication.Start(
                TestServices.MockThreadingInterface.With(
                    focusManager: new FocusManager(),
                    keyboardDevice: () => new KeyboardDevice(),
                    keyboardNavigation: new KeyboardNavigationHandler(),
                    inputManager: new InputManager()));
        }

        private class Node : NotifyingBase
        {
            private IAvaloniaList<Node> _children;

            public string Value { get; set; }

            public IAvaloniaList<Node> Children
            {
                get
                {
                    return _children;
                }

                set
                {
                    _children = value;
                    RaisePropertyChanged(nameof(Children));
                }
            }
        }

        private class TestTreeDataTemplate : ITreeDataTemplate
        {
            public IControl Build(object param)
            {
                var node = (Node)param;
                return new TextBlock { Text = node.Value };
            }

            public InstancedBinding ItemsSelector(object item)
            {
                var obs = ExpressionObserver.Create(item, o => (o as Node).Children);
                return InstancedBinding.OneWay(obs);
            }

            public bool Match(object data)
            {
                return data is Node;
            }
        }

        private class DerivedTreeView : TreeView
        {
        }

        private class TestDataContext : INotifyPropertyChanged
        {
            private string _selectedItem;

            public TestDataContext()
            {
                Items = new ObservableCollection<string>(Enumerable.Range(0, 5).Select(i => $"Item {i}"));
            }

            public ObservableCollection<string> Items { get; }

            public string SelectedItem
            {
                get { return _selectedItem; }
                set
                {
                    _selectedItem = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedItem)));
                }
            }

            public event PropertyChangedEventHandler PropertyChanged;
            
        }
    }
}
