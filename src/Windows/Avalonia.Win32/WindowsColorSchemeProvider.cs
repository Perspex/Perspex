#nullable enable
using System;

using Avalonia.Platform;
using Avalonia.Win32.WinRT;

namespace Avalonia.Win32
{
    public class WindowsColorSchemeProvider : IPlatformColorSchemeProvider
    {
        public AccentColorScheme? GetAccentColorScheme()
        {
            if (Win32Platform.WindowsVersion >= new Version(10, 0, 10586))
            {
                var settings = NativeWinRTMethods.CreateInstance<IUISettings3>("Windows.UI.ViewManagement.UISettings");
                return new AccentColorScheme(
                    settings.GetColorValue(UIColorType.Accent),
                    settings.GetColorValue(UIColorType.AccentDark1),
                    settings.GetColorValue(UIColorType.AccentDark2),
                    settings.GetColorValue(UIColorType.AccentDark3),
                    settings.GetColorValue(UIColorType.AccentLight1),
                    settings.GetColorValue(UIColorType.AccentLight2),
                    settings.GetColorValue(UIColorType.AccentLight3));
            }

            return null;
        }
    }
}
