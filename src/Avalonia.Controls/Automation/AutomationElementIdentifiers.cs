using Avalonia.Automation.Peers;

namespace Avalonia.Automation
{
    /// <summary>
    /// Contains values used as automation property identifiers by UI Automation providers.
    /// </summary>
    public static class AutomationElementIdentifiers
    {
        /// <summary>
        /// Identifies the bounding rectangle automation property. The bounding rectangle property
        /// value is returned by the <see cref="AutomationPeer.GetBoundingRectangle"/> method.
        /// </summary>
        public static AutomationProperty BoundingRectangleProperty { get; } = new AutomationProperty();

        /// <summary>
        /// Identifies the class name automation property. The class name property value is returned
        /// by the <see cref="AutomationPeer.GetClassName"/> method.
        /// </summary>
        public static AutomationProperty ClassNameProperty { get; } = new AutomationProperty();

        /// <summary>
        /// Identifies the name automation property. The class name property value is returned
        /// by the <see cref="AutomationPeer.GetName"/> method.
        /// </summary>
        public static AutomationProperty NameProperty { get; } = new AutomationProperty();
    }
}
