using Avalonia.Automation.Peers;
using Avalonia.Platform;

#nullable enable

namespace Avalonia.Automation.Provider
{
    public interface IRootProvider
    {
        ITopLevelImpl? PlatformImpl { get; }
        AutomationPeer? GetFocus();
        AutomationPeer? GetPeerFromPoint(Point p);
    }
}
