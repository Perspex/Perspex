using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace Avalonia.Controls.ApplicationLifetimes
{
    /// <summary>
    /// Controls application lifetime in classic desktop style
    /// </summary>
    public interface IClassicDesktopStyleApplicationLifetime : IControlledApplicationLifetime
    {
        /// <summary>
        /// Gets the arguments passed to the
        /// <see cref="ClassicDesktopStyleApplicationLifetimeExtensions.StartWithClassicDesktopLifetime{T}(T, string[], ShutdownMode)"/>
        /// method.
        /// </summary>
        string[] Args { get; }
        
        /// <summary>
        /// Gets or sets the <see cref="ShutdownMode"/>. This property indicates whether the application is shutdown explicitly or implicitly. 
        /// If <see cref="ShutdownMode"/> is set to OnExplicitShutdown the application is only closes if Shutdown is called.
        /// The default is OnLastWindowClose
        /// </summary>
        /// <value>
        /// The shutdown mode.
        /// </value>
        ShutdownMode ShutdownMode { get; set; }

        /// <summary>
        /// Gets or sets the main window of the application.
        /// </summary>
        /// <value>
        /// The main window.
        /// </value>
        Window MainWindow { get; set; }
        
        IReadOnlyList<Window> Windows { get; }

        /// <summary>
        /// Raised by the platform when a shutdown is requested.
        /// </summary>
        /// <remarks>
        /// Raised on on OSX via the Quit menu or right-clicking on the application icon and selecting Quit. This event
        /// provides a first-chance to cancel application shutdown; if shutdown is not canceled at this point the application
        /// will try to close each non-owned open window, invoking the <see cref="Window.Closing"/> event on each and allowing
        /// each window to cancel the shutdown.
        /// </remarks>
        event EventHandler<CancelEventArgs> ShutdownRequested;
    }
}
