// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

// These classes implement a frugal storage model for lists of type <T>.
// Performance measurements show that Avalon has many lists that contain
// a limited number of entries, and frequently zero or a single entry.
// Therefore these classes are structured to prefer a storage model that
// starts at zero, and employs a conservative growth strategy to minimize
// the steady state memory footprint. Also note that the list uses one
// fewer objects than ArrayList and List<T> and does no allocations at all
// until an item is inserted into the list.
//
// The code is also structured to perform well from a CPU standpoint. Perf
// analysis shows that the reduced number of processor cache misses makes
// FrugalList faster than ArrayList or List<T>, especially for lists of 6
// or fewer items. Timing differ with the size of <T>.
//
// FrugalList is appropriate for small lists or lists that grow slowly.
// Due to the slow growth, if you use it for a list with more than 6 initial
// entries is best to set the capacity of the list at construction time or
// soon after. If you must grow the list by a large amount, set the capacity
// or use Insert() method to force list growth to the final size. Choose
// your collections wisely and pay particular attention to the growth
// patterns and search methods.

// FrugalList has all of the methods of the IList interface, but implements
// them as virtual methods of the class and not as Interface methods. This
// is to avoid the virtual stub dispatch CPU costs associated with Interfaces.

namespace Avalonia.Utilities
{
    // These classes implement a frugal storage model for lists of <T>.
    // Performance measurements show that many lists contain a single item.
    // Therefore this list is structured to prefer a list that contains a single
    // item, and does conservative growth to minimize the memory footprint.

    // This enum controls the growth to successively more complex storage models
    internal enum FrugalListStoreState
    {
        Success,
        SingleItemList,
        ThreeItemList,
        SixItemList,
        Array
    }

    internal abstract class FrugalListBase<T>
    {
        /// <summary>
        /// Number of entries in this store
        /// </summary>
        // Number of entries in this store
        public int Count
        {
            get
            {
                return _count;
            }
        }

        // for use only by trusted callers - e.g. FrugalObjectList.Compacter
        internal void TrustedSetCount(int newCount)
        {
            _count = newCount;
        }

        /// <summary>
        /// Capacity of this store
        /// </summary>
        public abstract int Capacity
        {
            get;
        }

        // Increase size if needed, insert item into the store
        public abstract FrugalListStoreState Add(T value);

        /// <summary>
        /// Removes all values from the store
        /// </summary>
        public abstract void Clear();

        /// <summary>
        /// Returns true if the store contains the entry.
        /// </summary>
        public abstract bool Contains(T value);

        /// <summary>
        /// Returns the index into the store that contains the item.
        /// -1 is returned if the item is not in the store.
        /// </summary>
        public abstract int IndexOf(T value);

        /// <summary>
        /// Insert item into the store at index, grows if needed
        /// </summary>
        public abstract void Insert(int index, T value);

        // Place item into the store at index
        public abstract void SetAt(int index, T value);

        /// <summary>
        /// Removes the item from the store. If the item was not
        /// in the store false is returned.
        /// </summary>
        public abstract bool Remove(T value);

        /// <summary>
        /// Removes the item from the store
        /// </summary>
        public abstract void RemoveAt(int index);

        /// <summary>
        /// Return the item at index in the store
        /// </summary>
        public abstract T EntryAt(int index);

        /// <summary>
        /// Promotes the values in the current store to the next larger
        /// and more complex storage model.
        /// </summary>
        public abstract void Promote(FrugalListBase<T> newList);

        /// <summary>
        /// Returns the entries as an array
        /// </summary>
        public abstract T[] ToArray();

        /// <summary>
        /// Copies the entries to the given array starting at the
        /// specified index
        /// </summary>
        public abstract void CopyTo(T[] array, int index);

        /// <summary>
        /// Creates a shallow copy of the  list
        /// </summary>
        public abstract object Clone();

        // The number of items in the list.
        protected int _count;

        public virtual Compacter NewCompacter(int newCount)
        {
            return new Compacter(this, newCount);
        }

        // basic implementation - compacts in-place
        internal class Compacter
        {
            protected readonly FrugalListBase<T> _store;
            private readonly int _newCount;

            protected int _validItemCount;
            protected int _previousEnd;

            public Compacter(FrugalListBase<T> store, int newCount)
            {
                _store = store;
                _newCount = newCount;
            }

            public void Include(int start, int end)
            {
                Debug.Assert(start >= _previousEnd, "Arguments out of order during Compact");
                Debug.Assert(_validItemCount + end - start <= _newCount, "Too many items copied during Compact");

                IncludeOverride(start, end);

                _previousEnd = end;
            }

            protected virtual void IncludeOverride(int start, int end)
            {
                // item-by-item move
                for (var i = start; i < end; ++i)
                {
                    _store.SetAt(_validItemCount++, _store.EntryAt(i));
                }
            }

            public virtual FrugalListBase<T> Finish()
            {
                // clear out vacated entries
                var filler = default(T);

                for (int i = _validItemCount, n = _store._count; i < n; ++i)
                {
                    _store.SetAt(i, filler);
                }

                _store._count = _validItemCount;

                return _store;
            }
        }
    }

    /// <summary>
    /// A simple class to handle a single item
    /// </summary>
    internal sealed class SingleItemList<T> : FrugalListBase<T>
    {
        private const int SIZE = 1;

        private T _loneEntry;

        // Capacity of this store
        public override int Capacity
        {
            get
            {
                return SIZE;
            }
        }

        public override FrugalListStoreState Add(T value)
        {
            // If we don't have any entries or the existing entry is being overwritten,
            // then we can use this store. Otherwise we have to promote.
            if (0 == _count)
            {
                _loneEntry = value;
                ++_count;
                return FrugalListStoreState.Success;
            }
            else
            {
                // Entry already used, move to an ThreeItemList
                return FrugalListStoreState.ThreeItemList;
            }
        }

        public override void Clear()
        {
            // Wipe out the info
            _loneEntry = default;
            _count = 0;
        }

        public override bool Contains(T value)
        {
            return _loneEntry.Equals(value);
        }

        public override int IndexOf(T value)
        {
            if (_loneEntry.Equals(value))
            {
                return 0;
            }
            return -1;
        }

