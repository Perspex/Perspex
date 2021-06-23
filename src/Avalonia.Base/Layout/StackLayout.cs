// This source file is adapted from the WinUI project.
// (https://github.com/microsoft/microsoft-ui-xaml)
//
// Licensed to The Avalonia Project under MIT License, courtesy of The .NET Foundation.

using System;
using System.Collections.Specialized;
using Avalonia.Data;
using Avalonia.Logging;

namespace Avalonia.Layout
{
    /// <summary>
    /// Arranges elements into a single line (with spacing) that can be oriented horizontally or vertically.
    /// </summary>
    public class StackLayout : VirtualizingLayout, IFlowLayoutAlgorithmDelegates
    {
        /// <summary>
        /// Defines the <see cref="DisableVirtualization"/> property.
        /// </summary>
        public static readonly StyledProperty<bool> DisableVirtualizationProperty =
            AvaloniaProperty.Register<StackLayout, bool>(nameof(DisableVirtualization));

        /// <summary>
        /// Defines the <see cref="Orientation"/> property.
        /// </summary>
        public static readonly StyledProperty<Orientation> OrientationProperty =
            AvaloniaProperty.Register<StackLayout, Orientation>(nameof(Orientation), Orientation.Vertical);

        /// <summary>
        /// Defines the <see cref="Spacing"/> property.
        /// </summary>
        public static readonly StyledProperty<double> SpacingProperty =
            AvaloniaProperty.Register<StackLayout, double>(nameof(Spacing));

        private readonly OrientationBasedMeasures _orientation = new OrientationBasedMeasures();

        /// <summary>
        /// Initializes a new instance of the StackLayout class.
        /// </summary>
        public StackLayout()
        {
            LayoutId = "StackLayout";
        }

        /// <summary>
        /// Gets or sets a value indicating whether virtualization is disabled on the layout.
        /// </summary>
        public bool DisableVirtualization
        {
            get => GetValue(DisableVirtualizationProperty);
            set => SetValue(DisableVirtualizationProperty, value);
        }

        /// <summary>
        /// Gets or sets the axis along which items are laid out.
        /// </summary>
        /// <value>
        /// One of the enumeration values that specifies the axis along which items are laid out.
        /// The default is Vertical.
        /// </value>
        public Orientation Orientation
        {
            get => GetValue(OrientationProperty);
            set => SetValue(OrientationProperty, value);
        }

        /// <summary>
        /// Gets or sets a uniform distance (in pixels) between stacked items. It is applied in the
        /// direction of the StackLayout's Orientation.
        /// </summary>
        public double Spacing
        {
            get => GetValue(SpacingProperty);
            set => SetValue(SpacingProperty, value);
        }

        internal Rect GetExtent(
            Size availableSize,
            VirtualizingLayoutContext context,
            ILayoutable firstRealized,
            int firstRealizedItemIndex,
            Rect firstRealizedLayoutBounds,
            ILayoutable lastRealized,
            int lastRealizedItemIndex,
            Rect lastRealizedLayoutBounds)
        {
            var extent = new Rect();

            // Constants
            int itemsCount = context.ItemCount;
            var stackState = (StackLayoutState)context.LayoutState;
            double averageElementSize = GetAverageElementSize(availableSize, context, stackState) + Spacing;

            _orientation.SetMinorSize(ref extent, stackState.MaxArrangeBounds);
            _orientation.SetMajorSize(ref extent, Math.Max(0.0f, itemsCount * averageElementSize - Spacing));
            if (itemsCount > 0)
            {
                if (firstRealized != null)
                {
                    _orientation.SetMajorStart(
                        ref extent,
                        _orientation.MajorStart(firstRealizedLayoutBounds) - firstRealizedItemIndex * averageElementSize);
                    var remainingItems = itemsCount - lastRealizedItemIndex - 1;
                    _orientation.SetMajorSize(
                        ref extent,
                        _orientation.MajorEnd(lastRealizedLayoutBounds) -
                            _orientation.MajorStart(extent) + 
                            (remainingItems * averageElementSize));
                }
                else
                {
                    Logger.TryGet(LogEventLevel.Verbose, "Repeater")?.Log(this, "{LayoutId}: Estimating extent with no realized elements",
                        LayoutId);
                }
            }

            Logger.TryGet(LogEventLevel.Verbose, "Repeater")?.Log(this, "{LayoutId}: Extent is ({Size}). Based on average {Average}",
                LayoutId, extent.Size, averageElementSize);
            return extent;
        }

