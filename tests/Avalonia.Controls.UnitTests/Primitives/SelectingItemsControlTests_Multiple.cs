using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Avalonia.Collections;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Selection;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Xunit;

namespace Avalonia.Controls.UnitTests.Primitives
{
    public partial class SelectingItemsControlTests
    {
        [Fact]
        public void Setting_SelectedIndex_Should_Add_To_SelectedItems()
        {
            var target = new TestSelector(SelectionMode.Multiple)
            {
                Items = new[] { "foo", "bar" },
            };

            target.SelectedIndex = 1;

            Assert.Equal(new[] { "bar" }, target.SelectedItems.Cast<object>().ToList());
        }

        [Fact]
        public void Adding_SelectedItems_Should_Set_SelectedIndex()
        {
            var target = new TestSelector(SelectionMode.Multiple)
            {
                Items = new[] { "foo", "bar" },
            };

            target.SelectedItems.Add("bar");

            Assert.Equal(1, target.SelectedIndex);
        }

        [Fact]
        public void Assigning_Single_SelectedItems_Should_Set_SelectedIndex()
        {
            using var app = Start();

            var target = new TestSelector(SelectionMode.Multiple)
            {
                Items = new[] { "foo", "bar" },
            };

            Prepare(target);
            target.SelectedItems = new AvaloniaList<object>("bar");

            Assert.Equal(1, target.SelectedIndex);
            Assert.Equal(new[] { "bar" }, target.SelectedItems);
            Assert.Equal(new[] { 1 }, SelectedContainers(target));
        }

        [Fact]
        public void Assigning_Multiple_SelectedItems_Should_Set_SelectedIndex()
        {
            using var app = Start();

            var target = new TestSelector(SelectionMode.Multiple)
            {
                Items = new[] { "foo", "bar", "baz" },
            };

            Prepare(target);
            target.SelectedItems = new AvaloniaList<string>("foo", "bar", "baz");

            Assert.Equal(0, target.SelectedIndex);
            Assert.Equal(new[] { "foo", "bar", "baz" }, target.SelectedItems);
            Assert.Equal(new[] { 0, 1, 2 }, SelectedContainers(target));
        }

        [Fact]
        public void Reassigning_SelectedItems_Should_Clear_Selection()
        {
            var target = new TestSelector(SelectionMode.Multiple)
            {
                Items = new[] { "foo", "bar" },
            };

            target.SelectedItems.Add("bar");
            target.SelectedItems = new AvaloniaList<object>();

            Assert.Equal(-1, target.SelectedIndex);
            Assert.Null(target.SelectedItem);
        }

        [Fact]
        public void Adding_First_SelectedItem_Should_Raise_SelectedIndex_SelectedItem_Changed()
        {
            var target = new TestSelector(SelectionMode.Multiple)
            {
                Items = new[] { "foo", "bar" },
            };

            bool indexRaised = false;
            bool itemRaised = false;
            target.PropertyChanged += (s, e) =>
            {
                indexRaised |= e.Property.Name == "SelectedIndex" &&
                    (int)e.OldValue == -1 &&
                    (int)e.NewValue == 1;
                itemRaised |= e.Property.Name == "SelectedItem" &&
                    (string)e.OldValue == null &&
                    (string)e.NewValue == "bar";
            };

            target.SelectedItems.Add("bar");

            Assert.True(indexRaised);
            Assert.True(itemRaised);
        }

        [Fact]
        public void Adding_Subsequent_SelectedItems_Should_Not_Raise_SelectedIndex_SelectedItem_Changed()
        {
            var target = new TestSelector(SelectionMode.Multiple)
            {
                Items = new[] { "foo", "bar" },
            };

            target.SelectedItems.Add("foo");

            bool raised = false;
            target.PropertyChanged += (s, e) => 
                raised |= e.Property.Name == "SelectedIndex" ||
                          e.Property.Name == "SelectedItem";

            target.SelectedItems.Add("bar");

            Assert.False(raised);
        }

        [Fact]
        public void Removing_Last_SelectedItem_Should_Raise_SelectedIndex_Changed()
        {
            var target = new TestSelector
            {
                Items = new[] { "foo", "bar" },
            };

            target.SelectedItems.Add("foo");

            bool raised = false;
            target.PropertyChanged += (s, e) => 
                raised |= e.Property.Name == "SelectedIndex" && 
                          (int)e.OldValue == 0 && 
                          (int)e.NewValue == -1;

            target.SelectedItems.RemoveAt(0);

            Assert.True(raised);
        }

        [Fact]
        public void Adding_SelectedItems_Should_Set_Item_IsSelected()
        {
            using var app = Start();

            var items = new[]
            {
                new ListBoxItem(),
                new ListBoxItem(),
                new ListBoxItem(),
            };

            var target = new TestSelector(SelectionMode.Multiple)
            {
                Items = items,
            };

            Prepare(target);
            target.SelectedItems.Add(items[0]);
            target.SelectedItems.Add(items[1]);

            var foo = target.Presenter.RealizedElements.First();

            Assert.True(items[0].IsSelected);
            Assert.True(items[1].IsSelected);
            Assert.False(items[2].IsSelected);
        }

