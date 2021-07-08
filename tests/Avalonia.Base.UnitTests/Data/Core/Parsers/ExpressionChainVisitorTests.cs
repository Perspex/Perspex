using Avalonia.Data.Core.Parsers;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Base.UnitTests.Data.Core.Parsers
{
    public class ExpressionChainVisitorTests
    {
        [Fact]
        public void Should_Not_Include_Method_Before_Root_Object_Access_In_Links()
        {
            // This can happen unwittingly if someone creates a binding in a class and places
            // an "inline" converter method in the chain, e.g.:
            //
            // private class Class1 : NotifyingBase
            // {
            //     public Class1()
            //     {
            //         var binding = TypedBindingExpression.OneWay(data, x => PrependHello(data.Foo));
            //     }
            //     public string Foo { get; set; }
            //     public string PrependHello(string s) => "Hello " + s;
            // }
            //
            // In this case we don't want to subscribe to INPC notifications from `this`.
            var data = new Class2();
            var result = ExpressionChainVisitor<Class1>.Build(x => data.PrependHello(x.Foo));

            Assert.Equal(1, result.Length);
        }

        private class Class1 : NotifyingBase
        {
            public string Foo { get; set; }
        }

        private class Class2 : NotifyingBase
        {
            public string PrependHello(string s) => "Hello " + s;
        }
    }
}
