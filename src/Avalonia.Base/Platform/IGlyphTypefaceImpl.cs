using System;

namespace Avalonia.Platform
{
    public interface IGlyphTypefaceImpl : IDisposable
    {
        /// <summary>
        ///     Gets the font design units per em.
        /// </summary>
        short DesignEmHeight { get; }

        /// <summary>
        ///     Gets the recommended distance above the baseline in design em size. 
        /// </summary>
        int Ascent { get; }

        /// <summary>
        ///     Gets the recommended distance under the baseline in design em size. 
        /// </summary>
        int Descent { get; }

        /// <summary>
        ///      Gets the recommended additional space between two lines of text in design em size. 
        /// </summary>
        int LineGap { get; }

        /// <summary>
        ///     Gets a value that indicates the distance of the underline from the baseline in design em size.
        /// </summary>
        int UnderlinePosition { get; }

        /// <summary>
        ///     Gets a value that indicates the thickness of the underline in design em size.
        /// </summary>
        int UnderlineThickness { get; }

        /// <summary>
        ///     Gets a value that indicates the distance of the strikethrough from the baseline in design em size.
        /// </summary>
        int StrikethroughPosition { get; }

        /// <summary>
        ///     Gets a value that indicates the thickness of the underline in design em size.
        /// </summary>
        int StrikethroughThickness { get; }

        /// <summary>
        ///     A <see cref="bool"/> value indicating whether all glyphs in the font have the same advancement. 
        /// </summary>
        bool IsFixedPitch { get; }

        /// <summary>
        ///     Returns an glyph index for the specified codepoint.
        /// </summary>
        /// <remarks>
        ///     Returns <c>0</c> if a glyph isn't found.
        /// </remarks>
        /// <param name="codepoint">The codepoint.</param>
        /// <returns>
        ///     A glyph index.
        /// </returns>
        ushort GetGlyph(uint codepoint);

        /// <summary>
        ///     Returns an array of glyph indices. Codepoints that are not represented by the font are returned as <code>0</code>.
        /// </summary>
        /// <param name="codepoints">The codepoints to map.</param>
        /// <returns>
        ///     An array of glyph indices.
        /// </returns>
        ushort[] GetGlyphs(ReadOnlySpan<uint> codepoints);

        /// <summary>
        ///     Returns the glyph advance for the specified glyph.
        /// </summary>
        /// <param name="glyph">The glyph.</param>
        /// <returns>
        ///     The advance.
        /// </returns>
        int GetGlyphAdvance(ushort glyph);

        /// <summary>
        ///     Returns an array of glyph advances in design em size.
        /// </summary>
        /// <param name="glyphs">The glyph indices.</param>
        /// <returns>
        ///     An array of glyph advances.
        /// </returns>
        int[] GetGlyphAdvances(ReadOnlySpan<ushort> glyphs);
    }
}
