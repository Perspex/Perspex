using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Avalonia.Collections;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Controls.Utils;
using Avalonia.Layout;
using Avalonia.LogicalTree;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;
using Avalonia.UnitTests;
using Avalonia.VisualTree;
using Xunit;

namespace Avalonia.Controls.UnitTests
{
    public class TabControlTests
    {
        [Fact]
        public void First_Tab_Should_Be_Selected_By_Default()
        {
            using var app = Start();

            TabItem selected;
            var target = new TabControl
            {
                Items = new[]
                {
                    (selected = new TabItem
                    {
                        Name = "first",
                        Content = "foo",
                    }),
                    new TabItem
                    {
                        Name = "second",
                        Content = "bar",
                    },
                }
            };

            Prepare(target);

            Assert.Equal(0, target.SelectedIndex);
            Assert.Equal(selected, target.SelectedItem);
        }

        [Fact]
        public void Pre_Selecting_TabItem_Should_Set_SelectedContent_After_It_Was_Added()
        {
            using var app = Start();

            var target = new TabControl();

            const string secondContent = "Second";

            var items = new AvaloniaList<object>
            {
                new TabItem { Header = "First"},
                new TabItem { Header = "Second", Content = secondContent, IsSelected = true }
            };

            target.Items = items;

            Prepare(target);

            Assert.Equal(secondContent, target.SelectedContent);
        }

        [Fact]
        public void Logical_Children_Should_Be_TabItems_Plus_Content()
        {
            using var app = Start();

            var items = new[]
            {
                new TabItem
                {
                    Content = "foo"
                },
                new TabItem
                {
                    Content = "bar"
                },
            };

            var target = new TabControl
            {
                Items = items,
            };

            var logicalChildren = (IList<ILogical>)target.GetLogicalChildren();
            Assert.Equal(items, logicalChildren);

            Prepare(target);

            Assert.Equal(3, logicalChildren.Count);
            Assert.Same(items[0], logicalChildren[0]);
            Assert.Same(items[1], logicalChildren[1]);
            Assert.IsType<TextBlock>(logicalChildren[2]);
        }

        [Fact]
        public void Removal_Should_Set_First_Tab()
        {
            using var app = Start();

            var collection = new ObservableCollection<TabItem>()
            {
                new TabItem
                {
                    Name = "first",
                    Content = "foo",
                },
                new TabItem
                {
                    Name = "second",
                    Content = "bar",
                },
                new TabItem
                {
                    Name = "3rd",
                    Content = "barf",
                },
            };

            var target = new TabControl
            {
                Items = collection,
            };

            Prepare(target);
            target.SelectedItem = collection[1];
            Layout(target);

            Assert.Same(collection[1], target.SelectedItem);
            Assert.Equal(collection[1].Content, target.SelectedContent);

            collection.RemoveAt(1);
            Layout(target);

            Assert.Same(collection[0], target.SelectedItem);
            Assert.Equal(collection[0].Content, target.SelectedContent);
        }

        [Fact]
        public void Removal_Should_Set_New_Item0_When_Item0_Selected()
        {
            using var app = Start();

            var collection = new ObservableCollection<TabItem>()
            {
                new TabItem
                {
                    Name = "first",
                    Content = "foo",
                },
                new TabItem
                {
                    Name = "second",
                    Content = "bar",
                },
                new TabItem
                {
                    Name = "3rd",
                    Content = "barf",
                },
            };

            var target = new TabControl
            {
                Items = collection,
            };

            Prepare(target);
            target.SelectedItem = collection[0];

            Assert.Same(collection[0], target.SelectedItem);
            Assert.Equal(collection[0].Content, target.SelectedContent);

            collection.RemoveAt(0);

            Assert.Same(collection[0], target.SelectedItem);
            Assert.Equal(collection[0].Content, target.SelectedContent);
        }

        [Fact]
        public void Removal_Should_Set_New_Item0_When_Item0_Selected_With_DataTemplate()
        {
            using var app = Start();

            var collection = new ObservableCollection<Item>()
            {
                new Item("first"),
                new Item("second"),
                new Item("3rd"),
            };

            var target = new TabControl
            {
                Items = collection,
            };

            Prepare(target);
            target.SelectedItem = collection[0];

            Assert.Same(collection[0], target.SelectedItem);
            Assert.Equal(collection[0], target.SelectedContent);

            collection.RemoveAt(0);
            Layout(target);

            Assert.Same(collection[0], target.SelectedItem);
            Assert.Equal(collection[0], target.SelectedContent);
        }

