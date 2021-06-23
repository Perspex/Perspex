using System.Collections.Generic;
using System.Globalization;
using Avalonia.Media;

namespace Avalonia.Platform
{
    public interface IFontManagerImpl
    {
        /// <summary>
        ///     Gets the system's default font family's name.
        /// </summary>
        string GetDefaultFontFamilyName();

        /// <summary>
        ///     Get all installed fonts in the system.
        /// <param name="checkForUpdates">If <c>true</c> the font collection is updated.</param>
        /// </summary>
        IEnumerable<string> GetInstalledFontFamilyNames(bool checkForUpdates = false);

        /// <summary>
        ///     Tries to match a specified character to a typeface that supports specified font properties.
        /// </summary>
        /// <param name="codepoint">The codepoint to match against.</param>
        /// <param name="fontStyle">The font style.</param>
        /// <param name="fontWeight">The font weight.</param>
        /// <param name="fontFamily">The font family. This is optional and used for fallback lookup.</param>
        /// <param name="culture">The culture.</param>
        /// <param name="typeface">The matching typeface.</param>
        /// <returns>
        ///     <c>True</c>, if the <see cref="IFontManagerImpl"/> could match the character to specified parameters, <c>False</c> otherwise.
        /// </returns>
        bool TryMatchCharacter(int codepoint, FontStyle fontStyle,
            FontWeight fontWeight,
            FontFamily fontFamily, CultureInfo culture, out Typeface typeface);

        /// <summary>
        ///     Creates a glyph typeface.
        /// </summary>
        /// <param name="typeface">The typeface.</param>
        /// <returns>0
        ///     The created glyph typeface. Can be <c>Null</c> if it was not possible to create a glyph typeface.
        /// </returns>
        IGlyphTypefaceImpl CreateGlyphTypeface(Typeface typeface);
    }
}
