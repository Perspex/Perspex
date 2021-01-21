using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using Avalonia.Collections;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.LogicalTree;
using Avalonia.Styling;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Controls.UnitTests
{
    public class ItemsControlTests
    {
        [Fact]
        public void Should_Use_ItemTemplate_To_Create_Control()
        {
            var target = new ItemsControl
            {
                Template = GetTemplate(),
                ItemTemplate = new FuncDataTemplate<string>((_, __) => new Canvas()),
            };

            target.Items = new[] { "Foo" };
            Layout(target);

            var container = (ContentPresenter)target.Presenter.RealizedElements.First();
            container.UpdateChild();

            Assert.IsType<Canvas>(container.Child);
        }

        [Fact]
        public void Container_Should_Have_TemplatedParent_Set_To_Null()
        {
            var target = new ItemsControl();

            target.Template = GetTemplate();
            target.Items = new[] { "Foo" };
            Layout(target);

            var container = (ContentPresenter)target.Presenter.RealizedElements.First();

            Assert.Null(container.TemplatedParent);
        }

        [Fact]
        public void Container_Should_Have_LogicalParent_Set_To_ItemsControl()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var root = new Window();
                var target = new ItemsControl();

                root.Content = target;

                var templatedParent = new Button();
                target.SetValue(StyledElement.TemplatedParentProperty, templatedParent);
                target.Template = GetTemplate();

                target.Items = new[] { "Foo" };

                Layout(target);

                var container = (ContentPresenter)target.Presenter.RealizedElements.First();

                Assert.Equal(target, container.Parent);
            }
        }

        [Fact]
        public void Control_Item_Should_Be_Logical_Child_Before_ApplyTemplate()
        {
            var target = new ItemsControl();
            var child = new Control();

            target.Template = GetTemplate();
            target.Items = new[] { child };

            Assert.Equal(child.Parent, target);
            Assert.Equal(child.GetLogicalParent(), target);
            Assert.Equal(new[] { child }, target.GetLogicalChildren());
        }

        [Fact]
        public void Added_Container_Should_Have_LogicalParent_Set_To_ItemsControl()
        {
            var item = new Border();
            var items = new ObservableCollection<Border>();

            var target = new ItemsControl
            {
                Template = GetTemplate(),
                Items = items,
            };

            var root = new TestRoot(true, target);
            Layout(root);

            items.Add(item);

            Assert.Equal(target, item.Parent);
        }

        [Fact]
        public void Control_Item_Should_Be_Removed_From_Logical_Children_Before_ApplyTemplate()
        {
            var target = new ItemsControl();
            var child = new Control();
            var items = new AvaloniaList<Control>(child);

            target.Template = GetTemplate();
            target.Items = items;
            items.RemoveAt(0);

            Assert.Null(child.Parent);
            Assert.Null(child.GetLogicalParent());
            Assert.Empty(target.GetLogicalChildren());
        }

        [Fact]
        public void Clearing_Items_Should_Clear_Child_Controls_Parent_Before_ApplyTemplate()
        {
            var target = new ItemsControl();
            var child = new Control();

            target.Template = GetTemplate();
            target.Items = new[] { child };
            target.Items = null;

            Assert.Null(child.Parent);
            Assert.Null(((ILogical)child).LogicalParent);
        }

        [Fact]
        public void Clearing_Items_Should_Clear_Child_Controls_Parent()
        {
            var target = new ItemsControl();
            var child = new Control();

            target.Template = GetTemplate();
            target.Items = new[] { child };
            target.ApplyTemplate();
            target.Items = null;

            Assert.Null(child.Parent);
            Assert.Null(((ILogical)child).LogicalParent);
        }

        [Fact]
        public void Adding_Control_Item_Should_Make_Control_Appear_In_LogicalChildren()
        {
            var target = new ItemsControl();
            var child = new Control();

            target.Template = GetTemplate();
            target.Items = new[] { child };

            // Should appear both before and after applying template.
            Assert.Equal(new ILogical[] { child }, target.GetLogicalChildren());

            target.ApplyTemplate();

            Assert.Equal(new ILogical[] { child }, target.GetLogicalChildren());
        }

        [Fact]
        public void Adding_String_Item_Should_Make_ContentPresenter_Appear_In_LogicalChildren()
        {
            var target = new ItemsControl();
            var child = new Control();

            target.Template = GetTemplate();
            target.Items = new[] { "Foo" };
            Layout(target);

            var logical = (ILogical)target;
            Assert.Equal(1, logical.LogicalChildren.Count);
            Assert.IsType<ContentPresenter>(logical.LogicalChildren[0]);
        }

        [Fact]
        public void Setting_Items_To_Null_Should_Remove_LogicalChildren()
        {
            var target = new ItemsControl();
            var child = new Control();

            target.Template = GetTemplate();
            target.Items = new[] { "Foo" };
            Layout(target);

            Assert.NotEmpty(target.GetLogicalChildren());

            target.Items = null;

            Assert.Equal(new ILogical[0], target.GetLogicalChildren());
        }


        [Fact]
        public void Setting_Items_Should_Fire_LogicalChildren_CollectionChanged()
        {
            var target = new ItemsControl();
            var child = new Control();
            var called = false;

            target.Template = GetTemplate();
            target.ApplyTemplate();

            ((ILogical)target).LogicalChildren.CollectionChanged += (s, e) =>
                called = e.Action == NotifyCollectionChangedAction.Add;

            target.Items = new[] { child };

            Assert.True(called);
        }

        [Fact]
        public void Setting_Items_To_Null_Should_Fire_LogicalChildren_CollectionChanged()
        {
            var target = new ItemsControl();
            var child = new Control();
            var called = false;

            target.Template = GetTemplate();
            target.Items = new[] { child };
            target.ApplyTemplate();

            ((ILogical)target).LogicalChildren.CollectionChanged += (s, e) =>
                called = e.Action == NotifyCollectionChangedAction.Remove;

            target.Items = null;

            Assert.True(called);
        }

        [Fact]
        public void Changing_Items_Should_Fire_LogicalChildren_CollectionChanged()
        {
            var target = new ItemsControl();
            var child = new Control();
            var called = false;

            target.Template = GetTemplate();
            target.Items = new[] { child };
            target.ApplyTemplate();

            ((ILogical)target).LogicalChildren.CollectionChanged += (s, e) => called = true;

            target.Items = new[] { "Foo" };

            Assert.True(called);
        }

        [Fact]
        public void Adding_Items_Should_Fire_LogicalChildren_CollectionChanged()
        {
            var target = new ItemsControl();
            var items = new AvaloniaList<string> { "Foo" };
            var called = false;

            target.Template = GetTemplate();
            target.Items = items;
            Layout(target);

            ((ILogical)target).LogicalChildren.CollectionChanged += (s, e) =>
                called = e.Action == NotifyCollectionChangedAction.Add;

            items.Add("Bar");
            Layout(target.Presenter);

            Assert.True(called);
        }

        [Fact]
        public void Removing_Items_Should_Not_Fire_LogicalChildren_CollectionChanged()
        {
            var target = new ItemsControl();
            var items = new AvaloniaList<string> { "Foo", "Bar" };
            var called = false;

            target.Template = GetTemplate();
            target.Items = items;
            Layout(target);

            ((ILogical)target).LogicalChildren.CollectionChanged += (s, e) =>
                called = e.Action == NotifyCollectionChangedAction.Remove;

            items.Remove("Bar");
            Layout(target.Presenter);

            // In this case, the control will be marked for recycling and so should remain in the
            // logical children collection for performance reasons.
            Assert.False(called);
        }

        [Fact]
        public void LogicalChildren_Should_Not_Change_Instance_When_Template_Changed()
        {
            var target = new ItemsControl()
            {
                Template = GetTemplate(),
            };

            var before = ((ILogical)target).LogicalChildren;

            target.Template = null;
            target.Template = GetTemplate();

            var after = ((ILogical)target).LogicalChildren;

            Assert.NotNull(before);
            Assert.NotNull(after);
            Assert.Same(before, after);
        }

        [Fact]
        public void Should_Clear_Containers_When_ItemsPresenter_Changes()
        {
            var target = new ItemsControl
            {
                Items = new[] { "foo", "bar" },
                Template = GetTemplate(),
            };

            Layout(target);

            Assert.Equal(2, target.Presenter.RealizedElements.Count());

            target.Template = GetTemplate();
            target.ApplyTemplate();

            Assert.Empty(target.Presenter.RealizedElements);
        }

        [Fact]
        public void Control_Items_Should_Be_Removed_From_Presenter_When_Removed_From_Items()
        {
            using var app = Start();

            var items = new AvaloniaList<IControl> { new Canvas() };
            var target = new ItemsControl
            {
                Items = items,
            };

            Prepare(target);

            var presenterPanel = (IPanel)target.Presenter;
            Assert.Equal(1, presenterPanel.Children.Count);

            items.RemoveAt(0);
            Layout(target);

            Assert.Equal(0, presenterPanel.Children.Count);
        }

        [Fact]
        public void Empty_Class_Should_Initially_Be_Applied()
        {
            var target = new ItemsControl()
            {
                Template = GetTemplate(),
            };

            Assert.Contains(":empty", target.Classes);
        }

        [Fact]
        public void Empty_Class_Should_Be_Cleared_When_Items_Added()
        {
            var target = new ItemsControl()
            {
                Template = GetTemplate(),
                Items = new[] { 1, 2, 3 },
            };

            Assert.DoesNotContain(":empty", target.Classes);
        }

        [Fact]
        public void Empty_Class_Should_Be_Set_When_Empty_Collection_Set()
        {
            var target = new ItemsControl()
            {
                Template = GetTemplate(),
                Items = new[] { 1, 2, 3 },
            };

            target.Items = new int[0];

            Assert.Contains(":empty", target.Classes);
        }

        [Fact]
        public void Setting_Presenter_Explicitly_Should_Set_Item_Parent()
        {
            var target = new TestItemsControl();
            var child = new Control();

            var presenter = new ItemsPresenter
            {
                [StyledElement.TemplatedParentProperty] = target,
                [~ItemsPresenter.ItemsViewProperty] = target[~ItemsControl.ItemsViewProperty],
            };

            presenter.ApplyTemplate();
            target.Presenter = presenter;
            target.Items = new[] { child };
            target.ApplyTemplate();

            Assert.Equal(target, child.Parent);
            Assert.Equal(target, ((ILogical)child).LogicalParent);
        }

        [Fact]
        public void DataContexts_Should_Be_Correctly_Set()
        {
            using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface))
            {
                var items = new object[]
                {
                    "Foo",
                    new Item("Bar"),
                    new TextBlock { Text = "Baz" },
                    new ListBoxItem { Content = "Qux" },
                };

                var target = new ItemsControl
                {
                    Template = GetTemplate(),
                    DataContext = "Base",
                    DataTemplates =
                    {
                        new FuncDataTemplate<Item>((x, __) => new Button { Content = x })
                    },
                    Items = items,
                };

                Layout(target);

                var dataContexts = target.Presenter.RealizedElements
                    .Do(x => (x as ContentPresenter)?.UpdateChild())
                    .Cast<Control>()
                    .Select(x => x.DataContext)
                    .ToList();

                Assert.Equal(
                    new object[] { items[0], items[1], "Base", "Base" },
                    dataContexts);
            }
        }

        [Fact]
        public void Control_Item_Should_Not_Be_NameScope()
        {
            var items = new object[]
            {
                new TextBlock(),
            };

            var target = new ItemsControl
            {
                Template = GetTemplate(),
                Items = items,
            };

            Layout(target);

            var item = target.Presenter.RealizedElements.First();
            Assert.Null(NameScope.GetNameScope((TextBlock)item));
        }

        [Fact]
        public void Focuses_Next_Item_On_Key_Down()
        {
            using var app = Start();

            var items = new object[]
            {
                new Button { Height = 10 },
                new Button { Height = 10 },
            };

            var target = new ItemsControl
            {
                Items = items,
            };

            Prepare(target);
            target.Presenter.RealizedElements.First().Focus();

            target.RaiseEvent(new KeyEventArgs
            {
                RoutedEvent = InputElement.KeyDownEvent,
                Key = Key.Down,
            });

            Assert.Equal(
                target.Presenter.RealizedElements.ElementAt(1),
                FocusManager.Instance.Current);
        }

        [Fact]
        public void Does_Not_Focus_Non_Focusable_Item_On_Key_Down()
        {
            using var app = Start();

            var items = new object[]
            {
                    new Button { Height = 10 },
                    new Button { Height = 10, Focusable = false },
                    new Button { Height = 10 },
            };

            var target = new ItemsControl
            {
                Items = items,
            };

            Prepare(target);
            target.Presenter.RealizedElements.First().Focus();

            target.RaiseEvent(new KeyEventArgs
            {
                RoutedEvent = InputElement.KeyDownEvent,
                Key = Key.Down,
            });

            Assert.Equal(
                target.Presenter.RealizedElements.ElementAt(2),
                FocusManager.Instance.Current);
        }

        [Fact]
        public void Detaching_Then_Reattaching_To_Logical_Tree_Twice_Does_Not_Throw()
        {
            // # Issue 3487
            var target = new ItemsControl
            {
                Template = GetTemplate(),
                Items = new[] { "foo", "bar" },
                ItemTemplate = new FuncDataTemplate<string>((_, __) => new Canvas()),
            };

            var root = new TestRoot(target);
            root.Measure(Size.Infinity);
            root.Arrange(new Rect(root.DesiredSize));

            root.Child = null;
            root.Child = target;

            target.Measure(Size.Infinity);

            root.Child = null;
            root.Child = target;
        }

        private static IDisposable Start()
        {
            var services = TestServices.MockPlatformRenderInterface.With(
                focusManager: new FocusManager(),
                keyboardDevice: () => new KeyboardDevice(),
                keyboardNavigation: new KeyboardNavigationHandler(),
                inputManager: new InputManager(),
                styler: new Styler(),
                windowingPlatform: new MockWindowingPlatform());
            return UnitTestApplication.Start(services);
        }

        private static void Prepare(ItemsControl target)
        {
            var root = new TestRoot
            {
                Child = target,
                Width = 100,
                Height = 100,
                Styles =
                {
                    new Style(x => x.Is<ItemsControl>())
                    {
                        Setters =
                        {
                            new Setter(ListBox.TemplateProperty, GetTemplate()),
                        },
                    },
                },
            };

            root.LayoutManager.ExecuteInitialLayoutPass();
        }

        private static void Layout(IControl target)
        {
            if (target.VisualRoot is ILayoutRoot root)
            {
                root.LayoutManager.ExecuteLayoutPass();
            }
            else
            {
                target.Measure(new Size(100, 100));
                target.Arrange(new Rect(0, 0, 100, 100));
            }
        }

        private class Item
        {
            public Item(string value)
            {
                Value = value;
            }

            public string Value { get; }
        }

        private static FuncControlTemplate GetTemplate()
        {
            return new FuncControlTemplate<ItemsControl>((parent, scope) =>
            {
                return new Border
                {
                    Background = new Media.SolidColorBrush(0xffffffff),
                    Child = new ItemsPresenter
                    {
                        Name = "PART_ItemsPresenter",
                        [~ItemsPresenter.LayoutProperty] = parent[~ItemsControl.LayoutProperty],
                        [~ItemsPresenter.ItemsViewProperty] = parent[~ItemsControl.ItemsViewProperty],
                    }.RegisterInNameScope(scope)
                };
            });
        }

        private class TestItemsControl : ItemsControl
        {
            public new IItemsPresenter Presenter
            {
                get { return base.Presenter; }
                set { base.Presenter = value; }
            }
        }
    }
}
