namespace Avalonia.Input
{
    /// <summary>
    /// Defines a container in which the child controls can be navigated by keyboard.
    /// </summary>
    public interface INavigableContainer
    {
        /// <summary>
        /// Gets the next control in the specified direction.
        /// </summary>
        /// <param name="direction">The movement direction.</param>
        /// <param name="from">The control from which movement begins.</param>
        /// <param name="wrap">Whether to wrap around when the first or last item is reached.</param>
        /// <returns>The control.</returns>
        IInputElement GetControl(NavigationDirection direction, IInputElement from, bool wrap);
    }
}
