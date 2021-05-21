namespace Avalonia.Input
{
    /// <summary>
    /// Implemented by containers for <see cref="ContentInputElement"/>.
    /// </summary>
    public interface IContentInputHost
    {
        /// <summary>
        /// Performs hit-testing for child elements.
        /// </summary>
        /// <param name="point">
        /// Mouse coordinates relative to the ContentHost.
        /// </param>
        /// <remarks>
        /// Must return a descendant IInputElement, or NULL if no such
        /// element exists.
        /// </remarks>
        IInputElement InputHitTest(Point point);
    }
}
