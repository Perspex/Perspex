using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Diagnostics.Models;
using Avalonia.Diagnostics.ViewModels;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;

namespace Avalonia.Diagnostics.Views
{
    internal class EventsPageView : UserControl
    {
        private readonly ListBox _events;

        public EventsPageView()
        {
            InitializeComponent();
            _events = this.FindControl<ListBox>("EventsList");
        }

        public void NavigateTo(object sender, TappedEventArgs e)
        {
            if (DataContext is EventsPageViewModel vm && sender is Control control)
            {
                switch (control.Tag)
                {
                    case EventChainLink chainLink:
                    {
                        vm.RequestTreeNavigateTo(chainLink);
                        break;
                    }
                    case RoutedEvent evt:
                    {
                        vm.SelectEventByType(evt);

                        break;
                    }
                }
            }
        }

        protected override void OnDataContextChanged(EventArgs e)
        {
            base.OnDataContextChanged(e);

            if (DataContext is EventsPageViewModel vm)
            {
                vm.RecordedEvents.CollectionChanged += OnRecordedEventsChanged;
            }
        }

        private void OnRecordedEventsChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (sender is ObservableCollection<FiredEvent> events)
            {
                var evt = events.LastOrDefault();

                if (evt is null)
                {
                    return;
                }

                Dispatcher.UIThread.Post(() => _events.ScrollIntoView(evt));
            }
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