        [Fact]
        public void Assigning_SelectedItems_Should_Set_Item_IsSelected()
        {
            using var app = Start();

            var items = new[]
            {
                new ListBoxItem(),
                new ListBoxItem(),
                new ListBoxItem(),
            };

            var target = new TestSelector(SelectionMode.Multiple)
            {
                Items = items,
            };

            Prepare(target);
            target.SelectedItems = new AvaloniaList<object> { items[0], items[1] };

            Assert.True(items[0].IsSelected);
            Assert.True(items[1].IsSelected);
            Assert.False(items[2].IsSelected);
        }

        [Fact]
        public void Removing_SelectedItems_Should_Clear_Item_IsSelected()
        {
            using var app = Start();

            var items = new[]
            {
                new ListBoxItem(),
                new ListBoxItem(),
                new ListBoxItem(),
            };

            var target = new TestSelector(SelectionMode.Multiple)
            {
                Items = items,
            };

            Prepare(target);
            target.SelectedItems.Add(items[0]);
            target.SelectedItems.Add(items[1]);
            target.SelectedItems.Remove(items[1]);

            Assert.True(items[0].IsSelected);
            Assert.False(items[1].IsSelected);
        }

        [Fact]
        public void Reassigning_SelectedItems_Should_Clear_Item_IsSelected()
        {
            var items = new[]
            {
                new ListBoxItem(),
                new ListBoxItem(),
                new ListBoxItem(),
            };

            var target = new TestSelector(SelectionMode.Multiple)
            {
                Items = items,
            };

            target.SelectedItems.Add(items[0]);
            target.SelectedItems.Add(items[1]);

            target.SelectedItems = new AvaloniaList<object> { items[0], items[1] };

            Assert.False(items[0].IsSelected);
            Assert.False(items[1].IsSelected);
        }

        [Fact]
        public void Setting_SelectedIndex_Should_Unmark_Previously_Selected_Containers()
        {
            using var app = Start();

            var target = new TestSelector(SelectionMode.Multiple)
            {
                Items = new[] { "foo", "bar", "baz" },
            };

            Prepare(target);

            target.SelectedItems.Add("foo");
            target.SelectedItems.Add("bar");

            Assert.Equal(new[] { 0, 1 }, SelectedContainers(target));

            target.SelectedIndex = 2;

            Assert.Equal(new[] { 2 }, SelectedContainers(target));
        }

        [Fact]
        public void Range_Select_Should_Select_Range()
        {
            var target = new TestSelector(SelectionMode.Multiple)
            {
                Items = new[]
                {
                    "foo",
                    "bar",
                    "baz",
                    "qux",
                    "qiz",
                    "lol",
                },
            };

            target.SelectedIndex = 1;
            target.SelectRange(3);

            Assert.Equal(new[] { "bar", "baz", "qux" }, target.SelectedItems.Cast<object>().ToList());
        }

        [Fact]
        public void Range_Select_Backwards_Should_Select_Range()
        {
            var target = new TestSelector(SelectionMode.Multiple)
            {
                Items = new[]
                {
                    "foo",
                    "bar",
                    "baz",
                    "qux",
                    "qiz",
                    "lol",
                },
                SelectionMode = SelectionMode.Multiple,
            };

            target.SelectedIndex = 3;
            target.SelectRange(1);

            Assert.Equal(new[] { "qux", "bar", "baz" }, target.SelectedItems.Cast<object>().ToList());
        }

        [Fact]
        public void Second_Range_Select_Backwards_Should_Select_From_Original_Selection()
        {
            var target = new TestSelector(SelectionMode.Multiple)
            {
                Items = new[]
                {
                    "foo",
                    "bar",
                    "baz",
                    "qux",
                    "qiz",
                    "lol",
                },
                SelectionMode = SelectionMode.Multiple,
            };

            target.SelectedIndex = 2;
            target.SelectRange(5);
            target.SelectRange(4);

            Assert.Equal(new[] { "baz", "qux", "qiz" }, target.SelectedItems.Cast<object>().ToList());
        }

        [Fact]
        public void Setting_SelectedIndex_After_Range_Should_Unmark_Previously_Selected_Containers()
        {
            using var app = Start();

            var target = new TestSelector(SelectionMode.Multiple)
            {
                Items = new[] { "foo", "bar", "baz", "qux" },
                SelectedIndex = 0,
                SelectionMode = SelectionMode.Multiple,
            };

            Prepare(target);

            target.SelectRange(2);

            Assert.Equal(new[] { 0, 1, 2 }, SelectedContainers(target));

            target.SelectedIndex = 3;

            Assert.Equal(new[] { 3 }, SelectedContainers(target));
        }

        [Fact]
        public void Toggling_Selection_After_Range_Should_Work()
        {
            using var app = Start();

            var target = new TestSelector(SelectionMode.Multiple)
            {
                Items = new[] { "foo", "bar", "baz", "foo", "bar", "baz" },
                SelectedIndex = 0,
                SelectionMode = SelectionMode.Multiple,
            };

            Prepare(target);

            target.SelectRange(3);

            Assert.Equal(new[] { 0, 1, 2, 3 }, SelectedContainers(target));

            target.Toggle(4);

            Assert.Equal(new[] { 0, 1, 2, 3, 4 }, SelectedContainers(target));
        }

