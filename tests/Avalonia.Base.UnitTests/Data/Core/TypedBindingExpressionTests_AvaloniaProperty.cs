using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Avalonia.Diagnostics;
using Avalonia.Data.Core;
using Xunit;

namespace Avalonia.Base.UnitTests.Data.Core
{
    public class TypedBindingExpressionTests_AvaloniaProperty
    {
        public TypedBindingExpressionTests_AvaloniaProperty()
        {
            var foo = Class1.FooProperty;
        }

        [Fact]
        public async Task Should_Get_Simple_Property_Value()
        {
            var data = new Class1();
            var target = TypedBindingExpression.OneWay(data, o => o.Foo);
            var result = await target.Take(1);

            Assert.Equal("foo", result.Value);

            Assert.Null(((IAvaloniaObjectDebug)data).GetPropertyChangedSubscribers());
        }

        [Fact]
        public async Task Should_Get_Simple_ClrProperty_Value()
        {
            var data = new Class1();
            var target = TypedBindingExpression.OneWay(data, o => o.ClrProperty);
            var result = await target.Take(1);

            Assert.Equal("clr-property", result.Value);
        }

        [Fact]
        public void Should_Track_Simple_Property_Value()
        {
            var data = new Class1();
            var target = TypedBindingExpression.OneWay(data, o => o.Foo);
            var result = new List<string>();

            var sub = target.Subscribe(x => result.Add(x.Value));
            data.SetValue(Class1.FooProperty, "bar");

            Assert.Equal(new[] { "foo", "bar" }, result);

            sub.Dispose();

            Assert.Null(((IAvaloniaObjectDebug)data).GetPropertyChangedSubscribers());
        }

        [Fact]
        public void Should_Not_Keep_Source_Alive()
        {
            Func<Tuple<TypedBindingExpression<Class1, string>, WeakReference>> run = () =>
            {
                var source = new Class1();
                var target = TypedBindingExpression.OneWay(source, o => o.Foo);
                return Tuple.Create(target, new WeakReference(source));
            };

            var result = run();
            result.Item1.Subscribe(x => { });

            GC.Collect();

            Assert.Null(result.Item2.Target);
        }

        private class Class1 : AvaloniaObject
        {
            public static readonly StyledProperty<string> FooProperty =
                AvaloniaProperty.Register<Class1, string>("Foo", defaultValue: "foo");

            public string Foo { get => GetValue(FooProperty); set => SetValue(FooProperty, value); }

            public string ClrProperty { get; } = "clr-property";
        }
    }
}
