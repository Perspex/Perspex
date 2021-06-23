namespace Avalonia.Layout
{
    /// <summary>
    /// Defines the root of a layoutable tree.
    /// </summary>
    public interface ILayoutRoot : ILayoutable
    {
        /// <summary>
        /// The size available to lay out the controls.
        /// </summary>
        Size ClientSize { get; }

        /// <summary>
        /// The scaling factor to use in layout.
        /// </summary>
        double LayoutScaling { get; }

        /// <summary>
        /// Associated instance of layout manager
        /// </summary>
        ILayoutManager LayoutManager { get; }
    }
}
