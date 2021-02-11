namespace Avalonia.Media.TextFormatting
{
    /// <summary>
    /// Specialized text run used to mark a range of hidden characters
    /// </summary>
    public class TextHidden : TextRun
    {
        public TextHidden() : this(DefaultTextSourceLength)
        {
        }

        public TextHidden(int textSourceLength)
        {
            TextSourceLength = textSourceLength;
        }

        public override int TextSourceLength { get; }
    }
}
