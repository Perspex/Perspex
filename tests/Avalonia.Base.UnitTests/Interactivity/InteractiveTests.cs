using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using Xunit;

namespace Avalonia.Base.UnitTests.Interactivity
{
    public class InteractiveTests
    {
        [Fact]
        public void Direct_Event_Should_Go_Straight_To_Source()
        {
            var ev = new RoutedEvent("test", RoutingStrategies.Direct, typeof(RoutedEventArgs), typeof(TestInteractive));
            var invoked = new List<string>();
            EventHandler<RoutedEventArgs> handler = (s, e) => invoked.Add(((TestInteractive)s).Name);
            var target = CreateTree(ev, handler, RoutingStrategies.Direct);

            var args = new RoutedEventArgs(ev, target);
            target.RaiseEvent(args);

            Assert.Equal(new[] { "2b" }, invoked);
        }

        [Fact]
        public void Direct_Event_Should_Have_Route_Set_To_Direct()
        {
            var ev = new RoutedEvent("test", RoutingStrategies.Direct, typeof(RoutedEventArgs), typeof(TestInteractive));
            bool called = false;

            EventHandler<RoutedEventArgs> handler = (s, e) =>
            {
                Assert.Equal(RoutingStrategies.Direct, e.Route);
                called = true;
            };

            var target = CreateTree(ev, handler, RoutingStrategies.Direct);

            var args = new RoutedEventArgs(ev, target);
            target.RaiseEvent(args);

            Assert.True(called);
        }

        [Fact]
        public void Bubbling_Event_Should_Bubble_Up()
        {
            var ev = new RoutedEvent("test", RoutingStrategies.Bubble, typeof(RoutedEventArgs), typeof(TestInteractive));
            var invoked = new List<string>();
            EventHandler<RoutedEventArgs> handler = (s, e) => invoked.Add(((TestInteractive)s).Name);
            var target = CreateTree(ev, handler, RoutingStrategies.Bubble | RoutingStrategies.Tunnel);

            var args = new RoutedEventArgs(ev, target);
            target.RaiseEvent(args);

            Assert.Equal(new[] { "2b", "1" }, invoked);
        }

        [Fact]
        public void Tunneling_Event_Should_Tunnel()
        {
            var ev = new RoutedEvent("test", RoutingStrategies.Tunnel, typeof(RoutedEventArgs), typeof(TestInteractive));
            var invoked = new List<string>();
            EventHandler<RoutedEventArgs> handler = (s, e) => invoked.Add(((TestInteractive)s).Name);
            var target = CreateTree(ev, handler, RoutingStrategies.Bubble | RoutingStrategies.Tunnel);

            var args = new RoutedEventArgs(ev, target);
            target.RaiseEvent(args);

            Assert.Equal(new[] { "1", "2b" }, invoked);
        }

        [Fact]
        public void Tunneling_Bubbling_Event_Should_Tunnel_Then_Bubble_Up()
        {
            var ev = new RoutedEvent(
                "test",
                RoutingStrategies.Bubble | RoutingStrategies.Tunnel,
                typeof(RoutedEventArgs),
                typeof(TestInteractive));
            var invoked = new List<string>();
            EventHandler<RoutedEventArgs> handler = (s, e) => invoked.Add(((TestInteractive)s).Name);
            var target = CreateTree(ev, handler, RoutingStrategies.Bubble | RoutingStrategies.Tunnel);

            var args = new RoutedEventArgs(ev, target);
            target.RaiseEvent(args);

            Assert.Equal(new[] { "1", "2b", "2b", "1" }, invoked);
        }

        [Fact]
        public void Events_Should_Have_Route_Set()
        {
            var ev = new RoutedEvent(
                "test",
                RoutingStrategies.Bubble | RoutingStrategies.Tunnel,
                typeof(RoutedEventArgs),
                typeof(TestInteractive));
            var invoked = new List<RoutingStrategies>();
            EventHandler<RoutedEventArgs> handler = (s, e) => invoked.Add(e.Route);
            var target = CreateTree(ev, handler, RoutingStrategies.Bubble | RoutingStrategies.Tunnel);

            var args = new RoutedEventArgs(ev, target);
            target.RaiseEvent(args);

            Assert.Equal(new[]
            {
                RoutingStrategies.Tunnel,
                RoutingStrategies.Tunnel,
                RoutingStrategies.Bubble,
                RoutingStrategies.Bubble,
            },
            invoked);
        }

        [Fact]
        public void Handled_Bubbled_Event_Should_Not_Propogate_Further()
        {
            var ev = new RoutedEvent("test", RoutingStrategies.Bubble, typeof(RoutedEventArgs), typeof(TestInteractive));
            var invoked = new List<string>();

            EventHandler<RoutedEventArgs> handler = (s, e) =>
            {
                var t = (TestInteractive)s;
                invoked.Add(t.Name);
                e.Handled = t.Name == "2b";
            };

            var target = CreateTree(ev, handler, RoutingStrategies.Bubble);

            var args = new RoutedEventArgs(ev, target);
            target.RaiseEvent(args);

            Assert.Equal(new[] { "2b" }, invoked);
        }

