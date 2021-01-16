using Avalonia.Input.Raw;
using Avalonia.Interactivity;
using Moq;
using Xunit;

namespace Avalonia.Input.UnitTests
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
                    Key.A, "a",
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
                    Key.A, "a",
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
    }
}
