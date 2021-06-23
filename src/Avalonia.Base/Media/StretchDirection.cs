namespace Avalonia.Media
{
    /// <summary>
    /// Describes the type of scaling that can be used when scaling content.
    /// </summary>
    public enum StretchDirection
    {
        /// <summary>
        /// Only scales the content upwards when the content is smaller than the available space.
        /// If the content is larger, no scaling downwards is done.
        /// </summary>
        UpOnly,

        /// <summary>
        /// Only scales the content downwards when the content is larger than the available space.
        /// If the content is smaller, no scaling upwards is done.
        /// </summary>
        DownOnly,

        /// <summary>
        /// Always stretches to fit the available space according to the stretch mode.
        /// </summary>
        Both,
    }
}
