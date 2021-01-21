using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Contexts;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Controls.Templates;
using Avalonia.Diagnostics;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Rendering;
using Avalonia.Styling;
using Avalonia.UnitTests;
using Avalonia.VisualTree;
using JetBrains.dotMemoryUnit;
using Moq;
using Xunit;
using Xunit.Abstractions;

namespace Avalonia.LeakTests
{
    [DotMemoryUnit(FailIfRunWithoutSupport = false)]
    public class ControlTests
    {
        public ControlTests(ITestOutputHelper atr)
        {
            DotMemoryUnitTestOutput.SetOutputMethod(atr.WriteLine);
        }

        [Fact]
        public void Canvas_Is_Freed()
        {
            using (Start())
            {
                Func<Window> run = () =>
                {
                    var window = new Window
                    {
                        Content = new Canvas()
                    };

                    window.Show();

                    // Do a layout and make sure that Canvas gets added to visual tree.
                    window.LayoutManager.ExecuteInitialLayoutPass();
                    Assert.IsType<Canvas>(window.Presenter.Child);

                    // Clear the content and ensure the Canvas is removed.
                    window.Content = null;
                    window.LayoutManager.ExecuteLayoutPass();
                    Assert.Null(window.Presenter.Child);

                    return window;
                };

                var result = run();

                dotMemory.Check(memory =>
                    Assert.Equal(0, memory.GetObjects(where => where.Type.Is<Canvas>()).ObjectsCount));
            }
        }

        [Fact]
        public void Named_Canvas_Is_Freed()
        {
            using (Start())
            {
                Func<Window> run = () =>
                {
                    var scope = new NameScope();
                    var window = new Window
                    {
                        Content = new Canvas
                        {
                            Name = "foo"
                        }.RegisterInNameScope(scope)
                    };
                    NameScope.SetNameScope(window, scope);

                    window.Show();

                    // Do a layout and make sure that Canvas gets added to visual tree.
                    window.LayoutManager.ExecuteInitialLayoutPass();
                    Assert.IsType<Canvas>(window.Find<Canvas>("foo"));
                    Assert.IsType<Canvas>(window.Presenter.Child);

                    // Clear the content and ensure the Canvas is removed.
                    window.Content = null;
                    NameScope.SetNameScope(window, null);

                    window.LayoutManager.ExecuteLayoutPass();
                    Assert.Null(window.Presenter.Child);

                    return window;
                };

                var result = run();

                dotMemory.Check(memory =>
                    Assert.Equal(0, memory.GetObjects(where => where.Type.Is<Canvas>()).ObjectsCount));
            }
        }

        [Fact]
        public void ScrollViewer_With_Content_Is_Freed()
        {
            using (Start())
            {
                Func<Window> run = () =>
                {
                    var window = new Window
                    {
                        Content = new ScrollViewer
                        {
                            Content = new Canvas()
                        }
                    };

                    window.Show();

                    // Do a layout and make sure that ScrollViewer gets added to visual tree and its 
                    // template applied.
                    window.LayoutManager.ExecuteInitialLayoutPass();
                    Assert.IsType<ScrollViewer>(window.Presenter.Child);
                    Assert.IsType<Canvas>(((ScrollViewer)window.Presenter.Child).Presenter.Child);

                    // Clear the content and ensure the ScrollViewer is removed.
                    window.Content = null;
                    window.LayoutManager.ExecuteLayoutPass();
                    Assert.Null(window.Presenter.Child);

                    return window;
                };

                var result = run();

                dotMemory.Check(memory =>
                    Assert.Equal(0, memory.GetObjects(where => where.Type.Is<TextBox>()).ObjectsCount));
                dotMemory.Check(memory =>
                    Assert.Equal(0, memory.GetObjects(where => where.Type.Is<Canvas>()).ObjectsCount));
            }
        }

        [Fact]
        public void TextBox_Is_Freed()
        {
            using (Start())
            {
                Func<Window> run = () =>
                {
                    var window = new Window
                    {
                        Content = new TextBox()
                    };

                    window.Show();

                    // Do a layout and make sure that TextBox gets added to visual tree and its 
                    // template applied.
                    window.LayoutManager.ExecuteInitialLayoutPass();
                    Assert.IsType<TextBox>(window.Presenter.Child);
                    Assert.NotEmpty(window.Presenter.Child.GetVisualChildren());

                    // Clear the content and ensure the TextBox is removed.
                    window.Content = null;
                    window.LayoutManager.ExecuteLayoutPass();
                    Assert.Null(window.Presenter.Child);

                    return window;
                };

                var result = run();

                dotMemory.Check(memory =>
                    Assert.Equal(0, memory.GetObjects(where => where.Type.Is<TextBox>()).ObjectsCount));
            }
        }

