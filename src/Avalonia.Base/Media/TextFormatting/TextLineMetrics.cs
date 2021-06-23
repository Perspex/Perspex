namespace Avalonia.Media.TextFormatting
{
    /// <summary>
    /// Represents a metric for a <see cref="TextLine"/> objects,
    /// that holds information about ascent, descent, line gap, size and origin of the text line.
    /// </summary>
    public readonly struct TextLineMetrics
    {
        public TextLineMetrics(bool hasOverflowed, double height, int newLineLength, double start, double textBaseline,
            int trailingWhitespaceLength, double width,
            double widthIncludingTrailingWhitespace)
        {
            HasOverflowed = hasOverflowed;
            Height = height;
            NewLineLength = newLineLength;
            Start = start;
            TextBaseline = textBaseline;
            TrailingWhitespaceLength = trailingWhitespaceLength;
            Width = width;
            WidthIncludingTrailingWhitespace = widthIncludingTrailingWhitespace;
        }
        
        /// <summary>
        /// Gets a value that indicates whether content of the line overflows the specified paragraph width.
        /// </summary>
        public bool HasOverflowed { get; }

        /// <summary>
        /// Gets the height of a line of text.
        /// </summary>
        public double Height { get; }
        
        /// <summary>
        /// Gets the number of newline characters at the end of a line.
        /// </summary>
        public int NewLineLength { get; }
        
        /// <summary>
        /// Gets the distance from the start of a paragraph to the starting point of a line.
        /// </summary>
        public double Start { get; }

        /// <summary>
        /// Gets the distance from the top to the baseline of the line of text.
        /// </summary>
        public double TextBaseline { get; }

        /// <summary>
        /// Gets the number of whitespace code points beyond the last non-blank character in a line.
        /// </summary>
        public int TrailingWhitespaceLength { get; }

        /// <summary>
        /// Gets the width of a line of text, excluding trailing whitespace characters.
        /// </summary>
        public double Width { get; }

        /// <summary>
        /// Gets the width of a line of text, including trailing whitespace characters.
        /// </summary>
        public double WidthIncludingTrailingWhitespace { get; }
    }
}
