// (c) Copyright Microsoft Corporation.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Collections;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Controls.Utils;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace Avalonia.Controls
{
    /// <summary>
    /// Provides data for the
    /// <see cref="E:Avalonia.Controls.AutoCompleteBox.Populated" />
    /// event.
    /// </summary>
    public class PopulatedEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the list of possible matches added to the drop-down portion of
        /// the <see cref="T:Avalonia.Controls.AutoCompleteBox" />
        /// control.
        /// </summary>
        /// <value>The list of possible matches added to the
        /// <see cref="T:Avalonia.Controls.AutoCompleteBox" />.</value>
        public IEnumerable Data { get; private set; }

        /// <summary>
        /// Initializes a new instance of the
        /// <see cref="T:Avalonia.Controls.PopulatedEventArgs" />.
        /// </summary>
        /// <param name="data">The list of possible matches added to the
        /// drop-down portion of the
        /// <see cref="T:Avalonia.Controls.AutoCompleteBox" /> control.</param>
        public PopulatedEventArgs(IEnumerable data)
        {
            Data = data;
        }
    }

    /// <summary>
    /// Provides data for the
    /// <see cref="E:Avalonia.Controls.AutoCompleteBox.Populating" />
    /// event.
    /// </summary>
    public class PopulatingEventArgs : CancelEventArgs
    {
        /// <summary>
        /// Gets the text that is used to determine which items to display in
        /// the <see cref="T:Avalonia.Controls.AutoCompleteBox" />
        /// control.
        /// </summary>
        /// <value>The text that is used to determine which items to display in
        /// the <see cref="T:Avalonia.Controls.AutoCompleteBox" />.</value>
        public string Parameter { get; private set; }

        /// <summary>
        /// Initializes a new instance of the
        /// <see cref="T:Avalonia.Controls.PopulatingEventArgs" />.
        /// </summary>
        /// <param name="parameter">The value of the
        /// <see cref="P:Avalonia.Controls.AutoCompleteBox.SearchText" />
        /// property, which is used to filter items for the
        /// <see cref="T:Avalonia.Controls.AutoCompleteBox" /> control.</param>
        public PopulatingEventArgs(string parameter)
        {
            Parameter = parameter;
        }
    }

    /// <summary>
    /// Represents the filter used by the
    /// <see cref="T:Avalonia.Controls.AutoCompleteBox" /> control to
    /// determine whether an item is a possible match for the specified text.
    /// </summary>
    /// <returns>true to indicate <paramref name="item" /> is a possible match
    /// for <paramref name="search" />; otherwise false.</returns>
    /// <param name="search">The string used as the basis for filtering.</param>
    /// <param name="item">The item that is compared with the
    /// <paramref name="search" /> parameter.</param>
    /// <typeparam name="T">The type used for filtering the
    /// <see cref="T:Avalonia.Controls.AutoCompleteBox" />. This type can
    /// be either a string or an object.</typeparam>
    public delegate bool AutoCompleteFilterPredicate<T>(string search, T item);

    /// <summary>
    /// Specifies how text in the text box portion of the
    /// <see cref="T:Avalonia.Controls.AutoCompleteBox" /> control is used
    /// to filter items specified by the
    /// <see cref="P:Avalonia.Controls.AutoCompleteBox.ItemsSource" />
    /// property for display in the drop-down.
    /// </summary>
    public enum AutoCompleteFilterMode
    {
        /// <summary>
        /// Specifies that no filter is used. All items are returned.
        /// </summary>
        None = 0,

        /// <summary>
        /// Specifies a culture-sensitive, case-insensitive filter where the
        /// returned items start with the specified text. The filter uses the
        /// <see cref="M:System.String.StartsWith(System.String,System.StringComparison)" />
        /// method, specifying
        /// <see cref="P:System.StringComparer.CurrentCultureIgnoreCase" /> as
        /// the string comparison criteria.
        /// </summary>
        StartsWith = 1,

        /// <summary>
        /// Specifies a culture-sensitive, case-sensitive filter where the
        /// returned items start with the specified text. The filter uses the
        /// <see cref="M:System.String.StartsWith(System.String,System.StringComparison)" />
        /// method, specifying
        /// <see cref="P:System.StringComparer.CurrentCulture" /> as the string
        /// comparison criteria.
        /// </summary>
        StartsWithCaseSensitive = 2,

        /// <summary>
        /// Specifies an ordinal, case-insensitive filter where the returned
        /// items start with the specified text. The filter uses the
        /// <see cref="M:System.String.StartsWith(System.String,System.StringComparison)" />
        /// method, specifying
        /// <see cref="P:System.StringComparer.OrdinalIgnoreCase" /> as the
        /// string comparison criteria.
        /// </summary>
        StartsWithOrdinal = 3,

        /// <summary>
        /// Specifies an ordinal, case-sensitive filter where the returned items
        /// start with the specified text. The filter uses the
        /// <see cref="M:System.String.StartsWith(System.String,System.StringComparison)" />
        /// method, specifying <see cref="P:System.StringComparer.Ordinal" /> as
        /// the string comparison criteria.
        /// </summary>
        StartsWithOrdinalCaseSensitive = 4,

        /// <summary>
        /// Specifies a culture-sensitive, case-insensitive filter where the
        /// returned items contain the specified text.
        /// </summary>
        Contains = 5,

        /// <summary>
        /// Specifies a culture-sensitive, case-sensitive filter where the
        /// returned items contain the specified text.
        /// </summary>
        ContainsCaseSensitive = 6,

        /// <summary>
        /// Specifies an ordinal, case-insensitive filter where the returned
        /// items contain the specified text.
        /// </summary>
        ContainsOrdinal = 7,

        /// <summary>
        /// Specifies an ordinal, case-sensitive filter where the returned items
        /// contain the specified text.
        /// </summary>
        ContainsOrdinalCaseSensitive = 8,

        /// <summary>
        /// Specifies a culture-sensitive, case-insensitive filter where the
        /// returned items equal the specified text. The filter uses the
        /// <see cref="M:System.String.Equals(System.String,System.StringComparison)" />
        /// method, specifying
        /// <see cref="P:System.StringComparer.CurrentCultureIgnoreCase" /> as
        /// the search comparison criteria.
        /// </summary>
        Equals = 9,

        /// <summary>
        /// Specifies a culture-sensitive, case-sensitive filter where the
        /// returned items equal the specified text. The filter uses the
        /// <see cref="M:System.String.Equals(System.String,System.StringComparison)" />
        /// method, specifying
        /// <see cref="P:System.StringComparer.CurrentCulture" /> as the string
        /// comparison criteria.
        /// </summary>
        EqualsCaseSensitive = 10,

        /// <summary>
        /// Specifies an ordinal, case-insensitive filter where the returned
        /// items equal the specified text. The filter uses the
        /// <see cref="M:System.String.Equals(System.String,System.StringComparison)" />
        /// method, specifying
        /// <see cref="P:System.StringComparer.OrdinalIgnoreCase" /> as the
        /// string comparison criteria.
        /// </summary>
        EqualsOrdinal = 11,

        /// <summary>
        /// Specifies an ordinal, case-sensitive filter where the returned items
        /// equal the specified text. The filter uses the
        /// <see cref="M:System.String.Equals(System.String,System.StringComparison)" />
        /// method, specifying <see cref="P:System.StringComparer.Ordinal" /> as
        /// the string comparison criteria.
        /// </summary>
        EqualsOrdinalCaseSensitive = 12,

        /// <summary>
        /// Specifies that a custom filter is used. This mode is used when the
        /// <see cref="P:Avalonia.Controls.AutoCompleteBox.TextFilter" />
        /// or
        /// <see cref="P:Avalonia.Controls.AutoCompleteBox.ItemFilter" />
        /// properties are set.
        /// </summary>
        Custom = 13,
    }

    /// <summary>
    /// Represents the selector used by the
    /// <see cref="T:Avalonia.Controls.AutoCompleteBox" /> control to
    /// determine how the specified text should be modified with an item.
    /// </summary>
    /// <returns>
    /// Modified text that will be used by the
    /// <see cref="T:Avalonia.Controls.AutoCompleteBox" />.
    /// </returns>
    /// <param name="search">The string used as the basis for filtering.</param>
    /// <param name="item">
    /// The selected item that should be combined with the
    /// <paramref name="search" /> parameter.
    /// </param>
    /// <typeparam name="T">
    /// The type used for filtering the
    /// <see cref="T:Avalonia.Controls.AutoCompleteBox" />.
    /// This type can be either a string or an object.
    /// </typeparam>
    public delegate string AutoCompleteSelector<T>(string search, T item);

    /// <summary>
    /// Represents a control that provides a text box for user input and a
    /// drop-down that contains possible matches based on the input in the text
    /// box.
    /// </summary>
    [PseudoClasses(":dropdownopen")]
    public class AutoCompleteBox : TemplatedControl
    {
        /// <summary>
        /// Specifies the name of the selection adapter TemplatePart.
        /// </summary>
        private const string ElementSelectionAdapter = "PART_SelectionAdapter";

        /// <summary>
        /// Specifies the name of the Selector TemplatePart.
        /// </summary>
        private const string ElementSelector = "PART_SelectingItemsControl";

        /// <summary>
        /// Specifies the name of the Popup TemplatePart.
        /// </summary>
        private const string ElementPopup = "PART_Popup";

        /// <summary>
        /// The name for the text box part.
        /// </summary>
        private const string ElementTextBox = "PART_TextBox";

        private IEnumerable _itemsEnumerable;

        /// <summary>
        /// Gets or sets a local cached copy of the items data.
        /// </summary>
        private List<object> _items;

        /// <summary>
        /// Gets or sets the observable collection that contains references to
        /// all of the items in the generated view of data that is provided to
        /// the selection-style control adapter.
        /// </summary>
        private AvaloniaList<object> _view;

        /// <summary>
        /// Gets or sets a value to ignore a number of pending change handlers.
        /// The value is decremented after each use. This is used to reset the
        /// value of properties without performing any of the actions in their
        /// change handlers.
        /// </summary>
        /// <remarks>The int is important as a value because the TextBox
        /// TextChanged event does not immediately fire, and this will allow for
        /// nested property changes to be ignored.</remarks>
        private int _ignoreTextPropertyChange;

        /// <summary>
        /// Gets or sets a value indicating whether to ignore calling a pending
        /// change handlers.
        /// </summary>
        private bool _ignorePropertyChange;

        /// <summary>
        /// Gets or sets a value indicating whether to ignore the selection
        /// changed event.
        /// </summary>
        private bool _ignoreTextSelectionChange;

        /// <summary>
        /// Gets or sets a value indicating whether to skip the text update
        /// processing when the selected item is updated.
        /// </summary>
        private bool _skipSelectedItemTextUpdate;

        /// <summary>
        /// Gets or sets the last observed text box selection start location.
        /// </summary>
        private int _textSelectionStart;

        /// <summary>
        /// Gets or sets a value indicating whether the user initiated the
        /// current populate call.
        /// </summary>
        private bool _userCalledPopulate;

        /// <summary>
        /// A value indicating whether the popup has been opened at least once.
        /// </summary>
        private bool _popupHasOpened;

        /// <summary>
        /// Gets or sets the DispatcherTimer used for the MinimumPopulateDelay
        /// condition for auto completion.
        /// </summary>
        private DispatcherTimer _delayTimer;

        /// <summary>
        /// Gets or sets a value indicating whether a read-only dependency
        /// property change handler should allow the value to be set.  This is
        /// used to ensure that read-only properties cannot be changed via
        /// SetValue, etc.
        /// </summary>
        private bool _allowWrite;

        /// <summary>
        /// The TextBox template part.
        /// </summary>
        private TextBox _textBox;
        private IDisposable _textBoxSubscriptions;

        /// <summary>
        /// The SelectionAdapter.
        /// </summary>
        private ISelectionAdapter _adapter;

        /// <summary>
        /// A control that can provide updated string values from a binding.
        /// </summary>
        private BindingEvaluator<string> _valueBindingEvaluator;

        /// <summary>
        /// A weak subscription for the collection changed event.
        /// </summary>
        private IDisposable _collectionChangeSubscription;

        private Func<string, CancellationToken, Task<IEnumerable<object>>> _asyncPopulator;
        private CancellationTokenSource _populationCancellationTokenSource;

        private bool _itemTemplateIsFromValueMemberBinding = true;
        private bool _settingItemTemplateFromValueMemberBinding;

        private object _selectedItem;
        private bool _isDropDownOpen;
        private bool _isFocused = false;

        private string _text = string.Empty;
        private string _searchText = string.Empty;

        private AutoCompleteFilterPredicate<object> _itemFilter;
        private AutoCompleteFilterPredicate<string> _textFilter = AutoCompleteSearch.GetFilter(AutoCompleteFilterMode.StartsWith);

        private AutoCompleteSelector<object> _itemSelector;
        private AutoCompleteSelector<string> _textSelector;

        public static readonly RoutedEvent<SelectionChangedEventArgs> SelectionChangedEvent =
            RoutedEvent.Register<SelectionChangedEventArgs>(nameof(SelectionChanged), RoutingStrategies.Bubble, typeof(AutoCompleteBox));

        public static readonly StyledProperty<string> WatermarkProperty =
            TextBox.WatermarkProperty.AddOwner<AutoCompleteBox>();

        /// <summary>
        /// Identifies the
        /// <see cref="P:Avalonia.Controls.AutoCompleteBox.MinimumPrefixLength" />
        /// dependency property.
        /// </summary>
        /// <value>The identifier for the
        /// <see cref="P:Avalonia.Controls.AutoCompleteBox.MinimumPrefixLength" />
        /// dependency property.</value>
        public static readonly StyledProperty<int> MinimumPrefixLengthProperty =
            AvaloniaProperty.Register<AutoCompleteBox, int>(
                nameof(MinimumPrefixLength), 1,
                validate: IsValidMinimumPrefixLength);

        /// <summary>
        /// Identifies the
        /// <see cref="P:Avalonia.Controls.AutoCompleteBox.MinimumPopulateDelay" />
        /// dependency property.
        /// </summary>
        /// <value>The identifier for the
        /// <see cref="P:Avalonia.Controls.AutoCompleteBox.MinimumPopulateDelay" />
        /// dependency property.</value>
        public static readonly StyledProperty<TimeSpan> MinimumPopulateDelayProperty =
            AvaloniaProperty.Register<AutoCompleteBox, TimeSpan>(
                nameof(MinimumPopulateDelay),
                TimeSpan.Zero,
                validate: IsValidMinimumPopulateDelay);

        /// <summary>
        /// Identifies the
        /// <see cref="P:Avalonia.Controls.AutoCompleteBox.MaxDropDownHeight" />
        /// dependency property.
        /// </summary>
        /// <value>The identifier for the
        /// <see cref="P:Avalonia.Controls.AutoCompleteBox.MaxDropDownHeight" />
        /// dependency property.</value>
        public static readonly StyledProperty<double> MaxDropDownHeightProperty =
            AvaloniaProperty.Register<AutoCompleteBox, double>(
                nameof(MaxDropDownHeight),
                double.PositiveInfinity,
                validate: IsValidMaxDropDownHeight);

        /// <summary>
        /// Identifies the
        /// <see cref="P:Avalonia.Controls.AutoCompleteBox.IsTextCompletionEnabled" />
        /// dependency property.
        /// </summary>
        /// <value>The identifier for the
        /// <see cref="P:Avalonia.Controls.AutoCompleteBox.IsTextCompletionEnabled" />
        /// dependency property.</value>
        public static readonly StyledProperty<bool> IsTextCompletionEnabledProperty =
            AvaloniaProperty.Register<AutoCompleteBox, bool>(nameof(IsTextCompletionEnabled));

        /// <summary>
        /// Identifies the
        /// <see cref="P:Avalonia.Controls.AutoCompleteBox.ItemTemplate" />
        /// dependency property.
        /// </summary>
        /// <value>The identifier for the
        /// <see cref="P:Avalonia.Controls.AutoCompleteBox.ItemTemplate" />
        /// dependency property.</value>
        public static readonly StyledProperty<IDataTemplate> ItemTemplateProperty =
            AvaloniaProperty.Register<AutoCompleteBox, IDataTemplate>(nameof(ItemTemplate));

        /// <summary>
        /// Identifies the
        /// <see cref="P:Avalonia.Controls.AutoCompleteBox.IsDropDownOpen" />
        /// dependency property.
        /// </summary>
        /// <value>The identifier for the
        /// <see cref="P:Avalonia.Controls.AutoCompleteBox.IsDropDownOpen" />
        /// dependency property.</value>
        public static readonly DirectProperty<AutoCompleteBox, bool> IsDropDownOpenProperty =
            AvaloniaProperty.RegisterDirect<AutoCompleteBox, bool>(
                nameof(IsDropDownOpen),
                o => o.IsDropDownOpen,
                (o, v) => o.IsDropDownOpen = v);

        /// <summary>
        /// Identifies the
        /// <see cref="P:Avalonia.Controls.AutoCompleteBox.SelectedItem" />
        /// dependency property.
        /// </summary>
        /// <value>The identifier the
        /// <see cref="P:Avalonia.Controls.AutoCompleteBox.SelectedItem" />
        /// dependency property.</value>
        public static readonly DirectProperty<AutoCompleteBox, object> SelectedItemProperty =
            AvaloniaProperty.RegisterDirect<AutoCompleteBox, object>(
                nameof(SelectedItem),
                o => o.SelectedItem,
                (o, v) => o.SelectedItem = v,
                defaultBindingMode: BindingMode.TwoWay,
                enableDataValidation: true);

        /// <summary>
        /// Identifies the
        /// <see cref="P:Avalonia.Controls.AutoCompleteBox.Text" />
        /// dependency property.
        /// </summary>
        /// <value>The identifier for the
        /// <see cref="P:Avalonia.Controls.AutoCompleteBox.Text" />
        /// dependency property.</value>
        public static readonly DirectProperty<AutoCompleteBox, string> TextProperty =
            TextBlock.TextProperty.AddOwnerWithDataValidation<AutoCompleteBox>(
                o => o.Text,
                (o, v) => o.Text = v,
                defaultBindingMode: BindingMode.TwoWay,
                enableDataValidation: true);

        /// <summary>
        /// Identifies the
        /// <see cref="P:Avalonia.Controls.AutoCompleteBox.SearchText" />
        /// dependency property.
        /// </summary>
        /// <value>The identifier for the
        /// <see cref="P:Avalonia.Controls.AutoCompleteBox.SearchText" />
        /// dependency property.</value>
        public static readonly DirectProperty<AutoCompleteBox, string> SearchTextProperty =
            AvaloniaProperty.RegisterDirect<AutoCompleteBox, string>(
                nameof(SearchText),
                o => o.SearchText,
                unsetValue: string.Empty);

        /// <summary>
        /// Gets the identifier for the
        /// <see cref="P:Avalonia.Controls.AutoCompleteBox.FilterMode" />
        /// dependency property.
        /// </summary>
        public static readonly StyledProperty<AutoCompleteFilterMode> FilterModeProperty =
            AvaloniaProperty.Register<AutoCompleteBox, AutoCompleteFilterMode>(
                nameof(FilterMode),
                defaultValue: AutoCompleteFilterMode.StartsWith,
                validate: IsValidFilterMode);

        /// <summary>
        /// Identifies the
        /// <see cref="P:Avalonia.Controls.AutoCompleteBox.ItemFilter" />
        /// dependency property.
        /// </summary>
        /// <value>The identifier for the
        /// <see cref="P:Avalonia.Controls.AutoCompleteBox.ItemFilter" />
        /// dependency property.</value>
        public static readonly DirectProperty<AutoCompleteBox, AutoCompleteFilterPredicate<object>> ItemFilterProperty =
            AvaloniaProperty.RegisterDirect<AutoCompleteBox, AutoCompleteFilterPredicate<object>>(
                nameof(ItemFilter),
                o => o.ItemFilter,
                (o, v) => o.ItemFilter = v);

        /// <summary>
        /// Identifies the
        /// <see cref="P:Avalonia.Controls.AutoCompleteBox.TextFilter" />
        /// dependency property.
        /// </summary>
        /// <value>The identifier for the
        /// <see cref="P:Avalonia.Controls.AutoCompleteBox.TextFilter" />
        /// dependency property.</value>
        public static readonly DirectProperty<AutoCompleteBox, AutoCompleteFilterPredicate<string>> TextFilterProperty =
            AvaloniaProperty.RegisterDirect<AutoCompleteBox, AutoCompleteFilterPredicate<string>>(
                nameof(TextFilter),
                o => o.TextFilter,
                (o, v) => o.TextFilter = v,
                unsetValue: AutoCompleteSearch.GetFilter(AutoCompleteFilterMode.StartsWith));

        /// <summary>
        /// Identifies the
        /// <see cref="P:Avalonia.Controls.AutoCompleteBox.ItemSelector" />
        /// dependency property.
        /// </summary>
        /// <value>The identifier for the
        /// <see cref="P:Avalonia.Controls.AutoCompleteBox.ItemSelector" />
        /// dependency property.</value>
        public static readonly DirectProperty<AutoCompleteBox, AutoCompleteSelector<object>> ItemSelectorProperty =
            AvaloniaProperty.RegisterDirect<AutoCompleteBox, AutoCompleteSelector<object>>(
                nameof(ItemSelector),
                o => o.ItemSelector,
                (o, v) => o.ItemSelector = v);

        /// <summary>
        /// Identifies the
        /// <see cref="P:Avalonia.Controls.AutoCompleteBox.TextSelector" />
        /// dependency property.
        /// </summary>
        /// <value>The identifier for the
        /// <see cref="P:Avalonia.Controls.AutoCompleteBox.TextSelector" />
        /// dependency property.</value>
        public static readonly DirectProperty<AutoCompleteBox, AutoCompleteSelector<string>> TextSelectorProperty =
            AvaloniaProperty.RegisterDirect<AutoCompleteBox, AutoCompleteSelector<string>>(
                nameof(TextSelector),
                o => o.TextSelector,
                (o, v) => o.TextSelector = v);

        /// <summary>
        /// Identifies the
        /// <see cref="P:Avalonia.Controls.AutoCompleteBox.ItemsSource" />
        /// dependency property.
        /// </summary>
        /// <value>The identifier for the
        /// <see cref="P:Avalonia.Controls.AutoCompleteBox.ItemsSource" />
        /// dependency property.</value>
        public static readonly DirectProperty<AutoCompleteBox, IEnumerable> ItemsProperty =
            AvaloniaProperty.RegisterDirect<AutoCompleteBox, IEnumerable>(
                nameof(Items),
                o => o.Items,
                (o, v) => o.Items = v);

        public static readonly DirectProperty<AutoCompleteBox, Func<string, CancellationToken, Task<IEnumerable<object>>>> AsyncPopulatorProperty =
            AvaloniaProperty.RegisterDirect<AutoCompleteBox, Func<string, CancellationToken, Task<IEnumerable<object>>>>(
                nameof(AsyncPopulator),
                o => o.AsyncPopulator,
                (o, v) => o.AsyncPopulator = v);

        private static bool IsValidMinimumPrefixLength(int value) => value >= -1;

        private static bool IsValidMinimumPopulateDelay(TimeSpan value) => value.TotalMilliseconds >= 0.0;

        private static bool IsValidMaxDropDownHeight(double value) => value >= 0.0;

        private static bool IsValidFilterMode(AutoCompleteFilterMode mode)
        {
            switch (mode)
            {
                case AutoCompleteFilterMode.None:
                case AutoCompleteFilterMode.StartsWith:
                case AutoCompleteFilterMode.StartsWithCaseSensitive:
                case AutoCompleteFilterMode.StartsWithOrdinal:
                case AutoCompleteFilterMode.StartsWithOrdinalCaseSensitive:
                case AutoCompleteFilterMode.Contains:
                case AutoCompleteFilterMode.ContainsCaseSensitive:
                case AutoCompleteFilterMode.ContainsOrdinal:
                case AutoCompleteFilterMode.ContainsOrdinalCaseSensitive:
                case AutoCompleteFilterMode.Equals:
                case AutoCompleteFilterMode.EqualsCaseSensitive:
                case AutoCompleteFilterMode.EqualsOrdinal:
                case AutoCompleteFilterMode.EqualsOrdinalCaseSensitive:
                case AutoCompleteFilterMode.Custom:
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// Handle the change of the IsEnabled property.
        /// </summary>
        /// <param name="e">The event data.</param>
        private void OnControlIsEnabledChanged(AvaloniaPropertyChangedEventArgs e)
        {
            bool isEnabled = (bool)e.NewValue;
            if (!isEnabled)
            {
                IsDropDownOpen = false;
            }
        }

        /// <summary>
        /// MinimumPopulateDelayProperty property changed handler. Any current
        /// dispatcher timer will be stopped. The timer will not be restarted
        /// until the next TextUpdate call by the user.
        /// </summary>
        /// <param name="e">Event arguments.</param>
        private void OnMinimumPopulateDelayChanged(AvaloniaPropertyChangedEventArgs e)
        {
            var newValue = (TimeSpan)e.NewValue;

            // Stop any existing timer
            if (_delayTimer != null)
            {
                _delayTimer.Stop();

                if (newValue == TimeSpan.Zero)
                {
                    _delayTimer = null;
                }
            }

            if (newValue > TimeSpan.Zero)
            {
                // Create or clear a dispatcher timer instance
                if (_delayTimer == null)
                {
                    _delayTimer = new DispatcherTimer();
                    _delayTimer.Tick += PopulateDropDown;
                }

                // Set the new tick interval
                _delayTimer.Interval = newValue;
            }
        }

        /// <summary>
        /// IsDropDownOpenProperty property changed handler.
        /// </summary>
        /// <param name="e">Event arguments.</param>
        private void OnIsDropDownOpenChanged(AvaloniaPropertyChangedEventArgs e)
        {
            // Ignore the change if requested
            if (_ignorePropertyChange)
            {
                _ignorePropertyChange = false;
                return;
            }

            bool oldValue = (bool)e.OldValue;
            bool newValue = (bool)e.NewValue;

            if (newValue)
            {
                TextUpdated(Text, true);
            }
            else
            {
                ClosingDropDown(oldValue);
            }

            UpdatePseudoClasses();
        }

        private void OnSelectedItemPropertyChanged(AvaloniaPropertyChangedEventArgs e)
        {
            if (_ignorePropertyChange)
            {
                _ignorePropertyChange = false;
                return;
            }

            // Update the text display
            if (_skipSelectedItemTextUpdate)
            {
                _skipSelectedItemTextUpdate = false;
            }
            else
            {
                OnSelectedItemChanged(e.NewValue);
            }

            // Fire the SelectionChanged event
            List<object> removed = new List<object>();
            if (e.OldValue != null)
            {
                removed.Add(e.OldValue);
            }

            List<object> added = new List<object>();
            if (e.NewValue != null)
            {
                added.Add(e.NewValue);
            }

            OnSelectionChanged(new SelectionChangedEventArgs(SelectionChangedEvent, removed, added));
        }

        /// <summary>
        /// TextProperty property changed handler.
        /// </summary>
        /// <param name="e">Event arguments.</param>
        private void OnTextPropertyChanged(AvaloniaPropertyChangedEventArgs e)
        {
            TextUpdated((string)e.NewValue, false);
        }

        private void OnSearchTextPropertyChanged(AvaloniaPropertyChangedEventArgs e)
        {
            if (_ignorePropertyChange)
            {
                _ignorePropertyChange = false;
                return;
            }

            // Ensure the property is only written when expected
            if (!_allowWrite)
            {
                // Reset the old value before it was incorrectly written
                _ignorePropertyChange = true;
                SetValue(e.Property, e.OldValue);

                throw new InvalidOperationException("Cannot set read-only property SearchText.");
            }
        }

        /// <summary>
        /// FilterModeProperty property changed handler.
        /// </summary>
        /// <param name="e">Event arguments.</param>
        private void OnFilterModePropertyChanged(AvaloniaPropertyChangedEventArgs e)
        {
            AutoCompleteFilterMode mode = (AutoCompleteFilterMode)e.NewValue;

            // Sets the filter predicate for the new value
            TextFilter = AutoCompleteSearch.GetFilter(mode);
        }

        /// <summary>
        /// ItemFilterProperty property changed handler.
        /// </summary>
        /// <param name="e">Event arguments.</param>
        private void OnItemFilterPropertyChanged(AvaloniaPropertyChangedEventArgs e)
        {
            AutoCompleteFilterPredicate<object> value = e.NewValue as AutoCompleteFilterPredicate<object>;

            // If null, revert to the "None" predicate
            if (value == null)
            {
                FilterMode = AutoCompleteFilterMode.None;
            }
            else
            {
                FilterMode = AutoCompleteFilterMode.Custom;
                TextFilter = null;
            }
        }

        /// <summary>
        /// ItemsSourceProperty property changed handler.
        /// </summary>
        /// <param name="e">Event arguments.</param>
        private void OnItemsPropertyChanged(AvaloniaPropertyChangedEventArgs e)
        {
            OnItemsChanged((IEnumerable)e.NewValue);
        }

        private void OnItemTemplatePropertyChanged(AvaloniaPropertyChangedEventArgs e)
        {
            if (!_settingItemTemplateFromValueMemberBinding)
                _itemTemplateIsFromValueMemberBinding = false;
        }
        private void OnValueMemberBindingChanged(IBinding value)
        {
            if(_itemTemplateIsFromValueMemberBinding)
            {
                var template =
                    new FuncDataTemplate(
                        typeof(object),
                        (o, _) =>
                        {
                            var control = new ContentControl();
                            control.Bind(ContentControl.ContentProperty, value);
                            return control;
                        });

                _settingItemTemplateFromValueMemberBinding = true;
                ItemTemplate = template;
                _settingItemTemplateFromValueMemberBinding = false;
            }
        }

        static AutoCompleteBox()
        {
            FocusableProperty.OverrideDefaultValue<AutoCompleteBox>(true);

            MinimumPopulateDelayProperty.Changed.AddClassHandler<AutoCompleteBox>((x,e) => x.OnMinimumPopulateDelayChanged(e));
            IsDropDownOpenProperty.Changed.AddClassHandler<AutoCompleteBox>((x,e) => x.OnIsDropDownOpenChanged(e));
            SelectedItemProperty.Changed.AddClassHandler<AutoCompleteBox>((x,e) => x.OnSelectedItemPropertyChanged(e));
            TextProperty.Changed.AddClassHandler<AutoCompleteBox>((x,e) => x.OnTextPropertyChanged(e));
            SearchTextProperty.Changed.AddClassHandler<AutoCompleteBox>((x,e) => x.OnSearchTextPropertyChanged(e));
            FilterModeProperty.Changed.AddClassHandler<AutoCompleteBox>((x,e) => x.OnFilterModePropertyChanged(e));
            ItemFilterProperty.Changed.AddClassHandler<AutoCompleteBox>((x,e) => x.OnItemFilterPropertyChanged(e));
            ItemsProperty.Changed.AddClassHandler<AutoCompleteBox>((x,e) => x.OnItemsPropertyChanged(e));
            IsEnabledProperty.Changed.AddClassHandler<AutoCompleteBox>((x,e) => x.OnControlIsEnabledChanged(e));
        }

        /// <summary>
        /// Initializes a new instance of the
        /// <see cref="T:Avalonia.Controls.AutoCompleteBox" /> class.
        /// </summary>
        public AutoCompleteBox()
        {
            ClearView();
        }

        /// <summary>
        /// Gets or sets the minimum number of characters required to be entered
        /// in the text box before the
        /// <see cref="T:Avalonia.Controls.AutoCompleteBox" /> displays
        /// possible matches.
        /// matches.
        /// </summary>
        /// <value>
        /// The minimum number of characters to be entered in the text box
        /// before the <see cref="T:Avalonia.Controls.AutoCompleteBox" />
        /// displays possible matches. The default is 1.
        /// </value>
        /// <remarks>
        /// If you set MinimumPrefixLength to -1, the AutoCompleteBox will
        /// not provide possible matches. There is no maximum value, but
        /// setting MinimumPrefixLength to value that is too large will
        /// prevent the AutoCompleteBox from providing possible matches as well.
        /// </remarks>
        public int MinimumPrefixLength
        {
            get { return GetValue(MinimumPrefixLengthProperty); }
            set { SetValue(MinimumPrefixLengthProperty, value); }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the first possible match
        /// found during the filtering process will be displayed automatically
        /// in the text box.
        /// </summary>
        /// <value>
        /// True if the first possible match found will be displayed
        /// automatically in the text box; otherwise, false. The default is
        /// false.
        /// </value>
        public bool IsTextCompletionEnabled
        {
            get { return GetValue(IsTextCompletionEnabledProperty); }
            set { SetValue(IsTextCompletionEnabledProperty, value); }
        }

        /// <summary>
        /// Gets or sets the <see cref="T:Avalonia.DataTemplate" /> used
        /// to display each item in the drop-down portion of the control.
        /// </summary>
        /// <value>The <see cref="T:Avalonia.DataTemplate" /> used to
        /// display each item in the drop-down. The default is null.</value>
        /// <remarks>
        /// You use the ItemTemplate property to specify the visualization
        /// of the data objects in the drop-down portion of the AutoCompleteBox
        /// control. If your AutoCompleteBox is bound to a collection and you
        /// do not provide specific display instructions by using a
        /// DataTemplate, the resulting UI of each item is a string
        /// representation of each object in the underlying collection.
        /// </remarks>
        public IDataTemplate ItemTemplate
        {
            get { return GetValue(ItemTemplateProperty); }
            set { SetValue(ItemTemplateProperty, value); }
        }

        /// <summary>
        /// Gets or sets the minimum delay, after text is typed
        /// in the text box before the
        /// <see cref="T:Avalonia.Controls.AutoCompleteBox" /> control
        /// populates the list of possible matches in the drop-down.
        /// </summary>
        /// <value>The minimum delay, after text is typed in
        /// the text box, but before the
        /// <see cref="T:Avalonia.Controls.AutoCompleteBox" /> populates
        /// the list of possible matches in the drop-down. The default is 0.</value>
        public TimeSpan MinimumPopulateDelay
        {
            get { return GetValue(MinimumPopulateDelayProperty); }
            set { SetValue(MinimumPopulateDelayProperty, value); }
        }

        /// <summary>
        /// Gets or sets the maximum height of the drop-down portion of the
        /// <see cref="T:Avalonia.Controls.AutoCompleteBox" /> control.
        /// </summary>
        /// <value>The maximum height of the drop-down portion of the
        /// <see cref="T:Avalonia.Controls.AutoCompleteBox" /> control.
        /// The default is <see cref="F:System.Double.PositiveInfinity" />.</value>
        /// <exception cref="T:System.ArgumentException">The specified value is less than 0.</exception>
        public double MaxDropDownHeight
        {
            get { return GetValue(MaxDropDownHeightProperty); }
            set { SetValue(MaxDropDownHeightProperty, value); }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the drop-down portion of
        /// the control is open.
        /// </summary>
        /// <value>
        /// True if the drop-down is open; otherwise, false. The default is
        /// false.
        /// </value>
        public bool IsDropDownOpen
        {
            get { return  _isDropDownOpen; }
            set { SetAndRaise(IsDropDownOpenProperty, ref  _isDropDownOpen, value); }
        }

        /// <summary>
        /// Gets or sets the  <see cref="T:Avalonia.Data.Binding" /> that
        /// is used to get the values for display in the text portion of
        /// the <see cref="T:Avalonia.Controls.AutoCompleteBox" />
        /// control.
        /// </summary>
        /// <value>The <see cref="T:Avalonia.Data.IBinding" /> object used
        /// when binding to a collection property.</value>
        [AssignBinding]
        public IBinding ValueMemberBinding
        {
            get { return _valueBindingEvaluator?.ValueBinding; }
            set
            {
                if (ValueMemberBinding != value)
                {
                    _valueBindingEvaluator = new BindingEvaluator<string>(value);
                    OnValueMemberBindingChanged(value);
                }
            }
        }

        /// <summary>
        /// Gets or sets the selected item in the drop-down.
        /// </summary>
        /// <value>The selected item in the drop-down.</value>
        /// <remarks>
        /// If the IsTextCompletionEnabled property is true and text typed by
        /// the user matches an item in the ItemsSource collection, which is
        /// then displayed in the text box, the SelectedItem property will be
        /// a null reference.
        /// </remarks>
        public object SelectedItem
        {
            get { return _selectedItem; }
            set { SetAndRaise(SelectedItemProperty, ref _selectedItem, value); }
        }

        /// <summary>
        /// Gets or sets the text in the text box portion of the
        /// <see cref="T:Avalonia.Controls.AutoCompleteBox" /> control.
        /// </summary>
        /// <value>The text in the text box portion of the
        /// <see cref="T:Avalonia.Controls.AutoCompleteBox" /> control.</value>
        public string Text
        {
            get { return _text; }
            set { SetAndRaise(TextProperty, ref _text, value); }
        }

        /// <summary>
        /// Gets the text that is used to filter items in the
        /// <see cref="P:Avalonia.Controls.AutoCompleteBox.ItemsSource" />
        /// item collection.
        /// </summary>
        /// <value>The text that is used to filter items in the
        /// <see cref="P:Avalonia.Controls.AutoCompleteBox.ItemsSource" />
        /// item collection.</value>
        /// <remarks>
        /// The SearchText value is typically the same as the
        /// Text property, but is set after the TextChanged event occurs
        /// and before the Populating event.
        /// </remarks>
        public string SearchText
        {
            get { return _searchText; }
            private set
            {
                try
                {
                    _allowWrite = true;
                    SetAndRaise(SearchTextProperty, ref _searchText, value);
                }
                finally
                {
                    _allowWrite = false;
                }
            }
        }

        /// <summary>
        /// Gets or sets how the text in the text box is used to filter items
        /// specified by the
        /// <see cref="P:Avalonia.Controls.AutoCompleteBox.ItemsSource" />
        /// property for display in the drop-down.
        /// </summary>
        /// <value>One of the
        /// <see cref="T:Avalonia.Controls.AutoCompleteFilterMode" />
        /// values The default is
        /// <see cref="F:Avalonia.Controls.AutoCompleteFilterMode.StartsWith" />.</value>
        /// <exception cref="T:System.ArgumentException">The specified value is
        /// not a valid
        /// <see cref="T:Avalonia.Controls.AutoCompleteFilterMode" />.</exception>
        /// <remarks>
        /// Use the FilterMode property to specify how possible matches are
        /// filtered. For example, possible matches can be filtered in a
        /// predefined or custom way. The search mode is automatically set to
        /// Custom if you set the ItemFilter property.
        /// </remarks>
        public AutoCompleteFilterMode FilterMode
        {
            get { return GetValue(FilterModeProperty); }
            set { SetValue(FilterModeProperty, value); }
        }

        public string Watermark
        {
            get { return GetValue(WatermarkProperty); }
            set { SetValue(WatermarkProperty, value); }
        }

        /// <summary>
        /// Gets or sets the custom method that uses user-entered text to filter
        /// the items specified by the
        /// <see cref="P:Avalonia.Controls.AutoCompleteBox.ItemsSource" />
        /// property for display in the drop-down.
        /// </summary>
        /// <value>The custom method that uses the user-entered text to filter
        /// the items specified by the
        /// <see cref="P:Avalonia.Controls.AutoCompleteBox.ItemsSource" />
        /// property. The default is null.</value>
        /// <remarks>
        /// The filter mode is automatically set to Custom if you set the
        /// ItemFilter property.
        /// </remarks>
        public AutoCompleteFilterPredicate<object> ItemFilter
        {
            get { return _itemFilter; }
            set { SetAndRaise(ItemFilterProperty, ref _itemFilter, value); }
        }

        /// <summary>
        /// Gets or sets the custom method that uses the user-entered text to
        /// filter items specified by the
        /// <see cref="P:Avalonia.Controls.AutoCompleteBox.ItemsSource" />
        /// property in a text-based way for display in the drop-down.
        /// </summary>
        /// <value>The custom method that uses the user-entered text to filter
        /// items specified by the
        /// <see cref="P:Avalonia.Controls.AutoCompleteBox.ItemsSource" />
        /// property in a text-based way for display in the drop-down.</value>
        /// <remarks>
        /// The search mode is automatically set to Custom if you set the
        /// TextFilter property.
        /// </remarks>
        public AutoCompleteFilterPredicate<string> TextFilter
        {
            get { return _textFilter; }
            set { SetAndRaise(TextFilterProperty, ref _textFilter, value); }
        }

        /// <summary>
        /// Gets or sets the custom method that combines the user-entered
        /// text and one of the items specified by the
        /// <see cref="P:Avalonia.Controls.AutoCompleteBox.ItemsSource" />.
        /// </summary>
        /// <value>
        /// The custom method that combines the user-entered
        /// text and one of the items specified by the
        /// <see cref="P:Avalonia.Controls.AutoCompleteBox.ItemsSource" />.
        /// </value>
        public AutoCompleteSelector<object> ItemSelector
        {
            get { return _itemSelector; }
            set { SetAndRaise(ItemSelectorProperty, ref _itemSelector, value); }
        }

        /// <summary>
        /// Gets or sets the custom method that combines the user-entered
        /// text and one of the items specified by the
        /// <see cref="P:Avalonia.Controls.AutoCompleteBox.ItemsSource" />
        /// in a text-based way.
        /// </summary>
        /// <value>
        /// The custom method that combines the user-entered
        /// text and one of the items specified by the
        /// <see cref="P:Avalonia.Controls.AutoCompleteBox.ItemsSource" />
        /// in a text-based way.
        /// </value>
        public AutoCompleteSelector<string> TextSelector
        {
            get { return _textSelector; }
            set { SetAndRaise(TextSelectorProperty, ref _textSelector, value); }
        }

        public Func<string, CancellationToken, Task<IEnumerable<object>>> AsyncPopulator
        {
            get { return _asyncPopulator; }
            set { SetAndRaise(AsyncPopulatorProperty, ref _asyncPopulator, value); }
        }

        /// <summary>
        /// Gets or sets a collection that is used to generate the items for the
        /// drop-down portion of the
        /// <see cref="T:Avalonia.Controls.AutoCompleteBox" /> control.
        /// </summary>
        /// <value>The collection that is used to generate the items of the
        /// drop-down portion of the
        /// <see cref="T:Avalonia.Controls.AutoCompleteBox" /> control.</value>
        public IEnumerable Items
        {
            get { return _itemsEnumerable; }
            set { SetAndRaise(ItemsProperty, ref _itemsEnumerable, value); }
        }

        /// <summary>
        /// Gets or sets the drop down popup control.
        /// </summary>
        private Popup DropDownPopup { get; set; }

        /// <summary>
        /// Gets or sets the Text template part.
        /// </summary>
        private TextBox TextBox
        {
            get { return _textBox; }
            set
            {
                _textBoxSubscriptions?.Dispose();
                _textBox = value;

                // Attach handlers
                if (_textBox != null)
                {
                    _textBoxSubscriptions =
                        _textBox.GetObservable(TextBox.TextProperty)
                                .Skip(1)
                                .Subscribe(_ => OnTextBoxTextChanged());

                    if (Text != null)
                    {
                        UpdateTextValue(Text);
                    }
                }
            }
        }

        private int TextBoxSelectionStart
        {
            get
            {
                if (TextBox != null)
                {
                    return Math.Min(TextBox.SelectionStart, TextBox.SelectionEnd);
                }
                else
                {
                    return 0;
                }
            }
        }
        private int TextBoxSelectionLength
        {
            get
            {
                if (TextBox != null)
                {
                    return Math.Abs(TextBox.SelectionEnd - TextBox.SelectionStart);
                }
                else
                {
                    return 0;
                }
            }
        }

        /// <summary>
        /// Gets or sets the selection adapter used to populate the drop-down
        /// with a list of selectable items.
        /// </summary>
        /// <value>The selection adapter used to populate the drop-down with a
        /// list of selectable items.</value>
        /// <remarks>
        /// You can use this property when you create an automation peer to
        /// use with AutoCompleteBox or deriving from AutoCompleteBox to
        /// create a custom control.
        /// </remarks>
        protected ISelectionAdapter SelectionAdapter
        {
            get { return _adapter; }
            set
            {
                if (_adapter != null)
                {
                    _adapter.SelectionChanged -= OnAdapterSelectionChanged;
                    _adapter.Commit -= OnAdapterSelectionComplete;
                    _adapter.Cancel -= OnAdapterSelectionCanceled;
                    _adapter.Cancel -= OnAdapterSelectionComplete;
                    _adapter.Items = null;
                }

                _adapter = value;

                if (_adapter != null)
                {
                    _adapter.SelectionChanged += OnAdapterSelectionChanged;
                    _adapter.Commit += OnAdapterSelectionComplete;
                    _adapter.Cancel += OnAdapterSelectionCanceled;
                    _adapter.Cancel += OnAdapterSelectionComplete;
                    _adapter.Items = _view;
                }
            }
        }

        /// <summary>
        /// Returns the
        /// <see cref="T:Avalonia.Controls.ISelectionAdapter" /> part, if
        /// possible.
        /// </summary>
        /// <returns>
        /// A <see cref="T:Avalonia.Controls.ISelectionAdapter" /> object,
        /// if possible. Otherwise, null.
        /// </returns>
        protected virtual ISelectionAdapter GetSelectionAdapterPart(INameScope nameScope)
        {
            ISelectionAdapter adapter = null;
            SelectingItemsControl selector = nameScope.Find<SelectingItemsControl>(ElementSelector);
            if (selector != null)
            {
                // Check if it is already an IItemsSelector
                adapter = selector as ISelectionAdapter;
                if (adapter == null)
                {
                    // Built in support for wrapping a Selector control
                    adapter = new SelectingItemsControlSelectionAdapter(selector);
                }
            }
            if (adapter == null)
            {
                adapter = nameScope.Find<ISelectionAdapter>(ElementSelectionAdapter);
            }
            return adapter;
        }

        /// <summary>
        /// Builds the visual tree for the
        /// <see cref="T:Avalonia.Controls.AutoCompleteBox" /> control
        /// when a new template is applied.
        /// </summary>
        protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
        {

            if (DropDownPopup != null)
            {
                DropDownPopup.Closed -= DropDownPopup_Closed;
                DropDownPopup = null;
            }

            // Set the template parts. Individual part setters remove and add
            // any event handlers.
            Popup popup = e.NameScope.Find<Popup>(ElementPopup);
            if (popup != null)
            {
                DropDownPopup = popup;
                DropDownPopup.Closed += DropDownPopup_Closed;
            }

            SelectionAdapter = GetSelectionAdapterPart(e.NameScope);
            TextBox = e.NameScope.Find<TextBox>(ElementTextBox);

            // If the drop down property indicates that the popup is open,
            // flip its value to invoke the changed handler.
            if (IsDropDownOpen && DropDownPopup != null && !DropDownPopup.IsOpen)
            {
                OpeningDropDown(false);
            }

            base.OnApplyTemplate(e);
        }

        /// <summary>
        /// Called to update the validation state for properties for which data validation is
        /// enabled.
        /// </summary>
        /// <param name="property">The property.</param>
        /// <param name="value">The new binding value for the property.</param>
        protected override void UpdateDataValidation<T>(AvaloniaProperty<T> property, BindingValue<T> value)
        {
            if (property == TextProperty || property == SelectedItemProperty)
            {
                DataValidationErrors.SetError(this, value.Error);
            }
        }

        /// <summary>
        /// Provides handling for the
        /// <see cref="E:Avalonia.InputElement.KeyDown" /> event.
        /// </summary>
        /// <param name="e">A <see cref="T:Avalonia.Input.KeyEventArgs" />
        /// that contains the event data.</param>
        protected override void OnKeyDown(KeyEventArgs e)
        {
            Contract.Requires<ArgumentNullException>(e != null);

            base.OnKeyDown(e);

            if (e.Handled || !IsEnabled)
            {
                return;
            }

            // The drop down is open, pass along the key event arguments to the
            // selection adapter. If it isn't handled by the adapter's logic,
            // then we handle some simple navigation scenarios for controlling
            // the drop down.
            if (IsDropDownOpen)
            {
                if (SelectionAdapter != null)
                {
                    SelectionAdapter.HandleKeyDown(e);
                    if (e.Handled)
                    {
                        return;
                    }
                }

                if (e.Key == Key.Escape)
                {
                    OnAdapterSelectionCanceled(this, new RoutedEventArgs());
                    e.Handled = true;
                }
            }
            else
            {
                // The drop down is not open, the Down key will toggle it open.
                if (e.Key == Key.Down)
                {
                    IsDropDownOpen = true;
                    e.Handled = true;
                }
            }

            // Standard drop down navigation
            switch (e.Key)
            {
                case Key.F4:
                    IsDropDownOpen = !IsDropDownOpen;
                    e.Handled = true;
                    break;

                case Key.Enter:
                    if (IsDropDownOpen)
                    {
                        OnAdapterSelectionComplete(this, new RoutedEventArgs());
                        e.Handled = true;
                    }
                    break;

                default:
                    break;
            }
        }

        /// <summary>
        /// Provides handling for the
        /// <see cref="E:Avalonia.UIElement.GotFocus" /> event.
        /// </summary>
        /// <param name="e">A <see cref="T:Avalonia.RoutedEventArgs" />
        /// that contains the event data.</param>
        protected override void OnGotFocus(GotFocusEventArgs e)
        {
            base.OnGotFocus(e);
            FocusChanged(HasFocus());
        }

        /// <summary>
        /// Provides handling for the
        /// <see cref="E:Avalonia.UIElement.LostFocus" /> event.
        /// </summary>
        /// <param name="e">A <see cref="T:Avalonia.RoutedEventArgs" />
        /// that contains the event data.</param>
        protected override void OnLostFocus(RoutedEventArgs e)
        {
            base.OnLostFocus(e);
            FocusChanged(HasFocus());
        }

        /// <summary>
        /// Determines whether the text box or drop-down portion of the
        /// <see cref="T:Avalonia.Controls.AutoCompleteBox" /> control has
        /// focus.
        /// </summary>
        /// <returns>true to indicate the
        /// <see cref="T:Avalonia.Controls.AutoCompleteBox" /> has focus;
        /// otherwise, false.</returns>
        protected bool HasFocus()
        {
            var focused = FocusManager.Instance.Current;

            while (focused != null)
            {
                if (object.ReferenceEquals(focused, this))
                {
                    return true;
                }

                // This helps deal with popups that may not be in the same
                // visual tree
                var parent = focused.InputParent;
                if (parent == null)
                {
                    // Try the logical parent.
                    IControl element = focused as IControl;
                    if (element != null)
                    {
                        parent = element.Parent;
                    }
                }
                focused = parent;
            }
            return false;
        }

        /// <summary>
        /// Handles the FocusChanged event.
        /// </summary>
        /// <param name="hasFocus">A value indicating whether the control
        /// currently has the focus.</param>
        private void FocusChanged(bool hasFocus)
        {
            // The OnGotFocus & OnLostFocus are asynchronously and cannot
            // reliably tell you that have the focus.  All they do is let you
            // know that the focus changed sometime in the past.  To determine
            // if you currently have the focus you need to do consult the
            // FocusManager (see HasFocus()).

            bool wasFocused = _isFocused;
            _isFocused = hasFocus;

            if (hasFocus)
            {

                if (!wasFocused && TextBox != null && TextBoxSelectionLength <= 0)
                {
                    TextBox.Focus();
                    TextBox.SelectionStart = 0;
                    TextBox.SelectionEnd = TextBox.Text?.Length ?? 0;
                }
            }
            else
            {
                IsDropDownOpen = false;
                _userCalledPopulate = false;
                ClearTextBoxSelection();
            }

            _isFocused = hasFocus;
        }

        /// <summary>
        /// Occurs when the text in the text box portion of the
        /// <see cref="T:Avalonia.Controls.AutoCompleteBox" /> changes.
        /// </summary>
        public event EventHandler TextChanged;

        /// <summary>
        /// Occurs when the
        /// <see cref="T:Avalonia.Controls.AutoCompleteBox" /> is
        /// populating the drop-down with possible matches based on the
        /// <see cref="P:Avalonia.Controls.AutoCompleteBox.Text" />
        /// property.
        /// </summary>
        /// <remarks>
        /// If the event is canceled, by setting the PopulatingEventArgs.Cancel
        /// property to true, the AutoCompleteBox will not automatically
        /// populate the selection adapter contained in the drop-down.
        /// In this case, if you want possible matches to appear, you must
        /// provide the logic for populating the selection adapter.
        /// </remarks>
        public event EventHandler<PopulatingEventArgs> Populating;

        /// <summary>
        /// Occurs when the
        /// <see cref="T:Avalonia.Controls.AutoCompleteBox" /> has
        /// populated the drop-down with possible matches based on the
        /// <see cref="P:Avalonia.Controls.AutoCompleteBox.Text" />
        /// property.
        /// </summary>
        public event EventHandler<PopulatedEventArgs> Populated;

        /// <summary>
        /// Occurs when the value of the
        /// <see cref="P:Avalonia.Controls.AutoCompleteBox.IsDropDownOpen" />
        /// property is changing from false to true.
        /// </summary>
        public event EventHandler<CancelEventArgs> DropDownOpening;

        /// <summary>
        /// Occurs when the value of the
        /// <see cref="P:Avalonia.Controls.AutoCompleteBox.IsDropDownOpen" />
        /// property has changed from false to true and the drop-down is open.
        /// </summary>
        public event EventHandler DropDownOpened;

        /// <summary>
        /// Occurs when the
        /// <see cref="P:Avalonia.Controls.AutoCompleteBox.IsDropDownOpen" />
        /// property is changing from true to false.
        /// </summary>
        public event EventHandler<CancelEventArgs> DropDownClosing;

        /// <summary>
        /// Occurs when the
        /// <see cref="P:Avalonia.Controls.AutoCompleteBox.IsDropDownOpen" />
        /// property was changed from true to false and the drop-down is open.
        /// </summary>
        public event EventHandler DropDownClosed;

        /// <summary>
        /// Occurs when the selected item in the drop-down portion of the
        /// <see cref="T:Avalonia.Controls.AutoCompleteBox" /> has
        /// changed.
        /// </summary>
        public event EventHandler<SelectionChangedEventArgs> SelectionChanged
        {
            add { AddHandler(SelectionChangedEvent, value); }
            remove { RemoveHandler(SelectionChangedEvent, value); }
        }

        /// <summary>
        /// Raises the
        /// <see cref="E:Avalonia.Controls.AutoCompleteBox.Populating" />
        /// event.
        /// </summary>
        /// <param name="e">A
        /// <see cref="T:Avalonia.Controls.PopulatingEventArgs" /> that
        /// contains the event data.</param>
        protected virtual void OnPopulating(PopulatingEventArgs e)
        {
            Populating?.Invoke(this, e);
        }

        /// <summary>
        /// Raises the
        /// <see cref="E:Avalonia.Controls.AutoCompleteBox.Populated" />
        /// event.
        /// </summary>
        /// <param name="e">A
        /// <see cref="T:Avalonia.Controls.PopulatedEventArgs" />
        /// that contains the event data.</param>
        protected virtual void OnPopulated(PopulatedEventArgs e)
        {
            Populated?.Invoke(this, e);
        }

        /// <summary>
        /// Raises the
        /// <see cref="E:Avalonia.Controls.AutoCompleteBox.SelectionChanged" />
        /// event.
        /// </summary>
        /// <param name="e">A
        /// <see cref="T:Avalonia.Controls.SelectionChangedEventArgs" />
        /// that contains the event data.</param>
        protected virtual void OnSelectionChanged(SelectionChangedEventArgs e)
        {
            RaiseEvent(e);
        }

        /// <summary>
        /// Raises the
        /// <see cref="E:Avalonia.Controls.AutoCompleteBox.DropDownOpening" />
        /// event.
        /// </summary>
        /// <param name="e">A
        /// <see cref="T:Avalonia.Controls.CancelEventArgs" />
        /// that contains the event data.</param>
        protected virtual void OnDropDownOpening(CancelEventArgs e)
        {
            DropDownOpening?.Invoke(this, e);
        }

        /// <summary>
        /// Raises the
        /// <see cref="E:Avalonia.Controls.AutoCompleteBox.DropDownOpened" />
        /// event.
        /// </summary>
        /// <param name="e">A
        /// <see cref="T:System.EventArgs" />
        /// that contains the event data.</param>
        protected virtual void OnDropDownOpened(EventArgs e)
        {
            DropDownOpened?.Invoke(this, e);
        }

        /// <summary>
        /// Raises the
        /// <see cref="E:Avalonia.Controls.AutoCompleteBox.DropDownClosing" />
        /// event.
        /// </summary>
        /// <param name="e">A
        /// <see cref="T:Avalonia.Controls.CancelEventArgs" />
        /// that contains the event data.</param>
        protected virtual void OnDropDownClosing(CancelEventArgs e)
        {
            DropDownClosing?.Invoke(this, e);
        }

        /// <summary>
        /// Raises the
        /// <see cref="E:Avalonia.Controls.AutoCompleteBox.DropDownClosed" />
        /// event.
        /// </summary>
        /// <param name="e">A
        /// <see cref="T:System.EventArgs" />
        /// which contains the event data.</param>
        protected virtual void OnDropDownClosed(EventArgs e)
        {
            DropDownClosed?.Invoke(this, e);
        }

        /// <summary>
        /// Raises the
        /// <see cref="E:Avalonia.Controls.AutoCompleteBox.TextChanged" />
        /// event.
        /// </summary>
        /// <param name="e">A <see cref="T:Avalonia.RoutedEventArgs" />
        /// that contains the event data.</param>
        protected virtual void OnTextChanged(RoutedEventArgs e)
        {
            TextChanged?.Invoke(this, e);
        }

        /// <summary>
        /// Begin closing the drop-down.
        /// </summary>
        /// <param name="oldValue">The original value.</param>
        private void ClosingDropDown(bool oldValue)
        {
            var args = new CancelEventArgs();
            OnDropDownClosing(args);

            if (args.Cancel)
            {
                _ignorePropertyChange = true;
                SetValue(IsDropDownOpenProperty, oldValue);
            }
            else
            {
                CloseDropDown();
            }

            UpdatePseudoClasses();
        }

        /// <summary>
        /// Begin opening the drop down by firing cancelable events, opening the
        /// drop-down or reverting, depending on the event argument values.
        /// </summary>
        /// <param name="oldValue">The original value, if needed for a revert.</param>
        private void OpeningDropDown(bool oldValue)
        {
            var args = new CancelEventArgs();

            // Opening
            OnDropDownOpening(args);

            if (args.Cancel)
            {
                _ignorePropertyChange = true;
                SetValue(IsDropDownOpenProperty, oldValue);
            }
            else
            {
                OpenDropDown();
            }

            UpdatePseudoClasses();
        }

        /// <summary>
        /// Connects to the DropDownPopup Closed event.
        /// </summary>
        /// <param name="sender">The source object.</param>
        /// <param name="e">The event data.</param>
        private void DropDownPopup_Closed(object sender, EventArgs e)
        {
            // Force the drop down dependency property to be false.
            if (IsDropDownOpen)
            {
                IsDropDownOpen = false;
            }

            // Fire the DropDownClosed event
            if (_popupHasOpened)
            {
                OnDropDownClosed(EventArgs.Empty);
            }
        }

        /// <summary>
        /// Handles the timer tick when using a populate delay.
        /// </summary>
        /// <param name="sender">The source object.</param>
        /// <param name="e">The event arguments.</param>
        private void PopulateDropDown(object sender, EventArgs e)
        {
            if (_delayTimer != null)
            {
                _delayTimer.Stop();
            }

            // Update the prefix/search text.
            SearchText = Text;

            if(TryPopulateAsync(SearchText))
            {
                return;
            }

            // The Populated event enables advanced, custom filtering. The
            // client needs to directly update the ItemsSource collection or
            // call the Populate method on the control to continue the
            // display process if Cancel is set to true.
            PopulatingEventArgs populating = new PopulatingEventArgs(SearchText);
            OnPopulating(populating);
            if (!populating.Cancel)
            {
                PopulateComplete();
            }
        }
        private bool TryPopulateAsync(string searchText)
        {
            _populationCancellationTokenSource?.Cancel(false);
            _populationCancellationTokenSource?.Dispose();
            _populationCancellationTokenSource = null;

            if(_asyncPopulator == null)
            {
                return false;
            }

            _populationCancellationTokenSource = new CancellationTokenSource();
            var task = PopulateAsync(searchText, _populationCancellationTokenSource.Token);
            if (task.Status == TaskStatus.Created)
                task.Start();

            return true;
        }
        private async Task PopulateAsync(string searchText, CancellationToken cancellationToken)
        {

            try
            {
                IEnumerable<object> result = await _asyncPopulator.Invoke(searchText, cancellationToken);
                var resultList = result.ToList();

                if (cancellationToken.IsCancellationRequested)
                {
                    return;
                }

                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    if (!cancellationToken.IsCancellationRequested)
                    {
                        Items = resultList;
                        PopulateComplete();
                    }
                });
            }
            catch (TaskCanceledException)
            { }
            finally
            {
                _populationCancellationTokenSource?.Dispose();
                _populationCancellationTokenSource = null;
            }

        }

        /// <summary>
        /// Private method that directly opens the popup, checks the expander
        /// button, and then fires the Opened event.
        /// </summary>
        private void OpenDropDown()
        {
            if (DropDownPopup != null)
            {
                DropDownPopup.IsOpen = true;
            }
            _popupHasOpened = true;
            OnDropDownOpened(EventArgs.Empty);
        }

        /// <summary>
        /// Private method that directly closes the popup, flips the Checked
        /// value, and then fires the Closed event.
        /// </summary>
        private void CloseDropDown()
        {
            if (_popupHasOpened)
            {
                if (SelectionAdapter != null)
                {
                    SelectionAdapter.SelectedItem = null;
                }
                if (DropDownPopup != null)
                {
                    DropDownPopup.IsOpen = false;
                }
                OnDropDownClosed(EventArgs.Empty);
            }
        }

        /// <summary>
        /// Formats an Item for text comparisons based on Converter
        /// and ConverterCulture properties.
        /// </summary>
        /// <param name="value">The object to format.</param>
        /// <param name="clearDataContext">A value indicating whether to clear
        /// the data context after the lookup is performed.</param>
        /// <returns>Formatted Value.</returns>
        private string FormatValue(object value, bool clearDataContext)
        {
            string result = FormatValue(value);
            if(clearDataContext && _valueBindingEvaluator != null)
            {
                _valueBindingEvaluator.ClearDataContext();
            }

            return result;
        }

        /// <summary>
        /// Converts the specified object to a string by using the
        /// <see cref="P:Avalonia.Data.Binding.Converter" /> and
        /// <see cref="P:Avalonia.Data.Binding.ConverterCulture" /> values
        /// of the binding object specified by the
        /// <see cref="P:Avalonia.Controls.AutoCompleteBox.ValueMemberBinding" />
        /// property.
        /// </summary>
        /// <param name="value">The object to format as a string.</param>
        /// <returns>The string representation of the specified object.</returns>
        /// <remarks>
        /// Override this method to provide a custom string conversion.
        /// </remarks>
        protected virtual string FormatValue(object value)
        {
            if (_valueBindingEvaluator != null)
            {
                return _valueBindingEvaluator.GetDynamicValue(value) ?? String.Empty;
            }

            return value == null ? String.Empty : value.ToString();
        }

        /// <summary>
        /// Handle the TextChanged event that is directly attached to the
        /// TextBox part. This ensures that only user initiated actions will
        /// result in an AutoCompleteBox suggestion and operation.
        /// </summary>
        private void OnTextBoxTextChanged()
        {
            //Uses Dispatcher.Post to allow the TextBox selection to update before processing
            Dispatcher.UIThread.Post(() =>
            {
                // Call the central updated text method as a user-initiated action
                TextUpdated(_textBox.Text, true);
            });
        }

        /// <summary>
        /// Updates both the text box value and underlying text dependency
        /// property value if and when they change. Automatically fires the
        /// text changed events when there is a change.
        /// </summary>
        /// <param name="value">The new string value.</param>
        private void UpdateTextValue(string value)
        {
            UpdateTextValue(value, null);
        }

        /// <summary>
        /// Updates both the text box value and underlying text dependency
        /// property value if and when they change. Automatically fires the
        /// text changed events when there is a change.
        /// </summary>
        /// <param name="value">The new string value.</param>
        /// <param name="userInitiated">A nullable bool value indicating whether
        /// the action was user initiated. In a user initiated mode, the
        /// underlying text dependency property is updated. In a non-user
        /// interaction, the text box value is updated. When user initiated is
        /// null, all values are updated.</param>
        private void UpdateTextValue(string value, bool? userInitiated)
        {
            bool callTextChanged = false;
            // Update the Text dependency property
            if ((userInitiated ?? true) && Text != value)
            {
                _ignoreTextPropertyChange++;
                Text = value;
                callTextChanged = true;
            }

            // Update the TextBox's Text dependency property
            if ((userInitiated == null || userInitiated == false) && TextBox != null && TextBox.Text != value)
            {
                _ignoreTextPropertyChange++;
                TextBox.Text = value ?? string.Empty;

                // Text dependency property value was set, fire event
                if (!callTextChanged && (Text == value || Text == null))
                {
                    callTextChanged = true;
                }
            }

            if (callTextChanged)
            {
                OnTextChanged(new RoutedEventArgs());
            }
        }

        /// <summary>
        /// Handle the update of the text for the control from any source,
        /// including the TextBox part and the Text dependency property.
        /// </summary>
        /// <param name="newText">The new text.</param>
        /// <param name="userInitiated">A value indicating whether the update
        /// is a user-initiated action. This should be a True value when the
        /// TextUpdated method is called from a TextBox event handler.</param>
        private void TextUpdated(string newText, bool userInitiated)
        {
            // Only process this event if it is coming from someone outside
            // setting the Text dependency property directly.
            if (_ignoreTextPropertyChange > 0)
            {
                _ignoreTextPropertyChange--;
                return;
            }

            if (newText == null)
            {
                newText = string.Empty;
            }

            // The TextBox.TextChanged event was not firing immediately and
            // was causing an immediate update, even with wrapping. If there is
            // a selection currently, no update should happen.
            if (IsTextCompletionEnabled && TextBox != null && TextBoxSelectionLength > 0 && TextBoxSelectionStart != TextBox.Text.Length)
            {
                return;
            }

            // Evaluate the conditions needed for completion.
            // 1. Minimum prefix length
            // 2. If a delay timer is in use, use it
            bool populateReady = newText.Length >= MinimumPrefixLength && MinimumPrefixLength >= 0;
            if(populateReady && MinimumPrefixLength == 0 && String.IsNullOrEmpty(newText) && String.IsNullOrEmpty(SearchText))
            {
                populateReady = false;
            }
            _userCalledPopulate = populateReady ? userInitiated : false;

            // Update the interface and values only as necessary
            UpdateTextValue(newText, userInitiated);

            if (populateReady)
            {
                _ignoreTextSelectionChange = true;

                if (_delayTimer != null)
                {
                    _delayTimer.Start();
                }
                else
                {
                    PopulateDropDown(this, EventArgs.Empty);
                }
            }
            else
            {
                SearchText = string.Empty;
                if (SelectedItem != null)
                {
                    _skipSelectedItemTextUpdate = true;
                }
                SelectedItem = null;
                if (IsDropDownOpen)
                {
                    IsDropDownOpen = false;
                }
            }
        }

        /// <summary>
        /// A simple helper method to clear the view and ensure that a view
        /// object is always present and not null.
        /// </summary>
        private void ClearView()
        {
            if (_view == null)
            {
                _view = new AvaloniaList<object>();
            }
            else
            {
                _view.Clear();
            }
        }

        /// <summary>
        /// Walks through the items enumeration. Performance is not going to be
        /// perfect with the current implementation.
        /// </summary>
        private void RefreshView()
        {
            if (_items == null)
            {
                ClearView();
                return;
            }

            // Cache the current text value
            string text = Text ?? string.Empty;

            // Determine if any filtering mode is on
            bool stringFiltering = TextFilter != null;
            bool objectFiltering = FilterMode == AutoCompleteFilterMode.Custom && TextFilter == null;

            int view_index = 0;
            int view_count = _view.Count;
            List<object> items = _items;
            foreach (object item in items)
            {
                bool inResults = !(stringFiltering || objectFiltering);
                if (!inResults)
                {
                    inResults = stringFiltering ? TextFilter(text, FormatValue(item)) : ItemFilter(text, item);
                }

                if (view_count > view_index && inResults && _view[view_index] == item)
                {
                    // Item is still in the view
                    view_index++;
                }
                else if (inResults)
                {
                    // Insert the item
                    if (view_count > view_index && _view[view_index] != item)
                    {
                        // Replace item
                        // Unfortunately replacing via index throws a fatal
                        // exception: View[view_index] = item;
                        // Cost: O(n) vs O(1)
                        _view.RemoveAt(view_index);
                        _view.Insert(view_index, item);
                        view_index++;
                    }
                    else
                    {
                        // Add the item
                        if (view_index == view_count)
                        {
                            // Constant time is preferred (Add).
                            _view.Add(item);
                        }
                        else
                        {
                            _view.Insert(view_index, item);
                        }
                        view_index++;
                        view_count++;
                    }
                }
                else if (view_count > view_index && _view[view_index] == item)
                {
                    // Remove the item
                    _view.RemoveAt(view_index);
                    view_count--;
                }
            }

            // Clear the evaluator to discard a reference to the last item
            if (_valueBindingEvaluator != null)
            {
                _valueBindingEvaluator.ClearDataContext();
            }
        }

        /// <summary>
        /// Handle any change to the ItemsSource dependency property, update
        /// the underlying ObservableCollection view, and set the selection
        /// adapter's ItemsSource to the view if appropriate.
        /// </summary>
        /// <param name="newValue">The new enumerable reference.</param>
        private void OnItemsChanged(IEnumerable newValue)
        {
            // Remove handler for oldValue.CollectionChanged (if present)
            _collectionChangeSubscription?.Dispose();
            _collectionChangeSubscription = null;

            // Add handler for newValue.CollectionChanged (if possible)
            if (newValue is INotifyCollectionChanged newValueINotifyCollectionChanged)
            {
                _collectionChangeSubscription = newValueINotifyCollectionChanged.WeakSubscribe(ItemsCollectionChanged);
            }

            // Store a local cached copy of the data
            _items = newValue == null ? null : new List<object>(newValue.Cast<object>().ToList());

            // Clear and set the view on the selection adapter
            ClearView();
            if (SelectionAdapter != null && SelectionAdapter.Items != _view)
            {
                SelectionAdapter.Items = _view;
            }
            if (IsDropDownOpen)
            {
                RefreshView();
            }
        }

        /// <summary>
        /// Method that handles the ObservableCollection.CollectionChanged event for the ItemsSource property.
        /// </summary>
        /// <param name="sender">The object that raised the event.</param>
        /// <param name="e">The event data.</param>
        private void ItemsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            // Update the cache
            if (e.Action == NotifyCollectionChangedAction.Remove && e.OldItems != null)
            {
                for (int index = 0; index < e.OldItems.Count; index++)
                {
                    _items.RemoveAt(e.OldStartingIndex);
                }
            }
            if (e.Action == NotifyCollectionChangedAction.Add && e.NewItems != null && _items.Count >= e.NewStartingIndex)
            {
                for (int index = 0; index < e.NewItems.Count; index++)
                {
                    _items.Insert(e.NewStartingIndex + index, e.NewItems[index]);
                }
            }
            if (e.Action == NotifyCollectionChangedAction.Replace && e.NewItems != null && e.OldItems != null)
            {
                for (int index = 0; index < e.NewItems.Count; index++)
                {
                    _items[e.NewStartingIndex] = e.NewItems[index];
                }
            }

            // Update the view
            if ((e.Action == NotifyCollectionChangedAction.Remove || e.Action == NotifyCollectionChangedAction.Replace) && e.OldItems != null)
            {
                for (int index = 0; index < e.OldItems.Count; index++)
                {
                    _view.Remove(e.OldItems[index]);
                }
            }

            if (e.Action == NotifyCollectionChangedAction.Reset)
            {
                // Significant changes to the underlying data.
                ClearView();
                if (Items != null)
                {
                    _items = new List<object>(Items.Cast<object>().ToList());
                }
            }

            // Refresh the observable collection used in the selection adapter.
            RefreshView();
        }

        /// <summary>
        /// Notifies the
        /// <see cref="T:Avalonia.Controls.AutoCompleteBox" /> that the
        /// <see cref="P:Avalonia.Controls.AutoCompleteBox.Items" />
        /// property has been set and the data can be filtered to provide
        /// possible matches in the drop-down.
        /// </summary>
        /// <remarks>
        /// Call this method when you are providing custom population of
        /// the drop-down portion of the AutoCompleteBox, to signal the control
        /// that you are done with the population process.
        /// Typically, you use PopulateComplete when the population process
        /// is a long-running process and you want to cancel built-in filtering
        ///  of the ItemsSource items. In this case, you can handle the
        /// Populated event and set PopulatingEventArgs.Cancel to true.
        /// When the long-running process has completed you call
        /// PopulateComplete to indicate the drop-down is populated.
        /// </remarks>
        public void PopulateComplete()
        {
            // Apply the search filter
            RefreshView();

            // Fire the Populated event containing the read-only view data.
            PopulatedEventArgs populated = new PopulatedEventArgs(new ReadOnlyCollection<object>(_view));
            OnPopulated(populated);

            if (SelectionAdapter != null && SelectionAdapter.Items != _view)
            {
                SelectionAdapter.Items = _view;
            }

            bool isDropDownOpen = _userCalledPopulate && (_view.Count > 0);
            if (isDropDownOpen != IsDropDownOpen)
            {
                _ignorePropertyChange = true;
                IsDropDownOpen = isDropDownOpen;
            }
            if (IsDropDownOpen)
            {
                OpeningDropDown(false);
            }
            else
            {
                ClosingDropDown(true);
            }

            UpdateTextCompletion(_userCalledPopulate);
        }

        /// <summary>
        /// Performs text completion, if enabled, and a lookup on the underlying
        /// item values for an exact match. Will update the SelectedItem value.
        /// </summary>
        /// <param name="userInitiated">A value indicating whether the operation
        /// was user initiated. Text completion will not be performed when not
        /// directly initiated by the user.</param>
        private void UpdateTextCompletion(bool userInitiated)
        {
            // By default this method will clear the selected value
            object newSelectedItem = null;
            string text = Text;

            // Text search is StartsWith explicit and only when enabled, in
            // line with WPF's ComboBox lookup. When in use it will associate
            // a Value with the Text if it is found in ItemsSource. This is
            // only valid when there is data and the user initiated the action.
            if (_view.Count > 0)
            {
                if (IsTextCompletionEnabled && TextBox != null && userInitiated)
                {
                    int currentLength = TextBox.Text.Length;
                    int selectionStart = TextBoxSelectionStart;
                    if (selectionStart == text.Length && selectionStart > _textSelectionStart)
                    {
                        // When the FilterMode dependency property is set to
                        // either StartsWith or StartsWithCaseSensitive, the
                        // first item in the view is used. This will improve
                        // performance on the lookup. It assumes that the
                        // FilterMode the user has selected is an acceptable
                        // case sensitive matching function for their scenario.
                        object top = FilterMode == AutoCompleteFilterMode.StartsWith || FilterMode == AutoCompleteFilterMode.StartsWithCaseSensitive
                            ? _view[0]
                            : TryGetMatch(text, _view, AutoCompleteSearch.GetFilter(AutoCompleteFilterMode.StartsWith));

                        // If the search was successful, update SelectedItem
                        if (top != null)
                        {
                            newSelectedItem = top;
                            string topString = FormatValue(top, true);

                            // Only replace partially when the two words being the same
                            int minLength = Math.Min(topString.Length, Text.Length);
                            if (AutoCompleteSearch.Equals(Text.Substring(0, minLength), topString.Substring(0, minLength)))
                            {
                                // Update the text
                                UpdateTextValue(topString);

                                // Select the text past the user's caret
                                TextBox.SelectionStart = currentLength;
                                TextBox.SelectionEnd = topString.Length;
                            }
                        }
                    }
                }
                else
                {
                    // Perform an exact string lookup for the text. This is a
                    // design change from the original Toolkit release when the
                    // IsTextCompletionEnabled property behaved just like the
                    // WPF ComboBox's IsTextSearchEnabled property.
                    //
                    // This change provides the behavior that most people expect
                    // to find: a lookup for the value is always performed.
                    newSelectedItem = TryGetMatch(text, _view, AutoCompleteSearch.GetFilter(AutoCompleteFilterMode.EqualsCaseSensitive));
                }
            }

            // Update the selected item property

            if (SelectedItem != newSelectedItem)
            {
                _skipSelectedItemTextUpdate = true;
            }
            SelectedItem = newSelectedItem;

            // Restore updates for TextSelection
            if (_ignoreTextSelectionChange)
            {
                _ignoreTextSelectionChange = false;
                if (TextBox != null)
                {
                    _textSelectionStart = TextBoxSelectionStart;
                }
            }
        }

        /// <summary>
        /// Attempts to look through the view and locate the specific exact
        /// text match.
        /// </summary>
        /// <param name="searchText">The search text.</param>
        /// <param name="view">The view reference.</param>
        /// <param name="predicate">The predicate to use for the partial or
        /// exact match.</param>
        /// <returns>Returns the object or null.</returns>
        private object TryGetMatch(string searchText, AvaloniaList<object> view, AutoCompleteFilterPredicate<string> predicate)
        {
            if (view != null && view.Count > 0)
            {
                foreach (object o in view)
                {
                    if (predicate(searchText, FormatValue(o)))
                    {
                        return o;
                    }
                }
            }

            return null;
        }

        private void UpdatePseudoClasses()
        {
            PseudoClasses.Set(":dropdownopen", IsDropDownOpen);
        }

        private void ClearTextBoxSelection()
        {
            if (TextBox != null)
            {
                int length = TextBox.Text?.Length ?? 0;
                TextBox.SelectionStart = length;
                TextBox.SelectionEnd = length;
            }
        }

        /// <summary>
        /// Called when the selected item is changed, updates the text value
        /// that is displayed in the text box part.
        /// </summary>
        /// <param name="newItem">The new item.</param>
        private void OnSelectedItemChanged(object newItem)
        {
            string text;

            if (newItem == null)
            {
                text = SearchText;
            }
            else if (TextSelector != null)
            {
                text = TextSelector(SearchText, FormatValue(newItem, true));
            }
            else if (ItemSelector != null)
            {
                text = ItemSelector(SearchText, newItem);
            }
            else
            {
                text = FormatValue(newItem, true);
            }

            // Update the Text property and the TextBox values
            UpdateTextValue(text);

            // Move the caret to the end of the text box
            ClearTextBoxSelection();
        }

        /// <summary>
        /// Handles the SelectionChanged event of the selection adapter.
        /// </summary>
        /// <param name="sender">The source object.</param>
        /// <param name="e">The selection changed event data.</param>
        private void OnAdapterSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SelectedItem = _adapter.SelectedItem;
        }

        //TODO Check UpdateTextCompletion
        /// <summary>
        /// Handles the Commit event on the selection adapter.
        /// </summary>
        /// <param name="sender">The source object.</param>
        /// <param name="e">The event data.</param>
        private void OnAdapterSelectionComplete(object sender, RoutedEventArgs e)
        {
            IsDropDownOpen = false;

            // Completion will update the selected value
            //UpdateTextCompletion(false);

            // Text should not be selected
            ClearTextBoxSelection();

            TextBox.Focus();
        }

        /// <summary>
        /// Handles the Cancel event on the selection adapter.
        /// </summary>
        /// <param name="sender">The source object.</param>
        /// <param name="e">The event data.</param>
        private void OnAdapterSelectionCanceled(object sender, RoutedEventArgs e)
        {
            UpdateTextValue(SearchText);

            // Completion will update the selected value
            UpdateTextCompletion(false);
        }

        /// <summary>
        /// A predefined set of filter functions for the known, built-in
        /// AutoCompleteFilterMode enumeration values.
        /// </summary>
        private static class AutoCompleteSearch
        {
            /// <summary>
            /// Index function that retrieves the filter for the provided
            /// AutoCompleteFilterMode.
            /// </summary>
            /// <param name="FilterMode">The built-in search mode.</param>
            /// <returns>Returns the string-based comparison function.</returns>
            public static AutoCompleteFilterPredicate<string> GetFilter(AutoCompleteFilterMode FilterMode)
            {
                switch (FilterMode)
                {
                    case AutoCompleteFilterMode.Contains:
                        return Contains;

                    case AutoCompleteFilterMode.ContainsCaseSensitive:
                        return ContainsCaseSensitive;

                    case AutoCompleteFilterMode.ContainsOrdinal:
                        return ContainsOrdinal;

                    case AutoCompleteFilterMode.ContainsOrdinalCaseSensitive:
                        return ContainsOrdinalCaseSensitive;

                    case AutoCompleteFilterMode.Equals:
                        return Equals;

                    case AutoCompleteFilterMode.EqualsCaseSensitive:
                        return EqualsCaseSensitive;

                    case AutoCompleteFilterMode.EqualsOrdinal:
                        return EqualsOrdinal;

                    case AutoCompleteFilterMode.EqualsOrdinalCaseSensitive:
                        return EqualsOrdinalCaseSensitive;

                    case AutoCompleteFilterMode.StartsWith:
                        return StartsWith;

                    case AutoCompleteFilterMode.StartsWithCaseSensitive:
                        return StartsWithCaseSensitive;

                    case AutoCompleteFilterMode.StartsWithOrdinal:
                        return StartsWithOrdinal;

                    case AutoCompleteFilterMode.StartsWithOrdinalCaseSensitive:
                        return StartsWithOrdinalCaseSensitive;

                    case AutoCompleteFilterMode.None:
                    case AutoCompleteFilterMode.Custom:
                    default:
                        return null;
                }
            }

            /// <summary>
            /// An implementation of the Contains member of string that takes in a
            /// string comparison. The traditional .NET string Contains member uses
            /// StringComparison.Ordinal.
            /// </summary>
            /// <param name="s">The string.</param>
            /// <param name="value">The string value to search for.</param>
            /// <param name="comparison">The string comparison type.</param>
            /// <returns>Returns true when the substring is found.</returns>
            private static bool Contains(string s, string value, StringComparison comparison)
            {
                return s.IndexOf(value, comparison) >= 0;
            }

            /// <summary>
            /// Check if the string value begins with the text.
            /// </summary>
            /// <param name="text">The AutoCompleteBox prefix text.</param>
            /// <param name="value">The item's string value.</param>
            /// <returns>Returns true if the condition is met.</returns>
            public static bool StartsWith(string text, string value)
            {
                return value.StartsWith(text, StringComparison.CurrentCultureIgnoreCase);
            }

            /// <summary>
            /// Check if the string value begins with the text.
            /// </summary>
            /// <param name="text">The AutoCompleteBox prefix text.</param>
            /// <param name="value">The item's string value.</param>
            /// <returns>Returns true if the condition is met.</returns>
            public static bool StartsWithCaseSensitive(string text, string value)
            {
                return value.StartsWith(text, StringComparison.CurrentCulture);
            }

            /// <summary>
            /// Check if the string value begins with the text.
            /// </summary>
            /// <param name="text">The AutoCompleteBox prefix text.</param>
            /// <param name="value">The item's string value.</param>
            /// <returns>Returns true if the condition is met.</returns>
            public static bool StartsWithOrdinal(string text, string value)
            {
                return value.StartsWith(text, StringComparison.OrdinalIgnoreCase);
            }

            /// <summary>
            /// Check if the string value begins with the text.
            /// </summary>
            /// <param name="text">The AutoCompleteBox prefix text.</param>
            /// <param name="value">The item's string value.</param>
            /// <returns>Returns true if the condition is met.</returns>
            public static bool StartsWithOrdinalCaseSensitive(string text, string value)
            {
                return value.StartsWith(text, StringComparison.Ordinal);
            }

            /// <summary>
            /// Check if the prefix is contained in the string value. The current
            /// culture's case insensitive string comparison operator is used.
            /// </summary>
            /// <param name="text">The AutoCompleteBox prefix text.</param>
            /// <param name="value">The item's string value.</param>
            /// <returns>Returns true if the condition is met.</returns>
            public static bool Contains(string text, string value)
            {
                return Contains(value, text, StringComparison.CurrentCultureIgnoreCase);
            }

            /// <summary>
            /// Check if the prefix is contained in the string value.
            /// </summary>
            /// <param name="text">The AutoCompleteBox prefix text.</param>
            /// <param name="value">The item's string value.</param>
            /// <returns>Returns true if the condition is met.</returns>
            public static bool ContainsCaseSensitive(string text, string value)
            {
                return Contains(value, text, StringComparison.CurrentCulture);
            }

            /// <summary>
            /// Check if the prefix is contained in the string value.
            /// </summary>
            /// <param name="text">The AutoCompleteBox prefix text.</param>
            /// <param name="value">The item's string value.</param>
            /// <returns>Returns true if the condition is met.</returns>
            public static bool ContainsOrdinal(string text, string value)
            {
                return Contains(value, text, StringComparison.OrdinalIgnoreCase);
            }

            /// <summary>
            /// Check if the prefix is contained in the string value.
            /// </summary>
            /// <param name="text">The AutoCompleteBox prefix text.</param>
            /// <param name="value">The item's string value.</param>
            /// <returns>Returns true if the condition is met.</returns>
            public static bool ContainsOrdinalCaseSensitive(string text, string value)
            {
                return Contains(value, text, StringComparison.Ordinal);
            }

            /// <summary>
            /// Check if the string values are equal.
            /// </summary>
            /// <param name="text">The AutoCompleteBox prefix text.</param>
            /// <param name="value">The item's string value.</param>
            /// <returns>Returns true if the condition is met.</returns>
            public static bool Equals(string text, string value)
            {
                return value.Equals(text, StringComparison.CurrentCultureIgnoreCase);
            }

            /// <summary>
            /// Check if the string values are equal.
            /// </summary>
            /// <param name="text">The AutoCompleteBox prefix text.</param>
            /// <param name="value">The item's string value.</param>
            /// <returns>Returns true if the condition is met.</returns>
            public static bool EqualsCaseSensitive(string text, string value)
            {
                return value.Equals(text, StringComparison.CurrentCulture);
            }

            /// <summary>
            /// Check if the string values are equal.
            /// </summary>
            /// <param name="text">The AutoCompleteBox prefix text.</param>
            /// <param name="value">The item's string value.</param>
            /// <returns>Returns true if the condition is met.</returns>
            public static bool EqualsOrdinal(string text, string value)
            {
                return value.Equals(text, StringComparison.OrdinalIgnoreCase);
            }

            /// <summary>
            /// Check if the string values are equal.
            /// </summary>
            /// <param name="text">The AutoCompleteBox prefix text.</param>
            /// <param name="value">The item's string value.</param>
            /// <returns>Returns true if the condition is met.</returns>
            public static bool EqualsOrdinalCaseSensitive(string text, string value)
            {
                return value.Equals(text, StringComparison.Ordinal);
            }
        }

        /// <summary>
        /// A framework element that permits a binding to be evaluated in a new data
        /// context leaf node.
        /// </summary>
        /// <typeparam name="T">The type of dynamic binding to return.</typeparam>
        public class BindingEvaluator<T> : Control
        {
            /// <summary>
            /// Gets or sets the string value binding used by the control.
            /// </summary>
            private IBinding _binding;

            #region public T Value

            /// <summary>
            /// Identifies the Value dependency property.
            /// </summary>
            public static readonly StyledProperty<T> ValueProperty =
                AvaloniaProperty.Register<BindingEvaluator<T>, T>(nameof(Value));

            /// <summary>
            /// Gets or sets the data item value.
            /// </summary>
            public T Value
            {
                get { return GetValue(ValueProperty); }
                set { SetValue(ValueProperty, value); }
            }

            #endregion public string Value

            /// <summary>
            /// Gets or sets the value binding.
            /// </summary>
            public IBinding ValueBinding
            {
                get { return _binding; }
                set
                {
                    _binding = value;
                    AvaloniaObjectExtensions.Bind(this, ValueProperty, value);
                }
            }

            /// <summary>
            /// Initializes a new instance of the BindingEvaluator class.
            /// </summary>
            public BindingEvaluator()
            { }

            /// <summary>
            /// Initializes a new instance of the BindingEvaluator class,
            /// setting the initial binding to the provided parameter.
            /// </summary>
            /// <param name="binding">The initial string value binding.</param>
            public BindingEvaluator(IBinding binding)
                : this()
            {
                ValueBinding = binding;
            }

            /// <summary>
            /// Clears the data context so that the control does not keep a
            /// reference to the last-looked up item.
            /// </summary>
            public void ClearDataContext()
            {
                DataContext = null;
            }

            /// <summary>
            /// Updates the data context of the framework element and returns the
            /// updated binding value.
            /// </summary>
            /// <param name="o">The object to use as the data context.</param>
            /// <param name="clearDataContext">If set to true, this parameter will
            /// clear the data context immediately after retrieving the value.</param>
            /// <returns>Returns the evaluated T value of the bound dependency
            /// property.</returns>
            public T GetDynamicValue(object o, bool clearDataContext)
            {
                DataContext = o;
                T value = Value;
                if (clearDataContext)
                {
                    DataContext = null;
                }
                return value;
            }

            /// <summary>
            /// Updates the data context of the framework element and returns the
            /// updated binding value.
            /// </summary>
            /// <param name="o">The object to use as the data context.</param>
            /// <returns>Returns the evaluated T value of the bound dependency
            /// property.</returns>
            public T GetDynamicValue(object o)
            {
                DataContext = o;
                return Value;
            }
        }
    }
}
