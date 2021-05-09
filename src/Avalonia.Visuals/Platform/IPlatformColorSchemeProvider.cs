#nullable enable
using System;

using Avalonia.Media;

namespace Avalonia.Platform
{
    public interface IPlatformColorSchemeProvider
    {
        AccentColorScheme? GetAccentColorScheme();
    }

    public class AccentColorScheme
    {
        public AccentColorScheme(Color accent)
        {
            Accent = accent;
            AccentDark1 = ChangeColorLuminosity(accent, -0.3);
            AccentDark2 = ChangeColorLuminosity(accent, -0.5);
            AccentDark3 = ChangeColorLuminosity(accent, -0.7);
            AccentLight1 = ChangeColorLuminosity(accent, 0.3);
            AccentLight2 = ChangeColorLuminosity(accent, 0.5);
            AccentLight3 = ChangeColorLuminosity(accent, 0.7);
        }

        public AccentColorScheme(
            Color accent,
            Color accentDark1,
            Color accentDark2,
            Color accentDark3,
            Color accentLight1,
            Color accentLight2,
            Color accentLight3)
        {
            Accent = accent;
            AccentDark1 = accentDark1;
            AccentDark2 = accentDark2;
            AccentDark3 = accentDark3;
            AccentLight1 = accentLight1;
            AccentLight2 = accentLight2;
            AccentLight3 = accentLight3;
        }

        public Color AccentDark3 { get; }
        public Color AccentDark2 { get; }
        public Color AccentDark1 { get; }
        public Color Accent { get; }
        public Color AccentLight1 { get; }
        public Color AccentLight2 { get; }
        public Color AccentLight3 { get; }

        private static Color ChangeColorLuminosity(Color color, double newluminosityFactor)
        {
            var red = (double)color.R;
            var green = (double)color.G;
            var blue = (double)color.B;

            if (newluminosityFactor < 0)//applies darkness
            {
                newluminosityFactor = 1 + newluminosityFactor;
                red *= newluminosityFactor;
                green *= newluminosityFactor;
                blue *= newluminosityFactor;
            }
            else if (newluminosityFactor >= 0) //applies lightness
            {
                red = (255 - red) * newluminosityFactor + red;
                green = (255 - green) * newluminosityFactor + green;
                blue = (255 - blue) * newluminosityFactor + blue;
            }
            else
            {
                throw new ArgumentOutOfRangeException("The Luminosity Factor must be a finite number.");
            }

            return new Color(color.A, (byte)red, (byte)green, (byte)blue);
        }
    }
}
