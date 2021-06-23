namespace Avalonia.Input
{
    /// <summary>
    /// Defines the method by which a focus change occurred.
    /// </summary>
    public enum NavigationMethod
    {
        /// <summary>
        /// The focus was changed by an unspecified method, e.g. calling
        /// <see cref="InputElement.Focus"/>.
        /// </summary>
        Unspecified,

        /// <summary>
        /// The focus was changed by the user tabbing between control.
        /// </summary>
        Tab,

        /// <summary>
        /// The focus was changed by the user pressing a directional navigation key.
        /// </summary>
        Directional,

        /// <summary>
        /// The focus was changed by a pointer click.
        /// </summary>
        Pointer,
    }
}
