// This source file is adapted from the WinUI project.
// (https://github.com/microsoft/microsoft-ui-xaml)
//
// Licensed to The Avalonia Project under MIT License, courtesy of The .NET Foundation.

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Logging;
using Avalonia.VisualTree;

namespace Avalonia.Controls
{
    internal sealed class ViewManager
    {
        private const int FirstRealizedElementIndexDefault = int.MaxValue;
        private const int LastRealizedElementIndexDefault = int.MinValue;

        private readonly ItemsRepeater _owner;
        private readonly List<PinnedElementInfo> _pinnedPool = new List<PinnedElementInfo>();
        private readonly UniqueIdElementPool _resetPool;
        private IControl _lastFocusedElement;
        private bool _isDataSourceStableResetPending;
        private ElementFactoryGetArgs _elementFactoryGetArgs;
        private ElementFactoryRecycleArgs _elementFactoryRecycleArgs;
        private int _firstRealizedElementIndexHeldByLayout = FirstRealizedElementIndexDefault;
        private int _lastRealizedElementIndexHeldByLayout = LastRealizedElementIndexDefault;
        private bool _eventsSubscribed;

        public ViewManager(ItemsRepeater owner)
        {
            _owner = owner;
            _resetPool = new UniqueIdElementPool(owner);
        }

        public IControl GetElement(int index, bool forceCreate, bool suppressAutoRecycle)
        {
            var element = forceCreate ? null : GetElementIfAlreadyHeldByLayout(index);
            if (element == null)
            {
                // check if this is the anchor made through repeater in preparation 
                // for a bring into view.
                var madeAnchor = _owner.MadeAnchor;
                if (madeAnchor != null)
                {
                    var anchorVirtInfo = ItemsRepeater.TryGetVirtualizationInfo(madeAnchor);
                    if (anchorVirtInfo.Index == index)
                    {
                        element = madeAnchor;
                    }
                }
            }
            if (element == null) { element = GetElementFromUniqueIdResetPool(index); };
            if (element == null) { element = GetElementFromPinnedElements(index); }
            if (element == null) { element = GetElementFromElementFactory(index); }

            var virtInfo = ItemsRepeater.TryGetVirtualizationInfo(element);
            if (suppressAutoRecycle)
            {
                virtInfo.AutoRecycleCandidate = false;
                Logger.TryGet(LogEventLevel.Verbose, "Repeater")?.Log(this, "GetElement: {Index} Not AutoRecycleCandidate:", virtInfo.Index);
            }
            else
            {
                virtInfo.AutoRecycleCandidate = true;
                virtInfo.KeepAlive = true;
                Logger.TryGet(LogEventLevel.Verbose, "Repeater")?.Log(this, "GetElement: {Index} AutoRecycleCandidate:", virtInfo.Index);
            }

            return element;
        }

        public void ClearElement(IControl element, bool isClearedDueToCollectionChange)
        {
            var virtInfo = ItemsRepeater.GetVirtualizationInfo(element);
            var index = virtInfo.Index;
            bool cleared =
                ClearInlineElement(virtInfo, isClearedDueToCollectionChange) ||
                ClearElementToUniqueIdResetPool(element, virtInfo) ||
                ClearElementToPinnedPool(element, virtInfo, isClearedDueToCollectionChange);

            if (!cleared)
            {
                ClearElementToElementFactory(element);
            }

            // Both First and Last indices need to be valid or default.
            if (index == _firstRealizedElementIndexHeldByLayout && index == _lastRealizedElementIndexHeldByLayout)
            {
                // First and last were pointing to the same element and that is going away.
                InvalidateRealizedIndicesHeldByLayout();
            }
            else if (index == _firstRealizedElementIndexHeldByLayout)
            {
                // The FirstElement is going away, shrink the range by one.
                ++_firstRealizedElementIndexHeldByLayout;
            }
            else if (index == _lastRealizedElementIndexHeldByLayout)
            {
                // Last element is going away, shrink the range by one at the end.
                --_lastRealizedElementIndexHeldByLayout;
            }
            else
            {
                // Index is either outside the range we are keeping track of or inside the range.
                // In both these cases, we just keep the range we have. If this clear was due to 
                // a collection change, then in the CollectionChanged event, we will invalidate these guys.
            }
        }

