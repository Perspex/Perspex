using System;
using Avalonia.Automation.Platform;
using Avalonia.Automation.Provider;
using Avalonia.Controls;
using Avalonia.Utilities;

#nullable enable

namespace Avalonia.Automation.Peers
{
    public class ScrollViewerAutomationPeer : ControlAutomationPeer, IScrollProvider
    {
        public ScrollViewerAutomationPeer(IAutomationNodeFactory factory, ScrollViewer owner)
            : base(factory, owner)
        {
        }

        public new ScrollViewer Owner => (ScrollViewer)base.Owner;

        public bool HorizontallyScrollable
        {
            get => MathUtilities.GreaterThan(Owner.Extent.Width, Owner.Viewport.Width);
        }

        public double HorizontalScrollPercent
        {
            get
            {
                if (!HorizontallyScrollable)
                    return ScrollPatternIdentifiers.NoScroll;
                return (double)(Owner.Offset.X * 100.0 / (Owner.Extent.Width - Owner.Viewport.Width));
            }
        }

        public double HorizontalViewSize
        {
            get
            {
                if (MathUtilities.IsZero(Owner.Extent.Width))
                    return 100;
                return Math.Min(100, Owner.Viewport.Width * 100.0 / Owner.Extent.Width);
            }
        }

        public bool VerticallyScrollable
        {
            get => MathUtilities.GreaterThan(Owner.Extent.Height, Owner.Viewport.Height);
        }

        public double VerticalScrollPercent
        {
            get
            {
                if (!VerticallyScrollable)
                    return ScrollPatternIdentifiers.NoScroll;
                return (double)(Owner.Offset.Y * 100.0 / (Owner.Extent.Height - Owner.Viewport.Height));
            }
        }

        public double VerticalViewSize
        {
            get
            {
                if (MathUtilities.IsZero(Owner.Extent.Height))
                    return 100;
                return Math.Min(100, Owner.Viewport.Height * 100.0 / Owner.Extent.Height);
            }
        }

        protected override AutomationControlType GetAutomationControlTypeCore()
        {
            return AutomationControlType.Pane;
        }

        protected override bool IsContentElementCore() => false;

        protected override bool IsControlElementCore()
        {
            // Return false if the control is part of a control template.
            return Owner.TemplatedParent is null && base.IsControlElementCore();
        }

        public void Scroll(ScrollAmount horizontalAmount, ScrollAmount verticalAmount)
        {
            if (!IsEnabled())
                throw new ElementNotEnabledException();

            var scrollHorizontally = horizontalAmount != ScrollAmount.NoAmount;
            var scrollVertically = verticalAmount != ScrollAmount.NoAmount;

            if (scrollHorizontally && !HorizontallyScrollable || scrollVertically && !VerticallyScrollable)
            {
                throw new InvalidOperationException("Operation cannot be performed");
            }

            switch (horizontalAmount)
            {
                case ScrollAmount.LargeDecrement:
                    Owner.PageLeft();
                    break;
                case ScrollAmount.SmallDecrement:
                    Owner.LineLeft();
                    break;
                case ScrollAmount.SmallIncrement:
                    Owner.LineRight();
                    break;
                case ScrollAmount.LargeIncrement:
                    Owner.PageRight();
                    break;
                case ScrollAmount.NoAmount:
                    break;
                default:
                    throw new InvalidOperationException("Operation cannot be performed");
            }

            switch (verticalAmount)
            {
                case ScrollAmount.LargeDecrement:
                    Owner.PageUp();
                    break;
                case ScrollAmount.SmallDecrement:
                    Owner.LineUp();
                    break;
                case ScrollAmount.SmallIncrement:
                    Owner.LineDown();
                    break;
                case ScrollAmount.LargeIncrement:
                    Owner.PageDown();
                    break;
                case ScrollAmount.NoAmount:
                    break;
                default:
                    throw new InvalidOperationException("Operation cannot be performed");
            }
        }

        public void SetScrollPercent(double horizontalPercent, double verticalPercent)
        {
            if (!IsEnabled())
                throw new ElementNotEnabledException();

            var scrollHorizontally = horizontalPercent != ScrollPatternIdentifiers.NoScroll;
            var scrollVertically = verticalPercent != ScrollPatternIdentifiers.NoScroll;

            if (scrollHorizontally && !HorizontallyScrollable || scrollVertically && !VerticallyScrollable)
            {
                throw new InvalidOperationException("Operation cannot be performed");
            }

            if (scrollHorizontally && (horizontalPercent < 0.0) || (horizontalPercent > 100.0))
            {
                throw new ArgumentOutOfRangeException("horizontalPercent");
            }

            if (scrollVertically && (verticalPercent < 0.0) || (verticalPercent > 100.0))
            {
                throw new ArgumentOutOfRangeException("verticalPercent");
            }

            var offset = Owner.Offset;

            if (scrollHorizontally)
            {
                offset = offset.WithX((Owner.Extent.Width - Owner.Viewport.Width) * horizontalPercent * 0.01);
            }
            
            if (scrollVertically)
            {
                offset = offset.WithY((Owner.Extent.Height - Owner.Viewport.Height) * verticalPercent * 0.01);
            }

            Owner.Offset = offset;
        }
    }
}
