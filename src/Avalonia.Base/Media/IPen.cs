namespace Avalonia.Media
{
    /// <summary>
    /// Describes how a stroke is drawn.
    /// </summary>
    public interface IPen
    {
        /// <summary>
        /// Gets the brush used to draw the stroke.
        /// </summary>
        IBrush Brush { get; }

        /// <summary>
        /// Gets the style of dashed lines drawn with a <see cref="Pen"/> object.
        /// </summary>
        IDashStyle DashStyle { get; }

        /// <summary>
        /// Gets the type of shape to use on both ends of a line.
        /// </summary>
        PenLineCap LineCap { get; }

        /// <summary>
        /// Gets a value describing how to join consecutive line or curve segments in a 
        /// <see cref="PathFigure"/> contained in a <see cref="PathGeometry"/> object.
        /// </summary>
        PenLineJoin LineJoin { get; }

        /// <summary>
        /// Gets the limit of the thickness of the join on a mitered corner.
        /// </summary>
        double MiterLimit { get; }

        /// <summary>
        /// Gets the stroke thickness.
        /// </summary>
        double Thickness { get; }
    }
}
