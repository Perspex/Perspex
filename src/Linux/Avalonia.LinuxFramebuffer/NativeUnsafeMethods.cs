using System;
using System.Runtime.InteropServices;
using __s32 = System.Int32;
using __u16 = System.UInt16;
using __u32 = System.UInt32;
// ReSharper disable FieldCanBeMadeReadOnly.Local
// ReSharper disable ArrangeTypeMemberModifiers
// ReSharper disable BuiltInTypeReferenceStyle
// ReSharper disable InconsistentNaming

namespace Avalonia.LinuxFramebuffer
{
    unsafe class NativeUnsafeMethods
    {
        [DllImport("libc", EntryPoint = "open", SetLastError = true)]
        public static extern int open(string pathname, int flags, int mode);

        [DllImport("libc", EntryPoint = "close", SetLastError = true)]
        public static extern int close(int fd);

        [DllImport("libc", EntryPoint = "ioctl", SetLastError = true)]
        public static extern int ioctl(int fd, FbIoCtl code, void* arg);

        [DllImport("libc", EntryPoint = "mmap", SetLastError = true)]
        public static extern IntPtr mmap(IntPtr addr, IntPtr length, int prot, int flags,
                  int fd, IntPtr offset);
        [DllImport("libc", EntryPoint = "munmap", SetLastError = true)]
        public static extern int munmap(IntPtr addr, IntPtr length);

        [DllImport("libc", EntryPoint = "memcpy", SetLastError = true)]
        public static extern int memcpy(IntPtr dest, IntPtr src, IntPtr length);

        [DllImport("libc", EntryPoint = "select", SetLastError = true)]
        public static extern int select(int nfds, void* rfds, void* wfds, void* exfds, IntPtr* timevals);


        [DllImport("libc", EntryPoint = "poll", SetLastError = true)]
        public static extern int poll(pollfd* fds, IntPtr nfds, int timeout);

        [DllImport("libevdev.so.2", EntryPoint = "libevdev_new_from_fd", SetLastError = true)]
        public static extern int libevdev_new_from_fd(int fd, out IntPtr dev);

        [DllImport("libevdev.so.2", EntryPoint = "libevdev_has_event_type", SetLastError = true)]
        public static extern int libevdev_has_event_type(IntPtr dev, EvType type);

        [DllImport("libevdev.so.2", EntryPoint = "libevdev_next_event", SetLastError = true)]
        public static extern int libevdev_next_event(IntPtr dev, int flags, out input_event ev);

        [DllImport("libevdev.so.2", EntryPoint = "libevdev_get_name", SetLastError = true)]
        public static extern IntPtr libevdev_get_name(IntPtr dev);
        [DllImport("libevdev.so.2", EntryPoint = "libevdev_get_abs_info", SetLastError = true)]
        public static extern input_absinfo* libevdev_get_abs_info(IntPtr dev, int code);
        
        [DllImport("libc")]
        public extern static int epoll_create1(int size);

        [DllImport("libc")]
        public extern static int epoll_ctl(int epfd, int op, int fd, ref epoll_event __event);

        [DllImport("libc")]
        public extern static int epoll_wait(int epfd, epoll_event* events, int maxevents, int timeout);
        
        public const int EPOLLIN = 1;
        public const int EPOLL_CTL_ADD = 1;
        public const int O_NONBLOCK = 2048;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct pollfd {
        public int   fd;         /* file descriptor */
        public short events;     /* requested events */
        public short revents;    /* returned events */
    };
    
    enum FbIoCtl : uint
    {
        FBIOGET_VSCREENINFO = 0x4600,
        FBIOPUT_VSCREENINFO = 0x4601,
        FBIOGET_FSCREENINFO = 0x4602,
        FBIOGET_VBLANK = 0x80204612u,
        FBIO_WAITFORVSYNC = 0x40044620,
        FBIOPAN_DISPLAY = 0x4606
    }

