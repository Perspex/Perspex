using System;
using System.Collections;
using System.Collections.Specialized;
using Avalonia.Collections;
using Avalonia.Data;
using Avalonia.Logging;
using Avalonia.LogicalTree;
using Avalonia.Media;
using Avalonia.Metadata;
using Avalonia.Rendering;
using Avalonia.Utilities;
using Avalonia.VisualTree;

#nullable enable

namespace Avalonia
{
    /// <summary>
    /// Base class for controls that provides rendering and related visual properties.
    /// </summary>
    /// <remarks>
    /// The <see cref="Visual"/> class represents elements that have a visual on-screen
    /// representation and stores all the information needed for an 
    /// <see cref="IRenderer"/> to render the control. To traverse the visual tree, use the
    /// extension methods defined in <see cref="VisualExtensions"/>.
    /// </remarks>
    [UsableDuringInitialization]
    public class Visual : StyledElement, IVisual
    {
        /// <summary>
        /// Defines the <see cref="Bounds"/> property.
        /// </summary>
        public static readonly DirectProperty<Visual, Rect> BoundsProperty =
            AvaloniaProperty.RegisterDirect<Visual, Rect>(nameof(Bounds), o => o.Bounds);

        public static readonly DirectProperty<Visual, TransformedBounds?> TransformedBoundsProperty =
            AvaloniaProperty.RegisterDirect<Visual, TransformedBounds?>(
                nameof(TransformedBounds),
                o => o.TransformedBounds);

        /// <summary>
        /// Defines the <see cref="ClipToBounds"/> property.
        /// </summary>
        public static readonly StyledProperty<bool> ClipToBoundsProperty =
            AvaloniaProperty.Register<Visual, bool>(nameof(ClipToBounds));

        /// <summary>
        /// Defines the <see cref="Clip"/> property.
        /// </summary>
        public static readonly StyledProperty<Geometry?> ClipProperty =
            AvaloniaProperty.Register<Visual, Geometry?>(nameof(Clip));

        /// <summary>
        /// Defines the <see cref="IsVisibleProperty"/> property.
        /// </summary>
        public static readonly StyledProperty<bool> IsVisibleProperty =
            AvaloniaProperty.Register<Visual, bool>(nameof(IsVisible), true);

        /// <summary>
        /// Defines the <see cref="Opacity"/> property.
        /// </summary>
        public static readonly StyledProperty<double> OpacityProperty =
            AvaloniaProperty.Register<Visual, double>(nameof(Opacity), 1);

        /// <summary>
        /// Defines the <see cref="OpacityMask"/> property.
        /// </summary>
        public static readonly StyledProperty<IBrush?> OpacityMaskProperty =
            AvaloniaProperty.Register<Visual, IBrush?>(nameof(OpacityMask));

        /// <summary>
        /// Defines the <see cref="RenderTransform"/> property.
        /// </summary>
        public static readonly StyledProperty<ITransform?> RenderTransformProperty =
            AvaloniaProperty.Register<Visual, ITransform?>(nameof(RenderTransform));

        /// <summary>
        /// Defines the <see cref="RenderTransformOrigin"/> property.
        /// </summary>
        public static readonly StyledProperty<RelativePoint> RenderTransformOriginProperty =
            AvaloniaProperty.Register<Visual, RelativePoint>(nameof(RenderTransformOrigin), defaultValue: RelativePoint.Center);

        /// <summary>
        /// Defines the <see cref="IVisual.VisualParent"/> property.
        /// </summary>
        public static readonly DirectProperty<Visual, IVisual?> VisualParentProperty =
            AvaloniaProperty.RegisterDirect<Visual, IVisual?>(nameof(IVisual.VisualParent), o => o._visualParent);

        /// <summary>
        /// Defines the <see cref="ZIndex"/> property.
        /// </summary>
        public static readonly StyledProperty<int> ZIndexProperty =
            AvaloniaProperty.Register<Visual, int>(nameof(ZIndex));

        private Rect _bounds;
        private TransformedBounds? _transformedBounds;
        private IRenderRoot? _visualRoot;
        private IVisual? _visualParent;