        // We need to clear the datacontext to prevent crashes from happening,
        //  however we only do that if we were the ones setting it.
        // That is when one of the following is the case (numbering taken from line ~642):
        // 1.2    No ItemTemplate, data is not a UIElement
        // 2.1    ItemTemplate, data is not FrameworkElement
        // 2.2.2  Itemtemplate, data is FrameworkElement, ElementFactory returned Element different to data
        //
        // In all of those three cases, we the ItemTemplateShim is NOT null.
        // Luckily when we create the items, we store whether we were the once setting the DataContext.
        public void ClearElementToElementFactory(IControl element)
        {
            _owner.OnElementClearing(element);

            var virtInfo = ItemsRepeater.GetVirtualizationInfo(element);
            virtInfo.MoveOwnershipToElementFactory();

            // During creation of this object, we were the one setting the DataContext, so clear it now.
            if (virtInfo.MustClearDataContext)
            {
                element.DataContext = null;
            }

            if (_owner.ItemTemplateShim != null)
            {
                var context = _elementFactoryRecycleArgs ??= new ElementFactoryRecycleArgs();
                context.Element = element;
                context.Parent = _owner;

                _owner.ItemTemplateShim.RecycleElement(context);

                context.Element = null;
                context.Parent = null;
            }
            else
            {
                // No ItemTemplate to recycle to, remove the element from the children collection.
                if (!_owner.Children.Remove(element))
                {
                    throw new InvalidOperationException("ItemsRepeater's child not found in its Children collection.");
                }
            }

            if (_lastFocusedElement == element)
            {
                // Focused element is going away. Remove the tracked last focused element
                // and pick a reasonable next focus if we can find one within the layout 
                // realized elements.
                MoveFocusFromClearedIndex(virtInfo.Index);
            }
        }

        private void MoveFocusFromClearedIndex(int clearedIndex)
        {
            var focusCandidate = FindFocusCandidate(clearedIndex, out var focusedChild);
            if (focusCandidate != null)
            {
                focusCandidate.Focus();
                _lastFocusedElement = focusedChild;

                // Add pin to hold the focused element.
                UpdatePin(focusedChild, true /* addPin */);
            }
            else
            {
                // We could not find a candiate.
                _lastFocusedElement = null;
            }
        }

        IControl FindFocusCandidate(int clearedIndex, out IControl focusedChild)
        {
            // Walk through all the children and find elements with index before and after the cleared index.
            // Note that during a delete the next element would now have the same index.
            int previousIndex = int.MinValue;
            int nextIndex = int.MaxValue;
            IControl nextElement = null;
            IControl previousElement = null;

            foreach (var child in _owner.Children)
            {
                var virtInfo = ItemsRepeater.TryGetVirtualizationInfo(child);
                if (virtInfo?.IsHeldByLayout == true)
                {
                    int currentIndex = virtInfo.Index;
                    if (currentIndex < clearedIndex)
                    {
                        if (currentIndex > previousIndex)
                        {
                            previousIndex = currentIndex;
                            previousElement = child;
                        }
                    }
                    else if (currentIndex >= clearedIndex)
                    {
                        // Note that we use >= above because if we deleted the focused element, 
                        // the next element would have the same index now.
                        if (currentIndex < nextIndex)
                        {
                            nextIndex = currentIndex;
                            nextElement = child;
                        }
                    }
                }
            }

            // TODO: Find the next element if one exists, if not use the previous element.
            // If the container itself is not focusable, find a descendent that is.
            focusedChild = nextElement;
            return nextElement;
        }

        public int GetElementIndex(VirtualizationInfo virtInfo)
        {
            if (virtInfo == null)
            {
                //Element is not a child of this ItemsRepeater.
                return -1;
            }

            return virtInfo.IsRealized || virtInfo.IsInUniqueIdResetPool ? virtInfo.Index : -1;
        }

        public void PrunePinnedElements()
        {
            EnsureEventSubscriptions();

            // Go through pinned elements and make sure they still have
            // a reason to be pinned.
            for (var i = 0; i < _pinnedPool.Count; ++i)
            {
                var elementInfo = _pinnedPool[i];
                var virtInfo = elementInfo.VirtualizationInfo;

                if (!virtInfo.IsPinned)
                {
                    _pinnedPool.RemoveAt(i);
                    --i;

                    // Pinning was the only thing keeping this element alive.
                    ClearElementToElementFactory(elementInfo.PinnedElement);
                }
            }
        }

