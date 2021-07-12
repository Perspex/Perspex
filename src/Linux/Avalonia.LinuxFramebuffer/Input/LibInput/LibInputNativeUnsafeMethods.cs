using System;
using System.Runtime.InteropServices;

namespace Avalonia.LinuxFramebuffer.Input.LibInput
{
    unsafe class LibInputNativeUnsafeMethods
    {
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate int OpenRestrictedCallbackDelegate(IntPtr path, int flags, IntPtr userData);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void CloseRestrictedCallbackDelegate(int fd, IntPtr userData);

        static int OpenRestricted(IntPtr path, int flags, IntPtr userData)
        {
            var fd = NativeUnsafeMethods.open(Marshal.PtrToStringAnsi(path), flags, 0);
            if (fd == -1)
                return -Marshal.GetLastWin32Error();

            return fd;
        }

        static void CloseRestricted(int fd, IntPtr userData)
        {
            NativeUnsafeMethods.close(fd);
        }

        private static readonly IntPtr* s_Interface;

        static LibInputNativeUnsafeMethods()
        {
            s_Interface = (IntPtr*)Marshal.AllocHGlobal(IntPtr.Size * 2);

            IntPtr Convert<TDelegate>(TDelegate del)
            {
                GCHandle.Alloc(del);
                return Marshal.GetFunctionPointerForDelegate(del);
            }

            s_Interface[0] = Convert<OpenRestrictedCallbackDelegate>(OpenRestricted);
            s_Interface[1] = Convert<CloseRestrictedCallbackDelegate>(CloseRestricted);
        }

        private const string LibInput = "libinput.so.10";
        
        [DllImport(LibInput)]
        public extern static IntPtr libinput_path_create_context(IntPtr* iface, IntPtr userData);

        public static IntPtr libinput_path_create_context() =>
            libinput_path_create_context(s_Interface, IntPtr.Zero);

        [DllImport(LibInput)]
        public extern static IntPtr libinput_path_add_device(IntPtr ctx, [MarshalAs(UnmanagedType.LPStr)] string path);
        
        [DllImport(LibInput)]
        public extern static IntPtr libinput_path_remove_device(IntPtr device);
        
        [DllImport(LibInput)]
        public extern static int libinput_get_fd(IntPtr ctx);
        
        [DllImport(LibInput)]
        public extern static void libinput_dispatch(IntPtr ctx);
        
        [DllImport(LibInput)]
        public extern static IntPtr libinput_get_event(IntPtr ctx);
        
        [DllImport(LibInput)]
        public extern static LibInputEventType libinput_event_get_type(IntPtr ev);

        public enum LibInputEventType
        {
            LIBINPUT_EVENT_NONE = 0,
            LIBINPUT_EVENT_DEVICE_ADDED,
            LIBINPUT_EVENT_DEVICE_REMOVED,
            LIBINPUT_EVENT_KEYBOARD_KEY = 300,
            LIBINPUT_EVENT_POINTER_MOTION = 400,
            LIBINPUT_EVENT_POINTER_MOTION_ABSOLUTE,
            LIBINPUT_EVENT_POINTER_BUTTON,
            LIBINPUT_EVENT_POINTER_AXIS,
            LIBINPUT_EVENT_TOUCH_DOWN = 500,
            LIBINPUT_EVENT_TOUCH_UP,
            LIBINPUT_EVENT_TOUCH_MOTION,
            LIBINPUT_EVENT_TOUCH_CANCEL,
            LIBINPUT_EVENT_TOUCH_FRAME,
            LIBINPUT_EVENT_TABLET_TOOL_AXIS = 600,
            LIBINPUT_EVENT_TABLET_TOOL_PROXIMITY,
            LIBINPUT_EVENT_TABLET_TOOL_TIP,
            LIBINPUT_EVENT_TABLET_TOOL_BUTTON,
            LIBINPUT_EVENT_TABLET_PAD_BUTTON = 700,
            LIBINPUT_EVENT_TABLET_PAD_RING,
            LIBINPUT_EVENT_TABLET_PAD_STRIP,
            LIBINPUT_EVENT_GESTURE_SWIPE_BEGIN = 800,
            LIBINPUT_EVENT_GESTURE_SWIPE_UPDATE,
            LIBINPUT_EVENT_GESTURE_SWIPE_END,
            LIBINPUT_EVENT_GESTURE_PINCH_BEGIN,
            LIBINPUT_EVENT_GESTURE_PINCH_UPDATE,
            LIBINPUT_EVENT_GESTURE_PINCH_END,
            LIBINPUT_EVENT_SWITCH_TOGGLE = 900,
        }
        
        
        [DllImport(LibInput)]
        public extern static void libinput_event_destroy(IntPtr ev);
        
        [DllImport(LibInput)]
        public extern static IntPtr libinput_event_get_touch_event(IntPtr ev);
        
        [DllImport(LibInput)]
        public extern static int libinput_event_touch_get_slot(IntPtr ev);
        
        [DllImport(LibInput)]
        public extern static ulong libinput_event_touch_get_time_usec(IntPtr ev);

        [DllImport(LibInput)]
        public extern static double libinput_event_touch_get_x_transformed(IntPtr ev, int width);

        [DllImport(LibInput)]
        public extern static double libinput_event_pointer_get_dy(IntPtr ev);
        
        [DllImport(LibInput)]
        public extern static double libinput_event_pointer_get_dx(IntPtr ev);
        
        [DllImport(LibInput)]
        public extern static double libinput_event_touch_get_y_transformed(IntPtr ev, int height);
        
        [DllImport(LibInput)]
        public extern static IntPtr libinput_event_get_pointer_event(IntPtr ev);
        
        
        [DllImport(LibInput)]
        public extern static ulong libinput_event_pointer_get_time_usec(IntPtr ev);
        
        [DllImport(LibInput)]
        public extern static double libinput_event_pointer_get_absolute_x_transformed(IntPtr ev, int width);
        
        [DllImport(LibInput)]
        public extern static double libinput_event_pointer_get_absolute_y_transformed(IntPtr ev, int height);
        
        [DllImport(LibInput)]
        public extern static int libinput_event_pointer_get_button(IntPtr ev);
        
        [DllImport(LibInput)]
        public extern static int libinput_event_pointer_get_button_state(IntPtr ev);
    }
}
