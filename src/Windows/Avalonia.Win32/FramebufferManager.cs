using System;
using System.Threading;
using Avalonia.Controls.Platform.Surfaces;
using Avalonia.Platform;
using Avalonia.Win32.Interop;

#nullable enable

namespace Avalonia.Win32
{
    internal class FramebufferManager : IFramebufferPlatformSurface, IDisposable
    {
        private const int _bytesPerPixel = 4;
        private const PixelFormat _format = PixelFormat.Bgra8888;

        private readonly IntPtr _hwnd;
        private readonly object _lock;
        private readonly Action _onDisposeAction;

        private FramebufferData? _framebufferData;

        public FramebufferManager(IntPtr hwnd)
        {
            _hwnd = hwnd;
            _lock = new object();
            _onDisposeAction = DrawAndUnlock;
        }

        public ILockedFramebuffer Lock()
        {
            Monitor.Enter(_lock);

            LockedFramebuffer? fb = null;

            try
            {
                UnmanagedMethods.GetClientRect(_hwnd, out var rc);

                var width = Math.Max(1, rc.right - rc.left);
                var height = Math.Max(1, rc.bottom - rc.top);

                if (_framebufferData is null || _framebufferData?.Size.Width != width || _framebufferData?.Size.Height != height)
                {
                    _framebufferData?.Dispose();

                    _framebufferData = AllocateFramebufferData(width, height);
                }

                var framebufferData = _framebufferData.Value;

                return fb = new LockedFramebuffer(
                    framebufferData.Data.Address, framebufferData.Size, framebufferData.RowBytes,
                    GetCurrentDpi(), _format, _onDisposeAction);
            }
            finally
            {
                // We free the lock when for whatever reason framebuffer was not created.
                // This allows for a potential retry later.
                if (fb is null)
                {
                    Monitor.Exit(_lock);
                }
            }
        }

        public void Dispose()
        {
            lock (_lock)
            {
                _framebufferData?.Dispose();
                _framebufferData = null;
            }
        }

        private void DrawAndUnlock()
        {
            try
            {
                if (_framebufferData.HasValue)
                    DrawToWindow(_hwnd, _framebufferData.Value);
            }
            finally
            {
                Monitor.Exit(_lock);
            }
        }

        private Vector GetCurrentDpi()
        {
            if (UnmanagedMethods.ShCoreAvailable && Win32Platform.WindowsVersion > PlatformConstants.Windows8)
            {
                var monitor =
                    UnmanagedMethods.MonitorFromWindow(_hwnd, UnmanagedMethods.MONITOR.MONITOR_DEFAULTTONEAREST);

                if (UnmanagedMethods.GetDpiForMonitor(
                    monitor,
                    UnmanagedMethods.MONITOR_DPI_TYPE.MDT_EFFECTIVE_DPI,
                    out var dpix,
                    out var dpiy) == 0)
                {
                    return new Vector(dpix, dpiy);
                }
            }

            return new Vector(96, 96);
        }

        private static FramebufferData AllocateFramebufferData(int width, int height)
        {
            var bitmapBlob = AvaloniaLocator.Current.GetService<IRuntimePlatform>().AllocBlob(width * height * _bytesPerPixel);

            return new FramebufferData(bitmapBlob, width, height);
        }

        private static void DrawToDevice(FramebufferData framebufferData, IntPtr hDC, int destX = 0, int destY = 0, int srcX = 0,
            int srcY = 0, int width = -1,
            int height = -1)
        {
            if (width == -1)
                width = framebufferData.Size.Width;
            if (height == -1)
                height = framebufferData.Size.Height;

            var bmpInfo = framebufferData.Header;

            UnmanagedMethods.SetDIBitsToDevice(hDC, destX, destY, (uint)width, (uint)height, srcX, srcY,
                0, (uint)framebufferData.Size.Height, framebufferData.Data.Address, ref bmpInfo, 0);
        }

        private static bool DrawToWindow(IntPtr hWnd, FramebufferData framebufferData, int destX = 0, int destY = 0, int srcX = 0,
            int srcY = 0, int width = -1,
            int height = -1)
        {
            if (framebufferData.Data.IsDisposed)
                throw new ObjectDisposedException("Framebuffer");

            if (hWnd == IntPtr.Zero)
                return false;

            var hDC = UnmanagedMethods.GetDC(hWnd);

            if (hDC == IntPtr.Zero)
                return false;

            try
            {
                DrawToDevice(framebufferData, hDC, destX, destY, srcX, srcY, width, height);
            }
            finally
            {
                UnmanagedMethods.ReleaseDC(hWnd, hDC);
            }

            return true;
        }

        private readonly struct FramebufferData
        {
            public IUnmanagedBlob Data { get; }

            public PixelSize Size { get; }

            public int RowBytes => Size.Width * _bytesPerPixel;

            public UnmanagedMethods.BITMAPINFOHEADER Header { get; }

            public FramebufferData(IUnmanagedBlob data, int width, int height)
            {
                Data = data;
                Size = new PixelSize(width, height);

                var header = new UnmanagedMethods.BITMAPINFOHEADER();
                header.Init();

                header.biPlanes = 1;
                header.biBitCount = _bytesPerPixel * 8;
                header.Init();

                header.biWidth = width;
                header.biHeight = -height;

                Header = header;
            }

            public void Dispose()
            {
                Data.Dispose();
            }
        }
    }
}
