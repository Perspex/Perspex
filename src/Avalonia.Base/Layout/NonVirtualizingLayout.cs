// This source file is adapted from the WinUI project.
// (https://github.com/microsoft/microsoft-ui-xaml)
//
// Licensed to The Avalonia Project under MIT License, courtesy of The .NET Foundation.

namespace Avalonia.Layout
{
    /// <summary>
    /// Represents the base class for an object that sizes and arranges child elements for a host
    /// and and does not support virtualization.
    /// </summary>
    /// <remarks>
    /// NonVirtualizingLayout is the base class for layouts that do not support virtualization. You
    /// can inherit from it to create your own layout.
    /// 
    /// A non-virtualizing layout can measure and arrange child elements.
    /// </remarks>
    public abstract class NonVirtualizingLayout : AttachedLayout
    {
        /// <summary>
        /// When overridden in a derived class, initializes any per-container state the layout
        /// requires when it is attached to an ILayoutable container.
        /// </summary>
        /// <param name="context">
        /// The context object that facilitates communication between the layout and its host
        /// container.
        /// </param>
        protected internal virtual void InitializeForContextCore(LayoutContext context)
        {
        }

        /// <summary>
        /// When overridden in a derived class, removes any state the layout previously stored on
        /// the ILayoutable container.
        /// </summary>
        /// <param name="context">
        /// The context object that facilitates communication between the layout and its host
        /// container.
        /// </param>
        protected internal virtual void UninitializeForContextCore(LayoutContext context)
        {
        }

        /// <summary>
        /// Provides the behavior for the "Measure" pass of the layout cycle. Classes can override
        /// this method to define their own "Measure" pass behavior.
        /// </summary>
        /// <param name="context">
        /// The context object that facilitates communication between the layout and its host
        /// container.
        /// </param>
        /// <param name="availableSize">
        /// The available size that this object can give to child objects. Infinity can be
        /// specified as a value to indicate that the object will size to whatever content is
        /// available.
        /// </param>
        /// <returns>
        /// The size that this object determines it needs during layout, based on its calculations
        /// of the allocated sizes for child objects or based on other considerations such as a
        /// fixed container size.
        /// </returns>
        protected internal abstract Size MeasureOverride(
            NonVirtualizingLayoutContext context,
            Size availableSize);

        /// <summary>
        /// When implemented in a derived class, provides the behavior for the "Arrange" pass of
        /// layout. Classes can override this method to define their own "Arrange" pass behavior.
        /// </summary>
        /// <param name="context">
        /// The context object that facilitates communication between the layout and its host
        /// container.
        /// </param>
        /// <param name="finalSize">
        /// The final area within the container that this object should use to arrange itself and
        /// its children.
        /// </param>
        /// <returns>The actual size that is used after the element is arranged in layout.</returns>
        protected internal virtual Size ArrangeOverride(
            NonVirtualizingLayoutContext context,
            Size finalSize) => finalSize;
    }
}
