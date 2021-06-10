using System;
using System.Collections.ObjectModel;
using Avalonia.Diagnostics.Models;
using Avalonia.Interactivity;

namespace Avalonia.Diagnostics.ViewModels
{
    internal class FiredEvent : ViewModelBase
    {
        private readonly RoutedEventArgs _eventArgs;
        private EventChainLink? _handledBy;

        public FiredEvent(RoutedEventArgs eventArgs, EventChainLink originator)
        {
            _eventArgs = eventArgs ?? throw new ArgumentNullException(nameof(eventArgs));
            Originator = originator ?? throw new ArgumentNullException(nameof(originator));
            AddToChain(originator);
        }

        public bool IsPartOfSameEventChain(RoutedEventArgs e)
        {
            return e == _eventArgs;
        }

        public RoutedEvent Event => _eventArgs.RoutedEvent!;

        public bool IsHandled => HandledBy?.Handled == true;

        public ObservableCollection<EventChainLink> EventChain { get; } = new ObservableCollection<EventChainLink>();

        public string DisplayText
        {
            get
            {
                if (IsHandled)
                {
                    return $"{Event.Name} on {Originator.HandlerName};" + Environment.NewLine +
                           $"strategies: {Event.RoutingStrategies}; handled by: {HandledBy!.HandlerName}";
                }

                return $"{Event.Name} on {Originator.HandlerName}; strategies: {Event.RoutingStrategies}";
            }
        }

        public EventChainLink Originator { get; }

        public EventChainLink? HandledBy
        {
            get => _handledBy;
            set
            {
                if (_handledBy != value)
                {
                    _handledBy = value;
                    RaisePropertyChanged();
                    RaisePropertyChanged(nameof(IsHandled));
                    RaisePropertyChanged(nameof(DisplayText));
                }
            }
        }

        public void AddToChain(EventChainLink link)
        {
            if (EventChain.Count > 0)
            {
                var prevLink = EventChain[EventChain.Count-1];

                if (prevLink.Route != link.Route)
                {
                    link.BeginsNewRoute = true;
                }
            }

            EventChain.Add(link);
            if (HandledBy == null && link.Handled)
                HandledBy = link;
        }
    }
}
