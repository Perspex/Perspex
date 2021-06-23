namespace Avalonia.Media.TextFormatting
{
    /// <summary>
    /// Properties of text collapsing
    /// </summary>
    public abstract class TextCollapsingProperties
    {
        /// <summary>
        /// Gets the width in which the collapsible range is constrained to
        /// </summary>
        public abstract double Width { get; }

        /// <summary>
        /// Gets the text run that is used as collapsing symbol
        /// </summary>
        public abstract TextRun Symbol { get; }

        /// <summary>
        /// Gets the style of collapsing
        /// </summary>
        public abstract TextCollapsingStyle Style { get; }
    }
}
