using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Xunit;

namespace Avalonia.Base.UnitTests.Interactivity
{
    public class RoutedEventRegistryTests
    {
        [Fact]
        public void Pointer_Events_Should_Be_Registered()
        {
            var expectedEvents = new List<RoutedEvent> { InputElement.PointerPressedEvent, InputElement.PointerReleasedEvent }; 
            var registeredEvents = RoutedEventRegistry.Instance.GetRegistered<InputElement>();
            Assert.Contains(registeredEvents, expectedEvents.Contains);
        }

        [Fact]
        public void ClickEvent_Should_Be_Registered_On_Button()
        {
            var expectedEvents = new List<RoutedEvent> { Button.ClickEvent };
            var registeredEvents = RoutedEventRegistry.Instance.GetRegistered<Button>();
            Assert.Contains(registeredEvents, expectedEvents.Contains);
        }

        [Fact]
        public void ClickEvent_Should_Not_Be_Registered_On_ContentControl()
        {
            // force ContentControl type to be loaded
            new ContentControl();
            var expectedEvents = new List<RoutedEvent> { Button.ClickEvent };
            var registeredEvents = RoutedEventRegistry.Instance.GetRegistered<ContentControl>();
            Assert.DoesNotContain(registeredEvents, expectedEvents.Contains);
        }

        [Fact]
        public void InputElement_Events_Should_Not_Be_Registered_On_Button()
        {
            // force Button type to be loaded
            new Button();
            var expectedEvents = new List<RoutedEvent> { InputElement.PointerPressedEvent, InputElement.PointerReleasedEvent };
            var registeredEvents = RoutedEventRegistry.Instance.GetRegistered<Button>();
            Assert.DoesNotContain(registeredEvents, expectedEvents.Contains);
        }
    }
}
