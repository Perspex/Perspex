using Avalonia.Automation.Platform;
using Avalonia.Controls;

#nullable enable

namespace Avalonia.Automation.Peers
{
    public class TabControlAutomationPeer : SelectingItemsControlAutomationPeer
    {
        public TabControlAutomationPeer(IAutomationNodeFactory factory, TabControl owner)
            : base(factory, owner) 
        {
        }

        protected override AutomationControlType GetAutomationControlTypeCore()
        {
            return AutomationControlType.Tab;
        }
    }
}
