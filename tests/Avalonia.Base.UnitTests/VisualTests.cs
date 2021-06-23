using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Media;
using Avalonia.Rendering;
using Avalonia.UnitTests;
using Avalonia.VisualTree;
using Moq;
using Xunit;

namespace Avalonia.Base.UnitTests
{
    public class VisualTests
    {
        [Fact]
        public void Added_Child_Should_Have_VisualParent_Set()
        {
            var target = new TestVisual();
            var child = new Visual();

            target.AddChild(child);

            Assert.Equal(target, child.GetVisualParent());
        }

        [Fact]
        public void Added_Child_Should_Notify_VisualParent_Changed()
        {
            var target = new TestVisual();
            var child = new TestVisual();
            var parents = new List<IVisual>();

            child.GetObservable(Visual.VisualParentProperty).Subscribe(x => parents.Add(x));
            target.AddChild(child);
            target.RemoveChild(child);

            Assert.Equal(new IVisual[] { null, target, null }, parents);
        }

        [Fact]
        public void Removed_Child_Should_Have_VisualParent_Cleared()
        {
            var target = new TestVisual();
            var child = new Visual();

            target.AddChild(child);
            target.RemoveChild(child);

            Assert.Null(child.GetVisualParent());
        }

        [Fact]
        public void Clearing_Children_Should_Clear_VisualParent()
        {
            var children = new[] { new Visual(), new Visual() };
            var target = new TestVisual();

            target.AddChildren(children);
            target.ClearChildren();

            var result = children.Select(x => x.GetVisualParent()).ToList();

            Assert.Equal(new Visual[] { null, null }, result);
        }

        [Fact]
        public void Adding_Children_Should_Fire_OnAttachedToVisualTree()
        {
            var child2 = new Decorator();
            var child1 = new Decorator { Child = child2 };
            var root = new TestRoot();
            var called1 = false;
            var called2 = false;

            child1.AttachedToVisualTree += (s, e) =>
            {
                Assert.Equal(e.Parent, root);
                Assert.Equal(e.Root, root);
                called1 = true;
            };

            child2.AttachedToVisualTree += (s, e) =>
            {
                Assert.Equal(e.Parent, root);
                Assert.Equal(e.Root, root);
                called2 = true;
            };

            root.Child = child1;

            Assert.True(called1);
            Assert.True(called2);
        }

        [Fact]
        public void Removing_Children_Should_Fire_OnDetachedFromVisualTree()
        {
            var child2 = new Decorator();
            var child1 = new Decorator { Child = child2 };
            var root = new TestRoot();
            var called1 = false;
            var called2 = false;

            root.Child = child1;

            child1.DetachedFromVisualTree += (s, e) =>
            {
                Assert.Equal(e.Parent, root);
                Assert.Equal(e.Root, root);
                called1 = true;
            };

            child2.DetachedFromVisualTree += (s, e) =>
            {
                Assert.Equal(e.Parent, root);
                Assert.Equal(e.Root, root);
                called2 = true;
            };

            root.Child = null;

            Assert.True(called1);
            Assert.True(called2);
        }

        [Fact]
        public void Root_Should_Retun_Self_As_VisualRoot()
        {
            var root = new TestRoot();

            Assert.Same(root, ((IVisual)root).VisualRoot);
        }

        [Fact]
        public void Descendants_Should_RetunVisualRoot()
        {
            var root = new TestRoot();
            var child1 = new Decorator();
            var child2 = new Decorator();

            root.Child = child1;
            child1.Child = child2;

            Assert.Same(root, ((IVisual)child1).VisualRoot);
            Assert.Same(root, ((IVisual)child2).VisualRoot);
        }

        [Fact]
        public void Attaching_To_Visual_Tree_Should_Invalidate_Visual()
        {
            var renderer = new Mock<IRenderer>();
            var child = new Decorator();
            var root = new TestRoot
            {
                Renderer = renderer.Object,
            };

            root.Child = child;

            renderer.Verify(x => x.AddDirty(child));
        }

        [Fact]
        public void Detaching_From_Visual_Tree_Should_Invalidate_Visual()
        {
            var renderer = new Mock<IRenderer>();
            var child = new Decorator();
            var root = new TestRoot
            {
                Renderer = renderer.Object,
            };

            root.Child = child;
            renderer.Invocations.Clear();
            root.Child = null;

            renderer.Verify(x => x.AddDirty(child));
        }