        internal void OnElementMeasured(
            ILayoutable element,
            int index,
            Size availableSize,
            Size measureSize,
            Size desiredSize,
            Size provisionalArrangeSize,
            VirtualizingLayoutContext context)
        {
            if (context is VirtualizingLayoutContext virtualContext)
            {
                var stackState = (StackLayoutState)virtualContext.LayoutState;
                var provisionalArrangeSizeWinRt = provisionalArrangeSize;
                stackState.OnElementMeasured(
                    index,
                    _orientation.Major(provisionalArrangeSizeWinRt),
                    _orientation.Minor(provisionalArrangeSizeWinRt));
            }
        }

        Size IFlowLayoutAlgorithmDelegates.Algorithm_GetMeasureSize(
            int index,
            Size availableSize,
            VirtualizingLayoutContext context) => availableSize;

        Size IFlowLayoutAlgorithmDelegates.Algorithm_GetProvisionalArrangeSize(
            int index,
            Size measureSize,
            Size desiredSize,
            VirtualizingLayoutContext context)
        {
            var measureSizeMinor = _orientation.Minor(measureSize);
            return _orientation.MinorMajorSize(
                !double.IsInfinity(measureSizeMinor) ?
                    Math.Max(measureSizeMinor, _orientation.Minor(desiredSize)) :
                    _orientation.Minor(desiredSize),
                _orientation.Major(desiredSize));
        }

        bool IFlowLayoutAlgorithmDelegates.Algorithm_ShouldBreakLine(int index, double remainingSpace) => true;

        FlowLayoutAnchorInfo IFlowLayoutAlgorithmDelegates.Algorithm_GetAnchorForRealizationRect(
            Size availableSize,
            VirtualizingLayoutContext context) => GetAnchorForRealizationRect(availableSize, context);

        FlowLayoutAnchorInfo IFlowLayoutAlgorithmDelegates.Algorithm_GetAnchorForTargetElement(
            int targetIndex,
            Size availableSize,
            VirtualizingLayoutContext context)
        {
            double offset = double.NaN;
            int index = -1;
            int itemsCount = context.ItemCount;

            if (targetIndex >= 0 && targetIndex < itemsCount)
            {
                index = targetIndex;
                var state = (StackLayoutState)context.LayoutState;
                double averageElementSize = GetAverageElementSize(availableSize, context, state) + Spacing;
                offset = index * averageElementSize + _orientation.MajorStart(state.FlowAlgorithm.LastExtent);
            }

            return new FlowLayoutAnchorInfo { Index = index, Offset = offset };
        }

        Rect IFlowLayoutAlgorithmDelegates.Algorithm_GetExtent(
            Size availableSize,
            VirtualizingLayoutContext context,
            ILayoutable firstRealized,
            int firstRealizedItemIndex,
            Rect firstRealizedLayoutBounds,
            ILayoutable lastRealized,
            int lastRealizedItemIndex,
            Rect lastRealizedLayoutBounds)
        {
            return GetExtent(
                availableSize,
                context,
                firstRealized,
                firstRealizedItemIndex,
                firstRealizedLayoutBounds,
                lastRealized,
                lastRealizedItemIndex,
                lastRealizedLayoutBounds);
        }

        void IFlowLayoutAlgorithmDelegates.Algorithm_OnElementMeasured(ILayoutable element, int index, Size availableSize, Size measureSize, Size desiredSize, Size provisionalArrangeSize, VirtualizingLayoutContext context)
        {
            OnElementMeasured(
                element,
                index,
                availableSize,
                measureSize,
                desiredSize,
                provisionalArrangeSize,
                context);
        }

        void IFlowLayoutAlgorithmDelegates.Algorithm_OnLineArranged(int startIndex, int countInLine, double lineSize, VirtualizingLayoutContext context)
        {
        }