        public void UpdatePin(IControl element, bool addPin)
        {
            var parent = element.VisualParent;
            var child = (IVisual)element;

            while (parent != null)
            {
                if (parent is ItemsRepeater repeater)
                {
                    var virtInfo = ItemsRepeater.GetVirtualizationInfo((IControl)child);
                    if (virtInfo.IsRealized)
                    {
                        if (addPin)
                        {
                            virtInfo.AddPin();
                        }
                        else if (virtInfo.IsPinned)
                        {
                            if (virtInfo.RemovePin() == 0)
                            {
                                // ElementFactory is invoked during the measure pass.
                                // We will clear the element then.
                                repeater.InvalidateMeasure();
                            }
                        }
                    }
                }

                child = parent;
                parent = child.VisualParent;
            }
        }

        public void OnItemsSourceChanged(object sender, NotifyCollectionChangedEventArgs args)
        {
            // Note: For items that have been removed, the index will not be touched. It will hold
            // the old index before it was removed. It is not valid anymore.
            switch (args.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    {
                        var newIndex = args.NewStartingIndex;
                        var newCount = args.NewItems.Count;
                        EnsureFirstLastRealizedIndices();
                        if (newIndex <= _lastRealizedElementIndexHeldByLayout)
                        {
                            _lastRealizedElementIndexHeldByLayout += newCount;
                            foreach (var element in _owner.Children)
                            {
                                var virtInfo = ItemsRepeater.GetVirtualizationInfo(element);
                                var dataIndex = virtInfo.Index;

                                if (virtInfo.IsRealized && dataIndex >= newIndex)
                                {
                                    UpdateElementIndex(element, virtInfo, dataIndex + newCount);
                                }
                            }
                        }
                        else
                        {
                            // Indices held by layout are not affected
                            // We could still have items in the pinned elements that need updates. This is usually a very small vector.
                            for (var i = 0; i < _pinnedPool.Count; ++i)
                            {
                                var elementInfo = _pinnedPool[i];
                                var virtInfo = elementInfo.VirtualizationInfo;
                                var dataIndex = virtInfo.Index;

                                if (virtInfo.IsRealized && dataIndex >= newIndex)
                                {
                                    var element = elementInfo.PinnedElement;
                                    UpdateElementIndex(element, virtInfo, dataIndex + newCount);
                                }
                            }
                        }
                        break;
                    }

                case NotifyCollectionChangedAction.Replace:
                    {
                        // Requirement: oldStartIndex == newStartIndex. It is not a replace if this is not true.
                        // Two cases here
                        // case 1: oldCount == newCount 
                        //         indices are not affected. nothing to do here.  
                        // case 2: oldCount != newCount
                        //         Replaced with less or more items. This is like an insert or remove
                        //         depending on the counts.
                        var oldStartIndex = args.OldStartingIndex;
                        var newStartingIndex = args.NewStartingIndex;
                        var oldCount = args.OldItems.Count;
                        var newCount = args.NewItems.Count;
                        if (oldStartIndex != newStartingIndex)
                        {
                            throw new NotSupportedException("Replace is only allowed with OldStartingIndex equals to NewStartingIndex.");
                        }

                        if (oldCount == 0)
                        {
                            throw new NotSupportedException("Replace notification with args.OldItemsCount value of 0 is not allowed. Use Insert action instead.");
                        }

                        if (newCount == 0)
                        {
                            throw new NotSupportedException("Replace notification with args.NewItemCount value of 0 is not allowed. Use Remove action instead.");
                        }

                        int countChange = newCount - oldCount;
                        if (countChange != 0)
                        {
                            // countChange > 0 : countChange items were added
                            // countChange < 0 : -countChange  items were removed
                            foreach (var element in _owner.Children)
                            {
                                var virtInfo = ItemsRepeater.GetVirtualizationInfo(element);
                                var dataIndex = virtInfo.Index;

                                if (virtInfo.IsRealized)
                                {
                                    if (dataIndex >= oldStartIndex + oldCount)
                                    {
                                        UpdateElementIndex(element, virtInfo, dataIndex + countChange);
                                    }
                                }
                            }

                            EnsureFirstLastRealizedIndices();
                            _lastRealizedElementIndexHeldByLayout += countChange;
                        }
                        break;
                    }

                case NotifyCollectionChangedAction.Remove:
                    {
                        var oldStartIndex = args.OldStartingIndex;
                        var oldCount = args.OldItems.Count;
                        foreach (var element in _owner.Children)
                        {
                            var virtInfo = ItemsRepeater.GetVirtualizationInfo(element);
                            var dataIndex = virtInfo.Index;

                            if (virtInfo.IsRealized || virtInfo.IsInlineElement)
                            {
                                if ((virtInfo.AutoRecycleCandidate || virtInfo.IsInlineElement) &&
                                    oldStartIndex <= dataIndex && dataIndex < oldStartIndex + oldCount)
                                {
                                    // If we are doing the mapping, remove the element who's data was removed.
                                    _owner.ClearElementImpl(element);
                                }
                                else if (dataIndex >= (oldStartIndex + oldCount))
                                {
                                    UpdateElementIndex(element, virtInfo, dataIndex - oldCount);
                                }
                            }
                        }

                        InvalidateRealizedIndicesHeldByLayout();
                        break;
                    }

                case NotifyCollectionChangedAction.Reset:
                    // If we get multiple resets back to back before
                    // running layout, we dont have to clear all the elements again.
                    if (!_isDataSourceStableResetPending)
                    {
                        // Walk through all the elements and make sure they are cleared, they will go into
                        // the stable id reset pool or if they are inline elements marked for removal.
                        foreach (var element in _owner.Children)
                        {
                            var virtInfo = ItemsRepeater.GetVirtualizationInfo(element);
                            if ((virtInfo.IsRealized && virtInfo.AutoRecycleCandidate) || virtInfo.IsInlineElement)
                            {
                                _owner.ClearElementImpl(element);
                            }
                        }
                    }

                    InvalidateRealizedIndicesHeldByLayout();
                    break;
            }
        }

