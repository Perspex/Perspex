// This source file is adapted from the WinUI project.
// (https://github.com/microsoft/microsoft-ui-xaml)
//
// Licensed to The Avalonia Project under MIT License, courtesy of The .NET Foundation.

using System;
using System.Collections.Specialized;
using Avalonia.Logging;

namespace Avalonia.Layout
{
    internal class FlowLayoutAlgorithm
    {
        private readonly OrientationBasedMeasures _orientation = new OrientationBasedMeasures();
        private readonly ElementManager _elementManager = new ElementManager();
        private Size _lastAvailableSize;
        private double _lastItemSpacing;
        private bool _collectionChangePending;
        private VirtualizingLayoutContext _context;
        private IFlowLayoutAlgorithmDelegates _algorithmCallbacks;
        private Rect _lastExtent;
        private int _firstRealizedDataIndexInsideRealizationWindow = -1;
        private int _lastRealizedDataIndexInsideRealizationWindow = -1;

        // If the scroll orientation is the same as the follow orientation
        // we will only have one line since we will never wrap. In that case
        // we do not want to align the line. We could potentially switch the
        // meaning of line alignment in this case, but I'll hold off on that
        // feature until someone asks for it - This is not a common scenario
        // anyway. 
        private bool _scrollOrientationSameAsFlow;

        public Rect LastExtent => _lastExtent;

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

        private Rect RealizationRect => IsVirtualizingContext ? _context.RealizationRect : new Rect(Size.Infinity);

        public void InitializeForContext(VirtualizingLayoutContext context, IFlowLayoutAlgorithmDelegates callbacks)
        {
            _algorithmCallbacks = callbacks;
            _context = context;
            _elementManager.SetContext(context);
        }

        public void UninitializeForContext(VirtualizingLayoutContext context)
        {
            if (IsVirtualizingContext)
            {
                // This layout is about to be detached. Let go of all elements
                // being held and remove the layout state from the context.
                _elementManager.ClearRealizedRange();
            }

            context.LayoutState = null;
        }

        public Size Measure(
            Size availableSize,
            VirtualizingLayoutContext context,
            bool isWrapping,
            double minItemSpacing,
            double lineSpacing,
            int maxItemsPerLine,
            ScrollOrientation orientation,
            bool disableVirtualization,
            string layoutId)
        {
            _orientation.ScrollOrientation = orientation;

            // If minor size is infinity, there is only one line and no need to align that line.
            _scrollOrientationSameAsFlow = double.IsInfinity(_orientation.Minor(availableSize));
            var realizationRect = RealizationRect;
            Logger.TryGet(LogEventLevel.Verbose, "Repeater")?.Log(this, "{LayoutId}: MeasureLayout Realization({Rect})",
                layoutId,
                realizationRect);

            var suggestedAnchorIndex = _context.RecommendedAnchorIndex;
            if (_elementManager.IsIndexValidInData(suggestedAnchorIndex))
            {
                var anchorRealized = _elementManager.IsDataIndexRealized(suggestedAnchorIndex);
                if (!anchorRealized)
                {
                    MakeAnchor(_context, suggestedAnchorIndex, availableSize);
                }
            }

            _elementManager.OnBeginMeasure(orientation);

            int anchorIndex = GetAnchorIndex(availableSize, isWrapping, minItemSpacing, layoutId);
            Generate(GenerateDirection.Forward, anchorIndex, availableSize, minItemSpacing, lineSpacing, maxItemsPerLine, disableVirtualization, layoutId);
            Generate(GenerateDirection.Backward, anchorIndex, availableSize, minItemSpacing, lineSpacing, maxItemsPerLine, disableVirtualization, layoutId);
            if (isWrapping && IsReflowRequired())
            {
                Logger.TryGet(LogEventLevel.Verbose, "Repeater")?.Log(this, "{LayoutId}: Reflow Pass", layoutId);
                var firstElementBounds = _elementManager.GetLayoutBoundsForRealizedIndex(0);
                _orientation.SetMinorStart(ref firstElementBounds, 0);
                _elementManager.SetLayoutBoundsForRealizedIndex(0, firstElementBounds);
                Generate(GenerateDirection.Forward, 0 /*anchorIndex*/, availableSize, minItemSpacing, lineSpacing, maxItemsPerLine, disableVirtualization, layoutId);
            }

            RaiseLineArranged();
            _collectionChangePending = false;
            _lastExtent = EstimateExtent(availableSize, layoutId);
            SetLayoutOrigin();

            return new Size(_lastExtent.Width, _lastExtent.Height);
        }