        [Fact]
        public void Handled_Tunnelled_Event_Should_Not_Propogate_Further()
        {
            var ev = new RoutedEvent(
                "test",
                RoutingStrategies.Bubble | RoutingStrategies.Tunnel, 
                typeof(RoutedEventArgs), 
                typeof(TestInteractive));
            var invoked = new List<string>();

            EventHandler<RoutedEventArgs> handler = (s, e) =>
            {
                var t = (TestInteractive)s;
                invoked.Add(t.Name);
                e.Handled = t.Name == "2b";
            };

            var target = CreateTree(ev, handler, RoutingStrategies.Bubble | RoutingStrategies.Tunnel);

            var args = new RoutedEventArgs(ev, target);
            target.RaiseEvent(args);

            Assert.Equal(new[] { "1", "2b" }, invoked);
        }

        [Fact]
        public void Direct_Subscription_Should_Not_Catch_Tunneling_Or_Bubbling()
        {
            var ev = new RoutedEvent(
                "test",
                RoutingStrategies.Bubble | RoutingStrategies.Tunnel,
                typeof(RoutedEventArgs),
                typeof(TestInteractive));
            var count = 0;

            EventHandler<RoutedEventArgs> handler = (s, e) =>
            {
                ++count;
            };

            var target = CreateTree(ev, handler, RoutingStrategies.Direct);

            var args = new RoutedEventArgs(ev, target);
            target.RaiseEvent(args);

            Assert.Equal(0, count);
        }

        [Fact]
        public void Bubbling_Subscription_Should_Not_Catch_Tunneling()
        {
            var ev = new RoutedEvent(
                "test",
                RoutingStrategies.Bubble | RoutingStrategies.Tunnel,
                typeof(RoutedEventArgs),
                typeof(TestInteractive));
            var count = 0;

            EventHandler<RoutedEventArgs> handler = (s, e) =>
            {
                Assert.Equal(RoutingStrategies.Bubble, e.Route);
                ++count;
            };

            var target = CreateTree(ev, handler, RoutingStrategies.Bubble);

            var args = new RoutedEventArgs(ev, target);
            target.RaiseEvent(args);

            Assert.Equal(2, count);
        }

        [Fact]
        public void Tunneling_Subscription_Should_Not_Catch_Bubbling()
        {
            var ev = new RoutedEvent(
                "test",
                RoutingStrategies.Bubble | RoutingStrategies.Tunnel,
                typeof(RoutedEventArgs),
                typeof(TestInteractive));
            var count = 0;

            EventHandler<RoutedEventArgs> handler = (s, e) =>
            {
                Assert.Equal(RoutingStrategies.Tunnel, e.Route);
                ++count;
            };

            var target = CreateTree(ev, handler, RoutingStrategies.Tunnel);

            var args = new RoutedEventArgs(ev, target);
            target.RaiseEvent(args);

            Assert.Equal(2, count);
        }

        [Fact]
        public void Event_Should_Should_Keep_Propogating_To_HandedEventsToo_Handlers()
        {
            var ev = new RoutedEvent(
                "test",
                RoutingStrategies.Bubble | RoutingStrategies.Tunnel,
                typeof(RoutedEventArgs),
                typeof(TestInteractive));
            var invoked = new List<string>();

            EventHandler<RoutedEventArgs> handler = (s, e) =>
            {
                invoked.Add(((TestInteractive)s).Name);
                e.Handled = true;
            };

            var target = CreateTree(ev, handler, RoutingStrategies.Bubble | RoutingStrategies.Tunnel, true);

            var args = new RoutedEventArgs(ev, target);
            target.RaiseEvent(args);

            Assert.Equal(new[] { "1", "2b", "2b", "1" }, invoked);
        }

        [Fact]
        public void Direct_Class_Handlers_Should_Be_Called()
        {
            var ev = new RoutedEvent(
                "test",
                RoutingStrategies.Direct,
                typeof(RoutedEventArgs),
                typeof(TestInteractive));
            var invoked = new List<string>();
            EventHandler<RoutedEventArgs> handler = (s, e) => invoked.Add(((TestInteractive)s).Name);

            var target = CreateTree(ev, null, 0);

            ev.AddClassHandler(typeof(TestInteractive), handler, RoutingStrategies.Direct);

            var args = new RoutedEventArgs(ev, target);
            target.RaiseEvent(args);

            Assert.Equal(new[] { "2b" }, invoked);
        }

