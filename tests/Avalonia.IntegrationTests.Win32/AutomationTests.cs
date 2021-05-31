using OpenQA.Selenium.Appium.Windows;
using Xunit;

namespace Avalonia.IntegrationTests.Win32
{
    [Collection("Default")]
    public class AutomationTests
    {
        private WindowsDriver<WindowsElement> _session;

        public AutomationTests(TestAppFixture fixture)
        {
            _session = fixture.Session;

            var tabs = _session.FindElementByAccessibilityId("MainTabs");
            var tab = tabs.FindElementByName("Automation");
            tab.Click();
        }

        [Fact]
        public void AutomationId()
        {
            // AutomationID can be specified by the Name or AutomationProperties.AutomationId
            // properties, with the latter taking precedence.
            var byName = _session.FindElementByAccessibilityId("TextBlockWithName");
            var byAutomationId = _session.FindElementByAccessibilityId("TextBlockWithNameAndAutomationId");
        }

        [Fact]
        public void LabeledBy()
        {
            var label = _session.FindElementByAccessibilityId("TextBlockAsLabel");
            var labeledTextBox = _session.FindElementByAccessibilityId("LabeledByTextBox");

            Assert.Equal("Label for TextBox", label.Text);
            Assert.Equal("Label for TextBox", labeledTextBox.GetAttribute("Name"));
        }
    }
}
