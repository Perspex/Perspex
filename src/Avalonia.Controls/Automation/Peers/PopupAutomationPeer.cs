using System;
using System.Collections.Generic;
using Avalonia.Automation.Platform;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.VisualTree;

#nullable enable

namespace Avalonia.Automation.Peers
{
    public class PopupAutomationPeer : ControlAutomationPeer
    {
        public PopupAutomationPeer(IAutomationNodeFactory factory, Popup owner)
            : base(factory, owner)
        {
            owner.Opened += PopupOpenedClosed;
            owner.Closed += PopupOpenedClosed;
        }

        protected override IReadOnlyList<AutomationPeer>? GetChildrenCore()
        {
            var host = (IVisualTreeHost)Owner;
            System.Diagnostics.Debug.WriteLine($"Popup children='{host}'");
            return host.Root is Control c ? new[] { GetOrCreatePeer(c) } : null;
        }

        protected override bool IsContentElementCore() => false;
        protected override bool IsControlElementCore() => false;

        private void PopupOpenedClosed(object sender, EventArgs e)
        {
            // This is golden. We're following WPF's automation peer API here where the
            // parent of a peer is set when another peer returns it as a child. We want to
            // add the popup root as a child of the popup, so we need to return it as a
            // child right? Yeah except invalidating children doesn't automatically cause
            // UIA to re-read the children meaning that the parent doesn't get set. So the
            // MAIN MECHANISM FOR PARENTING CONTROLS IS BROKEN WITH THE ONLY AUTOMATION API
            // IT WAS WRITTEN FOR. Luckily WPF provides an escape-hatch by exposing the
            // TrySetParent API internally to work around this. We're exposing it publicly
            // to shame whoever came up with this abomination of an API.
            GetPopupRoot()?.TrySetParent(this);
            InvalidateChildren();
        }

        private AutomationPeer? GetPopupRoot()
        {
            var popupRoot = ((IVisualTreeHost)Owner).Root as Control;
            return popupRoot is object ? GetOrCreatePeer(popupRoot) : null;
        }
    }
}
