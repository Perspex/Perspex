using OpenQA.Selenium.Appium.Windows;
using Xunit;

namespace Avalonia.IntegrationTests.Win32
{
    [Collection("Default")]
    public class MenuTests
    {
        private WindowsDriver<WindowsElement> _session;

        public MenuTests(TestAppFixture fixture) => _session = fixture.Session;

        [Fact]
        public void File()
        {
            var fileMenu = _session.FindElementByAccessibilityId("FileMenu");

            Assert.Equal("File", fileMenu.Text);
        }

        [Fact]
        public void Open()
        {
            var fileMenu = _session.FindElementByAccessibilityId("FileMenu");
            fileMenu.Click();

            var openMenu = fileMenu.FindElementByAccessibilityId("OpenMenu");
            Assert.Equal("Ctrl+O", openMenu.GetAttribute("AcceleratorKey"));
        }
    }
}
