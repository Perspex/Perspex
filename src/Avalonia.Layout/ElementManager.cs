// This source file is adapted from the WinUI project.
// (https://github.com/microsoft/microsoft-ui-xaml)
//
// Licensed to The Avalonia Project under MIT License, courtesy of The .NET Foundation.

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using Avalonia.Layout.Utils;
using Avalonia.Logging;

namespace Avalonia.Layout
{
    internal class ElementManager
    {
        private readonly List<ILayoutable> _realizedElements = new List<ILayoutable>();
        private readonly List<Rect> _realizedElementLayoutBounds = new List<Rect>();
        private int _firstRealizedDataIndex;
        private VirtualizingLayoutContext _context;

        private bool IsVirtualizingContext
        {
            get
            {
                if (_context != null)
                {
                    var rect = _context.RealizationRect;
                    bool hasInfiniteSize = double.IsInfinity(rect.Height) || double.IsInfinity(rect.Width);
                    return !hasInfiniteSize;
                }
                return false;
            }
        }

        public void SetContext(VirtualizingLayoutContext virtualContext) => _context = virtualContext;

        public void OnBeginMeasure(ScrollOrientation orientation)
        {
            if (_context != null)
            {
                if (IsVirtualizingContext)
                {
                    // We proactively clear elements laid out outside of the realization
                    // rect so that they are available for reuse during the current
                    // measure pass.
                    // This is useful during fast panning scenarios in which the realization
                    // window is constantly changing and we want to reuse elements from
                    // the end that's opposite to the panning direction.
                    DiscardElementsOutsideWindow(_context.RealizationRect, orientation);
                }
                else
                {
                    // If we are initialized with a non-virtualizing context, make sure that
                    // we have enough space to hold the bounds for all the elements.
                    int count = _context.ItemCount;
                    if (_realizedElementLayoutBounds.Count != count)
                    {
                        // Make sure there is enough space for the bounds.
                        // Note: We could optimize when the count becomes smaller, but keeping
                        // it always up to date is the simplest option for now.
                        _realizedElementLayoutBounds.Resize(count);
                    }
                }
            }
        }

        public int GetRealizedElementCount()
        {
            return IsVirtualizingContext ? _realizedElements.Count : _context.ItemCount;
        }

        public ILayoutable GetAt(int realizedIndex)
        {
            ILayoutable element;

            if (IsVirtualizingContext)
            {
                if (_realizedElements[realizedIndex] == null)
                {
                    // Sentinel. Create the element now since we need it.
                    int dataIndex = GetDataIndexFromRealizedRangeIndex(realizedIndex);
                    Logger.TryGet(LogEventLevel.Verbose, "Repeater")?.Log(this, "Creating element for sentinal with data index {Index}", dataIndex);
                    element = _context.GetOrCreateElementAt(
                        dataIndex,
                        ElementRealizationOptions.ForceCreate | ElementRealizationOptions.SuppressAutoRecycle);
                    _realizedElements[realizedIndex] = element;
                }
                else
                {
                    element = _realizedElements[realizedIndex];
                }
            }
            else
            {
                // realizedIndex and dataIndex are the same (everything is realized)
                element = _context.GetOrCreateElementAt(
                    realizedIndex,
                    ElementRealizationOptions.ForceCreate | ElementRealizationOptions.SuppressAutoRecycle);
            }

            return element;
        }

        public void Add(ILayoutable element, int dataIndex)
        {
            if (_realizedElements.Count == 0)
            {
                _firstRealizedDataIndex = dataIndex;
            }

            _realizedElements.Add(element);
            _realizedElementLayoutBounds.Add(default);
        }

        public void Insert(int realizedIndex, int dataIndex, ILayoutable element)
        {
            if (realizedIndex == 0)
            {
                _firstRealizedDataIndex = dataIndex;
            }

            _realizedElements.Insert(realizedIndex, element);

            // Set bounds to an invalid rect since we do not know it yet.
            _realizedElementLayoutBounds.Insert(realizedIndex, new Rect(-1, -1, -1, -1));
        }

