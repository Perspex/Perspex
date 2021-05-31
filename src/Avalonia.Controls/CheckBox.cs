using Avalonia.Automation.Peers;
using Avalonia.Automation.Platform;
using Avalonia.Controls.Primitives;

namespace Avalonia.Controls
{
    /// <summary>
    /// A check box control.
    /// </summary>
    public class CheckBox : ToggleButton
    {
        protected override AutomationPeer OnCreateAutomationPeer(IAutomationNodeFactory factory)
        {
            return new CheckBoxAutomationPeer(factory, this);
        }
    }
}
