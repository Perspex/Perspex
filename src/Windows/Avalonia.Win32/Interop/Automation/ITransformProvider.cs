using System;
using System.Runtime.InteropServices;

namespace Avalonia.Win32.Interop.Automation
{
    [ComVisible(true)]
    [Guid("6829ddc4-4f91-4ffa-b86f-bd3e2987cb4c")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface ITransformProvider
    {
        void Move( double x, double y );
        void Resize( double width, double height );
        void Rotate( double degrees );
        bool CanMove  { [return: MarshalAs(UnmanagedType.Bool)] get; }
        bool CanResize { [return: MarshalAs(UnmanagedType.Bool)] get; }
        bool CanRotate { [return: MarshalAs(UnmanagedType.Bool)] get; }
    }
}
