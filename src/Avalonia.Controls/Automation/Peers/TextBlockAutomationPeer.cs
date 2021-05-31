using Avalonia.Automation.Platform;
using Avalonia.Controls;

#nullable enable

namespace Avalonia.Automation.Peers
{
    public class TextBlockAutomationPeer : ControlAutomationPeer
    {
        public TextBlockAutomationPeer(IAutomationNodeFactory factory, TextBlock owner)
            : base(factory, owner)
        {
        }

        public new TextBlock Owner => (TextBlock)base.Owner;

        protected override AutomationControlType GetAutomationControlTypeCore()
        {
            return AutomationControlType.Text;
        }

        protected override string? GetNameCore() => Owner.Text;

        protected override bool IsControlElementCore()
        {
            // Return false if the control is part of a control template.
            return Owner.TemplatedParent is null && base.IsControlElementCore();
        }
    }
}