        public Size Arrange(
            Size finalSize,
            VirtualizingLayoutContext context,
            bool isWrapping,
            LineAlignment lineAlignment,
            string layoutId)
        {
            Logger.TryGet(LogEventLevel.Verbose, "Repeater")?.Log(this, "{LayoutId}: ArrangeLayout", layoutId);
            ArrangeVirtualizingLayout(finalSize, lineAlignment, isWrapping, layoutId);

            return new Size(
                Math.Max(finalSize.Width, _lastExtent.Width),
                Math.Max(finalSize.Height, _lastExtent.Height));
        }

        public void OnItemsSourceChanged(
            object source,
            NotifyCollectionChangedEventArgs args,
            VirtualizingLayoutContext context)
        {
            _elementManager.DataSourceChanged(source, args);
            _collectionChangePending = true;
        }

        public Size MeasureElement(
            ILayoutable element,
            int index,
            Size availableSize,
            VirtualizingLayoutContext context)
        {
            var measureSize = _algorithmCallbacks.Algorithm_GetMeasureSize(index, availableSize, context);
            element.Measure(measureSize);
            var provisionalArrangeSize = _algorithmCallbacks.Algorithm_GetProvisionalArrangeSize(index, measureSize, element.DesiredSize, context);
            _algorithmCallbacks.Algorithm_OnElementMeasured(element, index, availableSize, measureSize, element.DesiredSize, provisionalArrangeSize, context);

            return provisionalArrangeSize; 
        }

