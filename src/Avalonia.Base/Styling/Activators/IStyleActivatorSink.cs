#nullable enable

namespace Avalonia.Styling.Activators
{
    /// <summary>
    /// Receives notifications from an <see cref="IStyleActivator"/>.
    /// </summary>
    public interface IStyleActivatorSink
    {
        /// <summary>
        /// Called when the subscribed activator value changes.
        /// </summary>
        /// <param name="value">The new value.</param>
        /// <param name="tag">The subscription tag.</param>
        void OnNext(bool value, int tag);
    }
}
