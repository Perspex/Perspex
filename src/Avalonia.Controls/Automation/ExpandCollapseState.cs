namespace Avalonia.Automation
{
    /// <summary>
    /// Contains values that specify the <see cref="ExpandCollapseState"/> of a UI Automation element.
    /// </summary>
    public enum ExpandCollapseState
    {
        /// <summary>
        /// No child nodes, controls, or content of the UI Automation element are displayed.
        /// </summary>
        Collapsed,

        /// <summary>
        /// All child nodes, controls or content of the UI Automation element are displayed.
        /// </summary>
        Expanded,

        /// <summary>
        /// The UI Automation element has no child nodes, controls, or content to display.
        /// </summary>
        LeafNode,

        /// <summary>
        /// Some, but not all, child nodes, controls, or content of the UI Automation element are
        /// displayed.
        /// </summary>
        PartiallyExpanded
    }
}
