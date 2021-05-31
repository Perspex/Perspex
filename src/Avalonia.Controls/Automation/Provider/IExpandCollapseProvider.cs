namespace Avalonia.Automation.Provider
{
    /// <summary>
    /// Exposes methods and properties to support UI Automation client access to controls that
    /// visually expand to display content and collapse to hide content.
    /// </summary>
    public interface IExpandCollapseProvider
    {
        /// <summary>
        /// Gets the state, expanded or collapsed, of the control.
        /// </summary>
        ExpandCollapseState ExpandCollapseState { get; }

        /// <summary>
        /// Gets a value indicating whether expanding the element shows a menu of items to the user,
        /// such as drop-down list.
        /// </summary>
        /// <remarks>
        /// Used in OSX to enable the "Show Menu" action on the element.
        /// </remarks>
        bool ShowsMenu { get; }
        
        /// <summary>
        /// Displays all child nodes, controls, or content of the control.
        /// </summary>
        void Expand();

        /// <summary>
        /// Hides all nodes, controls, or content that are descendants of the control.
        /// </summary>
        void Collapse();
    }
}
