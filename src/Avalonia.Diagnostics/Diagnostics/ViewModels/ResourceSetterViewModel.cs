using Avalonia.Media;

namespace Avalonia.Diagnostics.ViewModels
{
    internal class ResourceSetterViewModel : SetterViewModel
    {
        public object Key { get; }

        public IBrush Tint { get; }

        public ResourceSetterViewModel(AvaloniaProperty property, object resourceKey, object? resourceValue, bool isDynamic) : base(property, resourceValue)
        {
            Key = resourceKey;
            Tint = isDynamic ? Brushes.Orange : Brushes.Brown;
        }

        public void CopyResourceKey()
        {
            var textToCopy = Key?.ToString();

            if (textToCopy is null)
            {
                return;
            }

            CopyToClipboard(textToCopy);
        }
    }
}
