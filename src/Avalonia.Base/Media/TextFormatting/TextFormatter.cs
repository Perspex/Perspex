using Avalonia.Media.TextFormatting.Unicode;

namespace Avalonia.Media.TextFormatting
{
    /// <summary>
    /// Represents a base class for text formatting.
    /// </summary>
    public abstract class TextFormatter
    {
        /// <summary>
        /// Gets the current <see cref="TextFormatter"/> that is used for non complex text formatting.
        /// </summary>
        public static TextFormatter Current
        {
            get
            {
                var current = AvaloniaLocator.Current.GetService<TextFormatter>();

                if (current != null)
                {
                    return current;
                }

                current = new TextFormatterImpl();

                AvaloniaLocator.CurrentMutable.Bind<TextFormatter>().ToConstant(current);

                return current;
            }
        }

        /// <summary>
        /// Formats a text line.
        /// </summary>
        /// <param name="textSource">The text source.</param>
        /// <param name="firstTextSourceIndex">The first character index to start the text line from.</param>
        /// <param name="paragraphWidth">A <see cref="double"/> value that specifies the width of the paragraph that the line fills.</param>
        /// <param name="paragraphProperties">A <see cref="TextParagraphProperties"/> value that represents paragraph properties,
        /// such as TextWrapping, TextAlignment, or TextStyle.</param>
        /// <param name="previousLineBreak">A <see cref="TextLineBreak"/> value that specifies the text formatter state,
        /// in terms of where the previous line in the paragraph was broken by the text formatting process.</param>
        /// <returns>The formatted line.</returns>
        public abstract TextLine FormatLine(ITextSource textSource, int firstTextSourceIndex, double paragraphWidth,
            TextParagraphProperties paragraphProperties, TextLineBreak previousLineBreak = null);
    }
}
