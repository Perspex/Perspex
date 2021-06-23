using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Diagnostics;
using Avalonia.Styling;
using Avalonia.VisualTree;
using Moq;
using Xunit;

namespace Avalonia.Base.UnitTests.Styling
{
    public class SelectorTests_Template
    {
        [Fact]
        public void Control_In_Template_Is_Matched_With_Template_Selector()
        {
            var target = new Mock<IVisual>();
            var templatedControl = target.As<ITemplatedControl>();
            var styleable = target.As<IStyleable>();
            BuildVisualTree(target);

            var border = (Border)target.Object.GetVisualChildren().Single();
            var selector = default(Selector)
                .OfType(target.Object.GetType())
                .Template()
                .OfType<Border>();

            Assert.Equal(SelectorMatchResult.AlwaysThisInstance, selector.Match(border).Result);
        }

        [Fact]
        public void Control_Not_In_Template_Is_Not_Matched_With_Template_Selector()
        {
            var target = new Mock<IVisual>();
            var templatedControl = target.As<ITemplatedControl>();
            var styleable = target.As<IStyleable>();
            BuildVisualTree(target);

            var border = (Border)target.Object.GetVisualChildren().Single();
            border.SetValue(Control.TemplatedParentProperty, null);
            var selector = default(Selector)
                .OfType(target.Object.GetType())
                .Template()
                .OfType<Border>();

            Assert.Equal(SelectorMatchResult.NeverThisInstance, selector.Match(border).Result);
        }

        [Fact]
        public void Control_In_Template_Of_Wrong_Type_Is_Not_Matched_With_Template_Selector()
        {
            var target = new Mock<IVisual>();
            var templatedControl = target.As<ITemplatedControl>();
            var styleable = target.As<IStyleable>();
            BuildVisualTree(target);

            var border = (Border)target.Object.GetVisualChildren().Single();
            var selector = default(Selector)
                .OfType<Button>()
                .Template()
                .OfType<Border>();

            Assert.Equal(SelectorMatchResult.NeverThisInstance, selector.Match(border).Result);
        }

        [Fact]
        public void Nested_Control_In_Template_Is_Matched_With_Template_Selector()
        {
            var target = new Mock<IVisual>();
            var templatedControl = target.As<ITemplatedControl>();
            var styleable = target.As<IStyleable>();
            BuildVisualTree(target);

            var textBlock = (TextBlock)target.Object.VisualChildren.Single().VisualChildren.Single();
            var selector = default(Selector)
                .OfType(target.Object.GetType())
                .Template()
                .OfType<TextBlock>();

            Assert.Equal(SelectorMatchResult.AlwaysThisInstance, selector.Match(textBlock).Result);
        }

        [Fact]
        public void Control_In_Template_Is_Matched_With_TypeOf_TemplatedControl()
        {
            var target = new Mock<IVisual>();
            var templatedControl = target.As<ITemplatedControl>();
            var styleable = target.As<IStyleable>();
            var styleKey = templatedControl.Object.GetType();
            BuildVisualTree(target);

            var border = (Border)target.Object.VisualChildren.Single();

            var selector = default(Selector).OfType(styleKey).Template().OfType<Border>();

            Assert.Equal(SelectorMatchResult.AlwaysThisInstance, selector.Match(border).Result);
        }

        [Fact]
        public async Task Control_In_Template_Is_Matched_With_Correct_TypeOf_And_Class_Of_TemplatedControl()
        {
            var target = new Mock<IVisual>();
            var templatedControl = target.As<ITemplatedControl>();
            var styleable = target.As<IStyleable>();
            var styleKey = templatedControl.Object.GetType();
            BuildVisualTree(target);

            styleable.Setup(x => x.StyleKey).Returns(styleKey);
            styleable.Setup(x => x.Classes).Returns(new Classes("foo"));
            var border = (Border)target.Object.VisualChildren.Single();
            var selector = default(Selector).OfType(styleKey).Class("foo").Template().OfType<Border>();
            var activator = selector.Match(border).Activator;

            Assert.True(await activator.Take(1));
        }

        [Fact]
        public async Task Control_In_Template_Is_Not_Matched_With_Correct_TypeOf_And_Wrong_Class_Of_TemplatedControl()
        {
            var target = new Mock<IVisual>();
            var templatedControl = target.As<ITemplatedControl>();
            var styleable = target.As<IStyleable>();
            BuildVisualTree(target);

            styleable.Setup(x => x.Classes).Returns(new Classes("bar"));
            var border = (Border)target.Object.VisualChildren.Single();
            var selector = default(Selector).OfType(templatedControl.Object.GetType()).Class("foo").Template().OfType<Border>();
            var activator = selector.Match(border).Activator;

            Assert.False(await activator.Take(1));
        }

        [Fact]
        public void Nested_Selector_Is_Unsubscribed()
        {
            var target = new Mock<IVisual>();
            var templatedControl = target.As<ITemplatedControl>();
            var styleable = target.As<IStyleable>();
            BuildVisualTree(target);

            styleable.Setup(x => x.Classes).Returns(new Classes("foo"));
            var border = (Border)target.Object.VisualChildren.Single();
            var selector = default(Selector).OfType(templatedControl.Object.GetType()).Class("foo").Template().OfType<Border>();
            var activator = selector.Match(border).Activator;
            var inccDebug = (INotifyCollectionChangedDebug)styleable.Object.Classes;

            using (activator.Subscribe(_ => { }))
            {
                Assert.Single(inccDebug.GetCollectionChangedSubscribers());
            }

            Assert.Null(inccDebug.GetCollectionChangedSubscribers());
        }

        private void BuildVisualTree<T>(Mock<T> templatedControl) where T : class, IVisual
        {
            templatedControl.Setup(x => x.VisualChildren).Returns(new Controls.Controls
            {
                new Border
                {
                    [Control.TemplatedParentProperty] = templatedControl.Object,
                    Child = new TextBlock
                    {
                        [Control.TemplatedParentProperty] = templatedControl.Object,
                    },
                },
            });
        }
    }
}