        public void ClearRealizedRange(int realizedIndex, int count)
        {
            for (int i = 0; i < count; i++)
            {
                // Clear from the edges so that ItemsRepeater can optimize on maintaining
                // realized indices without walking through all the children every time.
                int index = realizedIndex == 0 ? realizedIndex + i : (realizedIndex + count - 1) - i;
                var elementRef = _realizedElements[index];

                if (elementRef != null)
                {
                    _context.RecycleElement(elementRef);
                }
            }

            int endIndex = realizedIndex + count;
            _realizedElements.RemoveRange(realizedIndex, endIndex - realizedIndex);
            _realizedElementLayoutBounds.RemoveRange(realizedIndex, endIndex - realizedIndex);

            if (realizedIndex == 0)
            {
                _firstRealizedDataIndex = _realizedElements.Count == 0 ?
                    -1 : _firstRealizedDataIndex + count;
            }
        }

        public void DiscardElementsOutsideWindow(bool forward, int startIndex)
        {
            // Remove layout elements that are outside the realized range.
            if (IsDataIndexRealized(startIndex))
            {
                int rangeIndex = GetRealizedRangeIndexFromDataIndex(startIndex);

                if (forward)
                {
                    ClearRealizedRange(rangeIndex, GetRealizedElementCount() - rangeIndex);
                }
                else
                {
                    ClearRealizedRange(0, rangeIndex + 1);
                }
            }
        }

        public void ClearRealizedRange() => ClearRealizedRange(0, GetRealizedElementCount());

        public Rect GetLayoutBoundsForDataIndex(int dataIndex)
        {
            int realizedIndex = GetRealizedRangeIndexFromDataIndex(dataIndex);
            return _realizedElementLayoutBounds[realizedIndex];
        }

        public void SetLayoutBoundsForDataIndex(int dataIndex, in Rect bounds)
        {
            int realizedIndex = GetRealizedRangeIndexFromDataIndex(dataIndex);
            _realizedElementLayoutBounds[realizedIndex] = bounds;
        }

        public Rect GetLayoutBoundsForRealizedIndex(int realizedIndex) => _realizedElementLayoutBounds[realizedIndex];

        public void SetLayoutBoundsForRealizedIndex(int realizedIndex, in Rect bounds)
        {
            _realizedElementLayoutBounds[realizedIndex] = bounds;
        }

        public bool IsDataIndexRealized(int index)
        {
            if (IsVirtualizingContext)
            {
                int realizedCount = GetRealizedElementCount();
                return
                    realizedCount > 0 &&
                    GetDataIndexFromRealizedRangeIndex(0) <= index &&
                    GetDataIndexFromRealizedRangeIndex(realizedCount - 1) >= index;
            }
            else
            {
                // Non virtualized - everything is realized
                return index >= 0 && index < _context.ItemCount;
            }
        }

        public bool IsIndexValidInData(int currentIndex) => (uint)currentIndex < _context.ItemCount;

        public ILayoutable GetRealizedElement(int dataIndex)
        {
            return IsVirtualizingContext ?
                GetAt(GetRealizedRangeIndexFromDataIndex(dataIndex)) :
                _context.GetOrCreateElementAt(
                    dataIndex,
                    ElementRealizationOptions.ForceCreate | ElementRealizationOptions.SuppressAutoRecycle);
        }

        public void EnsureElementRealized(bool forward, int dataIndex, string layoutId)
        {
            if (IsDataIndexRealized(dataIndex) == false)
            {
                var element = _context.GetOrCreateElementAt(
                    dataIndex,
                    ElementRealizationOptions.ForceCreate | ElementRealizationOptions.SuppressAutoRecycle);

                if (forward)
                {
                    Add(element, dataIndex);
                }
                else
                {
                    Insert(0, dataIndex, element);
                }

                Logger.TryGet(LogEventLevel.Verbose, "Repeater")?.Log(this, "{LayoutId}: Created element for index {index}", layoutId, dataIndex);
            }
        }

