using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using Avalonia.Controls.Selection;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;
using Avalonia.VisualTree;

#nullable enable

namespace Avalonia.Controls.Primitives
{
    /// <summary>
    /// An <see cref="ItemsControl"/> that maintains a selection.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="SelectingItemsControl"/> provides a base class for <see cref="ItemsControl"/>s
    /// that maintain a selection (single or multiple). By default only its 
    /// <see cref="SelectedIndex"/> and <see cref="SelectedItem"/> properties are visible; the
    /// current multiple <see cref="Selection"/> and <see cref="SelectedItems"/> together with the
    /// <see cref="SelectionMode"/> properties are protected, however a derived class can expose
    /// these if it wishes to support multiple selection.
    /// </para>
    /// <para>
    /// <see cref="SelectingItemsControl"/> maintains a selection respecting the current 
    /// <see cref="SelectionMode"/> but it does not react to user input; this must be handled in a
    /// derived class. It does, however, respond to <see cref="IsSelectedChangedEvent"/> events
    /// from items and updates the selection accordingly.
    /// </para>
    /// </remarks>
    public class SelectingItemsControl : ItemsControl
    {
        /// <summary>
        /// Defines the <see cref="AutoScrollToSelectedItem"/> property.
        /// </summary>
        public static readonly StyledProperty<bool> AutoScrollToSelectedItemProperty =
            AvaloniaProperty.Register<SelectingItemsControl, bool>(
                nameof(AutoScrollToSelectedItem),
                defaultValue: true);

        /// <summary>
        /// Defines the <see cref="SelectedIndex"/> property.
        /// </summary>
        public static readonly DirectProperty<SelectingItemsControl, int> SelectedIndexProperty =
            AvaloniaProperty.RegisterDirect<SelectingItemsControl, int>(
                nameof(SelectedIndex),
                o => o.SelectedIndex,
                (o, v) => o.SelectedIndex = v,
                unsetValue: -1,
                defaultBindingMode: BindingMode.TwoWay);

        /// <summary>
        /// Defines the <see cref="SelectedItem"/> property.
        /// </summary>
        public static readonly DirectProperty<SelectingItemsControl, object?> SelectedItemProperty =
            AvaloniaProperty.RegisterDirect<SelectingItemsControl, object?>(
                nameof(SelectedItem),
                o => o.SelectedItem,
                (o, v) => o.SelectedItem = v,
                defaultBindingMode: BindingMode.TwoWay);

        /// <summary>
        /// Defines the <see cref="SelectedItems"/> property.
        /// </summary>
        protected static readonly DirectProperty<SelectingItemsControl, IList?> SelectedItemsProperty =
            AvaloniaProperty.RegisterDirect<SelectingItemsControl, IList?>(
                nameof(SelectedItems),
                o => o.SelectedItems,
                (o, v) => o.SelectedItems = v);

        /// <summary>
        /// Defines the <see cref="Selection"/> property.
        /// </summary>
        protected static readonly DirectProperty<SelectingItemsControl, ISelectionModel> SelectionProperty =
            AvaloniaProperty.RegisterDirect<SelectingItemsControl, ISelectionModel>(
                nameof(Selection),
                o => o.Selection,
                (o, v) => o.Selection = v);

        /// <summary>
        /// Defines the <see cref="SelectionMode"/> property.
        /// </summary>
        protected static readonly StyledProperty<SelectionMode> SelectionModeProperty =
            AvaloniaProperty.Register<SelectingItemsControl, SelectionMode>(
                nameof(SelectionMode));

        /// <summary>
        /// Event that should be raised by items that implement <see cref="ISelectable"/> to
        /// notify the parent <see cref="SelectingItemsControl"/> that their selection state
        /// has changed.
        /// </summary>
        public static readonly RoutedEvent<RoutedEventArgs> IsSelectedChangedEvent =
            RoutedEvent.Register<SelectingItemsControl, RoutedEventArgs>(
                "IsSelectedChanged",
                RoutingStrategies.Bubble);

        /// <summary>
        /// Defines the <see cref="SelectionChanged"/> event.
        /// </summary>
        public static readonly RoutedEvent<SelectionChangedEventArgs> SelectionChangedEvent =
            RoutedEvent.Register<SelectingItemsControl, SelectionChangedEventArgs>(
                "SelectionChanged",
                RoutingStrategies.Bubble);

