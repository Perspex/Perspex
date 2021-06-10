using System;

#nullable enable

namespace Avalonia.Interactivity
{
    /// <summary>
    /// Interface for objects that raise routed events.
    /// </summary>
    public interface IInteractive
    {
        /// <summary>
        /// Gets the interactive parent of the object for bubbling and tunneling events.
        /// </summary>
        IInteractive? InteractiveParent { get; }

        /// <summary>
        /// Adds a handler for the specified routed event.
        /// </summary>
        /// <param name="routedEvent">The routed event.</param>
        /// <param name="handler">The handler.</param>
        /// <param name="routes">The routing strategies to listen to.</param>
        /// <param name="handledEventsToo">Whether handled events should also be listened for.</param>
        /// <returns>A disposable that terminates the event subscription.</returns>
        void AddHandler(
            RoutedEvent routedEvent,
            Delegate handler,
            RoutingStrategies routes = RoutingStrategies.Direct | RoutingStrategies.Bubble,
            bool handledEventsToo = false);

        /// <summary>
        /// Adds a handler for the specified routed event.
        /// </summary>
        /// <typeparam name="TEventArgs">The type of the event's args.</typeparam>
        /// <param name="routedEvent">The routed event.</param>
        /// <param name="handler">The handler.</param>
        /// <param name="routes">The routing strategies to listen to.</param>
        /// <param name="handledEventsToo">Whether handled events should also be listened for.</param>
        /// <returns>A disposable that terminates the event subscription.</returns>
        void AddHandler<TEventArgs>(
            RoutedEvent<TEventArgs> routedEvent,
            EventHandler<TEventArgs> handler,
            RoutingStrategies routes = RoutingStrategies.Direct | RoutingStrategies.Bubble,
            bool handledEventsToo = false) where TEventArgs : RoutedEventArgs;

        /// <summary>
        /// Removes a handler for the specified routed event.
        /// </summary>
        /// <param name="routedEvent">The routed event.</param>
        /// <param name="handler">The handler.</param>
        void RemoveHandler(RoutedEvent routedEvent, Delegate handler);

        /// <summary>
        /// Removes a handler for the specified routed event.
        /// </summary>
        /// <typeparam name="TEventArgs">The type of the event's args.</typeparam>
        /// <param name="routedEvent">The routed event.</param>
        /// <param name="handler">The handler.</param>
        void RemoveHandler<TEventArgs>(RoutedEvent<TEventArgs> routedEvent, EventHandler<TEventArgs> handler)
            where TEventArgs : RoutedEventArgs;

        /// <summary>
        /// Adds the object's handlers for a routed event to an event route.
        /// </summary>
        /// <param name="routedEvent">The event.</param>
        /// <param name="route">The event route.</param>
        void AddToEventRoute(RoutedEvent routedEvent, EventRoute route);

        /// <summary>
        /// Raises a routed event.
        /// </summary>
        /// <param name="e">The event args.</param>
        void RaiseEvent(RoutedEventArgs e);
    }
}
