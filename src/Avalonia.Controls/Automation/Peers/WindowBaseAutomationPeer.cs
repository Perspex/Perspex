using System.ComponentModel;
using Avalonia.Automation.Platform;
using Avalonia.Automation.Provider;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Platform;
using Avalonia.VisualTree;

#nullable enable

namespace Avalonia.Automation.Peers
{
    public class WindowBaseAutomationPeer : ControlAutomationPeer, IRootProvider
    {
        private Control? _focus;

        public WindowBaseAutomationPeer(IAutomationNode node, WindowBase owner)
            : base(node, owner)
        {
        }

        public new WindowBase Owner => (WindowBase)base.Owner;
        public ITopLevelImpl PlatformImpl => Owner.PlatformImpl;

        protected override AutomationControlType GetAutomationControlTypeCore()
        {
            return AutomationControlType.Window;
        }

        public AutomationPeer? GetFocus() => _focus is object ? GetOrCreatePeer(_focus) : null;

        public AutomationPeer? GetPeerFromPoint(Point p)
        {
            var hit = Owner.GetVisualAt(p)?.FindAncestorOfType<Control>(includeSelf: true);
            return hit is object ? GetOrCreatePeer(hit) : null;
        }

        protected void StartTrackingFocus()
        {
            KeyboardDevice.Instance.PropertyChanged += KeyboardDevicePropertyChanged;
            OnFocusChanged(KeyboardDevice.Instance.FocusedElement);
        }

        protected void StopTrackingFocus()
        {
            KeyboardDevice.Instance.PropertyChanged -= KeyboardDevicePropertyChanged;
        }

        private void OnFocusChanged(IInputElement? focus)
        {
            var oldFocus = _focus;
            
            _focus = focus?.VisualRoot == Owner ? focus as Control : null;
            
            if (_focus != oldFocus)
            {
                var peer = _focus is object ? GetOrCreatePeer(_focus) : null;
                ((IRootAutomationNode)Node).FocusChanged(peer);
            }
        }

        private void KeyboardDevicePropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(KeyboardDevice.FocusedElement))
            {
                OnFocusChanged(KeyboardDevice.Instance.FocusedElement);
            }
        }
    }
}


