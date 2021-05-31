using System;
using System.Collections.Generic;
using Avalonia.Automation.Platform;

#nullable enable

namespace Avalonia.Automation.Peers
{
    /// <summary>
    /// An automation peer which represents an unrealized element
    /// </summary>
    public abstract class UnrealizedElementAutomationPeer : AutomationPeer
    {
        protected UnrealizedElementAutomationPeer(IAutomationNodeFactory factory)
            : base(factory)
        {
        }

        public void SetParent(AutomationPeer? parent) => TrySetParent(parent);
        protected override void BringIntoViewCore() => GetParent()?.BringIntoView();
        protected override Rect GetBoundingRectangleCore() => GetParent()?.GetBoundingRectangle() ?? default;
        protected override IReadOnlyList<AutomationPeer> GetOrCreateChildrenCore() => Array.Empty<AutomationPeer>();
        protected override bool HasKeyboardFocusCore() => false;
        protected override bool IsContentElementCore() => false;
        protected override bool IsControlElementCore() => false;
        protected override bool IsEnabledCore() => true;
        protected override bool IsKeyboardFocusableCore() => false;
        protected override void SetFocusCore() { }
        protected override bool ShowContextMenuCore() => false;
        protected internal override bool TrySetParent(AutomationPeer? parent) => false;
    }
}
