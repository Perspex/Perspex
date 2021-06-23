
namespace Avalonia.Controls
{
    /// <summary>
    /// Exposes an interface for setting pseudoclasses on a <see cref="Classes"/> collection.
    /// </summary>
    public interface IPseudoClasses
    {
        /// <summary>
        /// Adds a pseudoclass to the collection.
        /// </summary>
        /// <param name="name">The pseudoclass name.</param>
        void Add(string name);

        /// <summary>
        /// Removes a pseudoclass from the collection.
        /// </summary>
        /// <param name="name">The pseudoclass name.</param>
        bool Remove(string name);
    }
}