        [Fact]
        public void Suprious_SelectedIndex_Changes_Should_Not_Be_Triggered()
        {
            var target = new TestSelector(SelectionMode.Multiple)
            {
                Items = new[] { "foo", "bar", "baz" },
            };

            var selectedIndexes = new List<int>();
            target.GetObservable(TestSelector.SelectedIndexProperty).Subscribe(x => selectedIndexes.Add(x));

            target.SelectedItems = new AvaloniaList<object> { "bar", "baz" };
            target.SelectedItem = "foo";

            Assert.Equal(0, target.SelectedIndex);
            Assert.Equal(new[] { -1, 1, 0 }, selectedIndexes);
        }

        [Fact]
        public void Can_Set_SelectedIndex_To_Another_Selected_Item()
        {
            using var app = Start();

            var target = new TestSelector(SelectionMode.Multiple)
            {
                Items = new[] { "foo", "bar", "baz" },
            };

            Prepare(target);
            target.SelectedItems.Add("foo");
            target.SelectedItems.Add("bar");

            Assert.Equal(0, target.SelectedIndex);
            Assert.Equal(new[] { "foo", "bar" }, target.SelectedItems);
            Assert.Equal(new[] { 0, 1 }, SelectedContainers(target));

            var raised = false;
            target.SelectionChanged += (s, e) =>
            {
                raised = true;
                Assert.Empty(e.AddedItems);
                Assert.Equal(new[] { "foo" }, e.RemovedItems);
            };

            target.SelectedIndex = 1;

            Assert.True(raised);
            Assert.Equal(1, target.SelectedIndex);
            Assert.Equal(new[] { "bar" }, target.SelectedItems);
            Assert.Equal(new[] { 1 }, SelectedContainers(target));
        }

        /// <summary>
        /// Tests a problem discovered with ListBox with selection.
        /// </summary>
        /// <remarks>
        /// - Items is bound to DataContext first, followed by say SelectedIndex
        /// - When the ListBox is removed from the visual tree, DataContext becomes null (as it's
        ///   inherited)
        /// - This changes Items to null, which changes SelectedIndex to null as there are no
        ///   longer any items
        /// - However, the news that DataContext is now null hasn't yet reached the SelectedItems
        ///   binding and so the unselection is sent back to the ViewModel
        /// 
        /// This is a similar problem to that tested by XamlBindingTest.Should_Not_Write_To_Old_DataContext.
        /// However, that tests a general property binding problem: here we are writing directly
        /// to the SelectedItems collection - not via a binding - so it's something that the 
        /// binding system cannot solve. Instead we solve it by not clearing SelectedItems when
        /// DataContext is in the process of changing.
        /// </remarks>
        [Fact]
        public void Should_Not_Write_SelectedItems_To_Old_DataContext()
        {
            var vm = new OldDataContextViewModel();
            var target = new TestSelector(SelectionMode.Multiple);

            var itemsBinding = new Binding
            {
                Path = "Items",
                Mode = BindingMode.OneWay,
            };

            var selectedItemsBinding = new Binding
            {
                Path = "SelectedItems",
                Mode = BindingMode.OneWay,
            };

            // Bind Items and SelectedItems to the VM.
            target.Bind(TestSelector.ItemsProperty, itemsBinding);
            target.Bind(TestSelector.SelectedItemsProperty, selectedItemsBinding);

            // Set DataContext and SelectedIndex
            target.DataContext = vm;
            target.SelectedIndex = 1;

            // Make sure SelectedItems are written back to VM.
            Assert.Equal(new[] { "bar" }, vm.SelectedItems);

            // Clear DataContext and ensure that SelectedItems is still set in the VM.
            target.DataContext = null;
            Assert.Equal(new[] { "bar" }, vm.SelectedItems);

            // Ensure target's SelectedItems is now clear.
            Assert.Empty(target.SelectedItems);
        }

        /// <summary>
        /// See <see cref="Should_Not_Write_SelectedItems_To_Old_DataContext"/>.
        /// </summary>
        [Fact]
        public void Should_Not_Write_SelectionModel_To_Old_DataContext()
        {
            var vm = new OldDataContextViewModel();
            var target = new TestSelector();

            var itemsBinding = new Binding
            {
                Path = "Items",
                Mode = BindingMode.OneWay,
            };

            var selectionBinding = new Binding
            {
                Path = "Selection",
                Mode = BindingMode.OneWay,
            };

            // Bind Items and Selection to the VM.
            target.Bind(TestSelector.ItemsProperty, itemsBinding);
            target.Bind(TestSelector.SelectionProperty, selectionBinding);

            // Set DataContext and SelectedIndex
            target.DataContext = vm;
            target.SelectedIndex = 1;

            // Make sure selection is written to selection model
            Assert.Equal(1, vm.Selection.SelectedIndex);

            // Clear DataContext and ensure that selection is still set in model.
            target.DataContext = null;
            Assert.Equal(1, vm.Selection.SelectedIndex);

            // Ensure target's SelectedItems is now clear.
            Assert.Empty(target.SelectedItems);
        }

