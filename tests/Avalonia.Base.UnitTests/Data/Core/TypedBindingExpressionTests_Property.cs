using System;
using System.Collections.Generic;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using Avalonia.Data;
using Avalonia.Data.Core;
using Avalonia.UnitTests;
using Microsoft.Reactive.Testing;
using Xunit;

namespace Avalonia.Base.UnitTests.Data.Core
{
    public class TypedBindingExpressionTests_Property
    {
        [Fact]
        public async Task Should_Get_Root_Value()
        {
            var target = TypedBindingExpression.OneWay("foo", o => o);
            var result = await target.Take(1);

            Assert.Equal("foo", result.Value);
        }

        [Fact]
        public async Task Should_Get_Simple_Property_Value()
        {
            var data = new { Foo = "foo" };
            var target = TypedBindingExpression.OneWay(data, o => o.Foo);
            var result = await target.Take(1);

            Assert.Equal("foo", result.Value);

            GC.KeepAlive(data);
        }

        [Fact]
        public async Task Should_Get_Simple_Property_Value_Null()
        {
            var data = new { Foo = (string)null };
            var target = TypedBindingExpression.OneWay(data, o => o.Foo);
            var result = await target.Take(1);

            Assert.Null(result.Value);

            GC.KeepAlive(data);
        }

        [Fact]
        public async Task Should_Get_Simple_Property_From_Base_Class()
        {
            var data = new Class3 { Foo = "foo" };
            var target = TypedBindingExpression.OneWay(data, o => o.Foo);
            var result = await target.Take(1);

            Assert.Equal("foo", result.Value);

            GC.KeepAlive(data);
        }

        [Fact]
        public void Should_Produce_Error_When_Root_Null()
        {
            var source = new BehaviorSubject<Class1>(null);
            var target = TypedBindingExpression.OneWay(source, o => o.Foo);
            var result = new List<BindingValue<string>>();

            target.Subscribe(x => result.Add(x));

            Assert.Equal(1, result.Count);
            Assert.IsType<NullReferenceException>(result[0].Error);
        }

        [Fact]
        public void Should_Produce_FallbackValue_When_Root_Null()
        {
            var source = new BehaviorSubject<Class1>(null);
            var target = TypedBindingExpression.OneWay(source, o => o.Foo, "fallback");
            var result = new List<BindingValue<string>>();

            target.Subscribe(x => result.Add(x));

            Assert.Equal(1, result.Count);
            Assert.IsType<NullReferenceException>(result[0].Error);
            Assert.Equal("fallback", result[0].Value);
        }

        [Fact]
        public void Should_Produce_Error_When_Root_Changed_To_Null()
        {
            var source = new BehaviorSubject<Class1>(new Class1 { Foo = "foo" });
            var target = TypedBindingExpression.OneWay(source, o => o.Foo);
            var result = new List<BindingValue<string>>();

            target.Subscribe(x => result.Add(x));
            source.OnNext(null);

            Assert.Equal(2, result.Count);
            Assert.Equal("foo", result[0].Value);
            Assert.IsType<NullReferenceException>(result[1].Error);
        }

        [Fact]
        public void Should_Not_Produce_Anything_Until_Root_Produces_First_Value()
        {
            var source = new Subject<Class1>();
            var target = TypedBindingExpression.OneWay(source, o => o.Foo);
            var result = new List<BindingValue<string>>();

            using (target.Subscribe(x => result.Add(x)))
            {
                Assert.Empty(result);

                // Ensure that the second subscription doesn't produce a value either.
                target.Subscribe(x => result.Add(x)).Dispose();

                Assert.Empty(result);

                source.OnNext(new Class1 { Foo = "foo" });

                Assert.Equal(1, result.Count);
                Assert.Equal("foo", result[0].Value);
            }
        }

        [Fact]
        public async Task Should_Get_Simple_Property_Chain()
        {
            var data = new { Foo = new { Bar = new { Baz = "baz" } } };
            var target = TypedBindingExpression.OneWay(data, o => o.Foo.Bar.Baz);
            var result = await target.Take(1);

            Assert.Equal("baz", result.Value);

            GC.KeepAlive(data);
        }

        [Fact]
        public void Should_Track_Simple_Property_Value()
        {
            var data = new Class1 { Foo = "foo" };
            var target = TypedBindingExpression.OneWay(data, o => o.Foo);
            var result = new List<object>();

            var sub = target.Subscribe(x => result.Add(x.Value));
            data.Foo = "bar";

            Assert.Equal(new[] { "foo", "bar" }, result);

            sub.Dispose();

            Assert.Equal(0, data.PropertyChangedSubscriptionCount);

            GC.KeepAlive(data);
        }

