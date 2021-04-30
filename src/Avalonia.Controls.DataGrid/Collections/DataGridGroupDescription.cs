// (c) Copyright Microsoft Corporation.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using Avalonia.Controls;
using Avalonia.Controls.Utils;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Utilities;

namespace Avalonia.Collections
{
    public abstract class DataGridGroupDescription : INotifyPropertyChanged
    {
        public AvaloniaList<object> GroupKeys { get; }

        public DataGridGroupDescription()
        {
            GroupKeys = new AvaloniaList<object>();
            GroupKeys.CollectionChanged += (sender, e) => OnPropertyChanged(new PropertyChangedEventArgs(nameof(GroupKeys)));
        }

        protected virtual event PropertyChangedEventHandler PropertyChanged;
        event PropertyChangedEventHandler INotifyPropertyChanged.PropertyChanged
        {
            add
            {
                PropertyChanged += value;
            }

            remove
            {
                PropertyChanged -= value;
            }
        }
        protected virtual void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            PropertyChanged?.Invoke(this, e);
        }

        public virtual string PropertyName => String.Empty;
        public abstract object GroupKeyFromItem(object item, int level, CultureInfo culture);
        public virtual bool KeysMatch(object groupKey, object itemKey)
        {
            return object.Equals(groupKey, itemKey);
        }
    }
    public class DataGridPathGroupDescription : DataGridGroupDescription
    {
        private string _propertyPath;
        private Type _propertyType;
        private StringComparison _stringComparison = StringComparison.Ordinal;

        public DataGridPathGroupDescription(string propertyPath)
        {
            _propertyPath = propertyPath;
        }

        public override object GroupKeyFromItem(object item, int level, CultureInfo culture)
        {
            object GetKey(object o)
            {
                if(o == null)
                    return null;

                if (_propertyType == null)
                    _propertyType = GetPropertyType(o);

                return InvokePath(o, _propertyPath, _propertyType);
            }

            var key = GetKey(item);
            if (key == null)
                key = item;

            if (ValueConverter != null)
                key = ValueConverter.Convert(key, typeof(object), level, culture);

            return key;
        }
        public override bool KeysMatch(object groupKey, object itemKey)
        {
            if(groupKey is string k1 && itemKey is string k2)
            {
                return String.Equals(k1, k2, _stringComparison);
            }
            else
                return base.KeysMatch(groupKey, itemKey);
        }
        public override string PropertyName => _propertyPath;

        public IValueConverter ValueConverter { get; set; }

        private Type GetPropertyType(object o)
        {
            return o.GetType().GetNestedPropertyType(_propertyPath);
        }
        private static object InvokePath(object item, string propertyPath, Type propertyType)
        {
            object propertyValue = TypeHelper.GetNestedPropertyValue(item, propertyPath, propertyType, out Exception exception);
            if (exception != null)
            {
                throw exception;
            }
            return propertyValue;
        }
    }

    public abstract class DataGridCollectionViewGroup : INotifyPropertyChanged
    {
        private int _itemCount;

        public object Key { get; }
        public int ItemCount => _itemCount;
        public IAvaloniaReadOnlyList<object> Items => ProtectedItems;

        protected AvaloniaList<object> ProtectedItems { get; }
        protected int ProtectedItemCount
        {
            get { return _itemCount; }
            set
            {
                _itemCount = value;
                OnPropertyChanged(new PropertyChangedEventArgs(nameof(ItemCount)));
            }
        }

        protected DataGridCollectionViewGroup(object key)
        {
            Key = key;
            ProtectedItems = new AvaloniaList<object>();
        }

        public abstract bool IsBottomLevel { get; }

        protected virtual event PropertyChangedEventHandler PropertyChanged;
        event PropertyChangedEventHandler INotifyPropertyChanged.PropertyChanged
        {
            add
            {
                PropertyChanged += value;
            }

            remove
            {
                PropertyChanged -= value;
            }
        }
        protected virtual void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            PropertyChanged?.Invoke(this, e);
        }
    }
    internal class DataGridCollectionViewGroupInternal : DataGridCollectionViewGroup
    {
        /// <summary>
        /// GroupDescription used to define how to group the items
        /// </summary>
        private DataGridGroupDescription _groupBy;

        /// <summary>
        /// Parent group of this CollectionViewGroupInternal
        /// </summary>
        private readonly DataGridCollectionViewGroupInternal _parentGroup;

        /// <summary>
        /// Used for detecting stale enumerators
        /// </summary>
        private int _version;

        public DataGridCollectionViewGroupInternal(object key, DataGridCollectionViewGroupInternal parent)
            : base(key)
        {
            _parentGroup = parent;
        }

        public override bool IsBottomLevel => _groupBy == null;

        internal int FullCount { get; set; }

        internal DataGridGroupDescription GroupBy
        {
            get { return _groupBy; }
            set
            {
                bool oldIsBottomLevel = IsBottomLevel;

                if (_groupBy != null)
                {
                    ((INotifyPropertyChanged)_groupBy).PropertyChanged -= OnGroupByChanged;
                }

                _groupBy = value;

                if (_groupBy != null)
                {
                    ((INotifyPropertyChanged)_groupBy).PropertyChanged += OnGroupByChanged;
                }

                if (oldIsBottomLevel != IsBottomLevel)
                {
                    OnPropertyChanged(new PropertyChangedEventArgs(nameof(IsBottomLevel)));
                }
            }
        }

        private void OnGroupByChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            OnGroupByChanged();
        }
        protected virtual void OnGroupByChanged()
        {
            _parentGroup?.OnGroupByChanged();
        }

        /// <summary>
        /// Gets or sets the most recent index where activity took place
        /// </summary>
        internal int LastIndex { get; set; }

        /// <summary>
        /// Gets the first item (leaf) added to this group.  If this can't be determined,
        /// DependencyProperty.UnsetValue.
        /// </summary>
        internal object SeedItem
        {
            get
            {
                if (ItemCount > 0 && (GroupBy == null || GroupBy.GroupKeys.Count == 0))
                {
                    // look for first item, child by child
                    for (int k = 0, n = Items.Count; k < n; ++k)
                    {
                        if (!(Items[k] is DataGridCollectionViewGroupInternal subgroup))
                        {
                            // child is an item - return it
                            return Items[k];
                        }
                        else if (subgroup.ItemCount > 0)
                        {
                            // child is a nonempty subgroup - ask it
                            return subgroup.SeedItem;
                        }
                        //// otherwise child is an empty subgroup - go to next child
                    }

                    // we shouldn't get here, but just in case...

                    return AvaloniaProperty.UnsetValue;
                }
                else
                {
                    // the group is empty, or it has explicit subgroups.
                    // In either case, we cannot determine the first item -
                    // it could have gone into any of the subgroups.
                    return AvaloniaProperty.UnsetValue;
                }
            }
        }

        private DataGridCollectionViewGroupInternal Parent => _parentGroup;

        /// <summary>
        /// Adds the specified item to the collection
        /// </summary>
        /// <param name="item">Item to add</param>
        internal void Add(object item)
        {
            ChangeCounts(item, +1);
            ProtectedItems.Add(item);
        }

        /// <summary>
        /// Clears the collection of items
        /// </summary>
        internal void Clear()
        {
            ProtectedItems.Clear();
            FullCount = 1;
            ProtectedItemCount = 0;
        }

        /// <summary>
        /// Finds the index of the specified item
        /// </summary>
        /// <param name="item">Item we are looking for</param>
        /// <param name="seed">Seed of the item we are looking for</param>
        /// <param name="comparer">Comparer used to find the item</param>
        /// <param name="low">Low range of item index</param>
        /// <param name="high">High range of item index</param>
        /// <returns>Index of the specified item</returns>
        protected virtual int FindIndex(object item, object seed, IComparer comparer, int low, int high)
        {
            int index;

            if (comparer != null)
            {
                if (comparer is ListComparer listComparer)
                {
                    // reset the IListComparer before each search. This cannot be done
                    // any less frequently (e.g. in Root.AddToSubgroups), due to the
                    // possibility that the item may appear in more than one subgroup.
                    listComparer.Reset();
                }

                if (comparer is CollectionViewGroupComparer groupComparer)
                {
                    // reset the CollectionViewGroupComparer before each search. This cannot be done
                    // any less frequently (e.g. in Root.AddToSubgroups), due to the
                    // possibility that the item may appear in more than one subgroup.
                    groupComparer.Reset();
                }

                for (index = low; index < high; ++index)
                {
                    object seed1 = (ProtectedItems[index] is DataGridCollectionViewGroupInternal subgroup) ? subgroup.SeedItem : ProtectedItems[index];
                    if (seed1 == AvaloniaProperty.UnsetValue)
                    {
                        continue;
                    }
                    if (comparer.Compare(seed, seed1) < 0)
                    {
                        break;
                    }
                }
            }
            else
            {
                index = high;
            }

            return index;
        }

        /// <summary>
        /// Returns an enumerator over the leaves governed by this group
        /// </summary>
        /// <returns>Enumerator of leaves</returns>
        internal IEnumerator GetLeafEnumerator()
        {
            return new LeafEnumerator(this);
        }

        /// <summary>
        /// Insert a new item or subgroup and return its index.  Seed is a
        /// representative from the subgroup (or the item itself) that
        /// is used to position the new item/subgroup w.r.t. the order given
        /// by the comparer. (If comparer is null, just add at the end).
        /// </summary>
        /// <param name="item">Item we are looking for</param>
        /// <param name="seed">Seed of the item we are looking for</param>
        /// <param name="comparer">Comparer used to find the item</param>
        /// <returns>The index where the item was inserted</returns>
        internal int Insert(object item, object seed, IComparer comparer)
        {
            // never insert the new item/group before the explicit subgroups
            int low = (GroupBy == null) ? 0 : GroupBy.GroupKeys.Count;
            int index = FindIndex(item, seed, comparer, low, ProtectedItems.Count);

            // now insert the item
            ChangeCounts(item, +1);
            ProtectedItems.Insert(index, item);

            return index;
        }

        /// <summary>
        /// Return the item at the given index within the list of leaves governed
        /// by this group
        /// </summary>
        /// <param name="index">Index of the leaf</param>
        /// <returns>Item at given index</returns>
        internal object LeafAt(int index)
        {
            for (int k = 0, n = Items.Count; k < n; ++k)
            {
                if (Items[k] is DataGridCollectionViewGroupInternal subgroup)
                {
                    // current item is a group - either drill in, or skip over
                    if (index < subgroup.ItemCount)
                    {
                        return subgroup.LeafAt(index);
                    }
                    else
                    {
                        index -= subgroup.ItemCount;
                    }
                }
                else
                {
                    // current item is a leaf - see if we're done
                    if (index == 0)
                    {
                        return Items[k];
                    }
                    else
                    {
                        index -= 1;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Returns the index of the given item within the list of leaves governed
        /// by the full group structure.  The item must be a (direct) child of this
        /// group.  The caller provides the index of the item within this group,
        /// if known, or -1 if not.
        /// </summary>
        /// <param name="item">Item we are looking for</param>
        /// <param name="index">Index of the leaf</param>
        /// <returns>Number of items under that leaf</returns>
        internal int LeafIndexFromItem(object item, int index)
        {
            int result = 0;

            // accumulate the number of predecessors at each level
            for (DataGridCollectionViewGroupInternal group = this;
                    group != null;
                    item = group, group = group.Parent, index = -1)
            {
                // accumulate the number of predecessors at the level of item
                for (int k = 0, n = group.Items.Count; k < n; ++k)
                {
                    // if we've reached the item, move up to the next level
                    if ((index < 0 && Object.Equals(item, group.Items[k])) ||
                        index == k)
                    {
                        break;
                    }

                    // accumulate leaf count
                    DataGridCollectionViewGroupInternal subgroup = group.Items[k] as DataGridCollectionViewGroupInternal;
                    result += subgroup?.ItemCount ?? 1;
                }
            }

            return result;
        }

        /// <summary>
        /// Returns the index of the given item within the list of leaves governed
        /// by this group
        /// </summary>
        /// <param name="item">Item we are looking for</param>
        /// <returns>Number of items under that leaf</returns>
        internal int LeafIndexOf(object item)
        {
            int leaves = 0;         // number of leaves we've passed over so far
            for (int k = 0, n = Items.Count; k < n; ++k)
            {
                if (Items[k] is DataGridCollectionViewGroupInternal subgroup)
                {
                    int subgroupIndex = subgroup.LeafIndexOf(item);
                    if (subgroupIndex < 0)
                    {
                        leaves += subgroup.ItemCount;       // item not in this subgroup
                    }
                    else
                    {
                        return leaves + subgroupIndex;    // item is in this subgroup
                    }
                }
                else
                {
                    // current item is a leaf - compare it directly
                    if (Object.Equals(item, Items[k]))
                    {
                        return leaves;
                    }
                    else
                    {
                        leaves += 1;
                    }
                }
            }

            // item not found
            return -1;
        }

        /// <summary>
        /// Removes the specified item from the collection
        /// </summary>
        /// <param name="item">Item to remove</param>
        /// <param name="returnLeafIndex">Whether we want to return the leaf index</param>
        /// <returns>Leaf index where item was removed, if value was specified. Otherwise '-1'</returns>
        internal int Remove(object item, bool returnLeafIndex)
        {
            int index = -1;
            int localIndex = ProtectedItems.IndexOf(item);

            if (localIndex >= 0)
            {
                if (returnLeafIndex)
                {
                    index = LeafIndexFromItem(null, localIndex);
                }

                ChangeCounts(item, -1);
                ProtectedItems.RemoveAt(localIndex);
            }

            return index;
        }

        /// <summary>
        /// Removes an empty group from the PagedCollectionView grouping
        /// </summary>
        /// <param name="group">Empty subgroup to remove</param>
        private static void RemoveEmptyGroup(DataGridCollectionViewGroupInternal group)
        {
            DataGridCollectionViewGroupInternal parent = group.Parent;

            if (parent != null)
            {
                DataGridGroupDescription groupBy = parent.GroupBy;
                int index = parent.ProtectedItems.IndexOf(group);

                // remove the subgroup unless it is one of the explicit groups
                if (index >= groupBy.GroupKeys.Count)
                {
                    parent.Remove(group, false);
                }
            }
        }

        /// <summary>
        /// Update the item count of the CollectionViewGroup
        /// </summary>
        /// <param name="item">CollectionViewGroup to update</param>
        /// <param name="delta">Delta to change count by</param>
        protected void ChangeCounts(object item, int delta)
        {
            bool changeLeafCount = !(item is DataGridCollectionViewGroup);

            for (DataGridCollectionViewGroupInternal group = this;
                    group != null;
                    group = group._parentGroup)
            {
                group.FullCount += delta;
                if (changeLeafCount)
                {
                    group.ProtectedItemCount += delta;

                    if (group.ProtectedItemCount == 0)
                    {
                        RemoveEmptyGroup(group);
                    }
                }
            }

            unchecked
            {
                // this invalidates enumerators
                ++_version;
            }
        }

        /// <summary>
        /// Enumerator for the leaves in the CollectionViewGroupInternal class.
        /// </summary>
        private class LeafEnumerator : IEnumerator
        {
            private object _current;   // current item
            private DataGridCollectionViewGroupInternal _group; // parent group
            private int _index;     // current index into Items
            private IEnumerator _subEnum;   // enumerator over current subgroup
            private int _version;   // parent group's version at ctor

            /// <summary>
            /// Initializes a new instance of the LeafEnumerator class.
            /// </summary>
            /// <param name="group">CollectionViewGroupInternal that uses the enumerator</param>
            public LeafEnumerator(DataGridCollectionViewGroupInternal group)
            {
                _group = group;
                DoReset();  // don't call virtual Reset in ctor
            }

            /// <summary>
            /// Private helper to reset the enumerator
            /// </summary>
            private void DoReset()
            {
                Debug.Assert(_group != null, "_group should have been initialized in constructor");
                _version = _group._version;
                _index = -1;
                _subEnum = null;
            }

            /// <summary>
            /// Reset implementation for IEnumerator
            /// </summary>
            void IEnumerator.Reset()
            {
                DoReset();
            }

            /// <summary>
            /// MoveNext implementation for IEnumerator
            /// </summary>
            /// <returns>Returns whether the MoveNext operation was successful</returns>
            bool IEnumerator.MoveNext()
            {
                Debug.Assert(_group != null, "_group should have been initialized in constructor");

                // check for invalidated enumerator
                if (_group._version != _version)
                {
                    throw new InvalidOperationException();
                }

                // move forward to the next leaf
                while (_subEnum == null || !_subEnum.MoveNext())
                {
                    // done with the current top-level item.  Move to the next one.
                    ++_index;
                    if (_index >= _group.Items.Count)
                    {
                        return false;
                    }

                    DataGridCollectionViewGroupInternal subgroup = _group.Items[_index] as DataGridCollectionViewGroupInternal;
                    if (subgroup == null)
                    {
                        // current item is a leaf - it's the new Current
                        _current = _group.Items[_index];
                        _subEnum = null;
                        return true;
                    }
                    else
                    {
                        // current item is a subgroup - get its enumerator
                        _subEnum = subgroup.GetLeafEnumerator();
                    }
                }

                // the loop terminates only when we have a subgroup enumerator
                // positioned at the new Current item
                _current = _subEnum.Current;
                return true;
            }

            /// <summary>
            /// Gets the current implementation for IEnumerator
            /// </summary>
            object IEnumerator.Current
            {
                get
                {
                    Debug.Assert(_group != null, "_group should have been initialized in constructor");

                    if (_index < 0 || _index >= _group.Items.Count)
                    {
                        throw new InvalidOperationException();
                    }

                    return _current;
                }
            }

        }

        // / <summary>
        // / This comparer is used to insert an item into a group in a position consistent
        // / with a given IList.  It only works when used in the pattern that FindIndex
        // / uses, namely first call Reset(), then call Compare(item, itemSequence) any number of
        // / times with the same item (the new item) as the first argument, and a sequence
        // / of items as the second argument that appear in the IList in the same sequence.
        // / This makes the total search time linear in the size of the IList.  (To give
        // / the correct answer regardless of the sequence of arguments would involve
        // / calling IndexOf and leads to O(N^2) total search time.) 
        // / </summary>
        internal class ListComparer : IComparer
        {
            /// <summary>
            /// Constructor for the ListComparer that takes
            /// in an IList.
            /// </summary>
            /// <param name="list">IList used to compare on</param>
            internal ListComparer(IList list)
            {
                ResetList(list);
            }

            /// <summary>
            /// Sets the index that we start comparing
            /// from to 0.
            /// </summary>
            internal void Reset()
            {
                _index = 0;
            }

            /// <summary>
            /// Sets our IList to a new instance
            /// of a list being passed in and resets
            /// the index.
            /// </summary>
            /// <param name="list">IList used to compare on</param>
            internal void ResetList(IList list)
            {
                _list = list;
                _index = 0;
            }

            /// <summary>
            /// Compares objects x and y to see which one
            /// should appear first.
            /// </summary>
            /// <param name="x">The first object</param>
            /// <param name="y">The second object</param>
            /// <returns>-1 if x is less than y, +1 otherwise</returns>
            public int Compare(object x, object y)
            {
                if (Object.Equals(x, y))
                {
                    return 0;
                }

                // advance the index until seeing one x or y
                int n = (_list != null) ? _list.Count : 0;
                for (; _index < n; ++_index)
                {
                    object z = _list[_index];
                    if (Object.Equals(x, z))
                    {
                        return -1;  // x occurs first, so x < y
                    }
                    else if (Object.Equals(y, z))
                    {
                        return +1;  // y occurs first, so x > y
                    }
                }

                // if we don't see either x or y, declare x > y.
                // This has the effect of putting x at the end of the list.
                return +1;
            }

            private int _index;
            private IList _list;
        }

        // / <summary>
        // / This comparer is used to insert an item into a group in a position consistent
        // / with a given CollectionViewGroupRoot. We will only use this when dealing with
        // / a temporary CollectionViewGroupRoot that points to the correct grouping of the
        // / entire collection, and we have paging that requires us to keep the paged group
        // / consistent with the order of items in the temporary group.
        // / </summary>
        internal class CollectionViewGroupComparer : IComparer
        {
            /// <summary>
            /// Constructor for the CollectionViewGroupComparer that takes
            /// in an CollectionViewGroupRoot.
            /// </summary>
            /// <param name="group">CollectionViewGroupRoot used to compare on</param>
            internal CollectionViewGroupComparer(CollectionViewGroupRoot group)
            {
                ResetGroup(group);
            }

            /// <summary>
            /// Sets the index that we start comparing
            /// from to 0.
            /// </summary>
            internal void Reset()
            {
                _index = 0;
            }

            /// <summary>
            /// Sets our group to a new instance of a
            /// CollectionViewGroupRoot being passed in
            /// and resets the index.
            /// </summary>
            /// <param name="group">CollectionViewGroupRoot used to compare on</param>
            internal void ResetGroup(CollectionViewGroupRoot group)
            {
                _group = group;
                _index = 0;
            }

            /// <summary>
            /// Compares objects x and y to see which one
            /// should appear first.
            /// </summary>
            /// <param name="x">The first object</param>
            /// <param name="y">The second object</param>
            /// <returns>-1 if x is less than y, +1 otherwise</returns>
            public int Compare(object x, object y)
            {
                if (Object.Equals(x, y))
                {
                    return 0;
                }

                // advance the index until seeing one x or y
                int n = (_group != null) ? _group.ItemCount : 0;
                for (; _index < n; ++_index)
                {
                    object z = _group.LeafAt(_index);
                    if (Object.Equals(x, z))
                    {
                        return -1;  // x occurs first, so x < y
                    }
                    else if (Object.Equals(y, z))
                    {
                        return +1;  // y occurs first, so x > y
                    }
                }

                // if we don't see either x or y, declare x > y.
                // This has the effect of putting x at the end of the list.
                return +1;
            }

            private int _index;
            private CollectionViewGroupRoot _group;
        }

    }

    internal class CollectionViewGroupRoot : DataGridCollectionViewGroupInternal, INotifyCollectionChanged
    {
        /// <summary>
        /// String constant used for the Root Name
        /// </summary>
        private const string RootName = "Root";

        /// <summary>
        /// Private accessor for empty object instance
        /// </summary>
        private static readonly object UseAsItemDirectly = new object();

        /// <summary>
        /// Private accessor for the top level GroupDescription
        /// </summary>
        private static DataGridGroupDescription topLevelGroupDescription;

        /// <summary>
        /// Private accessor for an ObservableCollection containing group descriptions
        /// </summary>
        private readonly AvaloniaList<DataGridGroupDescription> _groupBy = new AvaloniaList<DataGridGroupDescription>();

        /// <summary>
        /// Indicates whether the list of items (after applying the sort and filters, if any) 
        /// is already in the correct order for grouping.
        /// </summary>
        private bool _isDataInGroupOrder;

        /// <summary>
        /// Private accessor for the owning ICollectionView
        /// </summary>
        private readonly IDataGridCollectionView _view;

        /// <summary>
        /// Raise this event when the (grouped) view changes
        /// </summary>
        public event NotifyCollectionChangedEventHandler CollectionChanged;

        /// <summary>
        /// Raise this event when the GroupDescriptions change
        /// </summary>
        internal event EventHandler GroupDescriptionChanged;

        /// <summary>
        /// Initializes a new instance of the CollectionViewGroupRoot class.
        /// </summary>
        /// <param name="view">CollectionView that contains this grouping</param>
        /// <param name="isDataInGroupOrder">True if items are already in correct order for grouping</param>
        internal CollectionViewGroupRoot(IDataGridCollectionView view, bool isDataInGroupOrder)
            : base(RootName, null)
        {
            _view = view;
            _isDataInGroupOrder = isDataInGroupOrder;
        }

        /// <summary>
        /// Gets the description of grouping, indexed by level.
        /// </summary>
        public virtual AvaloniaList<DataGridGroupDescription> GroupDescriptions => _groupBy;

        /// <summary>
        /// Gets or sets the current IComparer being used
        /// </summary>
        internal IComparer ActiveComparer { get; set; }

        /// <summary>
        /// Gets the culture to use during sorting.
        /// </summary>
        internal CultureInfo Culture
        {
            get
            {
                Debug.Assert(_view != null, "this._view should have been set from the constructor");
                return _view.Culture;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the data is in group order
        /// </summary>
        internal bool IsDataInGroupOrder
        {
            get { return _isDataInGroupOrder; }
            set { _isDataInGroupOrder = value; }
        }

        /// <summary>
        /// Finds the index of the specified item
        /// </summary>
        /// <param name="item">Item we are looking for</param>
        /// <param name="seed">Seed of the item we are looking for</param>
        /// <param name="comparer">Comparer used to find the item</param>
        /// <param name="low">Low range of item index</param>
        /// <param name="high">High range of item index</param>
        /// <returns>Index of the specified item</returns>
        protected override int FindIndex(object item, object seed, IComparer comparer, int low, int high)
        {
            // root group needs to adjust the bounds of the search to exclude the new item (if any)
            if (_view is IDataGridEditableCollectionView iecv && iecv.IsAddingNew)
            {
                --high;
            }

            return base.FindIndex(item, seed, comparer, low, high);
        }

        /// <summary>
        /// Initializes the group descriptions
        /// </summary>
        internal void Initialize()
        {
            if (topLevelGroupDescription == null)
            {
                topLevelGroupDescription = new TopLevelGroupDescription();
            }

            InitializeGroup(this, 0, null);
        }

        /// <summary>
        /// Inserts specified item into the collection
        /// </summary>
        /// <param name="index">Index to insert into</param>
        /// <param name="item">Item to insert</param>
        /// <param name="loading">Whether we are currently loading</param>
        internal void InsertSpecialItem(int index, object item, bool loading)
        {
            ChangeCounts(item, +1);
            ProtectedItems.Insert(index, item);

            if (!loading)
            {
                int globalIndex = LeafIndexFromItem(item, index);
                OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item, globalIndex));
            }
        }

        /// <summary>
        /// Notify listeners that this View has changed
        /// </summary>
        /// <remarks>
        /// CollectionViews (and sub-classes) should take their filter/sort/grouping
        /// into account before calling this method to forward CollectionChanged events.
        /// </remarks>
        /// <param name="args">The NotifyCollectionChangedEventArgs to be passed to the EventHandler</param>
        public void OnCollectionChanged(NotifyCollectionChangedEventArgs args)
        {
            Debug.Assert(args != null, "Arguments passed in should not be null");
            CollectionChanged?.Invoke(this, args);
        }

        /// <summary>
        /// Notify host that a group description has changed somewhere in the tree
        /// </summary>
        protected override void OnGroupByChanged()
        {
            GroupDescriptionChanged?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Remove specified item from subgroups
        /// </summary>
        /// <param name="item">Item to remove</param>
        /// <returns>Whether the operation was successful</returns>
        internal bool RemoveFromSubgroups(object item)
        {
            return RemoveFromSubgroups(item, this, 0);
        }

        /// <summary>
        /// Remove specified item from subgroups using an exhaustive search
        /// </summary>
        /// <param name="item">Item to remove</param>
        internal void RemoveItemFromSubgroupsByExhaustiveSearch(object item)
        {
            RemoveItemFromSubgroupsByExhaustiveSearch(this, item);
        }

        /// <summary>
        /// Removes specified item into the collection
        /// </summary>
        /// <param name="index">Index to remove from</param>
        /// <param name="item">Item to remove</param>
        /// <param name="loading">Whether we are currently loading</param>
        internal void RemoveSpecialItem(int index, object item, bool loading)
        {
            Debug.Assert(Object.Equals(item, ProtectedItems[index]), "RemoveSpecialItem finds inconsistent data");
            int globalIndex = -1;

            if (!loading)
            {
                globalIndex = LeafIndexFromItem(item, index);
            }

            ChangeCounts(item, -1);
            ProtectedItems.RemoveAt(index);

            if (!loading)
            {
                OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, item, globalIndex));
            }
        }

        /// <summary>
        /// Adds specified item to subgroups
        /// </summary>
        /// <param name="item">Item to add</param>
        /// <param name="loading">Whether we are currently loading</param>
        internal void AddToSubgroups(object item, bool loading)
        {
            AddToSubgroups(item, this, 0, loading);
        }

        /// <summary>
        /// Add an item to the subgroup with the given name
        /// </summary>
        /// <param name="item">Item to add</param>
        /// <param name="group">Group to add item to</param>
        /// <param name="level">The level of grouping.</param>
        /// <param name="key">Name of subgroup to add to</param>
        /// <param name="loading">Whether we are currently loading</param>
        private void AddToSubgroup(object item, DataGridCollectionViewGroupInternal group, int level, object key, bool loading)
        {
            DataGridCollectionViewGroupInternal subgroup;
            int index = (_isDataInGroupOrder) ? group.LastIndex : 0;

            // find the desired subgroup
            for (int n = group.Items.Count; index < n; ++index)
            {
                subgroup = group.Items[index] as DataGridCollectionViewGroupInternal;
                if (subgroup == null)
                {
                    continue;           // skip children that are not groups
                }

                if (group.GroupBy.KeysMatch(subgroup.Key, key))
                {
                    group.LastIndex = index;
                    AddToSubgroups(item, subgroup, level + 1, loading);
                    return;
                }
            }

            // the item didn't match any subgroups.  Create a new subgroup and add the item.
            subgroup = new DataGridCollectionViewGroupInternal(key, group);
            InitializeGroup(subgroup, level + 1, item);

            if (loading)
            {
                group.Add(subgroup);
                group.LastIndex = index;
            }
            else
            {
                // using insert will find the correct sort index to
                // place the subgroup, and will default to the last
                // position if no ActiveComparer is specified
                group.Insert(subgroup, item, ActiveComparer);
            }

            AddToSubgroups(item, subgroup, level + 1, loading);
        }

        /// <summary>
        /// Add an item to the desired subgroup(s) of the given group
        /// </summary>
        /// <param name="item">Item to add</param>
        /// <param name="group">Group to add item to</param>
        /// <param name="level">The level of grouping</param>
        /// <param name="loading">Whether we are currently loading</param>
        private void AddToSubgroups(object item, DataGridCollectionViewGroupInternal group, int level, bool loading)
        {
            object key = GetGroupKey(item, group.GroupBy, level);

            if (key == UseAsItemDirectly)
            {
                // the item belongs to the group itself (not to any subgroups)
                if (loading)
                {
                    group.Add(item);
                }
                else
                {
                    int localIndex = group.Insert(item, item, ActiveComparer);
                    int index = group.LeafIndexFromItem(item, localIndex);
                    OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item, index));
                }
            }
            else if(key is ICollection keyList)
            {
                // the item belongs to multiple subgroups
                foreach (object o in keyList)
                {
                    AddToSubgroup(item, group, level, o, loading);
                }
            }
            else
            {
                // the item belongs to one subgroup
                AddToSubgroup(item, group, level, key, loading);
            }
        }

        public virtual Func<DataGridCollectionViewGroup, int, DataGridGroupDescription> GroupBySelector { get; set; }

        /// <summary>
        /// Returns the description of how to divide the given group into subgroups
        /// </summary>
        /// <param name="group">CollectionViewGroup to get group description from</param>
        /// <param name="level">The level of grouping</param>
        /// <returns>GroupDescription of how to divide the given group</returns>
        private DataGridGroupDescription GetGroupDescription(DataGridCollectionViewGroup group, int level)
        {
            DataGridGroupDescription result = null;
            if (group == this)
            {
                group = null;
            }

            if (result == null && GroupBySelector != null)
            {
                result = GroupBySelector?.Invoke(group, level);
            }

            if (result == null && level < GroupDescriptions.Count)
            {
                result = GroupDescriptions[level];
            }

            return result;
        }

        /// <summary>
        /// Get the group name(s) for the given item
        /// </summary>
        /// <param name="item">Item to get group name for</param>
        /// <param name="groupDescription">GroupDescription for the group</param>
        /// <param name="level">The level of grouping</param>
        /// <returns>Group names for the specified item</returns>
        private object GetGroupKey(object item, DataGridGroupDescription groupDescription, int level)
        {
            if (groupDescription != null)
            {
                return groupDescription.GroupKeyFromItem(item, level, Culture);
            }
            else
            {
                return UseAsItemDirectly;
            }
        }

        /// <summary>
        /// Initialize the given group
        /// </summary>
        /// <param name="group">Group to initialize</param>
        /// <param name="level">The level of grouping</param>
        /// <param name="seedItem">The seed item to compare with to see where to insert</param>
        private void InitializeGroup(DataGridCollectionViewGroupInternal group, int level, object seedItem)
        {
            // set the group description for dividing the group into subgroups
            DataGridGroupDescription groupDescription = GetGroupDescription(group, level);
            group.GroupBy = groupDescription;

            // create subgroups for each of the explicit names
            var keys = groupDescription?.GroupKeys;
            if (keys != null)
            {
                for (int k = 0, n = keys.Count; k < n; ++k)
                {
                    DataGridCollectionViewGroupInternal subgroup = new DataGridCollectionViewGroupInternal(keys[k], group);
                    InitializeGroup(subgroup, level + 1, seedItem);
                    group.Add(subgroup);
                }
            }

            group.LastIndex = 0;
        }

        /// <summary>
        /// Remove an item from the direct children of a group.
        /// </summary>
        /// <param name="group">Group to remove item from</param>
        /// <param name="item">Item to remove</param>
        /// <returns>True if item could not be removed</returns>
        private bool RemoveFromGroupDirectly(DataGridCollectionViewGroupInternal group, object item)
        {
            int leafIndex = group.Remove(item, true);
            if (leafIndex >= 0)
            {
                OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, item, leafIndex));
                return false;
            }
            else
            {
                return true;
            }
        }

        /// <summary>
        /// Remove an item from the subgroup with the given name.
        /// </summary>
        /// <param name="item">Item to remove</param>
        /// <param name="group">Group to remove item from</param>
        /// <param name="level">The level of grouping</param>
        /// <param name="key">Name of item to remove</param>
        /// <returns>Return true if the item was not in one of the subgroups it was supposed to be.</returns>
        private bool RemoveFromSubgroup(object item, DataGridCollectionViewGroupInternal group, int level, object key)
        {
            bool itemIsMissing = false;
            DataGridCollectionViewGroupInternal subgroup;

            // find the desired subgroup
            for (int index = 0, n = group.Items.Count; index < n; ++index)
            {
                subgroup = group.Items[index] as DataGridCollectionViewGroupInternal;
                if (subgroup == null)
                {
                    continue;           // skip children that are not groups
                }

                if (group.GroupBy.KeysMatch(subgroup.Key, key))
                {
                    if (RemoveFromSubgroups(item, subgroup, level + 1))
                    {
                        itemIsMissing = true;
                    }

                    return itemIsMissing;
                }
            }

            // the item didn't match any subgroups.  It should have.
            return true;
        }

        /// <summary>
        /// Remove an item from the desired subgroup(s) of the given group.
        /// </summary>
        /// <param name="item">Item to remove</param>
        /// <param name="group">Group to remove item from</param>
        /// <param name="level">The level of grouping</param>
        /// <returns>Return true if the item was not in one of the subgroups it was supposed to be.</returns>
        private bool RemoveFromSubgroups(object item, DataGridCollectionViewGroupInternal group, int level)
        {
            bool itemIsMissing = false;
            object key = GetGroupKey(item, group.GroupBy, level);

            if (key == UseAsItemDirectly)
            {
                // the item belongs to the group itself (not to any subgroups)
                itemIsMissing = RemoveFromGroupDirectly(group, item);
            }
            else if (key is ICollection keyList)
            {
                // the item belongs to multiple subgroups
                foreach (object o in keyList)
                {
                    if (RemoveFromSubgroup(item, group, level, o))
                    {
                        itemIsMissing = true;
                    }
                }
            }
            else
            {
                // the item belongs to one subgroup
                if (RemoveFromSubgroup(item, group, level, key))
                {
                    itemIsMissing = true;
                }
            }

            return itemIsMissing;
        }

        /// <summary>
        /// The item did not appear in one or more of the subgroups it
        /// was supposed to.  This can happen if the item's properties
        /// change so that the group names we used to insert it are
        /// different from the names used to remove it. If this happens,
        /// remove the item the hard way.
        /// </summary>
        /// <param name="group">Group to remove item from</param>
        /// <param name="item">Item to remove</param>
        private void RemoveItemFromSubgroupsByExhaustiveSearch(DataGridCollectionViewGroupInternal group, object item)
        {
            // try to remove the item from the direct children 
            // this function only returns true if it failed to remove from group directly
            // in which case we will step through and search exhaustively
            if (RemoveFromGroupDirectly(group, item))
            {
                // if that didn't work, recurse into each subgroup
                // (loop runs backwards in case an entire group is deleted)
                for (int k = group.Items.Count - 1; k >= 0; --k)
                {
                    if (group.Items[k] is DataGridCollectionViewGroupInternal subgroup)
                    {
                        RemoveItemFromSubgroupsByExhaustiveSearch(subgroup, item);
                    }
                }
            }
        }

        /// <summary>
        /// TopLevelGroupDescription class
        /// </summary>
        private class TopLevelGroupDescription : DataGridGroupDescription
        {
            /// <summary>
            /// Initializes a new instance of the TopLevelGroupDescription class.
            /// </summary>
            public TopLevelGroupDescription()
            {
            }

            /// <summary>
            /// We have to implement this abstract method, but it should never be called
            /// </summary>
            /// <param name="item">Item to get group name from</param>
            /// <param name="level">The level of grouping</param>
            /// <param name="culture">Culture used for sorting</param>
            /// <returns>We do not return a value here</returns>
            public override object GroupKeyFromItem(object item, int level, CultureInfo culture)
            {
                Debug.Assert(true, "We have to implement this abstract method, but it should never be called");
                return null;
            }
        }
    }

}
