using System;
using System.Collections.Generic;
using System.ComponentModel;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform;
using Avalonia.Threading;
using Avalonia.UnitTests;
using Moq;
using Xunit;

namespace Avalonia.Controls.UnitTests
{
    
    public class DesktopStyleApplicationLifetimeTests
    {
        [Fact]
        public void Should_Set_ExitCode_After_Shutdown()
        {
            using (UnitTestApplication.Start(TestServices.MockThreadingInterface))
            using(var lifetime = new ClassicDesktopStyleApplicationLifetime())    
            {
                lifetime.Shutdown(1337);

                var exitCode = lifetime.Start(Array.Empty<string>());

                Assert.Equal(1337, exitCode);
            }
        }
        
        
        [Fact]
        public void Should_Close_All_Remaining_Open_Windows_After_Explicit_Exit_Call()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            using(var lifetime = new ClassicDesktopStyleApplicationLifetime())
            {
                var windows = new List<Window> { new Window(), new Window(), new Window(), new Window() };

                foreach (var window in windows)
                {
                    window.Show();
                }
                Assert.Equal(4, lifetime.Windows.Count);
                lifetime.Shutdown();

                Assert.Empty(lifetime.Windows);
            }
        }
        
        [Fact]
        public void Should_Only_Exit_On_Explicit_Exit()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            using(var lifetime = new ClassicDesktopStyleApplicationLifetime())
            {
                lifetime.ShutdownMode = ShutdownMode.OnExplicitShutdown;

                var hasExit = false;

                lifetime.Exit += (s, e) => hasExit = true;

                var windowA = new Window();

                windowA.Show();

                var windowB = new Window();

                windowB.Show();

                windowA.Close();

                Assert.False(hasExit);

                windowB.Close();

                Assert.False(hasExit);

                lifetime.Shutdown();

                Assert.True(hasExit);
            }
        }
        
        [Fact]
        public void Should_Exit_After_MainWindow_Closed()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            using(var lifetime = new ClassicDesktopStyleApplicationLifetime())
            {
                lifetime.ShutdownMode = ShutdownMode.OnMainWindowClose;

                var hasExit = false;

                lifetime.Exit += (s, e) => hasExit = true;

                var mainWindow = new Window();

                mainWindow.Show();

                lifetime.MainWindow = mainWindow;

                var window = new Window();

                window.Show();

                mainWindow.Close();

                Assert.True(hasExit);
            }
        }

        [Fact]
        public void Should_Exit_After_Last_Window_Closed()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            using(var lifetime = new ClassicDesktopStyleApplicationLifetime())
            {
                lifetime.ShutdownMode = ShutdownMode.OnLastWindowClose;

                var hasExit = false;

                lifetime.Exit += (s, e) => hasExit = true;

                var windowA = new Window();

                windowA.Show();

                var windowB = new Window();

                windowB.Show();

                windowA.Close();

                Assert.False(hasExit);

                windowB.Close();

                Assert.True(hasExit);
            }
        }
        
        [Fact]
        public void Show_Should_Add_Window_To_OpenWindows()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            using(var lifetime = new ClassicDesktopStyleApplicationLifetime())
            {
                var window = new Window();

                window.Show();

                Assert.Equal(new[] { window }, lifetime.Windows);
            }
        }

        [Fact]
        public void Window_Should_Be_Added_To_OpenWindows_Only_Once()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            using(var lifetime = new ClassicDesktopStyleApplicationLifetime())
            {
                var window = new Window();

                window.Show();
                window.Show();
                window.IsVisible = true;

                Assert.Equal(new[] { window }, lifetime.Windows);

                window.Close();
            }
        }

        [Fact]
        public void Close_Should_Remove_Window_From_OpenWindows()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            using(var lifetime = new ClassicDesktopStyleApplicationLifetime())
            {
                var window = new Window();

                window.Show();
                Assert.Equal(1, lifetime.Windows.Count);
                window.Close();

                Assert.Empty(lifetime.Windows);
            }
        }
        
        [Fact]
        public void Impl_Closing_Should_Remove_Window_From_OpenWindows()
        {
            var windowImpl = new Mock<IWindowImpl>();
            windowImpl.SetupProperty(x => x.Closed);
            windowImpl.Setup(x => x.DesktopScaling).Returns(1);
            windowImpl.Setup(x => x.RenderScaling).Returns(1);

            var services = TestServices.StyledWindow.With(
                windowingPlatform: new MockWindowingPlatform(() => windowImpl.Object));

            using (UnitTestApplication.Start(services))
            using(var lifetime = new ClassicDesktopStyleApplicationLifetime())
            {
                var window = new Window();

                window.Show();
                Assert.Equal(1, lifetime.Windows.Count);
                windowImpl.Object.Closed();

                Assert.Empty(lifetime.Windows);
            }
        }

        [Fact]
        public void Should_Allow_Canceling_Shutdown_Via_ShutdownRequested_Event()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            using (var lifetime = new ClassicDesktopStyleApplicationLifetime())
            {
                var lifetimeEvents = new Mock<IPlatformLifetimeEventsImpl>();
                AvaloniaLocator.CurrentMutable.Bind<IPlatformLifetimeEventsImpl>().ToConstant(lifetimeEvents.Object);
                lifetime.Start(Array.Empty<string>());

                var window = new Window();
                var raised = 0;

                window.Show();

                lifetime.ShutdownRequested += (s, e) =>
                {
                    e.Cancel = true;
                    ++raised;
                };

                lifetimeEvents.Raise(x => x.ShutdownRequested += null, new CancelEventArgs());

                Assert.Equal(1, raised);
                Assert.Equal(new[] { window }, lifetime.Windows);
            }
        }
    }
}
