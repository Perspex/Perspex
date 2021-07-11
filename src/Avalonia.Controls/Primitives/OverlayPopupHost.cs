using System;
using System.Collections.Generic;
using System.Reactive.Disposables;
using Avalonia.Controls.Primitives.PopupPositioning;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace Avalonia.Controls.Primitives
{
    public class OverlayPopupHost : ContentControl, IPopupHost, IInteractive, IManagedPopupPositionerPopup
    {
        private readonly OverlayLayer _overlayLayer;
        private PopupPositionerParameters _positionerParameters = new PopupPositionerParameters();
        private ManagedPopupPositioner _positioner;
        private Point _lastRequestedPosition;
        private bool _shown;

        public OverlayPopupHost(OverlayLayer overlayLayer)
        {
            _overlayLayer = overlayLayer;
            _positioner = new ManagedPopupPositioner(this);
        }

        public void SetChild(IControl control)
        {
            Content = control;
        }

        public IVisual HostedVisualTreeRoot => null;
        
        /// <inheritdoc/>
        IInteractive IInteractive.InteractiveParent => Parent;

        public void Dispose() => Hide();


        public void Show()
        {
            _overlayLayer.Children.Add(this);
            _shown = true;
        }

        public void Hide()
        {
            _overlayLayer.Children.Remove(this);
            _shown = false;
        }

        public IDisposable BindConstraints(AvaloniaObject popup, StyledProperty<double> widthProperty, StyledProperty<double> minWidthProperty,
            StyledProperty<double> maxWidthProperty, StyledProperty<double> heightProperty, StyledProperty<double> minHeightProperty,
            StyledProperty<double> maxHeightProperty, StyledProperty<bool> topmostProperty)
        {
            // Topmost property is not supported
            var bindings = new List<IDisposable>();

            void Bind(AvaloniaProperty what, AvaloniaProperty to) => bindings.Add(this.Bind(what, popup[~to]));
            Bind(WidthProperty, widthProperty);
            Bind(MinWidthProperty, minWidthProperty);
            Bind(MaxWidthProperty, maxWidthProperty);
            Bind(HeightProperty, heightProperty);
            Bind(MinHeightProperty, minHeightProperty);
            Bind(MaxHeightProperty, maxHeightProperty);
            
            return Disposable.Create(() =>
            {
                foreach (var x in bindings)
                    x.Dispose();
            });
        }

        public void ConfigurePosition(IVisual target, PlacementMode placement, Point offset,
            PopupAnchor anchor = PopupAnchor.None, PopupGravity gravity = PopupGravity.None,
            PopupPositionerConstraintAdjustment constraintAdjustment = PopupPositionerConstraintAdjustment.All,
            Rect? rect = null)
        {
            _positionerParameters.ConfigurePosition((TopLevel)_overlayLayer.GetVisualRoot(), target, placement, offset, anchor,
                gravity, constraintAdjustment, rect);
            UpdatePosition();
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            if (_positionerParameters.Size != finalSize)
            {
                _positionerParameters.Size = finalSize;
                UpdatePosition();
            }
            return base.ArrangeOverride(finalSize);
        }


        private void UpdatePosition()
        {
            // Don't bother the positioner with layout system artifacts
            if (_positionerParameters.Size.Width == 0 || _positionerParameters.Size.Height == 0)
                return;
            if (_shown)
            {
                _positioner.Update(_positionerParameters);
            }
        }

        IReadOnlyList<ManagedPopupPositionerScreenInfo> IManagedPopupPositionerPopup.Screens
        {
            get
            {
                var rc = new Rect(default, _overlayLayer.AvailableSize);
                return new[] {new ManagedPopupPositionerScreenInfo(rc, rc)};
            }
        }

        Rect IManagedPopupPositionerPopup.ParentClientAreaScreenGeometry =>
            new Rect(default, _overlayLayer.Bounds.Size);

        void IManagedPopupPositionerPopup.MoveAndResize(Point devicePoint, Size virtualSize)
        {
            _lastRequestedPosition = devicePoint;
            Dispatcher.UIThread.Post(() =>
            {
                OverlayLayer.SetLeft(this, _lastRequestedPosition.X);
                OverlayLayer.SetTop(this, _lastRequestedPosition.Y);
            }, DispatcherPriority.Layout);
        }

        double IManagedPopupPositionerPopup.Scaling => 1;

        public static IPopupHost CreatePopupHost(IVisual target, IAvaloniaDependencyResolver dependencyResolver)
        {
            return CreatePopupHost(target, dependencyResolver, true);
        }

        public static IPopupHost CreatePopupHost(IVisual target, IAvaloniaDependencyResolver dependencyResolver, bool shouldConstrainToRootBounds)
        {
            if (!shouldConstrainToRootBounds)
            {
                var platform = (target.GetVisualRoot() as TopLevel)?.PlatformImpl?.CreatePopup();
                if (platform != null)
                    return new PopupRoot((TopLevel)target.GetVisualRoot(), platform, dependencyResolver);
            }

            var overlayLayer = OverlayLayer.GetOverlayLayer(target);
            if (overlayLayer == null)
                throw new InvalidOperationException(
                    "Unable to create IPopupImpl and no overlay layer is found for the target control");


            return new OverlayPopupHost(overlayLayer);
        }

        public override void Render(DrawingContext context)
        {
            context.FillRectangle(Brushes.White, new Rect(default, Bounds.Size));
        }
    }
}
