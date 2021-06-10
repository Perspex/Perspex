using System;
using Avalonia.Diagnostics.Models;
using Avalonia.Diagnostics.Views;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace Avalonia.Diagnostics.ViewModels
{
    internal class EventTreeNode : EventTreeNodeBase
    {
        private readonly EventsPageViewModel _parentViewModel;
        private bool _isRegistered;
        private FiredEvent? _currentEvent;

        public EventTreeNode(EventOwnerTreeNode parent, RoutedEvent @event, EventsPageViewModel vm)
            : base(parent, @event.Name)
        {
            Event = @event ?? throw new ArgumentNullException(nameof(@event));
            _parentViewModel = vm ?? throw new ArgumentNullException(nameof(vm));
        }

        public RoutedEvent Event { get; }

        public override bool? IsEnabled
        {
            get => base.IsEnabled;
            set
            {
                if (base.IsEnabled != value)
                {
                    base.IsEnabled = value;
                    UpdateTracker();
                    if (Parent != null && _updateParent)
                    {
                        try
                        {
                            Parent._updateChildren = false;
                            Parent.UpdateChecked();
                        }
                        finally
                        {
                            Parent._updateChildren = true;
                        }
                    }
                }
            }
        }

        private void UpdateTracker()
        {
            if (IsEnabled.GetValueOrDefault() && !_isRegistered)
            {
                var allRoutes = RoutingStrategies.Direct | RoutingStrategies.Tunnel | RoutingStrategies.Bubble;

                // FIXME: This leaks event handlers.
                Event.AddClassHandler(typeof(object), HandleEvent, allRoutes, handledEventsToo: true);
                _isRegistered = true;
            }
        }

        private void HandleEvent(object? sender, RoutedEventArgs e)
        {
            if (!_isRegistered || IsEnabled == false)
                return;
            if (sender is IVisual v && BelongsToDevTool(v))
                return;

            var s = sender!;
            var handled = e.Handled;
            var route = e.Route;

            void handler()
            {
                if (_currentEvent == null || !_currentEvent.IsPartOfSameEventChain(e))
                {
                    _currentEvent = new FiredEvent(e, new EventChainLink(s, handled, route));

                    _parentViewModel.RecordedEvents.Add(_currentEvent);

                    while (_parentViewModel.RecordedEvents.Count > 100)
                        _parentViewModel.RecordedEvents.RemoveAt(0);
                }
                else
                {
                    _currentEvent.AddToChain(new EventChainLink(s, handled, route));
                }
            };

            if (!Dispatcher.UIThread.CheckAccess())
                Dispatcher.UIThread.Post(handler);
            else
                handler();
        }

        private static bool BelongsToDevTool(IVisual v)
        {
            var current = v;

            while (current != null)
            {
                if (current is MainView || current is MainWindow)
                {
                    return true;
                }

                current = current.VisualParent;
            }

            return false;
        }
    }
}
