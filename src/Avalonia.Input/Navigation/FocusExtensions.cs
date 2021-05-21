namespace Avalonia.Input.Navigation
{
    /// <summary>
    /// Provides extension methods relating to control focus.
    /// </summary>
    internal static class FocusExtensions
    {
        /// <summary>
        /// Checks if the specified element can be focused.
        /// </summary>
        /// <param name="e">The element.</param>
        /// <returns>True if the element can be focused.</returns>
        public static bool CanFocus(this IInputElement e) => e.Focusable && e.IsEffectivelyEnabled && e.IsClosestVisualVisible();

        /// <summary>
        /// Checks if descendants of the specified element can be focused.
        /// </summary>
        /// <param name="e">The element.</param>
        /// <returns>True if descendants of the element can be focused.</returns>
        public static bool CanFocusDescendants(this IInputElement e) => e.IsEffectivelyEnabled && e.IsClosestVisualVisible();
    }
}
