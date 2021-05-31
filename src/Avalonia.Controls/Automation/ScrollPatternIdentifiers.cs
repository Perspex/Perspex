using Avalonia.Automation.Provider;

namespace Avalonia.Automation
{
    /// <summary>
    /// Contains values used as identifiers by <see cref="IScrollProvider"/>.
    /// </summary>
    public static class ScrollPatternIdentifiers
    {
        /// <summary>
        /// Specifies that scrolling should not be performed.
        /// </summary>
        public const double NoScroll = -1;

        /// <summary>
        /// Identifies <see cref="IScrollProvider.HorizontallyScrollable"/> automation property.
        /// </summary>
        public static AutomationProperty HorizontallyScrollableProperty { get; } = new AutomationProperty();

        /// <summary>
        /// Identifies <see cref="IScrollProvider.HorizontalScrollPercent"/> automation property.
        /// </summary>
        public static AutomationProperty HorizontalScrollPercentProperty { get; } = new AutomationProperty();

        /// <summary>
        /// Identifies <see cref="IScrollProvider.HorizontalViewSize"/> automation property.
        /// </summary>
        public static AutomationProperty HorizontalViewSizeProperty { get; } = new AutomationProperty();

        /// <summary>
        /// Identifies <see cref="IScrollProvider.VerticallyScrollable"/> automation property.
        /// </summary>
        public static AutomationProperty VerticallyScrollableProperty { get; } = new AutomationProperty();

        /// <summary>
        /// Identifies <see cref="IScrollProvider.VerticalScrollPercent"/> automation property.
        /// </summary>
        public static AutomationProperty VerticalScrollPercentProperty { get; } = new AutomationProperty();

        /// <summary>
        /// Identifies <see cref="IScrollProvider.VerticalViewSize"/> automation property.
        /// </summary>
        public static AutomationProperty VerticalViewSizeProperty { get; } = new AutomationProperty();
    }
}
