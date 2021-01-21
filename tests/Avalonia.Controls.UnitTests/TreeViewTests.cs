using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using Avalonia.Collections;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Selection;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Data.Core;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Layout;
using Avalonia.LogicalTree;
using Avalonia.Styling;
using Avalonia.UnitTests;
using Avalonia.VisualTree;
using Xunit;

namespace Avalonia.Controls.UnitTests
{
    public class TreeViewTests
    {
        MouseTestHelper _mouse = new MouseTestHelper();
        
        [Fact]
        public void Items_Should_Be_Created()
        {
            using var app = Start();

            var target = new TreeView
            {
                Items = CreateTestTreeData(),
            };

            Prepare(target);

            Assert.Equal(new[] { "Root" }, ExtractItemHeader(target, 0));
            Assert.Equal(new[] { "Child1", "Child2", "Child3" }, ExtractItemHeader(target, 1));
            Assert.Equal(new[] { "Grandchild2a" }, ExtractItemHeader(target, 2));
        }

        [Fact]
        public void Items_Should_Be_Created_Using_ItemTemplate_If_Present()
        {
            using var app = Start();

            var target = new TreeView
            {
                Items = CreateTestTreeData(),
                ItemTemplate = new FuncTreeDataTemplate<Node>(
                    (_, __) => new Canvas(),
                    x => x.Children),
            };

            Prepare(target, createDataTemplates: false);

            var items = target.Presenter
                .GetVisualDescendants()
                .OfType<TreeViewItem>()
                .ToList();

            Assert.Equal(5, items.Count);
            Assert.All(items, x => Assert.IsType<Canvas>(x.HeaderPresenter.Child));
        }

        [Fact]
        public void Items_Should_Have_Correct_IndexPath()
        {
            using var app = Start();

            var target = new TreeView
            {
                Items = CreateTestTreeData(),
            };

            Prepare(target);

            var items = target.Presenter
                .GetVisualDescendants()
                .OfType<TreeViewItem>()
                .Select(x => x.IndexPath)
                .ToList();

            Assert.Equal(
                new[]
                {
                    new IndexPath(0),
                    new IndexPath(0, 0),
                    new IndexPath(0, 1),
                    new IndexPath(new[] { 0, 1, 0 }),
                    new IndexPath(0, 2),
                }, 
                items);
        }

        [Fact]
        public void TreeContainerFromIndex_Should_Return_Descendant_Item()
        {
            using var app = Start();

            var target = new TreeView
            {
                Items = CreateTestTreeData(),
            };

            Prepare(target);

            var index = new IndexPath(new[] { 0, 1, 0 });
            var container = target.TreeContainerFromIndex(index);

            Assert.NotNull(container);

            var headerContent = ((TextBlock)container.HeaderPresenter.Child).Text;

            Assert.Equal("Grandchild2a", headerContent);
        }

        [Fact]
        public void TreeContainerFromIndex_Should_Return_Null_When_Item_Removed()
        {
            using var app = Start();

            var items = CreateTestTreeData();
            var target = new TreeView
            {
                Items = items,
            };

            var root = Prepare(target);

            var index = new IndexPath(new[] { 0, 1, 0 });
            var container = target.TreeContainerFromIndex(index);

            Assert.NotNull(container);

            items[0].Children[1].Children.RemoveAt(0);

            root.LayoutManager.ExecuteLayoutPass();

            container = target.TreeContainerFromIndex(index);

            Assert.Null(container);
        }

        [Fact]
        public void Clicking_Item_Should_Select_It()
        {
            using var app = Start();

            var tree = CreateTestTreeData();
            var target = new TreeView
            {
                Items = tree,
            };

            Prepare(target);

            var item = tree[0].Children[1].Children[0];
            var container = target.TreeContainerFromIndex(new IndexPath(new[] { 0, 1, 0 }));

            Assert.NotNull(container);

            _mouse.Click(container);

            Assert.Equal(item, target.SelectedItem);
            Assert.True(container.IsSelected);
        }

        [Fact]
        public void Clicking_WithControlModifier_Selected_Item_Should_Deselect_It()
        {
            using var app = Start();

            var tree = CreateTestTreeData();
            var target = new TreeView
            {
                Items = tree
            };

            Prepare(target);

            var item = tree[0].Children[1].Children[0];
            var container = target.TreeContainerFromIndex(new IndexPath(new[] { 0, 1, 0 }));

            Assert.NotNull(container);

            target.SelectedItem = item;

            Assert.True(container.IsSelected);

            _mouse.Click(container, modifiers: KeyModifiers.Control);

            Assert.Null(target.SelectedItem);
            Assert.False(container.IsSelected);
        }

