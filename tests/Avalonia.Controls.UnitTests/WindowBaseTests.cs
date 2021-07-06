using System;
using System.Reactive;
using System.Reactive.Subjects;
using Moq;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.Layout;
using Avalonia.Platform;
using Avalonia.Rendering;
using Avalonia.Styling;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Controls.UnitTests
{
    public class WindowBaseTests
    {
        [Fact]
        public void Activate_Should_Call_Impl_Activate()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var impl = new Mock<IWindowBaseImpl>();
                var target = new TestWindowBase(impl.Object);

                target.Activate();

                impl.Verify(x => x.Activate());
            }
        }

        [Fact]
        public void Impl_Activate_Should_Call_Raise_Activated_Event()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var impl = new Mock<IWindowBaseImpl>();
                impl.SetupAllProperties();

                bool raised = false;
                var target = new TestWindowBase(impl.Object);
                target.Activated += (s, e) => raised = true;

                impl.Object.Activated();

                Assert.True(raised);
            }
        }


        [Fact]
        public void Impl_Deactivate_Should_Call_Raise_Deativated_Event()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var impl = new Mock<IWindowBaseImpl>();
                impl.SetupAllProperties();

                bool raised = false;
                var target = new TestWindowBase(impl.Object);
                target.Deactivated += (s, e) => raised = true;

                impl.Object.Deactivated();

                Assert.True(raised);
            }
        }

        [Fact]
        public void IsVisible_Should_Initially_Be_False()
        {
            using (UnitTestApplication.Start(TestServices.MockWindowingPlatform))
            {
                var target = new TestWindowBase();

                Assert.False(target.IsVisible);
            }
        }

        [Fact]
        public void IsVisible_Should_Be_True_After_Show()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var target = new TestWindowBase();

                target.Show();

                Assert.True(target.IsVisible);
            }
        }

        [Fact]
        public void IsVisible_Should_Be_False_Atfer_Hide()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var target = new TestWindowBase();

                target.Show();
                target.Hide();

                Assert.False(target.IsVisible);
            }
        }

        [Fact]
        public void IsVisible_Should_Be_False_Atfer_Impl_Signals_Close()
        {
            var windowImpl = new Mock<IPopupImpl>();
            windowImpl.Setup(x => x.DesktopScaling).Returns(1);
            windowImpl.Setup(x => x.RenderScaling).Returns(1);
            windowImpl.SetupProperty(x => x.Closed);

            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var target = new TestWindowBase(windowImpl.Object);

                target.Show();
                windowImpl.Object.Closed();

                Assert.False(target.IsVisible);
            }
        }

        [Fact]
        public void Setting_IsVisible_True_Shows_Window()
        {
            var windowImpl = new Mock<IPopupImpl>();
            windowImpl.Setup(x => x.DesktopScaling).Returns(1);
            windowImpl.Setup(x => x.RenderScaling).Returns(1);

            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var target = new TestWindowBase(windowImpl.Object);
                target.IsVisible = true;

                windowImpl.Verify(x => x.Show(true, false));
            }
        }

        [Fact]
        public void Setting_IsVisible_False_Hides_Window()
        {
            var windowImpl = new Mock<IPopupImpl>();
            windowImpl.Setup(x => x.DesktopScaling).Returns(1);
            windowImpl.Setup(x => x.RenderScaling).Returns(1);

            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var target = new TestWindowBase(windowImpl.Object);
                target.Show();
                target.IsVisible = false;

                windowImpl.Verify(x => x.Hide());
            }
        }

        [Fact]
        public void Showing_Should_Start_Renderer()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var renderer = new Mock<IRenderer>();
                var target = new TestWindowBase(renderer.Object);

                target.Show();

                renderer.Verify(x => x.Start(), Times.Once);
            }
        }

        [Fact]
        public void Showing_Should_Raise_Opened()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var target = new TestWindowBase();
                var raised = false;

                target.Opened += (s, e) => raised = true;

                target.Show();

                Assert.True(raised);
            }
        }

        [Fact]
        public void Hiding_Should_Stop_Renderer()
        {

            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var renderer = new Mock<IRenderer>();
                var target = new TestWindowBase(renderer.Object);

                target.Show();
                target.Hide();

                renderer.Verify(x => x.Stop(), Times.Once);
            }
        }

        [Fact]
        public void Renderer_Should_Be_Disposed_When_Impl_Signals_Close()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var renderer = new Mock<IRenderer>();
                var windowImpl = new Mock<IPopupImpl>();
                windowImpl.Setup(x => x.DesktopScaling).Returns(1);
                windowImpl.Setup(x => x.RenderScaling).Returns(1);
                windowImpl.SetupProperty(x => x.Closed);
                windowImpl.Setup(x => x.CreateRenderer(It.IsAny<IRenderRoot>())).Returns(renderer.Object);

                var target = new TestWindowBase(windowImpl.Object);

                target.Show();
                windowImpl.Object.Closed();

                renderer.Verify(x => x.Dispose(), Times.Once);
            }
        }

        private FuncControlTemplate<TestWindowBase> CreateTemplate()
        {
            return new FuncControlTemplate<TestWindowBase>((x, scope) =>
                new ContentPresenter
                {
                    Name = "PART_ContentPresenter",
                    [!ContentPresenter.ContentProperty] = x[!ContentControl.ContentProperty],
                }.RegisterInNameScope(scope));
        }

        private class TestWindowBase : WindowBase
        {
            public bool IsClosed { get; private set; }

            public TestWindowBase(IRenderer renderer = null)
                : base(Mock.Of<IWindowBaseImpl>(x => 
                    x.RenderScaling == 1 &&
                    x.CreateRenderer(It.IsAny<IRenderRoot>()) == renderer))
            {
            }

            public TestWindowBase(IWindowBaseImpl impl)
                : base(impl)
            {
            }
        }
    }
}
