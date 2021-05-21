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
using Avalonia.Input;
using Avalonia.LogicalTree;
using Avalonia.Metadata;
using Avalonia.VisualTree;

namespace Avalonia.Controls
{
    /// <summary>
    /// Displays a collection of items.
    /// </summary>
    [PseudoClasses(":empty", ":singleitem")]
    public class ItemsControl : TemplatedControl, IItemsPresenterHost, ICollectionChangedListener
    {
        /// <summary>
        /// The default value for the <see cref="ItemsPanel"/> property.
        /// </summary>
        private static readonly FuncTemplate<IPanel> DefaultPanel =
            new FuncTemplate<IPanel>(() => new StackPanel());

        /// <summary>
        /// Defines the <see cref="Items"/> property.
        /// </summary>
        public static readonly DirectProperty<ItemsControl, IEnumerable> ItemsProperty =
            AvaloniaProperty.RegisterDirect<ItemsControl, IEnumerable>(nameof(Items), o => o.Items, (o, v) => o.Items = v);

        /// <summary>
        /// Defines the <see cref="ItemCount"/> property.
        /// </summary>
        public static readonly DirectProperty<ItemsControl, int> ItemCountProperty =
            AvaloniaProperty.RegisterDirect<ItemsControl, int>(nameof(ItemCount), o => o.ItemCount);

        /// <summary>
        /// Defines the <see cref="ItemsPanel"/> property.
        /// </summary>
        public static readonly StyledProperty<ITemplate<IPanel>> ItemsPanelProperty =
            AvaloniaProperty.Register<ItemsControl, ITemplate<IPanel>>(nameof(ItemsPanel), DefaultPanel);

        /// <summary>
        /// Defines the <see cref="ItemTemplate"/> property.
        /// </summary>
        public static readonly StyledProperty<IDataTemplate> ItemTemplateProperty =
            AvaloniaProperty.Register<ItemsControl, IDataTemplate>(nameof(ItemTemplate));

        private IEnumerable _items = new AvaloniaList<object>();
        private int _itemCount;
        private IItemContainerGenerator _itemContainerGenerator;

