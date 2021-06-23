using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using Avalonia.Animation;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Diagnostics;
using Avalonia.Logging;
using Avalonia.LogicalTree;
using Avalonia.Styling;

#nullable enable

namespace Avalonia
{
    /// <summary>
    /// Extends an <see cref="Animatable"/> with the following features:
    /// 
    /// - An inherited <see cref="DataContext"/>.
    /// - Implements <see cref="IStyleable"/> to allow styling to work on the styled element.
    /// - Implements <see cref="ILogical"/> to form part of a logical tree.
    /// - A collection of class strings for custom styling.
    /// </summary>
    public class StyledElement : Animatable, IDataContextProvider, IStyledElement, ISetLogicalParent, ISetInheritanceParent
    {
        /// <summary>
        /// Defines the <see cref="DataContext"/> property.
        /// </summary>
        public static readonly StyledProperty<object?> DataContextProperty =
            AvaloniaProperty.Register<StyledElement, object?>(
                nameof(DataContext),
                inherits: true,
                notifying: DataContextNotifying);

        /// <summary>
        /// Defines the <see cref="Name"/> property.
        /// </summary>
        public static readonly DirectProperty<StyledElement, string?> NameProperty =
            AvaloniaProperty.RegisterDirect<StyledElement, string?>(nameof(Name), o => o.Name, (o, v) => o.Name = v);
        
        /// <summary>
        /// Defines the <see cref="Parent"/> property.
        /// </summary>
        public static readonly DirectProperty<StyledElement, IStyledElement?> ParentProperty =
            AvaloniaProperty.RegisterDirect<StyledElement, IStyledElement?>(nameof(Parent), o => o.Parent);

        /// <summary>
        /// Defines the <see cref="TemplatedParent"/> property.
        /// </summary>
        public static readonly DirectProperty<StyledElement, ITemplatedControl?> TemplatedParentProperty =
            AvaloniaProperty.RegisterDirect<StyledElement, ITemplatedControl?>(
                nameof(TemplatedParent),
                o => o.TemplatedParent,
                (o ,v) => o.TemplatedParent = v);

        private int _initCount;
        private string? _name;
        private readonly Classes _classes = new Classes();
        private ILogicalRoot? _logicalRoot;
        private IAvaloniaList<ILogical>? _logicalChildren;
        private IResourceDictionary? _resources;
        private Styles? _styles;
        private bool _styled;
        private List<IStyleInstance>? _appliedStyles;
        private ITemplatedControl? _templatedParent;
        private bool _dataContextUpdating;

