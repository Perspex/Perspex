using Avalonia.Input;

namespace Avalonia.Diagnostics
{
    /// <summary>
    /// Describes options used to customize DevTools.
    /// </summary>
    public class DevToolsOptions
    {
        /// <summary>
        /// Gets or sets the key gesture used to open DevTools.
        /// </summary>
        public KeyGesture Gesture { get; set; } = new KeyGesture(Key.F12);

        /// <summary>
        /// Gets or sets a value indicating whether DevTools should be displayed as a child window
        /// of the window being inspected. The default value is true.
        /// </summary>
        public bool ShowAsChildWindow { get; set; } = true;

        /// <summary>
        /// Gets or sets the initial size of the DevTools window. The default value is 1280x720.
        /// </summary>
        public Size Size { get; set; } = new Size(1280, 720);
    }
}
