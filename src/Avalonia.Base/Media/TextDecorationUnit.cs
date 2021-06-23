namespace Avalonia.Media
{
    /// <summary>
    /// Specifies the unit type of either a <see cref="TextDecoration.StrokeOffset"/> or a <see cref="TextDecoration.StrokeThickness"/> value.
    /// </summary>
    public enum TextDecorationUnit
    {
        /// <summary>
        /// A unit value that is relative to the font used for the <see cref="TextDecoration"/>.
        /// If the decoration spans multiple fonts, an average recommended value is calculated.
        /// This is the default value.
        /// </summary>
        FontRecommended,

        /// <summary>
        /// A unit value that is relative to the em size of the font.
        /// The value of the offset or thickness is equal to the offset or thickness value multiplied by the font em size.
        /// </summary>
        FontRenderingEmSize,

        /// <summary>
        /// A unit value that is expressed in pixels.
        /// </summary>
        Pixel
    }
}