        [Fact]
        public void Adding_Already_Parented_Control_Should_Throw()
        {
            var root1 = new TestRoot();
            var root2 = new TestRoot();
            var child = new Canvas();

            root1.Child = child;

            Assert.Throws<InvalidOperationException>(() => root2.Child = child);
            Assert.Empty(root2.GetVisualChildren());
        }

        [Fact]
        public void TransformToVisual_Should_Work()
        {
            var child = new Decorator { Width = 100, Height = 100 };
            var root = new TestRoot() { Child = child, Width = 400, Height = 400 };

            root.Measure(Size.Infinity);
            root.Arrange(new Rect(new Point(), root.DesiredSize));

            var tr = child.TransformToVisual(root);

            Assert.NotNull(tr);

            var point = root.Bounds.TopLeft * tr;

            //child is centered (400 - 100)/2
            Assert.Equal(new Point(150, 150), point);
        }

        [Fact]
        public void TransformToVisual_With_RenderTransform_Should_Work()
        {
            var child = new Decorator
            {
                Width = 100,
                Height = 100,
                RenderTransform = new ScaleTransform() { ScaleX = 2, ScaleY = 2 }
            };
            var root = new TestRoot() { Child = child, Width = 400, Height = 400 };

            root.Measure(Size.Infinity);
            root.Arrange(new Rect(new Point(), root.DesiredSize));

            var tr = child.TransformToVisual(root);

            Assert.NotNull(tr);

            var point = root.Bounds.TopLeft * tr;

            //child is centered (400 - 100*2 scale)/2
            Assert.Equal(new Point(100, 100), point);
        }

        [Fact]
        public void TransformToVisual_With_NonInvertible_RenderTransform_Should_Work()
        {
            var child = new Decorator
            {
                Width = 100,
                Height = 100,
                RenderTransform = new ScaleTransform() { ScaleX = 0, ScaleY = 0 }
            };
            var root = new TestRoot() { Child = child, Width = 400, Height = 400 };

            root.Measure(Size.Infinity);
            root.Arrange(new Rect(new Point(), root.DesiredSize));

            var tr = root.TransformToVisual(child);

            Assert.Null(tr);
        }

        [Fact]
        public void Should_Not_Log_Binding_Error_When_Not_Attached_To_Logical_Tree()
        {
            var target = new Decorator { DataContext = "foo" };
            var called = false;

            LogCallback checkLogMessage = (level, area, src, mt, pv) =>
            {
                if (level >= Logging.LogEventLevel.Warning)
                {
                    called = true;
                }
            };

            using (TestLogSink.Start(checkLogMessage))
            {
                target.Bind(Decorator.TagProperty, new Binding("Foo"));
            }

            Assert.False(called);
        }

        [Fact]
        public void Should_Log_Binding_Error_When_Attached_To_Logical_Tree()
        {
            var target = new Decorator();
            var root = new TestRoot { Child = target, DataContext = "foo" };
            var called = false;

            LogCallback checkLogMessage = (level, area, src, mt, pv) =>
            {
                if (level >= Logging.LogEventLevel.Warning)
                {
                    called = true;
                }
            };

            using (TestLogSink.Start(checkLogMessage))
            {
                target.Bind(Decorator.TagProperty, new Binding("Foo"));
            }

            Assert.True(called);
        }

        [Fact]
        public void Changing_ZIndex_Should_InvalidateVisual()
        {
            Canvas canvas1;
            var renderer = new Mock<IRenderer>();
            var root = new TestRoot
            {
                Child = new StackPanel
                {
                    Children =
                    {
                        (canvas1 = new Canvas()),
                        new Canvas(),
                    },
                },
            };

            root.Renderer = renderer.Object;
            canvas1.ZIndex = 10;

            renderer.Verify(x => x.AddDirty(canvas1));
        }

        [Fact]
        public void Changing_ZIndex_Should_Recalculate_Parent_Children()
        {
            Canvas canvas1;
            StackPanel stackPanel;
            var renderer = new Mock<IRenderer>();
            var root = new TestRoot
            {
                Child = stackPanel = new StackPanel
                {
                    Children =
                    {
                        (canvas1 = new Canvas()),
                        new Canvas(),
                    },
                },
            };

            root.Renderer = renderer.Object;
            canvas1.ZIndex = 10;

            renderer.Verify(x => x.RecalculateChildren(stackPanel));
        }
    }
}
