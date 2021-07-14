using System;
using Avalonia.Controls;
using Avalonia.Input;

namespace Avalonia.Platform
{
    /// <summary>
    /// Defines a platform-specific window implementation.
    /// </summary>
    public interface IWindowImpl : IWindowBaseImpl
    {
        /// <summary>
        /// Gets or sets the minimized/maximized state of the window.
        /// </summary>
        WindowState WindowState { get; set; }

        /// <summary>
        /// Gets or sets a method called when the minimized/maximized state of the window changes.
        /// </summary>
        Action<WindowState> WindowStateChanged { get; set; }

        /// <summary>
        /// Sets the title of the window.
        /// </summary>
        /// <param name="title">The title.</param>
        void SetTitle(string title);

        /// <summary>
        /// Sets the parent of the window.
        /// </summary>
        /// <param name="parent">The parent <see cref="IWindowImpl"/>.</param>
        void SetParent(IWindowImpl parent);
        
        /// <summary>
        /// Disables the window for example when a modal dialog is open.
        /// </summary>
        /// <param name="enable">true if the window is enabled, or false if it is disabled.</param>
        void SetEnabled(bool enable);

        /// <summary>
        /// Called when a disabled window received input. Can be used to activate child windows.
        /// </summary>
        Action GotInputWhenDisabled { get; set; }        

        /// <summary>
        /// Enables or disables system window decorations (title bar, buttons, etc)
        /// </summary>
        void SetSystemDecorations(SystemDecorations enabled);

        /// <summary>
        /// Sets the icon of this window.
        /// </summary>
        void SetIcon(IWindowIconImpl icon);

        /// <summary>
        /// Enables or disables the taskbar icon
        /// </summary>
        void ShowTaskbarIcon(bool value);

        /// <summary>
        /// Enables or disables resizing of the window
        /// </summary>
        void CanResize(bool value);

        /// <summary>
        /// Gets or sets a method called before the underlying implementation is destroyed.
        /// Return true to prevent the underlying implementation from closing.
        /// </summary>
        Func<bool> Closing { get; set; }

        /// <summary>
        /// Gets a value to indicate if the platform was able to extend client area to non-client area.
        /// </summary>
        bool IsClientAreaExtendedToDecorations { get; }

        /// <summary>
        /// Gets or Sets an action that is called whenever one of the extend client area properties changed.
        /// </summary>
        Action<bool> ExtendClientAreaToDecorationsChanged { get; set; }

        /// <summary>
        /// Gets a flag that indicates if Managed decorations i.e. caption buttons are required.
        /// This property is used when <see cref="IsClientAreaExtendedToDecorations"/> is set.
        /// </summary>
        bool NeedsManagedDecorations { get; }

        /// <summary>
        /// Gets a thickness that describes the amount each side of the non-client area extends into the client area.
        /// It includes the titlebar.
        /// </summary>
        Thickness ExtendedMargins { get; }

        /// <summary>
        /// Gets a thickness that describes the margin around the window that is offscreen.
        /// This may happen when a window is maximized and <see cref="IsClientAreaExtendedToDecorations"/> is set.
        /// </summary>
        Thickness OffScreenMargin { get; }

        /// <summary>
        /// Starts moving a window with left button being held. Should be called from left mouse button press event handler.
        /// </summary>
        void BeginMoveDrag(PointerPressedEventArgs e);

        /// <summary>
        /// Starts resizing a window. This function is used if an application has window resizing controls. 
        /// Should be called from left mouse button press event handler
        /// </summary>
        void BeginResizeDrag(WindowEdge edge, PointerPressedEventArgs e);

        /// <summary>
        /// Sets the client size of the top level.
        /// </summary>
        /// <param name="clientSize">The new client size.</param>
        /// <param name="reason">The reason for the resize.</param>
        void Resize(Size clientSize, PlatformResizeReason reason = PlatformResizeReason.Application);

        /// <summary>
        /// Sets the client size of the top level.
        /// </summary>
        void Move(PixelPoint point);

        /// <summary>
        /// Minimum width of the window.
        /// </summary>
        /// 
        void SetMinMaxSize(Size minSize, Size maxSize);

        /// <summary>
        /// Sets if the ClientArea is extended into the non-client area.
        /// </summary>
        /// <param name="extendIntoClientAreaHint">true to enable, false to disable</param>
        void SetExtendClientAreaToDecorationsHint(bool extendIntoClientAreaHint);        

        /// <summary>
        /// Sets hints that configure how the client area extends. 
        /// </summary>
        /// <param name="hints"></param>
        void SetExtendClientAreaChromeHints(ExtendClientAreaChromeHints hints);

        /// <summary>
        /// Sets how big the non-client titlebar area should be.
        /// </summary>
        /// <param name="titleBarHeight">-1 for platform default, otherwise the height in DIPs.</param>
        void SetExtendClientAreaTitleBarHeightHint(double titleBarHeight);       
    }
}
