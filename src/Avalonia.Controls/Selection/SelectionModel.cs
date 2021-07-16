using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

#nullable enable

namespace Avalonia.Controls.Selection
{
    public class SelectionModel<T> : SelectionNodeBase<T>, ISelectionModel
    {
        private bool _singleSelect = true;
        private int _anchorIndex = -1;
        private int _selectedIndex = -1;
        private Operation? _operation;
        private SelectedIndexes<T>? _selectedIndexes;
        private SelectedItems<T>? _selectedItems;
        private SelectedItems<T>.Untyped? _selectedItemsUntyped;
        private EventHandler<SelectionModelSelectionChangedEventArgs>? _untypedSelectionChanged;
        private IList? _initSelectedItems;

        public SelectionModel()
        {
        }

        public SelectionModel(IEnumerable<T>? source)
        {
            Source = source;
        }

        public new IEnumerable<T>? Source
        {
            get => base.Source as IEnumerable<T>;
            set => SetSource(value);
        }

        public bool SingleSelect 
        {
            get => _singleSelect;
            set
            {
                if (_singleSelect != value)
                {
                    if (value == true)
                    {
                        using var update = BatchUpdate();
                        var selectedIndex = SelectedIndex;
                        Clear();
                        SelectedIndex = selectedIndex;
                    }

                    _singleSelect = value;
                    RangesEnabled = !value;

                    if (RangesEnabled && _selectedIndex >= 0)
                    {
                        CommitSelect(new IndexRange(_selectedIndex));
                    }

                    RaisePropertyChanged(nameof(SingleSelect));
               }
            }
        }

        public int SelectedIndex 
        {
            get => _selectedIndex;
            set
            {
                using var update = BatchUpdate();
                Clear();
                Select(value);
            }
        }

        public IReadOnlyList<int> SelectedIndexes => _selectedIndexes ??= new SelectedIndexes<T>(this);

        [MaybeNull, AllowNull]
        public T SelectedItem
        {
            get
            {
                if (ItemsView is object)
                {
                    return GetItemAt(_selectedIndex);
                }
                else if (_initSelectedItems is object && _initSelectedItems.Count > 0)
                {
                    return (T)_initSelectedItems[0];
                }

                return default;
            }
            set
            {
                if (ItemsView is object)
                {
                    SelectedIndex = ItemsView.IndexOf(value!);
                }
                else
                {
                    Clear();
#pragma warning disable CS8601
                    SetInitSelectedItems(new T[] { value });
#pragma warning restore CS8601
                }
            }
        }

        public IReadOnlyList<T> SelectedItems
        {
            get
            {
                if (ItemsView is null && _initSelectedItems is object)
                {
                    return _initSelectedItems is IReadOnlyList<T> i ?
                        i : _initSelectedItems.Cast<T>().ToList();
                }

                return _selectedItems ??= new SelectedItems<T>(this);
            }
        }

        public int AnchorIndex 
        {
            get => _anchorIndex;
            set
            {
                using var update = BatchUpdate();
                var index = CoerceIndex(value);
                update.Operation.AnchorIndex = index;
            }
        }

        public int Count
        {
            get
            {
                if (SingleSelect)
                {
                    return _selectedIndex >= 0 ? 1 : 0;
                }
                else
                {
                    return IndexRange.GetCount(Ranges);
                }
            }
        }

        IEnumerable? ISelectionModel.Source 
        {
            get => Source;
            set => SetSource(value);
        }

        object? ISelectionModel.SelectedItem
        {
            get => SelectedItem;
            set
            {
                if (value is T t)
                {
                    SelectedItem = t;
                }
                else
                {
                    SelectedIndex = -1;
                }
            }
                
        }

        IReadOnlyList<object?> ISelectionModel.SelectedItems 
        {
            get => _selectedItemsUntyped ??= new SelectedItems<T>.Untyped(SelectedItems);
        }

        public event EventHandler<SelectionModelIndexesChangedEventArgs>? IndexesChanged;
        public event EventHandler<SelectionModelSelectionChangedEventArgs<T>>? SelectionChanged;
        public event EventHandler? LostSelection;
        public event EventHandler? SourceReset;
        public event PropertyChangedEventHandler? PropertyChanged;

