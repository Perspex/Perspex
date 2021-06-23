namespace Avalonia.VisualTree
{
    /// <summary>
    /// Interface for controls that are at the root of a hosted visual tree, such as popups.
    /// </summary>
    public interface IHostedVisualTreeRoot
    {
        /// <summary>
        /// Gets the visual tree host.
        /// </summary>
        /// <value>
        /// The visual tree host.
        /// </value>
        IVisual Host { get; }
    }
}
