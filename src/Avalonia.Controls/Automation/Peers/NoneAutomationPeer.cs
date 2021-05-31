using Avalonia.Automation.Platform;
using Avalonia.Controls;

#nullable enable

namespace Avalonia.Automation.Peers
{
    /// <summary>
    /// An automation peer which represents an element that is exposed to automation as non-
    /// interactive or as not contributing to the logical structure of the application.
    /// </summary>
    public class NoneAutomationPeer : ControlAutomationPeer
    {
        public NoneAutomationPeer(IAutomationNodeFactory factory, Control owner)
            : base(factory, owner) 
        { 
        }

        protected override AutomationControlType GetAutomationControlTypeCore()
        {
            return AutomationControlType.Group;
        }

        protected override bool IsContentElementCore() => false;
        protected override bool IsControlElementCore() => false;
    }
}