    [Flags]
    enum VBlankFlags
    {
        FB_VBLANK_VBLANKING = 0x001 /* currently in a vertical blank */,
        FB_VBLANK_HBLANKING = 0x002 /* currently in a horizontal blank */,
        FB_VBLANK_HAVE_VBLANK = 0x004 /* vertical blanks can be detected */,
        FB_VBLANK_HAVE_HBLANK = 0x008 /* horizontal blanks can be detected */,
        FB_VBLANK_HAVE_COUNT = 0x010 /* global retrace counter is available */,
        FB_VBLANK_HAVE_VCOUNT = 0x020 /* the vcount field is valid */,
        FB_VBLANK_HAVE_HCOUNT = 0x040 /* the hcount field is valid */,
        FB_VBLANK_VSYNCING = 0x080 /* currently in a vsync */,
        FB_VBLANK_HAVE_VSYNC = 0x100 /* vertical syncs can be detected */
    }

    [StructLayout(LayoutKind.Sequential)]
    unsafe struct fb_vblank {
        public VBlankFlags flags;			/* FB_VBLANK flags */
        __u32 count;			/* counter of retraces since boot */
        __u32 vcount;			/* current scanline position */
        __u32 hcount;			/* current scandot position */
        fixed __u32 reserved[4];		/* reserved for future compatibility */
    };

    [StructLayout(LayoutKind.Sequential)]
    unsafe struct fb_fix_screeninfo
    {
        public fixed byte id[16]; /* identification string eg "TT Builtin" */

        public IntPtr smem_start; /* Start of frame buffer mem */

        /* (physical address) */
        public __u32 smem_len; /* Length of frame buffer mem */

        public __u32 type; /* see FB_TYPE_*		*/
        public __u32 type_aux; /* Interleave for interleaved Planes */
        public __u32 visual; /* see FB_VISUAL_*		*/
        public __u16 xpanstep; /* zero if no hardware panning  */
        public __u16 ypanstep; /* zero if no hardware panning  */
        public __u16 ywrapstep; /* zero if no hardware ywrap    */
        public __u32 line_length; /* length of a line in bytes    */

        public IntPtr mmio_start; /* Start of Memory Mapped I/O   */

        /* (physical address) */
        public __u32 mmio_len; /* Length of Memory Mapped I/O  */

        public __u32 accel; /* Type of acceleration available */
        public fixed __u16 reserved[3]; /* Reserved for future compatibility */
    };

    [StructLayout(LayoutKind.Sequential)]
    struct fb_bitfield
    {
        public __u32 offset; /* beginning of bitfield	*/
        public __u32 length; /* length of bitfield		*/

        public __u32 msb_right; /* != 0 : Most significant bit is */
        /* right */
    };

    [StructLayout(LayoutKind.Sequential)]
    unsafe struct fb_var_screeninfo
    {
        public __u32 xres; /* visible resolution		*/
        public __u32 yres;
        public __u32 xres_virtual; /* virtual resolution		*/
        public __u32 yres_virtual;
        public __u32 xoffset; /* offset from virtual to visible */
        public __u32 yoffset; /* resolution			*/

        public __u32 bits_per_pixel; /* guess what			*/
        public __u32 grayscale; /* != 0 Graylevels instead of colors */

        public fb_bitfield red; /* bitfield in fb mem if true color, */
        public fb_bitfield green; /* else only length is significant */
        public fb_bitfield blue;
        public fb_bitfield transp; /* transparency			*/

        public __u32 nonstd; /* != 0 Non standard pixel format */

        public __u32 activate; /* see FB_ACTIVATE_*		*/

        public __u32 height; /* height of picture in mm    */
        public __u32 width; /* width of picture in mm     */

        public __u32 accel_flags; /* acceleration flags (hints)	*/

        /* Timing: All values in pixclocks, except pixclock (of course) */
        public __u32 pixclock; /* pixel clock in ps (pico seconds) */

