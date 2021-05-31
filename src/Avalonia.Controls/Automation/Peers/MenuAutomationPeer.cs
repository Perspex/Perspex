using Avalonia.Automation.Platform;
using Avalonia.Controls;

#nullable enable

namespace Avalonia.Automation.Peers
{
    public class MenuAutomationPeer : ControlAutomationPeer
    {
        public MenuAutomationPeer(IAutomationNodeFactory factory, Menu owner)
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
