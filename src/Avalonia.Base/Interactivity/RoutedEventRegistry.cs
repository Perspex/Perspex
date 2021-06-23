using System;
using System.Collections.Generic;

namespace Avalonia.Interactivity
{
    /// <summary>
    /// Tracks registered <see cref="RoutedEvent"/>s.
    /// </summary>
    public class RoutedEventRegistry
    {
        private readonly Dictionary<Type, List<RoutedEvent>> _registeredRoutedEvents =
            new Dictionary<Type, List<RoutedEvent>>();

        /// <summary>
        /// Gets the <see cref="RoutedEventRegistry"/> instance.
        /// </summary>
        public static RoutedEventRegistry Instance { get; }
            = new RoutedEventRegistry();

        /// <summary>
        /// Registers a <see cref="RoutedEvent"/> on a type.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="event">The event.</param>
        /// <remarks>
        /// You won't usually want to call this method directly, instead use the
        /// <see cref="RoutedEvent.Register{TOwner, TEventArgs}(string, RoutingStrategies)"/>
        /// method.
        /// </remarks>
        public void Register(Type type, RoutedEvent @event)
        {
            type = type ?? throw new ArgumentNullException(nameof(type));
            @event = @event ?? throw new ArgumentNullException(nameof(@event));

            if (!_registeredRoutedEvents.TryGetValue(type, out var list))
            {
                list = new List<RoutedEvent>();
                _registeredRoutedEvents.Add(type, list);
            }
            list.Add(@event);
        }

        /// <summary>
        /// Returns all routed events, that are currently registered in the event registry.
        /// </summary>
        /// <returns>All routed events, that are currently registered in the event registry.</returns>
        public IEnumerable<RoutedEvent> GetAllRegistered()
        {
            foreach (var events in _registeredRoutedEvents.Values)
            {
                foreach (var e in events)
                {
                    yield return e;
                }
            }
        }

        /// <summary>
        /// Returns all routed events registered with the provided type.
        /// If the type is not found or does not provide any routed events, an empty list is returned.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>All routed events registered with the provided type.</returns>
        public IReadOnlyList<RoutedEvent> GetRegistered(Type type)
        {
            type = type ?? throw new ArgumentNullException(nameof(type));

            if (_registeredRoutedEvents.TryGetValue(type, out var events))
            {
                return events;
            }

            return Array.Empty<RoutedEvent>();
        }

        /// <summary>
        /// Returns all routed events registered with the provided type.         
        /// If the type is not found or does not provide any routed events, an empty list is returned.
        /// </summary>
        /// <typeparam name="TOwner">The type.</typeparam>
        /// <returns>All routed events registered with the provided type.</returns>
        public IReadOnlyList<RoutedEvent> GetRegistered<TOwner>()
        {
            return GetRegistered(typeof(TOwner));
        }
    }
}
