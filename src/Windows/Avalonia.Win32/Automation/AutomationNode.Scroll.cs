using Avalonia.Automation.Provider;
using UIA = Avalonia.Win32.Interop.Automation;

#nullable enable

namespace Avalonia.Win32.Automation
{
    internal partial class AutomationNode : UIA.IScrollProvider, UIA.IScrollItemProvider
    {
        public bool HorizontallyScrollable => InvokeSync<IScrollProvider, bool>(x => x.HorizontallyScrollable);
        public double HorizontalScrollPercent => InvokeSync<IScrollProvider, double>(x => x.HorizontalScrollPercent);
        public double HorizontalViewSize => InvokeSync<IScrollProvider, double>(x => x.HorizontalViewSize);
        public bool VerticallyScrollable => InvokeSync<IScrollProvider, bool>(x => x.VerticallyScrollable);
        public double VerticalScrollPercent => InvokeSync<IScrollProvider, double>(x => x.VerticalScrollPercent);
        public double VerticalViewSize => InvokeSync<IScrollProvider, double>(x => x.VerticalViewSize);

        public void Scroll(ScrollAmount horizontalAmount, ScrollAmount verticalAmount)
        {
            InvokeSync<IScrollProvider>(x => x.Scroll(horizontalAmount, verticalAmount));
        }

        public void SetScrollPercent(double horizontalPercent, double verticalPercent)
        {
            InvokeSync<IScrollProvider>(x => x.SetScrollPercent(horizontalPercent, verticalPercent));
        }

        public void ScrollIntoView()
        {
            InvokeSync(() => Peer.BringIntoView());
        }
    }
}
