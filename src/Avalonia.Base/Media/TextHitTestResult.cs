namespace Avalonia.Media
{
    /// <summary>
    /// Holds a hit test result from a <see cref="FormattedText"/>.
    /// </summary>
    public class TextHitTestResult
    {
        /// <summary>
        /// Gets or sets a value indicating whether the point is inside the bounds of the text.
        /// </summary>
        public bool IsInside { get; set; }

        /// <summary>
        /// Gets the index of the hit character in the text.
        /// </summary>
        public int TextPosition { get; set; }

        /// <summary>
        /// Gets a value indicating whether the hit is on the trailing edge of the character.
        /// </summary>
        public bool IsTrailing { get; set; }
    }
}
