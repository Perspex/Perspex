using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using Avalonia.Collections;
using Xunit;

namespace Avalonia.Base.UnitTests.Collections
{
    public class AvaloniaListTests
    {
        [Fact]
        public void Items_Passed_To_Constructor_Should_Appear_In_List()
        {
            var items = new[] { 1, 2, 3 };
            var target = new AvaloniaList<int>(items);

            Assert.Equal(items, target);
        }

        [Fact]
        public void AddRange_With_Null_Should_Throw_Exception()
        {
            var target = new AvaloniaList<int>();

            Assert.Throws<ArgumentNullException>(() => target.AddRange(null));
        }

        [Fact]
        public void RemoveAll_With_Null_Should_Throw_Exception()
        {
            var target = new AvaloniaList<int>();

            Assert.Throws<ArgumentNullException>(() => target.RemoveAll(null));
        }

        [Fact]
        public void InsertRange_With_Null_Should_Throw_Exception()
        {
            var target = new AvaloniaList<int>();

            Assert.Throws<ArgumentNullException>(() => target.InsertRange(1, null));
        }

        [Fact]
        public void InsertRange_Past_End_Should_Throw_Exception()
        {
            var target = new AvaloniaList<int>();

            Assert.Throws<ArgumentOutOfRangeException>(() => target.InsertRange(1, new List<int>() { 1 }));
        }

        [Fact]
        public void Move_Should_Update_Collection()
        {
            var target = new AvaloniaList<int>(new[] { 1, 2, 3 });

            target.Move(2, 0);

            Assert.Equal(new[] { 3, 1, 2 }, target);
        }

        [Fact]
        public void MoveRange_Should_Update_Collection()
        {
            var target = new AvaloniaList<int>(new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 });

            target.MoveRange(4, 3, 0);

            Assert.Equal(new[] { 5, 6, 7, 1, 2, 3, 4, 8, 9, 10 }, target);
        }

        [Fact]
        public void MoveRange_Can_Move_To_End()
        {
            var target = new AvaloniaList<int>(new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 });

            target.MoveRange(0, 5, 10);

