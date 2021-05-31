using Avalonia.Automation.Provider;
using UIA = Avalonia.Win32.Interop.Automation;

#nullable enable

namespace Avalonia.Win32.Automation
{
    internal partial class AutomationNode : UIA.IToggleProvider
    {
        public ToggleState ToggleState => InvokeSync<IToggleProvider, ToggleState>(x => x.ToggleState);
        public void Toggle() => InvokeSync<IToggleProvider>(x => x.Toggle());
    }
}
