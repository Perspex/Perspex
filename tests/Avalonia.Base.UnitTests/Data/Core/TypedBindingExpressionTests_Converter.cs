using System;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Avalonia.Data.Core;
using Xunit;

namespace Avalonia.Base.UnitTests.Data.Core
{
    public class TypedBindingExpressionTests_Converter
    {
        [Fact]
        public async Task Should_Convert_Simple_Property_Value()
        {
            var data = new { Foo = "123" };
            var target = TypedBindingExpression.OneWay(data, o => o.Foo, x => int.Parse(x));
            var result = await target.Take(1);

            Assert.Equal(123, result.Value);

            GC.KeepAlive(data);
        }

        [Fact]
        public async Task Should_Convert_Simple_Property_Chain()
        {
            var data = new { Foo = new { Bar = new { Baz = "321" } } };
            var target = TypedBindingExpression.OneWay(data, o => o.Foo.Bar.Baz, x => int.Parse(x));
            var result = await target.Take(1);

            Assert.Equal(321, result.Value);

            GC.KeepAlive(data);
        }

        [Fact]
        public void Should_Convert_TwoWay()
        {
            var data = new Class1 { Foo = "123" };
            var target = TypedBindingExpression.TwoWay(
                data,
                o => o.Foo,
                (o, v) => o.Foo = v,
                x => int.Parse(x),
                x => x.ToString());

            using (target.Subscribe())
            {
                target.OnNext(321);

                Assert.Equal("321", data.Foo);
            }

            GC.KeepAlive(data);
        }

        private class Class1
        {
            public string Foo { get; set; }
        }
    }
}
