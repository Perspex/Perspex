namespace Avalonia.Media
{
    /// <summary>
    /// Describes the formatting for a span of text in a <see cref="FormattedText"/> object.
    /// </summary>
    public class FormattedTextStyleSpan
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FormattedTextStyleSpan"/> class.
        /// </summary>
        /// <param name="startIndex">The index of the first character in the span.</param>
        /// <param name="length">The length of the span.</param>
        /// <param name="foregroundBrush">The span's foreground brush.</param>
        public FormattedTextStyleSpan(
            int startIndex,
            int length,
            IBrush foregroundBrush = null)
        {
            StartIndex = startIndex;
            Length = length;
            ForegroundBrush = foregroundBrush;
        }

        /// <summary>
        /// Gets the index of the first character in the span.
        /// </summary>
        public int StartIndex { get; }

        /// <summary>
        /// Gets the length of the span.
        /// </summary>
        public int Length { get; }

        /// <summary>
        /// Gets the span's foreground brush.
        /// </summary>
        public IBrush ForegroundBrush { get; }
    }
}
