#nullable enable
using System;
using Avalonia.Controls.Diagnostics;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;

namespace Avalonia.Controls
{
    /// <summary>
    /// A control which pops up a hint when a control is hovered.
    /// </summary>
    /// <remarks>
    /// You will probably not want to create a <see cref="ToolTip"/> control directly: if added to
    /// the tree it will act as a simple <see cref="ContentControl"/> styled to look like a tooltip.
    /// To add a tooltip to a control, use the <see cref="TipProperty"/> attached property,
    /// assigning the content that you want displayed.
    /// </remarks>
    [PseudoClasses(":open")]
    public class ToolTip : ContentControl, IPopupHostProvider
    {
        /// <summary>
        /// Defines the ToolTip.Tip attached property.
        /// </summary>
        public static readonly AttachedProperty<object?> TipProperty =
            AvaloniaProperty.RegisterAttached<ToolTip, Control, object?>("Tip");

        /// <summary>
        /// Defines the ToolTip.IsOpen attached property.
        /// </summary>
        public static readonly AttachedProperty<bool> IsOpenProperty =
            AvaloniaProperty.RegisterAttached<ToolTip, Control, bool>("IsOpen");

        /// <summary>
        /// Defines the ToolTip.Placement property.
        /// </summary>
        public static readonly AttachedProperty<PlacementMode> PlacementProperty =
            AvaloniaProperty.RegisterAttached<ToolTip, Control, PlacementMode>("Placement", defaultValue: PlacementMode.Pointer);

        /// <summary>
        /// Defines the ToolTip.HorizontalOffset property.
        /// </summary>
        public static readonly AttachedProperty<double> HorizontalOffsetProperty =
            AvaloniaProperty.RegisterAttached<ToolTip, Control, double>("HorizontalOffset");

        /// <summary>
        /// Defines the ToolTip.VerticalOffset property.
        /// </summary>
        public static readonly AttachedProperty<double> VerticalOffsetProperty =
            AvaloniaProperty.RegisterAttached<ToolTip, Control, double>("VerticalOffset", 20);

        /// <summary>
        /// Defines the ToolTip.ShowDelay property.
        /// </summary>
        public static readonly AttachedProperty<int> ShowDelayProperty =
            AvaloniaProperty.RegisterAttached<ToolTip, Control, int>("ShowDelay", 400);

        /// <summary>
        /// Stores the current <see cref="ToolTip"/> instance in the control.
        /// </summary>
        internal static readonly AttachedProperty<ToolTip?> ToolTipProperty =
            AvaloniaProperty.RegisterAttached<ToolTip, Control, ToolTip?>("ToolTip");

        private IPopupHost? _popupHost;
        private Action<IPopupHost?>? _popupHostChangedHandler;

        /// <summary>
        /// Initializes static members of the <see cref="ToolTip"/> class.
        /// </summary>
        static ToolTip()
        {
            TipProperty.Changed.Subscribe(ToolTipService.Instance.TipChanged);
            IsOpenProperty.Changed.Subscribe(ToolTipService.Instance.TipOpenChanged);
            IsOpenProperty.Changed.Subscribe(IsOpenChanged);

            HorizontalOffsetProperty.Changed.Subscribe(RecalculatePositionOnPropertyChanged);
            VerticalOffsetProperty.Changed.Subscribe(RecalculatePositionOnPropertyChanged);
            PlacementProperty.Changed.Subscribe(RecalculatePositionOnPropertyChanged);
        }

        /// <summary>
        /// Gets the value of the ToolTip.Tip attached property.
        /// </summary>
        /// <param name="element">The control to get the property from.</param>
        /// <returns>
        /// The content to be displayed in the control's tooltip.
        /// </returns>
        public static object? GetTip(Control element)
        {
            return element.GetValue(TipProperty);
        }

        /// <summary>
        /// Sets the value of the ToolTip.Tip attached property.
        /// </summary>
        /// <param name="element">The control to get the property from.</param>
        /// <param name="value">The content to be displayed in the control's tooltip.</param>
        public static void SetTip(Control element, object? value)
        {
            element.SetValue(TipProperty, value);
        }

        /// <summary>
        /// Gets the value of the ToolTip.IsOpen attached property.
        /// </summary>
        /// <param name="element">The control to get the property from.</param>
        /// <returns>
        /// A value indicating whether the tool tip is visible.
        /// </returns>
        public static bool GetIsOpen(Control element)
        {
            return element.GetValue(IsOpenProperty);
        }

        /// <summary>
        /// Sets the value of the ToolTip.IsOpen attached property.
        /// </summary>
        /// <param name="element">The control to get the property from.</param>
        /// <param name="value">A value indicating whether the tool tip is visible.</param>
        public static void SetIsOpen(Control element, bool value)
        {
            element.SetValue(IsOpenProperty, value);
        }

        /// <summary>
        /// Gets the value of the ToolTip.Placement attached property.
        /// </summary>
        /// <param name="element">The control to get the property from.</param>
        /// <returns>
        /// A value indicating how the tool tip is positioned.
        /// </returns>
        public static PlacementMode GetPlacement(Control element)
        {
            return element.GetValue(PlacementProperty);
        }

