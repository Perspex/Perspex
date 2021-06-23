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
    }
}
