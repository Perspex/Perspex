using System;
using System.Collections.Generic;
using System.Reactive.Disposables;
using Avalonia.Controls.Primitives.PopupPositioning;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Styling;
using Avalonia.VisualTree;
using JetBrains.Annotations;

namespace Avalonia.Controls.Primitives
{
    /// <summary>
    /// The root window of a <see cref="Popup"/>.
    /// </summary>
    public sealed class PopupRoot : WindowBase, IInteractive, IHostedVisualTreeRoot, IDisposable, IStyleHost, IPopupHost
    {
        private readonly TopLevel _parent;
        private PopupPositionerParameters _positionerParameters;        

        /// <summary>
        /// Initializes static members of the <see cref="PopupRoot"/> class.
        /// </summary>
        static PopupRoot()
        {
            BackgroundProperty.OverrideDefaultValue(typeof(PopupRoot), Brushes.White);            
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PopupRoot"/> class.
        /// </summary>
        public PopupRoot(TopLevel parent, IPopupImpl impl)
            : this(parent, impl,null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PopupRoot"/> class.
        /// </summary>
        /// <param name="parent">The popup parent.</param>
        /// <param name="impl">The popup implementation.</param>
        /// <param name="dependencyResolver">
        /// The dependency resolver to use. If null the default dependency resolver will be used.
        /// </param>
        public PopupRoot(TopLevel parent, IPopupImpl impl, IAvaloniaDependencyResolver dependencyResolver)
            : base(impl, dependencyResolver)
        {
            _parent = parent;
        }

        /// <summary>
        /// Gets the platform-specific window implementation.
        /// </summary>
        [CanBeNull]
        public new IPopupImpl PlatformImpl => (IPopupImpl)base.PlatformImpl;               

        /// <summary>
        /// Gets the parent control in the event route.
        /// </summary>
        /// <remarks>
        /// Popup events are passed to their parent window. This facilitates this.
        /// </remarks>
        IInteractive IInteractive.InteractiveParent => Parent;

        /// <summary>
        /// Gets the control that is hosting the popup root.
        /// </summary>
        IVisual IHostedVisualTreeRoot.Host => Parent;

        /// <summary>
        /// Gets the styling parent of the popup root.
        /// </summary>
        IStyleHost IStyleHost.StylingParent => Parent;

        /// <inheritdoc/>
        public void Dispose() => PlatformImpl?.Dispose();

        private void UpdatePosition()
        {
            PlatformImpl?.PopupPositioner.Update(_positionerParameters);
        }

        public void ConfigurePosition(IVisual target, PlacementMode placement, Point offset,
            PopupAnchor anchor = PopupAnchor.None,
            PopupGravity gravity = PopupGravity.None,
            PopupPositionerConstraintAdjustment constraintAdjustment = PopupPositionerConstraintAdjustment.All,
            Rect? rect = null)
        {
            _positionerParameters.ConfigurePosition(_parent, target,
                placement, offset, anchor, gravity, constraintAdjustment, rect);

            if (_positionerParameters.Size != default)
                UpdatePosition();
        }

        public void SetChild(IControl control) => Content = control;

        IVisual IPopupHost.HostedVisualTreeRoot => this;
        
        public IDisposable BindConstraints(AvaloniaObject popup, StyledProperty<double> widthProperty, StyledProperty<double> minWidthProperty,
            StyledProperty<double> maxWidthProperty, StyledProperty<double> heightProperty, StyledProperty<double> minHeightProperty,
            StyledProperty<double> maxHeightProperty, StyledProperty<bool> topmostProperty)
        {
            var bindings = new List<IDisposable>();

            void Bind(AvaloniaProperty what, AvaloniaProperty to) => bindings.Add(this.Bind(what, popup[~to]));
            Bind(WidthProperty, widthProperty);
            Bind(MinWidthProperty, minWidthProperty);
            Bind(MaxWidthProperty, maxWidthProperty);
            Bind(HeightProperty, heightProperty);
            Bind(MinHeightProperty, minHeightProperty);
            Bind(MaxHeightProperty, maxHeightProperty);
            Bind(TopmostProperty, topmostProperty);
            return Disposable.Create(() =>
            {
                foreach (var x in bindings)
                    x.Dispose();
            });
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            var maxAutoSize = PlatformImpl?.MaxAutoSizeHint ?? Size.Infinity;
            var constraint = availableSize;

            if (double.IsInfinity(constraint.Width))
            {
                constraint = constraint.WithWidth(maxAutoSize.Width);
            }

            if (double.IsInfinity(constraint.Height))
            {
                constraint = constraint.WithHeight(maxAutoSize.Height);
            }

            var measured = base.MeasureOverride(constraint);
            var width = measured.Width;
            var height = measured.Height;
            var widthCache = Width;
            var heightCache = Height;

            if (!double.IsNaN(widthCache))
            {
                width = widthCache;
            }

            width = Math.Min(width, MaxWidth);
            width = Math.Max(width, MinWidth);

            if (!double.IsNaN(heightCache))
            {
                height = heightCache;
            }

            height = Math.Min(height, MaxHeight);
            height = Math.Max(height, MinHeight);

            return new Size(width, height);
        }

        protected override sealed Size ArrangeSetBounds(Size size)
        {
            _positionerParameters.Size = size;
            UpdatePosition();
            return ClientSize;
        }
    }
}