        private void EnsureFirstLastRealizedIndices()
        {
            if (_firstRealizedElementIndexHeldByLayout == FirstRealizedElementIndexDefault)
            {
                // This will ensure that the indexes are updated.
                GetElementIfAlreadyHeldByLayout(0);
            }
        }

        public void OnLayoutChanging()
        {
            if (_owner.ItemsSourceView?.HasKeyIndexMapping == true)
            {
                _isDataSourceStableResetPending = true;
            }
        }

        public void OnOwnerArranged()
        {
            if (_isDataSourceStableResetPending)
            {
                _isDataSourceStableResetPending = false;

                foreach (var entry in _resetPool)
                {
                    // TODO: Task 14204306: ItemsRepeater: Find better focus candidate when focused element is deleted in the ItemsSource.
                    // Focused element is getting cleared. Need to figure out semantics on where
                    // focus should go when the focused element is removed from the data collection.
                    ClearElement(entry.Value, true /* isClearedDueToCollectionChange */);
                }

                _resetPool.Clear();

                // Flush the realized indices once the stable reset pool is cleared to start fresh.
                InvalidateRealizedIndicesHeldByLayout();
            }
        }

        // We optimize for the case where index is not realized to return null as quickly as we can.
        // Flow layouts manage containers on their own and will never ask for an index that is already realized.
        // If an index that is realized is requested by the layout, we unfortunately have to walk the
        // children. Not ideal, but a reasonable default to provide consistent behavior between virtualizing
        // and non-virtualizing hosts.
        private IControl GetElementIfAlreadyHeldByLayout(int index)
        {
            IControl element = null;

            bool cachedFirstLastIndicesInvalid = _firstRealizedElementIndexHeldByLayout == FirstRealizedElementIndexDefault;
            bool isRequestedIndexInRealizedRange = (_firstRealizedElementIndexHeldByLayout <= index && index <= _lastRealizedElementIndexHeldByLayout);

            if (cachedFirstLastIndicesInvalid || isRequestedIndexInRealizedRange)
            {
                foreach (var child in _owner.Children)
                {
                    var virtInfo = ItemsRepeater.TryGetVirtualizationInfo(child);
                    if (virtInfo?.IsHeldByLayout == true)
                    {
                        // Only give back elements held by layout. If someone else is holding it, they will be served by other methods.
                        int childIndex = virtInfo.Index;
                        _firstRealizedElementIndexHeldByLayout = Math.Min(_firstRealizedElementIndexHeldByLayout, childIndex);
                        _lastRealizedElementIndexHeldByLayout = Math.Max(_lastRealizedElementIndexHeldByLayout, childIndex);
                        if (virtInfo.Index == index)
                        {
                            element = child;
                            // If we have valid first/last indices, we don't have to walk the rest, but if we 
                            // do not, then we keep walking through the entire children collection to get accurate
                            // indices once.
                            if (!cachedFirstLastIndicesInvalid)
                            {
                                break;
                            }
                        }
                    }
                }
            }

            return element;
        }

