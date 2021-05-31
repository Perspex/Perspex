using System.Collections.Generic;
using Avalonia.Automation.Peers;

namespace Avalonia.Automation.Provider
{
    /// <summary>
    /// Exposes methods and properties to support access by a UI Automation client to controls
    /// that act as containers for a collection of individual, selectable child items.
    /// </summary>
    public interface ISelectionProvider
    {
        /// <summary>
        /// Gets a value that indicates whether the provider allows more than one child element
        /// to be selected concurrently.
        /// </summary>
        bool CanSelectMultiple { get; }

        /// <summary>
        /// Gets a value that indicates whether the provider requires at least one child element
        /// to be selected.
        /// </summary>
        bool IsSelectionRequired { get; }

        /// <summary>
        /// Retrieves a provider for each child element that is selected.
        /// </summary>
        IReadOnlyList<AutomationPeer> GetSelection();
    }
}