        [Fact]
        public void Tunneling_Class_Handlers_Should_Be_Called()
        {
            var ev = new RoutedEvent(
                "test",
                RoutingStrategies.Bubble | RoutingStrategies.Tunnel,
                typeof(RoutedEventArgs),
                typeof(TestInteractive));
            var invoked = new List<string>();
            EventHandler<RoutedEventArgs> handler = (s, e) => invoked.Add(((TestInteractive)s).Name);

            var target = CreateTree(ev, null, 0);

            ev.AddClassHandler(typeof(TestInteractive), handler, RoutingStrategies.Tunnel);

            var args = new RoutedEventArgs(ev, target);
            target.RaiseEvent(args);

            Assert.Equal(new[] { "1", "2b" }, invoked);
        }

        [Fact]
        public void Bubbling_Class_Handlers_Should_Be_Called()
        {
            var ev = new RoutedEvent(
                "test",
                RoutingStrategies.Bubble | RoutingStrategies.Tunnel,
                typeof(RoutedEventArgs),
                typeof(TestInteractive));
            var invoked = new List<string>();
            EventHandler<RoutedEventArgs> handler = (s, e) => invoked.Add(((TestInteractive)s).Name);

            var target = CreateTree(ev, null, 0);

            ev.AddClassHandler(typeof(TestInteractive), handler, RoutingStrategies.Bubble);

            var args = new RoutedEventArgs(ev, target);
            target.RaiseEvent(args);

            Assert.Equal(new[] { "2b", "1" }, invoked);
        }

        [Fact]
        public void Typed_Class_Handlers_Should_Be_Called()
        {
            var ev = new RoutedEvent<RoutedEventArgs>(
                "test",
                RoutingStrategies.Bubble | RoutingStrategies.Tunnel,
                typeof(TestInteractive));

            var target = CreateTree(ev, null, 0);

            ev.AddClassHandler<TestInteractive>((x, e) => x.ClassHandler(e), RoutingStrategies.Bubble);

            var args = new RoutedEventArgs(ev, target);
            target.RaiseEvent(args);

            Assert.True(target.ClassHandlerInvoked);
            Assert.True(target.GetVisualParent<TestInteractive>().ClassHandlerInvoked);
        }

        [Fact]
        public void GetObservable_Should_Listen_To_Event()
        {
            var ev = new RoutedEvent<RoutedEventArgs>("test", RoutingStrategies.Direct, typeof(TestInteractive));
            var target = new TestInteractive();
            var called = 0;
            var subscription = target.GetObservable(ev).Subscribe(_ => ++called);

            var args = new RoutedEventArgs(ev, target);
            target.RaiseEvent(args);

            subscription.Dispose();

            target.RaiseEvent(args);

            Assert.Equal(1, called);
        }

        [Fact]
        public void Removing_Control_In_Handler_Should_Not_Stop_Event()
        {
            // Issue #3176
            var ev = new RoutedEvent("test", RoutingStrategies.Bubble, typeof(RoutedEventArgs), typeof(TestInteractive));
            var invoked = new List<string>();
            EventHandler<RoutedEventArgs> handler = (s, e) => invoked.Add(((TestInteractive)s).Name);
            var parent = CreateTree(ev, handler, RoutingStrategies.Bubble | RoutingStrategies.Tunnel);
            var target = (IInteractive)parent.GetVisualChildren().Single();

            EventHandler<RoutedEventArgs> removeHandler = (s, e) =>
            {
                parent.Children = Array.Empty<IVisual>();
            };
            
            target.AddHandler(ev, removeHandler);

            var args = new RoutedEventArgs(ev, target);
            target.RaiseEvent(args);

            Assert.Equal(new[] { "3", "2b", "1" }, invoked);
        }

        private TestInteractive CreateTree(
            RoutedEvent ev,
            EventHandler<RoutedEventArgs> handler,
            RoutingStrategies handlerRoutes,
            bool handledEventsToo = false)
        {
            TestInteractive target;

            var tree = new TestInteractive
            {
                Name = "1",
                Children = new[]
                {
                    new TestInteractive
                    {
                        Name = "2a",
                    },
                    (target = new TestInteractive
                    {
                        Name = "2b",
                        Children = new[]
                        {
                            new TestInteractive
                            {
                                Name = "3",
                            },
                        },
                    }),
                }
            };

            if (handler != null)
            {
                foreach (var i in tree.GetSelfAndVisualDescendants().Cast<Interactive>())
                {
                    i.AddHandler(ev, handler, handlerRoutes, handledEventsToo);
                }
            }

            return target;
        }

        private class TestInteractive : Interactive
        {
            public bool ClassHandlerInvoked { get; private set; }
            public new string Name { get; set; }

            public IEnumerable<IVisual> Children
            {
                get
                {
                    return ((IVisual)this).VisualChildren.AsEnumerable();
                }

                set
                {
                    VisualChildren.Clear();
                    VisualChildren.AddRange(value.Cast<Visual>());
                }
            }

            public void ClassHandler(RoutedEventArgs e)
            {
                ClassHandlerInvoked = true;
            }
        }
    }
}