        /// <summary>
        /// Initializes static members of the <see cref="StyledElement"/> class.
        /// </summary>
        static StyledElement()
        {
            DataContextProperty.Changed.AddClassHandler<StyledElement>((x,e) => x.OnDataContextChangedCore(e));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="StyledElement"/> class.
        /// </summary>
        public StyledElement()
        {
            _logicalRoot = this as ILogicalRoot;
        }

        /// <summary>
        /// Raised when the styled element is attached to a rooted logical tree.
        /// </summary>
        public event EventHandler<LogicalTreeAttachmentEventArgs>? AttachedToLogicalTree;

        /// <summary>
        /// Raised when the styled element is detached from a rooted logical tree.
        /// </summary>
        public event EventHandler<LogicalTreeAttachmentEventArgs>? DetachedFromLogicalTree;

        /// <summary>
        /// Occurs when the <see cref="DataContext"/> property changes.
        /// </summary>
        /// <remarks>
        /// This event will be raised when the <see cref="DataContext"/> property has changed and
        /// all subscribers to that change have been notified.
        /// </remarks>
        public event EventHandler? DataContextChanged;

        /// <summary>
        /// Occurs when the styled element has finished initialization.
        /// </summary>
        /// <remarks>
        /// The Initialized event indicates that all property values on the styled element have been set.
        /// When loading the styled element from markup, it occurs when 
        /// <see cref="ISupportInitialize.EndInit"/> is called *and* the styled element
        /// is attached to a rooted logical tree. When the styled element is created by code and
        /// <see cref="ISupportInitialize"/> is not used, it is called when the styled element is attached
        /// to the visual tree.
        /// </remarks>
        public event EventHandler? Initialized;

        /// <summary>
        /// Occurs when a resource in this styled element or a parent styled element has changed.
        /// </summary>
        public event EventHandler<ResourcesChangedEventArgs>? ResourcesChanged;

        /// <summary>
        /// Gets or sets the name of the styled element.
        /// </summary>
        /// <remarks>
        /// An element's name is used to uniquely identify an element within the element's name
        /// scope. Once the element is added to a logical tree, its name cannot be changed.
        /// </remarks>
        public string? Name
        {
            get => _name;

            set
            {
                if (_styled)
                {
                    throw new InvalidOperationException("Cannot set Name : styled element already styled.");
                }

                _name = value;
            }
        }

        /// <summary>
        /// Gets or sets the styled element's classes.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Classes can be used to apply user-defined styling to styled elements, or to allow styled elements
        /// that share a common purpose to be easily selected.
        /// </para>
        /// <para>
        /// Even though this property can be set, the setter is only intended for use in object
        /// initializers. Assigning to this property does not change the underlying collection,
        /// it simply clears the existing collection and adds the contents of the assigned
        /// collection.
        /// </para>
        /// </remarks>
        public Classes Classes
        {
            get
            {
                return _classes;
            }

            set
            {
                if (_classes != value)
                {
                    _classes.Replace(value);
                }
            }
        }

        /// <summary>
        /// Gets or sets the control's data context.
        /// </summary>
        /// <remarks>
        /// The data context is an inherited property that specifies the default object that will
        /// be used for data binding.
        /// </remarks>
        public object? DataContext
        {
            get { return GetValue(DataContextProperty); }
            set { SetValue(DataContextProperty, value); }
        }

        /// <summary>
        /// Gets a value that indicates whether the element has finished initialization.
        /// </summary>
        /// <remarks>
        /// For more information about when IsInitialized is set, see the <see cref="Initialized"/>
        /// event.
        /// </remarks>
        public bool IsInitialized { get; private set; }

        /// <summary>
        /// Gets the styles for the styled element.
        /// </summary>
        /// <remarks>
        /// Styles for the entire application are added to the Application.Styles collection, but
        /// each styled element may in addition define its own styles which are applied to the styled element
        /// itself and its children.
        /// </remarks>
        public Styles Styles => _styles ??= new Styles(this);

        /// <summary>
        /// Gets or sets the styled element's resource dictionary.
        /// </summary>
        public IResourceDictionary Resources
        {
            get => _resources ??= new ResourceDictionary(this);
            set
            {
                value = value ?? throw new ArgumentNullException(nameof(value));
                _resources?.RemoveOwner(this);
                _resources = value;
                _resources.AddOwner(this);
            }
        }

        /// <summary>
        /// Gets the styled element whose lookless template this styled element is part of.
        /// </summary>
        public ITemplatedControl? TemplatedParent
        {
            get => _templatedParent;
            internal set => SetAndRaise(TemplatedParentProperty, ref _templatedParent, value);
        }

        /// <summary>
        /// Gets the styled element's logical children.
        /// </summary>
        protected IAvaloniaList<ILogical> LogicalChildren
        {
            get
            {
                if (_logicalChildren == null)
                {
                    var list = new AvaloniaList<ILogical>
                    {
                        ResetBehavior = ResetBehavior.Remove,
                        Validate = logical => ValidateLogicalChild(logical)
                    };
                    list.CollectionChanged += LogicalChildrenCollectionChanged;
                    _logicalChildren = list;
                }

                return _logicalChildren;
            }
        }

        /// <summary>
        /// Gets the <see cref="Classes"/> collection in a form that allows adding and removing
        /// pseudoclasses.
        /// </summary>
        protected IPseudoClasses PseudoClasses => Classes;

        /// <summary>
        /// Gets a value indicating whether the element is attached to a rooted logical tree.
        /// </summary>
        bool ILogical.IsAttachedToLogicalTree => _logicalRoot != null;

        /// <summary>
        /// Gets the styled element's logical parent.
        /// </summary>
        public IStyledElement? Parent { get; private set; }

        /// <summary>
        /// Gets the styled element's logical parent.
        /// </summary>
        ILogical? ILogical.LogicalParent => Parent;

        /// <summary>
        /// Gets the styled element's logical children.
        /// </summary>
        IAvaloniaReadOnlyList<ILogical> ILogical.LogicalChildren => LogicalChildren;

        /// <inheritdoc/>
        bool IResourceNode.HasResources => (_resources?.HasResources ?? false) ||
            (((IResourceNode?)_styles)?.HasResources ?? false);

        /// <inheritdoc/>
        IAvaloniaReadOnlyList<string> IStyleable.Classes => Classes;

        /// <summary>
        /// Gets the type by which the styled element is styled.
        /// </summary>
        /// <remarks>
        /// Usually controls are styled by their own type, but there are instances where you want
        /// a styled element to be styled by its base type, e.g. creating SpecialButton that
        /// derives from Button and adds extra functionality but is still styled as a regular
        /// Button.
        /// </remarks>
        Type IStyleable.StyleKey => GetType();

        /// <inheritdoc/>
        bool IStyleHost.IsStylesInitialized => _styles != null;

        /// <inheritdoc/>
        IStyleHost? IStyleHost.StylingParent => (IStyleHost?)InheritanceParent;

        /// <inheritdoc/>
        public virtual void BeginInit()
        {
            ++_initCount;
        }

        /// <inheritdoc/>
        public virtual void EndInit()
        {
            if (_initCount == 0)
            {
                throw new InvalidOperationException("BeginInit was not called.");
            }

            if (--_initCount == 0 && _logicalRoot != null)
            {
                ApplyStyling();
                InitializeIfNeeded();
            }
        }

        /// <summary>
        /// Applies styling to the control if the control is initialized and styling is not
        /// already applied.
        /// </summary>
        /// <returns>
        /// A value indicating whether styling is now applied to the control.
        /// </returns>
        protected bool ApplyStyling()
        {
            if (_initCount == 0 && !_styled)
            {
                try
                {
                    BeginBatchUpdate();
                    AvaloniaLocator.Current.GetService<IStyler>()?.ApplyStyles(this);
                }
                finally
                {
                    EndBatchUpdate();
                }

                _styled = true;
            }

            return _styled;
        }

        /// <summary>
        /// Detaches all styles from the element and queues a restyle.
        /// </summary>
        protected virtual void InvalidateStyles() => DetachStyles();

        protected void InitializeIfNeeded()
        {
            if (_initCount == 0 && !IsInitialized)
            {
                IsInitialized = true;
                OnInitialized();
                Initialized?.Invoke(this, EventArgs.Empty);
            }
        }

        internal StyleDiagnostics GetStyleDiagnosticsInternal()
        {
            IReadOnlyList<IStyleInstance>? appliedStyles = _appliedStyles;

            if (appliedStyles is null)
            {
                appliedStyles = Array.Empty<IStyleInstance>();
            }

            return new StyleDiagnostics(appliedStyles);
        }

        /// <inheritdoc/>
        void ILogical.NotifyAttachedToLogicalTree(LogicalTreeAttachmentEventArgs e)
        {
            OnAttachedToLogicalTreeCore(e);
        }

        /// <inheritdoc/>
        void ILogical.NotifyDetachedFromLogicalTree(LogicalTreeAttachmentEventArgs e)
        {
            OnDetachedFromLogicalTreeCore(e);
        }

        /// <inheritdoc/>
        void ILogical.NotifyResourcesChanged(ResourcesChangedEventArgs e) => NotifyResourcesChanged(e);

        /// <inheritdoc/>
        void IResourceHost.NotifyHostedResourcesChanged(ResourcesChangedEventArgs e) => NotifyResourcesChanged(e);

        /// <inheritdoc/>
        bool IResourceNode.TryGetResource(object key, out object? value)
        {
            value = null;
            return (_resources?.TryGetResource(key, out value) ?? false) ||
                   (_styles?.TryGetResource(key, out value) ?? false);
        }

        /// <summary>
        /// Sets the styled element's logical parent.
        /// </summary>
        /// <param name="parent">The parent.</param>
        void ISetLogicalParent.SetParent(ILogical? parent)
        {
            var old = Parent;

            if (parent != old)
            {
                if (old != null && parent != null)
                {
                    throw new InvalidOperationException("The Control already has a parent.");
                }

                if (InheritanceParent == null || parent == null)
                {
                    InheritanceParent = parent as AvaloniaObject;
                }

                Parent = (IStyledElement?)parent;

                if (_logicalRoot != null)
                {
                    var e = new LogicalTreeAttachmentEventArgs(_logicalRoot, this, old);
                    OnDetachedFromLogicalTreeCore(e);
                }

                var newRoot = FindLogicalRoot(this);

                if (newRoot is object)
                {
                    var e = new LogicalTreeAttachmentEventArgs(newRoot, this, parent);
                    OnAttachedToLogicalTreeCore(e);
                }
                else if (parent is null)
                {
                    // If we were attached to the logical tree, we piggyback on the tree traversal
                    // there to raise resources changed notifications. If we're being removed from
                    // the logical tree, then traverse the tree raising notifications now.
                    //
                    // We don't raise resources changed notifications if we're being attached to a 
                    // non-rooted control beacuse it's unlikely that dynamic resources need to be 
                    // correct until the control is added to the tree, and it causes a *lot* of
                    // notifications.
                    NotifyResourcesChanged();
                }

#nullable disable
                RaisePropertyChanged(
                    ParentProperty,
                    new Optional<IStyledElement>(old),
                    new BindingValue<IStyledElement>(Parent),
                    BindingPriority.LocalValue);
#nullable enable
            }
        }

        /// <summary>
        /// Sets the styled element's inheritance parent.
        /// </summary>
        /// <param name="parent">The parent.</param>
        void ISetInheritanceParent.SetParent(IAvaloniaObject? parent)
        {
            InheritanceParent = parent;
        }

        void IStyleable.StyleApplied(IStyleInstance instance)
        {
            instance = instance ?? throw new ArgumentNullException(nameof(instance));

            _appliedStyles ??= new List<IStyleInstance>();
            _appliedStyles.Add(instance);
        }

        void IStyleable.DetachStyles() => DetachStyles();

        void IStyleable.DetachStyles(IReadOnlyList<IStyle> styles) => DetachStyles(styles);

        void IStyleable.InvalidateStyles() => InvalidateStyles();

        void IStyleHost.StylesAdded(IReadOnlyList<IStyle> styles)
        {
            InvalidateStylesOnThisAndDescendents();
        }

        void IStyleHost.StylesRemoved(IReadOnlyList<IStyle> styles)
        {
            var allStyles = RecurseStyles(styles);
            DetachStylesFromThisAndDescendents(allStyles);
        }

        protected virtual void LogicalChildrenCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    SetLogicalParent(e.NewItems);
                    break;

                case NotifyCollectionChangedAction.Remove:
                    ClearLogicalParent(e.OldItems);
                    break;

                case NotifyCollectionChangedAction.Replace:
                    ClearLogicalParent(e.OldItems);
                    SetLogicalParent(e.NewItems);
                    break;

                case NotifyCollectionChangedAction.Reset:
                    throw new NotSupportedException("Reset should not be signaled on LogicalChildren collection");
            }
        }

