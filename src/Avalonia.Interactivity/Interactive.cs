using System;
using System.Collections.Generic;
using Avalonia.Layout;
using Avalonia.VisualTree;

namespace Avalonia.Interactivity
{
    /// <summary>
    /// Base class for objects that raise routed events.
    /// </summary>
    public class Interactive : Layoutable, IInteractive
    {
        private EventHandlers _eventHandlers;

        /// <inheritdoc />
        IInteractive? IInteractive.InteractiveParent => ((IVisual)this).VisualParent as IInteractive;

        /// <inheritdoc />
        public void AddHandler(
            RoutedEvent routedEvent,
            Delegate handler,
            RoutingStrategies routes = RoutingStrategies.Direct | RoutingStrategies.Bubble,
            bool handledEventsToo = false)
        {
            _eventHandlers.AddHandler(routedEvent, handler, routes, handledEventsToo);
        }

        /// <inheritdoc />
        public void AddHandler<TEventArgs>(
            RoutedEvent<TEventArgs> routedEvent,
            EventHandler<TEventArgs> handler,
            RoutingStrategies routes = RoutingStrategies.Direct | RoutingStrategies.Bubble,
            bool handledEventsToo = false) where TEventArgs : RoutedEventArgs
        {
            _eventHandlers.AddHandler(routedEvent, handler, routes, handledEventsToo);
        }

        /// <inheritdoc />
        public void RemoveHandler(RoutedEvent routedEvent, Delegate handler)
        {
            _eventHandlers.RemoveHandler(routedEvent, handler);
        }

        /// <inheritdoc />
        public void RemoveHandler<TEventArgs>(RoutedEvent<TEventArgs> routedEvent, EventHandler<TEventArgs> handler)
            where TEventArgs : RoutedEventArgs
        {
            RemoveHandler(routedEvent, (Delegate)handler);
        }

        /// <inheritdoc />
        public void RaiseEvent(RoutedEventArgs e)
        {
            EventHandlers.RaiseEvent(this, e);
        }

        void IInteractive.AddToEventRoute(RoutedEvent routedEvent, EventRoute route)
        {
            _eventHandlers.AddToEventRoute(this, routedEvent, route);
        }
    }
}
