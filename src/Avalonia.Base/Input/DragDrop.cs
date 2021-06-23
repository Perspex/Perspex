using System.Threading.Tasks;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;

namespace Avalonia.Input
{
    public static class DragDrop
    {
        /// <summary>
        /// Event which is raised, when a drag-and-drop operation enters the element.
        /// </summary>
        public static readonly RoutedEvent<DragEventArgs> DragEnterEvent = RoutedEvent.Register<DragEventArgs>("DragEnter", RoutingStrategies.Bubble, typeof(DragDrop));
        /// <summary>
        /// Event which is raised, when a drag-and-drop operation leaves the element.
        /// </summary>
        public static readonly RoutedEvent<RoutedEventArgs> DragLeaveEvent = RoutedEvent.Register<RoutedEventArgs>("DragLeave", RoutingStrategies.Bubble, typeof(DragDrop));
        /// <summary>
        /// Event which is raised, when a drag-and-drop operation is updated while over the element.
        /// </summary>
        public static readonly RoutedEvent<DragEventArgs> DragOverEvent = RoutedEvent.Register<DragEventArgs>("DragOver", RoutingStrategies.Bubble, typeof(DragDrop));
        /// <summary>
        /// Event which is raised, when a drag-and-drop operation should complete over the element.
        /// </summary>
        public static readonly RoutedEvent<DragEventArgs> DropEvent = RoutedEvent.Register<DragEventArgs>("Drop", RoutingStrategies.Bubble, typeof(DragDrop));

        public static readonly AttachedProperty<bool> AllowDropProperty = AvaloniaProperty.RegisterAttached<Interactive, bool>("AllowDrop", typeof(DragDrop), inherits: true);

        /// <summary>
        /// Gets a value indicating whether the given element can be used as the target of a drag-and-drop operation. 
        /// </summary>
        public static bool GetAllowDrop(Interactive interactive)
        {
            return interactive.GetValue(AllowDropProperty);
        }

        /// <summary>
        /// Sets a value indicating whether the given interactive can be used as the target of a drag-and-drop operation. 
        /// </summary>
        public static void SetAllowDrop(Interactive interactive, bool value)
        {
            interactive.SetValue(AllowDropProperty, value);
        }

        /// <summary>
        /// Starts a dragging operation with the given <see cref="IDataObject"/> and returns the applied drop effect from the target.
        /// <seealso cref="DataObject"/>
        /// </summary>
        public static Task<DragDropEffects> DoDragDrop(PointerEventArgs triggerEvent, IDataObject data, DragDropEffects allowedEffects)
        {
            var src = AvaloniaLocator.Current.GetService<IPlatformDragSource>();
            return src?.DoDragDrop(triggerEvent, data, allowedEffects) ?? Task.FromResult(DragDropEffects.None);
        }
    }
}
