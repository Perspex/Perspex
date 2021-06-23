namespace Avalonia.Media
{
    /// <summary>
    /// Controls the wrapping mode of text.
    /// </summary>
    public enum TextWrapping
    {
        /// <summary>
        /// Text should not wrap.
        /// </summary>
        NoWrap,

        /// <summary>
        /// Text can wrap.
        /// </summary>
        Wrap, 
        
        /// <summary>
        /// Line-breaking occurs if the line overflows the available block width.
        /// However, a line may overflow the block width if the line breaking algorithm
        /// cannot determine a break opportunity, as in the case of a very long word.
        /// </summary>
        WrapWithOverflow
    }
}
