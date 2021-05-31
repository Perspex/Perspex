using Avalonia.Automation.Platform;
using Avalonia.Controls;

namespace Avalonia.Automation.Peers
{
    public class TabItemAutomationPeer : ListItemAutomationPeer
    {
        public TabItemAutomationPeer(IAutomationNodeFactory factory, TabItem owner)
            : base(factory, owner)
        {
        }

        protected override AutomationControlType GetAutomationControlTypeCore()
        {
            return AutomationControlType.TabItem;
        }
    }
}
