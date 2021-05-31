using System;
using System.Runtime.InteropServices;
using Avalonia.Automation.Provider;

namespace Avalonia.Win32.Interop.Automation
{
    [ComVisible(true)]
    [Guid("56d00bd0-c4f4-433c-a836-1a52a57e0892")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IToggleProvider
    {
        void Toggle( );
        ToggleState ToggleState { get; }
    }
}
