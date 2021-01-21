using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using Avalonia.Controls.Utils;

#nullable enable

namespace Avalonia.Controls.Selection
{
    public abstract class SelectionNodeBase<T> : ICollectionChangedListener
    {
        private IEnumerable? _source;
        private bool _rangesEnabled;
        private List<IndexRange>? _ranges;
        private int _collectionChanging;

        protected IEnumerable? Source
        {
            get => _source;
            set
            {
                if (_source != value)
                {
                    ItemsView?.RemoveListener(this);
                    _source = value;
                    ItemsView = ItemsSourceView<T>.GetOrCreate(value);
                    ItemsView?.AddListener(this);
                }
            }
        }

        protected bool IsSourceCollectionChanging => _collectionChanging > 0;

        protected bool RangesEnabled
        {
            get => _rangesEnabled;
            set
            {
                if (_rangesEnabled != value)
                {
                    _rangesEnabled = value;

                    if (!_rangesEnabled)
                    {
                        _ranges = null;
                    }
                }
            }
        }

        internal ItemsSourceView<T>? ItemsView { get; set; }

        internal IReadOnlyList<IndexRange> Ranges
        {
            get
            {
                if (!RangesEnabled)
                {
                    throw new InvalidOperationException("Ranges not enabled.");
                }

                return _ranges ??= new List<IndexRange>();
            }
        }

        void ICollectionChangedListener.PreChanged(INotifyCollectionChanged sender, NotifyCollectionChangedEventArgs e)
        {
            ++_collectionChanging;
        }

        void ICollectionChangedListener.Changed(INotifyCollectionChanged sender, NotifyCollectionChangedEventArgs e)
        {
            OnSourceCollectionChanged(e);
        }

        void ICollectionChangedListener.PostChanged(INotifyCollectionChanged sender, NotifyCollectionChangedEventArgs e)
        {
            if (--_collectionChanging == 0)
            {
                OnSourceCollectionChangeFinished();
            }
        }

        protected abstract void OnSourceCollectionChangeFinished();

        private protected abstract void OnIndexesChanged(int shiftIndex, int shiftDelta);

        private protected abstract void OnSourceReset();

        private protected abstract void OnSelectionChanged(IReadOnlyList<T> deselectedItems);

        private protected int CommitSelect(IndexRange range)
        {
            if (RangesEnabled)
            {
                _ranges ??= new List<IndexRange>();
                return IndexRange.Add(_ranges, range);
            }

            return 0;
        }

        private protected int CommitSelect(IReadOnlyList<IndexRange> ranges)
        {
            if (RangesEnabled)
            {
                _ranges ??= new List<IndexRange>();
                return IndexRange.Add(_ranges, ranges);
            }

            return 0;
        }

        private protected int CommitDeselect(IndexRange range)
        {
            if (RangesEnabled)
            {
                _ranges ??= new List<IndexRange>();
                return IndexRange.Remove(_ranges, range);
            }

            return 0;
        }

        private protected int CommitDeselect(IReadOnlyList<IndexRange> ranges)
        {
            if (RangesEnabled && _ranges is object)
            {
                return IndexRange.Remove(_ranges, ranges);
            }

            return 0;
        }

        private protected virtual CollectionChangeState OnItemsAdded(int index, IList items)
        {
            var count = items.Count;
            var shifted = false;

            if (_ranges is object)
            {
                List<IndexRange>? toAdd = null;

                for (var i = 0; i < Ranges!.Count; ++i)
                {
                    var range = Ranges[i];

                    // The range is after the inserted items, need to shift the range right
                    if (range.End >= index)
                    {
                        int begin = range.Begin;

                        // If the index left of newIndex is inside the range,
                        // Split the range and remember the left piece to add later
                        if (range.Contains(index - 1))
                        {
                            range.Split(index - 1, out var before, out _);
                            (toAdd ??= new List<IndexRange>()).Add(before);
                            begin = index;
                        }

                        // Shift the range to the right
                        _ranges[i] = new IndexRange(begin + count, range.End + count);
                        shifted = true;
                    }
                }

                if (toAdd is object)
                {
                    foreach (var range in toAdd)
                    {
                        IndexRange.Add(_ranges, range);
                    }
                }
            }

            return new CollectionChangeState
            {
                ShiftIndex = index,
                ShiftDelta = shifted ? count : 0,
            };
        }

