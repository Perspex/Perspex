using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.LogicalTree;
using Avalonia.Styling;
using Avalonia.UnitTests;
using Avalonia.VisualTree;
using Moq;
using System;
using System.Linq;
using Xunit;
using Avalonia.Rendering;
using Avalonia.Media;
using Avalonia.Data;

namespace Avalonia.Controls.UnitTests.Presenters
{
    /// <summary>
    /// Tests for ContentControls that aren't hosted in a control template.
    /// </summary>
    public class ContentPresenterTests_Standalone
    {
        [Fact]
        public void Should_Set_Childs_Parent_To_Itself_Standalone()
        {
            var content = new Border();
            var target = new ContentPresenter { Content = content };

            target.UpdateChild();

            Assert.Same(target, content.Parent);
        }

        [Fact]
        public void Should_Add_Child_To_Own_LogicalChildren_Standalone()
        {
            var content = new Border();
            var target = new ContentPresenter { Content = content };

            target.UpdateChild();

            var logicalChildren = target.GetLogicalChildren();

            Assert.Single(logicalChildren);
            Assert.Equal(content, logicalChildren.First());
        }

        [Fact]
        public void Should_Raise_DetachedFromLogicalTree_On_Content_Changed_Standalone()
        {
            var target = new ContentPresenter
            {
                ContentTemplate =
                    new FuncDataTemplate<string>((t, _) => new ContentControl() { Content = t }, false)
            };

            var parentMock = new Mock<Control>();
            parentMock.As<IContentPresenterHost>();
            parentMock.As<IRenderRoot>();
            parentMock.As<ILogicalRoot>();

            (target as ISetLogicalParent).SetParent(parentMock.Object);

            target.Content = "foo";

            target.UpdateChild();

            var foo = target.Child as ContentControl;

            bool foodetached = false;

            Assert.NotNull(foo);
            Assert.Equal("foo", foo.Content);

            foo.DetachedFromLogicalTree += delegate { foodetached = true; };

            target.Content = "bar";
            target.UpdateChild();

            var bar = target.Child as ContentControl;

            Assert.NotNull(bar);
            Assert.True(bar != foo);
            Assert.False((foo as IControl).IsAttachedToLogicalTree);
            Assert.True(foodetached);
        }

        [Fact]
        public void Should_Raise_DetachedFromLogicalTree_In_ContentControl_On_Content_Changed_Standalone()
        {
            var contentControl = new ContentControl
            {
                Template = new FuncControlTemplate<ContentControl>((c, scope) => new ContentPresenter()
                {
                    Name = "PART_ContentPresenter",
                    [~ContentPresenter.ContentProperty] = c[~ContentControl.ContentProperty],
                    [~ContentPresenter.ContentTemplateProperty] = c[~ContentControl.ContentTemplateProperty]
                }.RegisterInNameScope(scope)),
                ContentTemplate =
                    new FuncDataTemplate<string>((t, _) => new ContentControl() { Content = t }, false)
            };

            var parentMock = new Mock<Control>();
            parentMock.As<IRenderRoot>();
            parentMock.As<ILogicalRoot>();
            parentMock.As<ILogical>().SetupGet(l => l.IsAttachedToLogicalTree).Returns(true);

            (contentControl as ISetLogicalParent).SetParent(parentMock.Object);

            contentControl.ApplyTemplate();
            var target = contentControl.Presenter as ContentPresenter;

            contentControl.Content = "foo";

            target.UpdateChild();

            var tbfoo = target.Child as ContentControl;

            bool foodetached = false;

            Assert.NotNull(tbfoo);
            Assert.Equal("foo", tbfoo.Content);

            tbfoo.DetachedFromLogicalTree += delegate { foodetached = true; };

            contentControl.Content = "bar";
            target.UpdateChild();

            var tbbar = target.Child as ContentControl;

            Assert.NotNull(tbbar);

            Assert.True(tbbar != tbfoo);
            Assert.False((tbfoo as IControl).IsAttachedToLogicalTree);
            Assert.True(foodetached);
        }

        [Fact]
        public void Should_Raise_DetachedFromLogicalTree_On_Detached_Standalone()
        {
            var target = new ContentPresenter
            {
                ContentTemplate =
                    new FuncDataTemplate<string>((t, _) => new ContentControl() { Content = t }, false)
            };

            var parentMock = new Mock<Control>();
            parentMock.As<IContentPresenterHost>();
            parentMock.As<IRenderRoot>();
            parentMock.As<ILogicalRoot>();

            (target as ISetLogicalParent).SetParent(parentMock.Object);

            target.Content = "foo";

            target.UpdateChild();

            var foo = target.Child as ContentControl;

            bool foodetached = false;

            Assert.NotNull(foo);
            Assert.Equal("foo", foo.Content);

            foo.DetachedFromLogicalTree += delegate { foodetached = true; };

            (target as ISetLogicalParent).SetParent(null);

            Assert.False((foo as IControl).IsAttachedToLogicalTree);
            Assert.True(foodetached);
        }