        event EventHandler<SelectionModelSelectionChangedEventArgs>? ISelectionModel.SelectionChanged
        {
            add => _untypedSelectionChanged += value;
            remove => _untypedSelectionChanged -= value;
        }

        public BatchUpdateOperation BatchUpdate() => new BatchUpdateOperation(this);

        public void BeginBatchUpdate()
        {
            _operation ??= new Operation(this);
            ++_operation.UpdateCount;
        }

        public void EndBatchUpdate()
        {
            if (_operation is null || _operation.UpdateCount == 0)
            {
                throw new InvalidOperationException("No batch update in progress.");
            }

            if (--_operation.UpdateCount == 0)
            {
                // If the collection is currently changing, commit the update when the
                // collection change finishes.
                if (!IsSourceCollectionChanging)
                {
                    CommitOperation(_operation);
                }
            }
        }

        public bool IsSelected(int index)
        {
            if (index < 0)
            {
                return false;
            }
            else if (SingleSelect)
            {
                return _selectedIndex == index;
            }
            else
            {
                return IndexRange.Contains(Ranges, index);
            }
        }

        public void Select(int index) => SelectRange(index, index, false, true);

        public void Deselect(int index) => DeselectRange(index, index);

        public void SelectRange(int start, int end) => SelectRange(start, end, false, false);

        public void DeselectRange(int start, int end)
        {
            using var update = BatchUpdate();
            var o = update.Operation;
            var range = new IndexRange(Math.Max(0, start), end);

            if (RangesEnabled)
            {
                var selected = Ranges.ToList();
                var deselected = new List<IndexRange>();
                var operationDeselected = new List<IndexRange>();

                o.DeselectedRanges ??= new List<IndexRange>();
                IndexRange.Remove(o.SelectedRanges, range, operationDeselected);
                IndexRange.Remove(selected, range, deselected);
                IndexRange.Add(o.DeselectedRanges, deselected);

                if (IndexRange.Contains(deselected, o.SelectedIndex) ||
                    IndexRange.Contains(operationDeselected, o.SelectedIndex))
                {
                    o.SelectedIndex = GetFirstSelectedIndexFromRanges(except: deselected);
                }
            }
            else if(range.Contains(_selectedIndex))
            {
                o.SelectedIndex = -1;
            }

            _initSelectedItems = null;
        }

        public void SelectAll() => SelectRange(0, int.MaxValue);
        public void Clear() => DeselectRange(0, int.MaxValue);

        protected void RaisePropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private protected virtual void SetSource(IEnumerable? value)
        {
            if (base.Source != value)
            {
                if (_operation is object)
                {
                    throw new InvalidOperationException("Cannot change source while update is in progress.");
                }

                if (base.Source is object && value is object)
                {
                    using var update = BatchUpdate();
                    update.Operation.SkipLostSelection = true;
                    Clear();
                }

                base.Source = value;

                using (var update = BatchUpdate())
                {
                    update.Operation.IsSourceUpdate = true;

                    if (_initSelectedItems is object && ItemsView is object)
                    {
                        foreach (T i in _initSelectedItems)
                        {
                            Select(ItemsView.IndexOf(i));
                        }

                        _initSelectedItems = null;
                    }
                    else
                    {
                        TrimInvalidSelections(update.Operation);
                    }

                    RaisePropertyChanged(nameof(Source));
                }
            }
        }

        private protected override void OnIndexesChanged(int shiftIndex, int shiftDelta)
        {
            IndexesChanged?.Invoke(this, new SelectionModelIndexesChangedEventArgs(shiftIndex, shiftDelta));
        }

        private protected override void OnSourceReset()
        {
            _selectedIndex = _anchorIndex = -1;
            CommitDeselect(new IndexRange(0, int.MaxValue));

            if (SourceReset is object)
            {
                SourceReset.Invoke(this, EventArgs.Empty);
            }
            else
            {
                //Logger.TryGet(LogEventLevel.Warning, LogArea.Control)?.Log(
                //    this,
                //    "SelectionModel received Reset but no SourceReset handler was registered to handle it. " +
                //    "Selection may be out of sync.",
                //    typeof(SelectionModel));
            }
        }

