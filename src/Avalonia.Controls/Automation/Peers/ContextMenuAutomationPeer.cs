using Avalonia.Automation.Platform;
using Avalonia.Controls;

#nullable enable

namespace Avalonia.Automation.Peers
{
    public class ContextMenuAutomationPeer : ControlAutomationPeer
    {
        public ContextMenuAutomationPeer(IAutomationNodeFactory factory, ContextMenu owner)
            : base(factory, owner) 
        { 
        }

        protected override AutomationControlType GetAutomationControlTypeCore()
        {
            return AutomationControlType.Menu;
        }

        protected override bool IsContentElementCore() => false;
    }
}
