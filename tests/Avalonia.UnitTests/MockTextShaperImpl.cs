using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia.Media;
using Avalonia.Media.TextFormatting.Unicode;
using Avalonia.Platform;
using Avalonia.Utilities;

namespace Avalonia.UnitTests
{
    public class MockTextShaperImpl : ITextShaperImpl
    {
        public GlyphRun ShapeText(ReadOnlySlice<char> text, Typeface typeface, double fontRenderingEmSize, CultureInfo culture)
        {
            var glyphTypeface = typeface.GlyphTypeface;
            var glyphIndices = new List<ushort>();
            var glyphClusters = new List<ushort>();

            for (var i = 0; i < text.Length;)
            {
                glyphClusters.Add((ushort)i);
                
                var codepoint = Codepoint.ReadAt(text, i, out var count);

                var glyph = glyphTypeface.GetGlyph(codepoint);

                glyphIndices.Add(glyph);

                i += count;
            }

            return new GlyphRun(
                glyphTypeface, 
                fontRenderingEmSize,
                glyphIndices.ToArray(), 
                characters: text,
                glyphClusters: glyphClusters.ToArray());
        }
    }
}
