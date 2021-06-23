using Avalonia.Interactivity;
using Avalonia.VisualTree;

namespace Avalonia.Input
{
    public class TappedEventArgs : RoutedEventArgs
    {
        private readonly PointerEventArgs lastPointerEventArgs;

        public TappedEventArgs(RoutedEvent routedEvent, PointerEventArgs lastPointerEventArgs)
            : base(routedEvent)
        {
            this.lastPointerEventArgs = lastPointerEventArgs;
        }

        public Point GetPosition(IVisual? relativeTo) => lastPointerEventArgs.GetPosition(relativeTo);
    }
}
