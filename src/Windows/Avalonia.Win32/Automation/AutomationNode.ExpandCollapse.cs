using Avalonia.Automation;
using Avalonia.Automation.Provider;
using UIA = Avalonia.Win32.Interop.Automation;

#nullable enable

namespace Avalonia.Win32.Automation
{
    internal partial class AutomationNode : UIA.IExpandCollapseProvider
    {
        public ExpandCollapseState ExpandCollapseState
        {
            get => InvokeSync<IExpandCollapseProvider, ExpandCollapseState>(x => x.ExpandCollapseState);
        }

        public void Expand() => InvokeSync<IExpandCollapseProvider>(x => x.Expand());
        public void Collapse() => InvokeSync<IExpandCollapseProvider>(x => x.Collapse());
    }
}
