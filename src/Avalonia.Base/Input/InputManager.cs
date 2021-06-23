using System;
using System.Reactive.Subjects;
using Avalonia.Input.Raw;

namespace Avalonia.Input
{
    /// <summary>
    /// Receives input from the windowing subsystem and dispatches it to interested parties
    /// for processing.
    /// </summary>
    public class InputManager : IInputManager
    {
        private readonly Subject<RawInputEventArgs> _preProcess = new Subject<RawInputEventArgs>();
        private readonly Subject<RawInputEventArgs> _process = new Subject<RawInputEventArgs>();
        private readonly Subject<RawInputEventArgs> _postProcess = new Subject<RawInputEventArgs>();

        /// <summary>
        /// Gets the global instance of the input manager.
        /// </summary>
        public static IInputManager Instance => AvaloniaLocator.Current.GetService<IInputManager>();

        /// <inheritdoc/>
        public IObservable<RawInputEventArgs> PreProcess => _preProcess;

        /// <inheritdoc/>
        public IObservable<RawInputEventArgs> Process => _process;

        /// <inheritdoc/>
        public IObservable<RawInputEventArgs> PostProcess => _postProcess;

        /// <inheritdoc/>
        public void ProcessInput(RawInputEventArgs e)
        {
            _preProcess.OnNext(e);
            e.Device?.ProcessRawEvent(e);
            _process.OnNext(e);
            _postProcess.OnNext(e);
        }
    }
}
