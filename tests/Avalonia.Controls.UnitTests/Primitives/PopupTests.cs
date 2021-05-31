using System;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using Moq;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Layout;
using Avalonia.LogicalTree;
using Avalonia.Platform;
using Avalonia.Styling;
using Avalonia.UnitTests;
using Avalonia.VisualTree;
using Xunit;
using Avalonia.Input;
using Avalonia.Rendering;

namespace Avalonia.Controls.UnitTests.Primitives
{
    public class PopupTests
    {
        protected bool UsePopupHost;

        [Fact]
        public void Popup_Open_Without_Target_Should_Attach_Itself_Later()
        {
            using (CreateServices())
            {
                int openedEvent = 0;
                var target = new Popup();
                target.Opened += (s, a) => openedEvent++;
                target.IsOpen = true;

                var window = PreparedWindow(target);
                window.Show();
                Assert.Equal(1, openedEvent);
            }
        }

        [Fact]
        public void Popup_Without_TopLevel_Shouldnt_Call_Open()
        {
            int openedEvent = 0;
            var target = new Popup();
            target.Opened += (s, a) => openedEvent++;
            target.IsOpen = true;

            Assert.Equal(0, openedEvent);
        }

        [Fact]
        public void Opening_Popup_Shouldnt_Throw_When_Not_In_Visual_Tree()
        {
            var target = new Popup();
            target.IsOpen = true;
        }

        [Fact]
        public void Opening_Popup_Shouldnt_Throw_When_In_Tree_Without_TopLevel()
        {
            Control c = new Control();
            var target = new Popup();
            ((ISetLogicalParent)target).SetParent(c);
            target.IsOpen = true;
        }

        [Fact]
        public void Setting_Child_Should_Set_Child_Controls_LogicalParent()
        {
            var target = new Popup();
            var child = new Control();

            target.Child = child;

            Assert.Equal(child.Parent, target);
            Assert.Equal(((ILogical)child).LogicalParent, target);
        }

        [Fact]
        public void Clearing_Child_Should_Clear_Child_Controls_Parent()
        {
            var target = new Popup();
            var child = new Control();

            target.Child = child;
            target.Child = null;

            Assert.Null(child.Parent);
            Assert.Null(((ILogical)child).LogicalParent);
        }

        [Fact]
        public void Child_Control_Should_Appear_In_LogicalChildren()
        {
            var target = new Popup();
            var child = new Control();

            target.Child = child;

            Assert.Equal(new[] { child }, target.GetLogicalChildren());
        }

        [Fact]
        public void Clearing_Child_Should_Remove_From_LogicalChildren()
        {
            var target = new Popup();
            var child = new Control();

            target.Child = child;
            target.Child = null;

            Assert.Equal(new ILogical[0], ((ILogical)target).LogicalChildren.ToList());
        }

        [Fact]
        public void Setting_Child_Should_Fire_LogicalChildren_CollectionChanged()
        {
            var target = new Popup();
            var child = new Control();
            var called = false;

            ((ILogical)target).LogicalChildren.CollectionChanged += (s, e) =>
                called = e.Action == NotifyCollectionChangedAction.Add;

            target.Child = child;

            Assert.True(called);
        }

        [Fact]
        public void Clearing_Child_Should_Fire_LogicalChildren_CollectionChanged()
        {
            var target = new Popup();
            var child = new Control();
            var called = false;

            target.Child = child;

            ((ILogical)target).LogicalChildren.CollectionChanged += (s, e) =>
                called = e.Action == NotifyCollectionChangedAction.Remove;

            target.Child = null;

            Assert.True(called);
        }

        [Fact]
        public void Changing_Child_Should_Fire_LogicalChildren_CollectionChanged()
        {
            var target = new Popup();
            var child1 = new Control();
            var child2 = new Control();
            var called = false;

            target.Child = child1;

            ((ILogical)target).LogicalChildren.CollectionChanged += (s, e) => called = true;

            target.Child = child2;

            Assert.True(called);
        }

        [Fact]
        public void Setting_Child_Should_Not_Set_Childs_VisualParent()
        {
            var target = new Popup();
            var child = new Control();

            target.Child = child;

            Assert.Null(((IVisual)child).VisualParent);
        }