        /// <summary>
        /// Sets the value of the ToolTip.Placement attached property.
        /// </summary>
        /// <param name="element">The control to get the property from.</param>
        /// <param name="value">A value indicating how the tool tip is positioned.</param>
        public static void SetPlacement(Control element, PlacementMode value)
        {
            element.SetValue(PlacementProperty, value);
        }

        /// <summary>
        /// Gets the value of the ToolTip.HorizontalOffset attached property.
        /// </summary>
        /// <param name="element">The control to get the property from.</param>
        /// <returns>
        /// A value indicating how the tool tip is positioned.
        /// </returns>
        public static double GetHorizontalOffset(Control element)
        {
            return element.GetValue(HorizontalOffsetProperty);
        }

        /// <summary>
        /// Sets the value of the ToolTip.HorizontalOffset attached property.
        /// </summary>
        /// <param name="element">The control to get the property from.</param>
        /// <param name="value">A value indicating how the tool tip is positioned.</param>
        public static void SetHorizontalOffset(Control element, double value)
        {
            element.SetValue(HorizontalOffsetProperty, value);
        }

        /// <summary>
        /// Gets the value of the ToolTip.VerticalOffset attached property.
        /// </summary>
        /// <param name="element">The control to get the property from.</param>
        /// <returns>
        /// A value indicating how the tool tip is positioned.
        /// </returns>
        public static double GetVerticalOffset(Control element)
        {
            return element.GetValue(VerticalOffsetProperty);
        }

        /// <summary>
        /// Sets the value of the ToolTip.VerticalOffset attached property.
        /// </summary>
        /// <param name="element">The control to get the property from.</param>
        /// <param name="value">A value indicating how the tool tip is positioned.</param>
        public static void SetVerticalOffset(Control element, double value)
        {
            element.SetValue(VerticalOffsetProperty, value);
        }

        /// <summary>
        /// Gets the value of the ToolTip.ShowDelay attached property.
        /// </summary>
        /// <param name="element">The control to get the property from.</param>
        /// <returns>
        /// A value indicating the time, in milliseconds, before a tool tip opens.
        /// </returns>
        public static int GetShowDelay(Control element)
        {
            return element.GetValue(ShowDelayProperty);
        }

        /// <summary>
        /// Sets the value of the ToolTip.ShowDelay attached property.
        /// </summary>
        /// <param name="element">The control to get the property from.</param>
        /// <param name="value">A value indicating the time, in milliseconds, before a tool tip opens.</param>
        public static void SetShowDelay(Control element, int value)
        {
            element.SetValue(ShowDelayProperty, value);
        }

        private static void IsOpenChanged(AvaloniaPropertyChangedEventArgs e)
        {
            var control = (Control)e.Sender;
            var newValue = (bool)e.NewValue!;
            ToolTip? toolTip;

            if (newValue)
            {
                var tip = GetTip(control);
                if (tip == null) return;

                toolTip = control.GetValue(ToolTipProperty);
                if (toolTip == null || (tip != toolTip && tip != toolTip.Content))
                {
                    toolTip?.Close();

                    toolTip = tip as ToolTip ?? new ToolTip { Content = tip };
                    control.SetValue(ToolTipProperty, toolTip);
                }

                toolTip.Open(control);
            }
            else
            {
                toolTip = control.GetValue(ToolTipProperty);
                toolTip?.Close();
            }

            toolTip?.UpdatePseudoClasses(newValue);
        }

        private static void RecalculatePositionOnPropertyChanged(AvaloniaPropertyChangedEventArgs args)
        {
            var control = (Control)args.Sender;
            var tooltip = control.GetValue(ToolTipProperty);
            if (tooltip == null)
            {
                return;
            }

            tooltip.RecalculatePosition(control);
        }
        
        IPopupHost? IPopupHostProvider.PopupHost => _popupHost;

        event Action<IPopupHost?>? IPopupHostProvider.PopupHostChanged 
        { 
            add => _popupHostChangedHandler += value; 
            remove => _popupHostChangedHandler -= value;
        }

        internal void RecalculatePosition(Control control)
        {
            _popupHost?.ConfigurePosition(control, GetPlacement(control), new Point(GetHorizontalOffset(control), GetVerticalOffset(control)));
        }

        private void Open(Control control)
        {
            Close();

            _popupHost = OverlayPopupHost.CreatePopupHost(control, null);
            _popupHost.SetChild(this);
            ((ISetLogicalParent)_popupHost).SetParent(control);
            
            _popupHost.ConfigurePosition(control, GetPlacement(control), 
                new Point(GetHorizontalOffset(control), GetVerticalOffset(control)));

            WindowManagerAddShadowHintChanged(_popupHost, false);

            _popupHost.Show();
            _popupHostChangedHandler?.Invoke(_popupHost);
        }

        private void Close()
        {
            if (_popupHost != null)
            {
                _popupHost.SetChild(null);
                _popupHost.Dispose();
                _popupHost = null;
                _popupHostChangedHandler?.Invoke(null);
            }
        }

        private void WindowManagerAddShadowHintChanged(IPopupHost host, bool hint)
        {
            if (host is PopupRoot pr)
            {
                pr.PlatformImpl.SetWindowManagerAddShadowHint(hint);
            }
        }

        private void UpdatePseudoClasses(bool newValue)
        {
            PseudoClasses.Set(":open", newValue);
        }
    }
}
