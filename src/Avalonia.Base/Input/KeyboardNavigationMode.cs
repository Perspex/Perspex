namespace Avalonia.Input
{
    /// <summary>
    /// Defines the mode of keyboard traversal within a container when the tab or arrow keys are
    /// pressed.
    /// </summary>
    public enum KeyboardNavigationMode
    {
        /// <summary>
        /// Items in the container will be cycled through, and focus will be moved to the
        /// previous/next container after the first/last control in the container.
        /// </summary>
        Continue,

        /// <summary>
        /// Items in the container will be cycled through, and moving past the first or last
        /// control in the container will cause the last/first control to be focused.
        /// </summary>
        Cycle,

        /// <summary>
        /// Items in the container will be cycled through and focus will stop moving when the edge
        /// of the container is reached.
        /// </summary>
        Contained,

        /// <summary>
        /// When focus is moved into the container, the control described by the
        /// <see cref="KeyboardNavigation.TabOnceActiveElementProperty"/> attached property on the
        /// container will be focused. When focus moves away from this control, focus will move to
        /// the previous/next container.
        /// </summary>
        Once,

        /// <summary>
        /// The container's children will not be focused when using the tab key.
        /// </summary>
        None,
    }
}