        [Fact]
        public void PopupRoot_Should_Initially_Be_Null()
        {
            using (CreateServices())
            {
                var target = new Popup();

                Assert.Null(((Visual)target.Host));
            }
        }

        [Fact]
        public void PopupRoot_Should_Have_Popup_As_LogicalParent()
        {
            using (CreateServices())
            {
                var target = new Popup() {PlacementTarget = PreparedWindow()};

                target.Open();

                Assert.Equal(target, ((Visual)target.Host).Parent);
                Assert.Equal(target, ((Visual)target.Host).GetLogicalParent());
            }
        }

        [Fact]
        public void PopupRoot_Should_Be_Detached_From_Logical_Tree_When_Popup_Is_Detached()
        {
            using (CreateServices())
            {
                var target = new Popup() {PlacementMode = PlacementMode.Pointer};
                var root = PreparedWindow(target);

                target.Open();

                var popupRoot = (ILogical)((Visual)target.Host);

                Assert.True(popupRoot.IsAttachedToLogicalTree);
                root.Content = null;
                Assert.False(((ILogical)target).IsAttachedToLogicalTree);
            }
        }

        [Fact]
        public void Popup_Open_Should_Raise_Single_Opened_Event()
        {
            using (CreateServices())
            {
                var window = PreparedWindow();
                var target = new Popup() {PlacementMode = PlacementMode.Pointer};

                window.Content = target;

                int openedCount = 0;

                target.Opened += (sender, args) =>
                {
                    openedCount++;
                };

                target.Open();

                Assert.Equal(1, openedCount);
            }
        }

        [Fact]
        public void Popup_Close_Should_Raise_Single_Closed_Event()
        {
            using (CreateServices())
            {
                var window = PreparedWindow();
                var target = new Popup() {PlacementMode = PlacementMode.Pointer};

                window.Content = target;
                window.ApplyTemplate();
                target.Open();

                int closedCount = 0;

                target.Closed += (sender, args) =>
                {
                    closedCount++;
                };

                target.Close();

                Assert.Equal(1, closedCount);
            }
        }

        [Fact]
        public void Popup_Close_On_Closed_Popup_Should_Not_Raise_Closed_Event()
        {
            using (CreateServices())
            {
                var window = PreparedWindow();
                var target = new Popup() { PlacementMode = PlacementMode.Pointer };

                window.Content = target;
                window.ApplyTemplate();
                
                int closedCount = 0;

                target.Closed += (sender, args) =>
                {
                    closedCount++;
                };

                target.Close();
                target.Close();
                target.Close();
                target.Close();

                Assert.Equal(0, closedCount);
            }
        }

        [Fact]
        public void Templated_Control_With_Popup_In_Template_Should_Set_TemplatedParent()
        {
            using (CreateServices())
            {
                PopupContentControl target;
                var root = PreparedWindow(target = new PopupContentControl
                {
                    Content = new Border(),
                    Template = new FuncControlTemplate<PopupContentControl>(PopupContentControlTemplate),
                });
                root.Show();

                target.ApplyTemplate();

                var popup = (Popup)target.GetTemplateChildren().First(x => x.Name == "popup");
                popup.Open();

                var popupRoot = (Control)popup.Host;
                popupRoot.Measure(Size.Infinity);
                popupRoot.Arrange(new Rect(popupRoot.DesiredSize));

                var children = popupRoot.GetVisualDescendants().ToList();
                var types = children.Select(x => x.GetType().Name).ToList();

                Assert.Equal(
                    new[]
                    {
                        "Panel",
                        "Border",
                        "VisualLayerManager",
                        "ContentPresenter",
                        "ContentPresenter",
                        "Border",
                    },
                    types);

                var templatedParents = children
                    .OfType<IControl>()
                    .Select(x => x.TemplatedParent).ToList();

                Assert.Equal(
                    new object[]
                    {
                        popupRoot,
                        popupRoot,
                        popupRoot,
                        popupRoot,
                        target,
                        null,
                    },
                    templatedParents);
            }
        }

