using System;
using System.Linq;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Platform;
using Avalonia.Controls.Generators;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Shapes;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.LogicalTree;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace Avalonia.Controls
{
    /// <summary>
    /// A drop-down list control.
    /// </summary>
    public class ComboBox : SelectingItemsControl
    {
        /// <summary>
        /// The default value for the <see cref="ItemsControl.ItemsPanel"/> property.
        /// </summary>
        private static readonly FuncTemplate<IPanel> DefaultPanel =
            new FuncTemplate<IPanel>(() => new VirtualizingStackPanel());

        /// <summary>
        /// Defines the <see cref="IsDropDownOpen"/> property.
        /// </summary>
        public static readonly DirectProperty<ComboBox, bool> IsDropDownOpenProperty =
            AvaloniaProperty.RegisterDirect<ComboBox, bool>(
                nameof(IsDropDownOpen),
                o => o.IsDropDownOpen,
                (o, v) => o.IsDropDownOpen = v);

        /// <summary>
        /// Defines the <see cref="MaxDropDownHeight"/> property.
        /// </summary>
        public static readonly StyledProperty<double> MaxDropDownHeightProperty =
            AvaloniaProperty.Register<ComboBox, double>(nameof(MaxDropDownHeight), 200);

        /// <summary>
        /// Defines the <see cref="SelectionBoxItem"/> property.
        /// </summary>
        public static readonly DirectProperty<ComboBox, object> SelectionBoxItemProperty =
            AvaloniaProperty.RegisterDirect<ComboBox, object>(nameof(SelectionBoxItem), o => o.SelectionBoxItem);

        /// <summary>
        /// Defines the <see cref="VirtualizationMode"/> property.
        /// </summary>
        public static readonly StyledProperty<ItemVirtualizationMode> VirtualizationModeProperty =
            ItemsPresenter.VirtualizationModeProperty.AddOwner<ComboBox>();

        /// <summary>
        /// Defines the <see cref="PlaceholderText"/> property.
        /// </summary>
        public static readonly StyledProperty<string> PlaceholderTextProperty =
            AvaloniaProperty.Register<ComboBox, string>(nameof(PlaceholderText));

        /// <summary>
        /// Defines the <see cref="PlaceholderForeground"/> property.
        /// </summary>
        public static readonly StyledProperty<IBrush> PlaceholderForegroundProperty =
            AvaloniaProperty.Register<ComboBox, IBrush>(nameof(PlaceholderForeground));

        /// <summary>
        /// Defines the <see cref="HorizontalContentAlignment"/> property.
        /// </summary>
        public static readonly StyledProperty<HorizontalAlignment> HorizontalContentAlignmentProperty =
            ContentControl.HorizontalContentAlignmentProperty.AddOwner<ComboBox>();

        /// <summary>
        /// Defines the <see cref="VerticalContentAlignment"/> property.
        /// </summary>
        public static readonly StyledProperty<VerticalAlignment> VerticalContentAlignmentProperty =
            ContentControl.VerticalContentAlignmentProperty.AddOwner<ComboBox>();

        /// <summary>
        /// Defines the <see cref="IsTextSearchEnabled"/> property.
        /// </summary>
        public static readonly StyledProperty<bool> IsTextSearchEnabledProperty =
            AvaloniaProperty.Register<ComboBox, bool>(nameof(IsTextSearchEnabled), true);

        private string _textSearchTerm = string.Empty;
        private DispatcherTimer _textSearchTimer;
        private bool _isDropDownOpen;
        private Popup _popup;
        private object _selectionBoxItem;
        private IDisposable _subscriptionsOnOpen;

        /// <summary>
        /// Initializes static members of the <see cref="ComboBox"/> class.
        /// </summary>
        static ComboBox()
        {
            ItemsPanelProperty.OverrideDefaultValue<ComboBox>(DefaultPanel);
            FocusableProperty.OverrideDefaultValue<ComboBox>(true);
            SelectedItemProperty.Changed.AddClassHandler<ComboBox>((x,e) => x.SelectedItemChanged(e));
            KeyDownEvent.AddClassHandler<ComboBox>((x, e) => x.OnKeyDown(e), Interactivity.RoutingStrategies.Tunnel);
        }

        /// <summary>
        /// Gets or sets a value indicating whether the dropdown is currently open.
        /// </summary>
        public bool IsDropDownOpen
        {
            get { return _isDropDownOpen; }
            set { SetAndRaise(IsDropDownOpenProperty, ref _isDropDownOpen, value); }
        }

        /// <summary>
        /// Gets or sets the maximum height for the dropdown list.
        /// </summary>
        public double MaxDropDownHeight
        {
            get { return GetValue(MaxDropDownHeightProperty); }
            set { SetValue(MaxDropDownHeightProperty, value); }
        }

        /// <summary>
        /// Gets or sets the item to display as the control's content.
        /// </summary>
        protected object SelectionBoxItem
        {
            get { return _selectionBoxItem; }
            set { SetAndRaise(SelectionBoxItemProperty, ref _selectionBoxItem, value); }
        }

        /// <summary>
        /// Gets or sets the PlaceHolder text.
        /// </summary>
        public string PlaceholderText
        {
            get { return GetValue(PlaceholderTextProperty); }
            set { SetValue(PlaceholderTextProperty, value); }
        }

        /// <summary>
        /// Gets or sets the Brush that renders the placeholder text.
        /// </summary>
        public IBrush PlaceholderForeground
        {
            get { return GetValue(PlaceholderForegroundProperty); }
            set { SetValue(PlaceholderForegroundProperty, value); }
        }

        /// <summary>
        /// Gets or sets the virtualization mode for the items.
        /// </summary>
        public ItemVirtualizationMode VirtualizationMode
        {
            get { return GetValue(VirtualizationModeProperty); }
            set { SetValue(VirtualizationModeProperty, value); }
        }

        /// <summary>
        /// Gets or sets the horizontal alignment of the content within the control.
        /// </summary>
        public HorizontalAlignment HorizontalContentAlignment
        {
            get { return GetValue(HorizontalContentAlignmentProperty); }
            set { SetValue(HorizontalContentAlignmentProperty, value); }
        }

        /// <summary>
        /// Gets or sets the vertical alignment of the content within the control.
        /// </summary>
        public VerticalAlignment VerticalContentAlignment
        {
            get { return GetValue(VerticalContentAlignmentProperty); }
            set { SetValue(VerticalContentAlignmentProperty, value); }
        }

        /// <summary>
        /// Gets or sets a value that specifies whether a user can jump to a value by typing.
        /// </summary>
        public bool IsTextSearchEnabled
        {
            get { return GetValue(IsTextSearchEnabledProperty); }
            set { SetValue(IsTextSearchEnabledProperty, value); }
        }

        /// <inheritdoc/>
        protected override IItemContainerGenerator CreateItemContainerGenerator()
        {
            return new ItemContainerGenerator<ComboBoxItem>(
                this,
                ComboBoxItem.ContentProperty,
                ComboBoxItem.ContentTemplateProperty);
        }

        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);
            this.UpdateSelectionBoxItem(SelectedItem);
        }

        /// <inheritdoc/>
        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            if (e.Handled)
                return;

            if (e.Key == Key.F4 ||
                ((e.Key == Key.Down || e.Key == Key.Up) && e.KeyModifiers.HasAllFlags(KeyModifiers.Alt)))
            {
                IsDropDownOpen = !IsDropDownOpen;
                e.Handled = true;
            }
            else if (IsDropDownOpen && e.Key == Key.Escape)
            {
                IsDropDownOpen = false;
                e.Handled = true;
            }
            else if (IsDropDownOpen && e.Key == Key.Enter)
            {
                SelectFocusedItem();
                IsDropDownOpen = false;
                e.Handled = true;
            }
            else if (!IsDropDownOpen)
            {
                if (e.Key == Key.Down)
                {
                    SelectNext();
                    e.Handled = true;
                }
                else if (e.Key == Key.Up)
                {
                    SelectPrev();
                    e.Handled = true;
                }
            }
            else if (IsDropDownOpen && SelectedIndex < 0 && ItemCount > 0 &&
                      (e.Key == Key.Up || e.Key == Key.Down))
            {
                var firstChild = Presenter?.Panel?.Children.FirstOrDefault(c => CanFocus(c));
                if (firstChild != null)
                {
                    FocusManager.Instance?.Focus(firstChild, NavigationMethod.Directional);
                    e.Handled = true;
                }
            }
        }

        /// <inheritdoc />
        protected override void OnTextInput(TextInputEventArgs e)
        {
            if (!IsTextSearchEnabled || e.Handled)
                return;

            StopTextSearchTimer();

            _textSearchTerm += e.Text;

            bool match(ItemContainerInfo info) => 
                info.ContainerControl is IContentControl control &&
                control.Content?.ToString()?.StartsWith(_textSearchTerm, StringComparison.OrdinalIgnoreCase) == true;

            var info = ItemContainerGenerator.Containers.FirstOrDefault(match);

            if (info != null)
            {
                SelectedIndex = info.Index;
            }

            StartTextSearchTimer();

            e.Handled = true;
        }

        /// <inheritdoc/>
        protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
        {
            base.OnPointerWheelChanged(e);

            if (!e.Handled)
            {
                if (!IsDropDownOpen)
                {
                    if (IsFocused)
                    {
                        if (e.Delta.Y < 0)
                            SelectNext();
                        else
                            SelectPrev();

                        e.Handled = true;
                    }
                }
                else
                {
                    e.Handled = true;
                }
            }
        }

        /// <inheritdoc/>
        protected override void OnPointerReleased(PointerReleasedEventArgs e)
        {
            if (!e.Handled)
            {
                if (_popup?.IsInsidePopup((IVisual)e.Source) == true)
                {
                    if (UpdateSelectionFromEventSource(e.Source))
                    {
                        _popup?.Close();
                        e.Handled = true;
                    }
                }
                else
                {
                    IsDropDownOpen = !IsDropDownOpen;
                    e.Handled = true;
                }
            }

            base.OnPointerReleased(e);
        }

        /// <inheritdoc/>
        protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
        {
            if (_popup != null)
            {
                _popup.Opened -= PopupOpened;
                _popup.Closed -= PopupClosed;
            }

            _popup = e.NameScope.Get<Popup>("PART_Popup");
            _popup.Opened += PopupOpened;
        }

        protected override AutomationPeer OnCreateAutomationPeer(IAutomationNodeFactory factory)
        {
            return new ComboBoxAutomationPeer(factory, this);
        }

        internal void ItemFocused(ComboBoxItem dropDownItem)
        {
            if (IsDropDownOpen && dropDownItem.IsFocused && dropDownItem.IsArrangeValid)
            {
                dropDownItem.BringIntoView();
            }
        }

        private void PopupClosed(object sender, EventArgs e)
        {
            _subscriptionsOnOpen?.Dispose();
            _subscriptionsOnOpen = null;

            if (CanFocus(this))
            {
                Focus();
            }
        }

        private void PopupOpened(object sender, EventArgs e)
        {
            TryFocusSelectedItem();

            _subscriptionsOnOpen?.Dispose();
            _subscriptionsOnOpen = null;

            var toplevel = this.GetVisualRoot() as TopLevel;
            if (toplevel != null)
            {
                _subscriptionsOnOpen = toplevel.AddDisposableHandler(PointerWheelChangedEvent, (s, ev) =>
                {
                    //eat wheel scroll event outside dropdown popup while it's open
                    if (IsDropDownOpen && (ev.Source as IVisual).GetVisualRoot() == toplevel)
                    {
                        ev.Handled = true;
                    }
                }, Interactivity.RoutingStrategies.Tunnel);
            }
        }

        private void SelectedItemChanged(AvaloniaPropertyChangedEventArgs e)
        {
            UpdateSelectionBoxItem(e.NewValue);
            TryFocusSelectedItem();
        }

        private void TryFocusSelectedItem()
        {
            var selectedIndex = SelectedIndex;
            if (IsDropDownOpen && selectedIndex != -1)
            {
                var container = ItemContainerGenerator.ContainerFromIndex(selectedIndex);

                if (container == null && SelectedIndex != -1)
                {
                    ScrollIntoView(Selection.SelectedIndex);
                    container = ItemContainerGenerator.ContainerFromIndex(selectedIndex);
                }

                if (container != null && CanFocus(container))
                {
                    container.Focus();
                }
            }
        }

        private bool CanFocus(IControl control) => control.Focusable && control.IsEffectivelyEnabled && control.IsVisible;

        private void UpdateSelectionBoxItem(object item)
        {
            var contentControl = item as IContentControl;

            if (contentControl != null)
            {
                item = contentControl.Content;
            }

            var control = item as IControl;

            if (control != null)
            {
                if (VisualRoot is object)
                {
                    control.Measure(Size.Infinity);

                    SelectionBoxItem = new Rectangle
                    {
                        Width = control.DesiredSize.Width,
                        Height = control.DesiredSize.Height,
                        Fill = new VisualBrush
                        {
                            Visual = control,
                            Stretch = Stretch.None,
                            AlignmentX = AlignmentX.Left,
                        }
                    };
                }
            }
            else
            {
                SelectionBoxItem = item;
            }
        }

        private void SelectFocusedItem()
        {
            foreach (ItemContainerInfo dropdownItem in ItemContainerGenerator.Containers)
            {
                if (dropdownItem.ContainerControl.IsFocused)
                {
                    SelectedIndex = dropdownItem.Index;
                    break;
                }
            }
        }

        private void SelectNext()
        {
            int next = SelectedIndex + 1;

            if (next >= ItemCount)
                next = 0;

            SelectedIndex = next;
        }

        private void SelectPrev()
        {
            int prev = SelectedIndex - 1;

            if (prev < 0)
                prev = ItemCount - 1;

            SelectedIndex = prev;
        }

        private void StartTextSearchTimer()
        {
            _textSearchTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _textSearchTimer.Tick += TextSearchTimer_Tick;
            _textSearchTimer.Start();
        }

        private void StopTextSearchTimer()
        {
            if (_textSearchTimer == null)
            {
                return;
            }

            _textSearchTimer.Stop();
            _textSearchTimer.Tick -= TextSearchTimer_Tick;

            _textSearchTimer = null;
        }

        private void TextSearchTimer_Tick(object sender, EventArgs e)
        {
            _textSearchTerm = string.Empty;
            StopTextSearchTimer();
        }
    }
}
