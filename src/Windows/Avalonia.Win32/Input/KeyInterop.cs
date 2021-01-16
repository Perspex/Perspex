using System.Collections.Generic;
using Avalonia.Input;
using Avalonia.Win32.Interop;

namespace Avalonia.Win32.Input
{
    static class KeyInterop
    {
        private static readonly Dictionary<Key, int> s_virtualKeyFromKey = new Dictionary<Key, int>
        {
            { Key.None, 0 },
            { Key.Cancel, 3 },
            { Key.Back, 8 },
            { Key.Tab, 9 },
            { Key.LineFeed, 0 },
            { Key.Clear, 12 },
            { Key.Return, 13 },
            { Key.Pause, 19 },
            { Key.Capital, 20 },
            { Key.KanaMode, 21 },
            { Key.JunjaMode, 23 },
            { Key.FinalMode, 24 },
            { Key.HanjaMode, 25 },
            { Key.Escape, 27 },
            { Key.ImeConvert, 28 },
            { Key.ImeNonConvert, 29 },
            { Key.ImeAccept, 30 },
            { Key.ImeModeChange, 31 },
            { Key.Space, 32 },
            { Key.PageUp, 33 },
            { Key.Next, 34 },
            { Key.End, 35 },
            { Key.Home, 36 },
            { Key.Left, 37 },
            { Key.Up, 38 },
            { Key.Right, 39 },
            { Key.Down, 40 },
            { Key.Select, 41 },
            { Key.Print, 42 },
            { Key.Execute, 43 },
            { Key.Snapshot, 44 },
            { Key.Insert, 45 },
            { Key.Delete, 46 },
            { Key.Help, 47 },
            { Key.D0, 48 },
            { Key.D1, 49 },
            { Key.D2, 50 },
            { Key.D3, 51 },
            { Key.D4, 52 },
            { Key.D5, 53 },
            { Key.D6, 54 },
            { Key.D7, 55 },
            { Key.D8, 56 },
            { Key.D9, 57 },
            { Key.A, 65 },
            { Key.B, 66 },
            { Key.C, 67 },
            { Key.D, 68 },
            { Key.E, 69 },
            { Key.F, 70 },
            { Key.G, 71 },
            { Key.H, 72 },
            { Key.I, 73 },
            { Key.J, 74 },
            { Key.K, 75 },
            { Key.L, 76 },
            { Key.M, 77 },
            { Key.N, 78 },
            { Key.O, 79 },
            { Key.P, 80 },
            { Key.Q, 81 },
            { Key.R, 82 },
            { Key.S, 83 },
            { Key.T, 84 },
            { Key.U, 85 },
            { Key.V, 86 },
            { Key.W, 87 },
            { Key.X, 88 },
            { Key.Y, 89 },
            { Key.Z, 90 },
            { Key.LWin, 91 },
            { Key.RWin, 92 },
            { Key.Apps, 93 },
            { Key.Sleep, 95 },
            { Key.NumPad0, 96 },
            { Key.NumPad1, 97 },
            { Key.NumPad2, 98 },
            { Key.NumPad3, 99 },
            { Key.NumPad4, 100 },
            { Key.NumPad5, 101 },
            { Key.NumPad6, 102 },
            { Key.NumPad7, 103 },
            { Key.NumPad8, 104 },
            { Key.NumPad9, 105 },
            { Key.Multiply, 106 },
            { Key.Add, 107 },
            { Key.Separator, 108 },
            { Key.Subtract, 109 },
            { Key.Decimal, 110 },
            { Key.Divide, 111 },
            { Key.F1, 112 },
            { Key.F2, 113 },
            { Key.F3, 114 },
            { Key.F4, 115 },
            { Key.F5, 116 },
            { Key.F6, 117 },
            { Key.F7, 118 },
            { Key.F8, 119 },
            { Key.F9, 120 },
            { Key.F10, 121 },
            { Key.F11, 122 },
            { Key.F12, 123 },
            { Key.F13, 124 },
            { Key.F14, 125 },
            { Key.F15, 126 },
            { Key.F16, 127 },
            { Key.F17, 128 },
            { Key.F18, 129 },
            { Key.F19, 130 },
            { Key.F20, 131 },
            { Key.F21, 132 },
            { Key.F22, 133 },
            { Key.F23, 134 },
            { Key.F24, 135 },
            { Key.NumLock, 144 },
            { Key.Scroll, 145 },
            { Key.LeftShift, 160 },
            { Key.RightShift, 161 },
            { Key.LeftCtrl, 162 },
            { Key.RightCtrl, 163 },
            { Key.LeftAlt, 164 },
            { Key.RightAlt, 165 },
            { Key.BrowserBack, 166 },
            { Key.BrowserForward, 167 },
            { Key.BrowserRefresh, 168 },
            { Key.BrowserStop, 169 },
            { Key.BrowserSearch, 170 },
            { Key.BrowserFavorites, 171 },
            { Key.BrowserHome, 172 },
            { Key.VolumeMute, 173 },
            { Key.VolumeDown, 174 },
            { Key.VolumeUp, 175 },
            { Key.MediaNextTrack, 176 },
            { Key.MediaPreviousTrack, 177 },
            { Key.MediaStop, 178 },
            { Key.MediaPlayPause, 179 },
            { Key.LaunchMail, 180 },
            { Key.SelectMedia, 181 },
            { Key.LaunchApplication1, 182 },
            { Key.LaunchApplication2, 183 },
            { Key.Oem1, 186 },
            { Key.OemPlus, 187 },
            { Key.OemComma, 188 },
            { Key.OemMinus, 189 },
            { Key.OemPeriod, 190 },
            { Key.OemQuestion, 191 },
            { Key.Oem3, 192 },
            { Key.AbntC1, 193 },
            { Key.AbntC2, 194 },
            { Key.OemOpenBrackets, 219 },
            { Key.Oem5, 220 },
            { Key.Oem6, 221 },
            { Key.OemQuotes, 222 },
            { Key.Oem8, 223 },
            { Key.OemBackslash, 226 },
            { Key.ImeProcessed, 229 },
            { Key.System, 0 },
            { Key.OemAttn, 240 },
            { Key.OemFinish, 241 },
            { Key.OemCopy, 242 },
            { Key.DbeSbcsChar, 243 },
            { Key.OemEnlw, 244 },
            { Key.OemBackTab, 245 },
            { Key.DbeNoRoman, 246 },
            { Key.DbeEnterWordRegisterMode, 247 },
            { Key.DbeEnterImeConfigureMode, 248 },
            { Key.EraseEof, 249 },
            { Key.Play, 250 },
            { Key.DbeNoCodeInput, 251 },
            { Key.NoName, 252 },
            { Key.Pa1, 253 },
            { Key.OemClear, 254 },
            { Key.DeadCharProcessed, 0 },
        };