        private int GetAnchorIndex(
            Size availableSize,
            bool isWrapping,
            double minItemSpacing,
            string layoutId)
        {
            int anchorIndex = -1;
            var anchorPosition= new Point();
            var context = _context;

            if (!IsVirtualizingContext)
            {
                // Non virtualizing host, start generating from the element 0
                anchorIndex = context.ItemCount > 0 ? 0 : -1;
            }
            else
            {       
                bool isRealizationWindowConnected = _elementManager.IsWindowConnected(RealizationRect, _orientation.ScrollOrientation, _scrollOrientationSameAsFlow);
                // Item spacing and size in non-virtualizing direction change can cause elements to reflow
                // and get a new column position. In that case we need the anchor to be positioned in the 
                // correct column.
                bool needAnchorColumnRevaluation = isWrapping && (
                    _orientation.Minor(_lastAvailableSize) != _orientation.Minor(availableSize) ||
                    _lastItemSpacing != minItemSpacing ||
                    _collectionChangePending);

                var suggestedAnchorIndex = _context.RecommendedAnchorIndex;

                var isAnchorSuggestionValid = suggestedAnchorIndex >= 0 &&
                    _elementManager.IsDataIndexRealized(suggestedAnchorIndex);

                if (isAnchorSuggestionValid)
                {
                    Logger.TryGet(LogEventLevel.Verbose, "Repeater")?.Log(this, "{LayoutId}: Using suggested anchor {Anchor}", layoutId, suggestedAnchorIndex);
                    anchorIndex = _algorithmCallbacks.Algorithm_GetAnchorForTargetElement(
                        suggestedAnchorIndex,
                        availableSize,
                        context).Index;

                    if (_elementManager.IsDataIndexRealized(anchorIndex))
                    {
                        var anchorBounds = _elementManager.GetLayoutBoundsForDataIndex(anchorIndex);
                        if (needAnchorColumnRevaluation)
                        {
                            // We were provided a valid anchor, but its position might be incorrect because for example it is in
                            // the wrong column. We do know that the anchor is the first element in the row, so we can force the minor position
                            // to start at 0.
                            anchorPosition = _orientation.MinorMajorPoint(0, _orientation.MajorStart(anchorBounds));
                        }
                        else
                        {
                            anchorPosition = new Point(anchorBounds.X, anchorBounds.Y);
                        }
                    }
                    else if (anchorIndex >= 0)
                    {
                        // It is possible to end up in a situation during a collection change where GetAnchorForTargetElement returns an index
                        // which is not in the realized range. Eg. insert one item at index 0 for a grid layout. 
                        // SuggestedAnchor will be 1 (used to be 0) and GetAnchorForTargetElement will return 0 (left most item in row). However 0 is not in the
                        // realized range yet. In this case we realize the gap between the target anchor and the suggested anchor.
                        int firstRealizedDataIndex = _elementManager.GetDataIndexFromRealizedRangeIndex(0);

                        for (int i = firstRealizedDataIndex - 1; i >= anchorIndex; --i)
                        {
                            _elementManager.EnsureElementRealized(false /*forward*/, i, layoutId);
                        }

                        var anchorBounds = _elementManager.GetLayoutBoundsForDataIndex(suggestedAnchorIndex);
                        anchorPosition = _orientation.MinorMajorPoint(0, _orientation.MajorStart(anchorBounds));
                    }
                }
                else if (needAnchorColumnRevaluation || !isRealizationWindowConnected)
                {
                    if (needAnchorColumnRevaluation) { Logger.TryGet(LogEventLevel.Verbose, "Repeater")?.Log(this, "{LayoutId}: NeedAnchorColumnReevaluation", layoutId); }
                    if (!isRealizationWindowConnected) { Logger.TryGet(LogEventLevel.Verbose, "Repeater")?.Log(this, "{LayoutId}: Disconnected Window", layoutId); }

                    // The anchor is based on the realization window because a connected ItemsRepeater might intersect the realization window
                    // but not the visible window. In that situation, we still need to produce a valid anchor.
                    var anchorInfo = _algorithmCallbacks.Algorithm_GetAnchorForRealizationRect(availableSize, context);
                    anchorIndex = anchorInfo.Index;
                    anchorPosition = _orientation.MinorMajorPoint(0, anchorInfo.Offset);
                }
                else
                {
                    Logger.TryGet(LogEventLevel.Verbose, "Repeater")?.Log(this, "{LayoutId}: Connected Window - picking first realized element as anchor", layoutId);
                    // No suggestion - just pick first in realized range
                    anchorIndex = _elementManager.GetDataIndexFromRealizedRangeIndex(0);
                    var firstElementBounds = _elementManager.GetLayoutBoundsForRealizedIndex(0);
                    anchorPosition = new Point(firstElementBounds.X, firstElementBounds.Y);
                }
            }

            Logger.TryGet(LogEventLevel.Verbose, "Repeater")?.Log(this, "{LayoutId}: Picked anchor: {Anchor}", layoutId, anchorIndex);
            _firstRealizedDataIndexInsideRealizationWindow = _lastRealizedDataIndexInsideRealizationWindow = anchorIndex;
            if (_elementManager.IsIndexValidInData(anchorIndex))
            {
                if (!_elementManager.IsDataIndexRealized(anchorIndex))
                {
                    // Disconnected, throw everything and create new anchor
                    Logger.TryGet(LogEventLevel.Verbose, "Repeater")?.Log(this, "{LayoutId} Disconnected Window - throwing away all realized elements", layoutId);
                    _elementManager.ClearRealizedRange();

                    var anchor = _context.GetOrCreateElementAt(anchorIndex, ElementRealizationOptions.ForceCreate | ElementRealizationOptions.SuppressAutoRecycle);
                    _elementManager.Add(anchor, anchorIndex);
                }

                var anchorElement = _elementManager.GetRealizedElement(anchorIndex);
                var desiredSize = MeasureElement(anchorElement, anchorIndex, availableSize, _context);
                var layoutBounds = new Rect(anchorPosition.X, anchorPosition.Y, desiredSize.Width, desiredSize.Height);
                _elementManager.SetLayoutBoundsForDataIndex(anchorIndex, layoutBounds);

                Logger.TryGet(LogEventLevel.Verbose, "Repeater")?.Log(this, "{LayoutId}: Layout bounds of anchor {anchor} are ({Bounds})",
                    layoutId,
                    anchorIndex,
                    layoutBounds);
            }
            else
            {
                // Throw everything away
                Logger.TryGet(LogEventLevel.Verbose, "Repeater")?.Log(this, "{LayoutId} Anchor index is not valid - throwing away all realized elements",
                    layoutId);
                _elementManager.ClearRealizedRange();
            }

            // TODO: Perhaps we can track changes in the property setter
            _lastAvailableSize = availableSize;
            _lastItemSpacing = minItemSpacing;

            return anchorIndex;
        }

