using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;

namespace Avalonia.Interactivity
{
    /// <summary>
    /// Provides extension methods for the <see cref="IInteractive"/> interface.
    /// </summary>
    public static class InteractiveExtensions
    {
        /// <summary>
        /// Adds a handler for the specified routed event and returns a disposable that can terminate the event subscription.
        /// </summary>
        /// <typeparam name="TEventArgs">The type of the event's args.</typeparam>
        /// <param name="o">Target for adding given event handler.</param>
        /// <param name="routedEvent">The routed event.</param>
        /// <param name="handler">The handler.</param>
        /// <param name="routes">The routing strategies to listen to.</param>
        /// <param name="handledEventsToo">Whether handled events should also be listened for.</param>
        /// <returns>A disposable that terminates the event subscription.</returns>
        public static IDisposable AddDisposableHandler<TEventArgs>(this IInteractive o, RoutedEvent<TEventArgs> routedEvent,
            EventHandler<TEventArgs> handler,
            RoutingStrategies routes = RoutingStrategies.Direct | RoutingStrategies.Bubble,
            bool handledEventsToo = false) where TEventArgs : RoutedEventArgs
        {
            o.AddHandler(routedEvent, handler, routes, handledEventsToo);

            return Disposable.Create(
                (instance: o, handler, routedEvent),
                state => state.instance.RemoveHandler(state.routedEvent, state.handler));
        }

        /// <summary>
        /// Gets an observable for a <see cref="RoutedEvent{TEventArgs}"/>.
        /// </summary>
        /// <param name="o">The object to listen for events on.</param>
        /// <param name="routedEvent">The routed event.</param>
        /// <param name="routes">The routing strategies to listen to.</param>
        /// <param name="handledEventsToo">Whether handled events should also be listened for.</param>
        /// <returns>
        /// An observable which fires each time the event is raised.
        /// </returns>
        public static IObservable<TEventArgs> GetObservable<TEventArgs>(
            this IInteractive o,
            RoutedEvent<TEventArgs> routedEvent,
            RoutingStrategies routes = RoutingStrategies.Direct | RoutingStrategies.Bubble,
            bool handledEventsToo = false)
                where TEventArgs : RoutedEventArgs
        {
            o = o ?? throw new ArgumentNullException(nameof(o));
            routedEvent = routedEvent ?? throw new ArgumentNullException(nameof(routedEvent));

            return Observable.Create<TEventArgs>(x => o.AddDisposableHandler(
                routedEvent, 
                (_, e) => x.OnNext(e), 
                routes,
                handledEventsToo));
        }

        /// <summary>
        /// Builds an event route for a routed event.
        /// </summary>
        /// <param name="interactive">The interactive element to start building the route at.</param>
        /// <param name="e">The routed event.</param>
        /// <returns>An <see cref="EventRoute"/> describing the route.</returns>
        /// <remarks>
        /// Usually, calling <see cref="IInteractive.RaiseEvent(RoutedEventArgs)"/> is sufficent to raise a routed
        /// event, however there are situations in which the construction of the event args is expensive
        /// and should be avoided if there are no handlers for an event. In these cases you can call
        /// this method to build the event route and check the <see cref="EventRoute.HasHandlers"/>
        /// property to see if there are any handlers registered on the route. If there are, call
        /// <see cref="EventRoute.RaiseEvent(IInteractive, RoutedEventArgs)"/> to raise the event.
        /// </remarks>
        public static EventRoute BuildEventRoute(this IInteractive interactive, RoutedEvent e)
        {
            e = e ?? throw new ArgumentNullException(nameof(e));

            var result = new EventRoute(e);
            var hasClassHandlers = e.HasRaisedSubscriptions;

            if (e.RoutingStrategies.HasAllFlags(RoutingStrategies.Bubble) ||
                e.RoutingStrategies.HasAllFlags(RoutingStrategies.Tunnel))
            {
                IInteractive? element = interactive;

                while (element != null)
                {
                    if (hasClassHandlers)
                    {
                        result.AddClassHandler(element);
                    }

                    element.AddToEventRoute(e, result);
                    element = element.InteractiveParent;
                }
            }
            else
            {
                if (hasClassHandlers)
                {
                    result.AddClassHandler(interactive);
                }

                interactive.AddToEventRoute(e, result);
            }

            return result;
        }
    }
}
