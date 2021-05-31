using System;
using Avalonia.Automation.Platform;
using Avalonia.Automation.Provider;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Selection;

#nullable enable

namespace Avalonia.Automation.Peers
{
    public class ComboBoxItemAutomationPeer : ListItemAutomationPeer
    {
        public ComboBoxItemAutomationPeer(IAutomationNodeFactory factory, ComboBoxItem owner)
            : base(factory, owner)
        {
        }

        protected override AutomationControlType GetAutomationControlTypeCore()
        {
            return AutomationControlType.ComboBoxItem;
        }
    }
}
