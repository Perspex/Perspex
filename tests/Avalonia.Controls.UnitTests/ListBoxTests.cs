using System;
using System.Linq;
using Avalonia.Collections;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.LogicalTree;
using Avalonia.Styling;
using Avalonia.UnitTests;
using Avalonia.VisualTree;
using Xunit;

namespace Avalonia.Controls.UnitTests
{
    public partial class ListBoxTests
    {
        private MouseTestHelper _mouse = new MouseTestHelper();
        
        [Fact]
        public void Should_Use_ItemTemplate_To_Create_Item_Content()
        {
            using var app = Start();

            var target = new ListBox
            {
                Items = new[] { "Foo" },
                ItemTemplate = new FuncDataTemplate<string>((_, __) => new Canvas()),
            };

            Prepare(target);

            var container = (ListBoxItem)target.Presenter.RealizedElements.First();
            Assert.IsType<Canvas>(container.Presenter.Child);
        }

        [Fact]
        public void ListBox_Should_Find_ItemsPresenter_In_ScrollViewer()
        {
            using var app = Start();

            var target = new ListBox();

            Prepare(target);

            Assert.IsType<ItemsPresenter>(target.Presenter);
        }

        [Fact]
        public void ListBox_Should_Find_Scrollviewer_In_Template()
        {
            using var app = Start();

            var target = new ListBox();
            ScrollViewer viewer = null;

            target.TemplateApplied += (sender, e) =>
            {
                viewer = target.Scroll as ScrollViewer;
            };

            Prepare(target);

            Assert.NotNull(viewer);
        }

        [Fact]
        public void ListBoxItem_Containers_Should_Be_Generated()
        {
            using var app = Start();

            var items = new[] { "Foo", "Bar", "Baz " };
            var target = new ListBox { Items = items };

            Prepare(target);

            var text = target.Presenter.RealizedElements
                .OfType<ListBoxItem>()
                .Select(x => x.Presenter.Child)
                .OfType<TextBlock>()
                .Select(x => x.Text)
                .ToList();

            Assert.Equal(items, text);
        }

        [Fact]
        public void ListBoxItem_Items_Should_Be_Removed_From_Presenter_When_Items_Cleared()
        {
            using var app = Start();

            var items = new AvaloniaList<ListBoxItem>(
                Enumerable.Range(0, 100)
                .Select(x => new ListBoxItem
                {
                    Content = "Item " + x,
                }));
            var target = new ListBox
            {
                Items = items,
            };

            Prepare(target);

            var presenterPanel = (IPanel)target.Presenter;

            // Some, but not all items should be realized.
            Assert.NotEmpty(presenterPanel.Children);
            Assert.True(presenterPanel.Children.Count < 100);

            // Page to the bottom to ensure all items are realized.
            var scroller = (ScrollViewer)target.Scroll;
            while (scroller.Offset.Y + scroller.Viewport.Height < scroller.Extent.Height)
            {
                scroller.PageDown();
                Layout(target);
            }

            // All items should now be realized.
            Assert.Equal(100, presenterPanel.Children.Count);

            // Clear items.
            items.Clear();
            Layout(target);

            // And ensure they're all removed as children of presenter.
            Assert.Equal(0, presenterPanel.Children.Count);
        }

        [Fact]
        public void ListBoxItem_Items_Should_Be_Removed_From_Presenter_When_Removed_From_Items()
        {
            using var app = Start();

            var items = new AvaloniaList<ListBoxItem>(
                Enumerable.Range(0, 100)
                .Select(x => new ListBoxItem
                {
                    Content = "Item " + x,
                }));
            var target = new ListBox
            {
                Items = items,
            };

            Prepare(target);

            var presenterPanel = (IPanel)target.Presenter;

            // Some, but not all items should be realized.
            Assert.NotEmpty(presenterPanel.Children);
            Assert.True(presenterPanel.Children.Count < 100);

            // Page to the bottom to ensure all items are realized.
            var scroller = (ScrollViewer)target.Scroll;
            while (scroller.Offset.Y + scroller.Viewport.Height < scroller.Extent.Height)
            {
                scroller.PageDown();
                Layout(target);
            }

            // All items should now be realized.
            Assert.Equal(100, presenterPanel.Children.Count);

            // Clear items.
            while (items.Count > 0)
            {
                items.RemoveAt(0);
            }

            Layout(target);

            // And ensure they're all removed as children of presenter.
            Assert.Equal(0, presenterPanel.Children.Count);
        }

