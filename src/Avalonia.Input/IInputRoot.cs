using Avalonia.VisualTree;
using JetBrains.Annotations;

namespace Avalonia.Input
{
    /// <summary>
    /// Defines the interface for top-level input elements.
    /// </summary>
    public interface IInputRoot : IInputElement, IVisual
    {
        /// <summary>
        /// Gets or sets the access key handler.
        /// </summary>
        IAccessKeyHandler AccessKeyHandler { get; }

        /// <summary>
        /// Gets or sets the keyboard navigation handler.
        /// </summary>
        IKeyboardNavigationHandler KeyboardNavigationHandler { get; }

        /// <summary>
        /// Gets or sets the input element that the pointer is currently over.
        /// </summary>
        IInputElement? PointerOverElement { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether access keys are shown in the window.
        /// </summary>
        bool ShowAccessKeys { get; set; }

        /// <summary>
        /// Converts a point from screen to client coordinates.
        /// Useful for converting from the coordinate system used by <see cref="MouseDevice"/>
        /// to the coordinate system of this input root.
        /// </summary>
        /// <param name="point">The point in screen device coordinates.</param>
        /// <returns>The point in client coordinates.</returns>
        Point PointToClient(PixelPoint point);

        /// <summary>
        /// Converts a point from client to screen coordinates.
        /// Useful for converting from the coordinate system of this input root
        /// to the coordinate system used by <see cref="MouseDevice"/>.
        /// </summary>
        /// <param name="point">The point in client coordinates.</param>
        /// <returns>The point in screen device coordinates.</returns>
        PixelPoint PointToScreen(Point point);

        /// <summary>
        /// Gets associated mouse device
        /// </summary>
        [CanBeNull]
        IMouseDevice? MouseDevice { get; }
    }
}
