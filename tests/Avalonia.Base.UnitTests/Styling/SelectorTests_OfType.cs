using Avalonia.Controls;
using Avalonia.Styling;
using Moq;
using Xunit;

namespace Avalonia.Base.UnitTests.Styling
{
    public class SelectorTests_OfType
    {
        [Fact]
        public void OfType_Matches_Control_Of_Correct_Type()
        {
            var control = new Control1();
            var target = default(Selector).OfType<Control1>();

            Assert.Equal(SelectorMatchResult.AlwaysThisType, target.Match(control).Result);
        }

        [Fact]
        public void OfType_Doesnt_Match_Control_Of_Wrong_Type()
        {
            var control = new Control2();
            var target = default(Selector).OfType<Control1>();

            Assert.Equal(SelectorMatchResult.NeverThisType, target.Match(control).Result);
        }

        [Fact]
        public void OfType_Class_Doesnt_Match_Control_Of_Wrong_Type()
        {
            var control = new Control2();
            var target = default(Selector).OfType<Control1>().Class("foo");

            Assert.Equal(SelectorMatchResult.NeverThisType, target.Match(control).Result);
        }

        [Fact]
        public void OfType_Matches_Control_With_TemplatedParent()
        {
            var control = new Control1 { TemplatedParent = new Mock<ITemplatedControl>().Object };
            var target = default(Selector).OfType<Control1>();

            Assert.Equal(SelectorMatchResult.AlwaysThisType, target.Match(control).Result);
        }

        public class Control1 : Control
        {
        }

        public class Control2 : Control
        {
        }
    }
}