        [Fact]
        public void ListBoxItem_Items_Should_Be_Removed_From_Presenter_When_Items_Reassigned()
        {
            using var app = Start();

            var items = new AvaloniaList<ListBoxItem>(
                Enumerable.Range(0, 100)
                .Select(x => new ListBoxItem
                {
                    Content = "Item " + x,
                }));
            var target = new ListBox
            {
                Items = items,
            };

            Prepare(target);

            var presenterPanel = (IPanel)target.Presenter;

            // Some, but not all items should be realized.
            Assert.NotEmpty(presenterPanel.Children);
            Assert.True(presenterPanel.Children.Count < 100);

            // Page to the bottom to ensure all items are realized.
            var scroller = (ScrollViewer)target.Scroll;
            while (scroller.Offset.Y + scroller.Viewport.Height < scroller.Extent.Height)
            {
                scroller.PageDown();
                Layout(target);
            }

            // All items should now be realized.
            Assert.Equal(100, presenterPanel.Children.Count);

            // Clear items.
            target.Items = null;
            Layout(target);

            // And ensure they're all removed as children of presenter.
            Assert.Equal(0, presenterPanel.Children.Count);
        }

        [Fact]
        public void LogicalChildren_Should_Be_Set_For_DataTemplate_Generated_Items()
        {
            using var app = Start();

            var target = new ListBox { Items = new[] { "Foo", "Bar", "Baz " } };

            Prepare(target);

            Assert.Equal(3, target.GetLogicalChildren().Count());

            foreach (var child in target.GetLogicalChildren())
            {
                Assert.IsType<ListBoxItem>(child);
            }
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
                new ListBoxItem { Content = "Qux" },
            };

