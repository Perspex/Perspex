using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Avalonia.Diagnostics;
using Avalonia.Data.Core;
using Xunit;

namespace Avalonia.Base.UnitTests.Data.Core
{
    public class TypedBindingExpressionTests_AttachedProperty
    {
        [Fact]
        public async Task Should_Get_Attached_Property_Value()
        {
            var data = new Class1();
            var target = TypedBindingExpression.OneWay(data, o => o[Owner.FooProperty]);
            var result = await target.Take(1);

            Assert.Equal("foo", result.Value);

            Assert.Null(((IAvaloniaObjectDebug)data).GetPropertyChangedSubscribers());
        }

        [Fact]
        public async Task Should_Get_Chained_Attached_Property_Value()
        {
            var data = new Class1
            {
                Next = new Class1
                {
                    [Owner.FooProperty] = "bar",
                }
            };

            var target = TypedBindingExpression.OneWay(data, o => o.Next[Owner.FooProperty]);
            var result = await target.Take(1);

            Assert.Equal("bar", result.Value);

            Assert.Null(((IAvaloniaObjectDebug)data).GetPropertyChangedSubscribers());
        }

        [Fact]
        public void Should_Track_Simple_Attached_Value()
        {
            var data = new Class1();
            var target = TypedBindingExpression.OneWay(data, o => o[Owner.FooProperty]);
            var result = new List<object>();

            var sub = target.Subscribe(x => result.Add(x.Value));
            data.SetValue(Owner.FooProperty, "bar");

            Assert.Equal(new[] { "foo", "bar" }, result);

            sub.Dispose();

            Assert.Null(((IAvaloniaObjectDebug)data).GetPropertyChangedSubscribers());
        }

        [Fact]
        public void Should_Track_Chained_Attached_Value()
        {
            var data = new Class1
            {
                Next = new Class1
                {
                    [Owner.FooProperty] = "foo",
                }
            };

            var target = TypedBindingExpression.OneWay(data, o => o.Next[Owner.FooProperty]);
            var result = new List<object>();

            var sub = target.Subscribe(x => result.Add(x.Value));
            data.Next.SetValue(Owner.FooProperty, "bar");

            Assert.Equal(new[] { "foo", "bar" }, result);

            sub.Dispose();

            Assert.Null(((IAvaloniaObjectDebug)data).GetPropertyChangedSubscribers());
        }

        [Fact]
        public void Should_Not_Keep_Source_Alive()
        {
            Func<Tuple<TypedBindingExpression<Class1, object>, WeakReference>> run = () =>
            {
                var source = new Class1();
                var target = TypedBindingExpression.OneWay(source, o => o.Next[Owner.FooProperty]);
                return Tuple.Create(target, new WeakReference(source));
            };

            var result = run();
            result.Item1.Subscribe(x => { });

            GC.Collect();

            Assert.Null(result.Item2.Target);
        }

        private static class Owner
        {
            public static readonly AttachedProperty<string> FooProperty =
                AvaloniaProperty.RegisterAttached<Class1, string>(
                    "Foo",
                    typeof(Owner),
                    defaultValue: "foo");
        }

        private class Class1 : AvaloniaObject
        {
            public static readonly StyledProperty<Class1> NextProperty =
                AvaloniaProperty.Register<Class1, Class1>(nameof(Next));

            public Class1 Next
            {
                get { return GetValue(NextProperty); }
                set { SetValue(NextProperty, value); }
            }
        }
    }
}
