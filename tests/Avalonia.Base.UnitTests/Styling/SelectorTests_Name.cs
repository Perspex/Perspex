using Avalonia.Controls;
using Avalonia.Styling;
using Moq;
using Xunit;

namespace Avalonia.Base.UnitTests.Styling
{
    public class SelectorTests_Name
    {
        [Fact]
        public void Name_Matches_Control_With_Correct_Name()
        {
            var control = new Control1 { Name = "foo" };
            var target = default(Selector).Name("foo");

            Assert.Equal(SelectorMatchResult.AlwaysThisInstance, target.Match(control).Result);
        }

        [Fact]
        public void Name_Doesnt_Match_Control_Of_Wrong_Name()
        {
            var control = new Control1 { Name = "foo" };
            var target = default(Selector).Name("bar");

            Assert.Equal(SelectorMatchResult.NeverThisInstance, target.Match(control).Result);
        }

        [Fact]
        public void Name_Doesnt_Match_Control_With_TemplatedParent()
        {
            var control = new Control1 { TemplatedParent = new Mock<ITemplatedControl>().Object };
            var target = default(Selector).Name("foo");
            var activator = target.Match(control);

            Assert.Equal(SelectorMatchResult.NeverThisInstance, target.Match(control).Result);
        }

        [Fact]
        public void Name_Has_Correct_String_Representation()
        {
            var target = default(Selector).Name("foo");

            Assert.Equal("#foo", target.ToString());
        }

        [Fact]
        public void Type_And_Name_Has_Correct_String_Representation()
        {
            var target = default(Selector).OfType<Control1>().Name("foo");

            Assert.Equal("Control1#foo", target.ToString());
        }

        public class Control1 : Control
        {
        }
    }
}
