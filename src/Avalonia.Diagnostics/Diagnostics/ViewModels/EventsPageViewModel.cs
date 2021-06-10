using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Diagnostics.Models;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace Avalonia.Diagnostics.ViewModels
{
    internal class EventsPageViewModel : ViewModelBase
    {
        private static readonly HashSet<RoutedEvent> s_defaultEvents = new HashSet<RoutedEvent>()
        {
            Button.ClickEvent,
            InputElement.KeyDownEvent,
            InputElement.KeyUpEvent,
            InputElement.TextInputEvent,
            InputElement.PointerReleasedEvent,
            InputElement.PointerPressedEvent
        };

        private readonly MainViewModel _mainViewModel;
        private FiredEvent? _selectedEvent;
        private EventTreeNodeBase? _selectedNode;

        public EventsPageViewModel(MainViewModel mainViewModel)
        {
            _mainViewModel = mainViewModel;

            Nodes = RoutedEventRegistry.Instance.GetAllRegistered()
                .GroupBy(e => e.OwnerType)
                .OrderBy(e => e.Key.Name)
                .Select(g => new EventOwnerTreeNode(g.Key, g, this))
                .ToArray();

            EventsFilter = new FilterViewModel();
            EventsFilter.RefreshFilter += (s, e) => UpdateEventFilters();

            EnableDefault();
        }

        public string Name => "Events";

        public EventTreeNodeBase[] Nodes { get; }

        public ObservableCollection<FiredEvent> RecordedEvents { get; } = new ObservableCollection<FiredEvent>();

        public FiredEvent? SelectedEvent
        {
            get => _selectedEvent;
            set => RaiseAndSetIfChanged(ref _selectedEvent, value);
        }

        public EventTreeNodeBase? SelectedNode
        {
            get => _selectedNode;
            set => RaiseAndSetIfChanged(ref _selectedNode, value);
        }

        public FilterViewModel EventsFilter { get; }

        public void Clear()
        {
            RecordedEvents.Clear();
        }

        public void DisableAll()
        {
            EvaluateNodeEnabled(_ => false);
        }

        public void EnableDefault()
        {
            EvaluateNodeEnabled(node => s_defaultEvents.Contains(node.Event));
        }

        public void RequestTreeNavigateTo(EventChainLink navTarget)
        {
            if (navTarget.Handler is IControl control)
            {
                _mainViewModel.RequestTreeNavigateTo(control, true);
            }
        }

        public void SelectEventByType(RoutedEvent evt)
        {
            foreach (var node in Nodes)
            {
                var result = FindNode(node, evt);

                if (result != null && result.IsVisible)
                {
                    SelectedNode = result;

                    break;
                }
            }

            static EventTreeNodeBase? FindNode(EventTreeNodeBase node, RoutedEvent eventType)
            {
                if (node is EventTreeNode eventNode && eventNode.Event == eventType)
                {
                    return node;
                }

                if (node.Children != null)
                {
                    foreach (var child in node.Children)
                    {
                        var result = FindNode(child, eventType);

                        if (result != null)
                        {
                            return result;
                        }
                    }
                }

                return null;
            }
        }

        private void EvaluateNodeEnabled(Func<EventTreeNode, bool> eval)
        {
            void ProcessNode(EventTreeNodeBase node)
            {
                if (node is EventTreeNode eventNode)
                {
                    node.IsEnabled = eval(eventNode);
                }

                if (node.Children != null)
                {
                    foreach (var childNode in node.Children)
                    {
                        ProcessNode(childNode);
                    }
                }
            }

            foreach (var node in Nodes)
            {
                ProcessNode(node);
            }
        }

        private void UpdateEventFilters()
        {
            foreach (var node in Nodes)
            {
                FilterNode(node, false);
            }

            bool FilterNode(EventTreeNodeBase node, bool isParentVisible)
            {
                bool matchesFilter = EventsFilter.Filter(node.Text);
                bool hasVisibleChild = false;

                if (node.Children != null)
                {
                    foreach (var childNode in node.Children)
                    {
                        hasVisibleChild |= FilterNode(childNode, matchesFilter);
                    }
                }

                node.IsVisible = hasVisibleChild || matchesFilter || isParentVisible;

                return node.IsVisible;
            }
        }
    }
}