            var target = new ListBox
            {
                DataContext = "Base",
                DataTemplates =
                {
                    new FuncDataTemplate<Item>((x, _) => new Button { Content = x })
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
        public void Selection_Should_Be_Cleared_On_Recycled_Items()
        {
            using var app = Start();

            var target = new ListBox
            {
                Items = Enumerable.Range(0, 20).Select(x => $"Item {x}").ToList(),
                ItemTemplate = new FuncDataTemplate<string>((x, _) => new TextBlock { Height = 10 }),
                SelectedIndex = 0,
            };

            Prepare(target);

            // Make sure we're virtualized and first item is selected.
            Assert.Equal(11, target.Presenter.RealizedElements.Count());
            Assert.True(((ListBoxItem)target.Presenter.RealizedElements.First()).IsSelected);

            // Scroll down a page.
            target.Scroll.Offset = new Vector(0, 100);
            Layout(target);

            // Make sure recycled item isn't now selected.
            Assert.False(((ListBoxItem)target.Presenter.RealizedElements.First()).IsSelected);
        }

        [Fact]
        public void ScrollViewer_Should_Have_Correct_Extent_And_Viewport()
        {
            using var app = Start();

            var target = new ListBox
            {
                Items = Enumerable.Range(0, 20).Select(x => $"Item {x}").ToList(),
                ItemTemplate = new FuncDataTemplate<string>((x, _) => new TextBlock { Width = 20, Height = 10 }),
                SelectedIndex = 0,
            };

            Prepare(target);

            Assert.Equal(new Size(100, 200), target.Scroll.Extent);
            Assert.Equal(new Size(100, 100), target.Scroll.Viewport);
        }

        [Fact]
        public void Containers_Correct_After_Clear_Add_Remove()
        {
            using var app = Start();

            // Issue #1936
            var items = new AvaloniaList<string>(Enumerable.Range(0, 11).Select(x => $"Item {x}"));
            var target = new ListBox
            {
                Items = items,
                ItemTemplate = new FuncDataTemplate<string>((x, _) => new TextBlock { Width = 20, Height = 10 }),
                SelectedIndex = 0,
            };

            Prepare(target);

            items.Clear();
            items.AddRange(Enumerable.Range(0, 11).Select(x => $"Item {x}"));
            items.Remove("Item 2");

            Layout(target);

            Assert.Equal(
                items,
                target.Presenter.RealizedElements.Cast<ListBoxItem>().Select(x => (string)x.Content));
        }

        [Fact]
        public void Toggle_Selection_Should_Update_Containers()
        {
            using var app = Start();

            var items = Enumerable.Range(0, 10).Select(x => $"Item {x}").ToArray();
            var target = new ListBox
            {
                Items = items,
                SelectionMode = SelectionMode.Toggle,
                ItemTemplate = new FuncDataTemplate<string>((x, _) => new TextBlock { Height = 10 })
            };

            Prepare(target);

            var lbItems = target.GetLogicalChildren().OfType<ListBoxItem>().ToArray();

            var item = lbItems[0];

            Assert.Equal(false, item.IsSelected);

            RaisePressedEvent(target, item, MouseButton.Left);

            Assert.Equal(true, item.IsSelected);

            RaisePressedEvent(target, item, MouseButton.Left);

            Assert.Equal(false, item.IsSelected);
        }

        [Fact]
        public void Can_Decrease_Number_Of_Materialized_Items_By_Removing_From_Source_Collection()
        {
            using var app = Start();

            var items = new AvaloniaList<string>(Enumerable.Range(0, 20).Select(x => $"Item {x}"));
            var target = new ListBox
            {
                Items = items,
                ItemTemplate = new FuncDataTemplate<string>((x, _) => new TextBlock { Height = 10 })
            };

            Prepare(target);
            target.Scroll.Offset = new Vector(0, 1);

            items.RemoveRange(0, 11);
        }

        private void RaisePressedEvent(ListBox listBox, ListBoxItem item, MouseButton mouseButton)
        {
            _mouse.Click(listBox, item, mouseButton);
        }

        [Fact]
        public void LayoutManager_Should_Measure_Arrange_All()
        {
            using var app = Start();

            var items = new AvaloniaList<string>(Enumerable.Range(1, 7).Select(v => v.ToString()));
            var wnd = new Window() { SizeToContent = SizeToContent.WidthAndHeight, IsVisible = true };
            var target = new ListBox();

            wnd.Content = target;

            var lm = wnd.LayoutManager;

            target.Height = 110;
            target.Width = 50;
            target.DataContext = items;

            target.ItemTemplate = new FuncDataTemplate<object>((c, _) =>
            {
                var tb = new TextBlock() { Height = 10, Width = 30 };
                tb.Bind(TextBlock.TextProperty, new Data.Binding());
                return tb;
            }, true);

            lm.ExecuteInitialLayoutPass();

            target.Items = items;

            lm.ExecuteLayoutPass();

            items.Insert(3, "3+");
            lm.ExecuteLayoutPass();

            items.Insert(4, "4+");
            lm.ExecuteLayoutPass();

            //RESET
            items.Clear();
            foreach (var i in Enumerable.Range(1, 7))
            {
                items.Add(i.ToString());
            }

            //working bit better with this line no outof memory or remaining to arrange/measure ???
            //lm.ExecuteLayoutPass();

            items.Insert(2, "2+");

            lm.ExecuteLayoutPass();
            //after few more layout cycles layoutmanager shouldn't hold any more visual for measure/arrange
            lm.ExecuteLayoutPass();
            lm.ExecuteLayoutPass();

            var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
            var toMeasure = lm.GetType().GetField("_toMeasure", flags).GetValue(lm) as System.Collections.Generic.IEnumerable<Layout.ILayoutable>;
            var toArrange = lm.GetType().GetField("_toArrange", flags).GetValue(lm) as System.Collections.Generic.IEnumerable<Layout.ILayoutable>;

            Assert.Equal(0, toMeasure.Count());
            Assert.Equal(0, toArrange.Count());
        }

        [Fact]
        public void Clicking_Item_Should_Raise_BringIntoView_For_Correct_Control()
        {
            using var app = Start();

            // Issue #3934
            var items = Enumerable.Range(0, 10).Select(x => $"Item {x}").ToArray();
            var target = new ListBox
            {
                Items = items,
                ItemTemplate = new FuncDataTemplate<string>((x, _) => new TextBlock { Height = 10 }),
                SelectionMode = SelectionMode.AlwaysSelected,
                Layout = new NonVirtualizingStackLayout(),
            };

            Prepare(target);

            // First an item that is not index 0 must be selected.
            _mouse.Click(target.Presenter.RealizedElements.ElementAt(1));
            Assert.Equal(1, target.Selection.AnchorIndex);

            // We're going to be clicking on item 9.
            var item = (ListBoxItem)target.Presenter.RealizedElements.ElementAt(9);
            var raised = 0;

            // Make sure a RequestBringIntoView event is raised for item 9. It won't be handled
            // by the ScrollContentPresenter as the item is already visible, so we don't need
            // handledEventsToo: true. Issue #3934 failed here because item 0 was being scrolled
            // into view due to SelectionMode.AlwaysSelected.
            target.AddHandler(Control.RequestBringIntoViewEvent, (s, e) =>
            {
                Assert.Same(item, e.TargetObject);
                ++raised;
            });

            // Click item 9.
            _mouse.Click(item);

            Assert.Equal(2, raised);
        }

        [Fact]
        public void Adding_And_Selecting_Item_With_AutoScrollToSelectedItem_Should_NotHide_FirstItem()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var items = new AvaloniaList<string>();

                var wnd = new Window() { Width = 100, Height = 100, IsVisible = true };

                var target = new ListBox()
                {
                    VerticalAlignment = VerticalAlignment.Top,
                    AutoScrollToSelectedItem = true,
                    Width = 50,
                    ItemTemplate = new FuncDataTemplate<object>((c, _) => new Border() { Height = 10 }),
                    Items = items,
                };
                wnd.Content = target;

                var lm = wnd.LayoutManager;

                lm.ExecuteInitialLayoutPass();

                items.Add("Item 1");
                target.Selection.Select(0);
                lm.ExecuteLayoutPass();

                Assert.Equal(1, target.Presenter.RealizedElements.Count());

                items.Add("Item 2");
                target.Selection.Select(1);
                lm.ExecuteLayoutPass();

                Assert.Equal(2, target.Presenter.RealizedElements.Count());

                //make sure we have enough space to show all items
                Assert.True(target.Presenter.Bounds.Height >= target.Presenter.RealizedElements.Sum(c => c.Bounds.Height));

                //make sure we show items and they completelly visible, not only partially
                var e0 = target.Presenter.RealizedElements.ElementAt(0);
                var e1 = target.Presenter.RealizedElements.ElementAt(1);
                Assert.True(e0.Bounds.Top >= 0 && e0.Bounds.Bottom <= target.Presenter.Bounds.Height, "First item is not completely visible.");
                Assert.True(e1.Bounds.Top >= 0 && e1.Bounds.Bottom <= target.Presenter.Bounds.Height, "Second item is not completely visible.");
            }
        }

