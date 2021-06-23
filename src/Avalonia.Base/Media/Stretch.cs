namespace Avalonia.Media
{
    /// <summary>
    /// Describes how content is resized to fill its allocated space.
    /// </summary>
    public enum Stretch
    {
        /// <summary>
        /// The content preserves its original size.
        /// </summary>
        None,

        /// <summary>
        /// The content is resized to fill the destination dimensions. The aspect ratio is not
        /// preserved.
        /// </summary>
        Fill,

        /// <summary>
        /// The content is resized to fit in the destination dimensions while preserving its
        /// native aspect ratio.
        /// </summary>
        Uniform,

        /// <summary>
        /// The content is resized to completely fill the destination rectangle while preserving
        /// its native aspect ratio. A portion of the content may not be visible if the aspect
        /// ratio of the content does not match the aspect ratio of the allocated space.
        /// </summary>
        UniformToFill,
    }
}