        private protected virtual CollectionChangeState OnItemsRemoved(int index, IList items)
        {
            var count = items.Count;
            var removedRange = new IndexRange(index, index + count - 1);
            bool shifted = false;
            List<T>? removed = null;

            if (_ranges is object)
            {
                var deselected = new List<IndexRange>();

                if (IndexRange.Remove(_ranges, removedRange, deselected) > 0)
                {
                    removed = new List<T>();

                    foreach (var range in deselected)
                    {
                        for (var i = range.Begin; i <= range.End; ++i)
                        {
#pragma warning disable CS8604
                            removed.Add((T)items[i - index]);
#pragma warning restore CS8604
                        }
                    }
                }

                for (var i = 0; i < Ranges!.Count; ++i)
                {
                    var existing = Ranges[i];

                    if (existing.End > removedRange.Begin)
                    {
                        _ranges[i] = new IndexRange(existing.Begin - count, existing.End - count);
                        shifted = true;
                    }
                }
            }

            return new CollectionChangeState
            {
                ShiftIndex = index,
                ShiftDelta = shifted ? -count : 0,
                RemovedItems = removed,
            };
        }

        private protected virtual void OnSourceCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            var shiftDelta = 0;
            var shiftIndex = -1;
            List<T>? removed = null;

            if (!IsValidCollectionChange(e))
            {
                return;
            }

            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    {
                        var change = OnItemsAdded(e.NewStartingIndex, e.NewItems);
                        shiftIndex = change.ShiftIndex;
                        shiftDelta = change.ShiftDelta;
                        break;
                    }
                case NotifyCollectionChangedAction.Remove:
                    {
                        var change = OnItemsRemoved(e.OldStartingIndex, e.OldItems);
                        shiftIndex = change.ShiftIndex;
                        shiftDelta = change.ShiftDelta;
                        removed = change.RemovedItems;
                        break;
                    }
                case NotifyCollectionChangedAction.Replace:
                    {
                        var removeChange = OnItemsRemoved(e.OldStartingIndex, e.OldItems);
                        var addChange = OnItemsAdded(e.NewStartingIndex, e.NewItems);
                        shiftIndex = removeChange.ShiftIndex;
                        shiftDelta = removeChange.ShiftDelta + addChange.ShiftDelta;
                        removed = removeChange.RemovedItems;
                    }
                    break;
                case NotifyCollectionChangedAction.Reset:
                    OnSourceReset();
                    break;
            }

            if (shiftDelta != 0)
            {
                OnIndexesChanged(shiftIndex, shiftDelta);
            }

            if (removed is object)
            {
                OnSelectionChanged(removed);
            }
        }

        private protected virtual bool IsValidCollectionChange(NotifyCollectionChangedEventArgs e)
        {
            // If the selection is modified in a CollectionChanged handler before the selection
            // model's CollectionChanged handler has had chance to run then we can end up with
            // a selected index that refers to the *new* state of the Source intermixed with
            // indexes that reference an old state of the source.
            //
            // There's not much we can do in this situation, so detect whether shifting the
            // current selected indexes would result in an invalid index in the source, and if
            // so bail.
            //
            // See unit test Handles_Selection_Made_In_CollectionChanged for more details.
            if (ItemsView is object &&
                RangesEnabled &&
                Ranges.Count > 0 &&
                e.Action == NotifyCollectionChangedAction.Add)
            {
                var lastIndex = Ranges.Last().End;

                if (e.NewStartingIndex <= lastIndex)
                {
                    return lastIndex + e.NewItems.Count < ItemsView.Count;
                }
            }

            return true;
        }

        private protected struct CollectionChangeState
        {
            public int ShiftIndex;
            public int ShiftDelta;
            public List<T>? RemovedItems;
        }
    }
}
