using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.LogicalTree;
using Xunit;

namespace Avalonia.Controls.UnitTests
{
    public partial class ListBoxTests
    {
        [Fact]
        public void Focusing_Item_Should_Not_Select_It()
        {
            using var app = Start();

            var target = new ListBox
            {
                Items = new[] { "Foo", "Bar", "Baz " },
            };

            Prepare(target);

            target.Presenter.RealizedElements.First().RaiseEvent(new GotFocusEventArgs
            {
                RoutedEvent = InputElement.GotFocusEvent,
                NavigationMethod = NavigationMethod.Directional,
            });

            Assert.Equal(-1, target.SelectedIndex);
        }

        [Fact]
        public void Clicking_Item_Should_Select_It()
        {
            using var app = Start();

            var target = new ListBox
            {
                Items = new[] { "Foo", "Bar", "Baz " },
            };

            Prepare(target);
            _mouse.Click(target.Presenter.RealizedElements.First());

            Assert.Equal(0, target.SelectedIndex);
        }

        [Fact]
        public void Clicking_Selected_Item_Should_Not_Deselect_It()
        {
            using var app = Start();

            var target = new ListBox
            {
                Items = new[] { "Foo", "Bar", "Baz " },
            };

            Prepare(target);
            target.SelectedIndex = 0;

            _mouse.Click(target.Presenter.RealizedElements.First());

            Assert.Equal(0, target.SelectedIndex);
        }

        [Fact]
        public void Clicking_Item_Should_Select_It_When_SelectionMode_Toggle()
        {
            using var app = Start();

            var target = new ListBox
            {
                Items = new[] { "Foo", "Bar", "Baz " },
                SelectionMode = SelectionMode.Single | SelectionMode.Toggle,
            };

            Prepare(target);

            _mouse.Click(target.Presenter.RealizedElements.First());

            Assert.Equal(0, target.SelectedIndex);
        }

        [Fact]
        public void Clicking_Selected_Item_Should_Deselect_It_When_SelectionMode_Toggle()
        {
            using var app = Start();

            var target = new ListBox
            {
                Items = new[] { "Foo", "Bar", "Baz " },
                SelectionMode = SelectionMode.Toggle,
            };

            Prepare(target);
            target.SelectedIndex = 0;

            _mouse.Click(target.Presenter.RealizedElements.First());

            Assert.Equal(-1, target.SelectedIndex);
        }

        [Fact]
        public void Clicking_Selected_Item_Should_Not_Deselect_It_When_SelectionMode_ToggleAlwaysSelected()
        {
            using var app = Start();

            var target = new ListBox
            {
                Items = new[] { "Foo", "Bar", "Baz " },
                SelectionMode = SelectionMode.Toggle | SelectionMode.AlwaysSelected,
            };

            Prepare(target);
            target.SelectedIndex = 0;

            _mouse.Click(target.Presenter.RealizedElements.First());

            Assert.Equal(0, target.SelectedIndex);
        }

        [Fact]
        public void Clicking_Another_Item_Should_Select_It_When_SelectionMode_Toggle()
        {
            using var app = Start();

            var target = new ListBox
            {
                Items = new[] { "Foo", "Bar", "Baz " },
                SelectionMode = SelectionMode.Single | SelectionMode.Toggle,
            };

            Prepare(target);
            target.SelectedIndex = 1;

            _mouse.Click(target.Presenter.RealizedElements.First());

            Assert.Equal(0, target.SelectedIndex);
        }

        [Fact]
        public void Down_Key_Should_Select_Next_Item()
        {
            using var app = Start();

            var target = new ListBox
            {
                Items = new[] { "Foo", "Bar", "Baz " },
            };

            Prepare(target);
            _mouse.Click(target.Presenter.RealizedElements.First());

            Assert.Equal(0, target.SelectedIndex);

            KeyDown(target, Key.Down);

            Assert.Equal(1, target.SelectedIndex);
            Assert.Equal(1, target.Selection.AnchorIndex);
        }

        [Fact]
        public void Setting_Item_IsSelected_Sets_ListBox_Selection()
        {
            using var app = Start();

            var target = new ListBox
            {
                Items = new[] { "Foo", "Bar", "Baz " },
            };

            Prepare(target);

            ((ListBoxItem)target.GetLogicalChildren().ElementAt(1)).IsSelected = true;

            Assert.Equal("Bar", target.SelectedItem);
            Assert.Equal(1, target.SelectedIndex);
        }

        [Fact]
        public void SelectedItem_Should_Not_Cause_StackOverflow()
        {
            using var app = Start();

            var viewModel = new TestStackOverflowViewModel()
            {
                Items = new List<string> { "foo", "bar", "baz" }
            };

            var target = new ListBox
            {
                DataContext = viewModel,
                Items = viewModel.Items
            };

            target.Bind(ListBox.SelectedItemProperty,
                new Binding("SelectedItem") { Mode = BindingMode.TwoWay });

            Assert.Equal(0, viewModel.SetterInvokedCount);

            // In Issue #855, a Stackoverflow occured here.
            target.SelectedItem = viewModel.Items[2];

            Assert.Equal(viewModel.Items[1], target.SelectedItem);
            Assert.Equal(1, viewModel.SetterInvokedCount);
        }

        private class TestStackOverflowViewModel : INotifyPropertyChanged
        {
            public List<string> Items { get; set; }

            public int SetterInvokedCount { get; private set; }

            public const int MaxInvokedCount = 1000;

            private string _selectedItem;

            public event PropertyChangedEventHandler PropertyChanged;

            public string SelectedItem
            {
                get { return _selectedItem; }
                set
                {
                    if (_selectedItem != value)
                    {
                        SetterInvokedCount++;

                        int index = Items.IndexOf(value);

                        if (MaxInvokedCount > SetterInvokedCount && index > 0)
                        {
                            _selectedItem = Items[index - 1];
                        }
                        else
                        {
                            _selectedItem = value;
                        }

                        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedItem)));
                    }
                }
            }
        }
    }
}
