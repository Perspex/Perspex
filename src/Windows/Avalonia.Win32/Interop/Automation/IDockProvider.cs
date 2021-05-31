using System;
using System.Runtime.InteropServices;

namespace Avalonia.Win32.Interop.Automation
{
    [ComVisible(true)]
    [Guid("70d46e77-e3a8-449d-913c-e30eb2afecdb")]
    public enum DockPosition
    {
        Top,
        Left,
        Bottom,
        Right,
        Fill,
        None
    }

    [ComVisible(true)]
    [Guid("159bc72c-4ad3-485e-9637-d7052edf0146")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IDockProvider
    {
        void SetDockPosition(DockPosition dockPosition);
        DockPosition DockPosition { get; }
    }
}
