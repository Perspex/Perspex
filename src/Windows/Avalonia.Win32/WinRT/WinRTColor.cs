using System.Runtime.InteropServices;

using Avalonia.Media;

namespace Avalonia.Win32.WinRT
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct WinRTColor
    {
        public byte A;
        public byte R;
        public byte G;
        public byte B;

        public static WinRTColor FromArgb(byte a, byte r, byte g, byte b) => new WinRTColor()
        {
            A = a, R = r, G = g, B = b
        };

        public static implicit operator Color(WinRTColor color)
        {
            return new Color(color.A, color.R, color.G, color.B);
        }
    }
}
