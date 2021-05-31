using System;
using System.Runtime.InteropServices;

#nullable enable

namespace Avalonia.Win32.Interop.Automation
{
    [ComVisible(true)]
    [Guid("c7935180-6fb3-4201-b174-7df73adbf64a")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IValueProvider
    {
        void SetValue([MarshalAs(UnmanagedType.LPWStr)] string? value);
        string? Value { get; }
        bool IsReadOnly { [return: MarshalAs(UnmanagedType.Bool)] get; }
    }
}
