using Avalonia.Controls;
using Avalonia.Styling;
using Xunit;

namespace Avalonia.Base.UnitTests.Styling
{
    public class SelectorTests_Or
    {
        [Fact]
        public void Or_Selector_Should_Have_Correct_String_Representation()
        {
            var target = Selectors.Or(
                default(Selector).OfType<Control1>().Class("foo"),
                default(Selector).OfType<Control2>().Class("bar"));

            Assert.Equal("Control1.foo, Control2.bar", target.ToString());
        }

        [Fact]
        public void Or_Selector_Matches_Control_Of_Correct_Type()
        {
            var target = Selectors.Or(
                default(Selector).OfType<Control1>(),
                default(Selector).OfType<Control2>().Class("bar"));
            var control = new Control1();

            Assert.Equal(SelectorMatchResult.AlwaysThisType, target.Match(control).Result);
        }

        [Fact]
        public void Or_Selector_Matches_Control_Of_Correct_Type_With_Class()
        {
            var target = Selectors.Or(
                default(Selector).OfType<Control1>(),
                default(Selector).OfType<Control2>().Class("bar"));
            var control = new Control2();

            Assert.Equal(SelectorMatchResult.Sometimes, target.Match(control).Result);
        }

        [Fact]
        public void Or_Selector_Doesnt_Match_Control_Of_Incorrect_Type()
        {
            var target = Selectors.Or(
                default(Selector).OfType<Control1>(),
                default(Selector).OfType<Control2>().Class("bar"));
            var control = new Control3();

            Assert.Equal(SelectorMatchResult.NeverThisType, target.Match(control).Result);
        }

        [Fact]
        public void Or_Selector_Doesnt_Match_Control_With_Incorrect_Name()
        {
            var target = Selectors.Or(
                default(Selector).OfType<Control1>().Name("foo"),
                default(Selector).OfType<Control2>().Name("foo"));
            var control = new Control1 { Name = "bar" };

            Assert.Equal(SelectorMatchResult.NeverThisInstance, target.Match(control).Result);
        }

        [Fact]
        public void Returns_Correct_TargetType_When_Types_Same()
        {
            var target = Selectors.Or(
                default(Selector).OfType<Control1>().Class("foo"),
                default(Selector).OfType<Control1>().Class("bar"));

            Assert.Equal(typeof(Control1), target.TargetType);
        }

        [Fact]
        public void Returns_Common_TargetType()
        {
            var target = Selectors.Or(
                default(Selector).OfType<Control1>().Class("foo"),
                default(Selector).OfType<Control2>().Class("bar"));

            Assert.Equal(typeof(Control), target.TargetType);
        }

        [Fact]
        public void Returns_Null_TargetType_When_A_Selector_Has_No_TargetType()
        {
            var target = Selectors.Or(
                default(Selector).OfType<Control1>().Class("foo"),
                default(Selector).Class("bar"));

            Assert.Equal(null, target.TargetType);
        }

        public class Control1 : Control
        {
        }

        public class Control2 : Control
        {
        }

        public class Control3 : Control
        {
        }
    }
}
