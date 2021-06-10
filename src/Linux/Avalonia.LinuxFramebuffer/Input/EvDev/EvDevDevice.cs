using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace Avalonia.LinuxFramebuffer.Input.EvDev
{
    unsafe class EvDevDevice
    {
        public int Fd { get; }
        private IntPtr _dev;
        public string Name { get; }
        public List<EvType> EventTypes { get; private set; } = new List<EvType>();
        public input_absinfo? AbsX { get; }
        public input_absinfo? AbsY { get; }

        public EvDevDevice(int fd, IntPtr dev)
        {
            Fd = fd;
            _dev = dev;
            Name = Marshal.PtrToStringAnsi(NativeUnsafeMethods.libevdev_get_name(_dev));
            foreach (EvType type in Enum.GetValues(typeof(EvType)))
            {
                if (NativeUnsafeMethods.libevdev_has_event_type(dev, type) != 0)
                    EventTypes.Add(type);
            }
            var ptr = NativeUnsafeMethods.libevdev_get_abs_info(dev, (int) AbsAxis.ABS_X);
            if (ptr != null)
                AbsX = *ptr;
            ptr = NativeUnsafeMethods.libevdev_get_abs_info(dev, (int)AbsAxis.ABS_Y);
            if (ptr != null)
                AbsY = *ptr;
        }
        
        public input_event? NextEvent()
        {
            input_event ev;
            if (NativeUnsafeMethods.libevdev_next_event(_dev, 2, out ev) == 0)
                return ev;
            return null;
        }

        public static EvDevDevice Open(string device)
        {
            var fd = NativeUnsafeMethods.open(device, 2048, 0);
            if (fd <= 0)
                throw new Exception($"Unable to open {device} code {Marshal.GetLastWin32Error()}");
            IntPtr dev;
            var rc = NativeUnsafeMethods.libevdev_new_from_fd(fd, out dev);
            if (rc < 0)
            {
                NativeUnsafeMethods.close(fd);
                throw new Exception($"Unable to initialize evdev for {device} code {Marshal.GetLastWin32Error()}");
            }
            return new EvDevDevice(fd, dev);
        }
    }

    internal class EvDevAxisInfo
    {
        public int Minimum { get; set; }
        public int Maximum { get; set; }
    }
}