        public override void Insert(int index, T value)
        {
            // Should only get here if count and index are 0
            if (_count < SIZE && index < SIZE)
            {
                _loneEntry = value;
                ++_count;
                return;
            }
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        public override void SetAt(int index, T value)
        {
            // Overwrite item at index
            _loneEntry = value;
        }

        public override bool Remove(T value)
        {
            // Wipe out the info in the only entry if it matches the item.
            if (_loneEntry.Equals(value))
            {
                _loneEntry = default;
                --_count;
                return true;
            }

            return false;
        }

        public override void RemoveAt(int index)
        {
            // Wipe out the info at index
            if (0 == index)
            {
                _loneEntry = default;
                --_count;
            }
            else
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }
        }

        public override T EntryAt(int index)
        {
            return _loneEntry;
        }

        public override void Promote(FrugalListBase<T> oldList)
        {
            if (SIZE == oldList.Count)
            {
                SetCount(SIZE);
                SetAt(0, oldList.EntryAt(0));
            }
            else
            {
                // this list is smaller than oldList
                throw new ArgumentException($"Cannot promote from '{oldList}' to '{ToString()}' because the target map is too small.", nameof(oldList));
            }
        }

        // Class specific implementation to avoid virtual method calls and additional logic
        public void Promote(SingleItemList<T> oldList)
        {
            SetCount(oldList.Count);
            SetAt(0, oldList.EntryAt(0));
        }

        public override T[] ToArray()
        {
            var array = new T[1];
            array[0] = _loneEntry;
            return array;
        }

        public override void CopyTo(T[] array, int index)
        {
            array[index] = _loneEntry;
        }

        public override object Clone()
        {
            var newList = new SingleItemList<T>();
            newList.Promote(this);
            return newList;
        }

