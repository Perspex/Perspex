namespace Avalonia.Media
{
    /// <summary>
    /// Abstract class that describes a 2-D drawing.
    /// </summary>
    public abstract class Drawing : AvaloniaObject
    {
        /// <summary>
        /// Draws this drawing to the given <see cref="DrawingContext"/>.
        /// </summary>
        /// <param name="context">The drawing context.</param>
        public abstract void Draw(DrawingContext context);

        /// <summary>
        /// Gets the drawing's bounding rectangle.
        /// </summary>
        public abstract Rect GetBounds();
    }
}
