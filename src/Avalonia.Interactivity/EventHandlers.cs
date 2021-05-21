using System;
using System.Collections.Generic;

namespace Avalonia.Interactivity
{
    /// <summary>
    /// This struct aids the implementation of <see cref="IInteractive"/>.
    /// </summary>
    internal struct EventHandlers
    {
        private Dictionary<RoutedEvent, List<EventSubscription>>? _eventHandlers;

        /// <inheritdoc cref="IInteractive.AddToEventRoute"/>
        public void AddToEventRoute(IInteractive element, RoutedEvent routedEvent, EventRoute route)
        {
            routedEvent = routedEvent ?? throw new ArgumentNullException(nameof(routedEvent));
            route = route ?? throw new ArgumentNullException(nameof(route));

            if (_eventHandlers != null &&
                _eventHandlers.TryGetValue(routedEvent, out var subscriptions))
            {
                foreach (var sub in subscriptions)
                {
                    route.Add(element, sub.Handler, sub.Routes, sub.HandledEventsToo, sub.InvokeAdapter);
                }
            }
        }

        /// <inheritdoc cref="IInteractive.AddHandler"/>
        public void AddHandler(
            RoutedEvent routedEvent,
            Delegate handler,
            RoutingStrategies routes = RoutingStrategies.Direct | RoutingStrategies.Bubble,
            bool handledEventsToo = false)
        {
            routedEvent = routedEvent ?? throw new ArgumentNullException(nameof(routedEvent));
            handler = handler ?? throw new ArgumentNullException(nameof(handler));

            var subscription = new EventSubscription(handler, routes, handledEventsToo);

            AddEventSubscription(routedEvent, subscription);
        }

        /// <inheritdoc cref="IInteractive.AddHandler"/>
        public void AddHandler<TEventArgs>(
            RoutedEvent<TEventArgs> routedEvent,
            EventHandler<TEventArgs> handler,
            RoutingStrategies routes = RoutingStrategies.Direct | RoutingStrategies.Bubble,
            bool handledEventsToo = false) where TEventArgs : RoutedEventArgs
        {
            routedEvent = routedEvent ?? throw new ArgumentNullException(nameof(routedEvent));
            handler = handler ?? throw new ArgumentNullException(nameof(handler));

            static void InvokeAdapter(Delegate baseHandler, object sender, RoutedEventArgs args)
            {
                var typedHandler = (EventHandler<TEventArgs>)baseHandler;
                var typedArgs = (TEventArgs)args;

                typedHandler(sender, typedArgs);
            }

            var subscription = new EventSubscription(handler, routes, handledEventsToo, (baseHandler, sender, args) => InvokeAdapter(baseHandler, sender, args));

            AddEventSubscription(routedEvent, subscription);
        }

        /// <inheritdoc cref="IInteractive.RemoveHandler"/>
        public void RemoveHandler(RoutedEvent routedEvent, Delegate handler)
        {
            routedEvent = routedEvent ?? throw new ArgumentNullException(nameof(routedEvent));
            handler = handler ?? throw new ArgumentNullException(nameof(handler));

            if (_eventHandlers != null &&
                _eventHandlers.TryGetValue(routedEvent, out var subscriptions))
            {
                for (var i = subscriptions.Count - 1; i >= 0; i--)
                {
                    if (subscriptions[i].Handler == handler)
                    {
                        subscriptions.RemoveAt(i);
                    }
                }
            }
        }

        /// <a cref="IInteractive.RaiseEvent"/>
        public static void RaiseEvent(IInteractive interactive, RoutedEventArgs e)
        {
            e = e ?? throw new ArgumentNullException(nameof(e));

            if (e.RoutedEvent == null)
            {
                throw new ArgumentException("Cannot raise an event whose RoutedEvent is null.");
            }

            using var route = interactive.BuildEventRoute(e.RoutedEvent);
            route.RaiseEvent(interactive, e);
        }

        private void AddEventSubscription(RoutedEvent routedEvent, EventSubscription subscription)
        {
            _eventHandlers ??= new Dictionary<RoutedEvent, List<EventSubscription>>();

            if (!_eventHandlers.TryGetValue(routedEvent, out var subscriptions))
            {
                subscriptions = new List<EventSubscription>();
                _eventHandlers.Add(routedEvent, subscriptions);
            }

            subscriptions.Add(subscription);
        }

        private readonly struct EventSubscription
        {
            public EventSubscription(
                Delegate handler,
                RoutingStrategies routes,
                bool handledEventsToo,
                Action<Delegate, object, RoutedEventArgs>? invokeAdapter = null)
            {
                Handler = handler;
                Routes = routes;
                HandledEventsToo = handledEventsToo;
                InvokeAdapter = invokeAdapter;
            }

            public Action<Delegate, object, RoutedEventArgs>? InvokeAdapter { get; }

            public Delegate Handler { get; }

            public RoutingStrategies Routes { get; }

            public bool HandledEventsToo { get; }
        }
    }

}
