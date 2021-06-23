using System.Linq;
using System.Reactive.Subjects;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Media;
using Avalonia.Rendering.SceneGraph;
using Avalonia.UnitTests;
using Avalonia.VisualTree;
using Xunit;

namespace Avalonia.Base.UnitTests.Rendering.SceneGraph
{
    public partial class SceneBuilderTests
    {
        [Fact(Skip = "Layers are disabled. See #2244")]
        public void Control_With_Animated_Opacity_And_Children_Should_Start_New_Layer()
        {
            using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface))
            {
                Decorator decorator;
                Border border;
                Canvas canvas;
                var tree = new TestRoot
                {
                    Padding = new Thickness(10),
                    Width = 100,
                    Height = 120,
                    Child = decorator = new Decorator
                    {
                        Padding = new Thickness(11),
                        Child = border = new Border
                        {
                            Background = Brushes.Red,
                            Padding = new Thickness(12),
                            Child = canvas = new Canvas()
                        }
                    }
                };

                var layout = tree.LayoutManager;
                layout.ExecuteInitialLayoutPass();

                var animation = new BehaviorSubject<double>(0.5);
                border.Bind(Border.OpacityProperty, animation, BindingPriority.Animation);

                var scene = new Scene(tree);
                var sceneBuilder = new SceneBuilder();
                sceneBuilder.UpdateAll(scene);

                var rootNode = (VisualNode)scene.Root;
                var borderNode = (VisualNode)scene.FindNode(border);
                var canvasNode = (VisualNode)scene.FindNode(canvas);

                Assert.Same(tree, rootNode.LayerRoot);
                Assert.Same(border, borderNode.LayerRoot);
                Assert.Same(border, canvasNode.LayerRoot);
                Assert.Equal(0.5, scene.Layers[border].Opacity);

                Assert.Equal(2, scene.Layers.Count());
                Assert.Empty(scene.Layers.Select(x => x.LayerRoot).Except(new IVisual[] { tree, border }));

                animation.OnCompleted();
                scene = scene.CloneScene();

                sceneBuilder.Update(scene, border);

                rootNode = (VisualNode)scene.Root;
                borderNode = (VisualNode)scene.FindNode(border);
                canvasNode = (VisualNode)scene.FindNode(canvas);

                Assert.Same(tree, rootNode.LayerRoot);
                Assert.Same(tree, borderNode.LayerRoot);
                Assert.Same(tree, canvasNode.LayerRoot);
                Assert.Single(scene.Layers);

                var rootDirty = scene.Layers[tree].Dirty;

                Assert.Single(rootDirty);
                Assert.Equal(new Rect(21, 21, 58, 78), rootDirty.Single());
            }
        }

        [Fact]
        public void Control_With_Animated_Opacity_And_No_Children_Should_Not_Start_New_Layer()
        {
            using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface))
            {
                Decorator decorator;
                Border border;
                var tree = new TestRoot
                {
                    Padding = new Thickness(10),
                    Width = 100,
                    Height = 120,
                    Child = decorator = new Decorator
                    {
                        Padding = new Thickness(11),
                        Child = border = new Border
                        {
                            Background = Brushes.Red,
                        }
                    }
                };

                var layout = tree.LayoutManager;
                layout.ExecuteInitialLayoutPass();

                var animation = new BehaviorSubject<double>(0.5);
                border.Bind(Border.OpacityProperty, animation, BindingPriority.Animation);

                var scene = new Scene(tree);
                var sceneBuilder = new SceneBuilder();
                sceneBuilder.UpdateAll(scene);

                Assert.Single(scene.Layers);
            }
        }

        [Fact(Skip = "Layers are disabled. See #2244")]
        public void Removing_Control_With_Animated_Opacity_Should_Remove_Layers()
        {
            using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface))
            {
                Decorator decorator;
                Border border;
                Canvas canvas;
                var tree = new TestRoot
                {
                    Padding = new Thickness(10),
                    Width = 100,
                    Height = 120,
                    Child = decorator = new Decorator
                    {
                        Padding = new Thickness(11),
                        Child = border = new Border
                        {
                            Background = Brushes.Red,
                            Padding = new Thickness(12),
                            Child = canvas = new Canvas
                            {
                                Children = { new TextBlock() },
                            }
                        }
                    }
                };

                var layout = tree.LayoutManager;
                layout.ExecuteInitialLayoutPass();

                var animation = new BehaviorSubject<double>(0.5);
                border.Bind(Border.OpacityProperty, animation, BindingPriority.Animation);
                canvas.Bind(Canvas.OpacityProperty, animation, BindingPriority.Animation);

                var scene = new Scene(tree);
                var sceneBuilder = new SceneBuilder();
                sceneBuilder.UpdateAll(scene);

                Assert.Equal(3, scene.Layers.Count);

                decorator.Child = null;
                scene = scene.CloneScene();

                sceneBuilder.Update(scene, border);

                Assert.Equal(1, scene.Layers.Count);
            }
        }

        [Fact(Skip = "Layers are disabled. See #2244")]
        public void Hiding_Transparent_Control_Should_Remove_Layers()
        {
            using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface))
            {
                Decorator decorator;
                Border border;
                Canvas canvas;
                var tree = new TestRoot
                {
                    Padding = new Thickness(10),
                    Width = 100,
                    Height = 120,
                    Child = decorator = new Decorator
                    {
                        Padding = new Thickness(11),
                        Child = border = new Border
                        {
                            Background = Brushes.Red,
                            Padding = new Thickness(12),
                            Child = canvas = new Canvas
                            {
                                Children = { new TextBlock() },
                            }
                        }
                    }
                };

                var layout = tree.LayoutManager;
                layout.ExecuteInitialLayoutPass();

                var animation = new BehaviorSubject<double>(0.5);
                border.Bind(Border.OpacityProperty, animation, BindingPriority.Animation);
                canvas.Bind(Canvas.OpacityProperty, animation, BindingPriority.Animation);

                var scene = new Scene(tree);
                var sceneBuilder = new SceneBuilder();
                sceneBuilder.UpdateAll(scene);

                Assert.Equal(3, scene.Layers.Count);

                border.IsVisible = false;
                scene = scene.CloneScene();

                sceneBuilder.Update(scene, border);

                Assert.Equal(1, scene.Layers.Count);
            }
        }

        [Fact(Skip = "Layers are disabled. See #2244")]
        public void GeometryClip_Should_Affect_Child_Layers()
        {
            using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface))
            {
                var clip = StreamGeometry.Parse("M100,0 L0,100 100,100");
                Decorator decorator;
                Border border;
                var tree = new TestRoot
                {
                    Child = decorator = new Decorator
                    {
                        Clip = clip,
                        Margin = new Thickness(12, 16),
                        Child = border = new Border
                        {
                            Opacity = 0.5,
                            Child = new Canvas(),
                        }
                    }
                };

                var layout = tree.LayoutManager;
                layout.ExecuteInitialLayoutPass();

                var animation = new BehaviorSubject<double>(0.5);
                border.Bind(Border.OpacityProperty, animation, BindingPriority.Animation);

                var scene = new Scene(tree);
                var sceneBuilder = new SceneBuilder();
                sceneBuilder.UpdateAll(scene);

                var borderLayer = scene.Layers[border];
                Assert.Equal(
                    Matrix.CreateTranslation(12, 16),
                    ((MockStreamGeometryImpl)borderLayer.GeometryClip).Transform);
            }
        }
    }
}
