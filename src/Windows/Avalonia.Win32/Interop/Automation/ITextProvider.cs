using System;
using System.Runtime.InteropServices;

namespace Avalonia.Win32.Interop.Automation
{
    [Flags]
    [ComVisible(true)]
    [Guid("3d9e3d8f-bfb0-484f-84ab-93ff4280cbc4")]
    public enum SupportedTextSelection
    {
        None,
        Single,
        Multiple,
    }

    [ComVisible(true)]
    [Guid("3589c92c-63f3-4367-99bb-ada653b77cf2")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface ITextProvider
    {
        ITextRangeProvider [] GetSelection();
        ITextRangeProvider [] GetVisibleRanges();
        ITextRangeProvider RangeFromChild(IRawElementProviderSimple childElement);
        ITextRangeProvider RangeFromPoint(Point screenLocation);
        ITextRangeProvider DocumentRange { get; }
        SupportedTextSelection SupportedTextSelection { get; }
    }
}


