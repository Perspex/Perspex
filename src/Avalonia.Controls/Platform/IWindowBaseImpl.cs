using System;

namespace Avalonia.Platform
{
    public interface IWindowBaseImpl : ITopLevelImpl
    {
        /// <summary>
        /// Shows the window.
        /// </summary>
        /// <param name="activate">Whether to activate the shown window.</param>
        /// <param name="isDialog">Whether the window is being shown as a dialog.</param>
        void Show(bool activate, bool isDialog);

        /// <summary>
        /// Hides the window.
        /// </summary>
        void Hide();
        
        /// <summary>
        /// Gets the scaling factor for Window positioning and sizing.
        /// </summary>
        double DesktopScaling { get; }

        /// <summary>
        /// Gets the position of the window in device pixels.
        /// </summary>
        PixelPoint Position { get; }
        
        /// <summary>
        /// Gets or sets a method called when the window's position changes.
        /// </summary>
        Action<PixelPoint> PositionChanged { get; set; }

        /// <summary>
        /// Activates the window.
        /// </summary>
        void Activate();

        /// <summary>
        /// Gets or sets a method called when the window is deactivated (loses focus).
        /// </summary>
        Action Deactivated { get; set; }

        /// <summary>
        /// Gets or sets a method called when the window is activated (receives focus).
        /// </summary>
        Action Activated { get; set; }

        /// <summary>
        /// Gets the platform window handle.
        /// </summary>
        IPlatformHandle Handle { get; }
       
        /// <summary>
        /// Gets a maximum client size hint for an auto-sizing window, in device-independent pixels.
        /// </summary>
        Size MaxAutoSizeHint { get; }

        /// <summary>
        /// Sets whether this window appears on top of all other windows
        /// </summary>
        void SetTopmost(bool value);

        /// <summary>
        /// Gets platform specific display information
        /// </summary>
        IScreenImpl Screen { get; }
    }
}