        [Fact]
        public void TabItem_Templates_Should_Be_Set_Before_TabItem_ApplyTemplate()
        {
            var collection = new[]
            {
                new TabItem
                {
                    Name = "first",
                    Content = "foo",
                },
                new TabItem
                {
                    Name = "second",
                    Content = "bar",
                },
                new TabItem
                {
                    Name = "3rd",
                    Content = "barf",
                },
            };

            var template = new FuncControlTemplate<TabItem>((x, __) => new Decorator());

            using (UnitTestApplication.Start(TestServices.RealStyler))
            {
                var root = new TestRoot
                {
                    Styles =
                    {
                        new Style(x => x.OfType<TabItem>())
                        {
                            Setters =
                            {
                                new Setter(TemplatedControl.TemplateProperty, template)
                            }
                        }
                    },
                    Child = new TabControl
                    {
                        Template = TabControlTemplate(),
                        Items = collection,
                    }
                };
            }

            Assert.Same(collection[0].Template, template);
            Assert.Same(collection[1].Template, template);
            Assert.Same(collection[2].Template, template);
        }

        [Fact]
        public void DataContexts_Should_Be_Correctly_Set()
        {
            using var app = Start();

            var items = new object[]
            {
                "Foo",
                new Item("Bar"),
                new TextBlock { Text = "Baz" },
                new TabItem { Content = "Qux" },
                new TabItem { Content = new TextBlock { Text = "Bob" } }
            };

            var target = new TabControl
            {
                Template = TabControlTemplate(),
                DataContext = "Base",
                DataTemplates =
                {
                    new FuncDataTemplate<Item>((x, __) => new Button { Content = x })
                },
                Items = items,
            };

            var root = new TestRoot(target);

            ApplyTemplate(target);
            root.LayoutManager.ExecuteInitialLayoutPass();

            ((ContentPresenter)target.ContentPart).UpdateChild();
            var dataContext = ((TextBlock)target.ContentPart.Child).DataContext;
            Assert.Equal(items[0], dataContext);

            target.SelectedIndex = 1;

            ((ContentPresenter)target.ContentPart).UpdateChild();
            dataContext = ((Button)target.ContentPart.Child).DataContext;
            Assert.Equal(items[1], dataContext);

            target.SelectedIndex = 2;
            ((ContentPresenter)target.ContentPart).UpdateChild();
            dataContext = ((TextBlock)target.ContentPart.Child).DataContext;
            Assert.Equal("Base", dataContext);

            target.SelectedIndex = 3;
            ((ContentPresenter)target.ContentPart).UpdateChild();
            dataContext = ((TextBlock)target.ContentPart.Child).DataContext;
            Assert.Equal("Qux", dataContext);

            target.SelectedIndex = 4;
            ((ContentPresenter)target.ContentPart).UpdateChild();
            dataContext = target.ContentPart.DataContext;
            Assert.Equal("Base", dataContext);
        }

        /// <summary>
        /// Non-headered control items should result in TabItems with empty header.
        /// </summary>
        /// <remarks>
        /// If a TabControl is created with non IHeadered controls as its items, don't try to
        /// display the control in the header: if the control is part of the header then
        /// *that* control would also end up in the content region, resulting in dual-parentage 
        /// breakage.
        /// </remarks>
        [Fact]
        public void Non_IHeadered_Control_Items_Should_Be_Ignored()
        {
            using var app = Start();

            var items = new[]
            {
                new TextBlock { Text = "foo" },
                new TextBlock { Text = "bar" },
            };

            var target = new TabControl
            {
                Template = TabControlTemplate(),
                Items = items,
            };

            var root = new TestRoot(target)
            {
                Width = 100,
                Height = 100
            };

            ApplyTemplate(target);
            root.LayoutManager.ExecuteInitialLayoutPass();

            var logicalChildren = target.Presenter.RealizedElements;

            var result = logicalChildren
                .OfType<TabItem>()
                .Select(x => x.Header)
                .ToList();

            Assert.Equal(new object[] { null, null }, result);
        }

        [Fact]
        public void Should_Handle_Changing_To_TabItem_With_Null_Content()
        {
            TabControl target = new TabControl
            {
                Template = TabControlTemplate(),
                Items = new[]
                {
                    new TabItem { Header = "Foo" },
                    new TabItem { Header = "Foo", Content = new Decorator() },
                    new TabItem { Header = "Baz" },
                },
            };

            ApplyTemplate(target);

            target.SelectedIndex = 2;

            var page = (TabItem)target.SelectedItem;

            Assert.Null(page.Content);
        }

        [Fact]
        public void DataTemplate_Created_Content_Should_Be_Logical_Child_After_ApplyTemplate()
        {
            using var app = Start();

            TabControl target = new TabControl
            {
                Template = TabControlTemplate(),
                ContentTemplate = new FuncDataTemplate<string>((x, _) =>
                    new TextBlock { Tag = "bar", Text = x }),
                Items = new[] { "Foo" },
            };

            var root = new TestRoot(target);

            ApplyTemplate(target);
            root.LayoutManager.ExecuteInitialLayoutPass();

            ((ContentPresenter)target.ContentPart).UpdateChild();

            var content = Assert.IsType<TextBlock>(target.ContentPart.Child);
            Assert.Equal("bar", content.Tag);
            Assert.Same(target, content.GetLogicalParent());
            Assert.Single(target.GetLogicalChildren(), content);
        }

