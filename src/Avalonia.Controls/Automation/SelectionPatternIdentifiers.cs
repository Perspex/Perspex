using Avalonia.Automation.Provider;

namespace Avalonia.Automation
{
    /// <summary>
    /// Contains values used as identifiers by <see cref="ISelectionProvider"/>.
    /// </summary>
    public static class SelectionPatternIdentifiers
    {
        /// <summary>
        /// Identifies <see cref="ISelectionProvider.CanSelectMultiple"/> automation property.
        /// </summary>
        public static AutomationProperty CanSelectMultipleProperty { get; } = new AutomationProperty();

        /// <summary>
        /// Identifies <see cref="ISelectionProvider.IsSelectionRequired"/> automation property.
        /// </summary>
        public static AutomationProperty IsSelectionRequiredProperty { get; } = new AutomationProperty();

        /// <summary>
        /// Identifies the property that gets the selected items in a container.
        /// </summary>
        public static AutomationProperty SelectionProperty { get; } = new AutomationProperty();
    }
}