        [Fact]
        public void DataContextBeginUpdate_Should_Not_Be_Called_For_Controls_That_Dont_Inherit()
        {
            using (CreateServices())
            {
                TestControl child;
                var popup = new Popup
                {
                    Child = child = new TestControl(),
                    DataContext = "foo",
                    PlacementTarget = PreparedWindow()
                };

                var beginCalled = false;
                child.DataContextBeginUpdate += (s, e) => beginCalled = true;

                // Test for #1245. Here, the child's logical parent is the popup but it's not yet
                // attached to a visual tree because the popup hasn't been opened.
                Assert.Same(popup, ((ILogical)child).LogicalParent);
                Assert.Same(popup, child.InheritanceParent);
                Assert.Null(child.GetVisualRoot());

                popup.Open();

                // #1245 was caused by the fact that DataContextBeginUpdate was called on `target`
                // when the PopupRoot was created, even though PopupRoot isn't the
                // InheritanceParent of child.
                Assert.False(beginCalled);
            }
        }
        
        [Fact]
        public void Popup_Host_Type_Should_Match_Platform_Preference()
        {
            using (CreateServices())
            {
                var target = new Popup() {PlacementTarget = PreparedWindow()};

                target.Open();
                if (UsePopupHost)
                    Assert.IsType<OverlayPopupHost>(target.Host);
                else
                    Assert.IsType<PopupRoot>(target.Host);
            }
        }

        [Fact]
        public void OverlayDismissEventPassThrough_Should_Pass_Event_To_Window_Contents()
        {
            using (CreateServices())
            {
                var renderer = new Mock<IRenderer>();
                var platform = AvaloniaLocator.Current.GetService<IWindowingPlatform>();
                var windowImpl = Mock.Get(platform.CreateWindow());
                windowImpl.Setup(x => x.CreateRenderer(It.IsAny<IRenderRoot>())).Returns(renderer.Object);

                var window = new Window(windowImpl.Object);
                window.ApplyTemplate();

                var target = new Popup() 
                { 
                    PlacementTarget = window ,
                    IsLightDismissEnabled = true,
                    OverlayDismissEventPassThrough = true,
                };

                var raised = 0;
                var border = new Border();
                window.Content = border;

                renderer.Setup(x =>
                    x.HitTestFirst(new Point(10, 15), window, It.IsAny<Func<IVisual, bool>>()))
                    .Returns(border);

                border.PointerPressed += (s, e) =>
                {
                    Assert.Same(border, e.Source);
                    ++raised;
                };

                target.Open();
                Assert.True(target.IsOpen);

                var e = CreatePointerPressedEventArgs(window, new Point(10, 15));
                var overlay = LightDismissOverlayLayer.GetLightDismissOverlayLayer(window);
                overlay.RaiseEvent(e);

                Assert.Equal(1, raised);
                Assert.False(target.IsOpen);
            }
        }

        [Fact]
        public void Focusable_Controls_In_Popup_Should_Get_Focus()
        {
            using (CreateServicesWithFocus())
            {
                var window = PreparedWindow();

                var tb = new TextBox();
                var b = new Button();
                var p = new Popup
                {
                    PlacementTarget = window,
                    Child = new StackPanel
                    {
                        Children =
                        {
                            tb,
                            b
                        }
                    }
                };
                ((ISetLogicalParent)p).SetParent(p.PlacementTarget);
                window.Show();

                p.Open();

                if(p.Host is OverlayPopupHost host)
                {
                    //Need to measure/arrange for visual children to show up
                    //in OverlayPopupHost
                    host.Measure(Size.Infinity);
                    host.Arrange(new Rect(host.DesiredSize));
                }

                tb.Focus();

                Assert.True(FocusManager.Instance?.Current == tb);

                //Ensure focus remains in the popup
                var nextFocus = KeyboardNavigationHandler.GetNext(FocusManager.Instance.Current, NavigationDirection.Next);

                Assert.True(nextFocus == b);

                p.Close();
            }
        }