        [Fact]
        public void Unbound_SelectedItems_Should_Be_Cleared_When_DataContext_Cleared()
        {
            var data = new
            {
                Items = new[] { "foo", "bar", "baz" },
            };

            var target = new TestSelector(SelectionMode.Multiple)
            {
                DataContext = data,
            };

            var itemsBinding = new Binding { Path = "Items" };
            target.Bind(TestSelector.ItemsProperty, itemsBinding);

            Assert.Same(data.Items, target.Items);

            target.SelectedItems.Add("bar");
            target.DataContext = null;

            Assert.Empty(target.SelectedItems);
        }

        [Fact]
        public void Adding_To_SelectedItems_Should_Raise_SelectionChanged()
        {
            var items = new[] { "foo", "bar", "baz" };

            var target = new TestSelector(SelectionMode.Multiple)
            {
                DataContext = items,
                Items = items,
            };

            var called = false;

            target.SelectionChanged += (s, e) =>
            {
                Assert.Equal(new[] { "bar" }, e.AddedItems.Cast<object>().ToList());
                Assert.Empty(e.RemovedItems);
                called = true;
            };

            target.SelectedItems.Add("bar");

            Assert.True(called);
        }

        [Fact]
        public void Removing_From_SelectedItems_Should_Raise_SelectionChanged()
        {
            var items = new[] { "foo", "bar", "baz" };

            var target = new TestSelector(SelectionMode.Multiple)
            {
                Items = items,
                SelectedItem = "bar",
            };

            var called = false;

            target.SelectionChanged += (s, e) =>
            {
                Assert.Equal(new[] { "bar" }, e.RemovedItems.Cast<object>().ToList());
                Assert.Empty(e.AddedItems);
                called = true;
            };

            target.SelectedItems.Remove("bar");

            Assert.True(called);
        }

        [Fact]
        public void Assigning_SelectedItems_Should_Raise_SelectionChanged()
        {
            var items = new[] { "foo", "bar", "baz" };

            var target = new TestSelector(SelectionMode.Multiple)
            {
                Items = items,
                SelectedItem = "bar",
            };

            var called = false;

            Prepare(target);

            target.SelectionChanged += (s, e) =>
            {
                Assert.Equal(new[] { "foo", "baz" }, e.AddedItems.Cast<object>());
                Assert.Equal(new[] { "bar" }, e.RemovedItems.Cast<object>());
                called = true;
            };

            target.SelectedItems = new AvaloniaList<object>("foo", "baz");

            Assert.True(called);
        }

        [Fact]
        public void Shift_Selecting_From_No_Selection_Selects_From_Start()
        {
            using var app = Start();

            var target = new ListBox
            {
                Items = new[] { "Foo", "Bar", "Baz" },
                SelectionMode = SelectionMode.Multiple,
            };

            Prepare(target);

            var selected = (Interactive)target.Presenter.RealizedElements.ElementAt(2);
            _helper.Click(selected, modifiers: KeyModifiers.Shift);

            Assert.Equal(new[] { "Foo", "Bar", "Baz" }, target.SelectedItems);
            Assert.Equal(new[] { 0, 1, 2 }, SelectedContainers(target));
        }

        [Fact]
        public void Ctrl_Selecting_Raises_SelectionChanged_Events()
        {
            using var app = Start();

            var target = new ListBox
            {
                Items = new[] { "Foo", "Bar", "Baz", "Qux" },
                SelectionMode = SelectionMode.Multiple,
            };

            Prepare(target);

            SelectionChangedEventArgs receivedArgs = null;

            target.SelectionChanged += (_, args) => receivedArgs = args;

            void VerifyAdded(string selection)
            {
                Assert.NotNull(receivedArgs);
                Assert.Equal(new[] { selection }, receivedArgs.AddedItems);
                Assert.Empty(receivedArgs.RemovedItems);
            }

            void VerifyRemoved(string selection)
            {
                Assert.NotNull(receivedArgs);
                Assert.Equal(new[] { selection }, receivedArgs.RemovedItems);
                Assert.Empty(receivedArgs.AddedItems);
            }

            ClickContainer(target, 1);

            VerifyAdded("Bar");

            receivedArgs = null;
            ClickContainer(target, 2, KeyModifiers.Control);

            VerifyAdded("Baz");

            receivedArgs = null;
            ClickContainer(target, 3, KeyModifiers.Control);

            VerifyAdded("Qux");

            receivedArgs = null;
            ClickContainer(target, 1, KeyModifiers.Control);

            VerifyRemoved("Bar");
        }

        [Fact]
        public void Ctrl_Selecting_SelectedItem_With_Multiple_Selection_Active_Sets_SelectedItem_To_Next_Selection()
        {
            using var app = Start();

            var target = new ListBox
            {
                Items = new[] { "Foo", "Bar", "Baz", "Qux" },
                SelectionMode = SelectionMode.Multiple,
            };

            Prepare(target);
            ClickContainer(target, 1);
            ClickContainer(target, 2, KeyModifiers.Control);
            ClickContainer(target, 3, KeyModifiers.Control);

            Assert.Equal(1, target.SelectedIndex);
            Assert.Equal("Bar", target.SelectedItem);
            Assert.Equal(new[] { "Bar", "Baz", "Qux" }, target.SelectedItems);

            _helper.Click((Interactive)target.Presenter.RealizedElements.ElementAt(1), modifiers: KeyModifiers.Control);

            Assert.Equal(2, target.SelectedIndex);
            Assert.Equal("Baz", target.SelectedItem);
            Assert.Equal(new[] { "Baz", "Qux" }, target.SelectedItems);
        }