        public __u32 left_margin; /* time from sync to picture	*/
        public __u32 right_margin; /* time from picture to sync	*/
        public __u32 upper_margin; /* time from sync to picture	*/
        public __u32 lower_margin;
        public __u32 hsync_len; /* length of horizontal sync	*/
        public __u32 vsync_len; /* length of vertical sync	*/
        public __u32 sync; /* see FB_SYNC_*		*/
        public __u32 vmode; /* see FB_VMODE_*		*/
        public fixed __u32 reserved[6]; /* Reserved for future compatibility */
    };


    enum EvType
    {
        EV_SYN = 0x00,
        EV_KEY = 0x01,
        EV_REL = 0x02,
        EV_ABS = 0x03,
        EV_MSC = 0x04,
        EV_SW = 0x05,
        EV_LED = 0x11,
        EV_SND = 0x12,
        EV_REP = 0x14,
        EV_FF = 0x15,
        EV_PWR = 0x16,
        EV_FF_STATUS = 0x17,
    }

    [StructLayout(LayoutKind.Sequential)]
    struct input_event
    {
        private IntPtr timeval1, timeval2;
        public ushort _type, _code;
        public int value;
        public EvType Type => (EvType)_type;
        public EvKey Key => (EvKey)_code;
        public AbsAxis Axis => (AbsAxis)_code;

        public ulong Timestamp
        {
            get
            {
                var ms = (ulong)timeval2.ToInt64() / 1000;
                var s = (ulong)timeval1.ToInt64() * 1000;
                return s + ms;
            }
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    unsafe struct fd_set
    {
        public int count;
        public fixed byte fds [256];
    }

    enum AxisEventCode
    {
        REL_X = 0x00,
        REL_Y = 0x01,
        REL_Z = 0x02,
        REL_RX = 0x03,
        REL_RY = 0x04,
        REL_RZ = 0x05,
        REL_HWHEEL = 0x06,
        REL_DIAL = 0x07,
        REL_WHEEL = 0x08,
        REL_MISC = 0x09,
        REL_MAX = 0x0f
    }

    enum AbsAxis
    {
        ABS_X = 0x00,
        ABS_Y = 0x01,
        ABS_Z = 0x02,
        ABS_RX = 0x03,
        ABS_RY = 0x04,
        ABS_RZ = 0x05,
        ABS_THROTTLE = 0x06,
        ABS_RUDDER = 0x07,
        ABS_WHEEL = 0x08,
        ABS_GAS = 0x09,
        ABS_BRAKE = 0x0a,
        ABS_HAT0X = 0x10,
        ABS_HAT0Y = 0x11,
        ABS_HAT1X = 0x12,
        ABS_HAT1Y = 0x13,
        ABS_HAT2X = 0x14,
        ABS_HAT2Y = 0x15,
        ABS_HAT3X = 0x16,
        ABS_HAT3Y = 0x17,
        ABS_PRESSURE = 0x18,
        ABS_DISTANCE = 0x19,
        ABS_TILT_X = 0x1a,
        ABS_TILT_Y = 0x1b,
        ABS_TOOL_WIDTH = 0x1c
    }

    enum EvKey
    {
        BTN_LEFT = 0x110,
        BTN_RIGHT = 0x111,
        BTN_MIDDLE = 0x112,
        BTN_TOUCH = 0x14a
    }

    [StructLayout(LayoutKind.Sequential)]
    struct input_absinfo
    {
        public __s32 value;
        public __s32 minimum;
        public __s32 maximum;
        public __s32 fuzz;
        public __s32 flat;
        public __s32 resolution;

    }
    
    [StructLayout(LayoutKind.Explicit)]
    struct epoll_data
    {
        [FieldOffset(0)]
        public IntPtr ptr;
        [FieldOffset(0)]
        public int fd;
        [FieldOffset(0)]
        public uint u32;
        [FieldOffset(0)]
        public ulong u64;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct epoll_event
    {
        public uint events;
        public epoll_data data;
    }
}