        /// <summary>
        /// Initializes static members of the <see cref="Visual"/> class.
        /// </summary>
        static Visual()
        {
            AffectsRender<Visual>(
                BoundsProperty,
                ClipProperty,
                ClipToBoundsProperty,
                IsVisibleProperty,
                OpacityProperty);
            RenderTransformProperty.Changed.Subscribe(RenderTransformChanged);
            ZIndexProperty.Changed.Subscribe(ZIndexChanged);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Visual"/> class.
        /// </summary>
        public Visual()
        {
            // Disable transitions until we're added to the visual tree.
            DisableTransitions();

            var visualChildren = new AvaloniaList<IVisual>();
            visualChildren.ResetBehavior = ResetBehavior.Remove;
            visualChildren.Validate = visual => ValidateVisualChild(visual);
            visualChildren.CollectionChanged += VisualChildrenChanged;
            VisualChildren = visualChildren;
        }

        /// <summary>
        /// Raised when the control is attached to a rooted visual tree.
        /// </summary>
        public event EventHandler<VisualTreeAttachmentEventArgs>? AttachedToVisualTree;

        /// <summary>
        /// Raised when the control is detached from a rooted visual tree.
        /// </summary>
        public event EventHandler<VisualTreeAttachmentEventArgs>? DetachedFromVisualTree;

        /// <summary>
        /// Gets the bounds of the control relative to its parent.
        /// </summary>
        public Rect Bounds
        {
            get { return _bounds; }
            protected set { SetAndRaise(BoundsProperty, ref _bounds, value); }
        }

        /// <summary>
        /// Gets the bounds of the control relative to the window, accounting for rendering transforms.
        /// </summary>
        public TransformedBounds? TransformedBounds => _transformedBounds;

        /// <summary>
        /// Gets or sets a value indicating whether the control should be clipped to its bounds.
        /// </summary>
        public bool ClipToBounds
        {
            get { return GetValue(ClipToBoundsProperty); }
            set { SetValue(ClipToBoundsProperty, value); }
        }

        /// <summary>
        /// Gets or sets the geometry clip for this visual.
        /// </summary>
        public Geometry? Clip
        {
            get { return GetValue(ClipProperty); }
            set { SetValue(ClipProperty, value); }
        }

        /// <summary>
        /// Gets a value indicating whether this control and all its parents are visible.
        /// </summary>
        public bool IsEffectivelyVisible
        {
            get
            {
                IVisual? node = this;

                while (node != null)
                {
                    if (!node.IsVisible)
                    {
                        return false;
                    }

                    node = node.VisualParent;
                }

                return true;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether this control is visible.
        /// </summary>
        public bool IsVisible
        {
            get { return GetValue(IsVisibleProperty); }
            set { SetValue(IsVisibleProperty, value); }
        }

        /// <summary>
        /// Gets or sets the opacity of the control.
        /// </summary>
        public double Opacity
        {
            get { return GetValue(OpacityProperty); }
            set { SetValue(OpacityProperty, value); }
        }

        /// <summary>
        /// Gets or sets the opacity mask of the control.
        /// </summary>
        public IBrush? OpacityMask
        {
            get { return GetValue(OpacityMaskProperty); }
            set { SetValue(OpacityMaskProperty, value); }
        }

        /// <summary>
        /// Gets or sets the render transform of the control.
        /// </summary>
        public ITransform? RenderTransform
        {
            get { return GetValue(RenderTransformProperty); }
            set { SetValue(RenderTransformProperty, value); }
        }

        /// <summary>
        /// Gets or sets the transform origin of the control.
        /// </summary>
        public RelativePoint RenderTransformOrigin
        {
            get { return GetValue(RenderTransformOriginProperty); }
            set { SetValue(RenderTransformOriginProperty, value); }
        }

        /// <summary>
        /// Gets or sets the Z index of the control.
        /// </summary>
        /// <remarks>
        /// Controls with a higher <see cref="ZIndex"/> will appear in front of controls with
        /// a lower ZIndex. If two controls have the same ZIndex then the control that appears
        /// later in the containing element's children collection will appear on top.
        /// </remarks>
        public int ZIndex
        {
            get { return GetValue(ZIndexProperty); }
            set { SetValue(ZIndexProperty, value); }
        }

        /// <summary>
        /// Gets the control's child visuals.
        /// </summary>
        protected IAvaloniaList<IVisual> VisualChildren
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the root of the visual tree, if the control is attached to a visual tree.
        /// </summary>
        protected IRenderRoot? VisualRoot => _visualRoot ?? (this as IRenderRoot);

        /// <summary>
        /// Gets a value indicating whether this control is attached to a visual root.
        /// </summary>
        bool IVisual.IsAttachedToVisualTree => VisualRoot != null;

        /// <summary>
        /// Gets the control's child controls.
        /// </summary>
        IAvaloniaReadOnlyList<IVisual> IVisual.VisualChildren => VisualChildren;

        /// <summary>
        /// Gets the control's parent visual.
        /// </summary>
        IVisual? IVisual.VisualParent => _visualParent;

        /// <summary>
        /// Gets the root of the visual tree, if the control is attached to a visual tree.
        /// </summary>
        IRenderRoot? IVisual.VisualRoot => VisualRoot;
        
        TransformedBounds? IVisual.TransformedBounds
        {
            get { return _transformedBounds; }
            set { SetAndRaise(TransformedBoundsProperty, ref _transformedBounds, value); }
        }

        /// <summary>
        /// Invalidates the visual and queues a repaint.
        /// </summary>
        public void InvalidateVisual()
        {
            VisualRoot?.Renderer?.AddDirty(this);
        }

        /// <summary>
        /// Renders the visual to a <see cref="DrawingContext"/>.
        /// </summary>
        /// <param name="context">The drawing context.</param>
        public virtual void Render(DrawingContext context)
        {
            Contract.Requires<ArgumentNullException>(context != null);
        }

        /// <summary>
        /// Indicates that a property change should cause <see cref="InvalidateVisual"/> to be
        /// called.
        /// </summary>
        /// <param name="properties">The properties.</param>
        /// <remarks>
        /// This method should be called in a control's static constructor with each property
        /// on the control which when changed should cause a redraw. This is similar to WPF's
        /// FrameworkPropertyMetadata.AffectsRender flag.
        /// </remarks>
        [Obsolete("Use AffectsRender<T> and specify the control type.")]
        protected static void AffectsRender(params AvaloniaProperty[] properties)
        {
            AffectsRender<Visual>(properties);
        }

        /// <summary>
        /// Indicates that a property change should cause <see cref="InvalidateVisual"/> to be
        /// called.
        /// </summary>
        /// <typeparam name="T">The control which the property affects.</typeparam>
        /// <param name="properties">The properties.</param>
        /// <remarks>
        /// This method should be called in a control's static constructor with each property
        /// on the control which when changed should cause a redraw. This is similar to WPF's
        /// FrameworkPropertyMetadata.AffectsRender flag.
        /// </remarks>
        protected static void AffectsRender<T>(params AvaloniaProperty[] properties)
            where T : Visual
        {
            static void Invalidate(AvaloniaPropertyChangedEventArgs e)
            {
                if (e.Sender is T sender)
                {
                    sender.InvalidateVisual();
                }
            }

            static void InvalidateAndSubscribe(AvaloniaPropertyChangedEventArgs e)
            {
                if (e.Sender is T sender)
                {
                    if (e.OldValue is IAffectsRender oldValue)
                    {
                        WeakEventHandlerManager.Unsubscribe<EventArgs, T>(oldValue, nameof(oldValue.Invalidated), sender.AffectsRenderInvalidated);
                    }

                    if (e.NewValue is IAffectsRender newValue)
                    {
                        WeakEventHandlerManager.Subscribe<IAffectsRender, EventArgs, T>(newValue, nameof(newValue.Invalidated), sender.AffectsRenderInvalidated);
                    }

                    sender.InvalidateVisual();
                }
            }

            foreach (var property in properties)
            {
                if (property.CanValueAffectRender())
                {
                    property.Changed.Subscribe(e => InvalidateAndSubscribe(e));
                }
                else
                {
                    property.Changed.Subscribe(e => Invalidate(e));
                }
            }
        }

        protected override void LogicalChildrenCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            base.LogicalChildrenCollectionChanged(sender, e);
            VisualRoot?.Renderer?.RecalculateChildren(this);
        }

        /// <summary>
        /// Calls the <see cref="OnAttachedToVisualTree(VisualTreeAttachmentEventArgs)"/> method 
        /// for this control and all of its visual descendants.
        /// </summary>
        /// <param name="e">The event args.</param>
        protected virtual void OnAttachedToVisualTreeCore(VisualTreeAttachmentEventArgs e)
        {
            Logger.TryGet(LogEventLevel.Verbose, LogArea.Visual)?.Log(this, "Attached to visual tree");

            _visualRoot = e.Root;

            if (RenderTransform is IMutableTransform mutableTransform)
            {
                mutableTransform.Changed += RenderTransformChanged;
            }

            EnableTransitions();
            OnAttachedToVisualTree(e);
            AttachedToVisualTree?.Invoke(this, e);
            InvalidateVisual();

            var visualChildren = VisualChildren;

            if (visualChildren != null)
            {
                var visualChildrenCount = visualChildren.Count;

                for (var i = 0; i < visualChildrenCount; i++)
                {
                    if (visualChildren[i] is Visual child)
                    {
                        child.OnAttachedToVisualTreeCore(e);
                    }
                }
            }
        }

        /// <summary>
        /// Calls the <see cref="OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs)"/> method 
        /// for this control and all of its visual descendants.
        /// </summary>
        /// <param name="e">The event args.</param>
        protected virtual void OnDetachedFromVisualTreeCore(VisualTreeAttachmentEventArgs e)
        {
            Logger.TryGet(LogEventLevel.Verbose, LogArea.Visual)?.Log(this, "Detached from visual tree");

            _visualRoot = null;

            if (RenderTransform is IMutableTransform mutableTransform)
            {
                mutableTransform.Changed -= RenderTransformChanged;
            }

            DisableTransitions();
            OnDetachedFromVisualTree(e);
            DetachedFromVisualTree?.Invoke(this, e);
            e.Root?.Renderer?.AddDirty(this);

            var visualChildren = VisualChildren;

            if (visualChildren != null)
            {
                var visualChildrenCount = visualChildren.Count;

                for (var i = 0; i < visualChildrenCount; i++)
                {
                    if (visualChildren[i] is Visual child)
                    {
                        child.OnDetachedFromVisualTreeCore(e);
                    }
                }
            }
        }

        /// <summary>
        /// Called when the control is added to a rooted visual tree.
        /// </summary>
        /// <param name="e">The event args.</param>
        protected virtual void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
        }

        /// <summary>
        /// Called when the control is removed from a rooted visual tree.
        /// </summary>
        /// <param name="e">The event args.</param>
        protected virtual void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
        {
        }

        /// <summary>
        /// Called when the control's visual parent changes.
        /// </summary>
        /// <param name="oldParent">The old visual parent.</param>
        /// <param name="newParent">The new visual parent.</param>
        protected virtual void OnVisualParentChanged(IVisual? oldParent, IVisual? newParent)
        {
            RaisePropertyChanged(
                VisualParentProperty,
                new Optional<IVisual?>(oldParent),
                new BindingValue<IVisual?>(newParent),
                BindingPriority.LocalValue);
        }

        protected internal sealed override void LogBindingError(AvaloniaProperty property, Exception e)
        {
            // Don't log a binding error unless the control is attached to a logical or visual tree.
            // In theory this should only need to check for logical tree attachment, but in practise
            // due to ContentControlMixin only taking effect when the template has finished being
            // applied, some controls are attached to the visual tree before the logical tree.
            if (((ILogical)this).IsAttachedToLogicalTree || ((IVisual)this).IsAttachedToVisualTree)
            {
                if (e is BindingChainException b &&
                    string.IsNullOrEmpty(b.ExpressionErrorPoint) &&
                    DataContext == null)
                {
                    // The error occurred at the root of the binding chain and DataContext is null;
                    // don't log this - the DataContext probably hasn't been set up yet.
                    return;
                }

                Logger.TryGet(LogEventLevel.Warning, LogArea.Binding)?.Log(
                    this,
                    "Error in binding to {Target}.{Property}: {Message}",
                    this,
                    property,
                    e.Message);
            }
        }

        /// <summary>
        /// Called when a visual's <see cref="RenderTransform"/> changes.
        /// </summary>
        /// <param name="e">The event args.</param>
        private static void RenderTransformChanged(AvaloniaPropertyChangedEventArgs e)
        {
            var sender = e.Sender as Visual;

            if (sender?.VisualRoot != null)
            {
                var oldValue = e.OldValue as Transform;
                var newValue = e.NewValue as Transform;

                if (oldValue != null)
                {
                    oldValue.Changed -= sender.RenderTransformChanged;
                }

                if (newValue != null)
                {
                    newValue.Changed += sender.RenderTransformChanged;
                }

                sender.InvalidateVisual();
            }
        }

        /// <summary>
        /// Ensures a visual child is not null and not already parented.
        /// </summary>
        /// <param name="c">The visual child.</param>
        private static void ValidateVisualChild(IVisual c)
        {
            if (c == null)
            {
                throw new ArgumentNullException(nameof(c), "Cannot add null to VisualChildren.");
            }

            if (c.VisualParent != null)
            {
                throw new InvalidOperationException("The control already has a visual parent.");
            }
        }

        /// <summary>
        /// Called when the <see cref="ZIndex"/> property changes on any control.
        /// </summary>
        /// <param name="e">The event args.</param>
        private static void ZIndexChanged(AvaloniaPropertyChangedEventArgs e)
        {
            var sender = e.Sender as IVisual;
            var parent = sender?.VisualParent;
            sender?.InvalidateVisual();
            parent?.VisualRoot?.Renderer?.RecalculateChildren(parent);
        }

        /// <summary>
        /// Called when the <see cref="RenderTransform"/>'s <see cref="Transform.Changed"/> event
        /// is fired.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The event args.</param>
        private void RenderTransformChanged(object sender, EventArgs e)
        {
            InvalidateVisual();
        }

        /// <summary>
        /// Sets the visual parent of the Visual.
        /// </summary>
        /// <param name="value">The visual parent.</param>
        private void SetVisualParent(Visual? value)
        {
            if (_visualParent == value)
            {
                return;
            }

            var old = _visualParent;
            _visualParent = value;

            if (_visualRoot != null)
            {
                var e = new VisualTreeAttachmentEventArgs(old, VisualRoot);
                OnDetachedFromVisualTreeCore(e);
            }

            if (_visualParent is IRenderRoot || _visualParent?.IsAttachedToVisualTree == true)
            {
                var root = this.FindAncestorOfType<IRenderRoot>();
                var e = new VisualTreeAttachmentEventArgs(_visualParent, root);
                OnAttachedToVisualTreeCore(e);
            }

            OnVisualParentChanged(old, value);
        }

        private void AffectsRenderInvalidated(object sender, EventArgs e) => InvalidateVisual();

        /// <summary>
        /// Called when the <see cref="VisualChildren"/> collection changes.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The event args.</param>
        private void VisualChildrenChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    SetVisualParent(e.NewItems, this);
                    break;

                case NotifyCollectionChangedAction.Remove:
                    SetVisualParent(e.OldItems, null);
                    break;

                case NotifyCollectionChangedAction.Replace:
                    SetVisualParent(e.OldItems, null);
                    SetVisualParent(e.NewItems, this);
                    break;
            }
        }
        
        private static void SetVisualParent(IList children, Visual? parent)
        {
            var count = children.Count;

            for (var i = 0; i < count; i++)
            {
                var visual = (Visual) children[i];
                
                visual.SetVisualParent(parent);
            }
        }

        protected override bool ApplyStylingOverride()
        {
            // ApplyStyling only when is not already applied and Visual is visble.
            if (IsAppliedStyling == false && IsEffectivelyVisible == true)
            {
                return base.ApplyStylingOverride();
            }
            return true;
        }

        protected override void OnPropertyChanged<T>(AvaloniaPropertyChangedEventArgs<T> change)
        {
            base.OnPropertyChanged(change);
            if (change.Property == IsVisibleProperty)
            {
                // When visibulity changed try apply Styling.
                ApplyStyling();
            }
        }
    }
}
