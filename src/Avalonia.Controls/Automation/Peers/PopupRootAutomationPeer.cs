using System;
using Avalonia.Automation.Platform;
using Avalonia.Controls.Primitives;

#nullable enable

namespace Avalonia.Automation.Peers
{
    public class PopupRootAutomationPeer : WindowBaseAutomationPeer
    {
        public PopupRootAutomationPeer(IAutomationNode node, PopupRoot owner)
            : base(node, owner)
        {
            if (owner.IsVisible)
                StartTrackingFocus();
            else
                owner.Opened += OnOpened;
            owner.Closed += OnClosed;
        }

        protected override bool IsContentElementCore() => false;
        protected override bool IsControlElementCore() => false;


        protected override AutomationPeer? GetParentCore()
        {
            var parent = base.GetParentCore();
            return parent;
        }

        private void OnOpened(object sender, EventArgs e)
        {
            ((PopupRoot)Owner).Opened -= OnOpened;
            StartTrackingFocus();
        }

        private void OnClosed(object sender, EventArgs e)
        {
            ((PopupRoot)Owner).Closed -= OnClosed;
            StopTrackingFocus();
        }
    }
}
