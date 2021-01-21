using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using Avalonia.Collections;
using Avalonia.Controls.Generators;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Controls.Utils;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.LogicalTree;
using Avalonia.Metadata;

#nullable enable

namespace Avalonia.Controls
{
    /// <summary>
    /// Displays a collection of items.
    /// </summary>
    [PseudoClasses(":empty", ":singleitem")]
    public class ItemsControl : TemplatedControl, IItemsPresenterHost, ICollectionChangedListener
    {
        /// <summary>
        /// Defines the <see cref="Items"/> property.
        /// </summary>
        public static readonly DirectProperty<ItemsControl, IEnumerable?> ItemsProperty =
            AvaloniaProperty.RegisterDirect<ItemsControl, IEnumerable?>(nameof(Items), o => o.Items, (o, v) => o.Items = v);

        /// <summary>
        /// Defines the <see cref="ItemCount"/> property.
        /// </summary>
        public static readonly DirectProperty<ItemsControl, int> ItemCountProperty =
            AvaloniaProperty.RegisterDirect<ItemsControl, int>(nameof(ItemCount), o => o.ItemCount);

        /// <summary>
        /// Defines the <see cref="ItemTemplate"/> property.
        /// </summary>
        public static readonly StyledProperty<IDataTemplate?> ItemTemplateProperty =
            AvaloniaProperty.Register<ItemsControl, IDataTemplate?>(nameof(ItemTemplate));

        /// <summary>
        /// Defines the <see cref="ItemsView"/> property.
        /// </summary>
        public static readonly DirectProperty<ItemsControl, ItemsSourceView> ItemsViewProperty =
            AvaloniaProperty.RegisterDirect<ItemsControl, ItemsSourceView>(nameof(ItemsView), o => o.ItemsView);

        /// <summary>
        /// Defines the <see cref="Layout"/> property.
        /// </summary>
        public static readonly StyledProperty<AttachedLayout> LayoutProperty =
            ItemsRepeater.LayoutProperty.AddOwner<ItemsControl>();

        private bool _itemsInitialized;
        private IItemContainerGenerator? _itemContainerGenerator;
        private IEnumerable? _items;
        private ItemsSourceView? _itemsView;
        private int _itemCount;

        /// <summary>
        /// Initializes a new instance of the <see cref="ItemsControl"/> class.
        /// </summary>
        public ItemsControl()
        {
            UpdatePseudoClasses();
        }

        static ItemsControl()
        {
            LayoutProperty.OverrideDefaultValue<ItemsControl>(new NonVirtualizingStackLayout());
        }

        /// <summary>
        /// Gets the <see cref="IItemContainerGenerator"/> for the control.
        /// </summary>
        public IItemContainerGenerator ItemContainerGenerator
        {
            get => _itemContainerGenerator ??= CreateItemContainerGenerator();
        }

        /// <summary>
        /// Gets or sets the items to display.
        /// </summary>
        [Content]
        public IEnumerable? Items
        {
            get
            {
                if (!_itemsInitialized)
                {
                    _items = new AvaloniaList<object>();
                    _itemsInitialized = true;
                    CreateItemsView();
                }

                return _items;
            }
            set
            {
                if (_items != value)
                {
                    var oldItems = _items;
                    var oldItemsView = _itemsView;

                    _items = value;
                    _itemsInitialized = true;
                    CreateItemsView();

                    RaisePropertyChanged(ItemsProperty,
                        new Optional<IEnumerable?>(oldItems),
                        new BindingValue<IEnumerable?>(_items));
                    RaisePropertyChanged(ItemsViewProperty, oldItemsView, _itemsView);
                }
            }
        }

        /// <summary>
        /// Gets the number of items in <see cref="Items"/>.
        /// </summary>
        public int ItemCount
        {
            get => _itemsView?.Count ?? 0;
            private set => SetAndRaise(ItemCountProperty, ref _itemCount, value);
        }

