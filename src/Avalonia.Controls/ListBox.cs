using System.Collections;
using Avalonia.Controls.Generators;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Selection;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.VisualTree;

namespace Avalonia.Controls
{
    /// <summary>
    /// An <see cref="ItemsControl"/> in which individual items can be selected.
    /// </summary>
    public class ListBox : SelectingItemsControl
    {
        /// <summary>
        /// The default value for the <see cref="ItemsControl.ItemsPanel"/> property.
        /// </summary>
        private static readonly FuncTemplate<IPanel> DefaultPanel =
            new FuncTemplate<IPanel>(() => new VirtualizingStackPanel());

        /// <summary>
        /// Defines the <see cref="Scroll"/> property.
        /// </summary>
        public static readonly DirectProperty<ListBox, IScrollable> ScrollProperty =
            AvaloniaProperty.RegisterDirect<ListBox, IScrollable>(nameof(Scroll), o => o.Scroll);

        /// <summary>
        /// Defines the <see cref="SelectedItems"/> property.
        /// </summary>
        public static readonly new DirectProperty<SelectingItemsControl, IList> SelectedItemsProperty =
            SelectingItemsControl.SelectedItemsProperty;

        /// <summary>
        /// Defines the <see cref="Selection"/> property.
        /// </summary>
        public static readonly new DirectProperty<SelectingItemsControl, ISelectionModel> SelectionProperty =
            SelectingItemsControl.SelectionProperty;

        /// <summary>
        /// Defines the <see cref="SelectionMode"/> property.
        /// </summary>
        public static readonly new StyledProperty<SelectionMode> SelectionModeProperty = 
            SelectingItemsControl.SelectionModeProperty;

        /// <summary>
        /// Defines the <see cref="VirtualizationMode"/> property.
        /// </summary>
        public static readonly StyledProperty<ItemVirtualizationMode> VirtualizationModeProperty =
            ItemsPresenter.VirtualizationModeProperty.AddOwner<ListBox>();

        private IScrollable _scroll;

        /// <summary>
        /// Initializes static members of the <see cref="ItemsControl"/> class.
        /// </summary>
        static ListBox()
        {
            ItemsPanelProperty.OverrideDefaultValue<ListBox>(DefaultPanel);
            VirtualizationModeProperty.OverrideDefaultValue<ListBox>(ItemVirtualizationMode.Simple);
        }

        /// <summary>
        /// Gets the scroll information for the <see cref="ListBox"/>.
        /// </summary>
        public IScrollable Scroll
        {
            get { return _scroll; }
            private set { SetAndRaise(ScrollProperty, ref _scroll, value); }
        }

        /// <inheritdoc/>
        public new IList SelectedItems
        {
            get => base.SelectedItems;
            set => base.SelectedItems = value;
        }

        /// <inheritdoc/>
        public new ISelectionModel Selection
        {
            get => base.Selection;
            set => base.Selection = value;
        }

        /// <summary>
        /// Gets or sets the selection mode.
        /// </summary>
        /// <remarks>
        /// Note that the selection mode only applies to selections made via user interaction.
        /// Multiple selections can be made programmatically regardless of the value of this property.
        /// </remarks>
        public new SelectionMode SelectionMode
        {
            get { return base.SelectionMode; }
            set { base.SelectionMode = value; }
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
        /// Selects all items in the <see cref="ListBox"/>.
        /// </summary>
        public void SelectAll() => Selection.SelectAll();

        /// <summary>
        /// Deselects all items in the <see cref="ListBox"/>.
        /// </summary>
        public void UnselectAll() => Selection.Clear();

        /// <inheritdoc/>
        protected override IItemContainerGenerator CreateItemContainerGenerator()
        {
            return new ItemContainerGenerator<ListBoxItem>(
                this, 
                ListBoxItem.ContentProperty,
                ListBoxItem.ContentTemplateProperty);
        }

        /// <inheritdoc/>
        protected override void OnGotFocus(GotFocusEventArgs e)
        {
            base.OnGotFocus(e);

            if (e.NavigationMethod == NavigationMethod.Directional)
            {
                e.Handled = UpdateSelectionFromEventSource(
                    e.Source,
                    true,
                    e.KeyModifiers.HasAllFlags(KeyModifiers.Shift),
                    e.KeyModifiers.HasAllFlags(KeyModifiers.Control));
            }
        }

        /// <inheritdoc/>
        protected override void OnPointerPressed(PointerPressedEventArgs e)
        {
            base.OnPointerPressed(e);

            if (e.Source is IVisual source)
            {
                var point = e.GetCurrentPoint(source);

                if (point.Properties.IsLeftButtonPressed || point.Properties.IsRightButtonPressed)
                {
                    e.Handled = UpdateSelectionFromEventSource(
                        e.Source,
                        true,
                        e.KeyModifiers.HasAllFlags(KeyModifiers.Shift),
                        e.KeyModifiers.HasAllFlags(KeyModifiers.Control),
                        point.Properties.IsRightButtonPressed);
                }
            }
        }

        protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
        {
            base.OnApplyTemplate(e);
            Scroll = e.NameScope.Find<IScrollable>("PART_ScrollViewer");
        }
    }
}
