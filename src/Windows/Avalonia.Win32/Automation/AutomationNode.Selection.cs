using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using UIA = Avalonia.Win32.Interop.Automation;

#nullable enable

namespace Avalonia.Win32.Automation
{
    internal partial class AutomationNode : UIA.ISelectionProvider, UIA.ISelectionItemProvider
    {
        public bool CanSelectMultiple => InvokeSync<ISelectionProvider, bool>(x => x.CanSelectMultiple);
        public bool IsSelectionRequired => InvokeSync<ISelectionProvider, bool>(x => x.IsSelectionRequired);
        public bool IsSelected => InvokeSync<ISelectionItemProvider, bool>(x => x.IsSelected);
        
        public UIA.IRawElementProviderSimple? SelectionContainer
        {
            get
            {
                var peer = InvokeSync<ISelectionItemProvider, ISelectionProvider?>(x => x.SelectionContainer);
                return (peer as AutomationPeer)?.Node as AutomationNode;
            }
        }

        public UIA.IRawElementProviderSimple[] GetSelection()
        {
            var peers = InvokeSync<ISelectionProvider, IReadOnlyList<AutomationPeer>>(x => x.GetSelection());
            return peers?.Select(x => (UIA.IRawElementProviderSimple)x.Node).ToArray() ??
                Array.Empty<UIA.IRawElementProviderSimple>();
        }

        public void AddToSelection() => InvokeSync<ISelectionItemProvider>(x => x.AddToSelection());
        public void RemoveFromSelection() => InvokeSync<ISelectionItemProvider>(x => x.RemoveFromSelection());
        public void Select() => InvokeSync<ISelectionItemProvider>(x => x.Select());
    }
}
