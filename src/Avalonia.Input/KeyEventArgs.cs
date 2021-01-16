using System;
using Avalonia.Interactivity;

namespace Avalonia.Input
{
    public class KeyEventArgs : RoutedEventArgs
    {
        public IKeyboardDevice? Device { get; set; }

        public Key Key { get; set; }

        public string MappedKey { get; set; }

        [Obsolete("Use KeyModifiers")]
        public InputModifiers Modifiers => (InputModifiers)KeyModifiers;
        public KeyModifiers KeyModifiers { get; set; }
    }
}