        [Fact]
        public void Ctrl_Selecting_Non_SelectedItem_With_Multiple_Selection_Active_Leaves_SelectedItem_The_Same()
        {
            using var app = Start();

            var target = new ListBox
            {
                Items = new[] { "Foo", "Bar", "Baz" },
                SelectionMode = SelectionMode.Multiple,
            };

            Prepare(target);

            ClickContainer(target, 1);
            ClickContainer(target, 2, KeyModifiers.Control);

            Assert.Equal(1, target.SelectedIndex);
            Assert.Equal("Bar", target.SelectedItem);

            ClickContainer(target, 2, KeyModifiers.Control);

            Assert.Equal(1, target.SelectedIndex);
            Assert.Equal("Bar", target.SelectedItem);
        }

        [Fact]
        public void Should_Ctrl_Select_Correct_Item_When_Duplicate_Items_Are_Present()
        {
            using var app = Start();

            var target = new ListBox
            {
                Items = new[] { "Foo", "Bar", "Baz", "Foo", "Bar", "Baz" },
                SelectionMode = SelectionMode.Multiple,
            };

            Prepare(target);

            ClickContainer(target, 3);
            ClickContainer(target, 4, KeyModifiers.Control);

            Assert.Equal(new[] { "Foo", "Bar" }, target.SelectedItems);
            Assert.Equal(new[] { 3, 4 }, SelectedContainers(target));
        }

        [Fact]
        public void Should_Shift_Select_Correct_Item_When_Duplicates_Are_Present()
        {
            using var app = Start();

            var target = new ListBox
            {
                Items = new[] { "Foo", "Bar", "Baz", "Foo", "Bar", "Baz" },
                SelectionMode = SelectionMode.Multiple,
            };

            Prepare(target);

            ClickContainer(target, 3);
            ClickContainer(target, 5, KeyModifiers.Shift);

            Assert.Equal(new[] { "Foo", "Bar", "Baz" }, target.SelectedItems);
            Assert.Equal(new[] { 3, 4, 5 }, SelectedContainers(target));
        }

        [Fact]
        public void Can_Shift_Select_All_Items_When_Duplicates_Are_Present()
        {
            using var app = Start();

            var target = new ListBox
            {
                Items = new[] { "Foo", "Bar", "Baz", "Foo", "Bar", "Baz" },
                SelectionMode = SelectionMode.Multiple,
            };

            Prepare(target);
            ClickContainer(target, 0);
            ClickContainer(target, 5, KeyModifiers.Shift);

            Assert.Equal(new[] { "Foo", "Bar", "Baz", "Foo", "Bar", "Baz" }, target.SelectedItems);
            Assert.Equal(new[] { 0, 1, 2, 3, 4, 5 }, SelectedContainers(target));
        }

        [Fact]
        public void Shift_Selecting_Raises_SelectionChanged_Events()
        {
            using var app = Start();

            var target = new ListBox
            {
                Items = new[] { "Foo", "Bar", "Baz", "Qux" },
                SelectionMode = SelectionMode.Multiple,
            };

            Prepare(target);

            SelectionChangedEventArgs receivedArgs = null;

            target.SelectionChanged += (_, args) => receivedArgs = args;

            void VerifyAdded(params string[] selection)
            {
                Assert.NotNull(receivedArgs);
                Assert.Equal(selection, receivedArgs.AddedItems);
                Assert.Empty(receivedArgs.RemovedItems);
            }

            void VerifyRemoved(string selection)
            {
                Assert.NotNull(receivedArgs);
                Assert.Equal(new[] { selection }, receivedArgs.RemovedItems);
                Assert.Empty(receivedArgs.AddedItems);
            }

            ClickContainer(target, 1);

            VerifyAdded("Bar");

            receivedArgs = null;
            ClickContainer(target, 3, KeyModifiers.Shift);

            VerifyAdded("Baz", "Qux");

            receivedArgs = null;
            ClickContainer(target, 2, KeyModifiers.Shift);

            VerifyRemoved("Qux");
        }

        [Fact]
        public void Duplicate_Items_Are_Added_To_SelectedItems_In_Order()
        {
            using var app = Start();

            var target = new ListBox
            {
                Items = new[] { "Foo", "Bar", "Baz", "Foo", "Bar", "Baz" },
                SelectionMode = SelectionMode.Multiple,
            };

            Prepare(target);
            ClickContainer(target, 0);

            Assert.Equal(new[] { "Foo" }, target.SelectedItems);

            ClickContainer(target, 4, KeyModifiers.Control);

            Assert.Equal(new[] { "Foo", "Bar" }, target.SelectedItems);

            ClickContainer(target, 3, KeyModifiers.Control);

            Assert.Equal(new[] { "Foo", "Bar", "Foo" }, target.SelectedItems);

            ClickContainer(target, 1, KeyModifiers.Control);

            Assert.Equal(new[] { "Foo", "Bar", "Foo", "Bar" }, target.SelectedItems);
        }

        [Fact]
        public void SelectAll_Sets_SelectedIndex_And_SelectedItem()
        {
            var target = new TestSelector(SelectionMode.Multiple)
            {
                Items = new[] { "Foo", "Bar", "Baz" },
                SelectionMode = SelectionMode.Multiple,
            };

            target.Selection.SelectAll();

            Assert.Equal(0, target.SelectedIndex);
            Assert.Equal("Foo", target.SelectedItem);
        }

