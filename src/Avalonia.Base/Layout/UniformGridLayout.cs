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
    /// Defines constants that specify how items are aligned on the non-scrolling or non-virtualizing axis.
    /// </summary>
    public enum UniformGridLayoutItemsJustification
    {
        /// <summary>
        /// Items are aligned with the start of the row or column, with extra space at the end.
        /// Spacing between items does not change.
        /// </summary>
        Start = 0,

        /// <summary>
        /// Items are aligned in the center of the row or column, with extra space at the start and
        /// end. Spacing between items does not change.
        /// </summary>
        Center = 1,

        /// <summary>
        /// Items are aligned with the end of the row or column, with extra space at the start.
        /// Spacing between items does not change.
        /// </summary>
        End = 2,

        /// <summary>
        /// Items are aligned so that extra space is added evenly before and after each item.
        /// </summary>
        SpaceAround = 3,

        /// <summary>
        /// Items are aligned so that extra space is added evenly between adjacent items. No space
        /// is added at the start or end.
        /// </summary>
        SpaceBetween = 4,

        SpaceEvenly = 5,
    };

    /// <summary>
    /// Defines constants that specify how items are sized to fill the available space.
    /// </summary>
    public enum UniformGridLayoutItemsStretch
    {
        /// <summary>
        /// The item retains its natural size. Use of extra space is determined by the
        /// <see cref="UniformGridLayout.ItemsJustification"/> property.
        /// </summary>
        None = 0,

        /// <summary>
        /// The item is sized to fill the available space in the non-scrolling direction. Item size
        /// in the scrolling direction is not changed.
        /// </summary>
        Fill = 1,

        /// <summary>
        /// The item is sized to both fill the available space in the non-scrolling direction and
        /// maintain its aspect ratio.
        /// </summary>
        Uniform = 2,
    };

    /// <summary>
    /// Positions elements sequentially from left to right or top to bottom in a wrapping layout.
    /// </summary>
    public class UniformGridLayout : VirtualizingLayout, IFlowLayoutAlgorithmDelegates
    {
        /// <summary>
        /// Defines the <see cref="ItemsJustification"/> property.
        /// </summary>
        public static readonly StyledProperty<UniformGridLayoutItemsJustification> ItemsJustificationProperty =
            AvaloniaProperty.Register<UniformGridLayout, UniformGridLayoutItemsJustification>(nameof(ItemsJustification));

        /// <summary>
        /// Defines the <see cref="ItemsStretch"/> property.
        /// </summary>
        public static readonly StyledProperty<UniformGridLayoutItemsStretch> ItemsStretchProperty =
            AvaloniaProperty.Register<UniformGridLayout, UniformGridLayoutItemsStretch>(nameof(ItemsStretch));

        /// <summary>
        /// Defines the <see cref="MinColumnSpacing"/> property.
        /// </summary>
        public static readonly StyledProperty<double> MinColumnSpacingProperty =
            AvaloniaProperty.Register<UniformGridLayout, double>(nameof(MinColumnSpacing));

        /// <summary>
        /// Defines the <see cref="MinItemHeight"/> property.
        /// </summary>
        public static readonly StyledProperty<double> MinItemHeightProperty =
            AvaloniaProperty.Register<UniformGridLayout, double>(nameof(MinItemHeight));

        /// <summary>
        /// Defines the <see cref="MinItemWidth"/> property.
        /// </summary>
        public static readonly StyledProperty<double> MinItemWidthProperty =
            AvaloniaProperty.Register<UniformGridLayout, double>(nameof(MinItemWidth));

        /// <summary>
        /// Defines the <see cref="MinRowSpacing"/> property.
        /// </summary>
        public static readonly StyledProperty<double> MinRowSpacingProperty =
            AvaloniaProperty.Register<UniformGridLayout, double>(nameof(MinRowSpacing));

        /// <summary>
        /// Defines the <see cref="MaximumRowsOrColumnsProperty"/> property.
        /// </summary>
        public static readonly StyledProperty<int> MaximumRowsOrColumnsProperty =
            AvaloniaProperty.Register<UniformGridLayout, int>(nameof(MinItemWidth));

        /// <summary>
        /// Defines the <see cref="Orientation"/> property.
        /// </summary>
        public static readonly StyledProperty<Orientation> OrientationProperty =
            StackLayout.OrientationProperty.AddOwner<UniformGridLayout>();

        private readonly OrientationBasedMeasures _orientation = new OrientationBasedMeasures();
        private double _minItemWidth = double.NaN;
        private double _minItemHeight = double.NaN;
        private double _minRowSpacing;
        private double _minColumnSpacing;
        private UniformGridLayoutItemsJustification _itemsJustification;
        private UniformGridLayoutItemsStretch _itemsStretch;
        private int _maximumRowsOrColumns = int.MaxValue;

        /// <summary>
        /// Initializes a new instance of the <see cref="UniformGridLayout"/> class.
        /// </summary>
        public UniformGridLayout()
        {
            LayoutId = "UniformGridLayout";
        } 

        static UniformGridLayout()
        {
            OrientationProperty.OverrideDefaultValue<UniformGridLayout>(Orientation.Horizontal);
        }

        /// <summary>
        /// Gets or sets a value that indicates how items are aligned on the non-scrolling or non-
        /// virtualizing axis.
        /// </summary>
        /// <value>
        /// An enumeration value that indicates how items are aligned. The default is Start.
        /// </value>
        public UniformGridLayoutItemsJustification ItemsJustification
        {
            get => GetValue(ItemsJustificationProperty);
            set => SetValue(ItemsJustificationProperty, value);
        }

        /// <summary>
        /// Gets or sets a value that indicates how items are sized to fill the available space.
        /// </summary>
        /// <value>
        /// An enumeration value that indicates how items are sized to fill the available space.
        /// The default is None.
        /// </value>
        /// <remarks>
        /// This property enables adaptive layout behavior where the items are sized to fill the
        /// available space along the non-scrolling axis, and optionally maintain their aspect ratio.
        /// </remarks>
        public UniformGridLayoutItemsStretch ItemsStretch
        {
            get => GetValue(ItemsStretchProperty);
            set => SetValue(ItemsStretchProperty, value);
        }

        /// <summary>
        /// Gets or sets the minimum space between items on the horizontal axis.
        /// </summary>
        /// <remarks>
        /// The spacing may exceed this minimum value when <see cref="ItemsJustification"/> is set
        /// to SpaceEvenly, SpaceAround, or SpaceBetween.
        /// </remarks>
        public double MinColumnSpacing
        {
            get => GetValue(MinColumnSpacingProperty);
            set => SetValue(MinColumnSpacingProperty, value);
        }

        /// <summary>
        /// Gets or sets the minimum height of each item.
        /// </summary>
        /// <value>
        /// The minimum height (in pixels) of each item. The default is NaN, in which case the
        /// height of the first item is used as the minimum.
        /// </value>
        public double MinItemHeight
        {
            get => GetValue(MinItemHeightProperty);
            set => SetValue(MinItemHeightProperty, value);
        }

        /// <summary>
        /// Gets or sets the minimum width of each item.
        /// </summary>
        /// <value>
        /// The minimum width (in pixels) of each item. The default is NaN, in which case the width
        /// of the first item is used as the minimum.
        /// </value>
        public double MinItemWidth
        {
            get => GetValue(MinItemWidthProperty);
            set => SetValue(MinItemWidthProperty, value);
        }

        /// <summary>
        /// Gets or sets the minimum space between items on the vertical axis.
        /// </summary>
        /// <remarks>
        /// The spacing may exceed this minimum value when <see cref="ItemsJustification"/> is set
        /// to SpaceEvenly, SpaceAround, or SpaceBetween.
        /// </remarks>
        public double MinRowSpacing
        {
            get => GetValue(MinRowSpacingProperty);
            set => SetValue(MinRowSpacingProperty, value);
        }

        /// <summary>
        /// Gets or sets the maximum row or column count.
        /// </summary>
        public int MaximumRowsOrColumns
        {
            get => GetValue(MaximumRowsOrColumnsProperty);
            set => SetValue(MaximumRowsOrColumnsProperty, value);
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

        internal double LineSpacing => Orientation == Orientation.Horizontal ? _minRowSpacing : _minColumnSpacing;
        internal double MinItemSpacing => Orientation == Orientation.Horizontal ? _minColumnSpacing : _minRowSpacing;

        Size IFlowLayoutAlgorithmDelegates.Algorithm_GetMeasureSize(
            int index,
            Size availableSize,
            VirtualizingLayoutContext context)
        {
            var gridState = (UniformGridLayoutState)context.LayoutState;
            return new Size(gridState.EffectiveItemWidth, gridState.EffectiveItemHeight);
        }

        Size IFlowLayoutAlgorithmDelegates.Algorithm_GetProvisionalArrangeSize(
            int index,
            Size measureSize,
            Size desiredSize,
            VirtualizingLayoutContext context)
        {
            var gridState = (UniformGridLayoutState)context.LayoutState;
            return new Size(gridState.EffectiveItemWidth, gridState.EffectiveItemHeight);
        }

        bool IFlowLayoutAlgorithmDelegates.Algorithm_ShouldBreakLine(int index, double remainingSpace) => remainingSpace < 0;

        FlowLayoutAnchorInfo IFlowLayoutAlgorithmDelegates.Algorithm_GetAnchorForRealizationRect(
            Size availableSize,
            VirtualizingLayoutContext context)
        {
            Rect bounds = new Rect(double.NaN, double.NaN, double.NaN, double.NaN);
            int anchorIndex = -1;

            int itemsCount = context.ItemCount;
            var realizationRect = context.RealizationRect;
            if (itemsCount > 0 && _orientation.MajorSize(realizationRect) > 0)
            {
                var gridState = (UniformGridLayoutState)context.LayoutState;
                var lastExtent = gridState.FlowAlgorithm.LastExtent;
                var itemsPerLine = Math.Min( // note use of unsigned ints
                    Math.Max(1u, (uint)(_orientation.Minor(availableSize) / GetMinorSizeWithSpacing(context))),
                    Math.Max(1u, (uint)_maximumRowsOrColumns));
                var majorSize = (itemsCount / itemsPerLine) * GetMajorSizeWithSpacing(context);
                var realizationWindowStartWithinExtent = _orientation.MajorStart(realizationRect) - _orientation.MajorStart(lastExtent);
                if ((realizationWindowStartWithinExtent + _orientation.MajorSize(realizationRect)) >= 0 && realizationWindowStartWithinExtent <= majorSize)
                {
                    double offset = Math.Max(0.0, _orientation.MajorStart(realizationRect) - _orientation.MajorStart(lastExtent));
                    int anchorRowIndex = (int)(offset / GetMajorSizeWithSpacing(context));

                    anchorIndex = (int)Math.Max(0, Math.Min(itemsCount - 1, anchorRowIndex * itemsPerLine));
                    bounds = GetLayoutRectForDataIndex(availableSize, anchorIndex, lastExtent, context);
                }
            }

            return new FlowLayoutAnchorInfo
            {
                Index = anchorIndex,
                Offset = _orientation.MajorStart(bounds)
            };
        }

        FlowLayoutAnchorInfo IFlowLayoutAlgorithmDelegates.Algorithm_GetAnchorForTargetElement(
            int targetIndex,
            Size availableSize,
            VirtualizingLayoutContext context)
        {
            int index = -1;
            double offset = double.NaN;
            int count = context.ItemCount;
            if (targetIndex >= 0 && targetIndex < count)
            {
                int itemsPerLine = (int)Math.Min( // note use of unsigned ints
                    Math.Max(1u, (uint)(_orientation.Minor(availableSize) / GetMinorSizeWithSpacing(context))),
                    Math.Max(1u, _maximumRowsOrColumns));
                int indexOfFirstInLine = (targetIndex / itemsPerLine) * itemsPerLine;
                index = indexOfFirstInLine;
                var state = context.LayoutState as UniformGridLayoutState;
                offset = _orientation.MajorStart(GetLayoutRectForDataIndex(availableSize, indexOfFirstInLine, state.FlowAlgorithm.LastExtent, context));
            }

            return new FlowLayoutAnchorInfo
            {
                Index = index,
                Offset = offset
            };
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
            var extent = new Rect();


            // Constants
            int itemsCount = context.ItemCount;
            double availableSizeMinor = _orientation.Minor(availableSize);
            int itemsPerLine =
                (int)Math.Min( // note use of unsigned ints
                    Math.Max(1u, !double.IsInfinity(availableSizeMinor)
                        ? (uint)(availableSizeMinor / GetMinorSizeWithSpacing(context))
                        : (uint)itemsCount),
                Math.Max(1u, _maximumRowsOrColumns));
            double lineSize = GetMajorSizeWithSpacing(context);

            if (itemsCount > 0)
            {
                _orientation.SetMinorSize(
                    ref extent,
                    !double.IsInfinity(availableSizeMinor) && _itemsStretch == UniformGridLayoutItemsStretch.Fill ?
                    availableSizeMinor :
                    Math.Max(0.0, itemsPerLine * GetMinorSizeWithSpacing(context) - (double)MinItemSpacing));
                _orientation.SetMajorSize(
                    ref extent,
                    Math.Max(0.0, (itemsCount / itemsPerLine) * lineSize - (double)LineSpacing));

                if (firstRealized != null)
                {
                    _orientation.SetMajorStart(
                        ref extent,
                        _orientation.MajorStart(firstRealizedLayoutBounds) - (firstRealizedItemIndex / itemsPerLine) * lineSize);
                    int remainingItems = itemsCount - lastRealizedItemIndex - 1;
                    _orientation.SetMajorSize(
                        ref extent,
                        _orientation.MajorEnd(lastRealizedLayoutBounds) - _orientation.MajorStart(extent) + (remainingItems / itemsPerLine) * lineSize);
                }
                else
                {
                    Logger.TryGet(LogEventLevel.Verbose, "Repeater")?.Log(this, "{LayoutId}: Estimating extent with no realized elements", LayoutId);
                }
            }

            Logger.TryGet(LogEventLevel.Verbose, "Repeater")?.Log(this, "{LayoutId}: Extent is ({Size}). Based on lineSize {LineSize} and items per line {ItemsPerLine}",
                LayoutId, extent.Size, lineSize, itemsPerLine);
            return extent;
        }

        void IFlowLayoutAlgorithmDelegates.Algorithm_OnElementMeasured(ILayoutable element, int index, Size availableSize, Size measureSize, Size desiredSize, Size provisionalArrangeSize, VirtualizingLayoutContext context)
        {
        }

        void IFlowLayoutAlgorithmDelegates.Algorithm_OnLineArranged(int startIndex, int countInLine, double lineSize, VirtualizingLayoutContext context)
        {
        }

        protected internal override void InitializeForContextCore(VirtualizingLayoutContext context)
        {
            var state = context.LayoutState;
            var gridState = state as UniformGridLayoutState;
            
            if (gridState == null)
            {
                if (state != null)
                {
                    throw new InvalidOperationException("LayoutState must derive from UniformGridLayoutState.");
                }

                // Custom deriving layouts could potentially be stateful.
                // If that is the case, we will just create the base state required by UniformGridLayout ourselves.
                gridState = new UniformGridLayoutState();
            }

            gridState.InitializeForContext(context, this);
        }

        protected internal override void UninitializeForContextCore(VirtualizingLayoutContext context)
        {
            var gridState = (UniformGridLayoutState)context.LayoutState;
            gridState.UninitializeForContext(context);
        }

        protected internal override Size MeasureOverride(VirtualizingLayoutContext context, Size availableSize)
        {
            // Set the width and height on the grid state. If the user already set them then use the preset. 
            // If not, we have to measure the first element and get back a size which we're going to be using for the rest of the items.
            var gridState = (UniformGridLayoutState)context.LayoutState;
            gridState.EnsureElementSize(availableSize, context, _minItemWidth, _minItemHeight, _itemsStretch, Orientation, MinRowSpacing, MinColumnSpacing, _maximumRowsOrColumns);

            var desiredSize = GetFlowAlgorithm(context).Measure(
                availableSize,
                context,
                true,
                MinItemSpacing,
                LineSpacing,
                _maximumRowsOrColumns,
                _orientation.ScrollOrientation,
                false,
                LayoutId);

            // If after Measure the first item is in the realization rect, then we revoke grid state's ownership,
            // and only use the layout when to clear it when it's done.
            gridState.EnsureFirstElementOwnership(context);

            return desiredSize;
        }

        protected internal override Size ArrangeOverride(VirtualizingLayoutContext context, Size finalSize)
        {
            var value = GetFlowAlgorithm(context).Arrange(
               finalSize,
               context,
               true,
               (FlowLayoutAlgorithm.LineAlignment)_itemsJustification,
               LayoutId);
            return new Size(value.Width, value.Height);
        }

        protected internal override void OnItemsChangedCore(VirtualizingLayoutContext context, object source, NotifyCollectionChangedEventArgs args)
        {
            GetFlowAlgorithm(context).OnItemsSourceChanged(source, args, context);
            // Always invalidate layout to keep the view accurate.
            InvalidateLayout();

            var gridState = (UniformGridLayoutState)context.LayoutState;
            gridState.ClearElementOnDataSourceChange(context, args);
        }

        protected override void OnPropertyChanged<T>(AvaloniaPropertyChangedEventArgs<T> change)
        {
            if (change.Property == OrientationProperty)
            {
                var orientation = change.NewValue.GetValueOrDefault<Orientation>();

                //Note: For UniformGridLayout Vertical Orientation means we have a Horizontal ScrollOrientation. Horizontal Orientation means we have a Vertical ScrollOrientation.
                //i.e. the properties are the inverse of each other.
                var scrollOrientation = (orientation == Orientation.Horizontal) ? ScrollOrientation.Vertical : ScrollOrientation.Horizontal;
                _orientation.ScrollOrientation = scrollOrientation;
            }
            else if (change.Property == MinColumnSpacingProperty)
            {
                _minColumnSpacing = change.NewValue.GetValueOrDefault<double>();
            }
            else if (change.Property == MinRowSpacingProperty)
            {
                _minRowSpacing = change.NewValue.GetValueOrDefault<double>();
            }
            else if (change.Property == ItemsJustificationProperty)
            {
                _itemsJustification = change.NewValue.GetValueOrDefault<UniformGridLayoutItemsJustification>();
            }
            else if (change.Property == ItemsStretchProperty)
            {
                _itemsStretch = change.NewValue.GetValueOrDefault<UniformGridLayoutItemsStretch>();
            }
            else if (change.Property == MinItemWidthProperty)
            {
                _minItemWidth = change.NewValue.GetValueOrDefault<double>();
            }
            else if (change.Property == MinItemHeightProperty)
            {
                _minItemHeight = change.NewValue.GetValueOrDefault<double>();
            }
            else if (change.Property == MaximumRowsOrColumnsProperty)
            {
                _maximumRowsOrColumns = change.NewValue.GetValueOrDefault<int>();
            }

            InvalidateLayout();
        }

        private double GetMinorSizeWithSpacing(VirtualizingLayoutContext context)
        {
            var minItemSpacing = MinItemSpacing;
            var gridState = (UniformGridLayoutState)context.LayoutState;
            return _orientation.ScrollOrientation == ScrollOrientation.Vertical?
                gridState.EffectiveItemWidth + minItemSpacing :
                gridState.EffectiveItemHeight + minItemSpacing;
        }

        private double GetMajorSizeWithSpacing(VirtualizingLayoutContext context)
        {
            var lineSpacing = LineSpacing;
            var gridState = (UniformGridLayoutState)context.LayoutState;
            return _orientation.ScrollOrientation == ScrollOrientation.Vertical ?
                gridState.EffectiveItemHeight + lineSpacing :
                gridState.EffectiveItemWidth + lineSpacing;
        }

        Rect GetLayoutRectForDataIndex(
            Size availableSize,
            int index,
            Rect lastExtent,
            VirtualizingLayoutContext context)
        {
            int itemsPerLine = (int)Math.Min( //note use of unsigned ints
                Math.Max(1u, (uint)(_orientation.Minor(availableSize) / GetMinorSizeWithSpacing(context))),
                Math.Max(1u, _maximumRowsOrColumns));
            int rowIndex = (int)(index / itemsPerLine);
            int indexInRow = index - (rowIndex * itemsPerLine);

            var gridState = (UniformGridLayoutState)context.LayoutState;
            Rect bounds = _orientation.MinorMajorRect(
                indexInRow * GetMinorSizeWithSpacing(context) + _orientation.MinorStart(lastExtent),
                rowIndex * GetMajorSizeWithSpacing(context) + _orientation.MajorStart(lastExtent),
                _orientation.ScrollOrientation == ScrollOrientation.Vertical ? gridState.EffectiveItemWidth : gridState.EffectiveItemHeight,
                _orientation.ScrollOrientation == ScrollOrientation.Vertical ? gridState.EffectiveItemHeight : gridState.EffectiveItemWidth);

            return bounds;
        }

        private void InvalidateLayout() => InvalidateMeasure();

        private FlowLayoutAlgorithm GetFlowAlgorithm(VirtualizingLayoutContext context) => ((UniformGridLayoutState)context.LayoutState).FlowAlgorithm;
    }
}
