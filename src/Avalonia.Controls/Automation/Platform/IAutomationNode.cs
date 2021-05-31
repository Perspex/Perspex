using System;
using Avalonia.Automation.Peers;

#nullable enable

namespace Avalonia.Automation.Platform
{
    /// <summary>
    /// Represents a platform implementation of a node in the UI Automation tree.
    /// </summary>
    public interface IAutomationNode
    {
        /// <summary>
        /// Gets a factory which can be used to create child nodes.
        /// </summary>
        IAutomationNodeFactory Factory { get; }

        /// <summary>
        /// Called by the <see cref="AutomationPeer"/> when the children of the peer change.
        /// </summary>
        void ChildrenChanged();

        /// <summary>
        /// Called by the <see cref="AutomationPeer"/> when a property other than the parent,
        /// children or root changes.
        /// </summary>
        /// <param name="property">The property that changed.</param>
        /// <param name="oldValue">The previous value of the property.</param>
        /// <param name="newValue">The new value of the property.</param>
        void PropertyChanged(AutomationProperty property, object? oldValue, object? newValue);
    }
}
