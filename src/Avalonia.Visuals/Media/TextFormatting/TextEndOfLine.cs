using Avalonia.Utilities;

namespace Avalonia.Media.TextFormatting
{
    /// <summary>
    /// A text run that indicates the end of a line.
    /// </summary>
    public class TextEndOfLine : TextRun
    {
        private static readonly ReadOnlySlice<char> LineSeparator = new(new[] { '\u2028' });

        public TextEndOfLine() : this(DefaultTextSourceLength)
        {
        }

        public TextEndOfLine(int textSourceLength)
        {
            TextSourceLength = textSourceLength;
        }

        public override int TextSourceLength { get; }

        public override ReadOnlySlice<char> Text => LineSeparator;
    }
}
