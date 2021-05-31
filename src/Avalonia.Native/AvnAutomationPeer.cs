using System.Collections.Generic;
using System.Linq;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Native.Interop;

#nullable enable

namespace Avalonia.Native
{
    internal class AvnAutomationPeer : CallbackBase, IAvnAutomationPeer
    {
        private readonly AutomationPeer _inner;

        public AvnAutomationPeer(AutomationPeer inner) => _inner = inner;

        public IAvnAutomationNode Node => ((AutomationNode)_inner.Node).Native;
        public IAvnString? AcceleratorKey => _inner.GetAcceleratorKey().ToAvnString();
        public IAvnString? AccessKey => _inner.GetAccessKey().ToAvnString();
        public AvnAutomationControlType AutomationControlType => (AvnAutomationControlType)_inner.GetAutomationControlType();
        public IAvnString? AutomationId => _inner.GetAutomationId().ToAvnString();
        public AvnRect BoundingRectangle => _inner.GetBoundingRectangle().ToAvnRect();
        public IAvnAutomationPeerArray Children => new AvnAutomationPeerArray(_inner.GetChildren());
        public IAvnString ClassName => _inner.GetClassName().ToAvnString();
        public IAvnAutomationPeer? LabeledBy => Wrap(_inner.GetLabeledBy());
        public IAvnString Name => _inner.GetName().ToAvnString();
        public IAvnAutomationPeer? Parent => Wrap(_inner.GetParent());

        public int HasKeyboardFocus() => _inner.HasKeyboardFocus().AsComBool();
        public int IsContentElement() => _inner.IsContentElement().AsComBool();
        public int IsControlElement() => _inner.IsControlElement().AsComBool();
        public int IsEnabled() => _inner.IsEnabled().AsComBool();
        public int IsKeyboardFocusable() => _inner.IsKeyboardFocusable().AsComBool();
        public void SetFocus() => _inner.SetFocus();
        public int ShowContextMenu() => _inner.ShowContextMenu().AsComBool();

        public IAvnAutomationPeer? RootPeer
        {
            get
            {
                var peer = _inner;
                var parent = peer.GetParent();

                while (!(peer is IRootProvider) && parent is object)
                {
                    peer = parent;
                    parent = peer.GetParent();
                }

                return new AvnAutomationPeer(peer);
            }
        }

        public int IsRootProvider() => (_inner is IRootProvider).AsComBool();
        public IAvnAutomationPeer? RootProvider_GetFocus() => Wrap(((IRootProvider)_inner).GetFocus());

        public IAvnAutomationPeer? RootProvider_GetPeerFromPoint(AvnPoint point)
        {
            var result = ((IRootProvider)_inner).GetPeerFromPoint(point.ToAvaloniaPoint());

            if (result is null)
                return null;

            // The OSX accessibility APIs expect non-ignored elements when hit-testing.
            while (!result.IsControlElement())
            {
                var parent = result.GetParent();

                if (parent is object)
                    result = parent;
                else
                    break;
            }
            
            return Wrap(result);
        }

        public int IsExpandCollapseProvider() => (_inner is IExpandCollapseProvider).AsComBool();

        public int ExpandCollapseProvider_GetIsExpanded() => ((IExpandCollapseProvider)_inner).ExpandCollapseState switch
        {
            ExpandCollapseState.Expanded => 1,
            ExpandCollapseState.PartiallyExpanded => 1,
            _ => 0,
        };

        public int ExpandCollapseProvider_GetShowsMenu() => ((IExpandCollapseProvider)_inner).ShowsMenu.AsComBool();
        public void ExpandCollapseProvider_Expand() => ((IExpandCollapseProvider)_inner).Expand();
        public void ExpandCollapseProvider_Collapse() => ((IExpandCollapseProvider)_inner).Collapse();

        public int IsInvokeProvider() => (_inner is IInvokeProvider).AsComBool();
        public void InvokeProvider_Invoke() => ((IInvokeProvider)_inner).Invoke();

        public int IsRangeValueProvider() => (_inner is IRangeValueProvider).AsComBool();
        public double RangeValueProvider_GetValue() => ((IRangeValueProvider)_inner).Value;
        public double RangeValueProvider_GetMinimum() => ((IRangeValueProvider)_inner).Minimum;
        public double RangeValueProvider_GetMaximum() => ((IRangeValueProvider)_inner).Maximum;
        public double RangeValueProvider_GetSmallChange() => ((IRangeValueProvider)_inner).SmallChange;
        public double RangeValueProvider_GetLargeChange() => ((IRangeValueProvider)_inner).LargeChange;
        public void RangeValueProvider_SetValue(double value) => ((IRangeValueProvider)_inner).SetValue(value);
        
        public int IsToggleProvider() => (_inner is IToggleProvider).AsComBool();
        public int ToggleProvider_GetToggleState() => (int)((IToggleProvider)_inner).ToggleState;
        public void ToggleProvider_Toggle() => ((IToggleProvider)_inner).Toggle();

        public int IsValueProvider() => (_inner is IValueProvider).AsComBool();
        public IAvnString ValueProvider_GetValue() => ((IValueProvider)_inner).Value.ToAvnString();
        public void ValueProvider_SetValue(string value) => ((IValueProvider)_inner).SetValue(value);
        
        public static AvnAutomationPeer? Wrap(AutomationPeer? peer) =>
            peer != null ? new AvnAutomationPeer(peer) : null;
    }

    internal class AvnAutomationPeerArray : CallbackBase, IAvnAutomationPeerArray
    {
        private readonly AvnAutomationPeer[] _items;
        
        public AvnAutomationPeerArray(IReadOnlyList<AutomationPeer> items)
        {
            _items = items.Select(x => new AvnAutomationPeer(x)).ToArray();
        }
        
        public uint Count => (uint)_items.Length;
        public IAvnAutomationPeer Get(uint index) => _items[index];
    }
}
