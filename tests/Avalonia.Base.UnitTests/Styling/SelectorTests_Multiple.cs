using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Styling;
using Avalonia.VisualTree;
using Xunit;

namespace Avalonia.Base.UnitTests.Styling
{
    public class SelectorTests_Multiple
    {
        [Fact]
        public void Named_Template_Child_Of_Control_With_Two_Classes()
        {
            var template = new FuncControlTemplate((parent, scope) =>
            {
                return new Border
                {
                    Name = "border",
                }.RegisterInNameScope(scope);
            });

            var control = new Button
            {
                Template = template,
            };

            control.ApplyTemplate();

            var selector = default(Selector)
                .OfType<Button>()
                .Class("foo")
                .Class("bar")
                .Template()
                .Name("border");

            var border = (Border)((IVisual)control).VisualChildren.Single();
            var values = new List<bool>();
            var match = selector.Match(border);

            Assert.Equal(SelectorMatchResult.Sometimes, match.Result);
            match.Activator.Subscribe(x => values.Add(x));

            Assert.Equal(new[] { false }, values);
            control.Classes.AddRange(new[] { "foo", "bar" });
            Assert.Equal(new[] { false, true }, values);
            control.Classes.Remove("foo");
            Assert.Equal(new[] { false, true, false }, values);
        }

        [Fact]
        public void Named_OfType_Template_Child_Of_Control_With_Two_Classes_Wrong_Type()
        {
            var template = new FuncControlTemplate((parent, scope) =>
            {
                return new Border
                {
                    Name = "border",
                }.RegisterInNameScope(scope);
            });

            var control = new Button
            {
                Template = template,
            };

            control.ApplyTemplate();

            var selector = default(Selector)
                .OfType<Button>()
                .Class("foo")
                .Class("bar")
                .Template()
                .OfType<TextBlock>()
                .Name("baz");

            var border = (Border)((IVisual)control).VisualChildren.Single();
            var values = new List<bool>();
            var match = selector.Match(border);

            Assert.Equal(SelectorMatchResult.NeverThisType, match.Result);
        }

        [Fact]
        public void Control_With_Class_Descendent_Of_Control_With_Two_Classes()
        {
            var textBlock = new TextBlock();
            var control = new Button { Content = textBlock };

            control.ApplyTemplate();

            var selector = default(Selector)
                .OfType<Button>()
                .Class("foo")
                .Class("bar")
                .Descendant()
                .OfType<TextBlock>()
                .Class("baz");

            var values = new List<bool>();
            var match = selector.Match(textBlock);

            Assert.Equal(SelectorMatchResult.Sometimes, match.Result);
            match.Activator.Subscribe(x => values.Add(x));

            Assert.Equal(new[] { false }, values);
            control.Classes.AddRange(new[] { "foo", "bar" });
            Assert.Equal(new[] { false }, values);
            textBlock.Classes.Add("baz");
            Assert.Equal(new[] { false, true }, values);
        }

        [Fact]
        public void Named_Class_Template_Child_Of_Control()
        {
            var template = new FuncControlTemplate((parent, scope) =>
            {
                return new Border
                {
                    Name = "border",
                }.RegisterInNameScope(scope);
            });

            var control = new Button
            {
                Template = template,
            };

            control.ApplyTemplate();

            var selector = default(Selector)
                .OfType<Button>()
                .Template()
                .Name("border")
                .Class("foo");

            var border = (Border)((IVisual)control).VisualChildren.Single();
            var values = new List<bool>();
            var match = selector.Match(border);

            Assert.Equal(SelectorMatchResult.Sometimes, match.Result);
            match.Activator.Subscribe(x => values.Add(x));

            Assert.Equal(new[] { false }, values);
            border.Classes.AddRange(new[] { "foo" });
            Assert.Equal(new[] { false, true }, values);
            border.Classes.Remove("foo");
            Assert.Equal(new[] { false, true, false }, values);
        }

        [Fact]
        public async Task Nested_PropertyEquals()
        {
            var control = new Canvas();
            var parent = new Border { Child = control };

            var target = default(Selector)
                .OfType<Border>()
                .PropertyEquals(Border.TagProperty, "foo")
                .Child()
                .OfType<Canvas>()
                .PropertyEquals(Canvas.TagProperty, "bar");

            var match = target.Match(control);
            Assert.Equal(SelectorMatchResult.Sometimes, match.Result);

            var activator = match.Activator;

            Assert.False(await activator.Take(1));
            control.Tag = "bar";
            Assert.False(await activator.Take(1));
            parent.Tag = "foo";
            Assert.True(await activator.Take(1));
        }

        [Fact]
        public void TargetType_OfType()
        {
            var selector = default(Selector).OfType<Button>();

            Assert.Equal(typeof(Button), selector.TargetType);
        }

        [Fact]
        public void TargetType_OfType_Class()
        {
            var selector = default(Selector)
                .OfType<Button>()
                .Class("foo");

            Assert.Equal(typeof(Button), selector.TargetType);
        }

        [Fact]
        public void TargetType_Is_Class()
        {
            var selector = default(Selector)
                .Is<Button>()
                .Class("foo");

            Assert.Equal(typeof(Button), selector.TargetType);
        }

        [Fact]
        public void TargetType_Child()
        {
            var selector = default(Selector)
                .OfType<Button>()
                .Child()
                .OfType<TextBlock>();

            Assert.Equal(typeof(TextBlock), selector.TargetType);
        }

        [Fact]
        public void TargetType_Descendant()
        {
            var selector = default(Selector)
                .OfType<Button>()
                .Descendant()
                .OfType<TextBlock>();

            Assert.Equal(typeof(TextBlock), selector.TargetType);
        }

        [Fact]
        public void TargetType_Template()
        {
            var selector = default(Selector)
                .OfType<Button>()
                .Template()
                .OfType<TextBlock>();

            Assert.Equal(typeof(TextBlock), selector.TargetType);
        }
    }
}