        [Fact]
        public void Should_Remove_Old_Child_From_LogicalChildren_On_ContentChanged_Standalone()
        {
            var target = new ContentPresenter
            {
                ContentTemplate =
                    new FuncDataTemplate<string>((t, _) => new ContentControl() { Content = t }, false)
            };

            target.Content = "foo";

            target.UpdateChild();

            var foo = target.Child as ContentControl;

            Assert.NotNull(foo);

            var logicalChildren = target.GetLogicalChildren();

            Assert.Single(logicalChildren);

            target.Content = "bar";
            target.UpdateChild();

            Assert.Null(foo.Parent);

            logicalChildren = target.GetLogicalChildren();

            Assert.Single(logicalChildren);
            Assert.NotEqual(foo, logicalChildren.First());
        }

        [Fact]
        public void Changing_Background_Brush_Color_Should_Invalidate_Visual()
        {
            var target = new ContentPresenter()
            {
                Background = new SolidColorBrush(Colors.Red),
            };

            var root = new TestRoot(target);
            var renderer = Mock.Get(root.Renderer);
            renderer.Invocations.Clear();

            ((SolidColorBrush)target.Background).Color = Colors.Green;

            renderer.Verify(x => x.AddDirty(target), Times.Once);
        }

        [Fact]
        public void Should_Not_Bind_Old_Child_To_New_DataContext()
        {
            // Test for issue #1099.
            var textBlock = new TextBlock
            {
                [!TextBlock.TextProperty] = new Binding(),
            };

            var target = new ContentPresenter()
            {
                DataTemplates =
                {
                    new FuncDataTemplate<string>((x, _) => textBlock),
                    new FuncDataTemplate<int>((x, _) => new Canvas()),
                },
            };

            var root = new TestRoot(target);
            target.Content = "foo";
            Assert.Same(textBlock, target.Child);

            textBlock.PropertyChanged += (s, e) =>
            {
                Assert.NotEqual(e.NewValue, "42");
            };

            target.Content = 42;
        }

        [Fact]
        public void Should_Reset_InheritanceParent_When_Child_Removed()
        {
            var logicalParent = new Canvas();
            var child = new TextBlock();
            var target = new ContentPresenter();
            var root = new TestRoot(target);

            ((ISetLogicalParent)child).SetParent(logicalParent);
            target.Content = child;
            target.Content = null;

            // InheritanceParent is exposed via StylingParent.
            Assert.Same(logicalParent, ((IStyledElement)child).StylingParent);
        }

        [Fact]
        public void Should_Create_Child_Even_With_Null_Content_When_ContentTemplate_Is_Set()
        {
            var target = new ContentPresenter
            {
                ContentTemplate = new FuncDataTemplate<object>(_ => true, (_, __) => new TextBlock
                {
                    Text = "Hello World"
                }),
                Content = null
            };

            target.UpdateChild();

            var textBlock = Assert.IsType<TextBlock>(target.Child);
            Assert.Equal("Hello World", textBlock.Text);
        }

        [Fact]
        public void Should_Not_Create_Child_Even_With_Null_Content_And_DataTemplates_InsteadOf_ContentTemplate()
        {
            var target = new ContentPresenter
            {
                DataTemplates =
                {
                    new FuncDataTemplate<object>(_ => true, (_, __) => new TextBlock
                    {
                        Text = "Hello World"
                    })
                },
                Content = null
            };

            target.UpdateChild();

            Assert.Null(target.Child);
        }

        [Fact]
        public void Should_Not_Create_Child_When_Content_And_Template_Are_Null()
        {
            var target = new ContentPresenter
            {
                ContentTemplate = null,
                Content = null
            };

            target.UpdateChild();

            Assert.Null(target.Child);
        }

        [Fact]
        public void Should_Not_Create_When_Child_Content_Is_Null_But_Expected_ValueType_With_FuncDataTemplate()
        {
            var target = new ContentPresenter
            {
                ContentTemplate = new FuncDataTemplate<int>(_ => true, (_, __) => new TextBlock
                {
                    Text = "Hello World"
                }),
                Content = null
            };

            target.UpdateChild();

            Assert.Null(target.Child);
        }

        [Fact]
        public void Should_Create_Child_When_Content_Is_Null_And_Expected_NullableValueType_With_FuncDataTemplate()
        {
            var target = new ContentPresenter
            {
                ContentTemplate = new FuncDataTemplate<int?>(_ => true, (_, __) => new TextBlock
                {
                    Text = "Hello World"
                }),
                Content = null
            };

            target.UpdateChild();

            Assert.NotNull(target.Child);
        }
    }
}
