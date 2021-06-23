#nullable enable

using System;

namespace Avalonia.Styling.Activators
{
    /// <summary>
    /// Defines a style activator.
    /// </summary>
    /// <remarks>
    /// A style activator is very similar to an `IObservable{bool}` but is optimized for the
    /// particular use-case of activating a style according to a selector. It differs from
    /// an observable in two major ways:
    /// 
    /// - Can only have a single subscription
    /// - The subscription can have a tag associated with it, allowing a subscriber to index
    ///   into a list of subscriptions without having to allocate additional objects.
    /// </remarks>
    public interface IStyleActivator : IDisposable
    {
        /// <summary>
        /// Subscribes to the activator.
        /// </summary>
        /// <param name="sink">The listener.</param>
        /// <param name="tag">An optional tag.</param>
        void Subscribe(IStyleActivatorSink sink, int tag = 0);

        /// <summary>
        /// Unsubscribes from the activator.
        /// </summary>
        void Unsubscribe(IStyleActivatorSink sink);
    }
}