        /// <summary>
        /// Gets an <see cref="ItemsSourceView"/> over <see cref="Items"/>.
        /// </summary>
        public ItemsSourceView ItemsView
        {
            get
            {
                if (!_itemsInitialized)
                {
                    _items = new AvaloniaList<object>();
                    _itemsInitialized = true;
                    CreateItemsView();
                }

                return _itemsView!;
            }
        }

        /// <summary>
        /// Gets or sets the data template used to display the items in the control.
        /// </summary>
        public IDataTemplate? ItemTemplate
        {
            get => GetValue(ItemTemplateProperty);
            set => SetValue(ItemTemplateProperty, value);
        }

        /// <summary>
        /// Gets or sets the layout used to size and position elements in the <see cref="ItemsControl"/>.
        /// </summary>
        /// <value>
        /// The layout used to size and position elements. The default is a <see cref="StackLayout"/> with
        /// vertical orientation.
        /// </value>
        public AttachedLayout Layout
        {
            get => GetValue(LayoutProperty);
            set => SetValue(LayoutProperty, value);
        }

        /// <summary>
        /// Gets the items presenter control.
        /// </summary>
        public IItemsPresenter? Presenter { get; protected set; }

        void ICollectionChangedListener.PreChanged(INotifyCollectionChanged sender, NotifyCollectionChangedEventArgs e)
        {
        }

        void ICollectionChangedListener.Changed(INotifyCollectionChanged sender, NotifyCollectionChangedEventArgs e)
        {
        }

        void ICollectionChangedListener.PostChanged(INotifyCollectionChanged sender, NotifyCollectionChangedEventArgs e)
        {
            ItemsViewCollectionChanged(e);
        }

        /// <summary>
        /// Occurs each time a container is cleared and made available to be re-used.
        /// </summary>
        /// <remarks>
        /// This event is raised immediately each time a a container is cleared, such as when it
        /// falls outside the range of realized items. Elements are cleared when they become
        /// available for re-use.
        /// </remarks>
        public event EventHandler<ElementClearingEventArgs>? ContainerClearing;

        /// <summary>
        /// Occurs each time a container is prepared for use.
        /// </summary>
        /// <remarks>
        /// The prepared container might be newly created or an existing element that is being re-
        /// used.
        /// </remarks>
        public event EventHandler<ElementPreparedEventArgs>? ContainerPrepared;

        /// <summary>
        /// Occurs for each realized container when the index for the item it represents has changed.
        /// </summary>
        /// <remarks>
        /// This event is raised for each realized container where the index for the item it
        /// represents has changed. For example, when another item is added or removed in the data
        /// source, the index for items that come after in the ordering will be impacted.
        /// </remarks>
        public event EventHandler<ElementIndexChangedEventArgs>? ContainerIndexChanged;

        /// <summary>
        /// Gets the container control for the specified index in <see cref="Items"/>, if realized.
        /// </summary>
        /// <param name="index">The item index.</param>
        /// <returns>The container control, or null if the item is not realized.</returns>
        public IControl? TryGetContainer(int index) => Presenter?.TryGetElement(index);

        /// <summary>
        /// Gets the index in <see cref="Items"/> of the specified container control.
        /// </summary>
        /// <param name="container">The container control.</param>
        /// <returns>The index of the container control, or -1 if the control is not a container.</returns>
        public int GetContainerIndex(IControl container)
        {
            container = container ?? throw new ArgumentNullException(nameof(container));

            return Presenter?.GetElementIndex(container) ?? -1;
        }

        /// <inheritdoc/>
        IElementFactory IItemsPresenterHost.ElementFactory => ItemContainerGenerator;

        /// <inheritdoc/>
        void IItemsPresenterHost.RegisterItemsPresenter(IItemsPresenter presenter)
        {
            var oldValue = Presenter;
            Presenter = presenter;
            ItemsPresenterChanged(oldValue, presenter);
        }

