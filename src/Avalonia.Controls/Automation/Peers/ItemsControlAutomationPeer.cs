using Avalonia.Automation.Platform;
using Avalonia.Automation.Provider;
using Avalonia.Controls;

#nullable enable

namespace Avalonia.Automation.Peers
{
    public class ItemsControlAutomationPeer : ControlAutomationPeer, IScrollProvider
    {
        private bool _searchedForScrollable;
        private IScrollProvider? _scroller;

        public ItemsControlAutomationPeer(IAutomationNodeFactory factory, ItemsControl owner)
            : base(factory, owner)
        {
        }

        public new ItemsControl Owner => (ItemsControl)base.Owner;
        public bool HorizontallyScrollable => _scroller?.HorizontallyScrollable ?? false;
        public double HorizontalScrollPercent => _scroller?.HorizontalScrollPercent ?? -1;
        public double HorizontalViewSize => _scroller?.HorizontalViewSize ?? 0;
        public bool VerticallyScrollable => _scroller?.VerticallyScrollable ?? false;
        public double VerticalScrollPercent => _scroller?.VerticalScrollPercent ?? -1;
        public double VerticalViewSize => _scroller?.VerticalViewSize ?? 0;

        protected virtual IScrollProvider? Scroller
        {
            get
            {
                if (!_searchedForScrollable)
                {
                    if (Owner.GetValue(ListBox.ScrollProperty) is Control scrollable)
                        _scroller = GetOrCreatePeer(scrollable) as IScrollProvider;
                    _searchedForScrollable = true;
                }

                return _scroller;
            }
        }

        protected override AutomationControlType GetAutomationControlTypeCore()
        {
            return AutomationControlType.List;
        }

        public void Scroll(ScrollAmount horizontalAmount, ScrollAmount verticalAmount)
        {
            _scroller?.Scroll(horizontalAmount, verticalAmount);
        }

        public void SetScrollPercent(double horizontalPercent, double verticalPercent)
        {
            _scroller?.SetScrollPercent(horizontalPercent, verticalPercent);
        }
    }
}
