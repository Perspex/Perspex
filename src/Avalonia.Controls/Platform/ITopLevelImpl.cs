using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.Layout;
using Avalonia.Rendering;
using JetBrains.Annotations;

namespace Avalonia.Platform
{
    /// <summary>
    /// Describes the reason for a <see cref="ITopLevelImpl.Resized"/> message.
    /// </summary>
    public enum PlatformResizeReason
    {
        /// <summary>
        /// The resize reason is unknown or unspecified.
        /// </summary>
        Unspecified,

        /// <summary>
        /// The resize was due to the user resizing the window, for example by dragging the
        /// window frame.
        /// </summary>
        User,

        /// <summary>
        /// The resize was initiated by the application, for example by setting one of the sizing-
        /// related properties on <see cref="Window"/> such as <see cref="Layoutable.Width"/> or
        /// <see cref="Layoutable.Height"/>.
        /// </summary>
        Application,

        /// <summary>
        /// The resize was initiated by the layout system.
        /// </summary>
        Layout,

        /// <summary>
        /// The resize was due to a change in DPI.
        /// </summary>
        DpiChange,
    }

    /// <summary>
    /// Defines a platform-specific top-level window implementation.
    /// </summary>
    /// <remarks>
    /// This interface is the common interface to <see cref="IWindowImpl"/> and
    /// <see cref="IPopupImpl"/>.
    /// </remarks>
    public interface ITopLevelImpl : IDisposable
    {
        /// <summary>
        /// Gets the client size of the toplevel.
        /// </summary>
        Size ClientSize { get; }

        /// <summary>
        /// Gets the total size of the toplevel, excluding shadows.
        /// </summary>
        Size? FrameSize { get; }

        /// <summary>
        /// Gets the scaling factor for the toplevel. This is used for rendering.
        /// </summary>
        double RenderScaling { get; }
        
        /// <summary>
        /// The list of native platform's surfaces that can be consumed by rendering subsystems.
        /// </summary>
        /// <remarks>
        /// Rendering platform will check that list and see if it can utilize one of them to output.
        /// It should be enough to expose a native window handle via IPlatformHandle
        /// and add support for framebuffer (even if it's emulated one) via IFramebufferPlatformSurface.
        /// If you have some rendering platform that's tied to your particular windowing platform,
        /// just expose some toolkit-specific object (e. g. Func&lt;Gdk.Drawable&gt; in case of GTK#+Cairo)
        /// </remarks>
        IEnumerable<object> Surfaces { get; }

        /// <summary>
        /// Gets or sets a method called when the toplevel receives input.
        /// </summary>
        Action<RawInputEventArgs> Input { get; set; }

        /// <summary>
        /// Gets or sets a method called when the toplevel requires painting.
        /// </summary>
        Action<Rect> Paint { get; set; }

        /// <summary>
        /// Gets or sets a method called when the toplevel is resized.
        /// </summary>
        Action<Size, PlatformResizeReason> Resized { get; set; }

        /// <summary>
        /// Gets or sets a method called when the toplevel's scaling changes.
        /// </summary>
        Action<double> ScalingChanged { get; set; }

        /// <summary>
        /// Gets or sets a method called when the toplevel's TransparencyLevel changes.
        /// </summary>
        Action<WindowTransparencyLevel> TransparencyLevelChanged { get; set; }

        /// <summary>
        /// Creates a new renderer for the toplevel.
        /// </summary>
        /// <param name="root">The toplevel.</param>
        IRenderer CreateRenderer(IRenderRoot root);

        /// <summary>
        /// Invalidates a rect on the toplevel.
        /// </summary>
        void Invalidate(Rect rect);

        /// <summary>
        /// Sets the <see cref="IInputRoot"/> for the toplevel.
        /// </summary>
        void SetInputRoot(IInputRoot inputRoot);

        /// <summary>
        /// Converts a point from screen to client coordinates.
        /// </summary>
        /// <param name="point">The point in screen coordinates.</param>
        /// <returns>The point in client coordinates.</returns>
        Point PointToClient(PixelPoint point);

        /// <summary>
        /// Converts a point from client to screen coordinates.
        /// </summary>
        /// <param name="point">The point in client coordinates.</param>
        /// <returns>The point in screen coordinates.</returns>
        PixelPoint PointToScreen(Point point);

        /// <summary>
        /// Sets the cursor associated with the toplevel.
        /// </summary>
        /// <param name="cursor">The cursor. Use null for default cursor</param>
        void SetCursor(ICursorImpl cursor);

        /// <summary>
        /// Gets or sets a method called when the underlying implementation is destroyed.
        /// </summary>
        Action Closed { get; set; }
        
        /// <summary>
        /// Gets or sets a method called when the input focus is lost.
        /// </summary>
        Action LostFocus { get; set; }

        /// <summary>
        /// Gets a mouse device associated with toplevel
        /// </summary>
        [CanBeNull]
        IMouseDevice MouseDevice { get; }

        IPopupImpl CreatePopup();

        /// <summary>
        /// Sets the <see cref="WindowTransparencyLevel"/> hint of the TopLevel.
        /// </summary>
        void SetTransparencyLevelHint(WindowTransparencyLevel transparencyLevel);

        /// <summary>
        /// Gets the current <see cref="WindowTransparencyLevel"/> of the TopLevel.
        /// </summary>
        WindowTransparencyLevel TransparencyLevel { get; }

        /// <summary>
        /// Gets the <see cref="AcrylicPlatformCompensationLevels"/> for the platform.        
        /// </summary>
        AcrylicPlatformCompensationLevels AcrylicCompensationLevels { get; }
    }
}
