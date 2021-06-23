namespace Avalonia.Media.TextFormatting
{
    /// <summary>
    /// A text run that supports drawing content.
    /// </summary>
    public abstract class DrawableTextRun : TextRun
    {
        /// <summary>
        /// Gets the size.
        /// </summary>
        public abstract Size Size { get; }

        /// <summary>
        /// Draws the <see cref="DrawableTextRun"/> at the given origin.
        /// </summary>
        /// <param name="drawingContext">The drawing context.</param>
        /// <param name="origin">The origin.</param>
        public abstract void Draw(DrawingContext drawingContext, Point origin);
    }
}
