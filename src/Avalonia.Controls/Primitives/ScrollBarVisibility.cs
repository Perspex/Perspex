namespace Avalonia.Controls.Primitives
{
    /// <summary>
    /// Specifies the visibility of a <see cref="ScrollBar"/> for scrollable content.
    /// </summary>
    public enum ScrollBarVisibility
    {
        /// <summary>
        /// No scrollbars and no scrolling in this dimension.
        /// </summary>
        Disabled,

        /// <summary>
        /// The scrollbar should be visible only if there is more content than fits in the viewport.
        /// </summary>
        Auto,

        /// <summary>
        /// The scrollbar should never be visible.  No space should ever be reserved for the scrollbar.
        /// </summary>
        Hidden,

        /// <summary>
        /// The scrollbar should always be visible.  Space should always be reserved for the scrollbar.
        /// </summary>
        Visible,
    }
}
