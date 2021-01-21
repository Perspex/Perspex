using System.Linq;
using Avalonia.Input;
using Xunit;

namespace Avalonia.Controls.UnitTests
{
    public partial class ListBoxTests
    {
        [Fact]
        public void Shift_And_Arrow_Key_Should_Add_To_Selection()
        {
            using var app = Start();
            
            var target = new ListBox
            {
                Items = new[] { "Foo", "Bar", "Baz " },
                SelectionMode = SelectionMode.Multiple
            };

            Prepare(target);

            target.SelectedItem = "Foo";
            target.TryGetContainer(0).Focus();

            KeyDown(target, Key.Down, KeyModifiers.Shift);

            Assert.Equal(new[] { "Foo", "Bar" }, target.SelectedItems);
            Assert.NotNull(target.TryGetContainer(1));
            Assert.Same(target.TryGetContainer(1), FocusManager.Instance.Current);
            Assert.Equal(0, target.Selection.AnchorIndex);
        }

        [Fact]
        public void Ctrl_And_Arrow_Key_Should_Move_Focus_But_Not_Change_Selection()
        {
            using var app = Start();
            
            var target = new ListBox
            {
                Items = new[] { "Foo", "Bar", "Baz " },
                SelectionMode = SelectionMode.Multiple
            };

            Prepare(target);

            target.SelectedItem = "Foo";
            target.TryGetContainer(0).Focus();

            KeyDown(target, Key.Down, KeyModifiers.Control);

            Assert.Equal(new[] { "Foo" }, target.SelectedItems);
            Assert.NotNull(target.TryGetContainer(1));
            Assert.Same(target.TryGetContainer(1), FocusManager.Instance.Current);
        }

        [Fact]
        public void Ctrl_Shift_And_Arrow_Key_Should_Add_To_Selection()
        {
            using var app = Start();

            var target = new ListBox
            {
                Items = new[] { "Foo", "Bar", "Baz " },
                SelectionMode = SelectionMode.Multiple
            };

            Prepare(target);

            target.SelectedItem = "Foo";
            target.TryGetContainer(0).Focus();

            KeyDown(target, Key.Down, KeyModifiers.Control | KeyModifiers.Shift);

            Assert.Equal(new[] { "Foo", "Bar" }, target.SelectedItems);
            Assert.NotNull(target.TryGetContainer(1));
            Assert.Same(target.TryGetContainer(1), FocusManager.Instance.Current);
            Assert.Equal(0, target.Selection.AnchorIndex);
        }


        [Fact]
        public void SelectAll_Gesture_Should_Select_All_Items()
        {
            using var app = Start();

            var target = new ListBox
            {
                Items = new[] { "Foo", "Bar", "Baz " },
                SelectionMode = SelectionMode.Multiple
            };

            Prepare(target);

            target.SelectedItem = "Foo";
            target.TryGetContainer(0).Focus();

            KeyDown(target, Key.A, KeyModifiers.Control);

            Assert.Equal(3, target.Selection.SelectedIndexes.Count);
        }
    }
}
