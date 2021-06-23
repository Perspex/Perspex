using Avalonia.Utilities;

namespace Avalonia.Media.TextFormatting
{
    /// <summary>
    /// a collapsing properties to collapse whole line toward the end
    /// at word granularity and with ellipsis being the collapsing symbol
    /// </summary>
    public class TextTrailingWordEllipsis : TextCollapsingProperties
    {
        private static readonly ReadOnlySlice<char> s_ellipsis = new ReadOnlySlice<char>(new[] { '\u2026' });

        /// <summary>
        /// Construct a text trailing word ellipsis collapsing properties
        /// </summary>
        /// <param name="width">width in which collapsing is constrained to</param>
        /// <param name="textRunProperties">text run properties of ellispis symbol</param>
        public TextTrailingWordEllipsis(
            double width,
            TextRunProperties textRunProperties
        )
        {
            Width = width;
            Symbol = new TextCharacters(s_ellipsis, textRunProperties);
        }


        /// <inheritdoc/>
        public sealed override double Width { get; }

        /// <inheritdoc/>
        public sealed override TextRun Symbol { get; }

        /// <inheritdoc/>
        public sealed override TextCollapsingStyle Style { get; } = TextCollapsingStyle.TrailingWord;
    }
}
