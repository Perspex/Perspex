using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Platform;
using Avalonia.Native.Interop;

#nullable enable

namespace Avalonia.Native
{
    internal class AutomationNode : IRootAutomationNode
    {
        public AutomationNode(AutomationNodeFactory factory, IAvnAutomationNode native)
        {
            Native = native;
            Factory = factory;
        }

        public IAvnAutomationNode Native { get; }
        public IAutomationNodeFactory Factory { get; }

        public void ChildrenChanged() => Native.ChildrenChanged();

        public void PropertyChanged(AutomationProperty property, object? oldValue, object? newValue)
        {
            AvnAutomationProperty p;

            if (property == AutomationElementIdentifiers.BoundingRectangleProperty)
                p = AvnAutomationProperty.AutomationPeer_BoundingRectangle;
            else if (property == AutomationElementIdentifiers.ClassNameProperty)
                p = AvnAutomationProperty.AutomationPeer_ClassName;
            else if (property == AutomationElementIdentifiers.NameProperty)
                p = AvnAutomationProperty.AutomationPeer_Name;
            else if (property == RangeValuePatternIdentifiers.ValueProperty)
                p = AvnAutomationProperty.RangeValueProvider_Value;
            else
                return;
            
            Native.PropertyChanged(p);
        }

        public void FocusChanged(AutomationPeer? focus) => Native.FocusChanged(AvnAutomationPeer.Wrap(focus));
    }
}
