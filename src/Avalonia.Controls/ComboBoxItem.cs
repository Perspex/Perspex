using System;
using System.Reactive.Linq;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Platform;

namespace Avalonia.Controls
{
    /// <summary>
    /// A selectable item in a <see cref="ComboBox"/>.
    /// </summary>
    public class ComboBoxItem : ListBoxItem
    {
        public ComboBoxItem()
        {
            this.GetObservable(ComboBoxItem.IsFocusedProperty).Where(focused => focused)
                .Subscribe(_ => (Parent as ComboBox)?.ItemFocused(this));
        }

        protected override AutomationPeer OnCreateAutomationPeer(IAutomationNodeFactory factory)
        {
            return new ComboBoxItemAutomationPeer(factory, this);
        }
    }
}