        private static readonly Dictionary<int, Key> s_keyFromVirtualKey = new Dictionary<int, Key>
        {
            { 0, Key.None },
            { 3, Key.Cancel },
            { 8, Key.Back },
            { 9, Key.Tab },
            { 12, Key.Clear },
            { 13, Key.Return },
            { 16, Key.LeftShift},
            { 17, Key.LeftCtrl},
            { 18, Key.LeftAlt },
            { 19, Key.Pause },
            { 20, Key.Capital },
            { 21, Key.KanaMode },
            { 23, Key.JunjaMode },
            { 24, Key.FinalMode },
            { 25, Key.HanjaMode },
            { 27, Key.Escape },
            { 28, Key.ImeConvert },
            { 29, Key.ImeNonConvert },
            { 30, Key.ImeAccept },
            { 31, Key.ImeModeChange },
            { 32, Key.Space },
            { 33, Key.PageUp },
            { 34, Key.PageDown },
            { 35, Key.End },
            { 36, Key.Home },
            { 37, Key.Left },
            { 38, Key.Up },
            { 39, Key.Right },
            { 40, Key.Down },
            { 41, Key.Select },
            { 42, Key.Print },
            { 43, Key.Execute },
            { 44, Key.Snapshot },
            { 45, Key.Insert },
            { 46, Key.Delete },
            { 47, Key.Help },
            { 48, Key.D0 },
            { 49, Key.D1 },
            { 50, Key.D2 },
            { 51, Key.D3 },
            { 52, Key.D4 },
            { 53, Key.D5 },
            { 54, Key.D6 },
            { 55, Key.D7 },
            { 56, Key.D8 },
            { 57, Key.D9 },
            { 65, Key.A },
            { 66, Key.B },
            { 67, Key.C },
            { 68, Key.D },
            { 69, Key.E },
            { 70, Key.F },
            { 71, Key.G },
            { 72, Key.H },
            { 73, Key.I },
            { 74, Key.J },
            { 75, Key.K },
            { 76, Key.L },
            { 77, Key.M },
            { 78, Key.N },
            { 79, Key.O },
            { 80, Key.P },
            { 81, Key.Q },
            { 82, Key.R },
            { 83, Key.S },
            { 84, Key.T },
            { 85, Key.U },
            { 86, Key.V },
            { 87, Key.W },
            { 88, Key.X },
            { 89, Key.Y },
            { 90, Key.Z },
            { 91, Key.LWin },
            { 92, Key.RWin },
            { 93, Key.Apps },
            { 95, Key.Sleep },
            { 96, Key.NumPad0 },
            { 97, Key.NumPad1 },
            { 98, Key.NumPad2 },
            { 99, Key.NumPad3 },
            { 100, Key.NumPad4 },
            { 101, Key.NumPad5 },
            { 102, Key.NumPad6 },
            { 103, Key.NumPad7 },
            { 104, Key.NumPad8 },
            { 105, Key.NumPad9 },
            { 106, Key.Multiply },
            { 107, Key.Add },
            { 108, Key.Separator },
            { 109, Key.Subtract },
            { 110, Key.Decimal },
            { 111, Key.Divide },
            { 112, Key.F1 },
            { 113, Key.F2 },
            { 114, Key.F3 },
            { 115, Key.F4 },
            { 116, Key.F5 },
            { 117, Key.F6 },
            { 118, Key.F7 },
            { 119, Key.F8 },
            { 120, Key.F9 },
            { 121, Key.F10 },
            { 122, Key.F11 },
            { 123, Key.F12 },
            { 124, Key.F13 },
            { 125, Key.F14 },
            { 126, Key.F15 },
            { 127, Key.F16 },
            { 128, Key.F17 },
            { 129, Key.F18 },
            { 130, Key.F19 },
            { 131, Key.F20 },
            { 132, Key.F21 },
            { 133, Key.F22 },
            { 134, Key.F23 },
            { 135, Key.F24 },
            { 144, Key.NumLock },
            { 145, Key.Scroll },
            { 160, Key.LeftShift },
            { 161, Key.RightShift },
            { 162, Key.LeftCtrl },
            { 163, Key.RightCtrl },
            { 164, Key.LeftAlt },
            { 165, Key.RightAlt },
            { 166, Key.BrowserBack },
            { 167, Key.BrowserForward },
            { 168, Key.BrowserRefresh },
            { 169, Key.BrowserStop },
            { 170, Key.BrowserSearch },
            { 171, Key.BrowserFavorites },
            { 172, Key.BrowserHome },
            { 173, Key.VolumeMute },
            { 174, Key.VolumeDown },
            { 175, Key.VolumeUp },
            { 176, Key.MediaNextTrack },
            { 177, Key.MediaPreviousTrack },
            { 178, Key.MediaStop },
            { 179, Key.MediaPlayPause },
            { 180, Key.LaunchMail },
            { 181, Key.SelectMedia },
            { 182, Key.LaunchApplication1 },
            { 183, Key.LaunchApplication2 },
            { 186, Key.Oem1 },
            { 187, Key.OemPlus },
            { 188, Key.OemComma },
            { 189, Key.OemMinus },
            { 190, Key.OemPeriod },
            { 191, Key.OemQuestion },
            { 192, Key.Oem3 },
            { 193, Key.AbntC1 },
            { 194, Key.AbntC2 },
            { 219, Key.OemOpenBrackets },
            { 220, Key.Oem5 },
            { 221, Key.Oem6 },
            { 222, Key.OemQuotes },
            { 223, Key.Oem8 },
            { 226, Key.OemBackslash },
            { 229, Key.ImeProcessed },
            { 240, Key.OemAttn },
            { 241, Key.OemFinish },
            { 242, Key.OemCopy },
            { 243, Key.DbeSbcsChar },
            { 244, Key.OemEnlw },
            { 245, Key.OemBackTab },
            { 246, Key.DbeNoRoman },
            { 247, Key.DbeEnterWordRegisterMode },
            { 248, Key.DbeEnterImeConfigureMode },
            { 249, Key.EraseEof },
            { 250, Key.Play },
            { 251, Key.DbeNoCodeInput },
            { 252, Key.NoName },
            { 253, Key.Pa1 },
            { 254, Key.OemClear },
        };

