using System.Collections.Generic;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Styling;
using Moq;
using Xunit;

namespace Avalonia.Base.UnitTests.Styling
{
    public class SelectorTests_Class
    {
        [Fact]
        public void Class_Selector_Should_Have_Correct_String_Representation()
        {
            var target = default(Selector).Class("foo");

            Assert.Equal(".foo", target.ToString());
        }

        [Fact]
        public void PesudoClass_Selector_Should_Have_Correct_String_Representation()
        {
            var target = default(Selector).Class(":foo");

            Assert.Equal(":foo", target.ToString());
        }

        [Fact]
        public async Task Class_Matches_Control_With_Class()
        {
            var control = new Control1
            {
                Classes = { "foo" },
            };

            var target = default(Selector).Class("foo");
            var match = target.Match(control);

            Assert.Equal(SelectorMatchResult.Sometimes, match.Result);
            Assert.True(await match.Activator.Take(1));
        }

        [Fact]
        public async Task Class_Doesnt_Match_Control_Without_Class()
        {
            var control = new Control1
            {
                Classes = { "bar" },
            };

            var target = default(Selector).Class("foo");
            var match = target.Match(control);

            Assert.Equal(SelectorMatchResult.Sometimes, match.Result);
            Assert.False(await match.Activator.Take(1));
        }

        [Fact]
        public async Task Class_Matches_Control_With_TemplatedParent()
        {
            var control = new Control1
            {
                Classes = { "foo" },
                TemplatedParent = new Mock<ITemplatedControl>().Object,
            };

            var target = default(Selector).Class("foo");
            var match = target.Match(control);

            Assert.Equal(SelectorMatchResult.Sometimes, match.Result);
            Assert.True(await match.Activator.Take(1));
        }

        [Fact]
        public async Task Class_Tracks_Additions()
        {
            var control = new Control1();

            var target = default(Selector).Class("foo");
            var activator = target.Match(control).Activator.ToObservable();

            Assert.False(await activator.Take(1));
            control.Classes.Add("foo");
            Assert.True(await activator.Take(1));
        }

        [Fact]
        public async Task Class_Tracks_Removals()
        {
            var control = new Control1
            {
                Classes = { "foo" },
            };

            var target = default(Selector).Class("foo");
            var activator = target.Match(control).Activator.ToObservable();

            Assert.True(await activator.Take(1));
            control.Classes.Remove("foo");
            Assert.False(await activator.Take(1));
        }

        [Fact]
        public async Task Multiple_Classes()
        {
            var control = new Control1();
            var target = default(Selector).Class("foo").Class("bar");
            var activator = target.Match(control).Activator.ToObservable();

            Assert.False(await activator.Take(1));
            control.Classes.Add("foo");
            Assert.False(await activator.Take(1));
            control.Classes.Add("bar");
            Assert.True(await activator.Take(1));
            control.Classes.Remove("bar");
            Assert.False(await activator.Take(1));
        }

        [Fact]
        public void Only_Notifies_When_Result_Changes()
        {
            // Test for #1698
            var control = new Control1
            {
                Classes = { "foo" },
            };

            var target = default(Selector).Class("foo");
            var activator = target.Match(control).Activator;
            var result = new List<bool>();

            using (activator.Subscribe(x => result.Add(x)))
            {
                control.Classes.Add("bar");
                control.Classes.Remove("foo");
            }

            Assert.Equal(new[] { true, false }, result);
        }

        public class Control1 : Control
        {
        }
    }
}
