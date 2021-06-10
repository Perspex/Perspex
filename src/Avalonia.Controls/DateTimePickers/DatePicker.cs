using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Shapes;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Interactivity;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace Avalonia.Controls
{
    /// <summary>
    /// A control to allow the user to select a date
    /// </summary>
    [PseudoClasses(":hasnodate")]
    public class DatePicker : TemplatedControl
    {
        /// <summary>
        /// Define the <see cref="DayFormat"/> Property
        /// </summary>
        public static readonly DirectProperty<DatePicker, string> DayFormatProperty =
            AvaloniaProperty.RegisterDirect<DatePicker, string>(nameof(DayFormat),
                x => x.DayFormat, (x, v) => x.DayFormat = v);

        /// <summary>
        /// Defines the <see cref="DayVisible"/> Property
        /// </summary>
        public static readonly DirectProperty<DatePicker, bool> DayVisibleProperty =
            AvaloniaProperty.RegisterDirect<DatePicker, bool>(nameof(DayVisible),
                x => x.DayVisible, (x, v) => x.DayVisible = v);

        /// <summary>
        /// Defines the <see cref="Header"/> Property
        /// </summary>
        public static readonly StyledProperty<object> HeaderProperty =
            AvaloniaProperty.Register<DatePicker, object>(nameof(Header));

        /// <summary>
        /// Defines the <see cref="HeaderTemplate"/> Property
        /// </summary>
        public static readonly StyledProperty<IDataTemplate> HeaderTemplateProperty =
            AvaloniaProperty.Register<DatePicker, IDataTemplate>(nameof(HeaderTemplate));

        /// <summary>
        /// Defines the <see cref="MaxYear"/> Property
        /// </summary>
        public static readonly DirectProperty<DatePicker, DateTimeOffset> MaxYearProperty =
            AvaloniaProperty.RegisterDirect<DatePicker, DateTimeOffset>(nameof(MaxYear), 
                x => x.MaxYear, (x, v) => x.MaxYear = v);

        /// <summary>
        /// Defines the <see cref="MinYear"/> Property
        /// </summary>
        public static readonly DirectProperty<DatePicker, DateTimeOffset> MinYearProperty =
            AvaloniaProperty.RegisterDirect<DatePicker, DateTimeOffset>(nameof(MinYear), 
                x => x.MinYear, (x, v) => x.MinYear = v);

        /// <summary>
        /// Defines the <see cref="MonthFormat"/> Property
        /// </summary>
        public static readonly DirectProperty<DatePicker, string> MonthFormatProperty =
            AvaloniaProperty.RegisterDirect<DatePicker, string>(nameof(MonthFormat), 
                x => x.MonthFormat, (x, v) => x.MonthFormat = v);

        /// <summary>
        /// Defines the <see cref="MonthVisible"/> Property
        /// </summary>
        public static readonly DirectProperty<DatePicker, bool> MonthVisibleProperty =
            AvaloniaProperty.RegisterDirect<DatePicker, bool>(nameof(MonthVisible), 
                x => x.MonthVisible, (x, v) => x.MonthVisible = v);

        /// <summary>
        /// Defiens the <see cref="YearFormat"/> Property
        /// </summary>
        public static readonly DirectProperty<DatePicker, string> YearFormatProperty =
            AvaloniaProperty.RegisterDirect<DatePicker, string>(nameof(YearFormat), 
                x => x.YearFormat, (x, v) => x.YearFormat = v);

        /// <summary>
        /// Defines the <see cref="YearVisible"/> Property
        /// </summary>
        public static readonly DirectProperty<DatePicker, bool> YearVisibleProperty =
            AvaloniaProperty.RegisterDirect<DatePicker, bool>(nameof(YearVisible), 
                x => x.YearVisible, (x, v) => x.YearVisible = v);

        /// <summary>
        /// Defines the <see cref="SelectedDate"/> Property
        /// </summary>
        public static readonly DirectProperty<DatePicker, DateTimeOffset?> SelectedDateProperty =
            AvaloniaProperty.RegisterDirect<DatePicker, DateTimeOffset?>(nameof(SelectedDate), 
                x => x.SelectedDate, (x, v) => x.SelectedDate = v,
                defaultBindingMode: BindingMode.TwoWay);

        // Template Items
        private Button _flyoutButton;
        private TextBlock _dayText;
        private TextBlock _monthText;
        private TextBlock _yearText;
        private Grid _container;
        private Rectangle _spacer1;
        private Rectangle _spacer2;
        private Popup _popup;
        private DatePickerPresenter _presenter;

        private bool _areControlsAvailable;

        private string _dayFormat = "%d";
        private bool _dayVisible = true;
        private DateTimeOffset _maxYear;
        private DateTimeOffset _minYear;
        private string _monthFormat = "MMMM";
        private bool _monthVisible = true;
        private string _yearFormat = "yyyy";
        private bool _yearVisible = true;
        private DateTimeOffset? _selectedDate;

        public DatePicker()
        {
            PseudoClasses.Set(":hasnodate", true);
            var now = DateTimeOffset.Now;
            _minYear = new DateTimeOffset(now.Date.Year - 100, 1, 1, 0, 0, 0, now.Offset);
            _maxYear = new DateTimeOffset(now.Date.Year + 100, 12, 31, 0, 0, 0, now.Offset);
        }

        public string DayFormat
        {
            get => _dayFormat;
            set => SetAndRaise(DayFormatProperty, ref _dayFormat, value);
        }

        /// <summary>
        /// Gets or sets whether the day is visible
        /// </summary>
        public bool DayVisible
        {
            get => _dayVisible;
            set
            {
                SetAndRaise(DayVisibleProperty, ref _dayVisible, value);
                SetGrid();
            }
        }

        /// <summary>
        /// Gets or sets the DatePicker header
        /// </summary>
        public object Header
        {
            get => GetValue(HeaderProperty);
            set => SetValue(HeaderProperty, value);
        }

        /// <summary>
        /// Gets or sets the header template
        /// </summary>
        public IDataTemplate HeaderTemplate
        {
            get => GetValue(HeaderTemplateProperty);
            set => SetValue(HeaderTemplateProperty, value);
        }

        /// <summary>
        /// Gets or sets the maximum year for the picker
        /// </summary>
        public DateTimeOffset MaxYear
        {
            get => _maxYear;
            set
            {
                if (value < MinYear)
                    throw new InvalidOperationException("MaxDate cannot be less than MinDate");
                SetAndRaise(MaxYearProperty, ref _maxYear, value);

                if (SelectedDate.HasValue && SelectedDate.Value > value)
                    SelectedDate = value;
            }
        }

        /// <summary>
        /// Gets or sets the minimum year for the picker
        /// </summary>
        public DateTimeOffset MinYear
        {
            get => _minYear;
            set
            {
                if (value > MaxYear)
                    throw new InvalidOperationException("MinDate cannot be greater than MaxDate");
                SetAndRaise(MinYearProperty, ref _minYear, value);

                if (SelectedDate.HasValue && SelectedDate.Value < value)
                    SelectedDate = value;
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
        /// Gets or sets whether the month is visible
        /// </summary>
        public bool MonthVisible
        {
            get => _monthVisible;
            set
            {
                SetAndRaise(MonthVisibleProperty, ref _monthVisible, value);
                SetGrid();
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
        /// Gets or sets whether the year is visible
        /// </summary>
        public bool YearVisible
        {
            get => _yearVisible;
            set
            {
                SetAndRaise(YearVisibleProperty, ref _yearVisible, value);
                SetGrid();
            }
        }

        /// <summary>
        /// Gets or sets the Selected Date for the picker, can be null
        /// </summary>
        public DateTimeOffset? SelectedDate
        {
            get => _selectedDate;
            set
            {
                var old = _selectedDate;
                SetAndRaise(SelectedDateProperty, ref _selectedDate, value);
                SetSelectedDateText();
                OnSelectedDateChanged(this, new DatePickerSelectedValueChangedEventArgs(old, value));
            }
        }

        /// <summary>
        /// Raised when the <see cref="SelectedDate"/> changes
        /// </summary>
        public event EventHandler<DatePickerSelectedValueChangedEventArgs> SelectedDateChanged;

        protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
        {
            _areControlsAvailable = false;
            if (_flyoutButton != null)
                _flyoutButton.Click -= OnFlyoutButtonClicked;
            if (_presenter != null)
            {
                _presenter.Confirmed -= OnConfirmed;
                _presenter.Dismissed -= OnDismissPicker;
            }

            base.OnApplyTemplate(e);
            _flyoutButton = e.NameScope.Find<Button>("FlyoutButton");
            _dayText = e.NameScope.Find<TextBlock>("DayText");
            _monthText = e.NameScope.Find<TextBlock>("MonthText");
            _yearText = e.NameScope.Find<TextBlock>("YearText");
            _container = e.NameScope.Find<Grid>("ButtonContentGrid");
            _spacer1 = e.NameScope.Find<Rectangle>("FirstSpacer");
            _spacer2 = e.NameScope.Find<Rectangle>("SecondSpacer");
            _popup = e.NameScope.Find<Popup>("Popup");
            _presenter = e.NameScope.Find<DatePickerPresenter>("PickerPresenter");

            _areControlsAvailable = true;

            SetGrid();
            SetSelectedDateText();

            if (_flyoutButton != null)
                _flyoutButton.Click += OnFlyoutButtonClicked;

            if (_presenter != null)
            {
                _presenter.Confirmed += OnConfirmed;
                _presenter.Dismissed += OnDismissPicker;

                _presenter[!DatePickerPresenter.MaxYearProperty] = this[!MaxYearProperty];
                _presenter[!DatePickerPresenter.MinYearProperty] = this[!MinYearProperty];

                _presenter[!DatePickerPresenter.MonthVisibleProperty] = this[!MonthVisibleProperty];
                _presenter[!DatePickerPresenter.MonthFormatProperty] = this[!MonthFormatProperty];

                _presenter[!DatePickerPresenter.DayVisibleProperty] = this[!DayVisibleProperty];
                _presenter[!DatePickerPresenter.DayFormatProperty] = this[!DayFormatProperty];

                _presenter[!DatePickerPresenter.YearVisibleProperty] = this[!YearVisibleProperty];
                _presenter[!DatePickerPresenter.YearFormatProperty] = this[!YearFormatProperty];
            }
        }

        private void OnDismissPicker(object sender, EventArgs e)
        {
            _popup.Close();
            Focus();
        }

        private void OnConfirmed(object sender, EventArgs e)
        {
            _popup.Close();
            SelectedDate = _presenter.Date;
        }

        private void SetGrid()
        {
            if (_container == null)
                return;

            var fmt = CultureInfo.CurrentCulture.DateTimeFormat.ShortDatePattern;
            var columns = new List<(TextBlock, int)>
            {
                (_monthText, MonthVisible ? fmt.IndexOf("m", StringComparison.OrdinalIgnoreCase) : -1),
                (_yearText, YearVisible ? fmt.IndexOf("y", StringComparison.OrdinalIgnoreCase) : -1),
                (_dayText, DayVisible ? fmt.IndexOf("d", StringComparison.OrdinalIgnoreCase) : -1),
            };

            columns.Sort((x, y) => x.Item2 - y.Item2);
            _container.ColumnDefinitions.Clear();

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
                        _container.ColumnDefinitions.Add(new ColumnDefinition(0, GridUnitType.Auto));
                    }

                    _container.ColumnDefinitions.Add(
                        new ColumnDefinition(column.Item1 == _monthText ? 138 : 78, GridUnitType.Star));

                    if (column.Item1.Parent is null)
                    {
                        _container.Children.Add(column.Item1);
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

        private void SetSelectedDateText()
        {
            if (!_areControlsAvailable)
                return;

            if (SelectedDate.HasValue)
            {
                PseudoClasses.Set(":hasnodate", false);
                var selDate = SelectedDate.Value;
                _monthText.Text = selDate.ToString(MonthFormat);
                _yearText.Text = selDate.ToString(YearFormat);
                _dayText.Text = selDate.ToString(DayFormat);
            }
            else
            {
                PseudoClasses.Set(":hasnodate", true);
                _monthText.Text = "month";
                _yearText.Text = "year";
                _dayText.Text = "day";
            }
        }

        private void OnFlyoutButtonClicked(object sender, RoutedEventArgs e)
        {
            if (_presenter == null)
                throw new InvalidOperationException("No DatePickerPresenter found");

            _presenter.Date = SelectedDate ?? DateTimeOffset.Now;

            _popup.IsOpen = true;

            var deltaY = _presenter.GetOffsetForPopup();

            // The extra 5 px I think is related to default popup placement behavior
            _popup.Host.ConfigurePosition(_popup.PlacementTarget, PlacementMode.AnchorAndGravity, new Point(0, deltaY + 5),
                Primitives.PopupPositioning.PopupAnchor.Bottom, Primitives.PopupPositioning.PopupGravity.Bottom,
                 Primitives.PopupPositioning.PopupPositionerConstraintAdjustment.SlideY);
        }

        protected virtual void OnSelectedDateChanged(object sender, DatePickerSelectedValueChangedEventArgs e)
        {
            SelectedDateChanged?.Invoke(sender, e);
        }
    }
}
