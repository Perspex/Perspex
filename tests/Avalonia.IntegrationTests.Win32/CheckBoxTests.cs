using OpenQA.Selenium.Appium.Windows;
using Xunit;

namespace Avalonia.IntegrationTests.Win32
{
    [Collection("Default")]
    public class CheckBoxTests
    {
        private WindowsDriver<WindowsElement> _session;

        public CheckBoxTests(TestAppFixture fixture)
        {
            _session = fixture.Session;

            var tabs = _session.FindElementByAccessibilityId("MainTabs");
            var tab = tabs.FindElementByName("CheckBox");
            tab.Click();
        }

        [Fact]
        public void UncheckedCheckBox()
        {
            var checkBox = _session.FindElementByAccessibilityId("UncheckedCheckBox");

            Assert.Equal("Unchecked", checkBox.Text);
            Assert.False(checkBox.Selected);
            Assert.Equal("0", checkBox.GetAttribute("Toggle.ToggleState"));

            checkBox.Click();

            Assert.True(checkBox.Selected);
            Assert.Equal("1", checkBox.GetAttribute("Toggle.ToggleState"));
        }

        [Fact]
        public void CheckedCheckBox()
        {
            var checkBox = _session.FindElementByAccessibilityId("CheckedCheckBox");

            Assert.Equal("Checked", checkBox.Text);
            Assert.True(checkBox.Selected);
            Assert.Equal("1", checkBox.GetAttribute("Toggle.ToggleState"));

            checkBox.Click();

            Assert.False(checkBox.Selected);
            Assert.Equal("0", checkBox.GetAttribute("Toggle.ToggleState"));
        }

        [Fact]
        public void ThreeStateCheckBox()
        {
            var checkBox = _session.FindElementByAccessibilityId("ThreeStateCheckBox");

            Assert.Equal("ThreeState", checkBox.Text);
            Assert.Equal("2", checkBox.GetAttribute("Toggle.ToggleState"));

            checkBox.Click();

            Assert.False(checkBox.Selected);
            Assert.Equal("0", checkBox.GetAttribute("Toggle.ToggleState"));

            checkBox.Click();

            Assert.True(checkBox.Selected);
            Assert.Equal("1", checkBox.GetAttribute("Toggle.ToggleState"));

            checkBox.Click();
            Assert.Equal("2", checkBox.GetAttribute("Toggle.ToggleState"));
        }
    }
}
