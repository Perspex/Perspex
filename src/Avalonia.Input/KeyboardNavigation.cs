namespace Avalonia.Input
{
    /// <summary>
    /// Defines attached properties that control keyboard navigation behaviour for a container.
    /// </summary>
    public static class KeyboardNavigation
    {
        /// <summary>
        /// Defines the TabIndex attached property.
        /// </summary>
        public static readonly AttachedProperty<int> TabIndexProperty =
            AvaloniaProperty.RegisterAttached<StyledElement, int>(
                "TabIndex",
                typeof(KeyboardNavigation),
                int.MaxValue);

        /// <summary>
        /// Defines the TabNavigation attached property.
        /// </summary>
        /// <remarks>
        /// The TabNavigation attached property defines how pressing the Tab key causes focus to
        /// be navigated between the children of the container.
        /// </remarks>
        public static readonly AttachedProperty<KeyboardNavigationMode> TabNavigationProperty =
            AvaloniaProperty.RegisterAttached<InputElement, KeyboardNavigationMode>(
                "TabNavigation",
                typeof(KeyboardNavigation));

        /// <summary>
        /// Defines the TabOnceActiveElement attached property.
        /// </summary>
        /// <remarks>
        /// When focus enters a container which has its <see cref="TabNavigationProperty"/>
        /// attached property set to <see cref="KeyboardNavigationMode.Once"/>, this property
        /// defines to which child the focus should move.
        /// </remarks>
        public static readonly AttachedProperty<IInputElement?> TabOnceActiveElementProperty =
            AvaloniaProperty.RegisterAttached<InputElement, IInputElement?>(
                "TabOnceActiveElement",
                typeof(KeyboardNavigation));

        /// <summary>
        /// Defines the IsTabStop attached property.
        /// </summary>
        /// <remarks>
        /// The IsTabStop attached property determines whether the control is focusable by tab navigation. 
        /// </remarks>
        public static readonly AttachedProperty<bool> IsTabStopProperty =
            AvaloniaProperty.RegisterAttached<InputElement, bool>(
                "IsTabStop",
                typeof(KeyboardNavigation), 
                true);

        /// <summary>
        /// Gets the <see cref="TabIndexProperty"/> for an element.
        /// </summary>
        /// <param name="element">The container.</param>
        /// <returns>The <see cref="KeyboardNavigationMode"/> for the container.</returns>
        public static int GetTabIndex(IInputElement element)
        {
            return ((IAvaloniaObject)element).GetValue(TabIndexProperty);
        }

        /// <summary>
        /// Sets the <see cref="TabIndexProperty"/> for an element.
        /// </summary>
        /// <param name="element">The element.</param>
        /// <param name="value">The tab index.</param>
        public static void SetTabIndex(IInputElement element, int value)
        {
            ((IAvaloniaObject)element).SetValue(TabIndexProperty, value);
        }

        /// <summary>
        /// Gets the <see cref="TabNavigationProperty"/> for a container.
        /// </summary>
        /// <param name="element">The container.</param>
        /// <returns>The <see cref="KeyboardNavigationMode"/> for the container.</returns>
        public static KeyboardNavigationMode GetTabNavigation(InputElement element)
        {
            return element.GetValue(TabNavigationProperty);
        }

        /// <summary>
        /// Sets the <see cref="TabNavigationProperty"/> for a container.
        /// </summary>
        /// <param name="element">The container.</param>
        /// <param name="value">The <see cref="KeyboardNavigationMode"/> for the container.</param>
        public static void SetTabNavigation(InputElement element, KeyboardNavigationMode value)
        {
            element.SetValue(TabNavigationProperty, value);
        }

        /// <summary>
        /// Gets the <see cref="TabOnceActiveElementProperty"/> for a container.
        /// </summary>
        /// <param name="element">The container.</param>
        /// <returns>The active element for the container.</returns>
        public static IInputElement? GetTabOnceActiveElement(InputElement element)
        {
            return element.GetValue(TabOnceActiveElementProperty);
        }

        /// <summary>
        /// Sets the <see cref="TabOnceActiveElementProperty"/> for a container.
        /// </summary>
        /// <param name="element">The container.</param>
        /// <param name="value">The active element for the container.</param>
        public static void SetTabOnceActiveElement(InputElement element, IInputElement? value)
        {
            element.SetValue(TabOnceActiveElementProperty, value);
        }

        /// <summary>
        /// Sets the <see cref="IsTabStopProperty"/> for an element.
        /// </summary>
        /// <param name="element">The container.</param>
        /// <param name="value">Value indicating whether the container is a tab stop.</param>
        public static void SetIsTabStop(InputElement element, bool value)
        {
            element.SetValue(IsTabStopProperty, value);
        }

        /// <summary>
        /// Gets the <see cref="IsTabStopProperty"/> for an element.
        /// </summary>
        /// <param name="element">The container.</param>
        /// <returns>Whether the container is a tab stop.</returns>
        public static bool GetIsTabStop(InputElement element)
        {
            return element.GetValue(IsTabStopProperty);
        }
    }
}