        private static IDisposable Start()
        {
            var services = TestServices.MockPlatformRenderInterface.With(
                focusManager: new FocusManager(),
                keyboardDevice: () => new KeyboardDevice(),
                styler: new Styler(),
                windowingPlatform: new MockWindowingPlatform());
            return UnitTestApplication.Start(services);
        }

        private static void Prepare(ListBox target)
        {
            var root = new TestRoot
            {
                Child = target,
                Width = 100,
                Height = 100,
                Styles =
                {
                    new Style(x => x.OfType<ListBox>())
                    {
                        Setters =
                        {
                            new Setter(ListBox.TemplateProperty, ListBoxTemplate()),
                        },
                    },
                    new Style(x => x.OfType<ListBoxItem>())
                    {
                        Setters =
                        {
                            new Setter(ListBoxItem.TemplateProperty, ListBoxItemTemplate()),
                        },
                    },
                },
            };

            root.LayoutManager.ExecuteInitialLayoutPass();
        }

        private void Layout(ListBox target)
        {
            var root = (TestRoot)target.GetVisualRoot();
            root.LayoutManager.ExecuteLayoutPass();
        }

        private void KeyDown(IControl target, Key key, KeyModifiers modifiers = KeyModifiers.None)
        {
            target.RaiseEvent(new KeyEventArgs
            {
                RoutedEvent = InputElement.KeyDownEvent,
                Key = key,
                KeyModifiers = modifiers,
            });
        }