        [Fact]
        public void Should_Not_Propagate_DataContext_To_TabItem_Content()
        {
            var dataContext = "DataContext";

            var tabItem = new TabItem();

            var target = new TabControl
            {
                Template = TabControlTemplate(),
                DataContext = dataContext,
                Items = new AvaloniaList<object> { tabItem }
            };

            ApplyTemplate(target);

            Assert.NotEqual(dataContext, tabItem.Content);
        }

        [Fact]
        public void Can_Have_Empty_Tab_Control()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var xaml = @"
<Window xmlns='https://github.com/avaloniaui'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:local='clr-namespace:Avalonia.Markup.Xaml.UnitTests.Xaml;assembly=Avalonia.Markup.Xaml.UnitTests'>
    <TabControl Name='tabs' Items='{Binding Tabs}'/>
</Window>";
                var window = (Window)AvaloniaRuntimeXamlLoader.Load(xaml);
                var tabControl = window.FindControl<TabControl>("tabs");

                tabControl.DataContext = new { Tabs = new List<string>() };
                window.ApplyTemplate();

                Assert.Equal(0, tabControl.Items.Count());
            }
        }

        private static IDisposable Start()
        {
            var services = TestServices.MockPlatformRenderInterface.With(
                styler: new Styler(),
                windowingPlatform: new MockWindowingPlatform());
            return UnitTestApplication.Start(services);
        }

        private static IControlTemplate TabControlTemplate()
        {
            return new FuncControlTemplate<TabControl>((parent, scope) =>
                new StackPanel
                {
                    Children =
                    {
                        new ItemsPresenter
                        {
                            Name = "PART_ItemsPresenter",
                            [!ItemsPresenter.ItemsViewProperty] = parent[!TabControl.ItemsViewProperty],
                            [!TabStrip.ItemTemplateProperty] = parent[!TabControl.ItemTemplateProperty],
                        }.RegisterInNameScope(scope),
                        new ContentPresenter
                        {
                            Name = "PART_SelectedContentHost",
                            [!ContentPresenter.ContentProperty] = parent[!TabControl.SelectedContentProperty],
                            [!ContentPresenter.ContentTemplateProperty] = parent[!TabControl.SelectedContentTemplateProperty],
                        }.RegisterInNameScope(scope)
                    }
                });
        }

        private static IControlTemplate TabItemTemplate()
        {
            return new FuncControlTemplate<TabItem>((parent, scope) =>
                new ContentPresenter
                {
                    Name = "PART_ContentPresenter",
                    [!ContentPresenter.ContentProperty] = parent[!TabItem.HeaderProperty],
                    [!ContentPresenter.ContentTemplateProperty] = parent[!TabItem.HeaderTemplateProperty]
                }.RegisterInNameScope(scope));
        }

        private static void Prepare(TabControl target)
        {
            var root = new TestRoot
            {
                Child = target,
                Width = 100,
                Height = 100,
                Styles =
                {
                    new Style(x => x.OfType<TabControl>())
                    {
                        Setters =
                        {
                            new Setter(TabControl.TemplateProperty, TabControlTemplate()),
                        },
                    },
                    new Style(x => x.OfType<TabItem>())
                    {
                        Setters =
                        {
                            new Setter(TabItem.TemplateProperty, TabItemTemplate()),
                        },
                    },
                },
            };

            root.LayoutManager.ExecuteInitialLayoutPass();
        }

        private void Layout(TabControl target)
        {
            var root = (TestRoot)target.GetVisualRoot();
            root.LayoutManager.ExecuteLayoutPass();
        }

        private void ApplyTemplate(TabControl target)
        {
            target.ApplyTemplate();

            target.Presenter.ApplyTemplate();

            foreach (var tabItem in target.GetLogicalChildren().OfType<TabItem>())
            {
                tabItem.Template = TabItemTemplate();

                tabItem.ApplyTemplate();

                ((ContentPresenter)tabItem.Presenter).UpdateChild();
            }

            target.ContentPart.ApplyTemplate();
        }

        private void ItemsRepeaterWorkaround(SelectingItemsControl target)
        {
            // HACK: Selecting an item in ItemsRepeater causes it to be scrolled to the top of the viewport,
            // even if all items can fit in the viewport. This causes items before the selected item to be
            // unrealized. Work around this by scrolling to the top.
            target.ScrollIntoView(0);
        }

        private class Item
        {
            public Item(string value)
            {
                Value = value;
            }

            public string Value { get; }
        }
    }
}
