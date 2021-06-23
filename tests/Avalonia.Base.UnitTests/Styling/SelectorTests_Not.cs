using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Styling;
using Xunit;

namespace Avalonia.Base.UnitTests.Styling
{
    public class SelectorTests_Not
    {
        [Fact]
        public void Not_Selector_Should_Have_Correct_String_Representation()
        {
            var target = default(Selector).Not(x => x.Class("foo"));

            Assert.Equal(":not(.foo)", target.ToString());
        }

        [Fact]
        public void Not_OfType_Matches_Control_Of_Incorrect_Type()
        {
            var control = new Control1();
            var target = default(Selector).Not(x => x.OfType<Control1>());

            Assert.Equal(SelectorMatchResult.NeverThisType, target.Match(control).Result);
        }
        
        [Fact]
        public void Not_OfType_Doesnt_Match_Control_Of_Correct_Type()
        {
            var control = new Control2();
            var target = default(Selector).Not(x => x.OfType<Control1>());

            Assert.Equal(SelectorMatchResult.AlwaysThisType, target.Match(control).Result);
        }

        [Fact]
        public async Task Not_Class_Doesnt_Match_Control_With_Class()
        {
            var control = new Control1
            {
                Classes = { "foo" },
            };

            var target = default(Selector).Not(x => x.Class("foo"));
            var match = target.Match(control);

            Assert.Equal(SelectorMatchResult.Sometimes, match.Result);
            Assert.False(await match.Activator.Take(1));
        }

        [Fact]
        public async Task Not_Class_Matches_Control_Without_Class()
        {
            var control = new Control1
            {
                Classes = { "bar" },
            };

            var target = default(Selector).Not(x => x.Class("foo"));
            var match = target.Match(control);

            Assert.Equal(SelectorMatchResult.Sometimes, match.Result);
            Assert.True(await match.Activator.Take(1));
        }

        [Fact]
        public async Task OfType_Not_Class_Matches_Control_Without_Class()
        {
            var control = new Control1
            {
                Classes = { "bar" },
            };

            var target = default(Selector).OfType<Control1>().Not(x => x.Class("foo"));
            var match = target.Match(control);

            Assert.Equal(SelectorMatchResult.Sometimes, match.Result);
            Assert.True(await match.Activator.Take(1));
        }

        [Fact]
        public void OfType_Not_Class_Doesnt_Match_Control_Of_Wrong_Type()
        {
            var control = new Control2
            {
                Classes = { "foo" },
            };

            var target = default(Selector).OfType<Control1>().Not(x => x.Class("foo"));
            var match = target.Match(control);

            Assert.Equal(SelectorMatchResult.NeverThisType, match.Result);
        }

        [Fact]
        public void Returns_Correct_TargetType()
        {
            var target = default(Selector).OfType<Control1>().Not(x => x.Class("foo"));

            Assert.Equal(typeof(Control1), target.TargetType);
        }

        public class Control1 : Control
        {
        }

        public class Control2 : Control
        {
        }
    }
}