        [Fact]
        public void Clicking_WithControlModifier_Not_Selected_Item_Should_Select_It()
        {
            using var app = Start();

            var tree = CreateTestTreeData();
            var target = new TreeView
            {
                Items = tree
            };

            Prepare(target);

            var item1 = tree[0].Children[1].Children[0];
            var container1 = target.TreeContainerFromIndex(new IndexPath(new[] { 0, 1, 0 }));

            var item2 = tree[0].Children[1];
            var container2 = target.TreeContainerFromIndex(new IndexPath(new[] { 0, 1 }));

            Assert.NotNull(container1);
            Assert.NotNull(container2);

            target.SelectedItem = item1;

            Assert.True(container1.IsSelected);

            _mouse.Click(container2, modifiers: KeyModifiers.Control);

            Assert.Equal(item2, target.SelectedItem);
            Assert.False(container1.IsSelected);
            Assert.True(container2.IsSelected);
        }

        [Fact]
        public void Clicking_WithControlModifier_Selected_Item_Should_Deselect_And_Remove_From_SelectedItems()
        {
            using var app = Start();

            var tree = CreateTestTreeData();
            var target = new TreeView
            {
                Items = tree,
                SelectionMode = SelectionMode.Multiple
            };

            Prepare(target);

            var rootNode = tree[0];

            var item1 = rootNode.Children[0];
            var item2 = rootNode.Children.Last();

            var item1Container = target.TreeContainerFromIndex(new IndexPath(0, 0));
            var item2Container = target.TreeContainerFromIndex(new IndexPath(0, 2));

            ClickContainer(item1Container, KeyModifiers.Control);
            Assert.True(item1Container.IsSelected);

            ClickContainer(item2Container, KeyModifiers.Control);
            Assert.True(item2Container.IsSelected);

            Assert.Equal(new[] { item1, item2 }, target.SelectedItems.OfType<Node>());

            ClickContainer(item1Container, KeyModifiers.Control);
            Assert.False(item1Container.IsSelected);

            Assert.DoesNotContain(item1, target.SelectedItems.OfType<Node>());
        }

        [Fact]
        public void Clicking_WithShiftModifier_DownDirection_Should_Select_Range_Of_Items()
        {
            using var app = Start();

            var tree = CreateTestTreeData();
            var target = new TreeView
            {
                Items = tree,
                SelectionMode = SelectionMode.Multiple
            };

            Prepare(target);

            var rootNode = tree[0];

            var from = rootNode.Children[0];
            var to = rootNode.Children[2];

            var fromContainer = target.TreeContainerFromIndex(new IndexPath(0, 0));
            var toContainer = target.TreeContainerFromIndex(new IndexPath(0, 2));

            ClickContainer(fromContainer, KeyModifiers.None);

            Assert.True(fromContainer.IsSelected);

            ClickContainer(toContainer, KeyModifiers.Shift);
            AssertChildrenSelected(target, rootNode);
        }

        [Fact]
        public void Clicking_WithShiftModifier_UpDirection_Should_Select_Range_Of_Items()
        {
            using var app = Start();

            var tree = CreateTestTreeData();
            var target = new TreeView
            {
                Items = tree,
                SelectionMode = SelectionMode.Multiple
            };

            Prepare(target);

            var rootNode = tree[0];

            var from = rootNode.Children[2];
            var to = rootNode.Children[0];

            var fromContainer = target.TreeContainerFromIndex(new IndexPath(0, 2));
            var toContainer = target.TreeContainerFromIndex(new IndexPath(0, 0));

            ClickContainer(fromContainer, KeyModifiers.None);

            Assert.True(fromContainer.IsSelected);

            ClickContainer(toContainer, KeyModifiers.Shift);
            AssertChildrenSelected(target, rootNode);
        }

