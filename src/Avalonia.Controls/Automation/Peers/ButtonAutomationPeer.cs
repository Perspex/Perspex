using Avalonia.Automation.Platform;
using Avalonia.Automation.Provider;
using Avalonia.Controls;

#nullable enable

namespace Avalonia.Automation.Peers
{
    public class ButtonAutomationPeer : ContentControlAutomationPeer,
        IInvokeProvider
    {
        public ButtonAutomationPeer(IAutomationNodeFactory factory,  Button owner)
            : base(factory, owner) 
        {
        }

        public new Button Owner => (Button)base.Owner;

        public void Invoke()
        {
            EnsureEnabled();
            (Owner as Button)?.PerformClick();
        }

        protected override string? GetAcceleratorKeyCore()
        {
            var result = base.GetAcceleratorKeyCore();

            if (string.IsNullOrWhiteSpace(result))
            {
                result = Owner.HotKey?.ToString();
            }

            return result;
        }

        protected override AutomationControlType GetAutomationControlTypeCore()
        {
            return AutomationControlType.Button;
        }

        protected override bool IsContentElementCore() => true;
        protected override bool IsControlElementCore() => true;
    }
}