        private void Generate(
            GenerateDirection direction,
            int anchorIndex,
            Size availableSize,
            double minItemSpacing,
            double lineSpacing,
            int maxItemsPerLine,
            bool disableVirtualization,
            string layoutId)
        {
            if (anchorIndex != -1)
            {
                int step = (direction == GenerateDirection.Forward) ? 1 : -1;

                Logger.TryGet(LogEventLevel.Verbose, "Repeater")?.Log(this, "{LayoutId}: Generating {Direction} from anchor {Anchor}",
                    layoutId,
                    direction,
                    anchorIndex);

                int previousIndex = anchorIndex;
                int currentIndex = anchorIndex + step;
                var anchorBounds = _elementManager.GetLayoutBoundsForDataIndex(anchorIndex);
                var lineOffset = _orientation.MajorStart(anchorBounds);
                var lineMajorSize = _orientation.MajorSize(anchorBounds);
                var countInLine = 1;
                int count = 0;
                bool lineNeedsReposition = false;

                while (_elementManager.IsIndexValidInData(currentIndex) &&
                    (disableVirtualization || ShouldContinueFillingUpSpace(previousIndex, direction)))
                {
                    // Ensure layout element.
                    _elementManager.EnsureElementRealized(direction == GenerateDirection.Forward, currentIndex, layoutId);
                    var currentElement = _elementManager.GetRealizedElement(currentIndex);
                    var desiredSize = MeasureElement(currentElement, currentIndex, availableSize, _context);
                    ++count;

                    // Lay it out.
                    var previousElement = _elementManager.GetRealizedElement(previousIndex);
                    var currentBounds = new Rect(0, 0, desiredSize.Width, desiredSize.Height);
                    var previousElementBounds = _elementManager.GetLayoutBoundsForDataIndex(previousIndex);

                    if (direction == GenerateDirection.Forward)
                    {
                        double remainingSpace = _orientation.Minor(availableSize) - (_orientation.MinorStart(previousElementBounds) + _orientation.MinorSize(previousElementBounds) + minItemSpacing + _orientation.Minor(desiredSize));
                        if (countInLine >= maxItemsPerLine || _algorithmCallbacks.Algorithm_ShouldBreakLine(currentIndex, remainingSpace))
                        {
                            // No more space in this row. wrap to next row.
                            _orientation.SetMinorStart(ref currentBounds, 0);
                            _orientation.SetMajorStart(ref currentBounds, _orientation.MajorStart(previousElementBounds) + lineMajorSize + lineSpacing);

                            if (lineNeedsReposition)
                            {
                                // reposition the previous line (countInLine items)
                                for (int i = 0; i < countInLine; i++)
                                {
                                    var dataIndex = currentIndex - 1 - i;
                                    var bounds = _elementManager.GetLayoutBoundsForDataIndex(dataIndex);
                                    _orientation.SetMajorSize(ref bounds, lineMajorSize);
                                    _elementManager.SetLayoutBoundsForDataIndex(dataIndex, bounds);
                                }
                            }

                            // Setup for next line.
                            lineMajorSize = _orientation.MajorSize(currentBounds);
                            lineOffset = _orientation.MajorStart(currentBounds);
                            lineNeedsReposition = false;
                            countInLine = 1;
                        }
                        else
                        {
                            // More space is available in this row.
                            _orientation.SetMinorStart(ref currentBounds, _orientation.MinorStart(previousElementBounds) + _orientation.MinorSize(previousElementBounds) + minItemSpacing);
                            _orientation.SetMajorStart(ref currentBounds, lineOffset);
                            lineMajorSize = Math.Max(lineMajorSize, _orientation.MajorSize(currentBounds));
                            lineNeedsReposition = _orientation.MajorSize(previousElementBounds) != _orientation.MajorSize(currentBounds);
                            countInLine++;
                        }
                    }
                    else
                    {
                        // Backward 
                        double remainingSpace = _orientation.MinorStart(previousElementBounds) - (_orientation.Minor(desiredSize) + minItemSpacing);
                        if (countInLine >= maxItemsPerLine || _algorithmCallbacks.Algorithm_ShouldBreakLine(currentIndex, remainingSpace))
                        {
                            // Does not fit, wrap to the previous row
                            var availableSizeMinor = _orientation.Minor(availableSize);

                            _orientation.SetMinorStart(ref currentBounds, !double.IsInfinity(availableSizeMinor) ? availableSizeMinor - _orientation.Minor(desiredSize) : 0);
                            _orientation.SetMajorStart(ref currentBounds, lineOffset - _orientation.Major(desiredSize) - lineSpacing);

                            if (lineNeedsReposition)
                            {
                                var previousLineOffset = _orientation.MajorStart(_elementManager.GetLayoutBoundsForDataIndex(currentIndex + countInLine + 1));
                                // reposition the previous line (countInLine items)
                                for (int i = 0; i < countInLine; i++)
                                {
                                    var dataIndex = currentIndex + 1 + i;
                                    if (dataIndex != anchorIndex)
                                    {
                                        var bounds = _elementManager.GetLayoutBoundsForDataIndex(dataIndex);
                                        _orientation.SetMajorStart(ref bounds, previousLineOffset - lineMajorSize - lineSpacing);
                                        _orientation.SetMajorSize(ref bounds, lineMajorSize);
                                        _elementManager.SetLayoutBoundsForDataIndex(dataIndex, bounds);
                                        Logger.TryGet(LogEventLevel.Verbose, "Repeater")?.Log(this, "{LayoutId}: Corrected Layout bounds of element {Index} are ({Bounds})",
                                            layoutId,
                                            dataIndex,
                                            bounds);
                                    }
                                }
                            }

                            // Setup for next line.
                            lineMajorSize = _orientation.MajorSize(currentBounds);
                            lineOffset = _orientation.MajorStart(currentBounds);
                            lineNeedsReposition = false;
                            countInLine = 1;
                        }
                        else
                        {
                            // Fits in this row. put it in the previous position
                            _orientation.SetMinorStart(ref currentBounds, _orientation.MinorStart(previousElementBounds) - _orientation.Minor(desiredSize) - minItemSpacing);
                            _orientation.SetMajorStart(ref currentBounds, lineOffset);
                            lineMajorSize = Math.Max(lineMajorSize, _orientation.MajorSize(currentBounds));
                            lineNeedsReposition = _orientation.MajorSize(previousElementBounds) != _orientation.MajorSize(currentBounds);
                            countInLine++;
                        }
                    }

                    _elementManager.SetLayoutBoundsForDataIndex(currentIndex, currentBounds);

                    Logger.TryGet(LogEventLevel.Verbose, "Repeater")?.Log(this, "{LayoutId}: Layout bounds of element {Index} are ({Bounds}).",
                        layoutId,
                        currentIndex,
                        currentBounds);
                    previousIndex = currentIndex;
                    currentIndex += step;
                }

                // If we did not reach the top or bottom of the extent, we realized one 
                // extra item before we knew we were outside the realization window. Do not
                // account for that element in the indices inside the realization window.
                if (direction == GenerateDirection.Forward)
                {
                    int dataCount = _context.ItemCount;
                    _lastRealizedDataIndexInsideRealizationWindow = previousIndex == dataCount - 1 ? dataCount - 1 : previousIndex - 1;
                    _lastRealizedDataIndexInsideRealizationWindow = Math.Max(0, _lastRealizedDataIndexInsideRealizationWindow);
                }
                else
                {
                    int dataCount = _context.ItemCount;
                    _firstRealizedDataIndexInsideRealizationWindow = previousIndex == 0 ? 0 : previousIndex + 1;
                    _firstRealizedDataIndexInsideRealizationWindow = Math.Min(dataCount - 1, _firstRealizedDataIndexInsideRealizationWindow);
                }

                _elementManager.DiscardElementsOutsideWindow(direction == GenerateDirection.Forward, currentIndex);
            }
        }

