using System;
using System.Collections.Generic;
using System.ComponentModel;
using Avalonia.Controls.Generators;
using Avalonia.Controls.Platform;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Primitives.PopupPositioning;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Styling;

#nullable enable

namespace Avalonia.Controls
{
    /// <summary>
    /// A control context menu.
    /// </summary>
    public class ContextMenu : MenuBase, ISetterValue
    {
        /// <summary>
        /// Defines the <see cref="HorizontalOffset"/> property.
        /// </summary>
        public static readonly StyledProperty<double> HorizontalOffsetProperty =
            Popup.HorizontalOffsetProperty.AddOwner<ContextMenu>();

        /// <summary>
        /// Defines the <see cref="VerticalOffset"/> property.
        /// </summary>
        public static readonly StyledProperty<double> VerticalOffsetProperty =
            Popup.VerticalOffsetProperty.AddOwner<ContextMenu>();

        /// <summary>
        /// Defines the <see cref="PlacementAnchor"/> property.
        /// </summary>
        public static readonly StyledProperty<PopupAnchor> PlacementAnchorProperty =
            Popup.PlacementAnchorProperty.AddOwner<ContextMenu>();

        /// <summary>
        /// Defines the <see cref="PlacementConstraintAdjustment"/> property.
        /// </summary>
        public static readonly StyledProperty<PopupPositionerConstraintAdjustment> PlacementConstraintAdjustmentProperty =
            Popup.PlacementConstraintAdjustmentProperty.AddOwner<ContextMenu>();

        /// <summary>
        /// Defines the <see cref="PlacementGravity"/> property.
        /// </summary>
        public static readonly StyledProperty<PopupGravity> PlacementGravityProperty =
            Popup.PlacementGravityProperty.AddOwner<ContextMenu>();

        /// <summary>
        /// Defines the <see cref="PlacementMode"/> property.
        /// </summary>
        public static readonly StyledProperty<PlacementMode> PlacementModeProperty =
            Popup.PlacementModeProperty.AddOwner<ContextMenu>();

        /// <summary>
        /// Defines the <see cref="PlacementRect"/> property.
        /// </summary>
        public static readonly StyledProperty<Rect?> PlacementRectProperty =
            AvaloniaProperty.Register<Popup, Rect?>(nameof(PlacementRect));

        /// <summary>
        /// Defines the <see cref="WindowManagerAddShadowHint"/> property.
        /// </summary>
        public static readonly StyledProperty<bool> WindowManagerAddShadowHintProperty  =
            Popup.WindowManagerAddShadowHintProperty.AddOwner<ContextMenu>();

        /// <summary>
        /// Defines the <see cref="PlacementTarget"/> property.
        /// </summary>
        public static readonly StyledProperty<Control?> PlacementTargetProperty =
            Popup.PlacementTargetProperty.AddOwner<ContextMenu>();

        private static readonly ITemplate<IPanel> DefaultPanel =
            new FuncTemplate<IPanel>(() => new StackPanel { Orientation = Orientation.Vertical });
        private Popup? _popup;
        private List<Control>? _attachedControls;
        private IInputElement? _previousFocus;

        /// <summary>
        /// Initializes a new instance of the <see cref="ContextMenu"/> class.
        /// </summary>
        public ContextMenu()
            : this(new DefaultMenuInteractionHandler(true))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ContextMenu"/> class.
        /// </summary>
        /// <param name="interactionHandler">The menu interaction handler.</param>
        public ContextMenu(IMenuInteractionHandler interactionHandler)
            : base(interactionHandler)
        {
        }

        /// <summary>
        /// Initializes static members of the <see cref="ContextMenu"/> class.
        /// </summary>
        static ContextMenu()
        {
            PlacementModeProperty.OverrideDefaultValue<ContextMenu>(PlacementMode.Pointer);
            ContextMenuProperty.Changed.Subscribe(ContextMenuChanged);
        }

        /// <summary>
        /// Gets or sets the Horizontal offset of the context menu in relation to the <see cref="PlacementTarget"/>.
        /// </summary>
        public double HorizontalOffset
        {
            get { return GetValue(HorizontalOffsetProperty); }
            set { SetValue(HorizontalOffsetProperty, value); }
        }

        /// <summary>
        /// Gets or sets the Vertical offset of the context menu in relation to the <see cref="PlacementTarget"/>.
        /// </summary>
        public double VerticalOffset
        {
            get { return GetValue(VerticalOffsetProperty); }
            set { SetValue(VerticalOffsetProperty, value); }
        }

