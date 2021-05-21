using System.Collections.ObjectModel;
using System.Linq;
using Moq;
using Avalonia.Collections;
using Avalonia.Controls.Generators;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.VisualTree;
using Xunit;

namespace Avalonia.Controls.UnitTests.Presenters
{
    public class ItemsPresenterTests
    {
        [Fact]
        public void Should_Register_With_Host_When_TemplatedParent_Set()
        {
            var host = new Mock<IItemsPresenterHost>();
            var target = new ItemsPresenter();

            target.SetValue(Control.TemplatedParentProperty, host.Object);

            host.Verify(x => x.RegisterItemsPresenter(target));
        }

        [Fact]
        public void Should_Add_Containers()
        {
            var target = new ItemsPresenter
            {
                Items = new[] { "foo", "bar" },
            };

            target.ApplyTemplate();

            Assert.Equal(2, target.Panel.Children.Count);
            Assert.IsType<ContentPresenter>(target.Panel.Children[0]);
            Assert.IsType<ContentPresenter>(target.Panel.Children[1]);
            Assert.Equal("foo", ((ContentPresenter)target.Panel.Children[0]).Content);
            Assert.Equal("bar", ((ContentPresenter)target.Panel.Children[1]).Content);
        }

        [Fact]
        public void Should_Add_Containers_Of_Correct_Type()
        {
            var target = new ItemsPresenter
            {
                Items = new[] { "foo", "bar" },
            };

            target.ItemContainerGenerator = new ItemContainerGenerator<ListBoxItem>(
                target, 
                ListBoxItem.ContentProperty,
                null);
            target.ApplyTemplate();

            Assert.Equal(2, target.Panel.Children.Count);
            Assert.IsType<ListBoxItem>(target.Panel.Children[0]);
            Assert.IsType<ListBoxItem>(target.Panel.Children[1]);
        }

        [Fact]
        public void Should_Create_Containers_Only_Once()
        {
            var parent = new TestItemsControl();
            var target = new ItemsPresenter
            {
                Items = new[] { "foo", "bar" },
                [StyledElement.TemplatedParentProperty] = parent,
            };
            var raised = 0;

            parent.ItemContainerGenerator.Materialized += (s, e) => ++raised;

            target.ApplyTemplate();

            Assert.Equal(2, target.Panel.Children.Count);
            Assert.Equal(2, raised);
        }

        [Fact]
        public void ItemContainerGenerator_Should_Be_Picked_Up_From_TemplatedControl()
        {
            var parent = new TestItemsControl();
            var target = new ItemsPresenter
            {
                [StyledElement.TemplatedParentProperty] = parent,
            };

            Assert.IsType<ItemContainerGenerator<TestItem>>(target.ItemContainerGenerator);
        }

        [Fact]
        public void Should_Remove_Containers()
        {
            var items = new AvaloniaList<string>(new[] { "foo", "bar" });
            var target = new ItemsPresenter
            {
                Items = items,
            };

            target.ApplyTemplate();
            items.RemoveAt(0);

            Assert.Single(target.Panel.Children);
            Assert.Equal("bar", ((ContentPresenter)target.Panel.Children[0]).Content);
            Assert.Equal("bar", ((ContentPresenter)target.ItemContainerGenerator.ContainerFromIndex(0)).Content);
        }

        [Fact]
        public void Clearing_Items_Should_Remove_Containers()
        {
            var items = new ObservableCollection<string> { "foo", "bar" };
            var target = new ItemsPresenter
            {
                Items = items,
            };

            target.ApplyTemplate();
            items.Clear();

            Assert.Empty(target.Panel.Children);
            Assert.Empty(target.ItemContainerGenerator.Containers);
        }

        [Fact]
        public void Replacing_Items_Should_Update_Containers()
        {
            var items = new ObservableCollection<string> { "foo", "bar", "baz" };
            var target = new ItemsPresenter
            {
                Items = items,
            };

            target.ApplyTemplate();
            items[1] = "baz";

            var text = target.Panel.Children
                .OfType<ContentPresenter>()
                .Select(x => x.Content)
                .ToList();

            Assert.Equal(new[] { "foo", "baz", "baz" }, text);
        }

        [Fact]
        public void Moving_Items_Should_Update_Containers()
        {
            var items = new ObservableCollection<string> { "foo", "bar", "baz" };
            var target = new ItemsPresenter
            {
                Items = items,
            };

            target.ApplyTemplate();
            items.Move(2, 1);

            var text = target.Panel.Children
                .OfType<ContentPresenter>()
                .Select(x => x.Content)
                .ToList();

            Assert.Equal(new[] { "foo", "baz", "bar" }, text);
        }

