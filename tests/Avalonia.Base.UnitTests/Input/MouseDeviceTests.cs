using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.Media;
using Avalonia.Rendering;
using Avalonia.UnitTests;
using Avalonia.VisualTree;
using Moq;
using Xunit;

namespace Avalonia.Base.UnitTests.Input
{
    public class MouseDeviceTests
    {
#pragma warning disable CS0618 // Type or member is obsolete
        [Fact]
        public void Capture_Is_Transferred_To_Parent_When_Control_Removed()
        {
            Canvas control;
            var root = new TestRoot
            {
                Child = control = new Canvas(),
            };
            var target = new MouseDevice();

            target.Capture(control);
            Assert.Same(control, target.Captured);

            root.Child = null;

            Assert.Same(root, target.Captured);
        }
#pragma warning restore CS0618 // Type or member is obsolete

        [Fact]
        public void MouseMove_Should_Update_IsPointerOver()
        {
            var renderer = new Mock<IRenderer>();

            using (TestApplication(renderer.Object))
            {
                var inputManager = InputManager.Instance;

                Canvas canvas;
                Border border;
                Decorator decorator;

                var root = new TestRoot
                {
                    MouseDevice = new MouseDevice(),
                    Renderer = renderer.Object,
                    Child = new Panel
                    {
                        Children =
                        {
                            (canvas = new Canvas()),
                            (border = new Border
                            {
                                Child = decorator = new Decorator(),
                            })
                        }
                    }
                };

                SetHit(renderer, decorator);
                SendMouseMove(inputManager, root);

                Assert.True(decorator.IsPointerOver);
                Assert.True(border.IsPointerOver);
                Assert.False(canvas.IsPointerOver);
                Assert.True(root.IsPointerOver);

                SetHit(renderer, canvas);
                SendMouseMove(inputManager, root);

                Assert.False(decorator.IsPointerOver);
                Assert.False(border.IsPointerOver);
                Assert.True(canvas.IsPointerOver);
                Assert.True(root.IsPointerOver);
            }
        }

        [Fact]
        public void IsPointerOver_Should_Be_Updated_When_Child_Sets_Handled_True()
        {
            var renderer = new Mock<IRenderer>();

            using (TestApplication(renderer.Object))
            {
                var inputManager = InputManager.Instance;

                Canvas canvas;
                Border border;
                Decorator decorator;

                var root = new TestRoot
                {
                    MouseDevice = new MouseDevice(),
                    Renderer = renderer.Object,
                    Child = new Panel
                    {
                        Children =
                        {
                            (canvas = new Canvas()),
                            (border = new Border
                            {
                                Child = decorator = new Decorator(),
                            })
                        }
                    }
                };

                SetHit(renderer, canvas);
                SendMouseMove(inputManager, root);

                Assert.False(decorator.IsPointerOver);
                Assert.False(border.IsPointerOver);
                Assert.True(canvas.IsPointerOver);
                Assert.True(root.IsPointerOver);

                // Ensure that e.Handled is reset between controls.
                decorator.PointerEnter += (s, e) => e.Handled = true;

                SetHit(renderer, decorator);
                SendMouseMove(inputManager, root);

                Assert.True(decorator.IsPointerOver);
                Assert.True(border.IsPointerOver);
                Assert.False(canvas.IsPointerOver);
                Assert.True(root.IsPointerOver);
            }
        }

        [Fact]
        public void PointerEnter_Leave_Should_Be_Raised_In_Correct_Order()
        {
            var renderer = new Mock<IRenderer>();
            var result = new List<(object, string)>();

            void HandleEvent(object sender, PointerEventArgs e)
            {
                result.Add((sender, e.RoutedEvent.Name));
            }

            using (TestApplication(renderer.Object))
            {
                var inputManager = InputManager.Instance;

                Canvas canvas;
                Border border;
                Decorator decorator;

                var root = new TestRoot
                {
                    MouseDevice = new MouseDevice(),
                    Renderer = renderer.Object,
                    Child = new Panel
                    {
                        Children =
                        {
                            (canvas = new Canvas()),
                            (border = new Border
                            {
                                Child = decorator = new Decorator(),
                            })
                        }
                    }
                };

                SetHit(renderer, canvas);
                SendMouseMove(inputManager, root);

                AddEnterLeaveHandlers(HandleEvent, root, canvas, border, decorator);
                SetHit(renderer, decorator);
                SendMouseMove(inputManager, root);

                Assert.Equal(
                    new[]
                    {
                        ((object)canvas, "PointerLeave"),
                        ((object)decorator, "PointerEnter"),
                        ((object)border, "PointerEnter"),
                    },
                    result);
            }
        }


        [Fact]
        public void GetPosition_Should_Respect_Control_RenderTransform()
        {
            var renderer = new Mock<IRenderer>();

            using (TestApplication(renderer.Object))
            {
                var inputManager = InputManager.Instance;

                var root = new TestRoot
                {
                    MouseDevice = new MouseDevice(),
                    Child = new Border
                    {
                        Background = Brushes.Black,
                        RenderTransform = new TranslateTransform(10, 0),
                    }
                };

                SendMouseMove(inputManager, root, new Point(11, 11));

#pragma warning disable CS0618 // Type or member is obsolete
                var result = root.MouseDevice.GetPosition(root.Child);
#pragma warning restore CS0618 // Type or member is obsolete
                Assert.Equal(new Point(1, 11), result);
            }
        }

        private void AddEnterLeaveHandlers(
            EventHandler<PointerEventArgs> handler,
            params IControl[] controls)
        {
            foreach (var c in controls)
            {
                c.PointerEnter += handler;
                c.PointerLeave += handler;
            }
        }

        private void SendMouseMove(IInputManager inputManager, TestRoot root, Point p = new Point())
        {
            inputManager.ProcessInput(new RawPointerEventArgs(
                root.MouseDevice,
                0,
                root,
                RawPointerEventType.Move,
                p,
                RawInputModifiers.None));
        }

        private void SetHit(Mock<IRenderer> renderer, IControl hit)
        {
            renderer.Setup(x => x.HitTest(It.IsAny<Point>(), It.IsAny<IVisual>(), It.IsAny<Func<IVisual, bool>>()))
                .Returns(new[] { hit });

            renderer.Setup(x => x.HitTestFirst(It.IsAny<Point>(), It.IsAny<IVisual>(), It.IsAny<Func<IVisual, bool>>()))
                .Returns(hit);
        }

        private IDisposable TestApplication(IRenderer renderer)
        {
            return UnitTestApplication.Start(
                new TestServices(inputManager: new InputManager()));
        }
    }
}
