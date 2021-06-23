using Avalonia.Interactivity;

namespace Avalonia.Input.TextInput
{
    public class TextInputOptionsQueryEventArgs : RoutedEventArgs
    {
        /// <summary>
        /// The content type (mostly for determining the shape of the virtual keyboard)
        /// </summary>
        public TextInputContentType ContentType { get; set; }
        /// <summary>
        /// Text is multiline
        /// </summary>
        public bool Multiline { get; set; }
        /// <summary>
        /// Text is in lower case
        /// </summary>
        public bool Lowercase { get; set; }
        /// <summary>
        /// Text is in upper case
        /// </summary>
        public bool Uppercase { get; set; }
        /// <summary>
        /// Automatically capitalize letters at the start of the sentence
        /// </summary>
        public bool AutoCapitalization { get; set; }
        /// <summary>
        /// Text contains sensitive data like card numbers and should not be stored  
        /// </summary>
        public bool IsSensitive { get; set; }
    }
}
