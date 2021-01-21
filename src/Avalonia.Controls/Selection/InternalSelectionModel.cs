using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Avalonia.Collections;

#nullable enable

namespace Avalonia.Controls.Selection
{
    internal class InternalSelectionModel : SelectionModel<object?>
    {
        private IList? _writableSelectedItems;
        private bool _ignoreModelChanges;
        private bool _ignoreSelectedItemsChanges;

        public InternalSelectionModel()
        {
            SelectionChanged += OnSelectionChanged;
            SourceReset += OnSourceReset;
        }

        [AllowNull]
        public IList WritableSelectedItems
        {
            get
            {
                if (_writableSelectedItems is null)
                {
                    _writableSelectedItems = new AvaloniaList<object?>();
                    SubscribeToSelectedItems();
                }

                return _writableSelectedItems;
            }
            set
            {
                value ??= new AvaloniaList<object?>();

                if (value.IsFixedSize)
                {
                    throw new NotSupportedException("Cannot assign fixed size selection to SelectedItems.");
                }

                if (_writableSelectedItems != value)
                {
                    UnsubscribeFromSelectedItems();
                    _writableSelectedItems = value;
                    SyncFromSelectedItems(_writableSelectedItems);
                    SubscribeToSelectedItems();
                    
                    if (ItemsView is null)
                    {
                        SetInitSelectedItems(value);
                    }

                    RaisePropertyChanged(nameof(WritableSelectedItems));
                }
            }
        }

        private protected override void SetSource(IEnumerable? value)
        {
            if (Source == value)
            {
                return;
            }

            object?[]? oldSelection = null;

            if (Source is object && value is object)
            {
                oldSelection = new object?[WritableSelectedItems.Count];
                WritableSelectedItems.CopyTo(oldSelection, 0);
            }

            try
            {
                _ignoreSelectedItemsChanges = true;
                base.SetSource(value);
            }
            finally
            {
                _ignoreSelectedItemsChanges = false;
            }

            if (oldSelection is null)
            {
                SyncToSelectedItems();
            }
            else
            {
                SyncFromSelectedItems(oldSelection);
            }
        }

        private void SyncToSelectedItems()
        {
            if (_writableSelectedItems is object)
            {
                try
                {
                    _ignoreSelectedItemsChanges = true;
                    _writableSelectedItems.Clear();

                    foreach (var i in base.SelectedItems)
                    {
                        _writableSelectedItems.Add(i);
                    }
                }
                finally
                {
                    _ignoreSelectedItemsChanges = false;
                }
            }
        }

        private void SyncFromSelectedItems(IList? selectedItems)
        {
            if (Source is null || selectedItems is null)
            {
                return;
            }

            try
            {
                _ignoreModelChanges = true;

                using (BatchUpdate())
                {
                    Clear();
                    Add(selectedItems);
                }
            }
            finally
            {
                _ignoreModelChanges = false;
            }
        }

        private void SubscribeToSelectedItems()
        {
            if (_writableSelectedItems is INotifyCollectionChanged incc)
            {
                incc.CollectionChanged += OnSelectedItemsCollectionChanged;
            }
        }

        private void UnsubscribeFromSelectedItems()
        {
            if (_writableSelectedItems is INotifyCollectionChanged incc)
            {
                incc.CollectionChanged += OnSelectedItemsCollectionChanged;
            }
        }

        private void OnSelectionChanged(object sender, SelectionModelSelectionChangedEventArgs e)
        {
            if (_ignoreModelChanges)
            {
                return;
            }

            try
            {
                var items = WritableSelectedItems;
                var deselected = e.DeselectedItems.ToList();
                var selected = e.SelectedItems.ToList();

                _ignoreSelectedItemsChanges = true;

                foreach (var i in deselected)
                {
                    items.Remove(i);
                }

                foreach (var i in selected)
                {
                    items.Add(i);
                }
            }
            finally
            {
                _ignoreSelectedItemsChanges = false;
            }
        }

        private void OnSourceReset(object sender, EventArgs e) => SyncFromSelectedItems(_writableSelectedItems);

        private void OnSelectedItemsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (_ignoreSelectedItemsChanges)
            {
                return;
            }

            if (_writableSelectedItems == null)
            {
                throw new AvaloniaInternalException("CollectionChanged raised but we don't have items.");
            }

            void Remove()
            {
                foreach (var i in e.OldItems)
                {
                    var index = IndexOf(Source, i);

                    if (index != -1)
                    {
                        Deselect(index);
                    }
                }
            }

            try
            {
                using var operation = BatchUpdate();

                _ignoreModelChanges = true;

                switch (e.Action)
                {
                    case NotifyCollectionChangedAction.Add:
                        Add(e.NewItems);
                        break;
                    case NotifyCollectionChangedAction.Remove:
                        Remove();
                        break;
                    case NotifyCollectionChangedAction.Replace:
                        Remove();
                        Add(e.NewItems);
                        break;
                    case NotifyCollectionChangedAction.Reset:
                        Clear();
                        Add(_writableSelectedItems);
                        break;
                }
            }
            finally
            {
                _ignoreModelChanges = false;
            }
        }

        private void Add(IList newItems)
        {
            foreach (var i in newItems)
            {
                var index = IndexOf(Source, i);

                if (index != -1)
                {
                    Select(index);
                }
            }
        }

        private static int IndexOf(object? source, object? item)
        {
            if (source is IList l)
            {
                return l.IndexOf(item);
            }
            else if (source is ItemsSourceView v)
            {
                return v.IndexOf(item);
            }

            return -1;
        }
    }
}
