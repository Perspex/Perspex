using System;
using Avalonia.Input;
using Avalonia.LogicalTree;
using Avalonia.Markup.Xaml;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Avalonia.Platform;
using Avalonia.UnitTests;
using Castle.DynamicProxy.Generators;
using Moq;
using Xunit;

namespace Avalonia.Controls.UnitTests
{
    public class ContextMenuTests
    {
        private Mock<IPopupImpl> popupImpl;
        private MouseTestHelper _mouse = new MouseTestHelper();

        [Fact]
        public void Opening_Raises_Single_Opened_Event()
        {
            using (Application())
            {
                var sut = new ContextMenu();
                var target = new Panel
                {
                    ContextMenu = sut
                };

                var window = new Window { Content = target };
                window.ApplyTemplate();
                window.Presenter.ApplyTemplate();

                int openedCount = 0;

                sut.MenuOpened += (sender, args) =>
                {
                    openedCount++;
                };

                sut.Open(target);

                Assert.Equal(1, openedCount);
            }
        }

        [Fact]
        public void Open_Should_Use_Default_Control()
        {
            using (Application())
            {
                var sut = new ContextMenu();
                var target = new Panel
                {
                    ContextMenu = sut
                };

                var window = new Window { Content = target };
                window.ApplyTemplate();
                window.Presenter.ApplyTemplate();

                bool opened = false;

                sut.MenuOpened += (sender, args) =>
                {
                    opened = true;
                };

                sut.Open();

                Assert.True(opened);
            }
        }

        [Fact]
        public void Open_Should_Raise_Exception_If_AlreadyDetached()
        {
            using (Application())
            {
                var sut = new ContextMenu();
                var target = new Panel
                {
                    ContextMenu = sut
                };

                var window = new Window { Content = target };
                window.ApplyTemplate();
                window.Presenter.ApplyTemplate();

                target.ContextMenu = null;

               Assert.ThrowsAny<Exception>(()=> sut.Open());
            }
        }

        [Fact]
        public void Closing_Raises_Single_Closed_Event()
        {
            using (Application())
            {
                var sut = new ContextMenu();
                var target = new Panel
                {
                    ContextMenu = sut
                };

                var window = new Window { Content = target };
                window.ApplyTemplate();
                window.Presenter.ApplyTemplate();

                sut.Open(target);

                int closedCount = 0;

                sut.MenuClosed += (sender, args) =>
                {
                    closedCount++;
                };

                sut.Close();

                Assert.Equal(1, closedCount);
            }
        }

        [Fact]
        public void Clicking_On_Control_Toggles_ContextMenu()
        {
            using (Application())
            {
                popupImpl.Setup(x => x.Show(true, false)).Verifiable();
                popupImpl.Setup(x => x.Hide()).Verifiable();

                var sut = new ContextMenu();
                var target = new Panel
                {
                    ContextMenu = sut
                };

                var window = new Window {Content = target};
                window.ApplyTemplate();
                window.Presenter.ApplyTemplate();

                _mouse.Click(target, MouseButton.Right);

                Assert.True(sut.IsOpen);

                _mouse.Click(target);

                Assert.False(sut.IsOpen);
                popupImpl.Verify(x => x.Show(true, false), Times.Once);
                popupImpl.Verify(x => x.Hide(), Times.Once);
            }
        }

        [Fact]
        public void Right_Clicking_On_Control_Twice_Re_Opens_ContextMenu()
        {
            using (Application())
            {
                popupImpl.Setup(x => x.Show(true, false)).Verifiable();
                popupImpl.Setup(x => x.Hide()).Verifiable();

                var sut = new ContextMenu();
                var target = new Panel
                {
                    ContextMenu = sut
                };

                var window = new Window {Content = target};
                window.ApplyTemplate();
                window.Presenter.ApplyTemplate();

                _mouse.Click(target, MouseButton.Right);

                Assert.True(sut.IsOpen);

                _mouse.Click(target, MouseButton.Right);

                Assert.True(sut.IsOpen);
                popupImpl.Verify(x => x.Hide(), Times.Once);
                popupImpl.Verify(x => x.Show(true, false), Times.Exactly(2));
            }
        }
        
        [Fact]
        public void Context_Menu_Can_Be_Shared_Between_Controls_Even_After_A_Control_Is_Removed_From_Visual_Tree()
        {
            using (Application())
            {
                var sut = new ContextMenu();
                var target1 = new Panel
                {
                    ContextMenu = sut
                };

                var target2 = new Panel
                {
                    ContextMenu = sut
                };

                var sp = new StackPanel { Children = { target1, target2 } };
                var window = new Window { Content = sp };
                
                window.ApplyTemplate();
                window.Presenter.ApplyTemplate();

                _mouse.Click(target1, MouseButton.Right);

                Assert.True(sut.IsOpen);

                _mouse.Click(target2, MouseButton.Left);
                
                Assert.False(sut.IsOpen);
                
                sp.Children.Remove(target1);
                
                _mouse.Click(target2, MouseButton.Right);
                
                Assert.True(sut.IsOpen);
            }
        }

