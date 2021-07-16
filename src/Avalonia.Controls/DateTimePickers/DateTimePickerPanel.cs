using System;
using System.Globalization;
using System.Linq;
using Avalonia.Input;
using Avalonia.VisualTree;

namespace Avalonia.Controls.Primitives
{
    public enum DateTimePickerPanelType
    {
        Year,
        Month,
        Day,
        Hour,
        Minute,
        TimePeriod //AM or PM
    }

    public class DateTimePickerPanel : Panel, ILogicalScrollable
    {
        /// <summary>
        /// Defines the <see cref="ItemHeight"/> property
        /// </summary>
        public static readonly StyledProperty<double> ItemHeightProperty =
            AvaloniaProperty.Register<DateTimePickerPanel, double>(nameof(ItemHeight), 40.0);

        /// <summary>
        /// Defines the <see cref="PanelType"/> property
        /// </summary>
        public static readonly StyledProperty<DateTimePickerPanelType> PanelTypeProperty =
            AvaloniaProperty.Register<DateTimePickerPanel, DateTimePickerPanelType>(nameof(PanelType));

        /// <summary>
        /// Defines the <see cref="ItemFormat"/> property
        /// </summary>
        public static readonly StyledProperty<string> ItemFormatProperty =
            AvaloniaProperty.Register<DateTimePickerPanel, string>(nameof(ItemFormat), "yyyy");

        /// <summary>
        /// Defines the <see cref="ShouldLoop"/> property
        /// </summary>
        public static readonly StyledProperty<bool> ShouldLoopProperty =
            AvaloniaProperty.Register<DateTimePickerPanel, bool>(nameof(ShouldLoop));

        //Backing fields for properties
        private int _minimumValue = 1;
        private int _maximumValue = 2;
        private int _selectedValue = 1;
        private int _increment = 1;

        //Helper fields
        private int _selectedIndex = 0;
        private int _totalItems;
        private int _numItemsAboveBelowSelected;
        private int _range;
        private double _extentOne;
        private Size _extent;
        private Vector _offset;
        private bool _hasInit;
        private bool _suppressUpdateOffset;
        private ListBoxItem _pressedItem;

        public DateTimePickerPanel()
        {
            FormatDate = DateTime.Now;
            AddHandler(ListBoxItem.PointerPressedEvent, OnItemPointerDown, Avalonia.Interactivity.RoutingStrategies.Bubble);
            AddHandler(ListBoxItem.PointerReleasedEvent, OnItemPointerUp, Avalonia.Interactivity.RoutingStrategies.Bubble);
        }

        static DateTimePickerPanel()
        {
            FocusableProperty.OverrideDefaultValue<DateTimePickerPanel>(true);
            AffectsMeasure<DateTimePickerPanel>(ItemHeightProperty);
        }

        /// <summary>
        /// Gets or sets what this panel displays in date or time units
        /// </summary>
        public DateTimePickerPanelType PanelType
        {
            get => GetValue(PanelTypeProperty);
            set => SetValue(PanelTypeProperty, value);
        }

        /// <summary>
        /// Gets or sets the height of each item
        /// </summary>
        public double ItemHeight
        {
            get => GetValue(ItemHeightProperty);
            set => SetValue(ItemHeightProperty, value);
        }

        /// <summary>
        /// Gets or sets the string format for the items, using standard
        /// .net DateTime or TimeSpan formatting. Format must match panel type
        /// </summary>
        public string ItemFormat
        {
            get => GetValue(ItemFormatProperty);
            set => SetValue(ItemFormatProperty, value);
        }

        /// <summary>
        /// Gets or sets whether the panel should loop
        /// </summary>
        public bool ShouldLoop
        {
            get => GetValue(ShouldLoopProperty);
            set => SetValue(ShouldLoopProperty, value);
        }

        /// <summary>
        /// Gets or sets the minimum value
        /// </summary>
        public int MinimumValue
        {
            get => _minimumValue;
            set
            {
                if (value > MaximumValue)
                    throw new InvalidOperationException("Minimum cannot be greater than Maximum");
                _minimumValue = value;
                UpdateHelperInfo();
                var sel = CoerceSelected(SelectedValue);
                if (sel != SelectedValue)
                    SelectedValue = sel;
                UpdateItems();
                InvalidateArrange();
                RaiseScrollInvalidated(EventArgs.Empty);
            }
        }