        private protected override void OnSelectionChanged(IReadOnlyList<T> deselectedItems)
        {
            // Note: We're *not* putting this in a using scope. A collection update is still in progress
            // so the operation won't get committed by normal means: we have to commit it manually.
            var update = BatchUpdate();

            update.Operation.DeselectedItems = deselectedItems;

            if (_selectedIndex == -1 && LostSelection is object)
            {
                LostSelection(this, EventArgs.Empty);
            }

            // Don't raise PropertyChanged events here as the OnSourceCollectionChanged event that
            // let to this method being called will raise them if necessary.
            CommitOperation(update.Operation, raisePropertyChanged: false);
        }

        private protected override CollectionChangeState OnItemsAdded(int index, IList items)
        {
            var count = items.Count;
            var shifted = SelectedIndex >= index;
            var shiftCount = shifted ? count : 0;

            _selectedIndex += shiftCount;
            _anchorIndex += shiftCount;

            var baseResult = base.OnItemsAdded(index, items);
            shifted |= baseResult.ShiftDelta != 0;

            return new CollectionChangeState
            {
                ShiftIndex = index,
                ShiftDelta = shifted ? count : 0,
            };
        }

        private protected override CollectionChangeState OnItemsRemoved(int index, IList items)
        {
            var count = items.Count;
            var removedRange = new IndexRange(index, index + count - 1);
            var shifted = false;
            List<T>? removed;

            var baseResult = base.OnItemsRemoved(index, items);
            shifted |= baseResult.ShiftDelta != 0;
            removed = baseResult.RemovedItems;

            if (removedRange.Contains(SelectedIndex))
            {
                if (SingleSelect)
                {
#pragma warning disable CS8604
                    removed = new List<T> { (T)items[SelectedIndex - index] };
#pragma warning restore CS8604
                }

                _selectedIndex = GetFirstSelectedIndexFromRanges();
            }
            else if (SelectedIndex >= index)
            {
                _selectedIndex -= count;
                shifted = true;
            }

            if (removedRange.Contains(AnchorIndex))
            {
                _anchorIndex = GetFirstSelectedIndexFromRanges();
            }
            else if (AnchorIndex >= index)
            {
                _anchorIndex -= count;
                shifted = true;
            }

            return new CollectionChangeState
            {
                ShiftIndex = index,
                ShiftDelta = shifted ? -count : 0,
                RemovedItems = removed,
            };
        }

        private protected override void OnSourceCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            if (_operation?.UpdateCount > 0)
            {
                throw new InvalidOperationException("Source collection was modified during selection update.");
            }

            var oldAnchorIndex = _anchorIndex;
            var oldSelectedIndex = _selectedIndex;

            base.OnSourceCollectionChanged(e);

            if (oldSelectedIndex != _selectedIndex)
            {
                RaisePropertyChanged(nameof(SelectedIndex));
            }

            if ((e.Action == NotifyCollectionChangedAction.Remove && e.OldStartingIndex <= oldSelectedIndex) ||
                (e.Action == NotifyCollectionChangedAction.Replace && e.OldStartingIndex == oldSelectedIndex) ||
                e.Action == NotifyCollectionChangedAction.Reset)
            {
                RaisePropertyChanged(nameof(SelectedItem));
            }

            if (oldAnchorIndex != _anchorIndex)
            {
                RaisePropertyChanged(nameof(AnchorIndex));
            }
        }

        private protected override bool IsValidCollectionChange(NotifyCollectionChangedEventArgs e)
        {
            if (!base.IsValidCollectionChange(e))
            {
                return false;
            }

            if (ItemsView is object && e.Action == NotifyCollectionChangedAction.Add)
            {
                if (e.NewStartingIndex <= _selectedIndex)
                {
                    return _selectedIndex + e.NewItems.Count < ItemsView.Count;
                }

                if (e.NewStartingIndex <= _anchorIndex)
                {
                    return _anchorIndex + e.NewItems.Count < ItemsView.Count;
                }
            }

            return true;
        }