        [Fact]
        public void Should_Track_End_Of_Property_Chain_Changing()
        {
            var data = new Class1 { Next = new Class2 { Bar = "bar" } };
            var target = TypedBindingExpression.OneWay(data, o => (o.Next as Class2).Bar);
            var result = new List<object>();

            var sub = target.Subscribe(x => result.Add(x.Value));
            ((Class2)data.Next).Bar = "baz";
            ((Class2)data.Next).Bar = null;

            Assert.Equal(new[] { "bar", "baz", null }, result);

            sub.Dispose();

            Assert.Equal(0, data.PropertyChangedSubscriptionCount);
            Assert.Equal(0, data.Next.PropertyChangedSubscriptionCount);

            GC.KeepAlive(data);
        }

        [Fact]
        public void Should_Track_Property_Chain_Changing()
        {
            var data = new Class1 { Next = new Class2 { Bar = "bar" } };
            var target = TypedBindingExpression.OneWay(data, o => (o.Next as Class2).Bar);
            var result = new List<object>();

            var sub = target.Subscribe(x => result.Add(x.Value));
            var old = data.Next;
            data.Next = new Class2 { Bar = "baz" };
            data.Next = new Class2 { Bar = null };
            ((Class2)data.Next).Bar = "qux";

            Assert.Equal(1, data.Next.PropertyChangedSubscriptionCount);
            Assert.Equal(0, old.PropertyChangedSubscriptionCount);

            Assert.Equal(new[] { "bar", "baz", null, "qux" }, result);

            sub.Dispose();

            Assert.Equal(0, data.PropertyChangedSubscriptionCount);
            Assert.Equal(0, data.Next.PropertyChangedSubscriptionCount);
            Assert.Equal(0, old.PropertyChangedSubscriptionCount);

            GC.KeepAlive(data);
        }

        [Fact]
        public void Should_Track_Property_Chain_Breaking_With_Null_Then_Mending()
        {
            var data = new Class1
            {
                Next = new Class2
                {
                    Next = new Class2
                    {
                        Bar = "bar"
                    }
                }
            };

            var target = TypedBindingExpression.OneWay(data, o => ((o.Next as Class2).Next as Class2).Bar);
            var result = new List<BindingValue<string>>();

            var sub = target.Subscribe(x => result.Add(x));
            var old = data.Next;
            data.Next = new Class2 { Bar = "baz" };
            data.Next = old;

            Assert.Equal(3, result.Count);
            Assert.Equal(new BindingValue<string>("bar"), result[0]);
            Assert.IsType<NullReferenceException>(result[1].Error);
            Assert.Equal(new BindingValue<string>("bar"), result[2]);

            sub.Dispose();

            Assert.Equal(0, data.PropertyChangedSubscriptionCount);
            Assert.Equal(0, data.Next.PropertyChangedSubscriptionCount);
            Assert.Equal(0, old.PropertyChangedSubscriptionCount);

            GC.KeepAlive(data);
        }

        [Fact]
        public void Should_Track_Property_Chain_Breaking_With_Missing_Member_Then_Mending()
        {
            var data = new Class1 { Next = new Class2 { Bar = "bar" } };
            var target = TypedBindingExpression.OneWay(data, o => (o.Next as Class2).Bar);
            var result = new List<BindingValue<string>>();

            var sub = target.Subscribe(x => result.Add(x));
            var old = data.Next;
            var breaking = new WithoutBar();
            data.Next = breaking;
            data.Next = new Class2 { Bar = "baz" };

            Assert.Equal(3, result.Count);
            Assert.Equal(new BindingValue<string>("bar"), result[0]);
            Assert.IsType<NullReferenceException>(result[1].Error);
            Assert.Equal(new BindingValue<string>("baz"), result[2]);

            sub.Dispose();

            Assert.Equal(0, data.PropertyChangedSubscriptionCount);
            Assert.Equal(0, data.Next.PropertyChangedSubscriptionCount);
            Assert.Equal(0, breaking.PropertyChangedSubscriptionCount);
            Assert.Equal(0, old.PropertyChangedSubscriptionCount);

            GC.KeepAlive(data);
        }

