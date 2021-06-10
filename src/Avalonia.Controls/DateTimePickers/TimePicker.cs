using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Shapes;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using System;
using System.Globalization;

namespace Avalonia.Controls
{
    /// <summary>
    /// A control to allow the user to select a time.
    /// </summary>
    [PseudoClasses(":hasnotime")]
    public class TimePicker : TemplatedControl
    {
        /// <summary>
        /// Defines the <see cref="MinuteIncrement"/> property
        /// </summary>
        public static readonly DirectProperty<TimePicker, int> MinuteIncrementProperty =
            AvaloniaProperty.RegisterDirect<TimePicker, int>(nameof(MinuteIncrement),
                x => x.MinuteIncrement, (x, v) => x.MinuteIncrement = v);

        /// <summary>
        /// Defines the <see cref="Header"/> property
        /// </summary>
        public static readonly StyledProperty<object> HeaderProperty =
            AvaloniaProperty.Register<DatePicker, object>(nameof(Header));

        /// <summary>
        /// Defines the <see cref="HeaderTemplate"/> property
        /// </summary>
        public static readonly StyledProperty<IDataTemplate> HeaderTemplateProperty =
            AvaloniaProperty.Register<DatePicker, IDataTemplate>(nameof(HeaderTemplate));

        /// <summary>
        /// Defines the <see cref="ClockIdentifier"/> property
        /// </summary>
        public static readonly DirectProperty<TimePicker, string> ClockIdentifierProperty =
           AvaloniaProperty.RegisterDirect<TimePicker, string>(nameof(ClockIdentifier),
               x => x.ClockIdentifier, (x, v) => x.ClockIdentifier = v);

        /// <summary>
        /// Defines the <see cref="SelectedTime"/> property
        /// </summary>
        public static readonly DirectProperty<TimePicker, TimeSpan?> SelectedTimeProperty =
            AvaloniaProperty.RegisterDirect<TimePicker, TimeSpan?>(nameof(SelectedTime),
                x => x.SelectedTime, (x, v) => x.SelectedTime = v,
                defaultBindingMode: BindingMode.TwoWay);

        // Template Items
        private TimePickerPresenter _presenter;
        private Button _flyoutButton;
        private Border _firstPickerHost;
        private Border _secondPickerHost;
        private Border _thirdPickerHost;
        private TextBlock _hourText;
        private TextBlock _minuteText;
        private TextBlock _periodText;
        private Rectangle _firstSplitter;
        private Rectangle _secondSplitter;
        private Grid _contentGrid;
        private Popup _popup;

        private TimeSpan? _selectedTime;
        private int _minuteIncrement = 1;
        private string _clockIdentifier = "12HourClock";

        public TimePicker()
        {
            PseudoClasses.Set(":hasnotime", true);

            var timePattern = CultureInfo.CurrentCulture.DateTimeFormat.ShortTimePattern;
            if (timePattern.IndexOf("H") != -1)
                _clockIdentifier = "24HourClock";
        }

        /// <summary>
        /// Gets or sets the minute increment in the picker
        /// </summary>
        public int MinuteIncrement
        {
            get => _minuteIncrement;
            set
            {
                if (value < 1 || value > 59)
                    throw new ArgumentOutOfRangeException("1 >= MinuteIncrement <= 59");
                SetAndRaise(MinuteIncrementProperty, ref _minuteIncrement, value);
                SetSelectedTimeText();
            }
        }

        /// <summary>
        /// Gets or sets the header
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
        /// Gets or sets the clock identifier, either 12HourClock or 24HourClock
        /// </summary>
        public string ClockIdentifier
        {
            get => _clockIdentifier;
            set
            {
                if (!(string.IsNullOrEmpty(value) || value == "" || value == "12HourClock" || value == "24HourClock"))
                    throw new ArgumentException("Invalid ClockIdentifier");
                SetAndRaise(ClockIdentifierProperty, ref _clockIdentifier, value);
                SetGrid();
                SetSelectedTimeText();
            }
        }

        /// <summary>
        /// Gets or sets the selected time. Can be null.
        /// </summary>
        public TimeSpan? SelectedTime
        {
            get => _selectedTime;
            set
            {
                var old = _selectedTime;
                SetAndRaise(SelectedTimeProperty, ref _selectedTime, value);
                OnSelectedTimeChanged(old, value);
                SetSelectedTimeText();
            }
        }

        /// <summary>
        /// Raised when the <see cref="SelectedTime"/> property changes
        /// </summary>
        public event EventHandler<TimePickerSelectedValueChangedEventArgs> SelectedTimeChanged;

        protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
        {
            if (_flyoutButton != null)
                _flyoutButton.Click -= OnFlyoutButtonClicked;

            if (_presenter != null)
            {
                _presenter.Confirmed -= OnConfirmed;
                _presenter.Dismissed -= OnDismissPicker;
            }
            base.OnApplyTemplate(e);

            _flyoutButton = e.NameScope.Find<Button>("FlyoutButton");

            _firstPickerHost = e.NameScope.Find<Border>("FirstPickerHost");
            _secondPickerHost = e.NameScope.Find<Border>("SecondPickerHost");
            _thirdPickerHost = e.NameScope.Find<Border>("ThirdPickerHost");

            _hourText = e.NameScope.Find<TextBlock>("HourTextBlock");
            _minuteText = e.NameScope.Find<TextBlock>("MinuteTextBlock");
            _periodText = e.NameScope.Find<TextBlock>("PeriodTextBlock");

            _firstSplitter = e.NameScope.Find<Rectangle>("FirstColumnDivider");
            _secondSplitter = e.NameScope.Find<Rectangle>("SecondColumnDivider");

            _contentGrid = e.NameScope.Find<Grid>("FlyoutButtonContentGrid");

            _popup = e.NameScope.Find<Popup>("Popup");
            _presenter = e.NameScope.Find<TimePickerPresenter>("PickerPresenter");

            if (_flyoutButton != null)
                _flyoutButton.Click += OnFlyoutButtonClicked;

            SetGrid();
            SetSelectedTimeText();

            if (_presenter != null)
            {
                _presenter.Confirmed += OnConfirmed;
                _presenter.Dismissed += OnDismissPicker;

                _presenter[!TimePickerPresenter.MinuteIncrementProperty] = this[!MinuteIncrementProperty];
                _presenter[!TimePickerPresenter.ClockIdentifierProperty] = this[!ClockIdentifierProperty];
            }
        }

        private void SetGrid()
        {
            if (_contentGrid == null)
                return;

            bool use24HourClock = ClockIdentifier == "24HourClock";

            var columnsD = use24HourClock ? "*, Auto, *" : "*, Auto, *, Auto, *";
            _contentGrid.ColumnDefinitions = new ColumnDefinitions(columnsD);

            _thirdPickerHost.IsVisible = !use24HourClock;
            _secondSplitter.IsVisible = !use24HourClock;

            Grid.SetColumn(_firstPickerHost, 0);
            Grid.SetColumn(_secondPickerHost, 2);

            Grid.SetColumn(_thirdPickerHost, use24HourClock ? 0 : 4);

            Grid.SetColumn(_firstSplitter, 1);
            Grid.SetColumn(_secondSplitter, use24HourClock ? 0 : 3);
        }

        private void SetSelectedTimeText()
        {
            if (_hourText == null || _minuteText == null || _periodText == null)
                return;

            var time = SelectedTime;
            if (time.HasValue)
            {
                var newTime = SelectedTime.Value;

                if (ClockIdentifier == "12HourClock")
                {
                    var hr = newTime.Hours;
                    hr = hr > 12 ? hr - 12 : hr == 0 ? 12 : hr;
                    newTime = new TimeSpan(hr, newTime.Minutes, 0);
                }

                _hourText.Text = newTime.ToString("%h");
                _minuteText.Text = newTime.ToString("mm");
                PseudoClasses.Set(":hasnotime", false);

                _periodText.Text = time.Value.Hours >= 12 ? CultureInfo.CurrentCulture.DateTimeFormat.PMDesignator :
                    CultureInfo.CurrentCulture.DateTimeFormat.AMDesignator;
            }
            else
            {
                _hourText.Text = "hour";
                _minuteText.Text = "minute";
                PseudoClasses.Set(":hasnotime", true);

                _periodText.Text = DateTime.Now.Hour >= 12 ? CultureInfo.CurrentCulture.DateTimeFormat.PMDesignator :
                    CultureInfo.CurrentCulture.DateTimeFormat.AMDesignator;
            }
        }

        protected virtual void OnSelectedTimeChanged(TimeSpan? oldTime, TimeSpan? newTime)
        {
            SelectedTimeChanged?.Invoke(this, new TimePickerSelectedValueChangedEventArgs(oldTime, newTime));
        }

        private void OnFlyoutButtonClicked(object sender, Interactivity.RoutedEventArgs e)
        {
            _presenter.Time = SelectedTime ?? DateTime.Now.TimeOfDay;

            _popup.IsOpen = true;

            var deltaY = _presenter.GetOffsetForPopup();

            // The extra 5 px I think is related to default popup placement behavior
            _popup.Host.ConfigurePosition(_popup.PlacementTarget, PlacementMode.AnchorAndGravity, new Point(0, deltaY + 5),
                Primitives.PopupPositioning.PopupAnchor.Bottom, Primitives.PopupPositioning.PopupGravity.Bottom,
                 Primitives.PopupPositioning.PopupPositionerConstraintAdjustment.SlideY);
        }

        private void OnDismissPicker(object sender, EventArgs e)
        {
            _popup.Close();
            Focus();
        }

        private void OnConfirmed(object sender, EventArgs e)
        {
            _popup.Close();
            SelectedTime = _presenter.Time;
        }
    }
}
