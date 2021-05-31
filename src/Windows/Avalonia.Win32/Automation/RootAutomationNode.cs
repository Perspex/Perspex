using System;
using System.Runtime.InteropServices;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Platform;
using Avalonia.Automation.Provider;
using Avalonia.Win32.Interop.Automation;

#nullable enable

namespace Avalonia.Win32.Automation
{
    internal class RootAutomationNode : AutomationNode,
        IRawElementProviderFragmentRoot,
        IRootAutomationNode
    {
        public RootAutomationNode(Func<IAutomationNode, AutomationPeer> peerGetter)
            : base(peerGetter)
        {
        }

        public override IRawElementProviderFragmentRoot? FragmentRoot => this;
        public new IRootProvider Peer => (IRootProvider)base.Peer;
        public WindowImpl? WindowImpl => Peer.PlatformImpl as WindowImpl;
        
        public IRawElementProviderFragment? ElementProviderFromPoint(double x, double y)
        {
            if (WindowImpl is null)
                return null;

            var p = WindowImpl.PointToClient(new PixelPoint((int)x, (int)y));
            var peer = (WindowBaseAutomationPeer)Peer;
            var found = InvokeSync(() => peer.GetPeerFromPoint(p));
            var result = found?.Node as IRawElementProviderFragment;
            return result;
        }

        public IRawElementProviderFragment? GetFocus()
        {
            var focus = InvokeSync(() => Peer.GetFocus());
            return (AutomationNode?)focus?.Node;
        }

        public void FocusChanged(AutomationPeer? focus)
        {
            var node = focus?.Node as AutomationNode;
            RaiseFocusChanged(node);
        }

        public Rect ToScreen(Rect rect)
        {
            if (WindowImpl is null)
                return default;
            return new PixelRect(
                WindowImpl.PointToScreen(rect.TopLeft),
                WindowImpl.PointToScreen(rect.BottomRight))
                    .ToRect(1);
        }

        public override IRawElementProviderSimple? HostRawElementProvider
        {
            get
            {
                var handle = WindowImpl?.Handle.Handle ?? IntPtr.Zero;
                if (handle == IntPtr.Zero)
                    return null;
                var hr = UiaCoreProviderApi.UiaHostProviderFromHwnd(handle, out var result);
                Marshal.ThrowExceptionForHR(hr);
                return result;
            }
        }
    }
}