        /// <summary>
        /// Gets or sets the anchor point on the <see cref="PlacementRect"/> when <see cref="PlacementMode"/>
        /// is <see cref="PlacementMode.AnchorAndGravity"/>.
        /// </summary>
        public PopupAnchor PlacementAnchor
        {
            get { return GetValue(PlacementAnchorProperty); }
            set { SetValue(PlacementAnchorProperty, value); }
        }

        /// <summary>
        /// Gets or sets a value describing how the context menu position will be adjusted if the
        /// unadjusted position would result in the context menu being partly constrained.
        /// </summary>
        public PopupPositionerConstraintAdjustment PlacementConstraintAdjustment
        {
            get { return GetValue(PlacementConstraintAdjustmentProperty); }
            set { SetValue(PlacementConstraintAdjustmentProperty, value); }
        }

        /// <summary>
        /// Gets or sets a value which defines in what direction the context menu should open
        /// when <see cref="PlacementMode"/> is <see cref="PlacementMode.AnchorAndGravity"/>.
        /// </summary>
        public PopupGravity PlacementGravity
        {
            get { return GetValue(PlacementGravityProperty); }
            set { SetValue(PlacementGravityProperty, value); }
        }

        /// <summary>
        /// Gets or sets the placement mode of the context menu in relation to the<see cref="PlacementTarget"/>.
        /// </summary>
        public PlacementMode PlacementMode
        {
            get { return GetValue(PlacementModeProperty); }
            set { SetValue(PlacementModeProperty, value); }
        }

        public bool WindowManagerAddShadowHint
        {
            get { return GetValue(WindowManagerAddShadowHintProperty); }
            set { SetValue(WindowManagerAddShadowHintProperty, value); }
        }

        /// <summary>
        /// Gets or sets the the anchor rectangle within the parent that the context menu will be placed
        /// relative to when <see cref="PlacementMode"/> is <see cref="PlacementMode.AnchorAndGravity"/>.
        /// </summary>
        /// <remarks>
        /// The placement rect defines a rectangle relative to <see cref="PlacementTarget"/> around
        /// which the popup will be opened, with <see cref="PlacementAnchor"/> determining which edge
        /// of the placement target is used.
        /// 
        /// If unset, the anchor rectangle will be the bounds of the <see cref="PlacementTarget"/>.
        /// </remarks>
        public Rect? PlacementRect
        {
            get { return GetValue(PlacementRectProperty); }
            set { SetValue(PlacementRectProperty, value); }
        }

        /// <summary>
        /// Gets or sets the control that is used to determine the popup's position.
        /// </summary>
        public Control? PlacementTarget
        {
            get { return GetValue(PlacementTargetProperty); }
            set { SetValue(PlacementTargetProperty, value); }
        }

        /// <summary>
        /// Occurs when the value of the
        /// <see cref="P:Avalonia.Controls.ContextMenu.IsOpen" />
        /// property is changing from false to true.
        /// </summary>
        public event CancelEventHandler? ContextMenuOpening;

        /// <summary>
        /// Occurs when the value of the
        /// <see cref="P:Avalonia.Controls.ContextMenu.IsOpen" />
        /// property is changing from true to false.
        /// </summary>
        public event CancelEventHandler? ContextMenuClosing;

        /// <summary>
        /// Called when the <see cref="Control.ContextMenu"/> property changes on a control.
        /// </summary>
        /// <param name="e">The event args.</param>
        private static void ContextMenuChanged(AvaloniaPropertyChangedEventArgs e)
        {
            var control = (Control)e.Sender;

            if (e.OldValue is ContextMenu oldMenu)
            {
                control.PointerReleased -= ControlPointerReleased;
                oldMenu._attachedControls?.Remove(control);
                ((ISetLogicalParent?)oldMenu._popup)?.SetParent(null);
            }

            if (e.NewValue is ContextMenu newMenu)
            {
                newMenu._attachedControls ??= new List<Control>();
                newMenu._attachedControls.Add(control);
                control.PointerReleased += ControlPointerReleased;
            }
        }

        protected override void OnPropertyChanged<T>(AvaloniaPropertyChangedEventArgs<T> change)
        {
            base.OnPropertyChanged(change);

            if (change.Property == WindowManagerAddShadowHintProperty && _popup != null)
            {
                _popup.WindowManagerAddShadowHint = change.NewValue.GetValueOrDefault<bool>();
            }
        }

        /// <summary>
        /// Opens the menu.
        /// </summary>
        public override void Open() => Open(null);