        [Fact]
        public void SelectAll_Raises_SelectionChanged_Event()
        {
            var target = new TestSelector(SelectionMode.Multiple)
            {
                Items = new[] { "Foo", "Bar", "Baz" },
                SelectionMode = SelectionMode.Multiple,
            };

            SelectionChangedEventArgs receivedArgs = null;

            target.SelectionChanged += (_, args) => receivedArgs = args;

            target.Selection.SelectAll();

            Assert.NotNull(receivedArgs);
            Assert.Equal(target.Items, receivedArgs.AddedItems);
            Assert.Empty(receivedArgs.RemovedItems);
        }

        [Fact]
        public void UnselectAll_Clears_SelectedIndex_And_SelectedItem()
        {
            var target = new TestSelector(SelectionMode.Multiple)
            {
                Items = new[] { "Foo", "Bar", "Baz" },
                SelectionMode = SelectionMode.Multiple,
                SelectedIndex = 0,
            };

            target.Selection.Clear();

            Assert.Equal(-1, target.SelectedIndex);
            Assert.Equal(null, target.SelectedItem);
        }

        [Fact]
        public void SelectAll_Handles_Duplicate_Items()
        {
            var target = new TestSelector(SelectionMode.Multiple)
            {
                Items = new[] { "Foo", "Bar", "Baz", "Foo", "Bar", "Baz" },
                SelectionMode = SelectionMode.Multiple,
            };

            target.Selection.SelectAll();

            Assert.Equal(new[] { "Foo", "Bar", "Baz", "Foo", "Bar", "Baz" }, target.SelectedItems);
        }

        [Fact]
        public void Adding_Item_Before_SelectedItems_Should_Update_Selection()
        {
            using var app = Start();

            var items = new ObservableCollection<string>
            {
               "Foo",
               "Bar",
               "Baz"
            };

            var target = new ListBox
            {
                Items = items,
                SelectionMode = SelectionMode.Multiple,
            };

            Prepare(target);
            target.SelectAll();
            items.Insert(0, "Qux");
            target.ScrollIntoView(0);

            Assert.Equal(1, target.SelectedIndex);
            Assert.Equal("Foo", target.SelectedItem);
            Assert.Equal(new[] { "Foo", "Bar", "Baz" }, target.SelectedItems);
            Assert.Equal(4, target.Presenter.RealizedElements.Count());
            Assert.Equal(new[] { 1, 2, 3 }, SelectedContainers(target));
        }

        [Fact]
        public void Removing_Item_Before_SelectedItem_Should_Update_Selection()
        {
            using var app = Start();

            var items = new ObservableCollection<string>
            {
               "Foo",
               "Bar",
               "Baz"
            };

            var target = new TestSelector(SelectionMode.Multiple)
            {
                Items = items,
                SelectionMode = SelectionMode.Multiple,
            };

            Prepare(target);

            target.SelectedIndex = 1;
            target.SelectRange(2);

            Assert.Equal(new[] { "Bar", "Baz" }, target.SelectedItems);

            items.RemoveAt(0);
            Layout(target);

            Assert.Equal(0, target.SelectedIndex);
            Assert.Equal("Bar", target.SelectedItem);
            Assert.Equal(new[] { "Bar", "Baz" }, target.SelectedItems);
            Assert.Equal(2, target.Presenter.RealizedElements.Count());
            Assert.Equal(new[] { 0, 1 }, SelectedContainers(target));
        }

        [Fact]
        public void Removing_SelectedItem_With_Multiple_Selection_Active_Should_Update_Selection()
        {
            using var app = Start();

            var items = new ObservableCollection<string>
            {
               "Foo",
               "Bar",
               "Baz"
            };

            var target = new ListBox
            {
                Items = items,
                SelectionMode = SelectionMode.Multiple,
            };

            Prepare(target);

            target.SelectAll();
            items.RemoveAt(0);

            Assert.Equal(0, target.SelectedIndex);
            Assert.Equal("Bar", target.SelectedItem);
            Assert.Equal(new[] { "Bar", "Baz" }, target.SelectedItems);
            Assert.Equal(new[] { 0, 1 }, SelectedContainers(target));
        }

        [Fact]
        public void Replacing_Selected_Item_Should_Update_SelectedItems()
        {
            using var app = Start();

            var items = new ObservableCollection<string>
            {
               "Foo",
               "Bar",
               "Baz"
            };

            var target = new ListBox
            {
                Items = items,
                SelectionMode = SelectionMode.Multiple,
            };

            Prepare(target);

            target.SelectAll();
            items[1] = "Qux";

            Assert.Equal(new[] { "Foo", "Baz" }, target.SelectedItems);
        }

        [Fact]
        public void Left_Click_On_SelectedItem_Should_Clear_Existing_Selection()
        {
            using var app = Start();

            var target = new ListBox
            {
                Items = new[] { "Foo", "Bar", "Baz" },
                ItemTemplate = new FuncDataTemplate<string>((x, _) => new TextBlock { Width = 20, Height = 10 }),
                SelectionMode = SelectionMode.Multiple,
            };

            Prepare(target);
            target.SelectAll();

            Assert.Equal(3, target.SelectedItems.Count);

            _helper.Click((Interactive)target.Presenter.RealizedElements.ElementAt(0));

            Assert.Equal(1, target.SelectedItems.Count);
            Assert.Equal(new[] { "Foo", }, target.SelectedItems);
            Assert.Equal(new[] { 0 }, SelectedContainers(target));
        }

