using System;

namespace Avalonia.Media
{
    /// <summary>
    /// Signals to a self-rendering control that changes to the resource should invoke
    /// <see cref="Visual.InvalidateVisual"/>.
    /// </summary>
    public interface IAffectsRender
    {
        /// <summary>
        /// Raised when the resource changes visually.
        /// </summary>
        event EventHandler Invalidated;
    }
}