        [Fact]
        public void Cancelling_Opening_Does_Not_Show_ContextMenu()
        {
            using (Application())
            {
                popupImpl.Setup(x => x.Show(true, false)).Verifiable();

                bool eventCalled = false;
                var sut = new ContextMenu();
                var target = new Panel
                {
                    ContextMenu = sut
                };
                new Window { Content = target };

                sut.ContextMenuOpening += (c, e) => { eventCalled = true; e.Cancel = true; };

                _mouse.Click(target, MouseButton.Right);

                Assert.True(eventCalled);
                Assert.False(sut.IsOpen);
                popupImpl.Verify(x => x.Show(true, false), Times.Never);
            }
        }

        [Fact]
        public void Can_Set_Clear_ContextMenu_Property()
        {
            using (Application())
            {
                var target = new ContextMenu();
                var control = new Panel();

                control.ContextMenu = target;
                control.ContextMenu = null;
            }
        }

        [Fact]
        public void Context_Menu_In_Resources_Can_Be_Shared()
        {
            using (Application())
            {
                var xaml = @"
<Window xmlns='https://github.com/avaloniaui'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <Window.Resources>
        <ContextMenu x:Key='contextMenu'>
            <MenuItem>Foo</MenuItem>
        </ContextMenu>
	</Window.Resources>

    <StackPanel>
        <TextBlock Name='target1' ContextMenu='{StaticResource contextMenu}'/>
        <TextBlock Name='target2' ContextMenu='{StaticResource contextMenu}'/>
    </StackPanel>
</Window>";

                var window = (Window)AvaloniaRuntimeXamlLoader.Load(xaml);
                var target1 = window.Find<TextBlock>("target1");
                var target2 = window.Find<TextBlock>("target2");
                var mouse = new MouseTestHelper();

                Assert.NotNull(target1.ContextMenu);
                Assert.NotNull(target2.ContextMenu);
                Assert.Same(target1.ContextMenu, target2.ContextMenu);

                window.Show();

                var menu = target1.ContextMenu;
                mouse.Click(target1, MouseButton.Right);
                Assert.True(menu.IsOpen);
                mouse.Click(target2, MouseButton.Right);
                Assert.True(menu.IsOpen);
            }
        }

        [Fact]
        public void Context_Menu_Can_Be_Set_In_Style()
        {
            using (Application())
            {
                var xaml = @"
<Window xmlns='https://github.com/avaloniaui'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <Window.Styles>
        <Style Selector='TextBlock'>
            <Setter Property='ContextMenu'>
                <ContextMenu>
                    <MenuItem>Foo</MenuItem>
                </ContextMenu>
            </Setter>
        </Style>
	</Window.Styles>

    <StackPanel>
        <TextBlock Name='target1'/>
        <TextBlock Name='target2'/>
    </StackPanel>
</Window>";

                var window = (Window)AvaloniaRuntimeXamlLoader.Load(xaml);
                var target1 = window.Find<TextBlock>("target1");
                var target2 = window.Find<TextBlock>("target2");
                var mouse = new MouseTestHelper();

                Assert.NotNull(target1.ContextMenu);
                Assert.NotNull(target2.ContextMenu);
                Assert.Same(target1.ContextMenu, target2.ContextMenu);

                window.Show();

                var menu = target1.ContextMenu;
                mouse.Click(target1, MouseButton.Right);
                Assert.True(menu.IsOpen);
                mouse.Click(target2, MouseButton.Right);
                Assert.True(menu.IsOpen);
            }
        }

        [Fact(Skip = "The only reason this test was 'passing' before was that the author forgot to call Window.ApplyTemplate()")]
        public void Cancelling_Closing_Leaves_ContextMenuOpen()
        {
            using (Application())
            {
                popupImpl.Setup(x => x.Show(true, false)).Verifiable();
                popupImpl.Setup(x => x.Hide()).Verifiable();

                bool eventCalled = false;
                var sut = new ContextMenu();
                var target = new Panel
                {
                    ContextMenu = sut
                };
                
                var window = new Window {Content = target};
                window.ApplyTemplate();

                sut.ContextMenuClosing += (c, e) => { eventCalled = true; e.Cancel = true; };

                _mouse.Click(target, MouseButton.Right);

                Assert.True(sut.IsOpen);

                _mouse.Click(target, MouseButton.Right);

                Assert.True(eventCalled);
                Assert.True(sut.IsOpen);

                popupImpl.Verify(x => x.Show(true, false), Times.Once());
                popupImpl.Verify(x => x.Hide(), Times.Never);
            }
        }

        private IDisposable Application()
        {
            var screen = new PixelRect(new PixelPoint(), new PixelSize(100, 100));
            var screenImpl = new Mock<IScreenImpl>();
            screenImpl.Setup(x => x.ScreenCount).Returns(1);
            screenImpl.Setup(X => X.AllScreens).Returns( new[] { new Screen(1, screen, screen, true) });

            var windowImpl = MockWindowingPlatform.CreateWindowMock();
            popupImpl = MockWindowingPlatform.CreatePopupMock(windowImpl.Object);
            popupImpl.SetupGet(x => x.RenderScaling).Returns(1);
            windowImpl.Setup(x => x.CreatePopup()).Returns(popupImpl.Object);

            windowImpl.Setup(x => x.Screen).Returns(screenImpl.Object);

            var services = TestServices.StyledWindow.With(
                                        inputManager: new InputManager(),
                                        windowImpl: windowImpl.Object,
                                        windowingPlatform: new MockWindowingPlatform(() => windowImpl.Object, x => popupImpl.Object));

            return UnitTestApplication.Start(services);
        }
    }
}