        [Fact]
        public void Inserting_Items_Should_Update_Containers()
        {
            var items = new ObservableCollection<string> { "foo", "bar", "baz" };
            var target = new ItemsPresenter
            {
                Items = items,
            };

            target.ApplyTemplate();
            items.Insert(2, "insert");

            var text = target.Panel.Children
                .OfType<ContentPresenter>()
                .Select(x => x.Content)
                .ToList();

            Assert.Equal(new[] { "foo", "bar", "insert", "baz" }, text);
        }

        [Fact]
        public void Setting_Items_To_Null_Should_Remove_Containers()
        {
            var target = new ItemsPresenter
            {
                Items = new[] { "foo", "bar" },
            };

            target.ApplyTemplate();
            target.Items = null;

            Assert.Empty(target.Panel.Children);
            Assert.Empty(target.ItemContainerGenerator.Containers);
        }

        [Fact]
        public void Should_Handle_Null_Items()
        {
            var items = new AvaloniaList<string>(new[] { "foo", null, "bar" });

            var target = new ItemsPresenter
            {
                Items = items,
            };

            target.ApplyTemplate();

            var text = target.Panel.Children.Cast<ContentPresenter>().Select(x => x.Content).ToList();

            Assert.Equal(new[] { "foo", null, "bar" }, text);
            Assert.NotNull(target.ItemContainerGenerator.ContainerFromIndex(0));
            Assert.NotNull(target.ItemContainerGenerator.ContainerFromIndex(1));
            Assert.NotNull(target.ItemContainerGenerator.ContainerFromIndex(2));

            items.RemoveAt(1);

            text = target.Panel.Children.Cast<ContentPresenter>().Select(x => x.Content).ToList();

            Assert.Equal(new[] { "foo", "bar" }, text);
            Assert.NotNull(target.ItemContainerGenerator.ContainerFromIndex(0));
            Assert.NotNull(target.ItemContainerGenerator.ContainerFromIndex(1));
        }

        [Fact]
        public void Inserting_Then_Removing_Should_Add_Remove_Containers()
        {
            var items = new AvaloniaList<string>(Enumerable.Range(0, 5).Select(x => $"Item {x}"));
            var toAdd = Enumerable.Range(0, 3).Select(x => $"Added Item {x}").ToArray();
            var target = new ItemsPresenter
            {
                VirtualizationMode = ItemVirtualizationMode.None,
                Items = items,
                ItemTemplate = new FuncDataTemplate<string>((x, _) => new TextBlock { Height = 10 }),
            };

            target.ApplyTemplate();

            Assert.Equal(items.Count, target.Panel.Children.Count);

            foreach (var item in toAdd)
            {
                items.Insert(1, item);
            }

            Assert.Equal(items.Count, target.Panel.Children.Count);

            foreach (var item in toAdd)
            {
                items.Remove(item);
            }

            Assert.Equal(items.Count, target.Panel.Children.Count);
        }

        [Fact]
        public void Should_Handle_Duplicate_Items()
        {
            var items = new AvaloniaList<int>(new[] { 1, 2, 1 });

            var target = new ItemsPresenter
            {
                Items = items,
            };

            target.ApplyTemplate();
            items.RemoveAt(2);

            var numbers = target.Panel.Children
                .OfType<ContentPresenter>()
                .Select(x => x.Content)
                .Cast<int>();
            Assert.Equal(new[] { 1, 2 }, numbers);
        }

        [Fact]
        public void Panel_Should_Be_Created_From_ItemsPanel_Template()
        {
            var panel = new Panel();
            var target = new ItemsPresenter
            {
                ItemsPanel = new FuncTemplate<IPanel>(() => panel),
            };

            target.ApplyTemplate();

            Assert.Same(panel, target.Panel);
            Assert.Same(target, target.Panel.Parent);
        }

        [Fact]
        public void Panel_TabNavigation_Should_Be_Set_To_Once()
        {
            var target = new ItemsPresenter();

            target.ApplyTemplate();

            Assert.Equal(KeyboardNavigationMode.Once, target.Panel.TabNavigation);
        }

        [Fact]
        public void Panel_TabNavigation_Should_Be_Set_To_ItemsPresenter_Value()
        {
            var target = new ItemsPresenter();

            KeyboardNavigation.SetTabNavigation(target, KeyboardNavigationMode.Cycle);
            target.ApplyTemplate();

            Assert.Equal(KeyboardNavigationMode.Cycle, target.Panel.TabNavigation);
        }

        [Fact]
        public void Panel_Should_Be_Visual_Child()
        {
            var target = new ItemsPresenter();

            target.ApplyTemplate();

            var child = target.GetVisualChildren().Single();

            Assert.Equal(target.Panel, child);
        }

        private class Item
        {
            public Item(string value)
            {
                Value = value;
            }

            public string Value { get; }
        }

        private class TestItem : ContentControl
        {
        }

        private class TestItemsControl : ItemsControl
        {
            protected override IItemContainerGenerator CreateItemContainerGenerator()
            {
                return new ItemContainerGenerator<TestItem>(this, TestItem.ContentProperty, null);
            }
        }
    }
}
