using System.Reactive.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.LogicalTree;
using Avalonia.Styling;
using Xunit;

namespace Avalonia.Base.UnitTests.Styling
{
    public class SelectorTests_Descendant
    {
        [Fact]
        public void Descendant_Matches_Control_When_It_Is_Child_OfType()
        {
            var parent = new TestLogical1();
            var child = new TestLogical2();

            child.LogicalParent = parent;

            var selector = default(Selector).OfType<TestLogical1>().Descendant().OfType<TestLogical2>();

            Assert.Equal(SelectorMatchResult.AlwaysThisInstance, selector.Match(child).Result);
        }

        [Fact]
        public void Descendant_Matches_Control_When_It_Is_Descendant_OfType()
        {
            var grandparent = new TestLogical1();
            var parent = new TestLogical2();
            var child = new TestLogical3();

            parent.LogicalParent = grandparent;
            child.LogicalParent = parent;

            var selector = default(Selector).OfType<TestLogical1>().Descendant().OfType<TestLogical3>();

            Assert.Equal(SelectorMatchResult.AlwaysThisInstance, selector.Match(child).Result);
        }

        [Fact]
        public async Task Descendant_Matches_Control_When_It_Is_Descendant_OfType_And_Class()
        {
            var grandparent = new TestLogical1();
            var parent = new TestLogical2();
            var child = new TestLogical3();

            grandparent.Classes.Add("foo");
            parent.LogicalParent = grandparent;
            child.LogicalParent = parent;

            var selector = default(Selector).OfType<TestLogical1>().Class("foo").Descendant().OfType<TestLogical3>();
            var activator = selector.Match(child).Activator;

            Assert.True(await activator.Take(1));
        }

        [Fact]
        public async Task Descendant_Doesnt_Match_Control_When_It_Is_Descendant_OfType_But_Wrong_Class()
        {
            var grandparent = new TestLogical1();
            var parent = new TestLogical2();
            var child = new TestLogical3();

            grandparent.Classes.Add("bar");
            parent.LogicalParent = grandparent;
            parent.Classes.Add("foo");
            child.LogicalParent = parent;

            var selector = default(Selector).OfType<TestLogical1>().Class("foo").Descendant().OfType<TestLogical3>();
            var activator = selector.Match(child).Activator;

            Assert.False(await activator.Take(1));
        }

        [Fact]
        public async Task Descendant_Matches_Any_Ancestor()
        {
            var grandparent = new TestLogical1();
            var parent = new TestLogical1();
            var child = new TestLogical3();

            parent.LogicalParent = grandparent;
            child.LogicalParent = parent;

            var selector = default(Selector).OfType<TestLogical1>().Class("foo").Descendant().OfType<TestLogical3>();
            var activator = selector.Match(child).Activator.ToObservable();

            Assert.False(await activator.Take(1));
            parent.Classes.Add("foo");
            Assert.True(await activator.Take(1));
            grandparent.Classes.Add("foo");
            Assert.True(await activator.Take(1));
            parent.Classes.Remove("foo");
            Assert.True(await activator.Take(1));
            grandparent.Classes.Remove("foo");
            Assert.False(await activator.Take(1));
        }

        [Fact]
        public void Descendant_Selector_Should_Have_Correct_String_Representation()
        {
            var selector = default(Selector).OfType<TestLogical1>().Class("foo").Descendant().OfType<TestLogical3>();

            Assert.Equal("TestLogical1.foo TestLogical3", selector.ToString());
        }

        public abstract class TestLogical : Control
        {
            public ILogical LogicalParent
            {
                get => Parent;
                set => ((ISetLogicalParent)this).SetParent(value);
            }
        }

        public class TestLogical1 : TestLogical
        {
        }

        public class TestLogical2 : TestLogical
        {
        }

        public class TestLogical3 : TestLogical
        {
        }
    }
}