        [Fact]
        public void Empty_Expression_Should_Track_Root()
        {
            var update = new Subject<Unit>();
            var source = new BehaviorSubject<Class1>(new Class1 { Foo = "foo" });
            var target = TypedBindingExpression.OneWay(source, o => o.Foo);
            var result = new List<string>();

            target.Subscribe(x => result.Add(x.Value));

            source.OnNext(new Class1 { Foo = "bar" });

            Assert.Equal(new[] { "foo", "bar" }, result);
        }

        [Fact]
        public void Should_Track_Property_Value_From_Observable_Root()
        {
            var scheduler = new TestScheduler();
            var source = scheduler.CreateColdObservable(
                OnNext(1, new Class1 { Foo = "foo" }),
                OnNext(2, new Class1 { Foo = "bar" }));
            var target = TypedBindingExpression.OneWay(source, o => o.Foo);
            var result = new List<string>();

            using (target.Subscribe(x => result.Add(x.Value)))
            {
                scheduler.Start();
            }

            Assert.Equal(new[] { "foo", "bar" }, result);
            Assert.All(source.Subscriptions, x => Assert.NotEqual(Subscription.Infinite, x.Unsubscribe));
        }

        [Fact]
        public void Subscribing_Multiple_Times_Should_Return_Values_To_All()
        {
            var data = new Class1 { Foo = "foo" };
            var target = TypedBindingExpression.OneWay(data, o => o.Foo);
            var result1 = new List<string>();
            var result2 = new List<string>();
            var result3 = new List<string>();

            target.Subscribe(x => result1.Add(x.Value));
            target.Subscribe(x => result2.Add(x.Value));

            data.Foo = "bar";

            target.Subscribe(x => result3.Add(x.Value));

            Assert.Equal(new[] { "foo", "bar" }, result1);
            Assert.Equal(new[] { "foo", "bar" }, result2);
            Assert.Equal(new[] { "bar" }, result3);

            GC.KeepAlive(data);
        }

        [Fact]
        public void Subscribing_Multiple_Times_Should_Only_Add_PropertyChanged_Handlers_Once()
        {
            var data = new Class1 { Foo = "foo" };
            var target = TypedBindingExpression.OneWay(data, o => o.Foo);

            var sub1 = target.Subscribe(x => { });
            var sub2 = target.Subscribe(x => { });

            Assert.Equal(1, data.PropertyChangedSubscriptionCount);

            sub1.Dispose();
            sub2.Dispose();

            Assert.Equal(0, data.PropertyChangedSubscriptionCount);

            GC.KeepAlive(data);
        }

        [Fact]
        public void Should_Not_Keep_Source_Alive()
        {
            Func<Tuple<TypedBindingExpression<Class1, string>, WeakReference>> run = () =>
            {
                var source = new Class1 { Foo = "foo" };
                var target = TypedBindingExpression.OneWay(source, o => o.Foo);
                return Tuple.Create(target, new WeakReference(source));
            };

            var result = run();
            result.Item1.Subscribe(x => { });

            // Mono trickery
            GC.Collect(2);
            GC.WaitForPendingFinalizers();
            GC.WaitForPendingFinalizers();
            GC.Collect(2);

            Assert.Null(result.Item2.Target);
        }

        private interface INext
        {
            int PropertyChangedSubscriptionCount { get; }
        }

        private class Class1 : NotifyingBase
        {
            private string _foo;
            private INext _next;

            public string Foo
            {
                get { return _foo; }
                set
                {
                    _foo = value;
                    RaisePropertyChanged(nameof(Foo));
                }
            }

            private string _bar;
            public string Bar
            {
                get { return _bar; }
                set { _bar = value; }
            }

            public INext Next
            {
                get { return _next; }
                set
                {
                    _next = value;
                    RaisePropertyChanged(nameof(Next));
                }
            }
        }

        private class Class2 : NotifyingBase, INext
        {
            private string _bar;
            private INext _next;

            public string Bar
            {
                get { return _bar; }
                set
                {
                    _bar = value;
                    RaisePropertyChanged(nameof(Bar));
                }
            }

            public INext Next
            {
                get { return _next; }
                set
                {
                    _next = value;
                    RaisePropertyChanged(nameof(Next));
                }
            }
        }

        private class Class3 : Class1
        {
        }

        private class WithoutBar : NotifyingBase, INext
        {
        }

        private Recorded<Notification<T>> OnNext<T>(long time, T value)
        {
            return new Recorded<Notification<T>>(time, Notification.CreateOnNext<T>(value));
        }
    }
}
