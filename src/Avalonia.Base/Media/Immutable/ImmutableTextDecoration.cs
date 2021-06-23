namespace Avalonia.Media.Immutable
{
    /// <summary>
    /// An immutable representation of a <see cref="TextDecoration"/>.
    /// </summary>
    public class ImmutableTextDecoration
    {
        public ImmutableTextDecoration(TextDecorationLocation location, ImmutablePen pen,
            TextDecorationUnit penThicknessUnit,
            double penOffset, TextDecorationUnit penOffsetUnit)
        {
            Location = location;
            Pen = pen;
            PenThicknessUnit = penThicknessUnit;
            PenOffset = penOffset;
            PenOffsetUnit = penOffsetUnit;
        }

        /// <summary>
        /// Gets or sets the location.
        /// </summary>
        /// <value>
        /// The location.
        /// </value>
        public TextDecorationLocation Location { get; }

        /// <summary>
        /// Gets or sets the pen.
        /// </summary>
        /// <value>
        /// The pen.
        /// </value>
        public ImmutablePen Pen { get; }

        /// <summary>
        /// Gets the units in which the Thickness of the text decoration's <see cref="Pen"/> is expressed.
        /// </summary>
        public TextDecorationUnit PenThicknessUnit { get; }

        /// <summary>
        /// Gets or sets the pen offset.
        /// </summary>
        /// <value>
        /// The pen offset.
        /// </value>
        public double PenOffset { get; }

        /// <summary>
        /// Gets the units in which the <see cref="PenOffset"/> value is expressed.
        /// </summary>
        public TextDecorationUnit PenOffsetUnit { get; }
    }
}
