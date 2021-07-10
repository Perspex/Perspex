using JetBrains.Annotations;

namespace Avalonia.Input
{
    /// <summary>
    /// Defines the interface for top-level input elements.
    /// </summary>
    public interface IInputRoot : IInputElement
    {
        /// <summary>
        /// Gets or sets the access key handler.
        /// </summary>
        IAccessKeyHandler AccessKeyHandler { get; }

        /// <summary>
        /// Gets or sets the keyboard navigation handler.
        /// </summary>
        IKeyboardNavigationHandler KeyboardNavigationHandler { get; }

        /// <summary>
        /// Gets or sets the input element that the pointer is currently over.
        /// </summary>
        IInputElement? PointerOverElement { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether access keys are shown in the window.
        /// </summary>
        bool ShowAccessKeys { get; set; }

        /// <summary>
        /// Gets associated mouse device
        /// </summary>
        [CanBeNull]
        IMouseDevice? MouseDevice { get; }

        /// <summary>
        /// If supported, gets a value indicating whether the element is enabled on the platform.
        /// </summary>
        bool? IsPlatformEnabled { get; }
    }
}