        private static readonly IList Empty = Array.Empty<object>();
        private ISelectionModel? _selection;
        private int _oldSelectedIndex;
        private object? _oldSelectedItem;
        private IList? _oldSelectedItems;
        private bool _ignoreContainerSelectionChanged;
        private UpdateState? _updateState;
        private bool _hasScrolledToSelectedItem;

        /// <summary>
        /// Initializes static members of the <see cref="SelectingItemsControl"/> class.
        /// </summary>
        static SelectingItemsControl()
        {
            IsSelectedChangedEvent.AddClassHandler<SelectingItemsControl>((x, e) => x.ContainerSelectionChanged(e));
        }

        /// <summary>
        /// Occurs when the control's selection changes.
        /// </summary>
        public event EventHandler<SelectionChangedEventArgs> SelectionChanged
        {
            add { AddHandler(SelectionChangedEvent, value); }
            remove { RemoveHandler(SelectionChangedEvent, value); }
        }

        /// <summary>
        /// Gets or sets a value indicating whether to automatically scroll to newly selected items.
        /// </summary>
        public bool AutoScrollToSelectedItem
        {
            get { return GetValue(AutoScrollToSelectedItemProperty); }
            set { SetValue(AutoScrollToSelectedItemProperty, value); }
        }

        /// <summary>
        /// Gets or sets the index of the selected item.
        /// </summary>
        public int SelectedIndex
        {
            get
            {
                // When a Begin/EndInit/DataContext update is in place we return the value to be
                // updated here, even though it's not yet active and the property changed notification
                // has not yet been raised. If we don't do this then the old value will be written back
                // to the source when two-way bound, and the update value will be lost.
                return _updateState?.SelectedIndex.HasValue == true ?
                    _updateState.SelectedIndex.Value :
                    Selection.SelectedIndex;
            }
            set
            {
                if (_updateState is object)
                {
                    _updateState.SelectedIndex = value;
                }
                else
                {
                    Selection.SelectedIndex = value;
                }
            }
        }

        /// <summary>
        /// Gets or sets the selected item.
        /// </summary>
        public object? SelectedItem
        {
            get
            {
                // See SelectedIndex setter for more information.
                return _updateState?.SelectedItem.HasValue == true ?
                    _updateState.SelectedItem.Value :
                    Selection.SelectedItem;
            }
            set
            {
                if (_updateState is object)
                {
                    _updateState.SelectedItem = value;
                }
                else
                {
                    Selection.SelectedItem = value;
                }
            }
        }

        /// <summary>
        /// Gets or sets the selected items.
        /// </summary>
        /// <remarks>
        /// By default returns a collection that can be modified in order to manipulate the control
        /// selection, however this property will return null if <see cref="Selection"/> is
        /// re-assigned; you should only use _either_ Selection or SelectedItems.
        /// </remarks>
        protected IList? SelectedItems
        {
            get
            {
                // See SelectedIndex setter for more information.
                if (_updateState?.SelectedItems.HasValue == true)
                {
                    return _updateState.SelectedItems.Value;
                }
                else if (Selection is InternalSelectionModel ism)
                {
                    var result = ism.WritableSelectedItems;
                    _oldSelectedItems = result;
                    return result;
                }

                return null;
            }
            set
            {
                if (_updateState is object)
                {
                    _updateState.SelectedItems = new Optional<IList?>(value);
                }
                else if (Selection is InternalSelectionModel i)
                {
                    i.WritableSelectedItems = value;
                }
                else
                {
                    throw new InvalidOperationException("Cannot set both Selection and SelectedItems.");
                }
            }
        }