        private void MakeAnchor(
            VirtualizingLayoutContext context,
            int index,
            Size availableSize)
        {
            _elementManager.ClearRealizedRange();
            // FlowLayout requires that the anchor is the first element in the row.
            var internalAnchor = _algorithmCallbacks.Algorithm_GetAnchorForTargetElement(index, availableSize, context);

            // No need to set the position of the anchor.
            // (0,0) is fine for now since the extent can
            // grow in any direction.
            for (int dataIndex = internalAnchor.Index; dataIndex < index + 1; ++dataIndex)
            {
                var element = context.GetOrCreateElementAt(dataIndex, ElementRealizationOptions.ForceCreate | ElementRealizationOptions.SuppressAutoRecycle);
                element.Measure(_algorithmCallbacks.Algorithm_GetMeasureSize(dataIndex, availableSize, context));
                _elementManager.Add(element, dataIndex);
            }
        }

        private bool IsReflowRequired()
        {
            // If first element is realized and is not at the very beginning we need to reflow.
            return
                _elementManager.GetRealizedElementCount() > 0 &&
                _elementManager.GetDataIndexFromRealizedRangeIndex(0) == 0 &&
                _orientation.MinorStart(_elementManager.GetLayoutBoundsForRealizedIndex(0)) != 0;
        }

