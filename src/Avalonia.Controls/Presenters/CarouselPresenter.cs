using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Animation;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Utils;

#nullable enable

namespace Avalonia.Controls.Presenters
{
    /// <summary>
    /// Displays the items for a <see cref="Carousel"/>.
    /// </summary>
    public class CarouselPresenter : Panel, IItemsPresenter, ICollectionChangedListener
    {
        /// <summary>
        /// Defines the <see cref="IsVirtualized"/> property.
        /// </summary>
        public static readonly StyledProperty<bool> IsVirtualizedProperty =
            Carousel.IsVirtualizedProperty.AddOwner<CarouselPresenter>();

        /// <summary>
        /// Defines the <see cref="ItemsView"/> property.
        /// </summary>
        public static readonly DirectProperty<CarouselPresenter, ItemsSourceView?> ItemsViewProperty =
            ItemsPresenter.ItemsViewProperty.AddOwner<CarouselPresenter>(o => o.ItemsView, (o, v) => o.ItemsView = v);
        
        /// <summary>
        /// Defines the <see cref="PageTransition"/> property.
        /// </summary>
        public static readonly StyledProperty<IPageTransition> PageTransitionProperty =
            Carousel.PageTransitionProperty.AddOwner<CarouselPresenter>();

        /// <summary>
        /// Defines the <see cref="SelectedIndex"/> property.
        /// </summary>
        public static readonly DirectProperty<CarouselPresenter, int> SelectedIndexProperty =
            SelectingItemsControl.SelectedIndexProperty.AddOwner<CarouselPresenter>(
                o => o.SelectedIndex,
                (o, v) => o.SelectedIndex = v);

        private IItemsPresenterHost? _host;
        private IElementFactory? _elementFactory;
        private ItemsSourceView? _itemsView;
        private int _realizedIndex = -1;
        private List<IControl?>? _nonVirtualizedContainers;
        private int _selectedIndex;
        private Task? _currentTransition;
        private Visual? _transitionFromControl;
        private Visual? _transitionToControl;
        private bool  _queuedTransition;
        private bool _transitionForward;