        public bool IsWindowConnected(in Rect window, ScrollOrientation orientation, bool scrollOrientationSameAsFlow)
        {
            bool intersects = false;

            if (_realizedElementLayoutBounds.Count > 0)
            {
                var firstElementBounds = GetLayoutBoundsForRealizedIndex(0);
                var lastElementBounds = GetLayoutBoundsForRealizedIndex(GetRealizedElementCount() - 1);

                var effectiveOrientation = scrollOrientationSameAsFlow ?
                    (orientation == ScrollOrientation.Vertical ? ScrollOrientation.Horizontal : ScrollOrientation.Vertical) :
                    orientation;

                var windowStart = effectiveOrientation == ScrollOrientation.Vertical ? window.Y : window.X;
                var windowEnd = effectiveOrientation == ScrollOrientation.Vertical ? window.Y + window.Height : window.X + window.Width;
                var firstElementStart = effectiveOrientation == ScrollOrientation.Vertical ? firstElementBounds.Y : firstElementBounds.X;
                var lastElementEnd = effectiveOrientation == ScrollOrientation.Vertical ? lastElementBounds.Y + lastElementBounds.Height : lastElementBounds.X + lastElementBounds.Width;

                intersects =
                    firstElementStart <= windowEnd &&
                    lastElementEnd >= windowStart;
            }

            return intersects;
        }

        public void DataSourceChanged(object source, NotifyCollectionChangedEventArgs args)
        {
            if (_realizedElements.Count > 0)
            {
                switch (args.Action)
                {
                    case NotifyCollectionChangedAction.Add:
                        {
                            OnItemsAdded(args.NewStartingIndex, args.NewItems.Count);
                        }
                        break;

                    case NotifyCollectionChangedAction.Replace:
                        {
                            int oldSize = args.OldItems.Count;
                            int newSize = args.NewItems.Count;
                            int oldStartIndex = args.OldStartingIndex;
                            int newStartIndex = args.NewStartingIndex;

                            if (oldSize == newSize &&
                                oldStartIndex == newStartIndex &&
                                IsDataIndexRealized(oldStartIndex) &&
                                IsDataIndexRealized(oldStartIndex + oldSize - 1))
                            {
                                // Straight up replace of n items within the realization window.
                                // Removing and adding might causes us to lose the anchor causing us
                                // to throw away all containers and start from scratch.
                                // Instead, we can just clear those items and set the element to
                                // null (sentinel) and let the next measure get new containers for them.
                                var startRealizedIndex = GetRealizedRangeIndexFromDataIndex(oldStartIndex);
                                for (int realizedIndex = startRealizedIndex; realizedIndex < startRealizedIndex + oldSize; realizedIndex++)
                                {
                                    var elementRef = _realizedElements[realizedIndex];

                                    if (elementRef != null)
                                    {
                                        _context.RecycleElement(elementRef);
                                        _realizedElements[realizedIndex] = null;
                                    }
                                }
                            }
                            else
                            {
                                OnItemsRemoved(oldStartIndex, oldSize);
                                OnItemsAdded(newStartIndex, newSize);
                            }
                        }
                        break;

                    case NotifyCollectionChangedAction.Remove:
                        {
                            OnItemsRemoved(args.OldStartingIndex, args.OldItems.Count);
                        }
                        break;

                    case NotifyCollectionChangedAction.Reset:
                        ClearRealizedRange();
                        break;

                    case NotifyCollectionChangedAction.Move:
                        int size = args.OldItems != null ? args.OldItems.Count : 1;
                        OnItemsRemoved(args.OldStartingIndex, size);
                        OnItemsAdded(args.NewStartingIndex, size);
                        break;
                }
            }
        }

        public int GetElementDataIndex(ILayoutable suggestedAnchor)
        {
            var it = _realizedElements.IndexOf(suggestedAnchor);
            return it != -1 ? GetDataIndexFromRealizedRangeIndex(it) : -1;
        }

        public int GetDataIndexFromRealizedRangeIndex(int rangeIndex)
        {
            return IsVirtualizingContext ? rangeIndex + _firstRealizedDataIndex : rangeIndex;
        }

        private int GetRealizedRangeIndexFromDataIndex(int dataIndex)
        {
            return IsVirtualizingContext ? dataIndex - _firstRealizedDataIndex : dataIndex;
        }

