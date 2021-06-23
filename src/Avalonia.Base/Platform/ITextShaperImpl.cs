using System.Globalization;
using Avalonia.Media;
using Avalonia.Utilities;

namespace Avalonia.Platform
{
    /// <summary>
    /// An abstraction that is used produce shaped text.
    /// </summary>
    public interface ITextShaperImpl
    {
        /// <summary>
        /// Shapes the specified region within the text and returns a resulting glyph run.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <param name="typeface">The typeface.</param>
        /// <param name="fontRenderingEmSize">The font rendering em size.</param>
        /// <param name="culture">The culture.</param>
        /// <returns>A shaped glyph run.</returns>
        GlyphRun ShapeText(ReadOnlySlice<char> text, Typeface typeface, double fontRenderingEmSize, CultureInfo culture);
    }
}
