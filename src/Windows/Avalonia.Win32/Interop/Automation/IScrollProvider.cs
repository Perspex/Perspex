using System;
using System.Runtime.InteropServices;
using Avalonia.Automation.Provider;

namespace Avalonia.Win32.Interop.Automation
{
    [ComVisible(true)]
    [Guid("b38b8077-1fc3-42a5-8cae-d40c2215055a")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IScrollProvider
    {
        void Scroll(ScrollAmount horizontalAmount, ScrollAmount verticalAmount);
        void SetScrollPercent(double horizontalPercent, double verticalPercent);
        double HorizontalScrollPercent { get; }
        double VerticalScrollPercent { get; }
        double HorizontalViewSize { get; }
        double VerticalViewSize { get; }
        bool HorizontallyScrollable { [return: MarshalAs(UnmanagedType.Bool)] get; }
        bool VerticallyScrollable { [return: MarshalAs(UnmanagedType.Bool)] get; }
    }
}
