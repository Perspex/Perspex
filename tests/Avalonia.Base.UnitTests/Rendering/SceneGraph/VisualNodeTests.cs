using Avalonia.Rendering.SceneGraph;
using Avalonia.Utilities;
using Avalonia.VisualTree;
using Moq;
using Xunit;

namespace Avalonia.Base.UnitTests.Rendering.SceneGraph
{
    public class VisualNodeTests
    {
        [Fact]
        public void Empty_Children_Collections_Should_Be_Shared()
        {
            var node1 = new VisualNode(Mock.Of<IVisual>(), null);
            var node2 = new VisualNode(Mock.Of<IVisual>(), null);

            Assert.Same(node1.Children, node2.Children);
        }

        [Fact]
        public void Adding_Child_Should_Create_Collection()
        {
            var node = new VisualNode(Mock.Of<IVisual>(), null);
            var collection = node.Children;

            node.AddChild(Mock.Of<IVisualNode>(x => x.Parent == node));

            Assert.NotSame(collection, node.Children);
        }

        [Fact]
        public void Empty_DrawOperations_Collections_Should_Be_Shared()
        {
            var node1 = new VisualNode(Mock.Of<IVisual>(), null);
            var node2 = new VisualNode(Mock.Of<IVisual>(), null);

            Assert.Same(node1.DrawOperations, node2.DrawOperations);
        }

        [Fact]
        public void Adding_DrawOperation_Should_Create_Collection()
        {
            var node = new VisualNode(Mock.Of<IVisual>(), null);
            var collection = node.DrawOperations;

            node.AddDrawOperation(RefCountable.Create(Mock.Of<IDrawOperation>()));

            Assert.NotSame(collection, node.DrawOperations);
        }

        [Fact]
        public void Cloned_Nodes_Should_Share_DrawOperations_Collection()
        {
            var node1 = new VisualNode(Mock.Of<IVisual>(), null);
            node1.AddDrawOperation(RefCountable.Create(Mock.Of<IDrawOperation>()));

            var node2 = node1.Clone(null);

            Assert.Same(node1.DrawOperations, node2.DrawOperations);
        }

        [Fact]
        public void Adding_DrawOperation_To_Cloned_Node_Should_Create_New_Collection()
        {
            var node1 = new VisualNode(Mock.Of<IVisual>(), null);
            var operation1 = RefCountable.Create(Mock.Of<IDrawOperation>());
            node1.AddDrawOperation(operation1);

            var node2 = node1.Clone(null);
            var operation2 = RefCountable.Create(Mock.Of<IDrawOperation>());
            node2.ReplaceDrawOperation(0, operation2);

            Assert.NotSame(node1.DrawOperations, node2.DrawOperations);
            Assert.Equal(1, node1.DrawOperations.Count);
            Assert.Equal(1, node2.DrawOperations.Count);
            Assert.Same(operation1.Item, node1.DrawOperations[0].Item);
            Assert.Same(operation2.Item, node2.DrawOperations[0].Item);
        }

        [Fact]
        public void DrawOperations_In_Cloned_Node_Are_Cloned()
        {
            var node1 = new VisualNode(Mock.Of<IVisual>(), null);
            var operation1 = RefCountable.Create(Mock.Of<IDrawOperation>());
            node1.AddDrawOperation(operation1);

            var node2 = node1.Clone(null);
            var operation2 = RefCountable.Create(Mock.Of<IDrawOperation>());
            node2.AddDrawOperation(operation2);
            
            Assert.Same(node1.DrawOperations[0].Item, node2.DrawOperations[0].Item);
            Assert.NotSame(node1.DrawOperations[0], node2.DrawOperations[0]);
        }

        [Fact]
        public void SortChildren_Does_Not_Throw_On_Null_Children()
        {
            var node = new VisualNode(Mock.Of<IVisual>(), null);
            var scene = new Scene(Mock.Of<IVisual>());

            node.SortChildren(scene);
        }

        [Fact]
        public void TrimChildren_Should_Work_Correctly()
        {
            var parent = new VisualNode(Mock.Of<IVisual>(), null);
            var child1 = new VisualNode(Mock.Of<IVisual>(), parent);
            var child2 = new VisualNode(Mock.Of<IVisual>(), parent);
            var child3 = new VisualNode(Mock.Of<IVisual>(), parent);

            parent.AddChild(child1);
            parent.AddChild(child2);
            parent.AddChild(child3);
            parent.TrimChildren(2);

            Assert.Equal(2, parent.Children.Count);
            Assert.False(child1.Disposed);
            Assert.False(child2.Disposed);
            Assert.True(child3.Disposed);
        }
    }
}
