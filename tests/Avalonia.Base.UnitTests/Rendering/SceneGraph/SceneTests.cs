using System.Linq;
using Avalonia.Controls;
using Avalonia.Rendering.SceneGraph;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Base.UnitTests.Rendering.SceneGraph
{
    public class SceneTests
    {
        [Fact]
        public void Cloning_Scene_Should_Retain_Layers_But_Not_DirtyRects()
        {
            Decorator decorator;
            var tree = new TestRoot
            {
                Child = decorator = new Decorator(),
            };

            var scene = new Scene(tree);
            scene.Layers.Add(tree);
            scene.Layers.Add(decorator);

            scene.Layers[tree].Dirty.Add(new Rect(0, 0, 100, 100));
            scene.Layers[decorator].Dirty.Add(new Rect(0, 0, 50, 100));

            scene = scene.CloneScene();
            Assert.Equal(2, scene.Layers.Count());
            Assert.Empty(scene.Layers[0].Dirty);
            Assert.Empty(scene.Layers[1].Dirty);
        }
    }
}
