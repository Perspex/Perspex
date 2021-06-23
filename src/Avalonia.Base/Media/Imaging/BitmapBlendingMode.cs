namespace Avalonia.Media.Imaging
{
    /// <summary>
    /// Controls the way the bitmaps are drawn together.
    /// </summary>
    public enum BitmapBlendingMode
    {
        /// <summary>
        /// Source is placed over the destination.
        /// </summary>
        SourceOver,
        /// <summary>
        /// Only the source will be present.
        /// </summary>
        Source,
        /// <summary>
        /// Only the destination will be present.
        /// </summary>
        Destination,
        /// <summary>
        /// Destination is placed over the source.
        /// </summary>
        DestinationOver,
        /// <summary>
        /// The source that overlaps the destination, replaces the destination.
        /// </summary>
        SourceIn,
        /// <summary>
        /// Destination which overlaps the source, replaces the source.
        /// </summary>
        DestinationIn,
        /// <summary>
        /// Source is placed, where it falls outside of the destination.
        /// </summary>
        SourceOut,
        /// <summary>
        /// Destination is placed, where it falls outside of the source.
        /// </summary>
        DestinationOut,
        /// <summary>
        /// Source which overlaps the destination, replaces the destination.
        /// </summary>
        SourceAtop,
        /// <summary>
        /// Destination which overlaps the source replaces the source.
        /// </summary>
        DestinationAtop,
        /// <summary>
        /// The non-overlapping regions of source and destination are combined.
        /// </summary>
        Xor,
        /// <summary>
        /// Display the sum of the source image and destination image.
        /// </summary>
        Plus,
    }
}