        /// <summary>
        /// Gets or sets the model that holds the current selection.
        /// </summary>
        protected ISelectionModel Selection
        {
            get
            {
                if (_updateState?.Selection.HasValue == true)
                {
                    return _updateState.Selection.Value;
                }
                else
                {
                    if (_selection is null)
                    {
                        _selection = CreateDefaultSelectionModel();
                        InitializeSelectionModel(_selection);
                    }

                    return _selection;
                }
            }
            set
            {
                value ??= CreateDefaultSelectionModel();

                if (_updateState is object)
                {
                    _updateState.Selection = new Optional<ISelectionModel>(value);
                }
                else if (_selection != value)
                {
                    if (value.Source != null && value.Source != Items)
                    {
                        throw new ArgumentException(
                            "The supplied ISelectionModel already has an assigned Source but this " +
                            "collection is different to the Items on the control.");
                    }

                    var oldSelection = _selection?.SelectedItems.ToList();
                    DeinitializeSelectionModel(_selection);
                    _selection = value;

                    if (oldSelection?.Count > 0)
                    {
                        RaiseEvent(new SelectionChangedEventArgs(
                            SelectionChangedEvent,
                            oldSelection,
                            Array.Empty<object>()));
                    }

                    InitializeSelectionModel(_selection);

                    if (_oldSelectedItems != SelectedItems)
                    {
                        RaisePropertyChanged(
                            SelectedItemsProperty,
                            new Optional<IList?>(_oldSelectedItems),
                            new BindingValue<IList?>(SelectedItems));
                        _oldSelectedItems = SelectedItems;
                    }
                }
            }
        }

        /// <summary>
        /// Gets or sets the selection mode.
        /// </summary>
        /// <remarks>
        /// Note that the selection mode only applies to selections made via user interaction.
        /// Multiple selections can be made programatically regardless of the value of this property.
        /// </remarks>
        protected SelectionMode SelectionMode
        {
            get { return GetValue(SelectionModeProperty); }
            set { SetValue(SelectionModeProperty, value); }
        }

        /// <summary>
        /// Gets a value indicating whether <see cref="SelectionMode.AlwaysSelected"/> is set.
        /// </summary>
        protected bool AlwaysSelected => (SelectionMode & SelectionMode.AlwaysSelected) != 0;

        /// <inheritdoc/>
        public override void BeginInit()
        {
            base.BeginInit();
            BeginUpdating();
        }

        /// <inheritdoc/>
        public override void EndInit()
        {
            base.EndInit();
            EndUpdating();
        }

        /// <summary>
        /// Scrolls the specified item into view.
        /// </summary>
        /// <param name="index">The index of the item.</param>
        public void ScrollIntoView(int index) => Presenter?.ScrollIntoView(index);

        /// <summary>
        /// Scrolls the specified item into view.
        /// </summary>
        /// <param name="item">The item.</param>
        public void ScrollIntoView(object item) => ScrollIntoView(ItemsView.IndexOf(item));

        /// <summary>
        /// Tries to get the container that was the source of an event.
        /// </summary>
        /// <param name="eventSource">The control that raised the event.</param>
        /// <returns>The container or null if the event did not originate in a container.</returns>
        protected IControl? GetContainerFromEventSource(IInteractive? eventSource)
        {
            for (var current = eventSource as IVisual; current != null; current = current.VisualParent)
            {
                if (current is IControl control &&
                    control.LogicalParent == this &&
                    GetContainerIndex(control) != -1)
                {
                    return control;
                }
            }

            return null;
        }

        /// <summary>
        /// Tries to get the index of the container that was the source of an event.
        /// </summary>
        /// <param name="eventSource">The control that raised the event.</param>
        /// <returns>The container index or -1 if the event did not originate in a container.</returns>
        protected int GetContainerIndexFromEventSource(IInteractive eventSource)
        {
            var parent = (IVisual)eventSource;

            while (parent != null)
            {
                if (parent is IControl control && control.LogicalParent == this)
                {
                    var index = GetContainerIndex(control);

                    if (index != -1)
                    {
                        return index;
                    }
                }

                parent = parent.VisualParent;
            }

            return -1;
        }

        protected override void ItemsViewChanged(ItemsSourceView? oldView, ItemsSourceView newView)
        {
            base.ItemsViewChanged(oldView, newView);

            AddSelectedContainersToSelection(0, ItemsView);

            if (AlwaysSelected && SelectedIndex == -1 && ItemCount > 0)
            {
                SelectedIndex = 0;
            }
        }

        protected override void ItemsViewCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            base.ItemsViewCollectionChanged(e);

