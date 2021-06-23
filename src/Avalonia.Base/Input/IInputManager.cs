using System;
using Avalonia.Input.Raw;

namespace Avalonia.Input
{
    /// <summary>
    /// Receives input from the windowing subsystem and dispatches it to interested parties
    /// for processing.
    /// </summary>
    public interface IInputManager
    {
        /// <summary>
        /// Gets an observable that notifies on each input event received before
        /// <see cref="Process"/>.
        /// </summary>
        IObservable<RawInputEventArgs> PreProcess { get; }

        /// <summary>
        /// Gets an observable that notifies on each input event received.
        /// </summary>
        IObservable<RawInputEventArgs> Process { get; }

        /// <summary>
        /// Gets an observable that notifies on each input event received after
        /// <see cref="Process"/>.
        /// </summary>
        IObservable<RawInputEventArgs> PostProcess { get; }

        /// <summary>
        /// Processes a raw input event.
        /// </summary>
        /// <param name="e">The raw input event.</param>
        void ProcessInput(RawInputEventArgs e);
    }
}