        [Fact]
        public void Closing_Popup_Sets_Focus_On_PlacementTarget()
        {
            using (CreateServicesWithFocus())
            {
                var window = PreparedWindow();
                window.Focusable = true;

                var tb = new TextBox();
                var p = new Popup
                {
                    PlacementTarget = window,
                    Child = tb
                };
                ((ISetLogicalParent)p).SetParent(p.PlacementTarget);
                window.Show();

                p.Open();

                if (p.Host is OverlayPopupHost host)
                {
                    //Need to measure/arrange for visual children to show up
                    //in OverlayPopupHost
                    host.Measure(Size.Infinity);
                    host.Arrange(new Rect(host.DesiredSize));
                }

                tb.Focus();

                p.Close();

                var focus = FocusManager.Instance?.Current;
                Assert.True(focus == window);
            }
        }

        [Fact]
        public void Prog_Close_Popup_NoLightDismiss_Doesnt_Move_Focus_To_PlacementTarget()
        {
            using (CreateServicesWithFocus())
            {
                var window = PreparedWindow();

                var windowTB = new TextBox();
                window.Content = windowTB;

                var popupTB = new TextBox();
                var p = new Popup
                {
                    PlacementTarget = window,
                    IsLightDismissEnabled = false,
                    Child = popupTB
                };
                ((ISetLogicalParent)p).SetParent(p.PlacementTarget);
                window.Show();

                p.Open();

                if (p.Host is OverlayPopupHost host)
                {
                    //Need to measure/arrange for visual children to show up
                    //in OverlayPopupHost
                    host.Measure(Size.Infinity);
                    host.Arrange(new Rect(host.DesiredSize));
                }

                popupTB.Focus();

                windowTB.Focus();

                var focus = FocusManager.Instance?.Current;

                Assert.True(focus == windowTB);

                p.Close();

                Assert.True(focus == windowTB);
            }
        }

        private IDisposable CreateServices()
        {
            return UnitTestApplication.Start(TestServices.StyledWindow.With(windowingPlatform:
                new MockWindowingPlatform(null,
                    x =>
                    {
                        if(UsePopupHost)
                            return null;
                        return MockWindowingPlatform.CreatePopupMock(x).Object;
                    })));
        }

        private IDisposable CreateServicesWithFocus()
        {
            return UnitTestApplication.Start(TestServices.StyledWindow.With(windowingPlatform:
                new MockWindowingPlatform(null,
                    x =>
                    {
                        if (UsePopupHost)
                            return null;
                        return MockWindowingPlatform.CreatePopupMock(x).Object;
                    }), 
                    focusManager: new FocusManager(),
                    keyboardDevice: () => new KeyboardDevice()));
        }

       
        private PointerPressedEventArgs CreatePointerPressedEventArgs(Window source, Point p)
        {
            var pointer = new Pointer(Pointer.GetNextFreeId(), PointerType.Mouse, true);
            return new PointerPressedEventArgs(
                source,
                pointer,
                source,
                p,
                0,
                new PointerPointProperties(RawInputModifiers.None, PointerUpdateKind.LeftButtonPressed),
                KeyModifiers.None);
        }

        private Window PreparedWindow(object content = null)
        {
            var w = new Window { Content = content };
            w.ApplyTemplate();
            return w;
        }

        private static IControl PopupContentControlTemplate(PopupContentControl control, INameScope scope)
        {
            return new Popup
            {
                Name = "popup",
                PlacementTarget = control,
                Child = new ContentPresenter
                {
                    [~ContentPresenter.ContentProperty] = control[~ContentControl.ContentProperty],
                }
            }.RegisterInNameScope(scope);
        }

        private class PopupContentControl : ContentControl
        {
        }

        private class TestControl : Decorator
        {
            public event EventHandler DataContextBeginUpdate;

            public new IAvaloniaObject InheritanceParent => base.InheritanceParent;

            protected override void OnDataContextBeginUpdate()
            {
                DataContextBeginUpdate?.Invoke(this, EventArgs.Empty);
                base.OnDataContextBeginUpdate();
            }
        }
    }

    public class PopupTestsWithPopupRoot : PopupTests
    {
        public PopupTestsWithPopupRoot()
        {
            UsePopupHost = true;
        }
    }
}
