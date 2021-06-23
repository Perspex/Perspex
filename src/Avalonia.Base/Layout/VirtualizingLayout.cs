// This source file is adapted from the WinUI project.
// (https://github.com/microsoft/microsoft-ui-xaml)
//
// Licensed to The Avalonia Project under MIT License, courtesy of The .NET Foundation.

using System.Collections.Specialized;

namespace Avalonia.Layout
{
    /// <summary>
    /// Represents the base class for an object that sizes and arranges child elements for a host
    /// and supports virtualization.
    /// </summary>
    /// <remarks>
    /// <see cref="VirtualizingLayout"/> is the base class for layouts that support virtualization.
    /// You can use one of the provided derived class, or inherit from it to create your own layout.
    /// Provided concrete virtualizing layout classes are <see cref="StackLayout"/> and 
    /// <see cref="UniformGridLayout"/>.
    /// </remarks>
    public abstract class VirtualizingLayout : AttachedLayout
    {
        /// <summary>
        /// Notifies the layout when the data collection assigned to the container element (Items)
        /// has changed.
        /// </summary>
        /// <param name="context">
        /// The context object that facilitates communication between the layout and its host
        /// container.
        /// </param>
        /// <param name="source">The data source.</param>
        /// <param name="args">Data about the collection change.</param>
        /// <remarks>
        /// Override <see cref="OnItemsChangedCore(VirtualizingLayoutContext, object, NotifyCollectionChangedEventArgs)"/>
        /// to provide the behavior for this method in a derived class.
        /// </remarks>
        public void OnItemsChanged(
            VirtualizingLayoutContext context,
            object source,
            NotifyCollectionChangedEventArgs args) => OnItemsChangedCore(context, source, args);

        /// <summary>
        /// When overridden in a derived class, initializes any per-container state the layout
        /// requires when it is attached to an ILayoutable container.
        /// </summary>
        /// <param name="context">
        /// The context object that facilitates communication between the layout and its host
        /// container.
        /// </param>
        protected internal virtual void InitializeForContextCore(VirtualizingLayoutContext context)
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
        protected internal virtual void UninitializeForContextCore(VirtualizingLayoutContext context)
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
            VirtualizingLayoutContext context,
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
            VirtualizingLayoutContext context,
            Size finalSize) => finalSize;

        /// <summary>
        /// Notifies the layout when the data collection assigned to the container element (Items)
        /// has changed.
        /// </summary>
        /// <param name="context">
        /// The context object that facilitates communication between the layout and its host
        /// container.
        /// </param>
        /// <param name="source">The data source.</param>
        /// <param name="args">Data about the collection change.</param>
        protected internal virtual void OnItemsChangedCore(
            VirtualizingLayoutContext context,
            object source,
            NotifyCollectionChangedEventArgs args) => InvalidateMeasure();
    }
}