        /// <summary>
        /// Initializes static members of the <see cref="ItemsControl"/> class.
        /// </summary>
        static ItemsControl()
        {
            ItemsProperty.Changed.AddClassHandler<ItemsControl>((x, e) => x.ItemsChanged(e));
            ItemTemplateProperty.Changed.AddClassHandler<ItemsControl>((x, e) => x.ItemTemplateChanged(e));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ItemsControl"/> class.
        /// </summary>
        public ItemsControl()
        {
            UpdatePseudoClasses(0);
            SubscribeToItems(_items);
        }

        /// <summary>
        /// Gets the <see cref="IItemContainerGenerator"/> for the control.
        /// </summary>
        public IItemContainerGenerator ItemContainerGenerator
        {
            get
            {
                if (_itemContainerGenerator == null)
                {
                    _itemContainerGenerator = CreateItemContainerGenerator();

                    if (_itemContainerGenerator != null)
                    {
                        _itemContainerGenerator.ItemTemplate = ItemTemplate;
                        _itemContainerGenerator.Materialized += (_, e) => OnContainersMaterialized(e);
                        _itemContainerGenerator.Dematerialized += (_, e) => OnContainersDematerialized(e);
                        _itemContainerGenerator.Recycled += (_, e) => OnContainersRecycled(e);
                    }
                }

                return _itemContainerGenerator;
            }
        }

        /// <summary>
        /// Gets or sets the items to display.
        /// </summary>
        [Content]
        public IEnumerable Items
        {
            get { return _items; }
            set { SetAndRaise(ItemsProperty, ref _items, value); }
        }

        /// <summary>
        /// Gets the number of items in <see cref="Items"/>.
        /// </summary>
        public int ItemCount
        {
            get => _itemCount;
            private set => SetAndRaise(ItemCountProperty, ref _itemCount, value);
        }

        /// <summary>
        /// Gets or sets the panel used to display the items.
        /// </summary>
        public ITemplate<IPanel> ItemsPanel
        {
            get { return GetValue(ItemsPanelProperty); }
            set { SetValue(ItemsPanelProperty, value); }
        }

        /// <summary>
        /// Gets or sets the data template used to display the items in the control.
        /// </summary>
        public IDataTemplate ItemTemplate
        {
            get { return GetValue(ItemTemplateProperty); }
            set { SetValue(ItemTemplateProperty, value); }
        }

        /// <summary>
        /// Gets the items presenter control.
        /// </summary>
        public IItemsPresenter Presenter
        {
            get;
            protected set;
        }

        /// <inheritdoc/>
        void IItemsPresenterHost.RegisterItemsPresenter(IItemsPresenter presenter)
        {
            Presenter = presenter;
            ItemContainerGenerator.Clear();
        }

        void ICollectionChangedListener.PreChanged(INotifyCollectionChanged sender, NotifyCollectionChangedEventArgs e)
        {
        }

        void ICollectionChangedListener.Changed(INotifyCollectionChanged sender, NotifyCollectionChangedEventArgs e)
        {
        }

        void ICollectionChangedListener.PostChanged(INotifyCollectionChanged sender, NotifyCollectionChangedEventArgs e)
        {
            ItemsCollectionChanged(sender, e);
        }

        /// <summary>
        /// Gets the item at the specified index in a collection.
        /// </summary>
        /// <param name="items">The collection.</param>
        /// <param name="index">The index.</param>
        /// <returns>The item at the given index or null if the index is out of bounds.</returns>
        protected static object ElementAt(IEnumerable items, int index)
        {
            if (index != -1 && index < items.Count())
            {
                return items.ElementAt(index) ?? null;
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Gets the index of an item in a collection.
        /// </summary>
        /// <param name="items">The collection.</param>
        /// <param name="item">The item.</param>
        /// <returns>The index of the item or -1 if the item was not found.</returns>
        protected static int IndexOf(IEnumerable items, object item)
        {
            if (items != null && item != null)
            {
                var list = items as IList;

                if (list != null)
                {
                    return list.IndexOf(item);
                }
                else
                {
                    int index = 0;

                    foreach (var i in items)
                    {
                        if (Equals(i, item))
                        {
                            return index;
                        }

                        ++index;
                    }
                }
            }

            return -1;
        }

        /// <summary>
        /// Creates the <see cref="ItemContainerGenerator"/> for the control.
        /// </summary>
        /// <returns>
        /// An <see cref="IItemContainerGenerator"/> or null.
        /// </returns>
        /// <remarks>
        /// Certain controls such as <see cref="TabControl"/> don't actually create item 
        /// containers; however they want it to be ItemsControls so that they have an Items 
        /// property etc. In this case, a derived class can override this method to return null
        /// in order to disable the creation of item containers.
        /// </remarks>
        protected virtual IItemContainerGenerator CreateItemContainerGenerator()
        {
            return new ItemContainerGenerator(this);
        }

        /// <summary>
        /// Called when new containers are materialized for the <see cref="ItemsControl"/> by its
        /// <see cref="ItemContainerGenerator"/>.
        /// </summary>
        /// <param name="e">The details of the containers.</param>
        protected virtual void OnContainersMaterialized(ItemContainerEventArgs e)
        {
            foreach (var container in e.Containers)
            {
                // If the item is its own container, then it will be added to the logical tree when
                // it was added to the Items collection.
                if (container.ContainerControl != null && container.ContainerControl != container.Item)
                {
                    LogicalChildren.Add(container.ContainerControl);
                }
            }
        }

        /// <summary>
        /// Called when containers are dematerialized for the <see cref="ItemsControl"/> by its
        /// <see cref="ItemContainerGenerator"/>.
        /// </summary>
        /// <param name="e">The details of the containers.</param>
        protected virtual void OnContainersDematerialized(ItemContainerEventArgs e)
        {
            foreach (var container in e.Containers)
            {
                // If the item is its own container, then it will be removed from the logical tree
                // when it is removed from the Items collection.
                if (container?.ContainerControl != container?.Item)
                {
                    LogicalChildren.Remove(container.ContainerControl);
                }
            }
        }

        /// <summary>
        /// Called when containers are recycled for the <see cref="ItemsControl"/> by its
        /// <see cref="ItemContainerGenerator"/>.
        /// </summary>
        /// <param name="e">The details of the containers.</param>
        protected virtual void OnContainersRecycled(ItemContainerEventArgs e)
        {
        }

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
                var container = Presenter?.Panel as INavigableContainer;

                if (container == null ||
                    focus.Current == null ||
                    direction == null ||
                    direction.Value.IsTab())
                {
                    return;
                }

                var current = focus.Current;

                while (current != null)
                {
                    if (current.InputParent == container)
                    {
                        var next = GetNextControl(container, direction.Value, current, false);

                        if (next != null)
                        {
                            focus.Focus(next, NavigationMethod.Directional, e.KeyModifiers);
                            e.Handled = true;
                        }

                        break;
                    }

                    current = current.InputParent;
                }
            }

            base.OnKeyDown(e);
        }

        protected override void OnPropertyChanged<T>(AvaloniaPropertyChangedEventArgs<T> change)
        {
            base.OnPropertyChanged(change);

            if (change.Property == ItemCountProperty)
            {
                UpdatePseudoClasses(change.NewValue.GetValueOrDefault<int>());
            }
        }

        /// <summary>
        /// Called when the <see cref="Items"/> property changes.
        /// </summary>
        /// <param name="e">The event args.</param>
        protected virtual void ItemsChanged(AvaloniaPropertyChangedEventArgs e)
        {
            var oldValue = e.OldValue as IEnumerable;
            var newValue = e.NewValue as IEnumerable;

            if (oldValue is INotifyCollectionChanged incc)
            {
                CollectionChangedEventManager.Instance.RemoveListener(incc, this);
            }

            UpdateItemCount();
            RemoveControlItemsFromLogicalChildren(oldValue);
            AddControlItemsToLogicalChildren(newValue);

            if (Presenter != null)
            {
                Presenter.Items = newValue;
            }

            SubscribeToItems(newValue);
        }

        /// <summary>
        /// Called when the <see cref="INotifyCollectionChanged.CollectionChanged"/> event is
        /// raised on <see cref="Items"/>.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event args.</param>
        protected virtual void ItemsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            UpdateItemCount();

            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    AddControlItemsToLogicalChildren(e.NewItems);
                    break;

                case NotifyCollectionChangedAction.Remove:
                    RemoveControlItemsFromLogicalChildren(e.OldItems);
                    break;
            }