        private protected void SetInitSelectedItems(IList items)
        {
            if (Source is object)
            {
                throw new InvalidOperationException("Cannot set init selected items when Source is set.");
            }

            _initSelectedItems = items;
        }

        protected override void OnSourceCollectionChangeFinished()
        {
            if (_operation is object)
            {
                CommitOperation(_operation);
            }
        }

        private int GetFirstSelectedIndexFromRanges(List<IndexRange>? except = null)
        {
            if (RangesEnabled)
            {
                var count = IndexRange.GetCount(Ranges);
                var index = 0;

                while (index < count)
                {
                    var result = IndexRange.GetAt(Ranges, index++);

                    if (!IndexRange.Contains(except, result))
                    {
                        return result;
                    }
                }
            }

            return -1;
        }

        private void SelectRange(
            int start,
            int end,
            bool forceSelectedIndex,
            bool forceAnchorIndex)
        {
            if (SingleSelect && start != end)
            {
                throw new InvalidOperationException("Cannot select range with single selection.");
            }

            var range = CoerceRange(start, end);

            if (range.Begin == -1)
            {
                return;
            }

            using var update = BatchUpdate();
            var o = update.Operation;
            var selected = new List<IndexRange>();

            if (RangesEnabled)
            {
                o.SelectedRanges ??= new List<IndexRange>();
                IndexRange.Remove(o.DeselectedRanges, range);
                IndexRange.Add(o.SelectedRanges, range);
                IndexRange.Remove(o.SelectedRanges, Ranges);

                if (o.SelectedIndex == -1 || forceSelectedIndex)
                {
                    o.SelectedIndex = range.Begin;
                }

                if (o.AnchorIndex == -1 || forceAnchorIndex)
                {
                    o.AnchorIndex = range.Begin;
                }
            }
            else
            {
                o.SelectedIndex = o.AnchorIndex = start;
            }

            _initSelectedItems = null;
        }

        [return: MaybeNull]
        private T GetItemAt(int index)
        {
            if (ItemsView is null || index < 0 || index >= ItemsView.Count)
            {
                return default;
            }

            return ItemsView[index];
        }

        private int CoerceIndex(int index)
        {
            index = Math.Max(index, -1);

            if (ItemsView is object && index >= ItemsView.Count)
            {
                index = -1;
            }

            return index;
        }

        private IndexRange CoerceRange(int start, int end)
        {
            var max = ItemsView is object ? ItemsView.Count - 1 : int.MaxValue;

            if (start > max || (start < 0 && end < 0))
            {
                return new IndexRange(-1);
            }

            start = Math.Max(start, 0);
            end = Math.Min(end, max);

            return new IndexRange(start, end);
        }

        private void TrimInvalidSelections(Operation operation)
        {
            if (ItemsView is null)
            {
                return;
            }

            var max = ItemsView.Count - 1;

            if (operation.SelectedIndex > max)
            {
                operation.SelectedIndex = GetFirstSelectedIndexFromRanges();
            }

            if (operation.AnchorIndex > max)
            {
                operation.AnchorIndex = GetFirstSelectedIndexFromRanges();
            }

            if (RangesEnabled && Ranges.Count > 0)
            {
                var selected = Ranges.ToList();
                
                if (max < 0)
                {
                    operation.DeselectedRanges = selected;
                }
                else
                {
                    var valid = new IndexRange(0, max);
                    var removed = new List<IndexRange>();
                    IndexRange.Intersect(selected, valid, removed);
                    operation.DeselectedRanges = removed;
                }
            }
        }