        private bool ShouldContinueFillingUpSpace(
            int index,
            GenerateDirection direction)
        {
            bool shouldContinue = false;
            if (!IsVirtualizingContext)
            {
                shouldContinue = true;
            }
            else
            {
                var realizationRect = _context.RealizationRect;
                var elementBounds = _elementManager.GetLayoutBoundsForDataIndex(index);

                var elementMajorStart = _orientation.MajorStart(elementBounds);
                var elementMajorEnd = _orientation.MajorEnd(elementBounds);
                var rectMajorStart = _orientation.MajorStart(realizationRect);
                var rectMajorEnd = _orientation.MajorEnd(realizationRect);

                var elementMinorStart = _orientation.MinorStart(elementBounds);
                var elementMinorEnd = _orientation.MinorEnd(elementBounds);
                var rectMinorStart = _orientation.MinorStart(realizationRect);
                var rectMinorEnd = _orientation.MinorEnd(realizationRect);

                // Ensure that both minor and major directions are taken into consideration so that if the scrolling direction
                // is the same as the flow direction we still stop at the end of the viewport rectangle.
                shouldContinue =
                    (direction == GenerateDirection.Forward && elementMajorStart < rectMajorEnd && elementMinorStart < rectMinorEnd) ||
                    (direction == GenerateDirection.Backward && elementMajorEnd > rectMajorStart && elementMinorEnd > rectMinorStart);
            }

            return shouldContinue;
        }

        private Rect EstimateExtent(Size availableSize, string layoutId)
        {
            ILayoutable firstRealizedElement = null;
            Rect firstBounds = new Rect();
            ILayoutable lastRealizedElement = null;
            Rect lastBounds = new Rect();
            int firstDataIndex = -1;
            int lastDataIndex = -1;

            if (_elementManager.GetRealizedElementCount() > 0)
            {
                firstRealizedElement = _elementManager.GetAt(0);
                firstBounds = _elementManager.GetLayoutBoundsForRealizedIndex(0);
                firstDataIndex = _elementManager.GetDataIndexFromRealizedRangeIndex(0);;

                int last = _elementManager.GetRealizedElementCount() - 1;
                lastRealizedElement = _elementManager.GetAt(last);
                lastDataIndex = _elementManager.GetDataIndexFromRealizedRangeIndex(last);
                lastBounds = _elementManager.GetLayoutBoundsForRealizedIndex(last);
            }

            Rect extent = _algorithmCallbacks.Algorithm_GetExtent(
                availableSize,
                _context,
                firstRealizedElement,
                firstDataIndex,
                firstBounds,
                lastRealizedElement,
                lastDataIndex,
                lastBounds);

            Logger.TryGet(LogEventLevel.Verbose, "Repeater")?.Log(this, "{LayoutId} Extent: ({Bounds})", layoutId, extent);
            return extent;
        }

