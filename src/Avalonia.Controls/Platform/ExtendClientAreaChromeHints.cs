using System;

namespace Avalonia.Platform
{
    /// <summary>
    /// Hint for Window Chrome when ClientArea is Extended.
    /// </summary>
    [Flags]
    public enum ExtendClientAreaChromeHints
    {
        /// <summary>
        /// The will be no chrome at all.
        /// </summary>
        NoChrome,

        /// <summary>
        /// The default for the platform.
        /// </summary>
        Default = PreferSystemChrome,

        /// <summary>
        /// Use SystemChrome
        /// </summary>
        SystemChrome = 0x01,

        /// <summary>
        /// Use system chrome where possible. OSX system chrome is used, Windows managed chrome is used.
        /// This is because Windows Chrome can not be shown on top of user content.
        /// </summary>
        PreferSystemChrome = 0x02,

        /// <summary>
        /// On OSX the titlebar is the thicker toolbar kind. Causes traffic lights to be positioned
        /// slightly lower than normal.
        /// </summary>
        OSXThickTitleBar = 0x08,               
    }
}
