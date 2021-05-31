using System;
using System.Runtime.InteropServices;

namespace Avalonia.Win32.Interop.Automation
{
    [ComVisible(true)]
    [Guid("fdc8f176-aed2-477a-8c89-ea04cc5f278d")]
    public enum WindowVisualState
    {
        Normal,
        Maximized,
        Minimized
    }

    [ComVisible(true)]
    [Guid("65101cc7-7904-408e-87a7-8c6dbd83a18b")]
    public enum WindowInteractionState
    {
        Running,
        Closing,
        ReadyForUserInteraction,
        BlockedByModalWindow,
        NotResponding
    }

    [ComVisible(true)]
    [Guid("987df77b-db06-4d77-8f8a-86a9c3bb90b9")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IWindowProvider
    {
        void SetVisualState(WindowVisualState state);
        void Close();
        [return: MarshalAs(UnmanagedType.Bool)]
        bool WaitForInputIdle(int milliseconds);
        bool Maximizable { [return: MarshalAs(UnmanagedType.Bool)] get; }
        bool Minimizable { [return: MarshalAs(UnmanagedType.Bool)] get; }
        bool IsModal { [return: MarshalAs(UnmanagedType.Bool)] get; }
        WindowVisualState VisualState { get; }
        WindowInteractionState InteractionState { get; }
        bool IsTopmost { [return: MarshalAs(UnmanagedType.Bool)] get; }
    }
}