        private void CommitOperation(Operation operation, bool raisePropertyChanged = true)
        {
            try
            {
                var oldAnchorIndex = _anchorIndex;
                var oldSelectedIndex = _selectedIndex;
                var indexesChanged = false;

                if (operation.SelectedIndex == -1 && LostSelection is object && !operation.SkipLostSelection)
                {
                    operation.UpdateCount++;
                    LostSelection?.Invoke(this, EventArgs.Empty);
                }

                _selectedIndex = operation.SelectedIndex;
                _anchorIndex = operation.AnchorIndex;

                if (operation.SelectedRanges is object)
                {
                    indexesChanged |= CommitSelect(operation.SelectedRanges) > 0;
                }

                if (operation.DeselectedRanges is object)
                {
                    indexesChanged |= CommitDeselect(operation.DeselectedRanges) > 0;
                }

                if (SelectionChanged is object || _untypedSelectionChanged is object)
                {
                    IReadOnlyList<IndexRange>? deselected = operation.DeselectedRanges;
                    IReadOnlyList<IndexRange>? selected = operation.SelectedRanges;

                    if (SingleSelect && oldSelectedIndex != _selectedIndex)
                    {
                        if (oldSelectedIndex != -1)
                        {
                            deselected = new[] { new IndexRange(oldSelectedIndex) };
                        }

                        if (_selectedIndex != -1)
                        {
                            selected = new[] { new IndexRange(_selectedIndex) };
                        }
                    }

                    if (deselected?.Count > 0 || selected?.Count > 0 || operation.DeselectedItems is object)
                    {
                        // If the operation was caused by Source being updated, then use a null source
                        // so that the items will appear as nulls.
                        var deselectedSource = operation.IsSourceUpdate ? null : ItemsView;

                        // If the operation contains DeselectedItems then we're notifying a source
                        // CollectionChanged event. LostFocus may have caused another item to have been
                        // selected, but it can't have caused a deselection (as it was called due to
                        // selection being lost) so we're ok to discard `deselected` here.
                        var deselectedItems = operation.DeselectedItems ??
                            SelectedItems<T>.Create(deselected, deselectedSource);

                        var e = new SelectionModelSelectionChangedEventArgs<T>(
                            SelectedIndexes<T>.Create(deselected),
                            SelectedIndexes<T>.Create(selected),
                            deselectedItems,
                            SelectedItems<T>.Create(selected, ItemsView));
                        SelectionChanged?.Invoke(this, e);
                        _untypedSelectionChanged?.Invoke(this, e);
                    }
                }

                if (raisePropertyChanged)
                {
                    if (oldSelectedIndex != _selectedIndex)
                    {
                        indexesChanged = true;
                        RaisePropertyChanged(nameof(SelectedIndex));
                    }

                    if (oldSelectedIndex != _selectedIndex || operation.IsSourceUpdate)
                    {
                        RaisePropertyChanged(nameof(SelectedItem));
                    }

                    if (oldAnchorIndex != _anchorIndex)
                    {
                        indexesChanged = true;
                        RaisePropertyChanged(nameof(AnchorIndex));
                    }

                    if (indexesChanged)
                    {
                        RaisePropertyChanged(nameof(SelectedIndexes));
                    }

                    if (indexesChanged || operation.IsSourceUpdate)
                    {
                        RaisePropertyChanged(nameof(SelectedItems));
                    }
                }
            }
            finally
            {
                _operation = null;
            }
        }

        public struct BatchUpdateOperation : IDisposable
        {
            private readonly SelectionModel<T> _owner;
            private bool _isDisposed;

            public BatchUpdateOperation(SelectionModel<T> owner)
            {
                _owner = owner;
                _isDisposed = false;
                owner.BeginBatchUpdate();
            }

            internal Operation Operation => _owner._operation!;

            public void Dispose()
            {
                if (!_isDisposed)
                {
                    _owner?.EndBatchUpdate();
                    _isDisposed = true;
                }
            }
        }

        internal class Operation
        {
            public Operation(SelectionModel<T> owner)
            {
                AnchorIndex = owner.AnchorIndex;
                SelectedIndex = owner.SelectedIndex;
            }

            public int UpdateCount { get; set; }
            public bool IsSourceUpdate { get; set; }
            public bool SkipLostSelection { get; set; }
            public int AnchorIndex { get; set; }
            public int SelectedIndex { get; set; }
            public List<IndexRange>? SelectedRanges { get; set; }
            public List<IndexRange>? DeselectedRanges { get; set; }
            public IReadOnlyList<T>? DeselectedItems { get; set; }
        }
    }
}