        [Fact]
        public void Clicking_First_Item_Of_SelectedItems_Should_Select_Only_It()
        {
            using var app = Start();

            var tree = CreateTestTreeData();
            var target = new TreeView
            {
                Items = tree,
                SelectionMode = SelectionMode.Multiple
            };

            Prepare(target);

            var rootNode = tree[0];

            var from = rootNode.Children[2];
            var to = rootNode.Children[0];

            var fromContainer = target.TreeContainerFromIndex(new IndexPath(0, 2));
            var toContainer = target.TreeContainerFromIndex(new IndexPath(0, 0));

            ClickContainer(fromContainer, KeyModifiers.None);

            ClickContainer(toContainer, KeyModifiers.Shift);
            AssertChildrenSelected(target, rootNode);

            ClickContainer(fromContainer, KeyModifiers.None);

            Assert.True(fromContainer.IsSelected);

            for (var i = 0; i < rootNode.Children.Count - 1; ++i)
            {
                var container = target.TreeContainerFromIndex(new IndexPath(0, i));
                Assert.False(container.IsSelected);
            }
        }

        [Fact]
        public void Setting_SelectedItem_Should_Set_Container_Selected()
        {
            using var app = Start();

            var tree = CreateTestTreeData();
            var target = new TreeView
            {
                Items = tree,
            };

            Prepare(target);

            var item = tree[0].Children[1].Children[0];
            var container = target.TreeContainerFromIndex(new IndexPath(new[] { 0, 1, 0 }));

            Assert.NotNull(container);

            target.SelectedItem = item;

            Assert.True(container.IsSelected);
        }

        [Fact]
        public void Setting_SelectedItem_Should_Raise_SelectedItemChanged_Event()
        {
            using var app = Start();

            var tree = CreateTestTreeData();
            var target = new TreeView
            {
                Items = tree,
            };

            Prepare(target);

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

        [Fact]
        public void Bound_SelectedItem_Should_Not_Be_Cleared_when_Changing_Selection()
        {
            using var app = Start();
            var dataContext = new TestDataContext();

            var target = new TreeView
            {
                DataContext = dataContext
            };

            target.Bind(TreeView.ItemsProperty, new Binding("Items"));
            target.Bind(TreeView.SelectedItemProperty, new Binding("SelectedItem"));

            Prepare(target);

            var selectedValues = new List<object>();

            dataContext.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(TestDataContext.SelectedItem))
                    selectedValues.Add(dataContext.SelectedItem);
            };
            selectedValues.Add(dataContext.SelectedItem);

            _mouse.Click(target.Presenter.RealizedElements.ElementAt(0), MouseButton.Left);
            _mouse.Click(target.Presenter.RealizedElements.ElementAt(2), MouseButton.Left);

            Assert.Equal(3, selectedValues.Count);
            Assert.Equal(new[] { null, "Item 0", "Item 2" }, selectedValues.ToArray());
        }

        [Fact]
        public void LogicalChildren_Should_Be_Set()
        {
            using var app = Start();

            var target = new TreeView
            {
                Items = new[] { "Foo", "Bar", "Baz " },
            };

            Prepare(target);

            var result = target.GetLogicalChildren()
                .OfType<TreeViewItem>()
                .Select(x => x.HeaderPresenter.Child)
                .OfType<TextBlock>()
                .Select(x => x.Text)
                .ToList();

            Assert.Equal(new[] { "Foo", "Bar", "Baz " }, result);
        }

        [Fact]
        public void Removing_Item_Should_Remove_Itself_And_Children_From_Index()
        {
            using var app = Start();

            var tree = CreateTestTreeData();
            var target = new TreeView
            {
                Items = tree,
            };

            Prepare(target);

            Assert.Equal(5, target.ItemContainerGenerator.Index.Containers.Count());

            tree[0].Children.RemoveAt(1);

            Assert.Equal(3, target.ItemContainerGenerator.Index.Containers.Count());
        }

        [Fact]
        public void DataContexts_Should_Be_Correctly_Set()
        {
            using var app = Start();

            var items = new object[]
            {
                "Foo",
                new Node { Value = "Bar" },
                new TextBlock { Text = "Baz" },
                new TreeViewItem { Header = "Qux" },
            };

            var target = new TreeView
            {
                DataContext = "Base",
                DataTemplates =
                {
                    new FuncDataTemplate<Node>((x, _) => new Button { Content = x })
                },
                Items = items,
            };

            Prepare(target);

            var dataContexts = target.Presenter.RealizedElements
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
            using var app = Start();

            var items = new object[]
            {
                new TreeViewItem(),
            };

            var target = new TreeView
            {
                Items = items,
            };

            Prepare(target);

            var item = target.Presenter.LogicalChildren[0];
            Assert.Null(NameScope.GetNameScope((TreeViewItem)item));
        }

