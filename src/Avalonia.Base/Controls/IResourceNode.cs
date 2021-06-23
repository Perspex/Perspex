using System;

#nullable enable

namespace Avalonia.Controls
{
    /// <summary>
    /// Represents an object that can be queried for resources.
    /// </summary>
    /// <remarks>
    /// The interface represents a common interface for both controls that host resources
    /// (<see cref="IResourceHost"/>) and resource providers such as <see cref="ResourceDictionary"/>
    /// (see <see cref="IResourceProvider"/>).
    /// </remarks>
    public interface IResourceNode
    {
        /// <summary>
        /// Gets a value indicating whether the object has resources.
        /// </summary>
        bool HasResources { get; }

        /// <summary>
        /// Tries to find a resource within the object.
        /// </summary>
        /// <param name="key">The resource key.</param>
        /// <param name="value">
        /// When this method returns, contains the value associated with the specified key,
        /// if the key is found; otherwise, null.
        /// </param>
        /// <returns>
        /// True if the resource if found, otherwise false.
        /// </returns>
        bool TryGetResource(object key, out object? value);
    }
}