        /// <summary>
        /// Gets or sets the element factory used to create items for the control.
        /// </summary>
        public IElementFactory? ElementFactory
        {
            get => _elementFactory;
            private set
            {
                if (_elementFactory != value)
                {
                    _elementFactory = value;
                    ResetContainers();
                }
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the items in the carousel are virtualized.
        /// </summary>
        /// <remarks>
        /// When the carousel is virtualized, only the active page is held in memory.
        /// </remarks>
        public bool IsVirtualized
        {
            get => GetValue(IsVirtualizedProperty);
            set => SetValue(IsVirtualizedProperty, value);
        }

        /// <summary>
        /// Gets or sets the items view containing the items to display in the presenter.
        /// </summary>
        public ItemsSourceView? ItemsView
        {
            get => _itemsView;
            set
            {
                if (value is null || value is ItemsSourceView)
                {
                    if (value != _itemsView)
                    {
                        var oldValue = _itemsView;
                        _itemsView?.RemoveListener(this);
                        _itemsView = value;
                        _itemsView?.AddListener(this);
                        RaisePropertyChanged(ItemsViewProperty, oldValue, _itemsView);
                        InvalidateMeasure();
                    }
                }
                else
                {
                    throw new ArgumentException("Carousel.Items must be an ItemsSourceView.");
                }
            }
        }
        
        /// <summary>
        /// Gets or sets a transition to use when switching pages.
        /// </summary>
        public IPageTransition PageTransition
        {
            get { return GetValue(PageTransitionProperty); }
            set { SetValue(PageTransitionProperty, value); }
        }

        /// <summary>
        /// Gets or sets the index of the selected page.
        /// </summary>
        public int SelectedIndex
        {
            get => _selectedIndex;
            set => SetAndRaise(SelectedIndexProperty, ref _selectedIndex, value);
        }

        /// <summary>
        /// Gets the currently realized elements.
        /// </summary>
        public IEnumerable<IControl> RealizedElements => VisualChildren.OfType<IControl>();

        public event EventHandler<ElementPreparedEventArgs>? ElementPrepared;
        public event EventHandler<ElementClearingEventArgs>? ElementClearing;
        public event EventHandler<ElementIndexChangedEventArgs>? ElementIndexChanged;

        public int GetElementIndex(IControl element)
        {
            if (IsVirtualized)
                return element == Children[0] ? _realizedIndex : -1;
            else
                return _nonVirtualizedContainers?.IndexOf(element) ?? -1;
        }

        public bool ScrollIntoView(int index)
        {
            // Scrolling into view in a carousel is not supported: the selected item must be
            // set in order for an element to be made visible.
            return false;
        }

        public IControl? TryGetElement(int index)
        {
            if (index < 0)
            {
                return null;
            }
            else if (IsVirtualized)
            {
                return index == _realizedIndex ? Children[0] : null;
            }
            else if (index < _nonVirtualizedContainers?.Count)
            {
                return _nonVirtualizedContainers?[index];
            }

            return null;
        }


        void ICollectionChangedListener.PreChanged(INotifyCollectionChanged sender, NotifyCollectionChangedEventArgs e)
        {
            // Handle collection changed events early so that we can update _realizedIndex before we
            // receive the SelectedIndex property change. This avoids having to carry out an unnecessary
            // layout pass because although the selected index has changed, the item being displayed
            // hasn't.
            OnItemsCollectionChanged(e);
        }

        void ICollectionChangedListener.Changed(INotifyCollectionChanged sender, NotifyCollectionChangedEventArgs e) { }
        void ICollectionChangedListener.PostChanged(INotifyCollectionChanged sender, NotifyCollectionChangedEventArgs e) { }

        protected override Size MeasureOverride(Size availableSize)
        {
            if (IsVirtualized)
                return MeasureVirtualized(availableSize);
            else
                return MeasureNonVirtualized(availableSize);
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            var result = base.ArrangeOverride(finalSize);
            var transition = PageTransition;

            if (transition is object &&
                _currentTransition is null &&
                (_transitionFromControl is object || _transitionToControl is object))
            {
                StartTransition();
            }
            
            return result;
        }

        protected override void OnPropertyChanged<T>(AvaloniaPropertyChangedEventArgs<T> change)
        {
            if (change.Property == SelectedIndexProperty &&
                change.NewValue.GetValueOrDefault<int>() != _realizedIndex)
            {
                InvalidateMeasure();
            }
            else if (change.Property == TemplatedParentProperty)
            {
                _host = change.NewValue.GetValueOrDefault<IItemsPresenterHost>();

                if (_host is object)
                {
                    _host.RegisterItemsPresenter(this);
                    ElementFactory = _host.ElementFactory;
                }
            }
            else if (change.Property == IsVirtualizedProperty)
            {
                InvalidateMeasure();
                Children.Clear();
                _realizedIndex = -1;
                _nonVirtualizedContainers = null;
            }
            else if (change.Property == ItemsViewProperty)
            {
                ResetContainers();
            }

            base.OnPropertyChanged(change);
        }

        private Size MeasureVirtualized(Size availableSize)
        {
            if (_itemsView is null || _elementFactory is null || _selectedIndex < 0 || _selectedIndex > _itemsView.Count)
            {
                Children.Clear();
                _realizedIndex = -1;
                return default;
            }

            if (_realizedIndex != _selectedIndex)
            {
                if (_currentTransition is null)
                {
                    var performTransition = PageTransition is object && _realizedIndex >= 0;

                    if (!performTransition)
                    {
                        Children.Clear();
                    }
                    else
                    {
                        _transitionFromControl = Children.FirstOrDefault() as Visual;
                        _transitionForward = _selectedIndex > _realizedIndex;
                    }

                    var element = GetElement(_selectedIndex);
                    _realizedIndex = _selectedIndex;

                    if (performTransition)
                        _transitionToControl = element as Visual;
                }
                else
                {
                    _queuedTransition = true;
                }
            }

            return base.MeasureOverride(availableSize);
        }

        private Size MeasureNonVirtualized(Size availableSize)
        {
            if (_itemsView is null || _elementFactory is null)
            {
                Children.Clear();
                _realizedIndex = -1;
                return default;
            }

            if (_nonVirtualizedContainers is null)
            {
                _nonVirtualizedContainers = new List<IControl?>(_itemsView.Count);
                AddNullNonVirtualizedEntries(0, _itemsView.Count);
            }

            var performTransition = PageTransition is object &&
                _realizedIndex != _selectedIndex &&
                _realizedIndex >= 0;

            if (performTransition)
            {
                _transitionForward = _selectedIndex > _realizedIndex;

                if (_currentTransition is object)
                {
                    _queuedTransition = true;
                    return base.MeasureOverride(availableSize);
                }
            }

            var result = Size.Empty;

            for (var i = 0; i < _nonVirtualizedContainers.Count; ++i)
            {
                var element = _nonVirtualizedContainers[i] ??= GetElement(i);

                if (i == _selectedIndex)
                {
                    element.IsVisible = true;
                    element.Measure(availableSize);
                    result = element.DesiredSize;

                    if (performTransition)
                        _transitionToControl = element as Visual;
                }
                else
                {
                    if (performTransition && i == _realizedIndex)
                        _transitionFromControl = element as Visual;

                    element.IsVisible = false;
                }
            }

            _realizedIndex = _selectedIndex;
            return result;
        }

        private IControl GetElement(int index)
        {
            var data = _itemsView![index];
            var element = _elementFactory!.GetElement(new ElementFactoryGetArgs
            {
                Data = data,
                Index = index,
                Parent = this,
            });

            element.DataContext = data;

            if (element.VisualParent != this)
                Children.Add(element);

            return element;
        }

        private void OnItemsCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            void RemoveItems(int startIndex, int count)
            {
                if (_nonVirtualizedContainers is null)
                    return;

                for (var i = 0; i < count; ++i)
                {
                    var element = _nonVirtualizedContainers[startIndex];
                    Children.Remove(element);
                    _nonVirtualizedContainers.RemoveAt(startIndex);
                }
            }

            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    if (e.NewStartingIndex <= _realizedIndex)
                        _realizedIndex += e.NewItems.Count;

                    AddNullNonVirtualizedEntries(e.NewStartingIndex, e.NewItems.Count);
                    break;
                case NotifyCollectionChangedAction.Remove:
                    if (e.OldStartingIndex + e.OldItems.Count < _realizedIndex)
                    {
                        _realizedIndex -= e.OldItems.Count;
                    }
                    else if (e.OldStartingIndex <= _realizedIndex &&
                        e.OldStartingIndex + e.OldItems.Count > _realizedIndex)
                    {
                        _realizedIndex = -1;

                        if (IsVirtualized)
                            Children.Clear();

                        InvalidateMeasure();
                    }

                    RemoveItems(e.OldStartingIndex, e.OldItems.Count);
                    break;
                case NotifyCollectionChangedAction.Replace:
                    if (e.NewStartingIndex <= _realizedIndex &&
                        e.NewStartingIndex + e.NewItems.Count > _realizedIndex)
                    {
                        InvalidateMeasure();
                    }

                    if (_nonVirtualizedContainers is object)
                    {
                        for (var i = e.OldStartingIndex; i < e.OldStartingIndex + e.OldItems.Count; ++i)
                        {
                            var element = _nonVirtualizedContainers[i];
                            Children.Remove(element);
                            _nonVirtualizedContainers[i] = null;
                        }
                    }
                    break;
                case NotifyCollectionChangedAction.Move:
                    if (e.OldStartingIndex <= _realizedIndex &&
                        e.OldStartingIndex + e.OldItems.Count > _realizedIndex)
                    {
                        _realizedIndex = (_realizedIndex - e.OldStartingIndex) + e.NewStartingIndex;
                        InvalidateMeasure();
                    }

                    RemoveItems(e.OldStartingIndex, e.OldItems.Count);
                    AddNullNonVirtualizedEntries(e.NewStartingIndex, e.NewItems.Count);
                    break;
                case NotifyCollectionChangedAction.Reset:
                    ResetContainers();
                    break;
                default:
                    throw new NotImplementedException();
            }
        }