        /// <summary>
        /// Opens a context menu on the specified control.
        /// </summary>
        /// <param name="control">The control.</param>
        public void Open(Control? control)
        {
            if (control is null && (_attachedControls is null || _attachedControls.Count == 0))
            {
                throw new ArgumentNullException(nameof(control));
            }

            if (control is object &&
                _attachedControls is object &&
                !_attachedControls.Contains(control))
            {
                throw new ArgumentException(
                    "Cannot show ContentMenu on a different control to the one it is attached to.",
                    nameof(control));
            }

            control ??= _attachedControls![0];

            if (IsOpen)
            {
                return;
            }

            if (_popup == null)
            {
                _popup = new Popup
                {
                    HorizontalOffset = HorizontalOffset,
                    VerticalOffset = VerticalOffset,
                    PlacementAnchor = PlacementAnchor,
                    PlacementConstraintAdjustment = PlacementConstraintAdjustment,
                    PlacementGravity = PlacementGravity,
                    PlacementMode = PlacementMode,
                    PlacementRect = PlacementRect,
                    PlacementTarget = PlacementTarget ?? control,
                    IsLightDismissEnabled = true,
                    OverlayDismissEventPassThrough = true,
                    WindowManagerAddShadowHint = WindowManagerAddShadowHint,
                };

                _popup.Opened += PopupOpened;
                _popup.Closed += PopupClosed;
            }

            if (_popup.Parent != control)
            {
                ((ISetLogicalParent)_popup).SetParent(null);
                ((ISetLogicalParent)_popup).SetParent(control);
            }

            if (PlacementTarget is null && _popup.PlacementTarget != control)
            {
                _popup.PlacementTarget = control;
            }

            _popup.Child = this;
            IsOpen = true;
            _popup.IsOpen = true;

            RaiseEvent(new RoutedEventArgs
            {
                RoutedEvent = MenuOpenedEvent,
                Source = this,
            });
        }

        /// <summary>
        /// Closes the menu.
        /// </summary>
        public override void Close()
        {
            if (!IsOpen)
            {
                return;
            }

            if (_popup != null && _popup.IsVisible)
            {
                _popup.IsOpen = false;
            }
        }

        void ISetterValue.Initialize(ISetter setter)
        {
            // ContextMenu can be assigned to the ContextMenu property in a setter. This overrides
            // the behavior defined in Control which requires controls to be wrapped in a <template>.
            if (!(setter is Setter s && s.Property == ContextMenuProperty))
            {
                throw new InvalidOperationException(
                    "Cannot use a control as a Setter value. Wrap the control in a <Template>.");
            }
        }

        protected override IItemContainerGenerator CreateItemContainerGenerator()
        {
            return new MenuItemContainerGenerator(this);
        }

        private void PopupOpened(object sender, EventArgs e)
        {
            _previousFocus = FocusManager.Instance?.Current;
            Focus();
        }

        private void PopupClosed(object sender, EventArgs e)
        {
            foreach (var i in LogicalChildren)
            {
                if (i is MenuItem menuItem)
                {
                    menuItem.IsSubMenuOpen = false;
                }
            }

            SelectedIndex = -1;
            IsOpen = false;

            if (_attachedControls is null || _attachedControls.Count == 0)
            {
                ((ISetLogicalParent)_popup!).SetParent(null);
            }

            // HACK: Reset the focus when the popup is closed. We need to fix this so it's automatic.
            FocusManager.Instance?.Focus(_previousFocus);

            RaiseEvent(new RoutedEventArgs
            {
                RoutedEvent = MenuClosedEvent,
                Source = this,
            });
        }

        private static void ControlPointerReleased(object sender, PointerReleasedEventArgs e)
        {
            var control = (Control)sender;
            var contextMenu = control.ContextMenu;

            if (control.ContextMenu.IsOpen)
            {
                if (contextMenu.CancelClosing())
                    return;

                control.ContextMenu.Close();
                e.Handled = true;
            }

            if (e.InitialPressMouseButton == MouseButton.Right)
            {
                if (contextMenu.CancelOpening())
                    return;

                contextMenu.Open(control);
                e.Handled = true;
            }
        }

        private bool CancelClosing()
        {
            var eventArgs = new CancelEventArgs();
            ContextMenuClosing?.Invoke(this, eventArgs);
            return eventArgs.Cancel;
        }

        private bool CancelOpening()
        {
            var eventArgs = new CancelEventArgs();
            ContextMenuOpening?.Invoke(this, eventArgs);
            return eventArgs.Cancel;
        }
    }
}
