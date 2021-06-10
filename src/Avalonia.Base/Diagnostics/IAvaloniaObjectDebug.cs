using System;

#nullable enable

namespace Avalonia.Diagnostics
{
    /// <summary>
    /// Provides a debug interface into <see cref="AvaloniaObject"/>.
    /// </summary>
    public interface IAvaloniaObjectDebug
    {
        /// <summary>
        /// Gets the subscriber list for the <see cref="IAvaloniaObject.PropertyChanged"/>
        /// event.
        /// </summary>
        /// <returns>
        /// The subscribers or null if no subscribers.
        /// </returns>
        Delegate[]? GetPropertyChangedSubscribers();
    }
}
