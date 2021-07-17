using System;

namespace Avalonia.Win32
{
    static class PlatformConstants
    {
        public const string WindowHandleType = "HWND";
        public const string CursorHandleType = "HCURSOR";

        public static readonly Version Windows8 = new Version(6, 2);
    }
}