        private static FuncControlTemplate ListBoxTemplate()
        {
            return new FuncControlTemplate<ListBox>((parent, scope) =>
                new ScrollViewer
                {
                    Name = "PART_ScrollViewer",
                    Template = ScrollViewerTemplate(),
                    Content = new ItemsPresenter
                    {
                        Name = "PART_ItemsPresenter",
                        HorizontalCacheLength = 0,
                        VerticalCacheLength = 0,
                        [~ItemsPresenter.ItemsViewProperty] = parent.GetObservable(ItemsControl.ItemsViewProperty).ToBinding(),
                        [~ItemsPresenter.LayoutProperty] = parent.GetObservable(ItemsControl.LayoutProperty).ToBinding(),
                    }.RegisterInNameScope(scope)
                }.RegisterInNameScope(scope));
        }

        private static FuncControlTemplate ListBoxItemTemplate()
        {
            return new FuncControlTemplate<ListBoxItem>((parent, scope) =>
                new ContentPresenter
                {
                    Name = "PART_ContentPresenter",
                    [!ContentPresenter.ContentProperty] = parent[!ListBoxItem.ContentProperty],
                    [!ContentPresenter.ContentTemplateProperty] = parent[!ListBoxItem.ContentTemplateProperty],
                }.RegisterInNameScope(scope));
        }

        private static FuncControlTemplate ScrollViewerTemplate()
        {
            return new FuncControlTemplate<ScrollViewer>((parent, scope) =>
                new Panel
                {
                    Children =
                    {
                        new ScrollContentPresenter
                        {
                            Name = "PART_ContentPresenter",
                            [~ScrollContentPresenter.CanHorizontallyScrollProperty] = parent.GetObservable(ScrollViewer.CanHorizontallyScrollProperty).ToBinding(),
                            [~ScrollContentPresenter.CanVerticallyScrollProperty] = parent.GetObservable(ScrollViewer.CanVerticallyScrollProperty).ToBinding(),
                            [~ScrollContentPresenter.ContentProperty] = parent.GetObservable(ScrollViewer.ContentProperty).ToBinding(),
                            [~~ScrollContentPresenter.ExtentProperty] = parent[~~ScrollViewer.ExtentProperty],
                            [~~ScrollContentPresenter.OffsetProperty] = parent[~~ScrollViewer.OffsetProperty],
                            [~~ScrollContentPresenter.ViewportProperty] = parent[~~ScrollViewer.ViewportProperty],
                        }.RegisterInNameScope(scope),
                        new ScrollBar
                        {
                            Name = "verticalScrollBar",
                            [~ScrollBar.MaximumProperty] = parent[~ScrollViewer.VerticalScrollBarMaximumProperty],
                            [~~ScrollBar.ValueProperty] = parent[~~ScrollViewer.VerticalScrollBarValueProperty],
                        }
                    }
                });
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
