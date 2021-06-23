using System;
using Avalonia.Collections.Pooled;

namespace Avalonia.Interactivity
{
    /// <summary>
    /// Holds the route for a routed event and supports raising an event on that route.
    /// </summary>
    public class EventRoute : IDisposable
    {
        private readonly RoutedEvent _event;
        private PooledList<RouteItem>? _route;

        /// <summary>
        /// Initializes a new instance of the <see cref="RoutedEvent"/> class.
        /// </summary>
        /// <param name="e">The routed event to be raised.</param>
        public EventRoute(RoutedEvent e)
        {
            e = e ?? throw new ArgumentNullException(nameof(e));

            _event = e;
            _route = null;
        }

        /// <summary>
        /// Gets a value indicating whether the route has any handlers.
        /// </summary>
        public bool HasHandlers => _route?.Count > 0;

        /// <summary>
        /// Adds a handler to the route.
        /// </summary>
        /// <param name="target">The target on which the event should be raised.</param>
        /// <param name="handler">The handler for the event.</param>
        /// <param name="routes">The routing strategies to listen to.</param>
        /// <param name="handledEventsToo">
        /// If true the handler will be raised even when the routed event is marked as handled.
        /// </param>
        /// <param name="adapter">
        /// An optional adapter which if supplied, will be called with <paramref name="handler"/>
        /// and the parameters for the event. This adapter can be used to avoid calling
        /// `DynamicInvoke` on the handler.
        /// </param>
        public void Add(
            IInteractive target,
            Delegate handler,
            RoutingStrategies routes,
            bool handledEventsToo = false,
            Action<Delegate, object, RoutedEventArgs>? adapter = null)
        {
            target = target ?? throw new ArgumentNullException(nameof(target));
            handler = handler ?? throw new ArgumentNullException(nameof(handler));

            _route ??= new PooledList<RouteItem>(16);
            _route.Add(new RouteItem(target, handler, adapter, routes, handledEventsToo));
        }

        /// <summary>
        /// Adds a class handler to the route.
        /// </summary>
        /// <param name="target">The target on which the event should be raised.</param>
        public void AddClassHandler(IInteractive target)
        {
            target = target ?? throw new ArgumentNullException(nameof(target));

            _route ??= new PooledList<RouteItem>(16);
            _route.Add(new RouteItem(target, null, null, 0, false));
        }

        /// <summary>
        /// Raises an event along the route.
        /// </summary>
        /// <param name="source">The event source.</param>
        /// <param name="e">The event args.</param>
        public void RaiseEvent(IInteractive source, RoutedEventArgs e)
        {
            source = source ?? throw new ArgumentNullException(nameof(source));
            e = e ?? throw new ArgumentNullException(nameof(e));

            e.Source = source;

            if (_event.RoutingStrategies == RoutingStrategies.Direct)
            {
                e.Route = RoutingStrategies.Direct;
                RaiseEventImpl(e);
                _event.InvokeRouteFinished(e);
            }
            else
            {
                if (_event.RoutingStrategies.HasAllFlags(RoutingStrategies.Tunnel))
                {
                    e.Route = RoutingStrategies.Tunnel;
                    RaiseEventImpl(e);
                    _event.InvokeRouteFinished(e);
                }

                if (_event.RoutingStrategies.HasAllFlags(RoutingStrategies.Bubble))
                {
                    e.Route = RoutingStrategies.Bubble;
                    RaiseEventImpl(e);
                    _event.InvokeRouteFinished(e);
                }
            }
        }

        /// <summary>
        /// Disposes of the event route.
        /// </summary>
        public void Dispose()
        {
            _route?.Dispose();
            _route = null;
        }

        private void RaiseEventImpl(RoutedEventArgs e)
        {
            if (_route is null)
            {
                return;
            }

            if (e.Source is null)
            {
                throw new ArgumentException("Event source may not be null", nameof(e));
            }

            IInteractive? lastTarget = null;
            var start = 0;
            var end = _route.Count;
            var step = 1;

            if (e.Route == RoutingStrategies.Tunnel)
            {
                start = end - 1;
                step = end = -1;
            }

            for (var i = start; i != end; i += step)
            {
                var entry = _route[i];

                // If we've got to a new control then call any RoutedEvent.Raised listeners.
                if (entry.Target != lastTarget)
                {
                    if (!e.Handled)
                    {
                        _event.InvokeRaised(entry.Target, e);
                    }

                    // If this is a direct event and we've already raised events then we're finished.
                    if (e.Route == RoutingStrategies.Direct && lastTarget is object)
                    {
                        return;
                    }

                    lastTarget = entry.Target;
                }

                // Raise the event handler.
                if (entry.Handler is object &&
                    entry.Routes.HasAllFlags(e.Route) &&
                    (!e.Handled || entry.HandledEventsToo))
                {
                    if (entry.Adapter is object)
                    {
                        entry.Adapter(entry.Handler, entry.Target, e);
                    }
                    else
                    {
                        entry.Handler.DynamicInvoke(entry.Target, e);
                    }
                }
            }
        }

        private readonly struct RouteItem
        {
            public RouteItem(
                IInteractive target,
                Delegate? handler,
                Action<Delegate, object, RoutedEventArgs>? adapter,
                RoutingStrategies routes,
                bool handledEventsToo)
            {
                Target = target;
                Handler = handler;
                Adapter = adapter;
                Routes = routes;
                HandledEventsToo = handledEventsToo;
            }

            public IInteractive Target { get; }
            public Delegate? Handler { get; }
            public Action<Delegate, object, RoutedEventArgs>? Adapter { get; }
            public RoutingStrategies Routes { get; }
            public bool HandledEventsToo { get; }
        }
    }
}