        /// <summary>
        /// Indicates whether the key is an extended key, such as the right-hand ALT and CTRL keys.
        /// According to https://docs.microsoft.com/en-us/windows/win32/inputdev/wm-keydown.
        /// </summary>
        private static bool IsExtended(int keyData)
        {
            const int extendedMask = 1 << 24;

            return (keyData & extendedMask) != 0;
        }

        private static int GetVirtualKey(int virtualKey, int keyData)
        {
            // Adapted from https://github.com/dotnet/wpf/blob/master/src/Microsoft.DotNet.Wpf/src/PresentationCore/System/Windows/InterOp/HwndKeyboardInputProvider.cs.

            if (virtualKey == (int)UnmanagedMethods.VirtualKeyStates.VK_SHIFT)
            {
                // Bits from 16 to 23 represent scan code.
                const int scanCodeMask = 0xFF0000;

                var scanCode = (keyData & scanCodeMask) >> 16;

                virtualKey = (int)UnmanagedMethods.MapVirtualKey((uint)scanCode, (uint)UnmanagedMethods.MapVirtualKeyMapTypes.MAPVK_VSC_TO_VK_EX);

                if (virtualKey == 0)
                {
                    virtualKey = (int)UnmanagedMethods.VirtualKeyStates.VK_LSHIFT;
                }
            }

            if (virtualKey == (int)UnmanagedMethods.VirtualKeyStates.VK_MENU)
            {
                bool isRight = IsExtended(keyData);

                if (isRight)
                {
                    virtualKey = (int)UnmanagedMethods.VirtualKeyStates.VK_RMENU;
                }
                else
                {
                    virtualKey = (int)UnmanagedMethods.VirtualKeyStates.VK_LMENU;
                }
            }
            
            if (virtualKey == (int)UnmanagedMethods.VirtualKeyStates.VK_CONTROL)
            {
                bool isRight = IsExtended(keyData);

                if (isRight)
                {
                    virtualKey = (int)UnmanagedMethods.VirtualKeyStates.VK_RCONTROL;
                }
                else
                {
                    virtualKey = (int)UnmanagedMethods.VirtualKeyStates.VK_LCONTROL;
                }
            }

            return virtualKey;
        }

        public static Key KeyFromVirtualKey(int virtualKey, int keyData)
        {
            virtualKey = GetVirtualKey(virtualKey, keyData);

            s_keyFromVirtualKey.TryGetValue(virtualKey, out var result);

            return result;
        }

        public static int VirtualKeyFromKey(Key key)
        {
            s_virtualKeyFromKey.TryGetValue(key, out var result);

            return result;
        }

    }
}