        [Fact]
        public void Right_Click_On_SelectedItem_Should_Not_Clear_Existing_Selection()
        {
            using var app = Start();

            var target = new ListBox
            {
                Items = new[] { "Foo", "Bar", "Baz" },
                ItemTemplate = new FuncDataTemplate<string>((x, _) => new TextBlock { Width = 20, Height = 10 }),
                SelectionMode = SelectionMode.Multiple,
            };

            Prepare(target);
            target.SelectAll();

            Assert.Equal(3, target.SelectedItems.Count);

            _helper.Click((Interactive)target.Presenter.RealizedElements.ElementAt(0), MouseButton.Right);

            Assert.Equal(3, target.SelectedItems.Count);
        }

        [Fact]
        public void Right_Click_On_UnselectedItem_Should_Clear_Existing_Selection()
        {
            using var app = Start();

            var target = new ListBox
            {
                Items = new[] { "Foo", "Bar", "Baz" },
                ItemTemplate = new FuncDataTemplate<string>((x, _) => new TextBlock { Width = 20, Height = 10 }),
                SelectionMode = SelectionMode.Multiple,
            };

            Prepare(target);
            _helper.Click((Interactive)target.Presenter.RealizedElements.ElementAt(0));
            _helper.Click((Interactive)target.Presenter.RealizedElements.ElementAt(1), modifiers: KeyModifiers.Shift);

            Assert.Equal(2, target.SelectedItems.Count);

            _helper.Click((Interactive)target.Presenter.RealizedElements.ElementAt(2), MouseButton.Right);

            Assert.Equal(1, target.SelectedItems.Count);
        }

        [Fact]
        public void Adding_Selected_ItemContainers_Should_Update_Selection()
        {
            using var app = Start();

            var items = new AvaloniaList<ItemContainer>(new[]
            {
                new ItemContainer(),
                new ItemContainer(),
            });

            var target = new TestSelector(SelectionMode.Multiple)
            {
                Items = items,
                SelectionMode = SelectionMode.Multiple,
            };

            Prepare(target);
            items.Add(new ItemContainer { IsSelected = true });
            items.Add(new ItemContainer { IsSelected = true });

            Assert.Equal(2, target.SelectedIndex);
            Assert.Equal(items[2], target.SelectedItem);
            Assert.Equal(new[] { items[2], items[3] }, target.SelectedItems);
        }

        [Fact]
        public void Shift_Right_Click_Should_Not_Select_Multiple()
        {
            using var app = Start();

            var target = new ListBox
            {
                Items = new[] { "Foo", "Bar", "Baz" },
                ItemTemplate = new FuncDataTemplate<string>((x, _) => new TextBlock { Width = 20, Height = 10 }),
                SelectionMode = SelectionMode.Multiple,
            };

            Prepare(target);

            _helper.Click((Interactive)target.Presenter.RealizedElements.ElementAt(0));
            _helper.Click((Interactive)target.Presenter.RealizedElements.ElementAt(2), MouseButton.Right, modifiers: KeyModifiers.Shift);

            Assert.Equal(1, target.SelectedItems.Count);
        }

        [Fact]
        public void Ctrl_Right_Click_Should_Not_Select_Multiple()
        {
            using var app = Start();

            var target = new ListBox
            {
                Items = new[] { "Foo", "Bar", "Baz" },
                ItemTemplate = new FuncDataTemplate<string>((x, _) => new TextBlock { Width = 20, Height = 10 }),
                SelectionMode = SelectionMode.Multiple,
            };

            Prepare(target);

            _helper.Click((Interactive)target.Presenter.RealizedElements.ElementAt(0));
            _helper.Click((Interactive)target.Presenter.RealizedElements.ElementAt(2), MouseButton.Right, modifiers: KeyModifiers.Control);

            Assert.Equal(1, target.SelectedItems.Count);
        }

        [Fact]
        public void Adding_To_Selection_Should_Set_SelectedIndex()
        {
            var target = new TestSelector(SelectionMode.Multiple)
            {
                Items = new[] { "foo", "bar" },
            };

            target.ApplyTemplate();
            target.SelectedItems.Add("bar");

            Assert.Equal(1, target.SelectedIndex);
        }

        [Fact]
        public void Assigning_Null_To_Selection_Should_Create_New_SelectionModel()
        {
            var target = new TestSelector(SelectionMode.Multiple)
            {
                Items = new[] { "foo", "bar" },
            };

            var oldSelection = target.Selection;

            target.Selection = null;

            Assert.NotNull(target.Selection);
            Assert.NotSame(oldSelection, target.Selection);
        }

        [Fact]
        public void Assigning_SelectionModel_With_Different_Source_To_Selection_Should_Fail()
        {
            var target = new TestSelector(SelectionMode.Multiple)
            {
                Items = new[] { "foo", "bar" },
                Template = Template(),
            };

            var selection = new SelectionModel<string> { Source = new[] { "baz" } };
            Assert.Throws<ArgumentException>(() => target.Selection = selection);
        }

