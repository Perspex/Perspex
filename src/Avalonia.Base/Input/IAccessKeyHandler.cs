namespace Avalonia.Input
{
    /// <summary>
    /// Defines the interface for classes that handle access keys for a window.
    /// </summary>
    public interface IAccessKeyHandler
    {
        /// <summary>
        /// Gets or sets the window's main menu.
        /// </summary>
        IMainMenu? MainMenu { get; set; }

        /// <summary>
        /// Sets the owner of the access key handler.
        /// </summary>
        /// <param name="owner">The owner.</param>
        /// <remarks>
        /// This method can only be called once, typically by the owner itself on creation.
        /// </remarks>
        void SetOwner(IInputRoot owner);

        /// <summary>
        /// Registers an input element to be associated with an access key.
        /// </summary>
        /// <param name="accessKey">The access key.</param>
        /// <param name="element">The input element.</param>
        void Register(char accessKey, IInputElement element);

        /// <summary>
        /// Unregisters the access keys associated with the input element.
        /// </summary>
        /// <param name="element">The input element.</param>
        void Unregister(IInputElement element);
    }
}
