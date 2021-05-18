using System;
using System.ComponentModel;

namespace Avalonia.LinuxFramebuffer.Output
{
    public sealed class GbmDevice : IDisposable
    {
        private IntPtr _gbmDevice;

        private bool _disposed;

        public GbmDevice(DrmCard drmCard)
        {
            _gbmDevice = LibDrm.gbm_create_device(drmCard.Fd);
            
            if (_gbmDevice == IntPtr.Zero)
                throw new Win32Exception("Could not create GBM Device for DRM card " + drmCard.Path);
        }

        ~GbmDevice() => Dispose(false);

        public IntPtr Handle => _gbmDevice;

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        internal IntPtr CreateSurface(int width, int height, uint format, LibDrm.GbmBoFlags flags)
        {
            return LibDrm.gbm_surface_create(_gbmDevice, width, height, format, flags);
        }
        
        private void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }
            
            ReleaseUnmanagedResources();

            _disposed = true;
        }

        private void ReleaseUnmanagedResources()
        {
            if (_gbmDevice != IntPtr.Zero)
            {
                LibDrm.gbm_device_destroy(_gbmDevice);
                _gbmDevice = IntPtr.Zero;
            }
        }
    }
}
