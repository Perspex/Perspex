using Avalonia.Interactivity;

namespace Avalonia.Input
{
    public class TextInputEventArgs : RoutedEventArgs
    {
        public IKeyboardDevice? Device { get; set; }

        public string? Text { get; set; }
    }
}