        internal FlowLayoutAnchorInfo GetAnchorForRealizationRect(
            Size availableSize,
            VirtualizingLayoutContext context)
        {
            int anchorIndex = -1;
            double offset = double.NaN;

            // Constants
            int itemsCount = context.ItemCount;
            if (itemsCount > 0)
            {
                var realizationRect = context.RealizationRect;
                var state = (StackLayoutState)context.LayoutState;
                var lastExtent = state.FlowAlgorithm.LastExtent;

                double averageElementSize = GetAverageElementSize(availableSize, context, state) + Spacing;
                double realizationWindowOffsetInExtent = _orientation.MajorStart(realizationRect) - _orientation.MajorStart(lastExtent);
                double majorSize = _orientation.MajorSize(lastExtent) == 0 ? Math.Max(0.0, averageElementSize * itemsCount - Spacing) : _orientation.MajorSize(lastExtent);
                if (itemsCount > 0 &&
                    _orientation.MajorSize(realizationRect) >= 0 &&
                    // MajorSize = 0 will account for when a nested repeater is outside the realization rect but still being measured. Also,
                    // note that if we are measuring this repeater, then we are already realizing an element to figure out the size, so we could
                    // just keep that element alive. It also helps in XYFocus scenarios to have an element realized for XYFocus to find a candidate
                    // in the navigating direction.
                    realizationWindowOffsetInExtent + _orientation.MajorSize(realizationRect) >= 0 && realizationWindowOffsetInExtent <= majorSize)
                {
                    anchorIndex = (int) (realizationWindowOffsetInExtent / averageElementSize);
                    anchorIndex = Math.Max(0, Math.Min(itemsCount - 1, anchorIndex));
                    offset = anchorIndex* averageElementSize + _orientation.MajorStart(lastExtent);
                }
        }

            return new FlowLayoutAnchorInfo { Index = anchorIndex, Offset = offset, };
        }

        protected internal override void InitializeForContextCore(VirtualizingLayoutContext context)
        {
            var state = context.LayoutState;
            var stackState = state as StackLayoutState;
            
            if (stackState == null)
            {
                if (state != null)
                {
                    throw new InvalidOperationException("LayoutState must derive from StackLayoutState.");
                }

                // Custom deriving layouts could potentially be stateful.
                // If that is the case, we will just create the base state required by UniformGridLayout ourselves.
                stackState = new StackLayoutState();
            }

            stackState.InitializeForContext(context, this);
        }

        protected internal override void UninitializeForContextCore(VirtualizingLayoutContext context)
        {
            var stackState = (StackLayoutState)context.LayoutState;
            stackState.UninitializeForContext(context);
        }

        protected internal override Size MeasureOverride(VirtualizingLayoutContext context, Size availableSize)
        {
            ((StackLayoutState)context.LayoutState).OnMeasureStart();

            var desiredSize = GetFlowAlgorithm(context).Measure(
                availableSize,
                context,
                false,
                0,
                Spacing,
                int.MaxValue,
                _orientation.ScrollOrientation,
                DisableVirtualization,
                LayoutId);

            return new Size(desiredSize.Width, desiredSize.Height);
        }

        protected internal override Size ArrangeOverride(VirtualizingLayoutContext context, Size finalSize)
        {
            var value = GetFlowAlgorithm(context).Arrange(
               finalSize,
               context,
               false,
               FlowLayoutAlgorithm.LineAlignment.Start,
               LayoutId);

            return new Size(value.Width, value.Height);
        }

        protected internal override void OnItemsChangedCore(VirtualizingLayoutContext context, object source, NotifyCollectionChangedEventArgs args)
        {
            GetFlowAlgorithm(context).OnItemsSourceChanged(source, args, context);
            // Always invalidate layout to keep the view accurate.
            InvalidateLayout();
        }

        protected override void OnPropertyChanged<T>(AvaloniaPropertyChangedEventArgs<T> change)
        {
            if (change.Property == OrientationProperty)
            {
                var orientation = change.NewValue.GetValueOrDefault<Orientation>();

                //Note: For StackLayout Vertical Orientation means we have a Vertical ScrollOrientation.
                //Horizontal Orientation means we have a Horizontal ScrollOrientation.
                _orientation.ScrollOrientation = orientation == Orientation.Horizontal ? ScrollOrientation.Horizontal : ScrollOrientation.Vertical;
            }

            InvalidateLayout();
        }

        private double GetAverageElementSize(
            Size availableSize,
            VirtualizingLayoutContext context,
            StackLayoutState stackLayoutState)
        {
            double averageElementSize = 0;

            if (context.ItemCount > 0)
            {
                if (stackLayoutState.TotalElementsMeasured == 0)
                {
                    var tmpElement = context.GetOrCreateElementAt(0, ElementRealizationOptions.ForceCreate | ElementRealizationOptions.SuppressAutoRecycle);
                    stackLayoutState.FlowAlgorithm.MeasureElement(tmpElement, 0, availableSize, context);
                    context.RecycleElement(tmpElement);
                }

                averageElementSize = Math.Round(stackLayoutState.TotalElementSize / stackLayoutState.TotalElementsMeasured);
            }

            return averageElementSize;
        }

        private void InvalidateLayout() => InvalidateMeasure();

        private FlowLayoutAlgorithm GetFlowAlgorithm(VirtualizingLayoutContext context) => ((StackLayoutState)context.LayoutState).FlowAlgorithm;
    }
}