        private IControl GetElementFromUniqueIdResetPool(int index)
        {
            IControl element = null;
            // See if you can get it from the reset pool.
            if (_isDataSourceStableResetPending)
            {
                element = _resetPool.Remove(index);
                if (element != null)
                {
                    // Make sure that the index is updated to the current one
                    var virtInfo = ItemsRepeater.GetVirtualizationInfo(element);
                    virtInfo.MoveOwnershipToLayoutFromUniqueIdResetPool();
                    UpdateElementIndex(element, virtInfo, index);

                    // Update realized indices
                    _firstRealizedElementIndexHeldByLayout = Math.Min(_firstRealizedElementIndexHeldByLayout, index);
                    _lastRealizedElementIndexHeldByLayout = Math.Max(_lastRealizedElementIndexHeldByLayout, index);
                }
            }

            return element;
        }

        private IControl GetElementFromPinnedElements(int index)
        {
            IControl element = null;

            // See if you can find something among the pinned elements.
            for (var i = 0; i < _pinnedPool.Count; ++i)
            {
                var elementInfo = _pinnedPool[i];
                var virtInfo = elementInfo.VirtualizationInfo;

                if (virtInfo.Index == index)
                {
                    _pinnedPool.RemoveAt(i);
                    element = elementInfo.PinnedElement;
                    elementInfo.VirtualizationInfo.MoveOwnershipToLayoutFromPinnedPool();

                    // Update realized indices
                    _firstRealizedElementIndexHeldByLayout = Math.Min(_firstRealizedElementIndexHeldByLayout, index);
                    _lastRealizedElementIndexHeldByLayout = Math.Max(_lastRealizedElementIndexHeldByLayout, index);
                    break;
                }
            }

            return element;
        }

        // There are several cases handled here with respect to which element gets returned and when DataContext is modified.
        //
        // 1. If there is no ItemTemplate:
        //    1.1 If data is an IControl -> the data is returned
        //    1.2 If data is not an IControl -> a default DataTemplate is used to fetch element and DataContext is set to data
        //
        // 2. If there is an ItemTemplate:
        //    2.1 If data is not an IControl -> Element is fetched from ElementFactory and DataContext is set to the data
        //    2.2 If data is an IControl:
        //        2.2.1 If Element returned by the ElementFactory is the same as the data -> Element (a.k.a. data) is returned as is
        //        2.2.2 If Element returned by the ElementFactory is not the same as the data
        //                 -> Element that is fetched from the ElementFactory is returned and
        //                    DataContext is set to the data's DataContext (if it exists), otherwise it is set to the data itself
        private IControl GetElementFromElementFactory(int index)
        {
            // The view generator is the provider of last resort.
            var data = _owner.ItemsSourceView.GetAt(index);
            var providedElementFactory = _owner.ItemTemplateShim;

            IElementFactory GetElementFactory()
            {
                if (providedElementFactory == null)
                {
                    var factory = FuncDataTemplate.Default;
                    _owner.ItemTemplate = factory;
                    return _owner.ItemTemplateShim;
                }

                return providedElementFactory;
            }

            IControl GetElement()
            {
                if (providedElementFactory == null)
                {
                    if (data is IControl dataAsElement)
                    {
                        return dataAsElement;
                    }
                }

                var elementFactory = GetElementFactory();
                var args = _elementFactoryGetArgs ??= new ElementFactoryGetArgs();

                try
                {
                    args.Data = data;
                    args.Parent = _owner;
                    args.Index = index;
                    return elementFactory.GetElement(args);
                }
                finally
                {
                    args.Data = null;
                    args.Parent = null;
                }
            }

            var element = GetElement();

            var virtInfo = ItemsRepeater.TryGetVirtualizationInfo(element);
            if (virtInfo == null)
            {
                virtInfo = ItemsRepeater.CreateAndInitializeVirtualizationInfo(element);
            }
            // Clear flag
            virtInfo.MustClearDataContext = false;

            if (!(data is IControl))
            {
                // Prepare the element
                element.DataContext = data;
                virtInfo.MustClearDataContext = true;
            }
            else
            {
                virtInfo.IsInlineElement = data == element;
            }

            virtInfo.MoveOwnershipToLayoutFromElementFactory(
                index,
                /* uniqueId: */
                _owner.ItemsSourceView.HasKeyIndexMapping ?
                _owner.ItemsSourceView.KeyFromIndex(index) :
                string.Empty);

            // The view generator is the only provider that prepares the element.
            var repeater = _owner;

            // Add the element to the children collection here before raising OnElementPrepared so 
            // that handlers can walk up the tree in case they want to find their IndexPath in the 
            // nested case.
            var children = repeater.Children;
            if (element.VisualParent != repeater)
            {
                children.Add(element);
            }

            repeater.OnElementPrepared(element, virtInfo);

            // Update realized indices
            _firstRealizedElementIndexHeldByLayout = Math.Min(_firstRealizedElementIndexHeldByLayout, index);
            _lastRealizedElementIndexHeldByLayout = Math.Max(_lastRealizedElementIndexHeldByLayout, index);

            return element;
        }

