using OpenQA.Selenium.Appium.Windows;
using Xunit;

namespace Avalonia.IntegrationTests.Win32
{
    [Collection("Default")]
    public class ComboBoxTests
    {
        private WindowsDriver<WindowsElement> _session;

        public ComboBoxTests(TestAppFixture fixture)
        {
            _session = fixture.Session;

            var tabs = _session.FindElementByAccessibilityId("MainTabs");
            var tab = tabs.FindElementByName("ComboBox");
            tab.Click();
        }

        [Fact]
        public void UnselectedComboBox()
        {
            var comboBox = _session.FindElementByAccessibilityId("UnselectedComboBox");

            Assert.Equal(string.Empty, comboBox.Text);

            comboBox.Click();
            comboBox.FindElementByName("Bar").Click();

            Assert.Equal("Bar", comboBox.Text);
        }

        [Fact]
        public void SelectedIndex0ComboBox()
        {
            var comboBox = _session.FindElementByAccessibilityId("SelectedIndex0ComboBox");

            Assert.Equal("Foo", comboBox.Text);
        }

        [Fact]
        public void SelectedIndex1ComboBox()
        {
            var comboBox = _session.FindElementByAccessibilityId("SelectedIndex1ComboBox");

            Assert.Equal("Bar", comboBox.Text);
        }
    }
}
