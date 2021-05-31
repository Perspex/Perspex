using System;
using System.Runtime.InteropServices;

namespace Avalonia.Win32.Interop.Automation
{
    [ComVisible(true)]
    [Guid("fdc8f176-aed2-477a-8c89-5604c66f278d")]
    public enum SynchronizedInputType
    {
        KeyUp = 0x01,
        KeyDown = 0x02,
        MouseLeftButtonUp = 0x04,
        MouseLeftButtonDown = 0x08,
        MouseRightButtonUp = 0x10,
        MouseRightButtonDown = 0x20
    }

    [ComVisible(true)]
    [Guid("29db1a06-02ce-4cf7-9b42-565d4fab20ee")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface ISynchronizedInputProvider
    {
        void  StartListening(SynchronizedInputType inputType);
        void Cancel();
    }
}