            if (e.Action == NotifyCollectionChangedAction.Add ||
                     e.Action == NotifyCollectionChangedAction.Replace)
            {
                AddSelectedContainersToSelection(e.NewStartingIndex, e.NewItems);
            }

            if (AlwaysSelected && SelectedIndex == -1 && ItemCount > 0)
            {
                SelectedIndex = 0;
            }
        }

        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);
            AutoScrollToSelectedItemOnLayoutUpdated();
        }

        protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
        {
            base.OnApplyTemplate(e);
            AutoScrollToSelectedItemOnLayoutUpdated();
        }

        /// <inheritdoc/>
        protected override void OnContainerPrepared(ElementPreparedEventArgs e)
        {
            base.OnContainerPrepared(e);

            MarkContainerSelected(e.Element, _selection?.IsSelected(e.Index) ?? false);

            if (_selection?.AnchorIndex == e.Index)
            {
                KeyboardNavigation.SetTabOnceActiveElement(
                    (InputElement)Presenter!,
                    e.Element);
            }
        }

        protected override void OnContainerClearing(ElementClearingEventArgs e)
        {
            base.OnContainerClearing(e);
            MarkContainerSelected(e.Element, false);
        }

        protected override void OnContainerIndexChanged(ElementIndexChangedEventArgs e)
        {
            base.OnContainerIndexChanged(e);
            MarkContainerSelected(e.Element, _selection?.IsSelected(e.NewIndex) ?? false);
        }

        /// <inheritdoc/>
        protected override void OnDataContextBeginUpdate()
        {
            base.OnDataContextBeginUpdate();
            BeginUpdating();
        }

        /// <inheritdoc/>
        protected override void OnDataContextEndUpdate()
        {
            base.OnDataContextEndUpdate();
            EndUpdating();
        }

        protected override void OnInitialized()
        {
            base.OnInitialized();

            if (_selection is object)
            {
                _selection.Source = Items;
            }
        }

        protected override void OnPropertyChanged<T>(AvaloniaPropertyChangedEventArgs<T> change)
        {
            base.OnPropertyChanged(change);

            if (change.Property == AutoScrollToSelectedItemProperty)
            {
                AutoScrollToSelectedItemIfNecessary();
            }
            if (change.Property == ItemsProperty && _updateState is null && _selection is object)
            {
                var newValue = change.NewValue.GetValueOrDefault<IEnumerable>();
                _selection.Source = newValue;

                if (newValue is null)
                {
                    _selection.Clear();
                }
            }
            else if (change.Property == SelectionModeProperty && _selection is object)
            {
                var newValue = change.NewValue.GetValueOrDefault<SelectionMode>();
                _selection.SingleSelect = !newValue.HasFlagCustom(SelectionMode.Multiple);
            }
        }

        /// <summary>
        /// Moves the selection in the specified direction relative to the current selection.
        /// </summary>
        /// <param name="direction">The direction to move.</param>
        /// <param name="rangeModifier">Whether the range modifier is enabled (i.e. shift key).</param>
        /// <returns>True if the selection was moved; otherwise false.</returns>
        protected bool MoveSelection(
            NavigationDirection direction,
            bool rangeModifier)
        {
            if (direction == NavigationDirection.First || direction == NavigationDirection.Last)
            {
                var index = direction == NavigationDirection.First ? 0 : ItemCount - 1;
                UpdateSelection(index, true, rangeModifier);
                ScrollIntoView(index);

                var c = TryGetContainer(index);

                if (c is object)
                {
                    FocusManager.Instance?.Focus(c, direction.ToNavigationMethod());
                }
            }
            else
            {
                var focus = FocusManager.Instance;
                var next = focus?.FindNextElement(direction);

                if (next is IControl c && GetContainerIndex(c) != -1)
                {
                    UpdateSelection(c, true, rangeModifier);
                    focus?.Focus(c, direction.ToNavigationMethod());
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Updates the selection for an item based on user interaction.
        /// </summary>
        /// <param name="index">The index of the item.</param>
        /// <param name="select">Whether the item should be selected or unselected.</param>
        /// <param name="rangeModifier">Whether the range modifier is enabled (i.e. shift key).</param>
        /// <param name="toggleModifier">Whether the toggle modifier is enabled (i.e. ctrl key).</param>
        /// <param name="rightButton">Whether the event is a right-click.</param>
        protected void UpdateSelection(
            int index,
            bool select = true,
            bool rangeModifier = false,
            bool toggleModifier = false,
            bool rightButton = false)
        {
            if (index < 0 || index >= ItemCount)
            {
                return;
            }

            var mode = SelectionMode;
            var multi = (mode & SelectionMode.Multiple) != 0;
            var toggle = (toggleModifier || (mode & SelectionMode.Toggle) != 0);
            var range = multi && rangeModifier;

            if (!select)
            {
                Selection.Deselect(index);
            }
            else if (rightButton)
            {
                if (Selection.IsSelected(index) == false)
                {
                    SelectedIndex = index;
                }
            }
            else if (range)
            {
                using var operation = Selection.BatchUpdate();
                Selection.Clear();
                Selection.SelectRange(Selection.AnchorIndex, index);
            }
            else if (multi && toggle)
            {
                if (Selection.IsSelected(index) == true)
                {
                    Selection.Deselect(index);
                }
                else
                {
                    Selection.Select(index);
                }
            }
            else if (toggle)
            {
                SelectedIndex = (SelectedIndex == index) ? -1 : index;
            }
            else
            {
                using var operation = Selection.BatchUpdate();
                Selection.Clear();
                Selection.Select(index);
            }
        }

        /// <summary>
        /// Updates the selection for a container based on user interaction.
        /// </summary>
        /// <param name="container">The container.</param>
        /// <param name="select">Whether the container should be selected or unselected.</param>
        /// <param name="rangeModifier">Whether the range modifier is enabled (i.e. shift key).</param>
        /// <param name="toggleModifier">Whether the toggle modifier is enabled (i.e. ctrl key).</param>
        /// <param name="rightButton">Whether the event is a right-click.</param>
        protected void UpdateSelection(
            IControl container,
            bool select = true,
            bool rangeModifier = false,
            bool toggleModifier = false,
            bool rightButton = false)
        {
            var index = GetContainerIndex(container);

            if (index != -1)
            {
                UpdateSelection(index, select, rangeModifier, toggleModifier, rightButton);
            }
        }

        /// <summary>
        /// Updates the selection based on an event that may have originated in a container that
        /// belongs to the control.
        /// </summary>
        /// <param name="eventSource">The control that raised the event.</param>
        /// <param name="select">Whether the container should be selected or unselected.</param>
        /// <param name="rangeModifier">Whether the range modifier is enabled (i.e. shift key).</param>
        /// <param name="toggleModifier">Whether the toggle modifier is enabled (i.e. ctrl key).</param>
        /// <param name="rightButton">Whether the event is a right-click.</param>
        /// <returns>
        /// True if the event originated from a container that belongs to the control; otherwise
        /// false.
        /// </returns>
        protected bool UpdateSelectionFromEventSource(
            IInteractive? eventSource,
            bool select = true,
            bool rangeModifier = false,
            bool toggleModifier = false,
            bool rightButton = false)
        {
            var container = GetContainerFromEventSource(eventSource);

            if (container != null)
            {
                UpdateSelection(container, select, rangeModifier, toggleModifier, rightButton);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Called when <see cref="INotifyPropertyChanged.PropertyChanged"/> is raised on
        /// <see cref="Selection"/>.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The event args.</param>
        private void OnSelectionModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ISelectionModel.AnchorIndex))
            {
                _hasScrolledToSelectedItem = false;
                AutoScrollToSelectedItemIfNecessary();

                if (Presenter is object)
                {
                    var container = TryGetContainer(Selection.AnchorIndex);
                    KeyboardNavigation.SetTabOnceActiveElement(
                        (InputElement)Presenter,
                        container);
                }
            }
            else if (e.PropertyName == nameof(ISelectionModel.SelectedIndex) && _oldSelectedIndex != SelectedIndex)
            {
                RaisePropertyChanged(SelectedIndexProperty, _oldSelectedIndex, SelectedIndex);
                _oldSelectedIndex = SelectedIndex;
            }
            else if (e.PropertyName == nameof(ISelectionModel.SelectedItem) && _oldSelectedItem != SelectedItem)
            {
                RaisePropertyChanged(SelectedItemProperty, _oldSelectedItem, SelectedItem);
                _oldSelectedItem = SelectedItem;
            }
            else if (e.PropertyName == nameof(InternalSelectionModel.WritableSelectedItems) &&
                _oldSelectedItems != (Selection as InternalSelectionModel)?.SelectedItems)
            {
                RaisePropertyChanged(
                    SelectedItemsProperty,
                    new Optional<IList?>(_oldSelectedItems),
                    new BindingValue<IList?>(SelectedItems));
                _oldSelectedItems = SelectedItems;
            }
        }

        /// <summary>
        /// Called when <see cref="ISelectionModel.SelectionChanged"/> event is raised on
        /// <see cref="Selection"/>.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The event args.</param>
        private void OnSelectionModelSelectionChanged(object sender, SelectionModelSelectionChangedEventArgs e)
        {
            void Mark(int index, bool selected)
            {
                var container = TryGetContainer(index);

                if (container != null)
                {
                    MarkContainerSelected(container, selected);
                }
            }

            foreach (var i in e.SelectedIndexes)
            {
                Mark(i, true);
            }

            foreach (var i in e.DeselectedIndexes)
            {
                Mark(i, false);
            }

            var route = BuildEventRoute(SelectionChangedEvent);

            if (route.HasHandlers)
            {
                var ev = new SelectionChangedEventArgs(
                    SelectionChangedEvent,
                    e.DeselectedItems.ToList(),
                    e.SelectedItems.ToList());
                RaiseEvent(ev);
            }
        }

        /// <summary>
        /// Called when <see cref="ISelectionModel.LostSelection"/> event is raised on
        /// <see cref="Selection"/>.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The event args.</param>
        private void OnSelectionModelLostSelection(object sender, EventArgs e)
        {
            if (AlwaysSelected && Items is object)
            {
                SelectedIndex = 0;
            }
        }

        private void AutoScrollToSelectedItemIfNecessary()
        {
            if (AutoScrollToSelectedItem &&
                !_hasScrolledToSelectedItem &&
                Presenter is object &&
                Selection.AnchorIndex >= 0 &&
                ((IVisual)this).IsAttachedToVisualTree)
            {
                ScrollIntoView(Selection.AnchorIndex);
                _hasScrolledToSelectedItem = true;
            }
        }

        private void AutoScrollToSelectedItemOnLayoutUpdated()
        {
            void ExecuteScrollWhenLayoutUpdated(object sender, EventArgs e)
            {
                LayoutUpdated -= ExecuteScrollWhenLayoutUpdated;
                AutoScrollToSelectedItemIfNecessary();
            }

            if (AutoScrollToSelectedItem)
            {
                LayoutUpdated += ExecuteScrollWhenLayoutUpdated;
            }
        }

        /// <summary>
        /// Called when a container raises the <see cref="IsSelectedChangedEvent"/>.
        /// </summary>
        /// <param name="e">The event.</param>
        private void ContainerSelectionChanged(RoutedEventArgs e)
        {
            if (!_ignoreContainerSelectionChanged &&
                e.Source is IControl control &&
                e.Source is ISelectable selectable &&
                control.LogicalParent == this &&
                GetContainerIndex(control) != -1)
            {
                UpdateSelection(control, selectable.IsSelected);
            }

            if (e.Source != this)
            {
                e.Handled = true;
            }
        }

        /// <summary>
        /// Sets a container's 'selected' class or <see cref="ISelectable.IsSelected"/>.
        /// </summary>
        /// <param name="container">The container.</param>
        /// <param name="selected">Whether the control is selected</param>
        /// <returns>The previous selection state.</returns>
        private bool MarkContainerSelected(IControl container, bool selected)
        {
            try
            {
                bool result;

                _ignoreContainerSelectionChanged = true;

                if (container is ISelectable selectable)
                {
                    result = selectable.IsSelected;
                    selectable.IsSelected = selected;
                }
                else
                {
                    result = container.Classes.Contains(":selected");
                    ((IPseudoClasses)container.Classes).Set(":selected", selected);
                }

                return result;
            }
            finally
            {
                _ignoreContainerSelectionChanged = false;
            }
        }

        private void UpdateContainerSelection()
        {
            if (Presenter is IPanel panel)
            {
                foreach (var container in panel.Children)
                {
                    MarkContainerSelected(
                        container,
                        Selection.IsSelected(GetContainerIndex(container)) != false);
                }
            }
        }

        private ISelectionModel CreateDefaultSelectionModel()
        {
            return new InternalSelectionModel
            {
                SingleSelect = !SelectionMode.HasFlagCustom(SelectionMode.Multiple),
            };
        }

        private void InitializeSelectionModel(ISelectionModel model)
        {
            if (_updateState is null)
            {
                model.Source = Items;
            }

            model.PropertyChanged += OnSelectionModelPropertyChanged;
            model.SelectionChanged += OnSelectionModelSelectionChanged;
            model.LostSelection += OnSelectionModelLostSelection;

            if (model.SingleSelect)
            {
                SelectionMode &= ~SelectionMode.Multiple;
            }
            else
            {
                SelectionMode |= SelectionMode.Multiple;
            }

            _oldSelectedIndex = model.SelectedIndex;
            _oldSelectedItem = model.SelectedItem;

            if (AlwaysSelected && model.Count == 0)
            {
                model.SelectedIndex = 0;
            }

            UpdateContainerSelection();

            if (SelectedIndex != -1)
            {
                RaiseEvent(new SelectionChangedEventArgs(
                    SelectionChangedEvent,
                    Array.Empty<object>(),
                    Selection.SelectedItems.ToList()));
            }
        }

        private void DeinitializeSelectionModel(ISelectionModel? model)
        {
            if (model is object)
            {
                model.PropertyChanged -= OnSelectionModelPropertyChanged;
                model.SelectionChanged -= OnSelectionModelSelectionChanged;
            }
        }

        private void AddSelectedContainersToSelection(int index, IEnumerable items)
        {
            foreach (var i in items)
            {
                if ((i as ISelectable)?.IsSelected == true)
                {
                    Selection.Select(index);
                }

                ++index;
            }
        }

        private void BeginUpdating()
        {
            _updateState ??= new UpdateState();
            _updateState.UpdateCount++;
        }

        private void EndUpdating()
        {
            if (_updateState is object && --_updateState.UpdateCount == 0)
            {
                var state = _updateState;
                _updateState = null;

                if (state.Selection.HasValue)
                {
                    Selection = state.Selection.Value;
                }

                if (state.SelectedItems.HasValue)
                {
                    SelectedItems = state.SelectedItems.Value;
                }

                Selection.Source = Items;

                if (Items is null)
                {
                    Selection.Clear();
                }

                if (state.SelectedIndex.HasValue)
                {
                    SelectedIndex = state.SelectedIndex.Value;
                }
                else if (state.SelectedItem.HasValue)
                {
                    SelectedItem = state.SelectedItem.Value;
                }
            }
        }

        // When in a BeginInit..EndInit block, or when the DataContext is updating, we need to
        // defer changes to the selection model because we have no idea in which order properties
        // will be set. Consider:
        //
        // - Both Items and SelectedItem are bound
        // - The DataContext changes
        // - The binding for SelectedItem updates first, producing an item
        // - Items is searched to find the index of the new selected item
        // - However Items isn't yet updated; the item is not found
        // - SelectedIndex is incorrectly set to -1
        //
        // This logic cannot be encapsulated in SelectionModel because the selection model can also
        // be bound, consider:
        //
        // - Both Items and Selection are bound
        // - The DataContext changes
        // - The binding for Items updates first
        // - The new items are assigned to Selection.Source
        // - The binding for Selection updates, producing a new SelectionModel
        // - Both the old and new SelectionModels have the incorrect Source
        private class UpdateState
        {
            private Optional<int> _selectedIndex;
            private Optional<object?> _selectedItem;

            public int UpdateCount { get; set; }
            public Optional<ISelectionModel> Selection { get; set; }
            public Optional<IList?> SelectedItems { get; set; }

            public Optional<int> SelectedIndex 
            {
                get => _selectedIndex;
                set
                {
                    _selectedIndex = value;
                    _selectedItem = default;
                }
            }

            public Optional<object?> SelectedItem
            {
                get => _selectedItem;
                set
                {
                    _selectedItem = value;
                    _selectedIndex = default;
                }
            }
       }
    }
}
