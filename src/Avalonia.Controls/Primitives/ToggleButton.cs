using System;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Platform;
using Avalonia.Controls.Metadata;
using Avalonia.Data;
using Avalonia.Interactivity;

namespace Avalonia.Controls.Primitives
{
    /// <summary>
    /// Represents a control that a user can select (check) or clear (uncheck). Base class for controls that can switch states.
    /// </summary>
    [PseudoClasses(":checked", ":unchecked", ":indeterminate")]
    public class ToggleButton : Button
    {
        /// <summary>
        /// Defines the <see cref="IsChecked"/> property.
        /// </summary>
        public static readonly DirectProperty<ToggleButton, bool?> IsCheckedProperty =
            AvaloniaProperty.RegisterDirect<ToggleButton, bool?>(
                nameof(IsChecked),
                o => o.IsChecked,
                (o, v) => o.IsChecked = v,
                unsetValue: null,
                defaultBindingMode: BindingMode.TwoWay);

        /// <summary>
        /// Defines the <see cref="IsThreeState"/> property.
        /// </summary>
        public static readonly StyledProperty<bool> IsThreeStateProperty =
            AvaloniaProperty.Register<ToggleButton, bool>(nameof(IsThreeState));

        /// <summary>
        /// Defines the <see cref="Checked"/> event.
        /// </summary>
        public static readonly RoutedEvent<RoutedEventArgs> CheckedEvent =
            RoutedEvent.Register<ToggleButton, RoutedEventArgs>(nameof(Checked), RoutingStrategies.Bubble);

        /// <summary>
        /// Defines the <see cref="Unchecked"/> event.
        /// </summary>
        public static readonly RoutedEvent<RoutedEventArgs> UncheckedEvent =
            RoutedEvent.Register<ToggleButton, RoutedEventArgs>(nameof(Unchecked), RoutingStrategies.Bubble);

        /// <summary>
        /// Defines the <see cref="Unchecked"/> event.
        /// </summary>
        public static readonly RoutedEvent<RoutedEventArgs> IndeterminateEvent =
            RoutedEvent.Register<ToggleButton, RoutedEventArgs>(nameof(Indeterminate), RoutingStrategies.Bubble);

        private bool? _isChecked = false;

        static ToggleButton()
        {
            IsCheckedProperty.Changed.AddClassHandler<ToggleButton>((x, e) => x.OnIsCheckedChanged(e));
        }

        public ToggleButton()
        {
            UpdatePseudoClasses(IsChecked);
        }

        /// <summary>
        /// Raised when a <see cref="ToggleButton"/> is checked.
        /// </summary>
        public event EventHandler<RoutedEventArgs> Checked
        {
            add => AddHandler(CheckedEvent, value);
            remove => RemoveHandler(CheckedEvent, value);
        }

        /// <summary>
        /// Raised when a <see cref="ToggleButton"/> is unchecked.
        /// </summary>
        public event EventHandler<RoutedEventArgs> Unchecked
        {
            add => AddHandler(UncheckedEvent, value);
            remove => RemoveHandler(UncheckedEvent, value);
        }

        /// <summary>
        /// Raised when a <see cref="ToggleButton"/> is neither checked nor unchecked.
        /// </summary>
        public event EventHandler<RoutedEventArgs> Indeterminate
        {
            add => AddHandler(IndeterminateEvent, value);
            remove => RemoveHandler(IndeterminateEvent, value);
        }

        /// <summary>
        /// Gets or sets whether the <see cref="ToggleButton"/> is checked.
        /// </summary>
        public bool? IsChecked
        {
            get => _isChecked;
            set 
            { 
                SetAndRaise(IsCheckedProperty, ref _isChecked, value);
                UpdatePseudoClasses(IsChecked);
            }
        }

        /// <summary>
        /// Gets or sets a value that indicates whether the control supports three states.
        /// </summary>
        public bool IsThreeState
        {
            get => GetValue(IsThreeStateProperty);
            set => SetValue(IsThreeStateProperty, value);
        }

        protected override void OnClick()
        {
            Toggle();
            base.OnClick();
        }

        /// <summary>
        /// Toggles the <see cref="IsChecked"/> property.
        /// </summary>
        protected virtual void Toggle()
        {
            if (IsChecked.HasValue)
            {
                if (IsChecked.Value)
                {
                    if (IsThreeState)
                    {
                        IsChecked = null;
                    }
                    else
                    {
                        IsChecked = false;
                    }
                }
                else
                {
                    IsChecked = true;
                }
            }
            else
            {
                IsChecked = false;
            }
        }

        /// <summary>
        /// Called when <see cref="IsChecked"/> becomes true.
        /// </summary>
        /// <param name="e">Event arguments for the routed event that is raised by the default implementation of this method.</param>
        protected virtual void OnChecked(RoutedEventArgs e)
        {
            RaiseEvent(e);
        }

        /// <summary>
        /// Called when <see cref="IsChecked"/> becomes false.
        /// </summary>
        /// <param name="e">Event arguments for the routed event that is raised by the default implementation of this method.</param>
        protected virtual void OnUnchecked(RoutedEventArgs e)
        {
            RaiseEvent(e);
        }

        /// <summary>
        /// Called when <see cref="IsChecked"/> becomes null.
        /// </summary>
        /// <param name="e">Event arguments for the routed event that is raised by the default implementation of this method.</param>
        protected virtual void OnIndeterminate(RoutedEventArgs e)
        {
            RaiseEvent(e);
        }

        protected override AutomationPeer OnCreateAutomationPeer(IAutomationNodeFactory factory)
        {
            return new ToggleButtonAutomationPeer(factory, this);
        }

        private void OnIsCheckedChanged(AvaloniaPropertyChangedEventArgs e)
        {
            var newValue = (bool?)e.NewValue;

            switch (newValue)
            {
                case true:
                    OnChecked(new RoutedEventArgs(CheckedEvent));
                    break;
                case false:
                    OnUnchecked(new RoutedEventArgs(UncheckedEvent));
                    break;
                default:
                    OnIndeterminate(new RoutedEventArgs(IndeterminateEvent));
                    break;
            }
        }

        private void UpdatePseudoClasses(bool? isChecked)
        {
            PseudoClasses.Set(":checked", isChecked == true);
            PseudoClasses.Set(":unchecked", isChecked == false);
            PseudoClasses.Set(":indeterminate", isChecked == null);
        }
    }
}
