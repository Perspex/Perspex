using System;
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

        /// <summary>
        /// Get or set the startup screen index where the DevTools window will be displayed.
        /// </summary>
        public int? StartupScreenIndex { get; set; }

        /// <summary>
        /// Get or sets the root folder where screeshots well be stored.
        /// The default root folder is [Environment.SpecialFolder.MyPictures]/Screenshots.
        /// </summary>
        public string? ScreenshotsRoot { get; set; }
           
        /// <summary>
        /// Get or sets conventin for screenshot fileName.
        /// For known default screen shot file name convection see <see href="https://github.com/AvaloniaUI/Avalonia/issues/4743">GH-4743</see>.
        /// </summary>
        public Func<Avalonia.Controls.IControl, string, string>? ScreenshotFileNameConvention { get; set; }
    }
}