        private void RaiseLineArranged()
        {
            var realizationRect = RealizationRect;
            if (realizationRect.Width != 0.0f || realizationRect.Height != 0.0f)
            {
                int realizedElementCount = _elementManager.GetRealizedElementCount();
                if (realizedElementCount > 0)
                {
                    int countInLine = 0;
                    var previousElementBounds = _elementManager.GetLayoutBoundsForDataIndex(_firstRealizedDataIndexInsideRealizationWindow);
                    var currentLineOffset = _orientation.MajorStart(previousElementBounds);
                    var currentLineSize = _orientation.MajorSize(previousElementBounds);
                    for (int currentDataIndex = _firstRealizedDataIndexInsideRealizationWindow; currentDataIndex <= _lastRealizedDataIndexInsideRealizationWindow; currentDataIndex++)
                    {
                        var currentBounds = _elementManager.GetLayoutBoundsForDataIndex(currentDataIndex);
                        if (_orientation.MajorStart(currentBounds) != currentLineOffset)
                        {
                            // Staring a new line
                            _algorithmCallbacks.Algorithm_OnLineArranged(currentDataIndex - countInLine, countInLine, currentLineSize, _context);
                            countInLine = 0;
                            currentLineOffset = _orientation.MajorStart(currentBounds);
                            currentLineSize = 0;
                        }

                        currentLineSize = Math.Max(currentLineSize, _orientation.MajorSize(currentBounds));
                        countInLine++;
                        previousElementBounds = currentBounds;
                    }

                    // Raise for the last line.
                    _algorithmCallbacks.Algorithm_OnLineArranged(_lastRealizedDataIndexInsideRealizationWindow - countInLine + 1, countInLine, currentLineSize, _context);
                }
            }
        }

        private void ArrangeVirtualizingLayout(
            Size finalSize,
            LineAlignment lineAlignment,
            bool isWrapping,
            string layoutId)
        {
            // Walk through the realized elements one line at a time and 
            // align them, Then call element.Arrange with the arranged bounds.
            int realizedElementCount = _elementManager.GetRealizedElementCount();
            if (realizedElementCount > 0)
            {
                var countInLine = 1;
                var previousElementBounds = _elementManager.GetLayoutBoundsForRealizedIndex(0);
                var currentLineOffset = _orientation.MajorStart(previousElementBounds);
                var spaceAtLineStart = _orientation.MinorStart(previousElementBounds);
                var spaceAtLineEnd = 0.0;
                var currentLineSize = _orientation.MajorSize(previousElementBounds);
                for (int i = 1; i < realizedElementCount; i++)
                {
                    var currentBounds = _elementManager.GetLayoutBoundsForRealizedIndex(i);
                    if (_orientation.MajorStart(currentBounds) != currentLineOffset)
                    {
                        spaceAtLineEnd = _orientation.Minor(finalSize) - _orientation.MinorStart(previousElementBounds) - _orientation.MinorSize(previousElementBounds);
                        PerformLineAlignment(i - countInLine, countInLine, spaceAtLineStart, spaceAtLineEnd, currentLineSize, lineAlignment, isWrapping, finalSize, layoutId);
                        spaceAtLineStart = _orientation.MinorStart(currentBounds);
                        countInLine = 0;
                        currentLineOffset = _orientation.MajorStart(currentBounds);
                        currentLineSize = 0;
                    }

                    countInLine++; // for current element
                    currentLineSize = Math.Max(currentLineSize, _orientation.MajorSize(currentBounds));
                    previousElementBounds = currentBounds;
                }

                // Last line - potentially have a property to customize
                // aligning the last line or not.
                if (countInLine > 0)
                {
                    var spaceAtEnd = _orientation.Minor(finalSize) - _orientation.MinorStart(previousElementBounds) - _orientation.MinorSize(previousElementBounds);
                    PerformLineAlignment(realizedElementCount - countInLine, countInLine, spaceAtLineStart, spaceAtEnd, currentLineSize, lineAlignment, isWrapping, finalSize, layoutId);
                }
            }
        }

