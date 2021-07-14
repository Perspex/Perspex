using System;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.Layout;
using Avalonia.LogicalTree;
using Avalonia.Platform;
using Avalonia.Styling;
using Avalonia.UnitTests;
using Moq;
using Xunit;

namespace Avalonia.Controls.UnitTests
{
    public class TopLevelTests
    {
        [Fact]
        public void IsAttachedToLogicalTree_Is_True()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var impl = new Mock<ITopLevelImpl>();
                var target = new TestTopLevel(impl.Object);

                Assert.True(((ILogical)target).IsAttachedToLogicalTree);
            }
        }

        [Fact]
        public void ClientSize_Should_Be_Set_On_Construction()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var impl = new Mock<ITopLevelImpl>();
                impl.Setup(x => x.ClientSize).Returns(new Size(123, 456));

                var target = new TestTopLevel(impl.Object);

                Assert.Equal(new Size(123, 456), target.ClientSize);
            }
        }

        [Fact]
        public void Width_Should_Not_Be_Set_On_Construction()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var impl = new Mock<ITopLevelImpl>();
                impl.Setup(x => x.ClientSize).Returns(new Size(123, 456));

                var target = new TestTopLevel(impl.Object);

                Assert.Equal(double.NaN, target.Width);
            }
        }

        [Fact]
        public void Height_Should_Not_Be_Set_On_Construction()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var impl = new Mock<ITopLevelImpl>();
                impl.Setup(x => x.ClientSize).Returns(new Size(123, 456));

                var target = new TestTopLevel(impl.Object);

                Assert.Equal(double.NaN, target.Height);
            }
        }

        [Fact]
        public void Layout_Pass_Should_Not_Be_Automatically_Scheduled()
        {
            var services = TestServices.StyledWindow;

            using (UnitTestApplication.Start(services))
            {
                var impl = new Mock<ITopLevelImpl>();
                
                var target = new TestTopLevel(impl.Object, Mock.Of<ILayoutManager>());

                // The layout pass should be scheduled by the derived class.
                var layoutManagerMock = Mock.Get(target.LayoutManager);
                layoutManagerMock.Verify(x => x.ExecuteLayoutPass(), Times.Never);
            }
        }

        [Fact]
        public void Bounds_Should_Be_Set_After_Layout_Pass()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var impl = new Mock<ITopLevelImpl>();
                impl.SetupProperty(x => x.Resized);
                impl.SetupGet(x => x.RenderScaling).Returns(1);

                var target = new TestTopLevel(impl.Object)
                {
                    IsVisible = true,
                    Template = CreateTemplate(),
                    Content = new TextBlock
                    {
                        Width = 321,
                        Height = 432,
                    }
                };

                target.LayoutManager.ExecuteInitialLayoutPass();

                Assert.Equal(new Rect(0, 0, 321, 432), target.Bounds);
            }
        }

        [Fact]
        public void Width_And_Height_Should_Not_Be_Set_After_Layout_Pass()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var impl = new Mock<ITopLevelImpl>();
                impl.Setup(x => x.ClientSize).Returns(new Size(123, 456));

                var target = new TestTopLevel(impl.Object);
                target.LayoutManager.ExecuteLayoutPass();

                Assert.Equal(double.NaN, target.Width);
                Assert.Equal(double.NaN, target.Height);
            }
        }

        [Fact]
        public void Width_And_Height_Should_Be_Set_After_Window_Resize_Notification()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var impl = new Mock<ITopLevelImpl>();
                impl.SetupAllProperties();
                impl.Setup(x => x.ClientSize).Returns(new Size(123, 456));

                // The user has resized the window, so we can no longer auto-size.
                var target = new TestTopLevel(impl.Object);
                impl.Object.Resized(new Size(100, 200), PlatformResizeReason.Unspecified);

                Assert.Equal(100, target.Width);
                Assert.Equal(200, target.Height);
            }
        }

        [Fact]
        public void Impl_Close_Should_Call_Raise_Closed_Event()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var impl = new Mock<ITopLevelImpl>();
                impl.SetupAllProperties();

                bool raised = false;
                var target = new TestTopLevel(impl.Object);
                target.Closed += (s, e) => raised = true;

                impl.Object.Closed();

                Assert.True(raised);
            }
        }

        [Fact]
        public void Impl_Close_Should_Raise_DetachedFromLogicalTree_Event()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var impl = new Mock<ITopLevelImpl>();
                impl.SetupAllProperties();

                var target = new TestTopLevel(impl.Object);
                var raised = 0;

                target.DetachedFromLogicalTree += (s, e) =>
                {
                    Assert.Same(target, e.Root);
                    Assert.Same(target, e.Source);
                    Assert.Null(e.Parent);
                    ++raised;
                };

                impl.Object.Closed();

                Assert.Equal(1, raised);
            }
        }

        [Fact]
        public void Impl_Input_Should_Pass_Input_To_InputManager()
        {
            var inputManagerMock = new Mock<IInputManager>();
            var services = TestServices.StyledWindow.With(inputManager: inputManagerMock.Object);

            using (UnitTestApplication.Start(services))
            {
                var impl = new Mock<ITopLevelImpl>();
                impl.SetupAllProperties();

                var target = new TestTopLevel(impl.Object);

                var input = new RawKeyEventArgs(
                    new Mock<IKeyboardDevice>().Object,
                    0,
                    target,
                    RawKeyEventType.KeyDown,
                    Key.A, RawInputModifiers.None);
                impl.Object.Input(input);

                inputManagerMock.Verify(x => x.ProcessInput(input));
            }
        }

        [Fact]
        public void Adding_Top_Level_As_Child_Should_Throw_Exception()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var impl = new Mock<ITopLevelImpl>();
                impl.SetupAllProperties();
                var target = new TestTopLevel(impl.Object);
                var child = new TestTopLevel(impl.Object);

                target.Template = CreateTemplate();
                target.Content = child;
                target.ApplyTemplate();

                Assert.Throws<InvalidOperationException>(() => target.Presenter.ApplyTemplate());
            }
        }

        [Fact]
        public void Adding_Resource_To_Application_Should_Raise_ResourcesChanged()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var impl = new Mock<ITopLevelImpl>();
                impl.SetupAllProperties();
                var target = new TestTopLevel(impl.Object);
                var raised = false;

                target.ResourcesChanged += (_, __) => raised = true;
                Application.Current.Resources.Add("foo", "bar");

                Assert.True(raised);
            }
        }

        [Fact]
        public void Close_Should_Notify_MouseDevice()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var impl = new Mock<ITopLevelImpl>();
                var mouseDevice = new Mock<IMouseDevice>();
                impl.SetupAllProperties();
                impl.Setup(x => x.MouseDevice).Returns(mouseDevice.Object);

                var target = new TestTopLevel(impl.Object);

                impl.Object.Closed();

                mouseDevice.Verify(x => x.TopLevelClosed(target));
            }
        }

        [Fact]
        public void Close_Should_Dispose_LayoutManager()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var impl = new Mock<ITopLevelImpl>();
                impl.SetupAllProperties();

                var layoutManager = new Mock<ILayoutManager>();
                var target = new TestTopLevel(impl.Object, layoutManager.Object);

                impl.Object.Closed();

                layoutManager.Verify(x => x.Dispose());
            }
        }

        [Fact]
        public void Reacts_To_Changes_In_Global_Styles()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var impl = new Mock<ITopLevelImpl>();
                impl.SetupGet(x => x.RenderScaling).Returns(1);

                var child = new Border { Classes = { "foo" } };
                var target = new TestTopLevel(impl.Object)
                {
                    Template = CreateTemplate(),
                    Content = child,
                };

                target.LayoutManager.ExecuteInitialLayoutPass();

                Assert.Equal(new Thickness(0), child.BorderThickness);

                var style = new Style(x => x.OfType<Border>().Class("foo"))
                {
                    Setters =
                    {
                        new Setter(Border.BorderThicknessProperty, new Thickness(2))
                    }
                };

                Application.Current.Styles.Add(style);
                target.LayoutManager.ExecuteInitialLayoutPass();

                Assert.Equal(new Thickness(2), child.BorderThickness);

                Application.Current.Styles.Remove(style);

                Assert.Equal(new Thickness(0), child.BorderThickness);
            }
        }

        private FuncControlTemplate<TestTopLevel> CreateTemplate()
        {
            return new FuncControlTemplate<TestTopLevel>((x, scope) =>
                new ContentPresenter
                {
                    Name = "PART_ContentPresenter",
                    [!ContentPresenter.ContentProperty] = x[!ContentControl.ContentProperty],
                }.RegisterInNameScope(scope));
        }

        private class TestTopLevel : TopLevel
        {
            private readonly ILayoutManager _layoutManager;
            public bool IsClosed { get; private set; }

            public TestTopLevel(ITopLevelImpl impl, ILayoutManager layoutManager = null)
                : base(impl)
            {
                _layoutManager = layoutManager ?? new LayoutManager(this);
            }

            protected override ILayoutManager CreateLayoutManager() => _layoutManager;
        }
    }
}
