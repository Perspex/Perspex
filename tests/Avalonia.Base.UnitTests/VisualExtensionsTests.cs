using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.UnitTests;
using Avalonia.VisualTree;
using Xunit;

namespace Avalonia.Base.UnitTests
{
    public class VisualExtensionsTests
    {
        [Fact]
        public void FindAncestorOfType_Finds_Direct_Parent()
        {
            StackPanel target;

            var root = new TestRoot
            {
                Child = target = new StackPanel()
            };

            Assert.Equal(root, target.FindAncestorOfType<TestRoot>());
        }

        [Fact]
        public void FindAncestorOfType_Finds_Ancestor_Of_Nested_Child()
        {
            Button target;

            var root = new TestRoot
            {
                Child = new StackPanel
                {
                    Children =
                    {
                        new StackPanel
                        {
                            Children =
                            {
                                (target = new Button())
                            }
                        }
                    }
                }
            };

            Assert.Equal(root, target.FindAncestorOfType<TestRoot>());
        }

        [Fact]
        public void FindDescendantOfType_Finds_Direct_Child()
        {
            StackPanel target;

            var root = new TestRoot
            {
                Child = target = new StackPanel()
            };

            Assert.Equal(target, root.FindDescendantOfType<StackPanel>());
        }

        [Fact]
        public void FindDescendantOfType_Finds_Nested_Child()
        {
            Button target;

            var root = new TestRoot
            {
                Child = new StackPanel
                {
                    Children =
                    {
                        new StackPanel
                        {
                            Children =
                            {
                                (target = new Button())
                            }
                        }
                    }
                }
            };

            Assert.Equal(target, root.FindDescendantOfType<Button>());
        }

        [Fact]
        public void FindCommonVisualAncestor_First_Is_Parent_Of_Second()
        {
            Control left, right;

            var root = new TestRoot
            {
                Child = left = new Decorator
                {
                    Child = right = new Decorator()
                }
            };

            var ancestor = left.FindCommonVisualAncestor(right);
            Assert.Equal(left, ancestor);

            ancestor = right.FindCommonVisualAncestor(left);
            Assert.Equal(left, ancestor);
        }

        [Fact]
        public void FindCommonVisualAncestor_Two_Subtrees_Uniform_Height()
        {
            Control left, right;

            var root = new TestRoot
            {
                Child = new StackPanel
                {
                    Children =
                    {
                        new Decorator
                        {
                            Child = new Decorator
                            {
                                Child = left = new Decorator()
                            }
                        },
                        new Decorator
                        {
                            Child = new Decorator
                            {
                                Child = right = new Decorator()
                            }
                        }
                    }
                }
            };

            var ancestor = left.FindCommonVisualAncestor(right);
            Assert.Equal(root.Child, ancestor);

            ancestor = right.FindCommonVisualAncestor(left);
            Assert.Equal(root.Child, ancestor);
        }

        [Fact]
        public void FindCommonVisualAncestor_Two_Subtrees_NonUniform_Height()
        {
            Control left, right;

            var root = new TestRoot
            {
                Child = new StackPanel
                {
                    Children =
                    {
                        new Decorator
                        {
                            Child = new Decorator
                            {
                                Child = left = new Decorator()
                            }
                        },
                        new Decorator
                        {
                            Child = new Decorator
                            {
                                Child = new Decorator
                                {
                                    Child = right = new Decorator()
                                }
                            }
                        }
                    }
                }
            };

            var ancestor = left.FindCommonVisualAncestor(right);
            Assert.Equal(root.Child, ancestor);

            ancestor = right.FindCommonVisualAncestor(left);
            Assert.Equal(root.Child, ancestor);
        }

        [Fact]
        public void TranslatePoint_Should_Respect_RenderTransforms()
        {
            Border target;
            var root = new TestRoot
            {
                Width = 100,
                Height = 100,
                Child = new Decorator
                {
                    Width = 50,
                    Height = 50,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    RenderTransform = new TranslateTransform(25, 25),
                    Child = target = new Border(),
                }
            };

            root.Measure(Size.Infinity);
            root.Arrange(new Rect(root.DesiredSize));

            var result = target.TranslatePoint(new Point(0, 0), root);

            Assert.Equal(new Point(50, 50), result);
        }
    }
}