        private void SetCount(int value)
        {
            if (value >= 0 && value <= SIZE)
            {
                _count = value;
            }
            else
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }
        }
    }


    /// <summary>
    /// A simple class to handle a list with 3 items.  Perf analysis showed
    /// that this yielded better memory locality and perf than an object and an array.
    /// </summary>
    internal sealed class ThreeItemList<T> : FrugalListBase<T>
    {
        private const int SIZE = 3;

        private T _entry0;
        private T _entry1;
        private T _entry2;

        // Capacity of this store
        public override int Capacity
        {
            get
            {
                return SIZE;
            }
        }

        public override FrugalListStoreState Add(T value)
        {
            switch (_count)
            {
                case 0:
                    _entry0 = value;
                    break;

                case 1:
                    _entry1 = value;
                    break;

                case 2:
                    _entry2 = value;
                    break;

                default:
                    // We have to promote
                    return FrugalListStoreState.SixItemList;
            }
            ++_count;
            return FrugalListStoreState.Success;
        }

        public override void Clear()
        {
            // Wipe out the info.
            _entry0 = default;
            _entry1 = default;
            _entry2 = default;
            _count = 0;
        }

        public override bool Contains(T value)
        {
            return -1 != IndexOf(value);
        }

        public override int IndexOf(T value)
        {
            if (_entry0.Equals(value))
            {
                return 0;
            }

            if (_count > 1)
            {
                if (_entry1.Equals(value))
                {
                    return 1;
                }
                if (3 == _count && _entry2.Equals(value))
                {
                    return 2;
                }
            }

            return -1;
        }

        public override void Insert(int index, T value)
        {
            // Should only get here if count < SIZE
            if (_count < SIZE)
            {
                switch (index)
                {
                    case 0:
                        _entry2 = _entry1;
                        _entry1 = _entry0;
                        _entry0 = value;
                        break;

                    case 1:
                        _entry2 = _entry1;
                        _entry1 = value;
                        break;

                    case 2:
                        _entry2 = value;
                        break;

                    default:
                        throw new ArgumentOutOfRangeException(nameof(index));
                }
                ++_count;
                return;
            }

            throw new ArgumentOutOfRangeException(nameof(index));
        }

        public override void SetAt(int index, T value)
        {
            // Overwrite item at index
            switch (index)
            {
                case 0:
                    _entry0 = value;
                    break;

                case 1:
                    _entry1 = value;
                    break;

                case 2:
                    _entry2 = value;
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(index));
            }
        }

        public override bool Remove(T value)
        {
            // If the item matches an existing entry, wipe out the last
            // entry and move all the other entries up.  Because we only
            // have three entries we can just unravel all the cases.
            if (_entry0.Equals(value))
            {
                RemoveAt(0);
                return true;
            }
            else if (_count > 1)
            {
                if (_entry1.Equals(value))
                {
                    RemoveAt(1);
                    return true;
                }
                else if (3 == _count && _entry2.Equals(value))
                {
                    RemoveAt(2);
                    return true;
                }
            }
            return false;
        }

        public override void RemoveAt(int index)
        {
            // Remove entry at index, wipe out the last entry and move
            // all the other entries up.  Because we only have three
            // entries we can just unravel all the cases.
            switch (index)
            {
                case 0:
                    _entry0 = _entry1;
                    _entry1 = _entry2;
                    break;

                case 1:
                    _entry1 = _entry2;
                    break;

                case 2:
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(index));
            }

            _entry2 = default;

            --_count;
        }

        public override T EntryAt(int index)
        {
            switch (index)
            {
                case 0:
                    return _entry0;

                case 1:
                    return _entry1;

                case 2:
                    return _entry2;

                default:
                    throw new ArgumentOutOfRangeException(nameof(index));
            }
        }

        public override void Promote(FrugalListBase<T> oldList)
        {
            var oldCount = oldList.Count;

            if (SIZE >= oldCount)
            {
                SetCount(oldList.Count);

                switch (oldCount)
                {
                    case 3:
                        SetAt(0, oldList.EntryAt(0));
                        SetAt(1, oldList.EntryAt(1));
                        SetAt(2, oldList.EntryAt(2));
                        break;

                    case 2:
                        SetAt(0, oldList.EntryAt(0));
                        SetAt(1, oldList.EntryAt(1));
                        break;

                    case 1:
                        SetAt(0, oldList.EntryAt(0));
                        break;

                    case 0:
                        break;

                    default:
                        throw new ArgumentOutOfRangeException(nameof(oldList));
                }
            }
            else
            {
                // this list is smaller than oldList
                throw new ArgumentException($"Cannot promote from '{oldList}' to '{ToString()}' because the target map is too small.", nameof(oldList));
            }
        }

        // Class specific implementation to avoid virtual method calls and additional logic
        public void Promote(SingleItemList<T> oldList)
        {
            SetCount(oldList.Count);
            SetAt(0, oldList.EntryAt(0));
        }

        // Class specific implementation to avoid virtual method calls and additional logic
        public void Promote(ThreeItemList<T> oldList)
        {
            var oldCount = oldList.Count;
            SetCount(oldList.Count);

            switch (oldCount)
            {
                case 3:
                    SetAt(0, oldList.EntryAt(0));
                    SetAt(1, oldList.EntryAt(1));
                    SetAt(2, oldList.EntryAt(2));
                    break;

                case 2:
                    SetAt(0, oldList.EntryAt(0));
                    SetAt(1, oldList.EntryAt(1));
                    break;

                case 1:
                    SetAt(0, oldList.EntryAt(0));
                    break;

                case 0:
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(oldList));
            }
        }

        public override T[] ToArray()
        {
            var array = new T[_count];

            array[0] = _entry0;
            if (_count >= 2)
            {
                array[1] = _entry1;
                if (_count == 3)
                {
                    array[2] = _entry2;
                }
            }
            return array;
        }

        public override void CopyTo(T[] array, int index)
        {
            array[index] = _entry0;
            if (_count >= 2)
            {
                array[index + 1] = _entry1;
                if (_count == 3)
                {
                    array[index + 2] = _entry2;
                }
            }
        }

        public override object Clone()
        {
            var newList = new ThreeItemList<T>();
            newList.Promote(this);
            return newList;
        }

        private void SetCount(int value)
        {
            if (value >= 0 && value <= SIZE)
            {
                _count = value;
            }
            else
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }
        }
    }

    /// <summary>
    /// A simple class to handle a list with 6 items.
    /// </summary>
    internal sealed class SixItemList<T> : FrugalListBase<T>
    {
        private const int SIZE = 6;

        private T _entry0;
        private T _entry1;
        private T _entry2;
        private T _entry3;
        private T _entry4;
        private T _entry5;

        // Capacity of this store
        public override int Capacity
        {
            get
            {
                return SIZE;
            }
        }

        public override FrugalListStoreState Add(T value)
        {
            switch (_count)
            {
                case 0:
                    _entry0 = value;
                    break;

                case 1:
                    _entry1 = value;
                    break;

                case 2:
                    _entry2 = value;
                    break;

                case 3:
                    _entry3 = value;
                    break;

                case 4:
                    _entry4 = value;
                    break;

                case 5:
                    _entry5 = value;
                    break;

                default:
                    // We have to promote
                    return FrugalListStoreState.Array;
            }
            ++_count;
            return FrugalListStoreState.Success;
        }

        public override void Clear()
        {
            // Wipe out the info.
            _entry0 = default;
            _entry1 = default;
            _entry2 = default;
            _entry3 = default;
            _entry4 = default;
            _entry5 = default;
            _count = 0;
        }

        public override bool Contains(T value)
        {
            return -1 != IndexOf(value);
        }

        public override int IndexOf(T value)
        {
            if (_entry0.Equals(value))
            {
                return 0;
            }
            if (_count > 1)
            {
                if (_entry1.Equals(value))
                {
                    return 1;
                }
                if (_count > 2)
                {
                    if (_entry2.Equals(value))
                    {
                        return 2;
                    }
                    if (_count > 3)
                    {
                        if (_entry3.Equals(value))
                        {
                            return 3;
                        }
                        if (_count > 4)
                        {
                            if (_entry4.Equals(value))
                            {
                                return 4;
                            }
                            if (6 == _count && _entry5.Equals(value))
                            {
                                return 5;
                            }
                        }
                    }
                }
            }
            return -1;
        }

        public override void Insert(int index, T value)
        {
            // Should only get here if count is less than SIZE
            if (_count < SIZE)
            {
                switch (index)
                {
                    case 0:
                        _entry5 = _entry4;
                        _entry4 = _entry3;
                        _entry3 = _entry2;
                        _entry2 = _entry1;
                        _entry1 = _entry0;
                        _entry0 = value;
                        break;

                    case 1:
                        _entry5 = _entry4;
                        _entry4 = _entry3;
                        _entry3 = _entry2;
                        _entry2 = _entry1;
                        _entry1 = value;
                        break;

                    case 2:
                        _entry5 = _entry4;
                        _entry4 = _entry3;
                        _entry3 = _entry2;
                        _entry2 = value;
                        break;

                    case 3:
                        _entry5 = _entry4;
                        _entry4 = _entry3;
                        _entry3 = value;
                        break;

                    case 4:
                        _entry5 = _entry4;
                        _entry4 = value;
                        break;

                    case 5:
                        _entry5 = value;
                        break;

                    default:
                        throw new ArgumentOutOfRangeException(nameof(index));
                }
                ++_count;
                return;
            }
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        public override void SetAt(int index, T value)
        {
            // Overwrite item at index
            switch (index)
            {
                case 0:
                    _entry0 = value;
                    break;

                case 1:
                    _entry1 = value;
                    break;

                case 2:
                    _entry2 = value;
                    break;

                case 3:
                    _entry3 = value;
                    break;

                case 4:
                    _entry4 = value;
                    break;

                case 5:
                    _entry5 = value;
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(index));
            }
        }

        public override bool Remove(T value)
        {
            // If the item matches an existing entry, wipe out the last
            // entry and move all the other entries up.  Because we only
            // have six entries we can just unravel all the cases.
            if (_entry0.Equals(value))
            {
                RemoveAt(0);
                return true;
            }
            else if (_count > 1)
            {
                if (_entry1.Equals(value))
                {
                    RemoveAt(1);
                    return true;
                }
                else if (_count > 2)
                {
                    if (_entry2.Equals(value))
                    {
                        RemoveAt(2);
                        return true;
                    }
                    else if (_count > 3)
                    {
                        if (_entry3.Equals(value))
                        {
                            RemoveAt(3);
                            return true;
                        }
                        else if (_count > 4)
                        {
                            if (_entry4.Equals(value))
                            {
                                RemoveAt(4);
                                return true;
                            }
                            else if (6 == _count && _entry5.Equals(value))
                            {
                                RemoveAt(5);
                                return true;
                            }
                        }
                    }
                }
            }

            return false;
        }

        public override void RemoveAt(int index)
        {
            // Remove entry at index, wipe out the last entry and move
            // all the other entries up. Because we only have six
            // entries we can just unravel all the cases.
            switch (index)
            {
                case 0:
                    _entry0 = _entry1;
                    _entry1 = _entry2;
                    _entry2 = _entry3;
                    _entry3 = _entry4;
                    _entry4 = _entry5;
                    break;

                case 1:
                    _entry1 = _entry2;
                    _entry2 = _entry3;
                    _entry3 = _entry4;
                    _entry4 = _entry5;
                    break;

                case 2:
                    _entry2 = _entry3;
                    _entry3 = _entry4;
                    _entry4 = _entry5;
                    break;

                case 3:
                    _entry3 = _entry4;
                    _entry4 = _entry5;
                    break;

                case 4:
                    _entry4 = _entry5;
                    break;

                case 5:
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(index));
            }
            _entry5 = default;
            --_count;
        }

        public override T EntryAt(int index)
        {
            switch (index)
            {
                case 0:
                    return _entry0;

                case 1:
                    return _entry1;

                case 2:
                    return _entry2;

                case 3:
                    return _entry3;

                case 4:
                    return _entry4;

                case 5:
                    return _entry5;

                default:
                    throw new ArgumentOutOfRangeException(nameof(index));
            }
        }

        public override void Promote(FrugalListBase<T> oldList)
        {
            var oldCount = oldList.Count;
            if (SIZE >= oldCount)
            {
                SetCount(oldList.Count);

                switch (oldCount)
                {
                    case 6:
                        SetAt(0, oldList.EntryAt(0));
                        SetAt(1, oldList.EntryAt(1));
                        SetAt(2, oldList.EntryAt(2));
                        SetAt(3, oldList.EntryAt(3));
                        SetAt(4, oldList.EntryAt(4));
                        SetAt(5, oldList.EntryAt(5));
                        break;

                    case 5:
                        SetAt(0, oldList.EntryAt(0));
                        SetAt(1, oldList.EntryAt(1));
                        SetAt(2, oldList.EntryAt(2));
                        SetAt(3, oldList.EntryAt(3));
                        SetAt(4, oldList.EntryAt(4));
                        break;

                    case 4:
                        SetAt(0, oldList.EntryAt(0));
                        SetAt(1, oldList.EntryAt(1));
                        SetAt(2, oldList.EntryAt(2));
                        SetAt(3, oldList.EntryAt(3));
                        break;

                    case 3:
                        SetAt(0, oldList.EntryAt(0));
                        SetAt(1, oldList.EntryAt(1));
                        SetAt(2, oldList.EntryAt(2));
                        break;

                    case 2:
                        SetAt(0, oldList.EntryAt(0));
                        SetAt(1, oldList.EntryAt(1));
                        break;

                    case 1:
                        SetAt(0, oldList.EntryAt(0));
                        break;

                    case 0:
                        break;

                    default:
                        throw new ArgumentOutOfRangeException(nameof(oldList));
                }
            }
            else
            {
                // this list is smaller than oldList
                throw new ArgumentException($"Cannot promote from '{oldList}' to '{ToString()}' because the target map is too small.", nameof(oldList));
            }
        }

        // Class specific implementation to avoid virtual method calls and additional logic
        public void Promote(ThreeItemList<T> oldList)
        {
            var oldCount = oldList.Count;

            if (oldCount <= SIZE)
            {
                SetCount(oldList.Count);

                switch (oldCount)
                {
                    case 3:
                        SetAt(0, oldList.EntryAt(0));
                        SetAt(1, oldList.EntryAt(1));
                        SetAt(2, oldList.EntryAt(2));
                        break;

                    case 2:
                        SetAt(0, oldList.EntryAt(0));
                        SetAt(1, oldList.EntryAt(1));
                        break;

                    case 1:
                        SetAt(0, oldList.EntryAt(0));
                        break;

                    case 0:
                        break;

                    default:
                        throw new ArgumentOutOfRangeException(nameof(oldList));
                }
            }
            else
            {
                // this list is smaller than oldList
                throw new ArgumentException($"Cannot promote from '{oldList}' to '{ToString()}' because the target map is too small.", nameof(oldList));
            }
        }

        // Class specific implementation to avoid virtual method calls and additional logic
        public void Promote(SixItemList<T> oldList)
        {
            var oldCount = oldList.Count;

            SetCount(oldList.Count);

            switch (oldCount)
            {
                case 6:
                    SetAt(0, oldList.EntryAt(0));
                    SetAt(1, oldList.EntryAt(1));
                    SetAt(2, oldList.EntryAt(2));
                    SetAt(3, oldList.EntryAt(3));
                    SetAt(4, oldList.EntryAt(4));
                    SetAt(5, oldList.EntryAt(5));
                    break;

                case 5:
                    SetAt(0, oldList.EntryAt(0));
                    SetAt(1, oldList.EntryAt(1));
                    SetAt(2, oldList.EntryAt(2));
                    SetAt(3, oldList.EntryAt(3));
                    SetAt(4, oldList.EntryAt(4));
                    break;

                case 4:
                    SetAt(0, oldList.EntryAt(0));
                    SetAt(1, oldList.EntryAt(1));
                    SetAt(2, oldList.EntryAt(2));
                    SetAt(3, oldList.EntryAt(3));
                    break;

                case 3:
                    SetAt(0, oldList.EntryAt(0));
                    SetAt(1, oldList.EntryAt(1));
                    SetAt(2, oldList.EntryAt(2));
                    break;

                case 2:
                    SetAt(0, oldList.EntryAt(0));
                    SetAt(1, oldList.EntryAt(1));
                    break;

                case 1:
                    SetAt(0, oldList.EntryAt(0));
                    break;

                case 0:
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(oldList));
            }
        }

        public override T[] ToArray()
        {
            var array = new T[_count];

            if (_count >= 1)
            {
                array[0] = _entry0;
                if (_count >= 2)
                {
                    array[1] = _entry1;
                    if (_count >= 3)
                    {
                        array[2] = _entry2;
                        if (_count >= 4)
                        {
                            array[3] = _entry3;
                            if (_count >= 5)
                            {
                                array[4] = _entry4;
                                if (_count == 6)
                                {
                                    array[5] = _entry5;
                                }
                            }
                        }
                    }
                }
            }
            return array;
        }

        public override void CopyTo(T[] array, int index)
        {
            if (_count >= 1)
            {
                array[index] = _entry0;
                if (_count >= 2)
                {
                    array[index + 1] = _entry1;
                    if (_count >= 3)
                    {
                        array[index + 2] = _entry2;
                        if (_count >= 4)
                        {
                            array[index + 3] = _entry3;
                            if (_count >= 5)
                            {
                                array[index + 4] = _entry4;
                                if (_count == 6)
                                {
                                    array[index + 5] = _entry5;
                                }
                            }
                        }
                    }
                }
            }
        }

        public override object Clone()
        {
            var newList = new SixItemList<T>();

            newList.Promote(this);

            return newList;
        }

        private void SetCount(int value)
        {
            if (value >= 0 && value <= SIZE)
            {
                _count = value;
            }
            else
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }
        }
    }

    /// <summary>
    /// A simple class to handle an array of 7 or more items.  It is unsorted and uses
    /// a linear search.
    /// </summary>
    internal sealed class ArrayItemList<T> : FrugalListBase<T>
    {
        // MINSIZE and GROWTH chosen to minimize memory footprint
        private const int MINSIZE = 9;
        private const int GROWTH = 3;
        private const int LARGEGROWTH = 18;

        private T[] _entries;

        public ArrayItemList()
        {
        }

        public ArrayItemList(int size)
        {
            // Make size a multiple of GROWTH
            size += GROWTH - 1;

            size -= size % GROWTH;

            _entries = new T[size];
        }

        public ArrayItemList(ICollection collection)
        {
            if (collection == null)
            {
                return;
            }

            _count = collection.Count;

            _entries = new T[_count];

            collection.CopyTo(_entries, 0);
        }

        public ArrayItemList(ICollection<T> collection)
        {
            if (collection == null)
            {
                return;
            }

            _count = collection.Count;

            _entries = new T[_count];

            collection.CopyTo(_entries, 0);
        }

        // Capacity of this store
        public override int Capacity
        {
            get
            {
                return _entries?.Length ?? 0;
            }
        }

        public override FrugalListStoreState Add(T value)
        {
            // If we don't have any entries or the existing entry is being overwritten,
            // then we can use this store. Otherwise we have to promote.
            if (null != _entries && _count < _entries.Length)
            {
                _entries[_count] = value;

                ++_count;
            }
            else
            {
                if (null != _entries)
                {
                    var size = _entries.Length;

                    // Grow the list slowly while it is small but
                    // faster once it reaches the LARGEGROWTH size
                    if (size < LARGEGROWTH)
                    {
                        size += GROWTH;
                    }
                    else
                    {
                        size += size >> 2;
                    }

                    var destEntries = new T[size];

                    // Copy old array
                    Array.Copy(_entries, 0, destEntries, 0, _entries.Length);

                    _entries = destEntries;
                }
                else
                {
                    _entries = new T[MINSIZE];
                }

                // Insert into new array
                _entries[_count] = value;

                ++_count;
            }

            return FrugalListStoreState.Success;
        }

        public override void Clear()
        {
            // Wipe out the info.
            for (var i = 0; i < _count; ++i)
            {
                _entries[i] = default;
            }

            _count = 0;
        }

        public override bool Contains(T value)
        {
            return -1 != IndexOf(value);
        }

        public override int IndexOf(T value)
        {
            for (var index = 0; index < _count; ++index)
            {
                if (_entries[index].Equals(value))
                {
                    return index;
                }
            }

            return -1;
        }

        public override void Insert(int index, T value)
        {
            if (null != _entries && _count < _entries.Length)
            {
                // Move down the required number of items
                Array.Copy(_entries, index, _entries, index + 1, _count - index);

                // Put in the new item at the specified index
                _entries[index] = value;

                ++_count;

                return;
            }

            throw new ArgumentOutOfRangeException(nameof(index));
        }

        public override void SetAt(int index, T value)
        {
            // Overwrite item at index
            _entries[index] = value;
        }

        public override bool Remove(T value)
        {
            for (var index = 0; index < _count; ++index)
            {
                if (_entries[index].Equals(value))
                {
                    RemoveAt(index);
                    return true;
                }
            }

            return false;
        }

        public override void RemoveAt(int index)
        {
            // Shift entries down
            var numToCopy = _count - index - 1;

            if (numToCopy > 0)
            {
                Array.Copy(_entries, index + 1, _entries, index, numToCopy);
            }

            // Wipe out the last entry
            _entries[_count - 1] = default;

            --_count;
        }

        public override T EntryAt(int index)
        {
            return _entries[index];
        }

        public override void Promote(FrugalListBase<T> oldList)
        {
            for (var index = 0; index < oldList.Count; ++index)
            {
                if (FrugalListStoreState.Success == Add(oldList.EntryAt(index)))
                {
                    continue;
                }

                // this list is smaller than oldList
                throw new ArgumentException($"Cannot promote from '{oldList}' to '{ToString()}' because the target map is too small.", nameof(oldList));
            }
        }

        // Class specific implementation to avoid virtual method calls and additional logic
        public void Promote(SixItemList<T> oldList)
        {
            var oldCount = oldList.Count;

            SetCount(oldList.Count);

            switch (oldCount)
            {
                case 6:
                    SetAt(0, oldList.EntryAt(0));
                    SetAt(1, oldList.EntryAt(1));
                    SetAt(2, oldList.EntryAt(2));
                    SetAt(3, oldList.EntryAt(3));
                    SetAt(4, oldList.EntryAt(4));
                    SetAt(5, oldList.EntryAt(5));
                    break;

                case 5:
                    SetAt(0, oldList.EntryAt(0));
                    SetAt(1, oldList.EntryAt(1));
                    SetAt(2, oldList.EntryAt(2));
                    SetAt(3, oldList.EntryAt(3));
                    SetAt(4, oldList.EntryAt(4));
                    break;

                case 4:
                    SetAt(0, oldList.EntryAt(0));
                    SetAt(1, oldList.EntryAt(1));
                    SetAt(2, oldList.EntryAt(2));
                    SetAt(3, oldList.EntryAt(3));
                    break;

                case 3:
                    SetAt(0, oldList.EntryAt(0));
                    SetAt(1, oldList.EntryAt(1));
                    SetAt(2, oldList.EntryAt(2));
                    break;

                case 2:
                    SetAt(0, oldList.EntryAt(0));
                    SetAt(1, oldList.EntryAt(1));
                    break;

                case 1:
                    SetAt(0, oldList.EntryAt(0));
                    break;

                case 0:
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(oldList));
            }
        }

        // Class specific implementation to avoid virtual method calls and additional logic
        public void Promote(ArrayItemList<T> oldList)
        {
            var oldCount = oldList.Count;

            if (_entries.Length >= oldCount)
            {
                SetCount(oldList.Count);

                for (var index = 0; index < oldCount; ++index)
                {
                    SetAt(index, oldList.EntryAt(index));
                }
            }
            else
            {
                // this list is smaller than oldList
                throw new ArgumentException($"Cannot promote from '{oldList}' to '{ToString()}' because the target map is too small.", nameof(oldList));
            }
        }

        public override T[] ToArray()
        {
            var array = new T[_count];

            for (var i = 0; i < _count; ++i)
            {
                array[i] = _entries[i];
            }

            return array;
        }

        public override void CopyTo(T[] array, int index)
        {
            for (var i = 0; i < _count; ++i)
            {
                array[index + i] = _entries[i];
            }
        }

        public override object Clone()
        {
            var newList = new ArrayItemList<T>(Capacity);

            newList.Promote(this);

            return newList;
        }

        private void SetCount(int value)
        {
            if (value >= 0 && value <= _entries.Length)
            {
                _count = value;
            }
            else
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }
        }

        public override Compacter NewCompacter(int newCount)
        {
            return new ArrayCompacter(this, newCount);
        }

        // array-based implementation - compacts in-place or into a new array
        internal class ArrayCompacter : Compacter
        {
            private readonly ArrayItemList<T> _targetStore;
            private readonly T[] _sourceArray;
            private readonly T[] _targetArray;

            public ArrayCompacter(ArrayItemList<T> store, int newCount)
                : base(store, newCount)
            {
                _sourceArray = store._entries;

                // compute capacity for target array
                // the first term agrees with AIL.Add, which grows by 5/4
                var newCapacity = Math.Max(newCount + (newCount >> 2), MINSIZE);

                if (newCapacity + (newCapacity >> 2) >= _sourceArray.Length)
                {
                    // if there's not much space to be reclaimed, compact in place
                    _targetStore = store;
                }
                else
                {
                    // otherwise, compact into a smaller array
                    _targetStore = new ArrayItemList<T>(newCapacity);
                }

                _targetArray = _targetStore._entries;
            }

            protected override void IncludeOverride(int start, int end)
            {
                // bulk move
                var size = end - start;
                Array.Copy(_sourceArray, start, _targetArray, _validItemCount, size);
                _validItemCount += size;

                /* The following code is necessary in the general case, to avoid
                 * aliased entries in the old array.  But the only user of Compacter
                 * is DependentList, where aliased entries are not a problem - they'll
                 * just get GC'd along with the old array.

                // when not compacting in place, clear out entries in the source
                if (_targetArray != _sourceArray)
                {
                    T filler = default(T);
                    for (int i=_previousEnd; i<end; ++i)
                    {
                        _sourceArray[i] = filler;
                    }
                }

                */
            }

            public override FrugalListBase<T> Finish()
            {
                // clear out vacated entries in the source
                var filler = default(T);
                if (_sourceArray == _targetArray)
                {
                    // in-place array source
                    for (int i = _validItemCount, n = _store.Count; i < n; ++i)
                    {
                        _sourceArray[i] = filler;
                    }
                }
                else
                {
                    // array source to new array target
                    /* this code is not needed - see remarks in IncludeOverride()
                    for (int i=_previousEnd, n=_store._count; i<n; ++i)
                    {
                        _sourceArray[i] = filler;
                    }
                    */
                }

                // reset Count and return target store
                _targetStore._count = _validItemCount;
                return _targetStore;
            }
        }
    }

    // Use FrugalObjectList when more than one reference to the list is needed.
    // The "object" in FrugalObjectLIst refers to the list itself, not what the list contains.

    internal class FrugalObjectList<T>
    {
        internal FrugalListBase<T> _listStore;

        public FrugalObjectList()
        {
        }

        public FrugalObjectList(int size)
        {
            Capacity = size;
        }

        public int Capacity
        {
            get
            {
                return _listStore?.Capacity ?? 0;
            }
            set
            {
                var capacity = 0;

                if (null != _listStore)
                {
                    capacity = _listStore.Capacity;
                }

                if (capacity < value)
                {
                    // Need to move to a more complex storage
                    FrugalListBase<T> newStore;

                    if (value == 1)
                    {
                        newStore = new SingleItemList<T>();
                    }
                    else if (value <= 3)
                    {
                        newStore = new ThreeItemList<T>();
                    }
                    else if (value <= 6)
                    {
                        newStore = new SixItemList<T>();
                    }
                    else
                    {
                        newStore = new ArrayItemList<T>(value);
                    }

                    if (null != _listStore)
                    {
                        // Move entries in the old store to the new one
                        newStore.Promote(_listStore);
                    }

                    _listStore = newStore;
                }
            }
        }

        public int Count
        {
            get
            {
                return _listStore?.Count ?? 0;
            }
        }


        public T this[int index]
        {
            get
            {
                // If no entry, default(T) is returned
                if (null != _listStore && index < _listStore.Count && index >= 0)
                {
                    return _listStore.EntryAt(index);
                }

                throw new ArgumentOutOfRangeException(nameof(index));
            }

            set
            {
                // Ensure write success
                if (null != _listStore && index < _listStore.Count && index >= 0)
                {
                    _listStore.SetAt(index, value);

                    return;
                }

                throw new ArgumentOutOfRangeException(nameof(index));
            }
        }

        public int Add(T value)
        {
            if (null != _listStore)
            {
                // This is done because forward branches
                // default prediction is not to be taken
                // making this a CPU win because Add is
                // a common operation.
            }
            else
            {
                _listStore = new SingleItemList<T>();
            }

            var myState = _listStore.Add(value);

            switch (myState) {
                case FrugalListStoreState.Success:
                    break;
                // Need to move to a more complex storage
                // Allocate the store, promote, and add using the derived classes
                // to avoid virtual method calls
                case FrugalListStoreState.ThreeItemList:
                {
                    var newStore = new ThreeItemList<T>();

                    // Extract the values from the old store and insert them into the new store
                    newStore.Promote(_listStore);

                    // Insert the new item
                    newStore.Add(value);

                    _listStore = newStore;
                    break;
                }
                case FrugalListStoreState.SixItemList:
                {
                    var newStore = new SixItemList<T>();

                    // Extract the values from the old store and insert them into the new store
                    newStore.Promote(_listStore);

                    _listStore = newStore;

                    // Insert the new item
                    newStore.Add(value);

                    _listStore = newStore;
                    break;
                }
                case FrugalListStoreState.Array:
                {
                    var newStore = new ArrayItemList<T>(_listStore.Count + 1);

                    // Extract the values from the old store and insert them into the new store
                    newStore.Promote(_listStore);

                    _listStore = newStore;

                    // Insert the new item
                    newStore.Add(value);

                    _listStore = newStore;
                    break;
                }
                default:
                    throw new InvalidOperationException("Cannot promote from Array.");
            }

            return _listStore.Count - 1;
        }

        public void Clear()
        {
            _listStore?.Clear();
        }

        public bool Contains(T value)
        {
            if (null != _listStore && _listStore.Count > 0)
            {
                return _listStore.Contains(value);
            }

            return false;
        }

        public int IndexOf(T value)
        {
            if (null != _listStore && _listStore.Count > 0)
            {
                return _listStore.IndexOf(value);
            }

            return -1;
        }

        public void Insert(int index, T value)
        {
            if (index == 0 || _listStore != null && index <= _listStore.Count && index >= 0)
            {
                // Make sure we have a place to put the item
                var minCapacity = 1;

                if (null != _listStore && _listStore.Count == _listStore.Capacity)
                {
                    // Store is full
                    minCapacity = Capacity + 1;
                }

                // Make the Capacity at *least* this big
                Capacity = minCapacity;

                _listStore?.Insert(index, value);

                return;
            }

            throw new ArgumentOutOfRangeException(nameof(index));
        }

        public bool Remove(T value)
        {
            if (null != _listStore && _listStore.Count > 0)
            {
                return _listStore.Remove(value);
            }

            return false;
        }

        public void RemoveAt(int index)
        {
            if (null != _listStore && index < _listStore.Count && index >= 0)
            {
                _listStore.RemoveAt(index);

                return;
            }

            throw new ArgumentOutOfRangeException(nameof(index));
        }

        public void EnsureIndex(int index)
        {
            if (index >= 0)
            {
                var delta = index + 1 - Count;
                if (delta > 0)
                {
                    // Grow the store
                    Capacity = index + 1;

                    var filler = default(T);

                    // Insert filler structs or objects
                    for (var i = 0; i < delta; ++i)
                    {
                        _listStore.Add(filler);
                    }
                }

                return;
            }

            throw new ArgumentOutOfRangeException(nameof(index));
        }

        public T[] ToArray()
        {
            if (null != _listStore && _listStore.Count > 0)
            {
                return _listStore.ToArray();
            }

            return null;
        }

        public void CopyTo(T[] array, int index)
        {
            if (null != _listStore && _listStore.Count > 0)
            {
                _listStore.CopyTo(array, index);
            }
        }

        public FrugalObjectList<T> Clone()
        {
            var myClone = new FrugalObjectList<T>();

            if (null != _listStore)
            {
                myClone._listStore = (FrugalListBase<T>)_listStore.Clone();
            }

            return myClone;
        }

        // helper class - compacts the valid entries, while removing the invalid ones.
        // Usage:
        //      Compacter compacter = new Compacter(this, newCount);
        //      compacter.Include(start, end);      // repeat as necessary
        //      compacter.Finish();
        // newCount is the expected number of valid entries - used to help choose
        //  a target array of appropriate capacity
        // Include(start, end) moves the entries in positions start, ..., end-1 toward
        //  the beginning, appending to the end of the "valid" area.  Successive calls
        //  must be monotonic - i.e. the next 'start' must be >= the previous 'end'.
        //  Also, the sum of the block sizes (end-start) cannot exceed newCount.
        // Finish() puts the provisional target array into permanent use.

        protected class Compacter
        {
            private readonly FrugalObjectList<T> _list;
            private readonly FrugalListBase<T>.Compacter _storeCompacter;

            public Compacter(FrugalObjectList<T> list, int newCount)
            {
                _list = list;

                var store = _list._listStore;

                _storeCompacter = store?.NewCompacter(newCount);
            }

            public void Include(int start, int end)
            {
                _storeCompacter.Include(start, end);
            }

            public void Finish()
            {
                if (_storeCompacter != null)
                {
                    _list._listStore = _storeCompacter.Finish();
                }
            }
        }
    }

    // Use FrugalStructList when only one reference to the list is needed.
    // The "struct" in FrugalStructList refers to the list itself, not what the list contains.
    internal struct FrugalStructList<T>
    {
        internal FrugalListBase<T> _listStore;

        public FrugalStructList(int size)
        {
            _listStore = null;
            Capacity = size;
        }

        public FrugalStructList(ICollection collection)
        {
            if (collection.Count > 6)
            {
                _listStore = new ArrayItemList<T>(collection);
            }
            else
            {
                _listStore = null;

                Capacity = collection.Count;

                foreach (T item in collection)
                {
                    Add(item);
                }
            }
        }

        public FrugalStructList(ICollection<T> collection)
        {
            if (collection.Count > 6)
            {
                _listStore = new ArrayItemList<T>(collection);
            }
            else
            {
                _listStore = null;

                Capacity = collection.Count;

                foreach (var item in collection)
                {
                    Add(item);
                }
            }
        }

        public int Capacity
        {
            get
            {
                if (null != _listStore)
                {
                    return _listStore.Capacity;
                }

                return 0;
            }
            set
            {
                var capacity = 0;

                if (null != _listStore)
                {
                    capacity = _listStore.Capacity;
                }

                if (capacity < value)
                {
                    // Need to move to a more complex storage
                    FrugalListBase<T> newStore;

                    if (value == 1)
                    {
                        newStore = new SingleItemList<T>();
                    }
                    else if (value <= 3)
                    {
                        newStore = new ThreeItemList<T>();
                    }
                    else if (value <= 6)
                    {
                        newStore = new SixItemList<T>();
                    }
                    else
                    {
                        newStore = new ArrayItemList<T>(value);
                    }

                    if (null != _listStore)
                    {
                        // Move entries in the old store to the new one
                        newStore.Promote(_listStore);
                    }

                    _listStore = newStore;
                }
            }
        }

        public int Count
        {
            get
            {
                return _listStore?.Count ?? 0;
            }
        }


        public T this[int index]
        {
            get
            {
                // If no entry, default(T) is returned
                if (null != _listStore && index < _listStore.Count && index >= 0)
                {
                    return _listStore.EntryAt(index);
                }

                throw new ArgumentOutOfRangeException(nameof(index));
            }

            set
            {
                // Ensure write success
                if (null != _listStore && index < _listStore.Count && index >= 0)
                {
                    _listStore.SetAt(index, value);
                    return;
                }

                throw new ArgumentOutOfRangeException(nameof(index));
            }
        }

        public int Add(T value)
        {
            if (null != _listStore)
            {
                // This is done because forward branches
                // default prediction is not to be taken
                // making this a CPU win because Add is
                // a common operation.
            }
            else
            {
                _listStore = new SingleItemList<T>();
            }

            var myState = _listStore.Add(value);
            switch (myState) {
                case FrugalListStoreState.Success:
                    break;
                // Need to move to a more complex storage
                // Allocate the store, promote, and add using the derived classes
                // to avoid virtual method calls
                case FrugalListStoreState.ThreeItemList:
                {
                    var newStore = new ThreeItemList<T>();

                    // Extract the values from the old store and insert them into the new store
                    newStore.Promote(_listStore);

                    // Insert the new item
                    newStore.Add(value);
                    _listStore = newStore;
                    break;
                }
                case FrugalListStoreState.SixItemList:
                {
                    var newStore = new SixItemList<T>();

                    // Extract the values from the old store and insert them into the new store
                    newStore.Promote(_listStore);
                    _listStore = newStore;

                    // Insert the new item
                    newStore.Add(value);
                    _listStore = newStore;
                    break;
                }
                case FrugalListStoreState.Array:
                {
                    var newStore = new ArrayItemList<T>(_listStore.Count + 1);

                    // Extract the values from the old store and insert them into the new store
                    newStore.Promote(_listStore);
                    _listStore = newStore;

                    // Insert the new item
                    newStore.Add(value);
                    _listStore = newStore;
                    break;
                }
                default:
                    throw new InvalidOperationException("Cannot promote from Array.");
            }

            return _listStore.Count - 1;
        }

        public void Clear()
        {
            _listStore?.Clear();
        }

        public bool Contains(T value)
        {
            if (null != _listStore && _listStore.Count > 0)
            {
                return _listStore.Contains(value);
            }

            return false;
        }

        public int IndexOf(T value)
        {
            if (null != _listStore && _listStore.Count > 0)
            {
                return _listStore.IndexOf(value);
            }

            return -1;
        }

        public void Insert(int index, T value)
        {
            if (index == 0 || null != _listStore && index <= _listStore.Count && index >= 0)
            {
                // Make sure we have a place to put the item
                var minCapacity = 1;

                if (null != _listStore && _listStore.Count == _listStore.Capacity)
                {
                    // Store is full
                    minCapacity = Capacity + 1;
                }

                // Make the Capacity at *least* this big
                Capacity = minCapacity;

                _listStore.Insert(index, value);

                return;
            }

            throw new ArgumentOutOfRangeException(nameof(index));
        }

        public bool Remove(T value)
        {
            if (null != _listStore && _listStore.Count > 0)
            {
                return _listStore.Remove(value);
            }

            return false;
        }

        public void RemoveAt(int index)
        {
            if (null != _listStore && index < _listStore.Count && index >= 0)
            {
                _listStore.RemoveAt(index);
                return;
            }

            throw new ArgumentOutOfRangeException(nameof(index));
        }

        public void EnsureIndex(int index)
        {
            if (index >= 0)
            {
                var delta = index + 1 - Count;
                if (delta > 0)
                {
                    // Grow the store
                    Capacity = index + 1;

                    var filler = default(T);

                    // Insert filler structs or objects
                    for (var i = 0; i < delta; ++i)
                    {
                        _listStore.Add(filler);
                    }
                }
                return;
            }

            throw new ArgumentOutOfRangeException(nameof(index));
        }

        public T[] ToArray()
        {
            if (null != _listStore && _listStore.Count > 0)
            {
                return _listStore.ToArray();
            }

            return null;
        }

        public void CopyTo(T[] array, int index)
        {
            if (null != _listStore && _listStore.Count > 0)
            {
                _listStore.CopyTo(array, index);
            }
        }

        public FrugalStructList<T> Clone()
        {
            var myClone = new FrugalStructList<T>();

            if (null != _listStore)
            {
                myClone._listStore = (FrugalListBase<T>)_listStore.Clone();
            }

            return myClone;
        }
    }
}

