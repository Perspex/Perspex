using System.Runtime.InteropServices;
using Avalonia.Automation.Provider;
using UIA = Avalonia.Win32.Interop.Automation;

#nullable enable

namespace Avalonia.Win32.Automation
{
    internal partial class AutomationNode : UIA.IValueProvider
    {
        public string? Value => InvokeSync<IValueProvider, string?>(x => x.Value);

        public void SetValue([MarshalAs(UnmanagedType.LPWStr)] string? value)
        {
            InvokeSync<IValueProvider>(x => x.SetValue(value));
        }
    }
}