        [Fact]
        public void TextBox_With_Xaml_Binding_Is_Freed()
        {
            using (Start())
            {
                Func<Window> run = () =>
                {
                    var window = new Window
                    {
                        DataContext = new Node { Name = "foo" },
                        Content = new TextBox()
                    };

                    var binding = new Avalonia.Data.Binding
                    {
                        Path = "Name"
                    };

                    var textBox = (TextBox)window.Content;
                    textBox.Bind(TextBox.TextProperty, binding);

                    window.Show();

                    // Do a layout and make sure that TextBox gets added to visual tree and its 
                    // Text property set.
                    window.LayoutManager.ExecuteInitialLayoutPass();
                    Assert.IsType<TextBox>(window.Presenter.Child);
                    Assert.Equal("foo", ((TextBox)window.Presenter.Child).Text);

                    // Clear the content and DataContext and ensure the TextBox is removed.
                    window.Content = null;
                    window.DataContext = null;
                    window.LayoutManager.ExecuteLayoutPass();
                    Assert.Null(window.Presenter.Child);

                    return window;
                };

                var result = run();

                dotMemory.Check(memory =>
                    Assert.Equal(0, memory.GetObjects(where => where.Type.Is<TextBox>()).ObjectsCount));
                dotMemory.Check(memory =>
                    Assert.Equal(0, memory.GetObjects(where => where.Type.Is<Node>()).ObjectsCount));
            }
        }

        [Fact]
        public void TextBox_Class_Listeners_Are_Freed()
        {
            using (Start())
            {
                TextBox textBox;

                var window = new Window
                {
                    Content = textBox = new TextBox()
                };

                window.Show();

                // Do a layout and make sure that TextBox gets added to visual tree and its 
                // template applied.
                window.LayoutManager.ExecuteInitialLayoutPass();
                Assert.Same(textBox, window.Presenter.Child);

                // Get the border from the TextBox template.
                var border = textBox.GetTemplateChildren().FirstOrDefault(x => x.Name == "border");

                // The TextBox should have subscriptions to its Classes collection from the
                // default theme.
                Assert.NotEmpty(((INotifyCollectionChangedDebug)textBox.Classes).GetCollectionChangedSubscribers());

                // Clear the content and ensure the TextBox is removed.
                window.Content = null;
                window.LayoutManager.ExecuteLayoutPass();
                Assert.Null(window.Presenter.Child);

                // Check that the TextBox has no subscriptions to its Classes collection.
                Assert.Null(((INotifyCollectionChangedDebug)textBox.Classes).GetCollectionChangedSubscribers());
            }
        }

        [Fact]
        public void TreeView_Is_Freed()
        {
            using (Start())
            {
                Func<Window> run = () =>
                {
                    var nodes = new[]
                    {
                        new Node
                        {
                            Children = new[] { new Node() },
                        }
                    };

                    TreeView target;

                    var window = new Window
                    {
                        Content = target = new TreeView
                        {
                            DataTemplates =
                            {
                                new FuncTreeDataTemplate<Node>(
                                    (x, _) => new TextBlock { Text = x.Name },
                                    x => x.Children)
                            },
                            Items = nodes
                        }
                    };

                    window.Show();

                    // Do a layout and make sure that TreeViewItems get realized.
                    window.LayoutManager.ExecuteInitialLayoutPass();
                    Assert.Single(target.Presenter.RealizedElements);

                    // Clear the content and ensure the TreeView is removed.
                    window.Content = null;
                    window.LayoutManager.ExecuteLayoutPass();
                    Assert.Null(window.Presenter.Child);

                    return window;
                };

                var result = run();

                dotMemory.Check(memory =>
                    Assert.Equal(0, memory.GetObjects(where => where.Type.Is<TreeView>()).ObjectsCount));
            }
        }


