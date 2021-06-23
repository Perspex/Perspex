using System.Collections.Generic;
using System.Linq;
using Avalonia.Layout;
using Xunit;

namespace Avalonia.Base.UnitTests.Layout
{
    public class LayoutQueueTests
    {
        [Fact]
        public void Should_Enqueue()
        {
            var target = new LayoutQueue<string>(_ => true);
            var refQueue = new Queue<string>();
            var items = new[] { "1", "2", "3" };

            foreach (var item in items)
            {
                target.Enqueue(item);
                refQueue.Enqueue(item);
            }

            Assert.Equal(refQueue, target);
        }

        [Fact]
        public void Should_Dequeue()
        {
            var target = new LayoutQueue<string>(_ => true);
            var refQueue = new Queue<string>();
            var items = new[] { "1", "2", "3" };

            foreach (var item in items)
            {
                target.Enqueue(item);
                refQueue.Enqueue(item);
            }

            while (refQueue.Count > 0)
            {
                Assert.Equal(refQueue.Dequeue(), target.Dequeue());
            }
        }

        [Fact]
        public void Should_Enqueue_UniqueElements()
        {
            var target = new LayoutQueue<string>(_ => true);

            var items = new[] { "1", "2", "3", "1" };

            foreach (var item in items)
            {
                target.Enqueue(item);
            }

            Assert.Equal(3, target.Count);
            Assert.Equal(items.Take(3), target);
        }

        [Fact]
        public void Shouldnt_Enqueue_More_Than_Limit_In_Loop()
        {
            var target = new LayoutQueue<string>(_ => true);

            //1
            target.Enqueue("Foo");

            Assert.Equal(1, target.Count);

            target.BeginLoop(3);

            target.Dequeue();

            //2
            target.Enqueue("Foo");
            target.Dequeue();

            //3
            target.Enqueue("Foo");

            Assert.Equal(1, target.Count);

            target.Dequeue();

            //4 more than limit shouldn't be added
            target.Enqueue("Foo");

            Assert.Equal(0, target.Count);
        }

        [Fact]
        public void Shouldnt_Count_Unique_Enqueue_For_Limit_In_Loop()
        {
            var target = new LayoutQueue<string>(_ => true);

            //1
            target.Enqueue("Foo");

            Assert.Equal(1, target.Count);

            target.BeginLoop(3);

            target.Dequeue();

            //2
            target.Enqueue("Foo");
            target.Enqueue("Foo");
            target.Dequeue();

            //3
            target.Enqueue("Foo");
            target.Enqueue("Foo");

            Assert.Equal(1, target.Count);

            target.Dequeue();

            //4 more than limit shouldn't be added
            target.Enqueue("Foo");

            Assert.Equal(0, target.Count);
        }

        [Fact]
        public void Should_Enqueue_When_Condition_True_After_Loop_When_Limit_Met()
        {
            var target = new LayoutQueue<string>(_ => true);

            //1
            target.Enqueue("Foo");

            Assert.Equal(1, target.Count);

            target.BeginLoop(3);

            target.Dequeue();

            //2
            target.Enqueue("Foo");
            target.Dequeue();

            //3
            target.Enqueue("Foo");

            Assert.Equal(1, target.Count);

            target.Dequeue();

            //4 more than limit shouldn't be added to queue
            target.Enqueue("Foo");

            Assert.Equal(0, target.Count);

            target.EndLoop();

            //after loop should be added once
            Assert.Equal(1, target.Count);
            Assert.Equal("Foo", target.First());
        }

        [Fact]
        public void Shouldnt_Enqueue_When_Condition_False_After_Loop_When_Limit_Met()
        {
            var target = new LayoutQueue<string>(_ => false);

            //1
            target.Enqueue("Foo");

            Assert.Equal(1, target.Count);

            target.BeginLoop(3);

            target.Dequeue();

            //2
            target.Enqueue("Foo");
            target.Dequeue();

            //3
            target.Enqueue("Foo");

            Assert.Equal(1, target.Count);

            target.Dequeue();

            //4 more than limit shouldn't be added
            target.Enqueue("Foo");

            Assert.Equal(0, target.Count);

            target.EndLoop();

            Assert.Equal(0, target.Count);
        }
    }
}
