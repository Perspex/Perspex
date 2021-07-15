#nullable enable

namespace Avalonia.Input
{
    /// <summary>
    /// Designates a control as handling its own keyboard navigation.
    /// </summary>
    public interface ICustomKeyboardNavigation
    {
        /// <summary>
        /// Gets the next element in the specified navigation direction.
        /// </summary>
        /// <param name="element">The element being navigated from.</param>
        /// <param name="direction">The navigation direction.</param>
        /// <returns>
        /// A tuple consisting of:
        /// - A boolean indicating whether the request was handled. If false is returned then 
        ///   custom navigation will be ignored and default navigation will take place.
        /// - If handled is true: the next element in the navigation direction, or null if default
        ///   navigation should continue outside the element.
        /// </returns>
        (bool handled, IInputElement? next) GetNext(IInputElement element, NavigationDirection direction);
    }
}
