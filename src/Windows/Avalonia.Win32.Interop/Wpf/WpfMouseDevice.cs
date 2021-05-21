using System;
using Avalonia.Controls.Embedding;
using Avalonia.Input;
using Avalonia.VisualTree;

namespace Avalonia.Win32.Interop.Wpf
{
    class WpfMouseDevice : MouseDevice
    {
        private readonly WpfTopLevelImpl _impl;

        public WpfMouseDevice(WpfTopLevelImpl impl) : base(new WpfMousePointer(impl))
        {
            _impl = impl;
        }

        class WpfMousePointer : Pointer
        {
            private readonly WpfTopLevelImpl _impl;

            public WpfMousePointer(WpfTopLevelImpl impl) : base(Pointer.GetNextFreeId(), PointerType.Mouse, true)
            {
                _impl = impl;
            }

            protected override void PlatformCapture(IInputElement control)
            {
                if (control == null)
                {
                    System.Windows.Input.Mouse.Capture(null);
                }
                else if ((control.GetClosestVisual()?.GetVisualRoot() as EmbeddableControlRoot)?.PlatformImpl != _impl)
                    throw new ArgumentException("Visual belongs to unknown toplevel");
                else
                    System.Windows.Input.Mouse.Capture(_impl);
            }
        }
        
    }
}