            Presenter?.ItemsChanged(e);
        }

        /// <summary>
        /// Given a collection of items, adds those that are controls to the logical children.
        /// </summary>
        /// <param name="items">The items.</param>
        private void AddControlItemsToLogicalChildren(IEnumerable items)
        {
            var toAdd = new List<ILogical>();

            if (items != null)
            {
                foreach (var i in items)
                {
                    var control = i as IControl;

                    if (control != null && !LogicalChildren.Contains(control))
                    {
                        toAdd.Add(control);
                    }
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

            if (items != null)
            {
                foreach (var i in items)
                {
                    var control = i as IControl;

                    if (control != null)
                    {
                        toRemove.Add(control);
                    }
                }
            }

            LogicalChildren.RemoveAll(toRemove);
        }

        /// <summary>
        /// Subscribes to an <see cref="Items"/> collection.
        /// </summary>
        /// <param name="items">The items collection.</param>
        private void SubscribeToItems(IEnumerable items)
        {
            if (items is INotifyCollectionChanged incc)
            {
                CollectionChangedEventManager.Instance.AddListener(incc, this);
            }
        }

        /// <summary>
        /// Called when the <see cref="ItemTemplate"/> changes.
        /// </summary>
        /// <param name="e">The event args.</param>
        private void ItemTemplateChanged(AvaloniaPropertyChangedEventArgs e)
        {
            if (_itemContainerGenerator != null)
            {
                _itemContainerGenerator.ItemTemplate = (IDataTemplate)e.NewValue;
                // TODO: Rebuild the item containers.
            }
        }

        private void UpdateItemCount()
        {
            if (Items == null)
            {
                ItemCount = 0;
            }
            else if (Items is IList list)
            {
                ItemCount = list.Count;
            }
            else
            {
                ItemCount = Items.Count();
            }
        }

        private void UpdatePseudoClasses(int itemCount)
        {
            PseudoClasses.Set(":empty", itemCount == 0);
            PseudoClasses.Set(":singleitem", itemCount == 1);
        }

        protected static IInputElement GetNextControl(
            INavigableContainer container,
            NavigationDirection direction,
            IInputElement from,
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
                    result.IsClosestVisualEffectivelyVisible())
                {
                    return result;
                }

                c = result;
            } while (c != null && c != from);

            return null;
        }
    }
}
