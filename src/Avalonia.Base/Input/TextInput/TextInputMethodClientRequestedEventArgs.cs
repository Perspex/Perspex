using Avalonia.Interactivity;

namespace Avalonia.Input.TextInput
{
    public class TextInputMethodClientRequestedEventArgs : RoutedEventArgs
    {
        /// <summary>
        /// Set this property to a valid text input client to enable input method interaction
        /// </summary>
        public ITextInputMethodClient? Client { get; set; }
    }
}
