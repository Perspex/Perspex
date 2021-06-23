using System;

namespace Avalonia.Input
{
    /// <summary>
    /// Represents a mouse device.
    /// </summary>
    public interface IMouseDevice : IPointerDevice
    {
        /// <summary>
        /// Gets the mouse position, in screen coordinates.
        /// </summary>
        [Obsolete("Use PointerEventArgs.GetPosition")]
        PixelPoint Position { get; }

        void TopLevelClosed(IInputRoot root);

        void SceneInvalidated(IInputRoot root, Rect rect);
    }
}
