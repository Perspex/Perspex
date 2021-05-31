using Avalonia.Automation.Platform;
using Avalonia.Controls;

#nullable enable

namespace Avalonia.Automation.Peers
{
    public class CheckBoxAutomationPeer : ToggleButtonAutomationPeer
    {
        public CheckBoxAutomationPeer(IAutomationNodeFactory factory, CheckBox owner)
            : base(factory, owner) 
        {
        }

        protected override AutomationControlType GetAutomationControlTypeCore()
        {
            return AutomationControlType.CheckBox;
        }
    }
}