        private void DiscardElementsOutsideWindow(in Rect window, ScrollOrientation orientation)
        {
            // The following illustration explains the cutoff indices.
            // We will clear all the realized elements from both ends
            // up to the corresponding cutoff index.
            // '-' means the element is outside the cutoff range.
            // '*' means the element is inside the cutoff range and will be cleared.
            //
            // Window:
            //        |______________________________|
            // Realization range:
            // |*****----------------------------------*********|
            //      |                                  |
            //  frontCutoffIndex                backCutoffIndex
            //
            // Note that we tolerate at most one element outside of the window
            // because the FlowLayoutAlgorithm.Generate routine stops *after*
            // it laid out an element outside the realization window.
            // This is also convenient because it protects the anchor
            // during a BringIntoView operation during which the anchor may
            // not be in the realization window (in fact, the realization window
            // might be empty if the BringIntoView is issued before the first
            // layout pass).

            int realizedRangeSize = GetRealizedElementCount();
            int frontCutoffIndex = -1;
            int backCutoffIndex = realizedRangeSize;

            for (int i = 0;
                i < realizedRangeSize &&
                !Intersects(window, _realizedElementLayoutBounds[i], orientation);
                ++i)
            {
                ++frontCutoffIndex;
            }

            for (int i = realizedRangeSize - 1;
                i >= 0 &&
                !Intersects(window, _realizedElementLayoutBounds[i], orientation);
                --i)
            {
                --backCutoffIndex;
            }

            if (backCutoffIndex < realizedRangeSize - 1)
            {
                ClearRealizedRange(backCutoffIndex + 1, realizedRangeSize - backCutoffIndex - 1);
            }

            if (frontCutoffIndex > 0)
            {
                ClearRealizedRange(0, Math.Min(frontCutoffIndex, GetRealizedElementCount()));
            }
        }

        private static bool Intersects(in Rect lhs, in Rect rhs, ScrollOrientation orientation)
        {
            var lhsStart = orientation == ScrollOrientation.Vertical ? lhs.Y : lhs.X;
            var lhsEnd = orientation == ScrollOrientation.Vertical ? lhs.Y + lhs.Height : lhs.X + lhs.Width;
            var rhsStart = orientation == ScrollOrientation.Vertical ? rhs.Y : rhs.X;
            var rhsEnd = orientation == ScrollOrientation.Vertical ? rhs.Y + rhs.Height : rhs.X + rhs.Width;

            return lhsEnd >= rhsStart && lhsStart <= rhsEnd;
        }

        private void OnItemsAdded(int index, int count)
        {
            // Using the old indices here (before it was updated by the collection change)
            // if the insert data index is between the first and last realized data index, we need
            // to insert items.
            int lastRealizedDataIndex = _firstRealizedDataIndex + GetRealizedElementCount() - 1;
            int newStartingIndex = index;
            if (newStartingIndex >= _firstRealizedDataIndex &&
                newStartingIndex <= lastRealizedDataIndex)
            {
                // Inserted within the realized range
                int insertRangeStartIndex = newStartingIndex - _firstRealizedDataIndex;
                for (int i = 0; i < count; i++)
                {
                    // Insert null (sentinel) here instead of an element, that way we dont
                    // end up creating a lot of elements only to be thrown out in the next layout.
                    int insertRangeIndex = insertRangeStartIndex + i;
                    int dataIndex = newStartingIndex + i;
                    // This is to keep the contiguousness of the mapping
                    Insert(insertRangeIndex, dataIndex, null);
                }
            }
            else if (index <= _firstRealizedDataIndex)
            {
                // Items were inserted before the realized range.
                // We need to update m_firstRealizedDataIndex;
                _firstRealizedDataIndex += count;
            }
        }

        private void OnItemsRemoved(int index, int count)
        {
            int lastRealizedDataIndex = _firstRealizedDataIndex + _realizedElements.Count - 1;
            int startIndex = Math.Max(_firstRealizedDataIndex, index);
            int endIndex = Math.Min(lastRealizedDataIndex, index + count - 1);
            bool removeAffectsFirstRealizedDataIndex = (index <= _firstRealizedDataIndex);

            if (endIndex >= startIndex)
            {
                ClearRealizedRange(GetRealizedRangeIndexFromDataIndex(startIndex), endIndex - startIndex + 1);
            }

            if (removeAffectsFirstRealizedDataIndex &&
                _firstRealizedDataIndex != -1)
            {
                _firstRealizedDataIndex -= count;
            }
        }
    }
}
