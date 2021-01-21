using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using Avalonia.Collections;
using Xunit;

namespace Avalonia.Controls.UnitTests.Primitives
{
    public partial class SelectingItemsControlTests
    {
        [Fact]
        public void First_Item_Should_Be_Selected()
        {
            var target = new TestSelector(SelectionMode.AlwaysSelected)
            {
                Items = new[] { "foo", "bar" },
            };

            Assert.Equal(0, target.SelectedIndex);
            Assert.Equal("foo", target.SelectedItem);
        }

        [Fact]
        public void First_Item_Should_Be_Selected_When_Added()
        {
            var items = new AvaloniaList<string>();
            var target = new TestSelector(SelectionMode.AlwaysSelected)
            {
                Items = items,
            };

            items.Add("foo");

            Assert.Equal(0, target.SelectedIndex);
            Assert.Equal("foo", target.SelectedItem);
        }


        [Fact]
        public void First_Item_Should_Be_Selected_When_Reset()
        {
            var items = new ResetOnAdd();
            var target = new TestSelector(SelectionMode.AlwaysSelected)
            {
                Items = items,
            };

            items.Add("foo");

            Assert.Equal(0, target.SelectedIndex);
            Assert.Equal("foo", target.SelectedItem);
        }

        [Fact]
        public void Item_Should_Be_Selected_When_Selection_Removed()
        {
            var items = new AvaloniaList<string>(new[] { "foo", "bar", "baz", "qux" });
            var target = new TestSelector(SelectionMode.AlwaysSelected)
            {
                Items = items,
            };

            target.SelectedIndex = 2;
            items.RemoveAt(2);

            Assert.Equal(0, target.SelectedIndex);
            Assert.Equal("foo", target.SelectedItem);
        }

        [Fact]
        public void Selection_Should_Be_Cleared_When_No_Items_Left()
        {
            var items = new AvaloniaList<string>(new[] { "foo", "bar" });
            var target = new TestSelector(SelectionMode.AlwaysSelected)
            {
                Items = items,
            };

            target.SelectedIndex = 1;
            items.RemoveAt(1);
            items.RemoveAt(0);

            Assert.Equal(-1, target.SelectedIndex);
            Assert.Null(target.SelectedItem);
        }

        [Fact]
        public void Removing_Selected_First_Item_Should_Select_Next_Item()
        {
            using var app = Start();

            var items = new AvaloniaList<string>(new[] { "foo", "bar" });
            var target = new TestSelector(SelectionMode.AlwaysSelected)
            {
                Items = items,
            };

            Prepare(target);
            items.RemoveAt(0);
            Layout(target);

            Assert.Equal(0, target.SelectedIndex);
            Assert.Equal("bar", target.SelectedItem);
            Assert.Equal(new[] { ":selected" }, target.Presenter.RealizedElements.First().Classes);
        }

        private class ResetOnAdd : List<string>, INotifyCollectionChanged
        {
            public event NotifyCollectionChangedEventHandler CollectionChanged;

            public new void Add(string item)
            {
                base.Add(item);
                CollectionChanged?.Invoke(
                    this,
                    new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
            }
        }
    }
}
