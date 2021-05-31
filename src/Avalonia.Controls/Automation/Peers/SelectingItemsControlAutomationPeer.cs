using System;
using System.Collections.Generic;
using Avalonia.Automation.Platform;
using Avalonia.Automation.Provider;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Selection;
using Avalonia.VisualTree;

#nullable enable

namespace Avalonia.Automation.Peers
{
    public abstract class SelectingItemsControlAutomationPeer : ItemsControlAutomationPeer,
        ISelectionProvider
    {
        private ISelectionModel _selection;

        protected SelectingItemsControlAutomationPeer(IAutomationNodeFactory factory, SelectingItemsControl owner)
            : base(factory, owner) 
        {
            _selection = owner.GetValue(ListBox.SelectionProperty);
            _selection.SelectionChanged += OwnerSelectionChanged;
            owner.PropertyChanged += OwnerPropertyChanged;
        }

        public bool CanSelectMultiple => GetSelectionModeCore().HasFlagCustom(SelectionMode.Multiple);
        public bool IsSelectionRequired => GetSelectionModeCore().HasFlagCustom(SelectionMode.AlwaysSelected);
        public IReadOnlyList<AutomationPeer> GetSelection() => GetSelectionCore() ?? Array.Empty<AutomationPeer>();

        protected virtual IReadOnlyList<AutomationPeer>? GetSelectionCore()
        {
            List<AutomationPeer>? result = null;

            if (Owner is SelectingItemsControl owner)
            {
                var selection = Owner.GetValue(ListBox.SelectionProperty);

                foreach (var i in selection.SelectedIndexes)
                {
                    var container = owner.ItemContainerGenerator.ContainerFromIndex(i);

                    if (container is Control c && ((IVisual)c).IsAttachedToVisualTree)
                    {
                        var peer = GetOrCreatePeer(c);

                        if (peer is object)
                        {
                            result ??= new List<AutomationPeer>();
                            result.Add(peer);
                        }
                    }
                }

                return result;
            }

            return result;
        }

        protected virtual SelectionMode GetSelectionModeCore()
        {
            return (Owner as SelectingItemsControl)?.GetValue(ListBox.SelectionModeProperty) ?? SelectionMode.Single;
        }

        protected virtual void OwnerPropertyChanged(object sender, AvaloniaPropertyChangedEventArgs e)
        {
            if (e.Property == ListBox.SelectionProperty)
            {
                _selection.SelectionChanged -= OwnerSelectionChanged;
                _selection = Owner.GetValue(ListBox.SelectionProperty);
                _selection.SelectionChanged += OwnerSelectionChanged;
                RaiseSelectionChanged();
            }
        }

        protected virtual void OwnerSelectionChanged(object sender, SelectionModelSelectionChangedEventArgs e)
        {
            RaiseSelectionChanged();
        }

        private void RaiseSelectionChanged()
        {
            RaisePropertyChangedEvent(SelectionPatternIdentifiers.SelectionProperty, null, null);
        }
    }
}
