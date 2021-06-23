using System.Linq;
using Avalonia.Controls;
using Avalonia.Rendering.SceneGraph;
using Avalonia.UnitTests;
using Avalonia.VisualTree;
using Xunit;

namespace Avalonia.Base.UnitTests.Rendering.SceneGraph
{
    public class SceneLayersTests
    {
        [Fact]
        public void Layers_Should_Be_Ordered()
        {
            Border border;
            Decorator decorator;
            var root = new TestRoot
            {
                Child = border = new Border
                {
                    Child = decorator = new Decorator(),
                }
            };

            var target = new SceneLayers(root);
            target.Add(root);
            target.Add(decorator);
            target.Add(border);

            var result = target.Select(x => x.LayerRoot).ToArray();

            Assert.Equal(new IVisual[] { root, border, decorator }, result);
        }
    }
}
