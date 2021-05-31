using System;
using System.Runtime.InteropServices;

namespace Avalonia.Win32.Interop.Automation
{
    [ComVisible(true)]
    [Guid("15fdf2e2-9847-41cd-95dd-510612a025ea")]
    public enum RowOrColumnMajor
    {
        RowMajor,
        ColumnMajor,
        Indeterminate,
    }

    [ComVisible(true)]
    [Guid("9c860395-97b3-490a-b52a-858cc22af166")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface ITableProvider : IGridProvider
    {
        IRawElementProviderSimple [] GetRowHeaders();
        IRawElementProviderSimple [] GetColumnHeaders();
        RowOrColumnMajor RowOrColumnMajor { get; }
    }
}