        /// <summary>
        /// Creates the <see cref="ItemContainerGenerator"/> for the control.
        /// </summary>
        protected virtual IItemContainerGenerator CreateItemContainerGenerator() => new ItemContainerGenerator(this);

        /// <summary>
        /// Raises the <see cref="ContainerPrepared"/> event.
        /// </summary>
        /// <param name="e">The event args.</param>
        /// <remarks>
        /// If you override this method, be sure to call the base method implementation.
        /// </remarks>
        protected virtual void OnContainerPrepared(ElementPreparedEventArgs e) => ContainerPrepared?.Invoke(this, e);

        /// <summary>
        /// Raises the <see cref="ContainerClearing"/> event.
        /// </summary>
        /// <param name="e">The event args.</param>
        /// <remarks>
        /// If you override this method, be sure to call the base method implementation.
        /// </remarks>
        protected virtual void OnContainerClearing(ElementClearingEventArgs e) => ContainerClearing?.Invoke(this, e);

        /// <summary>
        /// Raises the <see cref="ContainerIndexChanged"/> event.
        /// </summary>
        /// <param name="e">The event args.</param>
        /// <remarks>
        /// If you override this method, be sure to call the base method implementation.
        /// </remarks>
        protected virtual void OnContainerIndexChanged(ElementIndexChangedEventArgs e) => ContainerIndexChanged?.Invoke(this, e);

        /// <summary>
        /// Handles directional navigation within the <see cref="ItemsControl"/>.
        /// </summary>
        /// <param name="e">The key events.</param>
        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (!e.Handled)
            {
                var focus = FocusManager.Instance;
                var direction = e.Key.ToNavigationDirection();

                if (direction.HasValue)
                {
                    var next = focus.FindNextElement(direction.Value);

                    if (next is IControl c && GetContainerIndex(c) != -1)
                    {
                        focus.Focus(next, NavigationMethod.Directional);
                        e.Handled = true;
                    }
                }
            }

            base.OnKeyDown(e);
        }

        /// <summary>
        /// Called when the <see cref="ItemsView"/> changes.
        /// </summary>
        /// <param name="oldView">
        /// The old items view. Will be null on first invocation and non-null thereafter.
        /// </param>
        /// <param name="newView">The new items view.</param>
        protected virtual void ItemsViewChanged(ItemsSourceView? oldView, ItemsSourceView newView)
        {
            if (oldView is object)
                RemoveControlItemsFromLogicalChildren(oldView);
            AddControlItemsToLogicalChildren(newView);
            UpdatePseudoClasses();
            ItemCount = ItemsView.Count;
        }

        /// <summary>
        /// Called when the <see cref="INotifyCollectionChanged.CollectionChanged"/> event is
        /// raised on <see cref="ItemsView"/>.
        /// </summary>
        /// <param name="e">The event args.</param>
        protected virtual void ItemsViewCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    AddControlItemsToLogicalChildren(e.NewItems);
                    break;

                case NotifyCollectionChangedAction.Remove:
                    RemoveControlItemsFromLogicalChildren(e.OldItems);
                    break;