        [Fact]
        public void Slider_Is_Freed()
        {
            using (Start())
            {
                Func<Window> run = () =>
                {
                    var window = new Window
                    {
                        Content = new Slider()
                    };

                    window.Show();

                    // Do a layout and make sure that Slider gets added to visual tree.
                    window.LayoutManager.ExecuteInitialLayoutPass();
                    Assert.IsType<Slider>(window.Presenter.Child);

                    // Clear the content and ensure the Slider is removed.
                    window.Content = null;
                    window.LayoutManager.ExecuteLayoutPass();
                    Assert.Null(window.Presenter.Child);

                    return window;
                };

                var result = run();

                dotMemory.Check(memory =>
                    Assert.Equal(0, memory.GetObjects(where => where.Type.Is<Slider>()).ObjectsCount));
            }
        }

        [Fact]
        public void RendererIsDisposed()
        {
            using (Start())
            {
                var renderer = new Mock<IRenderer>();
                renderer.Setup(x => x.Dispose());
                var impl = new Mock<IWindowImpl>();
                impl.SetupGet(x => x.RenderScaling).Returns(1);
                impl.SetupProperty(x => x.Closed);
                impl.Setup(x => x.CreateRenderer(It.IsAny<IRenderRoot>())).Returns(renderer.Object);
                impl.Setup(x => x.Dispose()).Callback(() => impl.Object.Closed());

                AvaloniaLocator.CurrentMutable.Bind<IWindowingPlatform>()
                    .ToConstant(new MockWindowingPlatform(() => impl.Object));
                var window = new Window()
                {
                    Content = new Button()
                };
                window.Show();
                window.Close();
                renderer.Verify(r => r.Dispose());
            }
        }

        [Fact]
        public void Control_With_Style_RenderTransform_Is_Freed()
        {
            // # Issue #3545
            using (Start())
            {
                Func<Window> run = () =>
                {
                    var window = new Window
                    {
                        Styles =
                        {
                            new Style(x => x.OfType<Canvas>())
                            {
                                Setters =
                                {
                                    new Setter
                                    {
                                        Property = Visual.RenderTransformProperty,
                                        Value = new RotateTransform(45),
                                    }
                                }
                            }
                        },
                        Content = new Canvas()
                    };

                    window.Show();

                    // Do a layout and make sure that Canvas gets added to visual tree with
                    // its render transform.
                    window.LayoutManager.ExecuteInitialLayoutPass();
                    var canvas = Assert.IsType<Canvas>(window.Presenter.Child);
                    Assert.IsType<RotateTransform>(canvas.RenderTransform);

                    // Clear the content and ensure the Canvas is removed.
                    window.Content = null;
                    window.LayoutManager.ExecuteLayoutPass();
                    Assert.Null(window.Presenter.Child);

                    return window;
                };

                var result = run();

                dotMemory.Check(memory =>
                    Assert.Equal(0, memory.GetObjects(where => where.Type.Is<Canvas>()).ObjectsCount));
            }
        }

        [Fact]
        public void Attached_ContextMenu_Is_Freed()
        {
            using (Start())
            {
                void AttachShowAndDetachContextMenu(Control control)
                {
                    var contextMenu = new ContextMenu
                    {
                        Items = new[]
                        {
                            new MenuItem { Header = "Foo" },
                            new MenuItem { Header = "Foo" },
                        }
                    };

                    control.ContextMenu = contextMenu;
                    contextMenu.Open(control);
                    contextMenu.Close();
                    control.ContextMenu = null;
                }

                var window = new Window();
                window.Show();

                Assert.Same(window, FocusManager.Instance.Current);

                // Context menu in resources means the baseline may not be 0.
                var initialMenuCount = 0;
                var initialMenuItemCount = 0;
                dotMemory.Check(memory =>
                {
                    initialMenuCount = memory.GetObjects(where => where.Type.Is<ContextMenu>()).ObjectsCount;
                    initialMenuItemCount = memory.GetObjects(where => where.Type.Is<MenuItem>()).ObjectsCount;
                });
                
                AttachShowAndDetachContextMenu(window);

                Mock.Get(window.PlatformImpl).Invocations.Clear();
                dotMemory.Check(memory =>
                    Assert.Equal(initialMenuCount, memory.GetObjects(where => where.Type.Is<ContextMenu>()).ObjectsCount));
                dotMemory.Check(memory =>
                    Assert.Equal(initialMenuItemCount, memory.GetObjects(where => where.Type.Is<MenuItem>()).ObjectsCount));
            }
        }