        [Fact]
        public void Assigning_SelectionModel_With_Null_Source_To_Selection_Should_Set_Source()
        {
            var target = new TestSelector(SelectionMode.Multiple)
            {
                Items = new[] { "foo", "bar" },
            };

            var selection = new SelectionModel<string>();
            target.Selection = selection;

            Assert.Same(target.Items, selection.Source);
        }

        [Fact]
        public void Assigning_Single_Selected_Item_To_Selection_Should_Set_SelectedIndex()
        {
            using var app = Start();

            var target = new TestSelector(SelectionMode.Multiple)
            {
                Items = new[] { "foo", "bar" },
            };

            Prepare(target);

            var selection = new SelectionModel<string> { SingleSelect = false };
            selection.Select(1);
            target.Selection = selection;

            Assert.Equal(1, target.SelectedIndex);
            Assert.Equal(new[] { "bar" }, target.Selection.SelectedItems);
            Assert.Equal(new[] { 1 }, SelectedContainers(target));
        }

        [Fact]
        public void Assigning_Multiple_Selected_Items_To_Selection_Should_Set_SelectedIndex()
        {
            using var app = Start();

            var target = new TestSelector(SelectionMode.Multiple)
            {
                Items = new[] { "foo", "bar", "baz" },
            };

            Prepare(target);

            var selection = new SelectionModel<string> { SingleSelect = false };
            selection.SelectRange(0, 2);
            target.Selection = selection;

            Assert.Equal(0, target.SelectedIndex);
            Assert.Equal(new[] { "foo", "bar", "baz" }, target.Selection.SelectedItems);
            Assert.Equal(new[] { 0, 1, 2 }, SelectedContainers(target));
        }

        [Fact]
        public void Reassigning_Selection_Should_Clear_Selection()
        {
            using var app = Start();

            var target = new TestSelector(SelectionMode.Multiple)
            {
                Items = new[] { "foo", "bar" },
            };

            target.Selection.Select(1);
            target.Selection = new SelectionModel<string>();

            Assert.Equal(-1, target.SelectedIndex);
            Assert.Null(target.SelectedItem);
        }

        [Fact]
        public void Assigning_Selection_Should_Set_Item_IsSelected()
        {
            using var app = Start();

            var items = new[]
            {
                new ListBoxItem(),
                new ListBoxItem(),
                new ListBoxItem(),
            };

            var target = new TestSelector(SelectionMode.Multiple)
            {
                Items = items,
            };

            Prepare(target);

            var selection = new SelectionModel<object> { SingleSelect = false };
            selection.SelectRange(0, 1);
            target.Selection = selection;

            Assert.True(items[0].IsSelected);
            Assert.True(items[1].IsSelected);
            Assert.False(items[2].IsSelected);
        }

        [Fact]
        public void Assigning_Selection_Should_Raise_SelectionChanged()
        {
            var items = new[] { "foo", "bar", "baz" };

            var target = new TestSelector(SelectionMode.Multiple)
            {
                Items = items,
                SelectedItem = "bar",
            };

            var raised = 0;

            target.SelectionChanged += (s, e) =>
            {
                if (raised == 0)
                {
                    Assert.Empty(e.AddedItems.Cast<object>());
                    Assert.Equal(new[] { "bar" }, e.RemovedItems.Cast<object>());
                }
                else
                {
                    Assert.Equal(new[] { "foo", "baz" }, e.AddedItems.Cast<object>());
                    Assert.Empty(e.RemovedItems.Cast<object>());
                }

                ++raised;
            };

            Prepare(target);

            var selection = new SelectionModel<string> { Source = items, SingleSelect = false };
            selection.Select(0);
            selection.Select(2);
            target.Selection = selection;

            Assert.Equal(2, raised);
        }
        private IEnumerable<int> SelectedContainers(SelectingItemsControl target)
        {
            return target.Presenter.RealizedElements
                .Select(x => x.Classes.Contains(":selected") ? target.GetContainerIndex(x) : -1)
                .Where(x => x != -1);
        }

        private void ClickContainer(SelectingItemsControl target, int index, KeyModifiers modifiers = KeyModifiers.None)
        {
            _helper.Click((Interactive)target.Presenter.RealizedElements.ElementAt(index), modifiers: modifiers);
            ItemsRepeaterWorkaround(target);
        }

        private void ItemsRepeaterWorkaround(SelectingItemsControl target)
        {
            // HACK: Selecting an item in ItemsRepeater causes it to be scrolled to the top of the viewport,
            // even if all items can fit in the viewport. This causes items before the selected item to be
            // unrealized. Work around this by scrolling to the top.
            target.ScrollIntoView(0);
        }

        private class OldDataContextViewModel
        {
            public OldDataContextViewModel()
            {
                Items = new List<string> { "foo", "bar" };
                SelectedItems = new List<string>();
                Selection = new SelectionModel<string>();
            }

            public List<string> Items { get; } 
            public List<string> SelectedItems { get; }
            public SelectionModel<string> Selection { get; }
        }

        private class ItemContainer : Control, ISelectable
        {
            public string Value { get; set; }
            public bool IsSelected { get; set; }
        }
    }
}
