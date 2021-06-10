using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Collections;
using Avalonia.Interactivity;

namespace Avalonia.Diagnostics.ViewModels
{
    internal class EventOwnerTreeNode : EventTreeNodeBase
    {
        public EventOwnerTreeNode(Type type, IEnumerable<RoutedEvent> events, EventsPageViewModel vm)
            : base(null, type.Name)
        {
            Children = new AvaloniaList<EventTreeNodeBase>(events.OrderBy(e => e.Name)
                .Select(e => new EventTreeNode(this, e, vm)));
            IsExpanded = true;
        }

        public override bool? IsEnabled
        {
            get => base.IsEnabled;
            set
            {
                if (base.IsEnabled != value)
                {
                    base.IsEnabled = value;

                    if (_updateChildren && value != null)
                    {
                        foreach (var child in Children!)
                        {
                            try
                            {
                                child._updateParent = false;
                                child.IsEnabled = value;
                            }
                            finally
                            {
                                child._updateParent = true;
                            }
                        }
                    }
                }
            }
        }
    }
}
