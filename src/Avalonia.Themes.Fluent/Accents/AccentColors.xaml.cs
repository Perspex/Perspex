#nullable enable
using System;

using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Styling;

namespace Avalonia.Themes.Fluent.Accents
{
    public class AccentColors : Styles
    {
        public AccentColors()
        {
            AvaloniaXamlLoader.Load(this);

            var accentColorProvider = AvaloniaLocator.CurrentMutable.GetService<IPlatformColorSchemeProvider>();
            var accentColorScheme = accentColorProvider?.GetAccentColorScheme();

            if (accentColorScheme is null)
            {
                if (!TryGetResource("FallbackSystemAccentColor", out var fallbackAccentColorResource))
                {
                    throw new InvalidOperationException("\"FallbackSystemAccentColor\" resource is not defined in the application.");
                }

                if (!(fallbackAccentColorResource is Color fallbackAccentColor))
                {
                    throw new InvalidOperationException("\"FallbackSystemAccentColor\" must be a Color type.");
                }

                accentColorScheme = new AccentColorScheme(fallbackAccentColor);
            }

            Resources.Add("SystemAccentColor", accentColorScheme.Accent);
            Resources.Add("SystemAccentColorLight1", accentColorScheme.AccentLight1);
            Resources.Add("SystemAccentColorLight2", accentColorScheme.AccentLight2);
            Resources.Add("SystemAccentColorLight3", accentColorScheme.AccentLight3);
            Resources.Add("SystemAccentColorDark1", accentColorScheme.AccentDark1);
            Resources.Add("SystemAccentColorDark2", accentColorScheme.AccentDark2);
            Resources.Add("SystemAccentColorDark3", accentColorScheme.AccentDark3);
        }
    }
}