                case NotifyCollectionChangedAction.Reset:
                    LogicalChildren.Clear();
                    AddControlItemsToLogicalChildren(ItemsView);
                    break;
            }

            UpdatePseudoClasses();
            ItemCount = ItemsView.Count;
        }

        protected virtual void ItemsPresenterChanged(IItemsPresenter? oldValue, IItemsPresenter? newValue)
        {
            void Prepared(object sender, ElementPreparedEventArgs e) => OnContainerPrepared(e);
            void Clearing(object sender, ElementClearingEventArgs e) => OnContainerClearing(e);
            void IndexChanged(object sender, ElementIndexChangedEventArgs e) => OnContainerIndexChanged(e);

            if (oldValue != null)
            {
                oldValue.LogicalChildren.CollectionChanged -= PresenterLogicalChildrenChanged;
                oldValue.ElementPrepared -= Prepared;
                oldValue.ElementClearing -= Clearing;
                oldValue.ElementIndexChanged -= IndexChanged;
                LogicalChildren.RemoveAll(oldValue.LogicalChildren);
            }

            if (newValue != null)
            {
                newValue.LogicalChildren.CollectionChanged += PresenterLogicalChildrenChanged;
                newValue.ElementPrepared += Prepared;
                newValue.ElementClearing += Clearing;
                newValue.ElementIndexChanged += IndexChanged;
                LogicalChildren.AddRange(newValue.LogicalChildren);
            }
        }

        private void CreateItemsView()
        {
            var oldView = _itemsView;

            if (_itemsView is object && _itemsView != ItemsSourceView.Empty)
            {
                _itemsView.RemoveListener(this);
                _itemsView?.Dispose();
            }

            _itemsView = ItemsSourceView.GetOrCreateOrEmpty(_items)!;
            _itemsView.AddListener(this);

            ItemsViewChanged(oldView, _itemsView);
            RaisePropertyChanged(ItemsViewProperty, oldView, _itemsView);
        }

        /// <summary>
        /// Given a collection of items, adds those that are controls to the logical children.
        /// </summary>
        /// <param name="items">The items.</param>
        private void AddControlItemsToLogicalChildren(IEnumerable items)
        {
            var toAdd = new List<ILogical>();

            foreach (var i in items)
            {
                if (i is IControl control && !LogicalChildren.Contains(control))
                {
                    toAdd.Add(control);
                }
            }

            LogicalChildren.AddRange(toAdd);
        }

        /// <summary>
        /// Given a collection of items, removes those that are controls to from logical children.
        /// </summary>
        /// <param name="items">The items.</param>
        private void RemoveControlItemsFromLogicalChildren(IEnumerable items)
        {
            var toRemove = new List<ILogical>();

            foreach (var i in items)
            {
                if (i is IControl control)
                {
                    toRemove.Add(control);
                }
            }

            LogicalChildren.RemoveAll(toRemove);
        }

        private void PresenterLogicalChildrenChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            void Add(IList items)
            {
                foreach (ILogical l in items)
                {
                    if (!LogicalChildren.Contains(l))
                    {
                        LogicalChildren.Add(l);
                    }
                }
            }

            void Remove(IList items)
            {
                foreach (ILogical l in items)
                {
                    LogicalChildren.Remove(l);
                }
            }

            if (Presenter is null)
            {
                throw new AvaloniaInternalException(
                    "Received presenter logical children changed notification, but Presenter is null.");
            }

            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    Add(e.NewItems);
                    break;
                case NotifyCollectionChangedAction.Remove:
                    Remove(e.OldItems);
                    break;
                case NotifyCollectionChangedAction.Replace:
                    Remove(e.OldItems);
                    Add(e.NewItems);
                    break;
                case NotifyCollectionChangedAction.Reset:
                    LogicalChildren.Clear();
                    LogicalChildren.AddRange(Presenter.LogicalChildren);
                    break;
            }
        }

        private void UpdatePseudoClasses()
        {
            var count = _itemsView?.Count ?? 0;
            PseudoClasses.Set(":empty", count == 0);
            PseudoClasses.Set(":singleitem", count == 1);
        }

        protected static IInputElement? GetNextControl(
            INavigableContainer container,
            NavigationDirection direction,
            IInputElement? from,
            bool wrap)
        {
            IInputElement result;
            var c = from;

            do
            {
                result = container.GetControl(direction, c, wrap);
                from = from ?? result;

                if (result != null &&
                    result.Focusable &&
                    result.IsEffectivelyEnabled &&
                    result.IsEffectivelyVisible)
                {
                    return result;
                }

                c = result;
            } while (c != null && c != from);

            return null;
        }
    }
}
