using System;
using System.Runtime.InteropServices;
using System.Text;
using Avalonia.Controls.Platform.Surfaces;
using Avalonia.LinuxFramebuffer.Output;
using Avalonia.Platform;

namespace Avalonia.LinuxFramebuffer
{
    public sealed unsafe class FbdevOutput : IFramebufferPlatformSurface, IDisposable, IOutputBackend
    {
        private int _fd;
        private fb_fix_screeninfo _fixedInfo;
        private fb_var_screeninfo _varInfo;
        private IntPtr _mappedLength;
        private IntPtr _mappedAddress;

        public FbdevOutput(string fileName = null)
        {
            fileName = fileName ?? Environment.GetEnvironmentVariable("FRAMEBUFFER") ?? "/dev/fb0";
            _fd = NativeUnsafeMethods.open(fileName, 2, 0);
            if (_fd <= 0)
                throw new Exception("Error: " + Marshal.GetLastWin32Error());

            try
            {
                Init();
            }
            catch
            {
                Dispose();
                throw;
            }
        }
        
        /// <inheritdoc />
        public double Scaling { get; set; }
        
        /// <inheritdoc />
        public string Name => "fbdev";

        void Init()
        {
            fixed (void* pnfo = &_varInfo)
            {
                if (-1 == NativeUnsafeMethods.ioctl(_fd, FbIoCtl.FBIOGET_VSCREENINFO, pnfo))
                    throw new Exception("FBIOGET_VSCREENINFO error: " + Marshal.GetLastWin32Error());

                SetBpp();

                if (-1 == NativeUnsafeMethods.ioctl(_fd, FbIoCtl.FBIOPUT_VSCREENINFO, pnfo))
                    _varInfo.transp = new fb_bitfield();

                NativeUnsafeMethods.ioctl(_fd, FbIoCtl.FBIOPUT_VSCREENINFO, pnfo);

                if (-1 == NativeUnsafeMethods.ioctl(_fd, FbIoCtl.FBIOGET_VSCREENINFO, pnfo))
                    throw new Exception("FBIOGET_VSCREENINFO error: " + Marshal.GetLastWin32Error());

                if (_varInfo.bits_per_pixel != 32)
                    throw new Exception("Unable to set 32-bit display mode");
            }
            fixed(void*pnfo = &_fixedInfo)
                if (-1 == NativeUnsafeMethods.ioctl(_fd, FbIoCtl.FBIOGET_FSCREENINFO, pnfo))
                    throw new Exception("FBIOGET_FSCREENINFO error: " + Marshal.GetLastWin32Error());

            _mappedLength = new IntPtr(_fixedInfo.line_length * _varInfo.yres);
            _mappedAddress = NativeUnsafeMethods.mmap(IntPtr.Zero, _mappedLength, 3, 1, _fd, IntPtr.Zero);
            if (_mappedAddress == new IntPtr(-1))
                throw new Exception($"Unable to mmap {_mappedLength} bytes, error {Marshal.GetLastWin32Error()}");
            fixed (fb_fix_screeninfo* pnfo = &_fixedInfo)
            {
                int idlen;
                for (idlen = 0; idlen < 16 && pnfo->id[idlen] != 0; idlen++) ;
                Id = Encoding.ASCII.GetString(pnfo->id, idlen);
            }
        }

        void SetBpp()
        {
            _varInfo.bits_per_pixel = 32;
            _varInfo.grayscale = 0;
            _varInfo.red = _varInfo.blue = _varInfo.green = _varInfo.transp = new fb_bitfield
            {
                length = 8
            };
            _varInfo.green.offset = 8;
            _varInfo.blue.offset = 16;
            _varInfo.transp.offset = 24;
        }

        public string Id { get; private set; }

        public PixelSize PixelSize
        {
            get
            {
                fb_var_screeninfo nfo;
                if (-1 == NativeUnsafeMethods.ioctl(_fd, FbIoCtl.FBIOGET_VSCREENINFO, &nfo))
                    throw new Exception("FBIOGET_VSCREENINFO error: " + Marshal.GetLastWin32Error());
                return new PixelSize((int)nfo.xres, (int)nfo.yres);
            }
        }

        public ILockedFramebuffer Lock()
        {
            if (_fd <= 0)
                throw new ObjectDisposedException("LinuxFramebuffer");
            return new LockedFramebuffer(_fd, _fixedInfo, _varInfo, _mappedAddress, new Vector(96, 96) * Scaling);
        }


        private void ReleaseUnmanagedResources()
        {
            if (_mappedAddress != IntPtr.Zero)
            {
                NativeUnsafeMethods.munmap(_mappedAddress, _mappedLength);
                _mappedAddress = IntPtr.Zero;
            }
            if(_fd == 0)
                return;
            NativeUnsafeMethods.close(_fd);
            _fd = 0;
        }

        public void Dispose()
        {
            ReleaseUnmanagedResources();
            GC.SuppressFinalize(this);
        }

        ~FbdevOutput()
        {
            ReleaseUnmanagedResources();
        }
    }
}
