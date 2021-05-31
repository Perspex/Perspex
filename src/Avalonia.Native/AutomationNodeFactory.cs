using Avalonia.Automation.Peers;
using Avalonia.Automation.Platform;
using Avalonia.Native.Interop;

namespace Avalonia.Native
{
    internal class AutomationNodeFactory : IAutomationNodeFactory
    {
        private static AutomationNodeFactory _instance;
        private readonly IAvaloniaNativeFactory _native;

        public static AutomationNodeFactory GetInstance(IAvaloniaNativeFactory native)
        {
            return _instance ??= new AutomationNodeFactory(native);
        }
        
        private AutomationNodeFactory(IAvaloniaNativeFactory native) => _native = native;
        
        public IAutomationNode CreateNode(AutomationPeer peer)
        {
            return new AutomationNode(this, _native.CreateAutomationNode(new AvnAutomationPeer(peer)));
        }
    }
}
