using System;
using System.Windows.Input;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.UnitTests;
using Moq;
using Xunit;

namespace Avalonia.Base.UnitTests.Input
{
    public class KeyboardDeviceTests
    {
        [Fact]
        public void Keypresses_Should_Be_Sent_To_Root_If_No_Focused_Element()
        {
            var target = new KeyboardDevice();
            var root = new Mock<IInputRoot>();

            target.ProcessRawEvent(
                new RawKeyEventArgs(
                    target,
                    0,
                    root.Object,
                    RawKeyEventType.KeyDown,
                    Key.A,
                    RawInputModifiers.None));

            root.Verify(x => x.RaiseEvent(It.IsAny<KeyEventArgs>()));
        }

        [Fact]
        public void Keypresses_Should_Be_Sent_To_Focused_Element()
        {
            var target = new KeyboardDevice();
            var focused = new Mock<IInputElement>();
            var root = Mock.Of<IInputRoot>();

            target.SetFocusedElement(
                focused.Object,
                NavigationMethod.Unspecified,
                KeyModifiers.None);

            target.ProcessRawEvent(
                new RawKeyEventArgs(
                    target,
                    0,
                    root,
                    RawKeyEventType.KeyDown,
                    Key.A,
                    RawInputModifiers.None));

            focused.Verify(x => x.RaiseEvent(It.IsAny<KeyEventArgs>()));
        }

        [Fact]
        public void TextInput_Should_Be_Sent_To_Root_If_No_Focused_Element()
        {
            var target = new KeyboardDevice();
            var root = new Mock<IInputRoot>();

            target.ProcessRawEvent(
                new RawTextInputEventArgs(
                    target,
                    0,
                    root.Object,
                    "Foo"));

            root.Verify(x => x.RaiseEvent(It.IsAny<TextInputEventArgs>()));
        }

        [Fact]
        public void TextInput_Should_Be_Sent_To_Focused_Element()
        {
            var target = new KeyboardDevice();
            var focused = new Mock<IInputElement>();
            var root = Mock.Of<IInputRoot>();

            target.SetFocusedElement(
                focused.Object,
                NavigationMethod.Unspecified,
                KeyModifiers.None);

            target.ProcessRawEvent(
                new RawTextInputEventArgs(
                    target,
                    0,
                    root,
                    "Foo"));

            focused.Verify(x => x.RaiseEvent(It.IsAny<TextInputEventArgs>()));
        }

        [Fact]
        public void Can_Change_KeyBindings_In_Keybinding_Event_Handler()
        {
            var target = new KeyboardDevice();
            var button = new Button();
            var root = new TestRoot(button);
            var raised = 0;

            button.KeyBindings.Add(new KeyBinding
            {
                Gesture = new KeyGesture(Key.O, KeyModifiers.Control),
                Command = new DelegateCommand(() =>
                {
                    button.KeyBindings.Clear();
                    ++raised;
                }),
            });

            target.SetFocusedElement(button, NavigationMethod.Pointer, 0);
            target.ProcessRawEvent(
                new RawKeyEventArgs(
                    target,
                    0,
                    root,
                    RawKeyEventType.KeyDown,
                    Key.O,
                    RawInputModifiers.Control));

            Assert.Equal(1, raised);
        }

        private class DelegateCommand : ICommand
        {
            private readonly Action _action;
            public DelegateCommand(Action action) => _action = action;
            public event EventHandler CanExecuteChanged;
            public bool CanExecute(object parameter) => true;
            public void Execute(object parameter) => _action();
        }
    }
}
