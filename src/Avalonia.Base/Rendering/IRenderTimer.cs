using System;
using System.Threading.Tasks;

namespace Avalonia.Rendering
{
    /// <summary>
    /// Defines the interface implemented by an application render timer.
    /// </summary>
    public interface IRenderTimer
    {
        /// <summary>
        /// Raised when the render timer ticks to signal a new frame should be drawn.
        /// </summary>
        /// <remarks>
        /// This event can be raised on any thread; it is the responsibility of the subscriber to
        /// switch execution to the right thread.
        /// </remarks>
        event Action<TimeSpan> Tick;
    }
}