        /// <summary>
        /// Notifies child controls that a change has been made to resources that apply to them.
        /// </summary>
        /// <param name="e">The event args.</param>
        protected virtual void NotifyChildResourcesChanged(ResourcesChangedEventArgs e)
        {
            if (_logicalChildren is object)
            {
                var count = _logicalChildren.Count;

                if (count > 0)
                {
                    e ??= ResourcesChangedEventArgs.Empty;

                    for (var i = 0; i < count; ++i)
                    {
                        _logicalChildren[i].NotifyResourcesChanged(e);
                    }
                }
            }
        }

        /// <summary>
        /// Called when the styled element is added to a rooted logical tree.
        /// </summary>
        /// <param name="e">The event args.</param>
        protected virtual void OnAttachedToLogicalTree(LogicalTreeAttachmentEventArgs e)
        {
        }

        /// <summary>
        /// Called when the styled element is removed from a rooted logical tree.
        /// </summary>
        /// <param name="e">The event args.</param>
        protected virtual void OnDetachedFromLogicalTree(LogicalTreeAttachmentEventArgs e)
        {
        }

        /// <summary>
        /// Called when the <see cref="DataContext"/> property changes.
        /// </summary>
        /// <param name="e">The event args.</param>
        protected virtual void OnDataContextChanged(EventArgs e)
        {
            DataContextChanged?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Called when the <see cref="DataContext"/> begins updating.
        /// </summary>
        protected virtual void OnDataContextBeginUpdate()
        {
        }

        /// <summary>
        /// Called when the <see cref="DataContext"/> finishes updating.
        /// </summary>
        protected virtual void OnDataContextEndUpdate()
        {
        }

        /// <summary>
        /// Called when the control finishes initialization.
        /// </summary>
        protected virtual void OnInitialized()
        {
        }

        private static void DataContextNotifying(IAvaloniaObject o, bool updateStarted)
        {
            if (o is StyledElement element)
            {
                DataContextNotifying(element, updateStarted);
            }
        }

        private static void DataContextNotifying(StyledElement element, bool updateStarted)
        {
            if (updateStarted)
            {
                if (!element._dataContextUpdating)
                {
                    element._dataContextUpdating = true;
                    element.OnDataContextBeginUpdate();

                    var logicalChildren = element.LogicalChildren;
                    var logicalChildrenCount = logicalChildren.Count;

                    for (var i = 0; i < logicalChildrenCount; i++)
                    {
                        if (element.LogicalChildren[i] is StyledElement s &&
                            s.InheritanceParent == element &&
                            !s.IsSet(DataContextProperty))
                        {
                            DataContextNotifying(s, updateStarted);
                        }
                    }
                }
            }
            else
            {
                if (element._dataContextUpdating)
                {
                    element.OnDataContextEndUpdate();
                    element._dataContextUpdating = false;
                }
            }
        }

        private static ILogicalRoot? FindLogicalRoot(IStyleHost? e)
        {
            while (e != null)
            {
                if (e is ILogicalRoot root)
                {
                    return root;
                }

                e = e.StylingParent;
            }

            return null;
        }

        private static void ValidateLogicalChild(ILogical c)
        {
            if (c == null)
            {
                throw new ArgumentException("Cannot add null to LogicalChildren.");
            }
        }

        private void OnAttachedToLogicalTreeCore(LogicalTreeAttachmentEventArgs e)
        {
            if (this.GetLogicalParent() == null && !(this is ILogicalRoot))
            {
                throw new InvalidOperationException(
                    $"AttachedToLogicalTreeCore called for '{GetType().Name}' but control has no logical parent.");
            }

            // This method can be called when a control is already attached to the logical tree
            // in the following scenario:
            // - ListBox gets assigned Items containing ListBoxItem
            // - ListBox makes ListBoxItem a logical child
            // - ListBox template gets applied; making its Panel get attached to logical tree
            // - That AttachedToLogicalTree signal travels down to the ListBoxItem
            if (_logicalRoot == null)
            {
                _logicalRoot = e.Root;

                ApplyStyling();
                NotifyResourcesChanged(propagate: false);

                OnAttachedToLogicalTree(e);
                AttachedToLogicalTree?.Invoke(this, e);
            }

            var logicalChildren = LogicalChildren;
            var logicalChildrenCount = logicalChildren.Count;

            for (var i = 0; i < logicalChildrenCount; i++)
            {
                if (logicalChildren[i] is StyledElement child)
                {
                    child.OnAttachedToLogicalTreeCore(e);
                }
            }
        }

        private void OnDetachedFromLogicalTreeCore(LogicalTreeAttachmentEventArgs e)
        {
            if (_logicalRoot != null)
            {
                _logicalRoot = null;
                DetachStyles();
                OnDetachedFromLogicalTree(e);
                DetachedFromLogicalTree?.Invoke(this, e);

                var logicalChildren = LogicalChildren;
                var logicalChildrenCount = logicalChildren.Count;

                for (var i = 0; i < logicalChildrenCount; i++)
                {
                    if (logicalChildren[i] is StyledElement child)
                    {
                        child.OnDetachedFromLogicalTreeCore(e);
                    }
                }

#if DEBUG
                if (((INotifyCollectionChangedDebug)Classes).GetCollectionChangedSubscribers()?.Length > 0)
                {
                    Logger.TryGet(LogEventLevel.Warning, LogArea.Control)?.Log(
                        this,
                        "{Type} detached from logical tree but still has class listeners",
                        GetType());
                }
#endif
            }
        }

        private void OnDataContextChangedCore(AvaloniaPropertyChangedEventArgs e)
        {
            OnDataContextChanged(EventArgs.Empty);
        }

        private void SetLogicalParent(IList children)
        {
            var count = children.Count;

            for (var i = 0; i < count; i++)
            {
                var logical = (ILogical) children[i];
                
                if (logical.LogicalParent is null)
                {
                    ((ISetLogicalParent)logical).SetParent(this);
                }
            }
        }

        private void ClearLogicalParent(IList children)
        {
            var count = children.Count;

            for (var i = 0; i < count; i++)
            {
                var logical = (ILogical) children[i];
                
                if (logical.LogicalParent == this)
                {
                    ((ISetLogicalParent)logical).SetParent(null);
                }
            }
        }

        private void DetachStyles()
        {
            if (_appliedStyles is object)
            {
                BeginBatchUpdate();

                try
                {
                    foreach (var i in _appliedStyles)
                    {
                        i.Dispose();
                    }

                    _appliedStyles.Clear();
                }
                finally
                {
                    EndBatchUpdate();
                }
            }

            _styled = false;
        }

        private void DetachStyles(IReadOnlyList<IStyle> styles)
        {
            styles = styles ?? throw new ArgumentNullException(nameof(styles));

            if (_appliedStyles is null)
            {
                return;
            }

            var count = styles.Count;

            for (var i = 0; i < count; ++i)
            {
                for (var j = _appliedStyles.Count - 1; j >= 0; --j)
                {
                    var applied = _appliedStyles[j];

                    if (applied.Source == styles[i])
                    {
                        applied.Dispose();
                        _appliedStyles.RemoveAt(j);
                    }
                }
            }
        }

        private void InvalidateStylesOnThisAndDescendents()
        {
            InvalidateStyles();

            if (_logicalChildren is object)
            {
                var childCount = _logicalChildren.Count;

                for (var i = 0; i < childCount; ++i)
                {
                    (_logicalChildren[i] as StyledElement)?.InvalidateStylesOnThisAndDescendents();
                }
            }
        }

        private void DetachStylesFromThisAndDescendents(IReadOnlyList<IStyle> styles)
        {
            DetachStyles(styles);

            if (_logicalChildren is object)
            {
                var childCount = _logicalChildren.Count;

                for (var i = 0; i < childCount; ++i)
                {
                    (_logicalChildren[i] as StyledElement)?.DetachStylesFromThisAndDescendents(styles);
                }
            }
        }

        private void NotifyResourcesChanged(
            ResourcesChangedEventArgs? e = null,
            bool propagate = true)
        {
            if (ResourcesChanged is object)
            {
                e ??= ResourcesChangedEventArgs.Empty;
                ResourcesChanged(this, e);
            }

            if (propagate)
            {
                e ??= ResourcesChangedEventArgs.Empty;
                NotifyChildResourcesChanged(e);
            }
        }

        private static IReadOnlyList<IStyle> RecurseStyles(IReadOnlyList<IStyle> styles)
        {
            var count = styles.Count;
            List<IStyle>? result = null;

            for (var i = 0; i < count; ++i)
            {
                var style = styles[i];

                if (style.Children.Count > 0)
                {
                    if (result is null)
                    {
                        result = new List<IStyle>(styles);
                    }

                    RecurseStyles(style.Children, result);
                }
            }

            return result ?? styles;
        }

        private static void RecurseStyles(IReadOnlyList<IStyle> styles, List<IStyle> result)
        {
            var count = styles.Count;

            for (var i = 0; i < count; ++i)
            {
                var style = styles[i];
                result.Add(style);
                RecurseStyles(style.Children, result);
            }
        }
    }
}
