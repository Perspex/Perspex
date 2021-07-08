using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Avalonia.Data;
using Avalonia.Data.Core;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Base.UnitTests.Data.Core
{
    public class TypedBindingExpressionTests_TwoWay
    {
        [Fact]
        public void Should_Set_Simple_Property_Value()
        {
            var data = new Class1 { Foo = "123" };
            var target = TypedBindingExpression.TwoWay(data, o => o.Foo, (o, v) => o.Foo = v);

            using (target.Subscribe())
            {
                target.OnNext("321");

                Assert.Equal("321", data.Foo);
            }

            GC.KeepAlive(data);
        }

        [Fact]
        public void Should_Set_Nested_Property_Value()
        {
            var data = new Class1 { Next = new Class2 { Bar = "123" } };
            var target = TypedBindingExpression.TwoWay(data, o => o.Next.Bar, (o, v) => o.Next.Bar = v);

            using (target.Subscribe())
            {
                target.OnNext("321");

                Assert.Equal("321", data.Next.Bar);
            }

            GC.KeepAlive(data);
        }

        [Fact]
        public void Should_Notify_Changed_Value()
        {
            var data = new Class1 { Foo = "123" };
            var target = TypedBindingExpression.TwoWay(data, o => o.Foo, (o, v) => o.Foo = v);
            var result = new List<string>();

            using (target.Subscribe(x => result.Add(x.Value)))
            {
                target.OnNext("321");

                Assert.Equal(new[] { "123", "321" }, result);
            }

            GC.KeepAlive(data);
        }

        [Fact]
        public void Should_Notify_Changed_Value_Non_INPC()
        {
            var data = new NoInpc { Foo = "123" };
            var target = TypedBindingExpression.TwoWay(data, o => o.Foo, (o, v) => o.Foo = v);
            var result = new List<string>();

            using (target.Subscribe(x => result.Add(x.Value)))
            {
                target.OnNext("321");

                Assert.Equal(new[] { "123", "321" }, result);
            }

            GC.KeepAlive(data);
        }

        [Fact]
        public void Should_Handle_Broken_Chain()
        {
            var data = new Class1 { Next = new Class2 { Bar = "bar" } };
            var target = TypedBindingExpression.TwoWay(data, o => o.Next.Bar, (o, v) => o.Next.Bar = v);
            var result = new List<BindingValue<string>>();

            using (target.Subscribe(x => result.Add(x)))
            {
                data.Next = null;
                target.OnNext("foo");

                Assert.Equal(3, result.Count);
                Assert.Equal("bar", result[0].Value);
                Assert.IsType<NullReferenceException>(result[1].Error);
                Assert.IsType<NullReferenceException>(result[2].Error);
            }
        }

        /// <summary>
        /// Test for #831 - Bound properties are incorrectly updated when changing tab items.
        /// </summary>
        /// <remarks>
        /// There was a bug whereby pushing a null as the ExpressionObserver root didn't update
        /// the leaf node, cauing a subsequent SetValue to update an object that should have become
        /// unbound.
        /// </remarks>
        [Fact]
        public void Pushing_Null_To_RootObservable_Updates_Correct_Object()
        {
            var data = new Class1 { Next = new Class2 { Bar = "bar" } };
            var rootObservable = new BehaviorSubject<Class1>(data);
            var target = TypedBindingExpression.TwoWay(rootObservable, o => o.Next.Bar, (o, v) => o.Next.Bar = v);

            using (target.Subscribe())
            {
                rootObservable.OnNext(null);
                target.OnNext("baz");
                Assert.Equal("bar", data.Next.Bar);
            }
        }

        private class Class1 : NotifyingBase
        {
            private string _foo;
            private Class2 _next;

            public string Foo
            {
                get => _foo;
                set
                {
                    _foo = value;
                    RaisePropertyChanged(nameof(Foo));
                }
            }

            public Class2 Next
            {
                get => _next;
                set
                {
                    _next = value;
                    RaisePropertyChanged(nameof(Next));
                }
            }
        }

        private class Class2 : NotifyingBase
        {
            private string _bar;

            public string Bar
            {
                get { return _bar; }
                set
                {
                    _bar = value;
                    RaisePropertyChanged(nameof(Bar));
                }
            }
        }

        private class NoInpc
        {
            public string Foo { get; set; }
        }
    }
}
