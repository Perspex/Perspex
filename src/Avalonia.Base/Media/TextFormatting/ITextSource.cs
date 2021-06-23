namespace Avalonia.Media.TextFormatting
{
    /// <summary>
    /// Produces <see cref="TextRun"/> objects that are used by the <see cref="TextFormatter"/>.
    /// </summary>
    public interface ITextSource
    {
        /// <summary>
        /// Gets a <see cref="TextRun"/> for specified text source index.
        /// </summary>
        /// <param name="textSourceIndex">The text source index.</param>
        /// <returns>The text run.</returns>
        TextRun GetTextRun(int textSourceIndex);
    }
}
