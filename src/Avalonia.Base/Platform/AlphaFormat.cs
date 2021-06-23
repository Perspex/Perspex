namespace Avalonia.Platform
{
    /// <summary>
    /// Describes how to interpret the alpha component of a pixel.
    /// </summary>
    public enum AlphaFormat
    {
        /// <summary>
        /// All pixels have their alpha premultiplied in their color components.
        /// </summary>
        Premul,
        /// <summary>
        /// All pixels have their color components stored without any regard to the alpha. e.g. this is the default configuration for PNG images.
        /// </summary>
        Unpremul,
        /// <summary>
        /// All pixels are stored as opaque.
        /// </summary>
        Opaque
    }
}
