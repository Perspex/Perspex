using Avalonia.Controls.Primitives;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Interactivity;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace Avalonia.Controls
{
    /// <summary>
    /// Defines the presenter used for selecting a date for a 
    /// <see cref="DatePicker"/>
    /// </summary>
    public class DatePickerPresenter : PickerPresenterBase
    {
        /// <summary>
        /// Defines the <see cref="Date"/> Property
        /// </summary>
        public static readonly DirectProperty<DatePickerPresenter, DateTimeOffset> DateProperty =
            AvaloniaProperty.RegisterDirect<DatePickerPresenter, DateTimeOffset>(nameof(Date),
                x => x.Date, (x, v) => x.Date = v);

        /// <summary>
        /// Defines the <see cref="DayFormat"/> Property
        /// </summary>
        public static readonly DirectProperty<DatePickerPresenter, string> DayFormatProperty =
            DatePicker.DayFormatProperty.AddOwner<DatePickerPresenter>(x =>
            x.DayFormat, (x, v) => x.DayFormat = v);

        /// <summary>
        /// Defines the <see cref="DayVisible"/> Property
        /// </summary>
        public static readonly DirectProperty<DatePickerPresenter, bool> DayVisibleProperty =
            DatePicker.DayVisibleProperty.AddOwner<DatePickerPresenter>(x =>
            x.DayVisible, (x, v) => x.DayVisible = v);

        /// <summary>
        /// Defines the <see cref="MaxYear"/> Property
        /// </summary>
        public static readonly DirectProperty<DatePickerPresenter, DateTimeOffset> MaxYearProperty =
            DatePicker.MaxYearProperty.AddOwner<DatePickerPresenter>(x =>
            x.MaxYear, (x, v) => x.MaxYear = v);

        /// <summary>
        /// Defines the <see cref="MinYear"/> Property
        /// </summary>
        public static readonly DirectProperty<DatePickerPresenter, DateTimeOffset> MinYearProperty =
            DatePicker.MinYearProperty.AddOwner<DatePickerPresenter>(x =>
            x.MinYear, (x, v) => x.MinYear = v);

        /// <summary>
        /// Defines the <see cref="MonthFormat"/> Property
        /// </summary>
        public static readonly DirectProperty<DatePickerPresenter, string> MonthFormatProperty =
            DatePicker.MonthFormatProperty.AddOwner<DatePickerPresenter>(x =>
            x.MonthFormat, (x, v) => x.MonthFormat = v);

        /// <summary>
        /// Defines the <see cref="MonthVisible"/> Property
        /// </summary>
        public static readonly DirectProperty<DatePickerPresenter, bool> MonthVisibleProperty =
            DatePicker.MonthVisibleProperty.AddOwner<DatePickerPresenter>(x =>
            x.MonthVisible, (x, v) => x.MonthVisible = v);

        /// <summary>
        /// Defines the <see cref="YearFormat"/> Property
        /// </summary>
        public static readonly DirectProperty<DatePickerPresenter, string> YearFormatProperty =
            DatePicker.YearFormatProperty.AddOwner<DatePickerPresenter>(x =>
            x.YearFormat, (x, v) => x.YearFormat = v);

        /// <summary>
        /// Defines the <see cref="YearVisible"/> Property
        /// </summary>
        public static readonly DirectProperty<DatePickerPresenter, bool> YearVisibleProperty =
            DatePicker.YearVisibleProperty.AddOwner<DatePickerPresenter>(x =>
            x.YearVisible, (x, v) => x.YearVisible = v);

        // Template Items
        private Grid _pickerContainer;
        private Button _acceptButton;
        private Button _dismissButton;
        private Rectangle _spacer1;
        private Rectangle _spacer2;
        private Panel _monthHost;
        private Panel _yearHost;
        private Panel _dayHost;
        private DateTimePickerPanel _monthSelector;
        private DateTimePickerPanel _yearSelector;
        private DateTimePickerPanel _daySelector;
        private Button _monthUpButton;
        private Button _dayUpButton;
        private Button _yearUpButton;
        private Button _monthDownButton;
        private Button _dayDownButton;
        private Button _yearDownButton;

        private DateTimeOffset _date;
        private string _dayFormat = "%d";
        private bool _dayVisible = true;
        private DateTimeOffset _maxYear;
        private DateTimeOffset _minYear;
        private string _monthFormat = "MMMM";
        private bool _monthVisible = true;
        private string _yearFormat = "yyyy";
        private bool _yearVisible = true;
        private DateTimeOffset _syncDate;

        private readonly GregorianCalendar _calendar;
        private bool _suppressUpdateSelection;

        public DatePickerPresenter()
        {
            var now = DateTimeOffset.Now;
            _minYear = new DateTimeOffset(now.Year - 100, 1, 1, 0, 0, 0, now.Offset);
            _maxYear = new DateTimeOffset(now.Year + 100, 12, 31, 0, 0, 0, now.Offset);
            _date = now;
            _calendar = new GregorianCalendar();
            KeyboardNavigation.SetTabNavigation(this, KeyboardNavigationMode.Cycle);
        }

        /// <summary>
        /// Gets or sets the current Date for the picker
        /// </summary>
        public DateTimeOffset Date
        {
            get => _date;
            set
            {
                SetAndRaise(DateProperty, ref _date, value);
                _syncDate = Date;
                InitPicker();
            }
        }

        /// <summary>
        /// Gets or sets the DayFormat
        /// </summary>
        public string DayFormat
        {
            get => _dayFormat;
            set => SetAndRaise(DayFormatProperty, ref _dayFormat, value);
        }

        /// <summary>
        /// Get or sets whether the Day selector is visible
        /// </summary>
        public bool DayVisible
        {
            get => _dayVisible;
            set
            {
                SetAndRaise(DayVisibleProperty, ref _dayVisible, value);
            }
        }

        /// <summary>
        /// Gets or sets the maximum pickable year
        /// </summary>
        public DateTimeOffset MaxYear
        {
            get => _maxYear;
            set
            {
                if (value < MinYear)
                    throw new InvalidOperationException("MaxDate cannot be less than MinDate");
                SetAndRaise(MaxYearProperty, ref _maxYear, value);

                if (Date > value)
                    Date = value;
            }
        }

        /// <summary>
        /// Gets or sets the minimum pickable year
        /// </summary>
        public DateTimeOffset MinYear
        {
            get => _minYear;
            set
            {
                if (value > MaxYear)
                    throw new InvalidOperationException("MinDate cannot be greater than MaxDate");
                SetAndRaise(MinYearProperty, ref _minYear, value);

                if (Date < value)
                    Date = value;
            }
        }

        /// <summary>
        /// Gets or sets the month format
        /// </summary>
        public string MonthFormat
        {
            get => _monthFormat;
            set => SetAndRaise(MonthFormatProperty, ref _monthFormat, value);
        }

        /// <summary>
        /// Gets or sets whether the month selector is visible
        /// </summary>
        public bool MonthVisible
        {
            get => _monthVisible;
            set
            {
                SetAndRaise(MonthVisibleProperty, ref _monthVisible, value);
            }
        }

        /// <summary>
        /// Gets or sets the year format
        /// </summary>
        public string YearFormat
        {
            get => _yearFormat;
            set => SetAndRaise(YearFormatProperty, ref _yearFormat, value);
        }

        /// <summary>
        /// Gets or sets whether the year selector is visible
        /// </summary>
        public bool YearVisible
        {
            get => _yearVisible;
            set
            {
                SetAndRaise(YearVisibleProperty, ref _yearVisible, value);
            }
        }

        protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
        {
            base.OnApplyTemplate(e);
            // These are requirements, so throw if not found
            _pickerContainer = e.NameScope.Get<Grid>("PickerContainer");
            _monthHost = e.NameScope.Get<Panel>("MonthHost");
            _dayHost = e.NameScope.Get<Panel>("DayHost");
            _yearHost = e.NameScope.Get<Panel>("YearHost");

            _monthSelector = e.NameScope.Get<DateTimePickerPanel>("MonthSelector");
            _monthSelector.SelectionChanged += OnMonthChanged;

            _daySelector = e.NameScope.Get<DateTimePickerPanel>("DaySelector");
            _daySelector.SelectionChanged += OnDayChanged;

            _yearSelector = e.NameScope.Get<DateTimePickerPanel>("YearSelector");
            _yearSelector.SelectionChanged += OnYearChanged;

            _acceptButton = e.NameScope.Get<Button>("AcceptButton");

            _monthUpButton = e.NameScope.Find<RepeatButton>("MonthUpButton");
            if (_monthUpButton != null)
            {
                _monthUpButton.Click += OnSelectorButtonClick;
            }
            _monthDownButton = e.NameScope.Find<RepeatButton>("MonthDownButton");
            if (_monthDownButton != null)
            {
                _monthDownButton.Click += OnSelectorButtonClick;
            }

            _dayUpButton = e.NameScope.Find<RepeatButton>("DayUpButton");
            if (_dayUpButton != null)
            {
                _dayUpButton.Click += OnSelectorButtonClick;
            }
            _dayDownButton = e.NameScope.Find<RepeatButton>("DayDownButton");
            if (_dayDownButton != null)
            {
                _dayDownButton.Click += OnSelectorButtonClick;
            }

            _yearUpButton = e.NameScope.Find<RepeatButton>("YearUpButton");
            if (_yearUpButton != null)
            {
                _yearUpButton.Click += OnSelectorButtonClick;
            }
            _yearDownButton = e.NameScope.Find<RepeatButton>("YearDownButton");
            if (_yearDownButton != null)
            {
                _yearDownButton.Click += OnSelectorButtonClick;
            }

            _dismissButton = e.NameScope.Find<Button>("DismissButton");
            _spacer1 = e.NameScope.Find<Rectangle>("FirstSpacer");
            _spacer2 = e.NameScope.Find<Rectangle>("SecondSpacer");

            if (_acceptButton != null)
            {
                _acceptButton.Click += OnAcceptButtonClicked;
            }
            if (_dismissButton != null)
            {
                _dismissButton.Click += OnDismissButtonClicked;
            }
            InitPicker();
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Escape:
                    OnDismiss();
                    e.Handled = true;
                    break;
                case Key.Tab:
                    var nextFocus = KeyboardNavigationHandler.GetNext(FocusManager.Instance.Current, NavigationDirection.Next);
                    KeyboardDevice.Instance?.SetFocusedElement(nextFocus, NavigationMethod.Tab, KeyModifiers.None);
                    e.Handled = true;
                    break;
                case Key.Enter:
                    Date = _syncDate;
                    OnConfirmed();
                    e.Handled = true;
                    break;
            }
            base.OnKeyDown(e);
        }

        /// <summary>
        /// Initializes the picker selectors.
        /// </summary>
        private void InitPicker()
        {
            // OnApplyTemplate must've been called before we can init here...
            if (_pickerContainer == null)
                return;

            _suppressUpdateSelection = true;

            _monthSelector.MaximumValue = 12;
            _monthSelector.MinimumValue = 1;
            _monthSelector.ItemFormat = MonthFormat;

            _daySelector.ItemFormat = DayFormat;

            _yearSelector.MaximumValue = MaxYear.Year;
            _yearSelector.MinimumValue = MinYear.Year;
            _yearSelector.ItemFormat = YearFormat;

            SetGrid();

            // Date should've been set when we reach this point
            var dt = Date;
            if (DayVisible)
            {
                var maxDays = _calendar.GetDaysInMonth(dt.Year, dt.Month);
                _daySelector.MaximumValue = maxDays;
                _daySelector.MinimumValue = 1;
                _daySelector.SelectedValue = dt.Day;
                _daySelector.FormatDate = dt.Date;
            }

            if (MonthVisible)
            {
                _monthSelector.SelectedValue = dt.Month;
                _monthSelector.FormatDate = dt.Date;
            }

            if (YearVisible)
            {
                _yearSelector.SelectedValue = dt.Year;
                _yearSelector.FormatDate = dt.Date;
            }

            _suppressUpdateSelection = false;

            SetInitialFocus();
        }

        private void SetGrid()
        {
            var fmt = CultureInfo.CurrentCulture.DateTimeFormat.ShortDatePattern;
            var columns = new List<(Panel, int)>
            {
                (_monthHost, MonthVisible ? fmt.IndexOf("m", StringComparison.OrdinalIgnoreCase) : -1),
                (_yearHost, YearVisible ? fmt.IndexOf("y", StringComparison.OrdinalIgnoreCase) : -1),
                (_dayHost, DayVisible ? fmt.IndexOf("d", StringComparison.OrdinalIgnoreCase) : -1),
            };

            columns.Sort((x, y) => x.Item2 - y.Item2);
            
            if (_pickerContainer.ColumnDefinitions is null)
                _pickerContainer.ColumnDefinitions = new ColumnDefinitions();
            else
                _pickerContainer.ColumnDefinitions.Clear();

            var columnIndex = 0;

            foreach (var column in columns)
            {
                if (column.Item1 is null)
                    continue;

                column.Item1.IsVisible = column.Item2 != -1;

                if (column.Item2 != -1)
                {
                    if (columnIndex > 0)
                    {
                        _pickerContainer.ColumnDefinitions.Add(new ColumnDefinition(0, GridUnitType.Auto));
                    }

                    _pickerContainer.ColumnDefinitions.Add(
                        new ColumnDefinition(column.Item1 == _monthHost ? 138 : 78, GridUnitType.Star));

                    if (column.Item1.Parent is null)
                    {
                        _pickerContainer.Children.Add(column.Item1);
                    }

                    Grid.SetColumn(column.Item1, (columnIndex++ * 2));
                }
            }

            var isSpacer1Visible = columnIndex > 1;
            var isSpacer2Visible = columnIndex > 2;
            // ternary conditional operator is used to make sure grid cells will be validated
            Grid.SetColumn(_spacer1, isSpacer1Visible ? 1 : 0);
            Grid.SetColumn(_spacer2, isSpacer2Visible ? 3 : 0);

            _spacer1.IsVisible = isSpacer1Visible;
            _spacer2.IsVisible = isSpacer2Visible;
        }

        private void SetInitialFocus()
        {
            int monthCol = MonthVisible ? Grid.GetColumn(_monthHost) : int.MaxValue;
            int dayCol = DayVisible ? Grid.GetColumn(_dayHost) : int.MaxValue;
            int yearCol = YearVisible ? Grid.GetColumn(_yearHost) : int.MaxValue;

            if (monthCol < dayCol && monthCol < yearCol)
            {
                KeyboardDevice.Instance?.SetFocusedElement(_monthSelector, NavigationMethod.Pointer, KeyModifiers.None);
            }
            else if (dayCol < monthCol && dayCol < yearCol)
            {
                KeyboardDevice.Instance?.SetFocusedElement(_daySelector, NavigationMethod.Pointer, KeyModifiers.None);
            }
            else if (yearCol < monthCol && yearCol < dayCol)
            {
                KeyboardDevice.Instance?.SetFocusedElement(_yearSelector, NavigationMethod.Pointer, KeyModifiers.None);
            }
        }

        private void OnDismissButtonClicked(object sender, RoutedEventArgs e)
        {
            OnDismiss();
        }

        private void OnAcceptButtonClicked(object sender, RoutedEventArgs e)
        {
            Date = _syncDate;
            OnConfirmed();
        }

        private void OnSelectorButtonClick(object sender, RoutedEventArgs e)
        {
            if (sender == _monthUpButton)
                _monthSelector.ScrollUp();
            else if (sender == _monthDownButton)
                _monthSelector.ScrollDown();
            else if (sender == _yearUpButton)
                _yearSelector.ScrollUp();
            else if (sender == _yearDownButton)
                _yearSelector.ScrollDown();
            else if (sender == _dayUpButton)
                _daySelector.ScrollUp();
            else if (sender == _dayDownButton)
                _daySelector.ScrollDown();
        }

        private void OnYearChanged(object sender, EventArgs e)
        {
            if (_suppressUpdateSelection)
                return;

            int maxDays = _calendar.GetDaysInMonth(_yearSelector.SelectedValue, _syncDate.Month);
            var newDate = new DateTimeOffset(_yearSelector.SelectedValue, _syncDate.Month,
                _syncDate.Day > maxDays ? maxDays : _syncDate.Day, 0, 0, 0, _syncDate.Offset);

            _syncDate = newDate;

            // We don't need to update the days if not displaying day, not february
            if (!DayVisible || _syncDate.Month != 2)
                return;

            _suppressUpdateSelection = true;

            _daySelector.FormatDate = newDate.Date;

            if (_daySelector.MaximumValue != maxDays)
                _daySelector.MaximumValue = maxDays;
            else
                _daySelector.RefreshItems();

            _suppressUpdateSelection = false;
        }

        private void OnDayChanged(object sender, EventArgs e)
        {
            if (_suppressUpdateSelection)
                return;
            _syncDate = new DateTimeOffset(_syncDate.Year, _syncDate.Month, _daySelector.SelectedValue, 0, 0, 0, _syncDate.Offset);
        }

        private void OnMonthChanged(object sender, EventArgs e)
        {
            if (_suppressUpdateSelection)
                return;

            int maxDays = _calendar.GetDaysInMonth(_syncDate.Year, _monthSelector.SelectedValue);
            var newDate = new DateTimeOffset(_syncDate.Year, _monthSelector.SelectedValue,
                _syncDate.Day > maxDays ? maxDays : _syncDate.Day, 0, 0, 0, _syncDate.Offset);

            if (!DayVisible)
            {
                _syncDate = newDate;
                return;
            }

            _suppressUpdateSelection = true;

            _daySelector.FormatDate = newDate.Date;
            _syncDate = newDate;

            if (_daySelector.MaximumValue != maxDays)
                _daySelector.MaximumValue = maxDays;
            else
                _daySelector.RefreshItems();

            _suppressUpdateSelection = false;
        }

        internal double GetOffsetForPopup()
        {
            var acceptDismissButtonHeight = _acceptButton != null ? _acceptButton.Bounds.Height : 41;
            return -(MaxHeight - acceptDismissButtonHeight) / 2 - (_monthSelector.ItemHeight / 2);
        }
    }
}
