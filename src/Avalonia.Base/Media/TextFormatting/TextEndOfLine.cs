namespace Avalonia.Media.TextFormatting
{
    /// <summary>
    /// A text run that indicates the end of a line.
    /// </summary>
    public class TextEndOfLine : TextRun
    {
        public TextEndOfLine(int textSourceLength = DefaultTextSourceLength)
        {
            TextSourceLength = textSourceLength;
        }

        public override int TextSourceLength { get; }
    }
}
