using Avalonia.Automation.Peers;

#nullable enable

namespace Avalonia.Automation.Platform
{
    /// <summary>
    /// Creates nodes in the UI Automation tree of the underlying platform.
    /// </summary>
    public interface IAutomationNodeFactory
    {
        /// <summary>
        /// Creates an automation node for a peer.
        /// </summary>
        /// <param name="peer">The peer.</param>
        IAutomationNode CreateNode(AutomationPeer peer);
    }
}
