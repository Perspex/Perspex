using System;
using Avalonia.Interactivity;

namespace Avalonia.Input
{
    /// <summary>
    /// Holds arguments for a <see cref="InputElement.GotFocusEvent"/>.
    /// </summary>
    public class GotFocusEventArgs : RoutedEventArgs
    {
        /// <summary>
        /// Gets or sets a value indicating how the change in focus occurred.
        /// </summary>
        public NavigationMethod NavigationMethod { get; set; }

        /// <summary>
        /// Gets or sets any input modifiers active at the time of focus.
        /// </summary>
        [Obsolete("Use KeyModifiers")]
        public InputModifiers InputModifiers
        {
            get => (InputModifiers)KeyModifiers;
            set => KeyModifiers = (KeyModifiers)((int)value & 0xF);
        }

        /// <summary>
        /// Gets or sets any key modifiers active at the time of focus.
        /// </summary>
        public KeyModifiers KeyModifiers { get; set; }
    }
}
