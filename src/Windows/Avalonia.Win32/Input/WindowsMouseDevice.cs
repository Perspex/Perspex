using System;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.VisualTree;
using Avalonia.Win32.Interop;

namespace Avalonia.Win32.Input
{
    class WindowsMouseDevice : MouseDevice
    {
        public WindowsMouseDevice() : base(new WindowsMousePointer())
        {
            
        }
        
        class WindowsMousePointer : Pointer
        {
            public WindowsMousePointer() : base(Pointer.GetNextFreeId(),PointerType.Mouse, true)
            {
            }

            protected override void PlatformCapture(IInputElement element)
            {
                var hwnd = ((element?.GetClosestVisual()?.GetVisualRoot() as TopLevel)?.PlatformImpl as WindowImpl)
                    ?.Handle.Handle;

                if (hwnd.HasValue && hwnd != IntPtr.Zero)
                    UnmanagedMethods.SetCapture(hwnd.Value);
                else
                    UnmanagedMethods.ReleaseCapture();
            }
        }
    }
}
