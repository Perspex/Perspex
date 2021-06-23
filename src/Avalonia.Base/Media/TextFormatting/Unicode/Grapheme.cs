using Avalonia.Utilities;

namespace Avalonia.Media.TextFormatting.Unicode
{
    /// <summary>
    /// Represents the smallest unit of a writing system of any given language.
    /// </summary>
    public readonly struct Grapheme
    {
        public Grapheme(Codepoint firstCodepoint, ReadOnlySlice<char> text)
        {
            FirstCodepoint = firstCodepoint;
            Text = text;
        }

        /// <summary>
        /// The first <see cref="Codepoint"/> of the grapheme cluster.
        /// </summary>
        public Codepoint FirstCodepoint { get; }

        /// <summary>
        /// The text that is representing the <see cref="Grapheme"/>.
        /// </summary>
        public ReadOnlySlice<char> Text { get; }
    }
}
