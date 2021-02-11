using Avalonia.Utilities;

namespace Avalonia.Media.TextFormatting
{
    /// <summary>
    ///  A text run that indicates the end of a paragraph.
    /// </summary>
    public class TextEndOfParagraph : TextEndOfLine
    {
        private static readonly ReadOnlySlice<char> ParagraphSeparator = new(new[] { '\u2029' });

        public TextEndOfParagraph() : this(DefaultTextSourceLength) { }

        public TextEndOfParagraph(int textSourceLength) : base(textSourceLength)
        {
        }

        public override ReadOnlySlice<char> Text => ParagraphSeparator;
    }
}
