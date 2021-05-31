using Avalonia.Automation.Provider;

namespace Avalonia.Automation
{
    /// <summary>
    /// Contains values used as identifiers by <see cref="IRangeValueProvider"/>.
    /// </summary>
    public static class RangeValuePatternIdentifiers
    {
        /// <summary>
        /// Identifies <see cref="IRangeValueProvider.IsReadOnly"/> automation property.
        /// </summary>
        public static AutomationProperty IsReadOnlyProperty { get; } = new AutomationProperty();

        /// <summary>
        /// Identifies <see cref="IRangeValueProvider.Minimum"/> automation property.
        /// </summary>
        public static AutomationProperty MinimumProperty { get; } = new AutomationProperty();

        /// <summary>
        /// Identifies <see cref="IRangeValueProvider.Maximum"/> automation property.
        /// </summary>
        public static AutomationProperty MaximumProperty { get; } = new AutomationProperty();

        /// <summary>
        /// Identifies <see cref="IRangeValueProvider.Value"/> automation property.
        /// </summary>
        public static AutomationProperty ValueProperty { get; } = new AutomationProperty();
    }
}
