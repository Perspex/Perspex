using System;
using Avalonia.Platform;
using Avalonia.Win32.Interop;
using SharpDX;
using SharpDX.DXGI;

namespace Avalonia.Direct2D1
{
    class HwndRenderTarget : SwapChainRenderTarget
    {
        private readonly IPlatformHandle _window;

        public HwndRenderTarget(IPlatformHandle window)
        {
            _window = window;
        }

        protected override SwapChain1 CreateSwapChain(Factory2 dxgiFactory, SwapChainDescription1 swapChainDesc)
        {
            return new SwapChain1(dxgiFactory, Direct2D1Platform.DxgiDevice, _window.Handle, ref swapChainDesc);
        }

        protected override Size2F GetWindowDpi()
        {
            if (UnmanagedMethods.ShCoreAvailable && Environment.OSVersion.Version > PlatformConstants.Windows8)
            {
                uint dpix, dpiy;

                var monitor = UnmanagedMethods.MonitorFromWindow(
                    _window.Handle,
                    UnmanagedMethods.MONITOR.MONITOR_DEFAULTTONEAREST);

                if (UnmanagedMethods.GetDpiForMonitor(
                        monitor,
                        UnmanagedMethods.MONITOR_DPI_TYPE.MDT_EFFECTIVE_DPI,
                        out dpix,
                        out dpiy) == 0)
                {
                    return new Size2F(dpix, dpiy);
                }
            }

            return new Size2F(96, 96);
        }

        protected override Size2 GetWindowSize()
        {
            UnmanagedMethods.RECT rc;
            UnmanagedMethods.GetClientRect(_window.Handle, out rc);
            return new Size2(rc.right - rc.left, rc.bottom - rc.top);
        }
    }
}