        [Fact]
        public void Should_React_To_Children_Changing()
        {
            using var app = Start();

            var data = CreateTestTreeData();

            var target = new TreeView
            {
                Items = data,
            };

            Prepare(target);
            ExpandAll(target);

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

            Layout(target);

            Assert.Equal(new[] { "Root" }, ExtractItemHeader(target, 0));
            Assert.Equal(new[] { "NewChild1" }, ExtractItemHeader(target, 1));
        }

        [Fact]
        public void Keyboard_Navigation_Should_Move_To_Last_Selected_Node()
        {
            using (Start())
            {
                var focus = FocusManager.Instance;
                var navigation = AvaloniaLocator.Current.GetService<IKeyboardNavigationHandler>();
                var data = CreateTestTreeData();

                var target = new TreeView
                {
                    Items = data,
                };

                var button = new Button();

                CreateRoot(new StackPanel
                {
                    Children = { target, button },
                });

                Prepare(target);

                var item = data[0].Children[0];
                var node = target.TreeContainerFromIndex(new IndexPath(0, 0));
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
            using (Start())
            {
                var focus = FocusManager.Instance;
                var navigation = AvaloniaLocator.Current.GetService<IKeyboardNavigationHandler>();
                var data = CreateTestTreeData();

                var selectedNode = new Node { Value = "Out of Tree Selected Item" };

                var target = new TreeView
                {
                    Items = data,
                    SelectedItem = selectedNode
                };

                var button = new Button();

                CreateRoot(new StackPanel
                {
                    Children = { target, button },
                });

                Prepare(target);
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
            using (Start())
            {
                var tree = CreateTestTreeData();
                var target = new TreeView
                {
                    Items = tree,
                    SelectionMode = SelectionMode.Multiple
                };

                Prepare(target);
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
            using (Start())
            {
                var tree = CreateTestTreeData();
                var target = new TreeView
                {
                    Items = tree,
                    SelectionMode = SelectionMode.Multiple
                };

                Prepare(target);

                var rootNode = tree[0];
                var fromContainer = target.TreeContainerFromIndex(new IndexPath(0, 0));
                var toContainer = target.TreeContainerFromIndex(new IndexPath(0, 2));

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
            using (Start())
            {
                var tree = CreateTestTreeData();
                var target = new TreeView
                {
                    Items = tree,
                    SelectionMode = SelectionMode.Multiple
                };

                Prepare(target);

                var rootNode = tree[0];
                var fromContainer = target.TreeContainerFromIndex(new IndexPath(0, 2));
                var toContainer = target.TreeContainerFromIndex(new IndexPath(0, 0));

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
            using var app = Start();

            var tree = CreateTestTreeData();
            var target = new TreeView
            {
                Items = tree,
                SelectionMode = SelectionMode.Multiple,
            };

            Prepare(target);
            ExpandAll(target);
            target.SelectAll();

            AssertChildrenSelected(target, tree[0]);
            Assert.Equal(5, target.SelectedItems.Count);

            _mouse.Click(target.TreeContainerFromIndex(new IndexPath(0)), MouseButton.Right);

            Assert.Equal(5, target.SelectedItems.Count);
        }

        [Fact]
        public void Right_Click_On_UnselectedItem_Should_Clear_Existing_Selection()
        {
            using var app = Start();

            var tree = CreateTestTreeData();
            var target = new TreeView
            {
                Items = tree,
                SelectionMode = SelectionMode.Multiple,
            };

            Prepare(target);
            ExpandAll(target);

            var fromContainer = target.TreeContainerFromIndex(new IndexPath(0));
            var toContainer = target.TreeContainerFromIndex(new IndexPath(0, 0));
            var thenContainer = target.TreeContainerFromIndex(new IndexPath(0, 1));

            ClickContainer(fromContainer, KeyModifiers.None);
            ClickContainer(toContainer, KeyModifiers.Shift);

            Assert.Equal(2, target.SelectedItems.Count);

            _mouse.Click(thenContainer, MouseButton.Right);

            Assert.Equal(1, target.SelectedItems.Count);
        }

        [Fact]
        public void Shift_Right_Click_Should_Not_Select_Multiple()
        {
            using var app = Start();

            var tree = CreateTestTreeData();
            var target = new TreeView
            {
                Items = tree,
                SelectionMode = SelectionMode.Multiple,
            };

            Prepare(target);
            ExpandAll(target);

            var fromContainer = target.TreeContainerFromIndex(new IndexPath(0, 0));
            var toContainer = target.TreeContainerFromIndex(new IndexPath(0, 1));

            _mouse.Click(fromContainer);
            _mouse.Click(toContainer, MouseButton.Right, modifiers: KeyModifiers.Shift);

            Assert.Equal(1, target.SelectedItems.Count);
        }

        [Fact]
        public void Ctrl_Right_Click_Should_Not_Select_Multiple()
        {
            using var app = Start();

            var tree = CreateTestTreeData();
            var target = new TreeView
            {
                Items = tree,
                SelectionMode = SelectionMode.Multiple,
            };

            Prepare(target);
            ExpandAll(target);

            var fromContainer = target.TreeContainerFromIndex(new IndexPath(0, 0));
            var toContainer = target.TreeContainerFromIndex(new IndexPath(0, 1));

            _mouse.Click(fromContainer);
            _mouse.Click(toContainer, MouseButton.Right, modifiers: KeyModifiers.Control);

            Assert.Equal(1, target.SelectedItems.Count);
        }

        [Fact]
        public void TreeViewItems_Level_Should_Be_Set()
        {
            using var app = Start();

            var tree = CreateTestTreeData();
            var target = new TreeView
            {
                Items = tree,
            };

            Prepare(target);
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
            using var app = Start();

            var tree = CreateTestTreeData();
            var target = new DerivedTreeView
            {
                Items = tree,
            };

            Prepare(target);
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
            using var app = Start();

            // Issue #2985
            var tree = CreateTestTreeData();
            var target = new TreeView
            {
                Items = tree,
            };

            Prepare(target);
            ExpandAll(target);

            var parent = tree[0];
            var node = parent.Children[1];

            parent.Children.Remove(node);
            parent.Children.Add(node);

            var item = target.TreeContainerFromIndex(new IndexPath(new[] { 0, 0, 1 }));

            // #2985 causes ArgumentException here.
            node.Children.Add(new Node());
        }

        [Fact]
        public void Auto_Expanding_In_Style_Should_Not_Break_Range_Selection()
        {
            /// Issue #2980.
            using (Start())
            {
                var target = new DerivedTreeView
                {
                    SelectionMode = SelectionMode.Multiple,
                    Items = new List<Node>
                    {
                        new Node { Value = "Root1", },
                        new Node { Value = "Root2", },
                    },
                };

                var style = new Style(x => x.OfType<TreeViewItem>())
                {
                    Setters =
                    {
                        new Setter(TreeViewItem.IsExpandedProperty, true),
                    },
                };

                var root = CreateRoot(target);
                root.Styles.Add(style);

                Prepare(target);

                _mouse.Click(GetItem(target, 0));
                _mouse.Click(GetItem(target, 1), modifiers: KeyModifiers.Shift);
            }
        }

        [Fact]
        public void Removing_TreeView_From_Root_Should_Preserve_TreeViewItems()
        {
            using var app = Start();

            // Issue #3328
            var tree = CreateTestTreeData();
            var target = new TreeView
            {
                Items = tree,
            };

            var root = Prepare(target);
            ExpandAll(target);

            Assert.Equal(1, target.Presenter.RealizedElements.Count());

            root.Child = null;

            Assert.Equal(1, target.Presenter.RealizedElements.Count());

            var rootNode = (TreeViewItem)target.TryGetContainer(0);
            Assert.Equal(3, rootNode.Presenter.RealizedElements.Count());

            var child2Node = Assert.IsType<TreeViewItem>(rootNode.TryGetContainer(1));
            Assert.Equal(1, child2Node.Presenter.RealizedElements.Count());
        }

        [Fact]
        public void Clearing_TreeView_Items_Clears_Index()
        {
            using var app = Start();

            // Issue #3551
            var tree = CreateTestTreeData();
            var target = new TreeView
            {
                Items = tree,
            };

            Prepare(target);

            var rootNode = tree[0];
            var container = (TreeViewItem)target.ItemContainerGenerator.Index.ContainerFromItem(rootNode);

            Assert.NotNull(container);

            var root = (TestRoot)target.Parent;
            root.Child = null;

            tree.Clear();

            Assert.Empty(target.ItemContainerGenerator.Index.Containers);
        }

        private TestRoot Prepare(TreeView tree, bool createDataTemplates = true)
        {
            var root = tree.GetVisualRoot() as ILayoutRoot ?? CreateRoot(tree);

            if (createDataTemplates)
            {
                CreateNodeDataTemplate(tree);
            }

            root.LayoutManager.ExecuteInitialLayoutPass();

            return tree.Parent as TestRoot;
        }

        private void Layout(TreeView target)
        {
            var root = (ILayoutRoot)target.GetVisualRoot();
            root.LayoutManager.ExecuteLayoutPass();
        }

        private TestRoot CreateRoot(IControl child)
        {
            return new TestRoot
            {
                Styles =
                {
                    new Style(x => x.Is<TreeView>())
                    {
                        Setters =
                        {
                            new Setter(TreeView.TemplateProperty, TreeViewTemplate()),
                        }
                    },
                    new Style(x => x.OfType<TreeViewItem>())
                    {
                        Setters =
                        {
                            new Setter(TreeView.TemplateProperty, TreeViewItemTemplate()),
                        }
                    },
                },
                Child = child,
            };
        }

        private TreeViewItem GetItem(TreeView target, params int[] indexes)
        {
            return target.TreeContainerFromIndex(new IndexPath(indexes));
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

        private IControlTemplate TreeViewTemplate()
        {
            return new FuncControlTemplate<TreeView>((parent, scope) => new ItemsPresenter
            {
                Name = "PART_ItemsPresenter",
                [~ItemsPresenter.ItemsViewProperty] = parent[~ItemsControl.ItemsViewProperty],
                [~ItemsPresenter.LayoutProperty] = parent[~ItemsControl.LayoutProperty],
            }.RegisterInNameScope(scope));
        }

        private IControlTemplate TreeViewItemTemplate()
        {
            return new FuncControlTemplate<TreeViewItem>((parent, scope) => new Panel
            {
                Children =
                {
                    new ContentPresenter
                    {
                        Name = "PART_HeaderPresenter",
                        [~ContentPresenter.ContentProperty] = parent[~TreeViewItem.HeaderProperty],
                        [~ContentPresenter.ContentTemplateProperty] = parent[~TreeViewItem.HeaderTemplateProperty],
                    }.RegisterInNameScope(scope),
                    new ItemsPresenter
                    {
                        Name = "PART_ItemsPresenter",
                        [~ItemsPresenter.ItemsViewProperty] = parent[~ItemsControl.ItemsViewProperty],
                        [~ItemsPresenter.LayoutProperty] = parent[~ItemsControl.LayoutProperty],
                    }.RegisterInNameScope(scope)
                }
            });
        }

        private void ExpandAll(TreeView tree)
        {
            foreach (var i in tree.Presenter.RealizedElements)
            {
                tree.ExpandSubTree((TreeViewItem)i);
            }
        }

        private List<string> ExtractItemHeader(TreeView tree, int level)
        {
            return ExtractItemContent(tree.Presenter, 0, level)
                .Select(x => x.HeaderPresenter.Child)
                .OfType<TextBlock>()
                .Select(x => x.Text)
                .ToList();
        }

        private IEnumerable<TreeViewItem> ExtractItemContent(IItemsPresenter presenter, int currentLevel, int level)
        {
            foreach (TreeViewItem container in presenter.RealizedElements)
            {
                if (container.Template == null)
                {
                    container.ApplyTemplate();
                }

                if (currentLevel == level)
                {
                    yield return container;
                }
                else
                {
                    foreach (var child in ExtractItemContent(container.Presenter, currentLevel + 1, level))
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
            for (var i = 0; i < rootNode.Children.Count; ++i)
            {
                var container = treeView.TreeContainerFromIndex(new IndexPath(0, i));
                Assert.True(container.IsSelected);
            }
        }

        private static IDisposable Start()
        {
            var services = TestServices.MockPlatformRenderInterface.With(
                focusManager: new FocusManager(),
                keyboardDevice: () => new KeyboardDevice(),
                keyboardNavigation: new KeyboardNavigationHandler(),
                styler: new Styler(),
                threadingInterface: TestServices.MockThreadingInterface.ThreadingInterface);
            return UnitTestApplication.Start(services);
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
