
namespace Avalonia.Input
{
    /// <summary>
    /// Designates a control as handling its own keyboard navigation.
    /// </summary>
    public interface ICustomKeyboardNavigation
    {
        (bool handled, IInputElement next) GetNext(IInputElement element, NavigationDirection direction);
    }
}
