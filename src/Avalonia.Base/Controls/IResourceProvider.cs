using System;
using Avalonia.Styling;

#nullable enable

namespace Avalonia.Controls
{
    /// <summary>
    /// Represents an object that can be queried for resources but does not appear in the logical tree.
    /// </summary>
    /// <remarks>
    /// This interface is implemented by <see cref="ResourceDictionary"/>, <see cref="Style"/> and
    /// <see cref="Styles"/>
    /// </remarks>
    public interface IResourceProvider : IResourceNode
    {
        /// <summary>
        /// Gets the owner of the resource provider.
        /// </summary>
        /// <remarks>
        /// If multiple owners are added, returns the first.
        /// </remarks>
        IResourceHost? Owner { get; }

        /// <summary>
        /// Raised when the <see cref="Owner"/> of the resource provider changes.
        /// </summary>
        event EventHandler OwnerChanged;

        /// <summary>
        /// Adds an owner to the resource provider.
        /// </summary>
        /// <param name="owner">The owner.</param>
        void AddOwner(IResourceHost owner);

        /// <summary>
        /// Removes a resource provider owner.
        /// </summary>
        /// <param name="owner">The owner.</param>
        void RemoveOwner(IResourceHost owner);
    }
}
