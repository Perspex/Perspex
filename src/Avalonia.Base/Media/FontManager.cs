using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using Avalonia.Media.Fonts;
using Avalonia.Platform;

namespace Avalonia.Media
{
    /// <summary>
    ///     The font manager is used to query the system's installed fonts and is responsible for caching loaded fonts.
    ///     It is also responsible for the font fallback.
    /// </summary>
    public sealed class FontManager
    {
        private readonly ConcurrentDictionary<Typeface, GlyphTypeface> _glyphTypefaceCache =
            new ConcurrentDictionary<Typeface, GlyphTypeface>();
        private readonly FontFamily _defaultFontFamily;

        public FontManager(IFontManagerImpl platformImpl)
        {
            PlatformImpl = platformImpl;

            DefaultFontFamilyName = PlatformImpl.GetDefaultFontFamilyName();

            if (string.IsNullOrEmpty(DefaultFontFamilyName))
            {
                throw new InvalidOperationException("Default font family name can't be null or empty.");
            }

            _defaultFontFamily = new FontFamily(DefaultFontFamilyName);
        }

        public static FontManager Current
        {
            get
            {
                var current = AvaloniaLocator.Current.GetService<FontManager>();

                if (current != null)
                {
                    return current;
                }

                var fontManagerImpl = AvaloniaLocator.Current.GetService<IFontManagerImpl>();

                if (fontManagerImpl == null)
                    throw new InvalidOperationException("No font manager implementation was registered.");

                current = new FontManager(fontManagerImpl);

                AvaloniaLocator.CurrentMutable.Bind<FontManager>().ToConstant(current);

                return current;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public IFontManagerImpl PlatformImpl { get; }

        /// <summary>
        ///     Gets the system's default font family's name.
        /// </summary>
        public string DefaultFontFamilyName
        {
            get;
        }

        /// <summary>
        ///     Get all installed font family names.
        /// </summary>
        /// <param name="checkForUpdates">If <c>true</c> the font collection is updated.</param>
        public IEnumerable<string> GetInstalledFontFamilyNames(bool checkForUpdates = false) =>
            PlatformImpl.GetInstalledFontFamilyNames(checkForUpdates);

        /// <summary>
        ///     Returns a new <see cref="GlyphTypeface"/>, or an existing one if a matching <see cref="GlyphTypeface"/> exists.
        /// </summary>
        /// <param name="typeface">The typeface.</param>
        /// <returns>
        ///     The <see cref="GlyphTypeface"/>.
        /// </returns>
        public GlyphTypeface GetOrAddGlyphTypeface(Typeface typeface)
        {
            while (true)
            {
                if (_glyphTypefaceCache.TryGetValue(typeface, out var glyphTypeface))
                {
                    return glyphTypeface;
                }

                glyphTypeface = new GlyphTypeface(typeface);

                if (_glyphTypefaceCache.TryAdd(typeface, glyphTypeface))
                {
                    return glyphTypeface;
                }

                if (typeface.FontFamily == _defaultFontFamily)
                {
                    return null;
                }

                typeface = new Typeface(_defaultFontFamily, typeface.Style, typeface.Weight);
            }
        }

        /// <summary>
        ///     Tries to match a specified character to a <see cref="Typeface"/> that supports specified font properties.
        /// </summary>
        /// <param name="codepoint">The codepoint to match against.</param>
        /// <param name="fontStyle">The font style.</param>
        /// <param name="fontWeight">The font weight.</param>
        /// <param name="fontFamily">The font family. This is optional and used for fallback lookup.</param>
        /// <param name="culture">The culture.</param>
        /// <param name="typeface">The matching <see cref="Typeface"/>.</param>
        /// <returns>
        ///     <c>True</c>, if the <see cref="FontManager"/> could match the character to specified parameters, <c>False</c> otherwise.
        /// </returns>
        public bool TryMatchCharacter(int codepoint, FontStyle fontStyle,
            FontWeight fontWeight,
            FontFamily fontFamily, CultureInfo culture, out Typeface typeface) =>
            PlatformImpl.TryMatchCharacter(codepoint, fontStyle, fontWeight, fontFamily, culture, out typeface);
    }
}