        // Align elements within a line. Note that this does not modify LayoutBounds. So if we get
        // repeated measures, the LayoutBounds remain the same in each layout.
        private void PerformLineAlignment(
            int lineStartIndex,
            int countInLine,
            double spaceAtLineStart,
            double spaceAtLineEnd,
            double lineSize,
            LineAlignment lineAlignment,
            bool isWrapping,
            Size finalSize,
            string layoutId)
        {
            for (int rangeIndex = lineStartIndex; rangeIndex < lineStartIndex + countInLine; ++rangeIndex)
            {
                var bounds = _elementManager.GetLayoutBoundsForRealizedIndex(rangeIndex);
                _orientation.SetMajorSize(ref bounds, lineSize);

                if (!_scrollOrientationSameAsFlow)
                {
                    // Note: Space at start could potentially be negative
                    if (spaceAtLineStart != 0 || spaceAtLineEnd != 0)
                    {
                        var totalSpace = spaceAtLineStart + spaceAtLineEnd;
                        var minorStart = _orientation.MinorStart(bounds);
                        switch (lineAlignment)
                        {
                            case LineAlignment.Start:
                                {
                                    _orientation.SetMinorStart(ref bounds, minorStart - spaceAtLineStart);
                                    break;
                                }

                            case LineAlignment.End:
                                {
                                    _orientation.SetMinorStart(ref bounds, minorStart + spaceAtLineEnd);
                                    break;
                                }

                            case LineAlignment.Center:
                                {
                                    _orientation.SetMinorStart(ref bounds, (minorStart - spaceAtLineStart) + (totalSpace / 2));
                                    break;
                                }

                            case LineAlignment.SpaceAround:
                                {
                                    var interItemSpace = countInLine >= 1 ? totalSpace / (countInLine * 2) : 0;
                                    _orientation.SetMinorStart(
                                        ref bounds, 
                                        (minorStart - spaceAtLineStart) + (interItemSpace * ((rangeIndex - lineStartIndex + 1) * 2 - 1)));
                                    break;
                                }

                            case LineAlignment.SpaceBetween:
                                {
                                    var interItemSpace = countInLine > 1 ? totalSpace / (countInLine - 1) : 0;
                                    _orientation.SetMinorStart(
                                        ref bounds,
                                        (minorStart - spaceAtLineStart) + (interItemSpace * (rangeIndex - lineStartIndex)));
                                    break;
                                }

                            case LineAlignment.SpaceEvenly:
                                {
                                    var interItemSpace = countInLine >= 1 ? totalSpace / (countInLine + 1) : 0;
                                    _orientation.SetMinorStart(
                                        ref bounds,
                                        (minorStart - spaceAtLineStart) + (interItemSpace * (rangeIndex - lineStartIndex + 1)));
                                    break;
                                }
                        }
                    }
                }

                bounds = bounds.Translate(-_lastExtent.Position);

                if (!isWrapping)
                {
                    _orientation.SetMinorSize(
                        ref bounds,
                        Math.Max(_orientation.MinorSize(bounds), _orientation.Minor(finalSize)));
                }

                var element = _elementManager.GetAt(rangeIndex);

                Logger.TryGet(LogEventLevel.Verbose, "Repeater")?.Log(this, "{LayoutId}: Arranging element {Index} at ({Bounds})",
                    layoutId,
                    _elementManager.GetDataIndexFromRealizedRangeIndex(rangeIndex),
                    bounds);
                element.Arrange(bounds);
            }
        }

        private void SetLayoutOrigin()
        {
            if (IsVirtualizingContext)
            {
                _context.LayoutOrigin = new Point(_lastExtent.X, _lastExtent.Y);
            }
        }

        public ILayoutable GetElementIfRealized(int dataIndex)
        {
            if (_elementManager.IsDataIndexRealized(dataIndex))
            {
                return _elementManager.GetRealizedElement(dataIndex);
            }

            return null;
        }

        public bool TryAddElement0(ILayoutable element)
        {
            if (_elementManager.GetRealizedElementCount() == 0)
            {
                _elementManager.Add(element, 0);
                return true;
            }

            return false;
        }

        public enum LineAlignment
        {
            Start,
            Center,
            End,
            SpaceAround,
            SpaceBetween,
            SpaceEvenly,
        }

        private enum GenerateDirection
        {
            Forward,
            Backward,
        }
    }
}