        /// <summary>
        /// Gets or sets the maximum value
        /// </summary>
        public int MaximumValue
        {
            get => _maximumValue;
            set
            {
                if (value < MinimumValue)
                    throw new InvalidOperationException("Maximum cannot be less than Minimum");
                _maximumValue = value;
                UpdateHelperInfo();
                var sel = CoerceSelected(SelectedValue);
                if (sel != SelectedValue)
                    SelectedValue = sel;
                UpdateItems();
                InvalidateArrange();
                RaiseScrollInvalidated(EventArgs.Empty);
            }
        }

        /// <summary>
        /// Gets or sets the selected value
        /// </summary>
        public int SelectedValue
        {
            get => _selectedValue;
            set
            {
                if (value > MaximumValue || value < MinimumValue)
                    throw new ArgumentOutOfRangeException("SelectedValue");

                var sel = CoerceSelected(value);
                _selectedValue = sel;
                _selectedIndex = (value - MinimumValue) / Increment;

                if (!ShouldLoop)
                    CreateOrDestroyItems(Children);

                if (!_suppressUpdateOffset)
                    _offset = new Vector(0, ShouldLoop ? _selectedIndex * ItemHeight + (_extentOne * 50) :
                        _selectedIndex * ItemHeight);

                UpdateItems();
                InvalidateArrange();
                RaiseScrollInvalidated(EventArgs.Empty);

                SelectionChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// Gets or sets the increment
        /// </summary>
        public int Increment
        {
            get => _increment;
            set
            {
                if (value <= 0 || value > _range)
                    throw new ArgumentOutOfRangeException("Increment");
                _increment = value;
                UpdateHelperInfo();
                var sel = CoerceSelected(SelectedValue);
                if (sel != SelectedValue)
                    SelectedValue = sel;
                UpdateItems();
                InvalidateArrange();
                RaiseScrollInvalidated(EventArgs.Empty);
            }
        }

        //Used to help format the date (if applicable), for ex.,
        //if we're want to display the day of week, we need context
        //for the month/year, this is our context
        internal DateTime FormatDate { get; set; }

        public Vector Offset
        {
            get => _offset;
            set
            {
                var old = _offset;
                _offset = value;
                var dy = _offset.Y - old.Y;
                var children = Children;

                if (dy > 0) // Scroll Down
                {
                    int numCountsToMove = 0;
                    for (int i = 0; i < children.Count; i++)
                    {
                        if (children[i].Bounds.Bottom - dy < 0)
                            numCountsToMove++;
                        else
                            break;
                    }
                    children.MoveRange(0, numCountsToMove, children.Count);

                    var scrollHeight = _extent.Height - Viewport.Height;
                    if (ShouldLoop && value.Y >= scrollHeight - _extentOne)
                        _offset = new Vector(0, value.Y - (_extentOne * 50));
                }
                else if (dy < 0) // Scroll Up
                {
                    int numCountsToMove = 0;
                    for (int i = children.Count - 1; i >= 0; i--)
                    {
                        if (children[i].Bounds.Top - dy > Bounds.Height)
                            numCountsToMove++;
                        else
                            break;
                    }
                    children.MoveRange(children.Count - numCountsToMove, numCountsToMove, 0);
                    if (ShouldLoop && value.Y < _extentOne)
                        _offset = new Vector(0, value.Y + (_extentOne * 50));
                }

                //Setting selection will handle all invalidation
                var newSel = (Offset.Y / ItemHeight) % _totalItems;
                _suppressUpdateOffset = true;
                SelectedValue = (int)newSel * Increment + MinimumValue;
                _suppressUpdateOffset = false;
            }
        }

        public bool CanHorizontallyScroll { get => false; set { } }

        public bool CanVerticallyScroll { get => true; set { } }

        public bool IsLogicalScrollEnabled => true;

        public Size ScrollSize => new Size(0, ItemHeight);

        public Size PageScrollSize => new Size(0, ItemHeight * 4);

        public Size Extent => _extent;

        public Size Viewport => new Size(0, ItemHeight);

        public event EventHandler ScrollInvalidated;

        public event EventHandler SelectionChanged;

        protected override Size MeasureOverride(Size availableSize)
        {
            if (double.IsInfinity(availableSize.Width) ||
                double.IsInfinity(availableSize.Height))
                throw new InvalidOperationException("Panel must have finite height");

            if (!_hasInit)
                UpdateHelperInfo();

            double initY = (availableSize.Height / 2.0) - (ItemHeight / 2.0);
            _numItemsAboveBelowSelected = (int)Math.Ceiling(initY / ItemHeight) + 1;

            var children = Children;

            CreateOrDestroyItems(children);

            for (int i = 0; i < children.Count; i++)
                children[i].Measure(availableSize);

            if (!_hasInit)
            {
                UpdateItems();
                RaiseScrollInvalidated(EventArgs.Empty);
                _hasInit = true;
            }

            return availableSize;
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            if (Children.Count == 0)
                return base.ArrangeOverride(finalSize);

            var itemHgt = ItemHeight;
            var children = Children;
            Rect rc;
            double initY = (finalSize.Height / 2.0) - (itemHgt / 2.0);

            if (ShouldLoop)
            {
                var currentSet = Math.Truncate(Offset.Y / _extentOne);
                initY += (_extentOne * currentSet) + (_selectedIndex - _numItemsAboveBelowSelected) * ItemHeight;

                for (int i = 0; i < children.Count; i++)
                {
                    rc = new Rect(0, initY - Offset.Y, finalSize.Width, itemHgt);
                    children[i].Arrange(rc);
                    initY += itemHgt;
                }
            }
            else
            {
                var first = Math.Max(0, (_selectedIndex - _numItemsAboveBelowSelected));
                for (int i = 0; i < children.Count; i++)
                {
                    rc = new Rect(0, (initY + first * itemHgt) - Offset.Y, finalSize.Width, itemHgt);
                    children[i].Arrange(rc);
                    initY += itemHgt;
                }
            }

            return finalSize;
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Up:
                    ScrollUp();
                    e.Handled = true;
                    break;
                case Key.Down:
                    ScrollDown();
                    e.Handled = true;
                    break;
                case Key.PageUp:
                    ScrollUp(4);
                    e.Handled = true;
                    break;
                case Key.PageDown:
                    ScrollDown(4);
                    e.Handled = true;
                    break;
            }
            base.OnKeyDown(e);
        }

        /// <summary>
        /// Refreshes the content of the visible items
        /// </summary>
        public void RefreshItems()
        {
            UpdateItems();
        }

        /// <summary>
        /// Scrolls up the specified number of items
        /// </summary>
        public void ScrollUp(int numItems = 1)
        {
            var newY = Math.Max(Offset.Y - (numItems * ItemHeight), 0);
            Offset = new Vector(0, newY);
        }

        /// <summary>
        /// Scrolls down the specified number of items
        /// </summary>
        public void ScrollDown(int numItems = 1)
        {
            var scrollHeight = _extent.Height - Viewport.Height;
            var newY = Math.Min(Offset.Y + (numItems * ItemHeight), scrollHeight);
            Offset = new Vector(0, newY);
        }

        /// <summary>
        /// Updates helper fields used in various calculations
        /// </summary>
        private void UpdateHelperInfo()
        {
            _range = _maximumValue - _minimumValue + 1;
            _totalItems = (int)Math.Ceiling((double)_range / _increment);

            var itemHgt = ItemHeight;
            //If looping, measure 100x as many items as we actually have
            _extent = new Size(0, ShouldLoop ? _totalItems * itemHgt * 100 : _totalItems * itemHgt);

            //Height of 1 "set" of items
            _extentOne = _totalItems * itemHgt;
            _offset = new Vector(0, ShouldLoop ? _extentOne * 50 + _selectedIndex * itemHgt : _selectedIndex * itemHgt);

        }

        /// <summary>
        /// Ensures enough containers are visible in the viewport
        /// </summary>
        /// <param name="children"></param>
        private void CreateOrDestroyItems(Controls children)
        {
            int totalItemsInViewport = _numItemsAboveBelowSelected * 2 + 1;

            if (!ShouldLoop)
            {
                int numItemAboveSelect = _numItemsAboveBelowSelected;
                if (_selectedIndex - _numItemsAboveBelowSelected < 0)
                    numItemAboveSelect = _selectedIndex;
                int numItemBelowSelect = _numItemsAboveBelowSelected;
                if (_selectedIndex + _numItemsAboveBelowSelected >= _totalItems)
                    numItemBelowSelect = _totalItems - _selectedIndex - 1;

                totalItemsInViewport = numItemBelowSelect + numItemAboveSelect + 1;
            }

            while (children.Count < totalItemsInViewport)
            {
                children.Add(new ListBoxItem
                {
                    Height = ItemHeight,
                    Classes = new Classes("DateTimePickerItem", $"{PanelType}Item"),
                    VerticalContentAlignment = Avalonia.Layout.VerticalAlignment.Center,
                    Focusable = false
                });
            }
            if (children.Count > totalItemsInViewport)
            {
                var numToRemove = children.Count - totalItemsInViewport;
                children.RemoveRange(children.Count - numToRemove, numToRemove);
            }
        }

        /// <summary>
        /// Updates item content based on the current selection 
        /// and the panel type
        /// </summary>
        private void UpdateItems()
        {
            var children = Children;
            var min = MinimumValue;
            var panelType = PanelType;
            var selected = SelectedValue;
            var max = MaximumValue;

            int first;
            if (ShouldLoop)
            {
                first = (_selectedIndex - _numItemsAboveBelowSelected) % _totalItems;
                first = first < 0 ? min + (first + _totalItems) * Increment : min + first * Increment;
            }
            else
            {
                first = min + Math.Max(0, _selectedIndex - _numItemsAboveBelowSelected) * Increment;
            }

            for (int i = 0; i < children.Count; i++)
            {
                ListBoxItem item = (ListBoxItem)children[i];
                item.Content = FormatContent(first, panelType);
                item.Tag = first;
                item.IsSelected = first == selected;
                first += Increment;
                if (first > max)
                    first = min;
            }
        }

        private string FormatContent(int value, DateTimePickerPanelType panelType)
        {
            switch (panelType)
            {
                case DateTimePickerPanelType.Year:
                    return new DateTime(value, 1, 1).ToString(ItemFormat);
                case DateTimePickerPanelType.Month:
                    return new DateTime(FormatDate.Year, value, 1).ToString(ItemFormat);
                case DateTimePickerPanelType.Day:
                    return new DateTime(FormatDate.Year, FormatDate.Month, value).ToString(ItemFormat);
                case DateTimePickerPanelType.Hour:
                    return new TimeSpan(value, 0, 0).ToString(ItemFormat);
                case DateTimePickerPanelType.Minute:
                    return new TimeSpan(0, value, 0).ToString(ItemFormat);
                case DateTimePickerPanelType.TimePeriod:
                    return value == MinimumValue ? CultureInfo.CurrentCulture.DateTimeFormat.AMDesignator :
                        CultureInfo.CurrentCulture.DateTimeFormat.PMDesignator;
                default:
                        return "";
            }
        }

        /// <summary>
        /// Ensures the <see cref="SelectedValue"/> is within the bounds and
        /// follows the current Increment
        /// </summary>
        private int CoerceSelected(int newValue)
        {
            if (newValue < MinimumValue)
                return MinimumValue;
            if (newValue > MaximumValue)
                return MaximumValue;

            if (newValue % Increment != 0)
            {
                var items = Enumerable.Range(MinimumValue, MaximumValue + 1).Where(i => i % Increment == 0).ToList();
                var nearest = items.Aggregate((x, y) => Math.Abs(x - newValue) > Math.Abs(y - newValue) ? y : x);
                return items.IndexOf(nearest) * Increment;
            }
            return newValue;
        }

        private void OnItemPointerDown(object sender, PointerPressedEventArgs e)
        {
            if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            {
                _pressedItem = GetItemFromSource((IVisual)e.Source);
                e.Handled = true;
            }
        }

        private void OnItemPointerUp(object sender, PointerReleasedEventArgs e)
        {
            if (e.GetCurrentPoint(this).Properties.PointerUpdateKind == PointerUpdateKind.LeftButtonReleased &&
                _pressedItem != null)
            {
                SelectedValue = (int)GetItemFromSource((IVisual)e.Source).Tag;
                _pressedItem = null;
                e.Handled = true;
            }
        }

        //Helper to get ListBoxItem from pointerevent source
        private ListBoxItem GetItemFromSource(IVisual src)
        {
            var item = src;
            while (item != null && !(item is ListBoxItem))
            {
                item = item.VisualParent;
            }
            return (ListBoxItem)item;
        }

        public bool BringIntoView(IControl target, Rect targetRect) { return false; }

        public IControl GetControlInDirection(NavigationDirection direction, IControl from) { return null; }

        public void RaiseScrollInvalidated(EventArgs e)
        {
            ScrollInvalidated?.Invoke(this, e);
        }
    }
}