        private bool ClearInlineElement(VirtualizationInfo virtInfo, bool isClearedDueToCollectionChange)
        {
            if (virtInfo.IsInlineElement)
            {
                if (isClearedDueToCollectionChange)
                {
                    virtInfo.ToRemove = true;
                }

                return true;
            }

            return false;
        }

        private bool ClearElementToUniqueIdResetPool(IControl element, VirtualizationInfo virtInfo)
        {
            if (_isDataSourceStableResetPending)
            {
                _resetPool.Add(element);
                virtInfo.MoveOwnershipToUniqueIdResetPoolFromLayout();
            }

            return _isDataSourceStableResetPending;
        }

        private bool ClearElementToPinnedPool(IControl element, VirtualizationInfo virtInfo, bool isClearedDueToCollectionChange)
        {
            bool moveToPinnedPool =
                !isClearedDueToCollectionChange && virtInfo.IsPinned;

            if (moveToPinnedPool)
            {
                _pinnedPool.Add(new PinnedElementInfo(element));
                virtInfo.MoveOwnershipToPinnedPool();
            }

            return moveToPinnedPool;
        }

        private void UpdateFocusedElement()
        {
            IControl focusedElement = null;

            var child = FocusManager.Instance.Current;

            if (child != null)
            {
                var parent = child.VisualParent;
                var owner = _owner;

                // Find out if the focused element belongs to one of our direct
                // children.
                while (parent != null)
                {
                    if (parent is ItemsRepeater repeater)
                    {
                        var element = child as IControl;
                        if (repeater == owner && ItemsRepeater.GetVirtualizationInfo(element).IsRealized)
                        {
                            focusedElement = element;
                        }

                        break;
                    }

                    child = parent as IInputElement;
                    parent = child.VisualParent;
                }
            }

            // If the focused element has changed,
            // we need to unpin the old one and pin the new one.
            if (_lastFocusedElement != focusedElement)
            {
                if (_lastFocusedElement != null)
                {
                    UpdatePin(_lastFocusedElement, false /* addPin */);
                }

                if (focusedElement != null)
                {
                    UpdatePin(focusedElement, true /* addPin */);
                }

                _lastFocusedElement = focusedElement;
            }
        }

        private void OnFocusChanged(object sender, RoutedEventArgs e) => UpdateFocusedElement();

        private void EnsureEventSubscriptions()
        {
            if (!_eventsSubscribed)
            {
                _owner.GotFocus += OnFocusChanged;
                _owner.LostFocus += OnFocusChanged;
                _eventsSubscribed = true;
            }
        }

        private void UpdateElementIndex(IControl element, VirtualizationInfo virtInfo, int index)
        {
            var oldIndex = virtInfo.Index;
            if (oldIndex != index)
            {
                virtInfo.UpdateIndex(index);
                _owner.OnElementIndexChanged(element, oldIndex, index);
            }
        }

        private void InvalidateRealizedIndicesHeldByLayout()
        {
            _firstRealizedElementIndexHeldByLayout = FirstRealizedElementIndexDefault;
            _lastRealizedElementIndexHeldByLayout = LastRealizedElementIndexDefault;
        }

        private struct PinnedElementInfo
        {
            public PinnedElementInfo(IControl element)
            {
                PinnedElement = element;
                VirtualizationInfo = ItemsRepeater.GetVirtualizationInfo(element);
            }

            public IControl PinnedElement { get; }
            public VirtualizationInfo VirtualizationInfo { get; }
        }
    }
}