        private void AddNullNonVirtualizedEntries(int index, int count)
        {
            if (_nonVirtualizedContainers is null)
                return;

            for (var i = 0; i < count; ++i)
                _nonVirtualizedContainers.Insert(index, null);

            InvalidateMeasure();
        }

        private void ResetContainers()
        {
            if (_nonVirtualizedContainers is object)
            {
                _nonVirtualizedContainers.Clear();
                AddNullNonVirtualizedEntries(0, ItemsView?.Count ?? 0);
                Children.Clear();
            }
            else
            {
                _realizedIndex = -1;
                Children.Clear();
                InvalidateMeasure();
            }
        }

        private void StartTransition()
        {
            _currentTransition = PageTransition.Start(_transitionFromControl, _transitionToControl, _transitionForward);
            _currentTransition.ContinueWith(TransitionCompleted, TaskScheduler.FromCurrentSynchronizationContext());
        }

        private void TransitionCompleted(Task task)
        {
            if (IsVirtualized)
            {
                if (_transitionFromControl is object)
                    Children.Remove((IControl)_transitionFromControl);
            }

            _currentTransition = null;
            _transitionFromControl = _transitionToControl = null;

            if (_queuedTransition)
            {
                InvalidateMeasure();
                _queuedTransition = false;
            }
        }
    }
}