            Assert.Equal(new[] { 6, 7, 8, 9, 10, 1, 2, 3, 4, 5 }, target);
        }

        [Fact]
        public void MoveRange_Raises_Correct_CollectionChanged_Event()
        {
            var target = new AvaloniaList<int>(new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 });
            var raised = false;

            target.CollectionChanged += (s, e) =>
            {
                Assert.Equal(NotifyCollectionChangedAction.Move, e.Action);
                Assert.Equal(0, e.OldStartingIndex);
                Assert.Equal(10, e.NewStartingIndex);
                Assert.Equal(new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9 }, e.OldItems);
                Assert.Equal(new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9 }, e.NewItems);
                raised = true;
            };

            target.MoveRange(0, 9, 10);

            Assert.True(raised);
            Assert.Equal(new[] { 10, 1, 2, 3, 4, 5, 6, 7, 8, 9 }, target);
        }

        [Fact]
        public void Adding_Item_Should_Raise_CollectionChanged()
        {
            var target = new AvaloniaList<int>(new[] { 1, 2 });
            var raised = false;

            target.CollectionChanged += (s, e) =>
            {
                Assert.Equal(target, s);
                Assert.Equal(NotifyCollectionChangedAction.Add, e.Action);
                Assert.Equal(new[] { 3 }, e.NewItems.Cast<int>());
                Assert.Equal(2, e.NewStartingIndex);

                raised = true;
            };

            target.Add(3);

            Assert.True(raised);
        }

        [Fact]
        public void Adding_Items_Should_Raise_CollectionChanged()
        {
            var target = new AvaloniaList<int>(new[] { 1, 2 });
            var raised = false;

            target.CollectionChanged += (s, e) =>
            {
                Assert.Equal(target, s);
                Assert.Equal(NotifyCollectionChangedAction.Add, e.Action);
                Assert.Equal(new[] { 3, 4 }, e.NewItems.Cast<int>());
                Assert.Equal(2, e.NewStartingIndex);

                raised = true;
            };

            target.AddRange(new[] { 3, 4 });

            Assert.True(raised);
        }

        [Fact]
        public void AddRange_Items_Should_Raise_Correct_CollectionChanged()
        {
            var target = new AvaloniaList<object>();

            var eventItems = new List<object>();

            target.CollectionChanged += (sender, args) =>
            {
                eventItems.AddRange(args.NewItems.Cast<object>());
            };
            
            target.AddRange(Enumerable.Range(0,10).Select(i => new object()));

            Assert.Equal(eventItems, target);
        }

        [Fact]
        public void Replacing_Item_Should_Raise_CollectionChanged()
        {
            var target = new AvaloniaList<int>(new[] { 1, 2 });
            var raised = false;

            target.CollectionChanged += (s, e) =>
            {
                Assert.Equal(target, s);
                Assert.Equal(NotifyCollectionChangedAction.Replace, e.Action);
                Assert.Equal(new[] { 2 }, e.OldItems.Cast<int>());
                Assert.Equal(new[] { 3 }, e.NewItems.Cast<int>());
                Assert.Equal(1, e.OldStartingIndex);
                Assert.Equal(1, e.NewStartingIndex);

                raised = true;
            };

            target[1] = 3;

            Assert.True(raised);
        }

        [Fact]
        public void Inserting_Item_Should_Raise_CollectionChanged()
        {
            var target = new AvaloniaList<int>(new[] { 1, 2 });
            var raised = false;

            target.CollectionChanged += (s, e) =>
            {
                Assert.Equal(target, s);
                Assert.Equal(NotifyCollectionChangedAction.Add, e.Action);
                Assert.Equal(new[] { 3 }, e.NewItems.Cast<int>());
                Assert.Equal(1, e.NewStartingIndex);

                raised = true;
            };

            target.Insert(1, 3);

            Assert.True(raised);
        }

        [Fact]
        public void Inserting_Items_Should_Raise_CollectionChanged()
        {
            var target = new AvaloniaList<int>(new[] { 1, 2 });
            var raised = false;

            target.CollectionChanged += (s, e) =>
            {
                Assert.Equal(target, s);
                Assert.Equal(NotifyCollectionChangedAction.Add, e.Action);
                Assert.Equal(new[] { 3, 4 }, e.NewItems.Cast<int>());
                Assert.Equal(1, e.NewStartingIndex);

                raised = true;
            };

            target.InsertRange(1, new[] { 3, 4 });

            Assert.True(raised);
        }

        [Fact]
        public void Removing_Item_Should_Raise_CollectionChanged()
        {
            var target = new AvaloniaList<int>(new[] { 1, 2, 3 });
            var raised = false;

            target.CollectionChanged += (s, e) =>
            {
                Assert.Equal(target, s);
                Assert.Equal(NotifyCollectionChangedAction.Remove, e.Action);
                Assert.Equal(new[] { 3 }, e.OldItems.Cast<int>());
                Assert.Equal(2, e.OldStartingIndex);

                raised = true;
            };

            target.Remove(3);

            Assert.True(raised);
        }

        [Fact]
        public void Moving_Item_Should_Raise_CollectionChanged()
        {
            var target = new AvaloniaList<int>(new[] { 1, 2, 3 });
            var raised = false;

            target.CollectionChanged += (s, e) =>
            {
                Assert.Equal(target, s);
                Assert.Equal(NotifyCollectionChangedAction.Move, e.Action);
                Assert.Equal(new[] { 3 }, e.OldItems.Cast<int>());
                Assert.Equal(new[] { 3 }, e.NewItems.Cast<int>());
                Assert.Equal(2, e.OldStartingIndex);
                Assert.Equal(0, e.NewStartingIndex);

                raised = true;
            };

            target.Move(2, 0);

            Assert.True(raised);
        }

        [Fact]
        public void Moving_Items_Should_Raise_CollectionChanged()
        {
            var target = new AvaloniaList<int>(new[] { 1, 2, 3 });
            var raised = false;

            target.CollectionChanged += (s, e) =>
            {
                Assert.Equal(target, s);
                Assert.Equal(NotifyCollectionChangedAction.Move, e.Action);
                Assert.Equal(new[] { 2, 3 }, e.OldItems.Cast<int>());
                Assert.Equal(new[] { 2, 3 }, e.NewItems.Cast<int>());
                Assert.Equal(1, e.OldStartingIndex);
                Assert.Equal(0, e.NewStartingIndex);

                raised = true;
            };

            target.MoveRange(1, 2, 0);

            Assert.True(raised);
        }

        [Fact]
        public void Clearing_Items_Should_Raise_CollectionChanged_Reset()
        {
            var target = new AvaloniaList<int>(new[] { 1, 2, 3 });
            var raised = false;

            target.CollectionChanged += (s, e) =>
            {
                Assert.Equal(target, s);
                Assert.Equal(NotifyCollectionChangedAction.Reset, e.Action);

                raised = true;
            };

            target.Clear();

            Assert.True(raised);
        }

        [Fact]
        public void Clearing_Items_Should_Raise_CollectionChanged_Remove()
        {
            var target = new AvaloniaList<int>(new[] { 1, 2, 3 });
            var raised = false;

            target.ResetBehavior = ResetBehavior.Remove;
            target.CollectionChanged += (s, e) =>
            {
                Assert.Equal(target, s);
                Assert.Equal(NotifyCollectionChangedAction.Remove, e.Action);
                Assert.Equal(new[] { 1, 2, 3 }, e.OldItems.Cast<int>());
                Assert.Equal(0, e.OldStartingIndex);

                raised = true;
            };

            target.Clear();

            Assert.True(raised);
        }

        [Fact]
        public void Can_CopyTo_Array_Of_Same_Type()
        {
            var target = new AvaloniaList<string> { "foo", "bar", "baz" };
            var result = new string[3];

            target.CopyTo(result, 0);

            Assert.Equal(target, result);
        }

        [Fact]
        public void Can_CopyTo_Array_Of_Base_Type()
        {
            var target = new AvaloniaList<string> { "foo", "bar", "baz" };
            var result = new object[3];

            ((IList)target).CopyTo(result, 0);

            Assert.Equal(target, result);
        }

        [Fact]
        public void RemoveAll_Should_Remove_Items()
        {
            int[] bar = Enumerable.Range(-7, 15).ToArray();
            var itemsLst = new List<int[]>()
            {
                Array.Empty<int>(),
                bar,
                bar.Concat(bar).ToArray(),
                bar.Concat(bar).Concat(bar).ToArray(),
                bar.Concat(bar).OrderBy(x => x).ToArray(),
                bar.Concat(bar).Concat(bar).OrderBy(x => x).ToArray()
            };

            itemsLst.AddRange(itemsLst.Select(x => x.Reverse().ToArray()).ToArray());

            var testCases = new List<(int[] src, int[] excludes)>(2048);
            foreach (int[] items in itemsLst)
            {
                var missed_seq = new[] { -100, 100 };
                var to_remove_lst = new List<int[]>()
                {
                    items,
                    missed_seq,
                    items.Concat(missed_seq).ToArray(),
                    items.Concat(items).ToArray(),
                    missed_seq.Concat(missed_seq).ToArray(),
                    items.Concat(items).Concat(Enumerable.Repeat(512, 11)).ToArray(),
                    missed_seq.Concat(items).Concat(missed_seq).ToArray()
                };

                // extra remove test
                to_remove_lst.InsertRange(0, new[] { items.Take(2).ToArray() });
                to_remove_lst.InsertRange(0, new[] { items.Take(7).ToArray() });
                to_remove_lst.InsertRange(0, new[] { items.Take(3).Reverse().ToArray() });
                to_remove_lst.InsertRange(0, items.Select(x => new int[] { x, x }));
                to_remove_lst.InsertRange(0, items.Select(x => new int[] { x }));

                to_remove_lst.ForEach(rl => testCases.Add((items, rl)));
            }

            foreach (var tCase in testCases)
            {
                var target = new AvaloniaList<int>(tCase.src);

                bool raised = false;
                var innerItems = tCase.src.ToList();

                target.CollectionChanged += (s, e) =>
                {
                    Assert.Equal(target, s);
                    Assert.Equal(NotifyCollectionChangedAction.Remove, e.Action);

                    // simulate next remove notification
                    int expected_end = innerItems.FindLastIndex(x => tCase.excludes.Contains(x)) + 1;
                    int[] expected_items = innerItems.Take(expected_end).Reverse().TakeWhile(x => tCase.excludes.Contains(x)).Reverse().ToArray();
                    int expected_start = expected_end - expected_items.Length;
                    // handle simulated notification
                    innerItems.RemoveRange(expected_start, expected_items.Length);

                    Assert.Equal(expected_items, e.OldItems.Cast<int>());
                    Assert.Equal(expected_start, e.OldStartingIndex);
                    Assert.Equal(target.ToArray(), innerItems);

                    raised = true;
                };

                target.RemoveAll(tCase.excludes);

                if (tCase.src.Intersect(tCase.excludes).Count() != 0)
                {
                    Assert.True(raised);
                    Assert.Equal(tCase.src.Where(x => !tCase.excludes.Contains(x)), innerItems);
                }
                else
                {
                    Assert.False(raised);
                    Assert.Equal(target.Intersect(tCase.excludes).Count(), 0);
                }
            }
        }
    }
}
