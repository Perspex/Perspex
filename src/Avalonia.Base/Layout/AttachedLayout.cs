// This source file is adapted from the WinUI project.
// (https://github.com/microsoft/microsoft-ui-xaml)
//
// Licensed to The Avalonia Project under MIT License, courtesy of The .NET Foundation.

using System;

namespace Avalonia.Layout
{
    /// <summary>
    /// Represents the base class for an object that sizes and arranges child elements for a host.
    /// </summary>
    public abstract class AttachedLayout : AvaloniaObject
    {
        public string LayoutId { get; set; }

        /// <summary>
        /// Occurs when the measurement state (layout) has been invalidated.
        /// </summary>
        public event EventHandler MeasureInvalidated;

        /// <summary>
        /// Occurs when the arrange state (layout) has been invalidated.
        /// </summary>
        public event EventHandler ArrangeInvalidated;

        /// <summary>
        /// Initializes any per-container state the layout requires when it is attached to an
        /// <see cref="ILayoutable"/> container.
        /// </summary>
        /// <param name="context">
        /// The context object that facilitates communication between the layout and its host
        /// container.
        /// </param>
        /// <remarks>
        /// Container elements that support attached layouts should call this method when a layout
        /// instance is first assigned. The container is expected to give the attached layout
        /// instance a way to store and retrieve any per-container state by way of the provided
        /// context. It is also the responsibility of the container to not reuse the context, or
        /// otherwise expose the state from one layout to another.
        ///
        /// When an attached layout is removed the container should release any reference to the
        /// layout state it stored.
        /// 
        /// Override <see cref="NonVirtualizingLayout.InitializeForContextCore"/> or
        /// <see cref="VirtualizingLayout.InitializeForContextCore"/> to provide the behavior for
        /// this method in a derived class.
        /// </remarks>
        public void InitializeForContext(LayoutContext context)
        {
            if (this is VirtualizingLayout virtualizingLayout)
            {
                var virtualizingContext = GetVirtualizingLayoutContext(context);
                virtualizingLayout.InitializeForContextCore(virtualizingContext);
            }
            else if (this is NonVirtualizingLayout nonVirtualizingLayout)
            {
                var nonVirtualizingContext = GetNonVirtualizingLayoutContext(context);
                nonVirtualizingLayout.InitializeForContextCore(nonVirtualizingContext);
            }
            else
            {
                throw new NotSupportedException();
            }
        }

        /// <summary>
        /// Removes any state the layout previously stored on the ILayoutable container.
        /// </summary>
        /// <param name="context">
        /// The context object that facilitates communication between the layout and its host
        /// container.
        /// </param>
        public void UninitializeForContext(LayoutContext context)
        {
            if (this is VirtualizingLayout virtualizingLayout)
            {
                var virtualizingContext = GetVirtualizingLayoutContext(context);
                virtualizingLayout.UninitializeForContextCore(virtualizingContext);
            }
            else if (this is NonVirtualizingLayout nonVirtualizingLayout)
            {
                var nonVirtualizingContext = GetNonVirtualizingLayoutContext(context);
                nonVirtualizingLayout.UninitializeForContextCore(nonVirtualizingContext);
            }
            else
            {
                throw new NotSupportedException();
            }
        }

        /// <summary>
        /// Suggests a DesiredSize for a container element. A container element that supports
        /// attached layouts should call this method from their own MeasureOverride implementations
        /// to form a recursive layout update. The attached layout is expected to call the Measure
        /// for each of the container’s ILayoutable children.
        /// </summary>
        /// <param name="context">
        /// The context object that facilitates communication between the layout and its host
        /// container.
        /// </param>
        /// <param name="availableSize">
        /// The available space that a container can allocate to a child object. A child object can
        /// request a larger space than what is available; the provided size might be accommodated
        /// if scrolling or other resize behavior is possible in that particular container.
        /// </param>
        /// <returns></returns>
        public Size Measure(LayoutContext context, Size availableSize)
        {
            if (this is VirtualizingLayout virtualizingLayout)
            {
                var virtualizingContext = GetVirtualizingLayoutContext(context);
                return virtualizingLayout.MeasureOverride(virtualizingContext, availableSize);
            }
            else if (this is NonVirtualizingLayout nonVirtualizingLayout)
            {
                var nonVirtualizingContext = GetNonVirtualizingLayoutContext(context);
                return nonVirtualizingLayout.MeasureOverride(nonVirtualizingContext, availableSize);
            }
            else
            {
                throw new NotSupportedException();
            }
        }

        /// <summary>
        /// Positions child elements and determines a size for a container UIElement. Container
        /// elements that support attached layouts should call this method from their layout
        /// override implementations to form a recursive layout update.
        /// </summary>
        /// <param name="context">
        /// The context object that facilitates communication between the layout and its host
        /// container.
        /// </param>
        /// <param name="finalSize">
        /// The final size that the container computes for the child in layout.
        /// </param>
        /// <returns>The actual size that is used after the element is arranged in layout.</returns>
        public Size Arrange(LayoutContext context, Size finalSize)
        {
            if (this is VirtualizingLayout virtualizingLayout)
            {
                var virtualizingContext = GetVirtualizingLayoutContext(context);
                return virtualizingLayout.ArrangeOverride(virtualizingContext, finalSize);
            }
            else if (this is NonVirtualizingLayout nonVirtualizingLayout)
            {
                var nonVirtualizingContext = GetNonVirtualizingLayoutContext(context);
                return nonVirtualizingLayout.ArrangeOverride(nonVirtualizingContext, finalSize);
            }
            else
            {
                throw new NotSupportedException();
            }
        }

        /// <summary>
        /// Invalidates the measurement state (layout) for all ILayoutable containers that reference
        /// this layout.
        /// </summary>
        protected void InvalidateMeasure() => MeasureInvalidated?.Invoke(this, EventArgs.Empty);

        /// <summary>
        /// Invalidates the arrange state (layout) for all UIElement containers that reference this
        /// layout. After the invalidation, the UIElement will have its layout updated, which
        /// occurs asynchronously.
        /// </summary>
        protected void InvalidateArrange() => ArrangeInvalidated?.Invoke(this, EventArgs.Empty);

        private VirtualizingLayoutContext GetVirtualizingLayoutContext(LayoutContext context)
        {
            if (context is VirtualizingLayoutContext virtualizingContext)
            {
                return virtualizingContext;
            }
            else if (context is NonVirtualizingLayoutContext nonVirtualizingContext)
            {
                return nonVirtualizingContext.GetVirtualizingContextAdapter();
            }
            else
            {
                throw new NotSupportedException();
            }
        }

        private NonVirtualizingLayoutContext GetNonVirtualizingLayoutContext(LayoutContext context)
        {
            if (context is NonVirtualizingLayoutContext nonVirtualizingContext)
            {
                return nonVirtualizingContext;
            }
            else if (context is VirtualizingLayoutContext virtualizingContext)
            {
                return virtualizingContext.GetNonVirtualizingContextAdapter();
            }
            else
            {
                throw new NotSupportedException();
            }
        }
    }
}