        [Fact]
        public void Standalone_ContextMenu_Is_Freed()
        {
            using (Start())
            {
                void BuildAndShowContextMenu(Control control)
                {
                    var contextMenu = new ContextMenu
                    {
                        Items = new[]
                        {
                            new MenuItem { Header = "Foo" },
                            new MenuItem { Header = "Foo" },
                        }
                    };

                    contextMenu.Open(control);
                    contextMenu.Close();
                }

                var window = new Window();
                window.Show();

                Assert.Same(window, FocusManager.Instance.Current);

                // Context menu in resources means the baseline may not be 0.
                var initialMenuCount = 0;
                var initialMenuItemCount = 0;
                dotMemory.Check(memory =>
                {
                    initialMenuCount = memory.GetObjects(where => where.Type.Is<ContextMenu>()).ObjectsCount;
                    initialMenuItemCount = memory.GetObjects(where => where.Type.Is<MenuItem>()).ObjectsCount;
                });
                
                BuildAndShowContextMenu(window);
                BuildAndShowContextMenu(window);

                Mock.Get(window.PlatformImpl).Invocations.Clear();
                dotMemory.Check(memory =>
                    Assert.Equal(initialMenuCount, memory.GetObjects(where => where.Type.Is<ContextMenu>()).ObjectsCount));
                dotMemory.Check(memory =>
                    Assert.Equal(initialMenuItemCount, memory.GetObjects(where => where.Type.Is<MenuItem>()).ObjectsCount));
            }
        }

        [Fact]
        public void Path_Is_Freed()
        {
            using (Start())
            {
                var geometry = new EllipseGeometry { Rect = new Rect(0, 0, 10, 10) };

                Func<Window> run = () =>
                {
                    var window = new Window
                    {
                        Content = new Path
                        {
                            Data = geometry
                        }
                    };

                    window.Show();

                    window.LayoutManager.ExecuteInitialLayoutPass();
                    Assert.IsType<Path>(window.Presenter.Child);

                    window.Content = null;
                    window.LayoutManager.ExecuteLayoutPass();
                    Assert.Null(window.Presenter.Child);

                    return window;
                };

                var result = run();

                dotMemory.Check(memory =>
                    Assert.Equal(0, memory.GetObjects(where => where.Type.Is<Path>()).ObjectsCount));

                // We are keeping geometry alive to simulate a resource that outlives the control.
                GC.KeepAlive(geometry);
            }
        }

        [Fact]
        public void ItemsRepeater_Is_Freed()
        {
            using (Start())
            {
                Func<Window> run = () =>
                {
                    var window = new Window
                    {
                        Content = new ItemsRepeater(),
                    };

                    window.Show();

                    window.LayoutManager.ExecuteInitialLayoutPass();
                    Assert.IsType<ItemsRepeater>(window.Presenter.Child);

                    window.Content = null;
                    window.LayoutManager.ExecuteLayoutPass();
                    Assert.Null(window.Presenter.Child);

                    return window;
                };

                var result = run();

                dotMemory.Check(memory =>
                    Assert.Equal(0, memory.GetObjects(where => where.Type.Is<ItemsRepeater>()).ObjectsCount));
            }
        }

        private IDisposable Start()
        {
            return UnitTestApplication.Start(TestServices.StyledWindow.With(
                focusManager: new FocusManager(),
                keyboardDevice: () => new KeyboardDevice(),
                inputManager: new InputManager()));
        }

        private class Node
        {
            public string Name { get; set; }
            public IEnumerable<Node> Children { get; set; }
        }

        private class NullRenderer : IRenderer
        {
            public bool DrawFps { get; set; }
            public bool DrawDirtyRects { get; set; }
#pragma warning disable CS0067
            public event EventHandler<SceneInvalidatedEventArgs> SceneInvalidated;
#pragma warning restore CS0067
            public void AddDirty(IVisual visual)
            {
            }

            public void Dispose()
            {
            }

            public IEnumerable<IVisual> HitTest(Point p, IVisual root, Func<IVisual, bool> filter) => null;

            public IVisual HitTestFirst(Point p, IVisual root, Func<IVisual, bool> filter) => null;

            public void Paint(Rect rect)
            {
            }

            public void RecalculateChildren(IVisual visual)
            {
            }

            public void Resized(Size size)
            {
            }

            public void Start()
            {
            }

            public void Stop()
            {
            }
        }
    }
}
