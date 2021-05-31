using System;
using System.Runtime.InteropServices;

namespace Avalonia.Win32.Interop.Automation
{
    [ComVisible(true)]
    [Guid("36dc7aef-33e6-4691-afe1-2be7274b3d33")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IRangeValueProvider
    {
        void SetValue(double value);
        double Value { get; }
        bool IsReadOnly { [return: MarshalAs(UnmanagedType.Bool)] get; }
        double Maximum { get; }
        double Minimum { get; }
        double LargeChange { get; }
        double SmallChange { get; }
    }
}
