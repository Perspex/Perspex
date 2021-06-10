using System;
using Avalonia.Interactivity;

namespace Avalonia.Diagnostics.Models
{
    internal class EventChainLink
    {
        public EventChainLink(object handler, bool handled, RoutingStrategies route)
        {
            Handler = handler ?? throw new ArgumentNullException(nameof(handler));
            Handled = handled;
            Route = route;
        }

        public object Handler { get; }

        public bool BeginsNewRoute { get; set; }

        public string HandlerName
        {
            get
            {
                if (Handler is INamed named && !string.IsNullOrEmpty(named.Name))
                {
                    return named.Name + " (" + Handler.GetType().Name + ")";
                }

                return Handler.GetType().Name;
            }
        }

        public bool Handled { get; }

        public RoutingStrategies Route { get; }
    }
}
