using System;

#nullable enable

namespace Avalonia.Controls
{
    /// <summary>
    /// Represents an element which hosts resources.
    /// </summary>
    /// <remarks>
    /// This interface is implemented by <see cref="StyledElement"/> and `Application`.
    /// </remarks>
    public interface IResourceHost : IResourceNode
    {
        /// <summary>
        /// Raised when the resources change on the element or an ancestor of the element.
        /// </summary>
        event EventHandler<ResourcesChangedEventArgs> ResourcesChanged;

        /// <summary>
        /// Notifies the resource host that one or more of its hosted resources has changed.
        /// </summary>
        /// <param name="e">The event args.</param>
        /// <remarks>
        /// This method will be called automatically by the framework, you should not need to call
        /// this method yourself. It is called when the resources hosted by this element have
        /// changed, and is usually called by a resource dictionary or style hosted by the element
        /// in response to a resource being added or removed.
        /// </remarks>
        void NotifyHostedResourcesChanged(ResourcesChangedEventArgs e);
    }
}
