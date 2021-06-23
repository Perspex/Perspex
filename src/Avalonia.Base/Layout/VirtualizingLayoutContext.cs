// This source file is adapted from the WinUI project.
// (https://github.com/microsoft/microsoft-ui-xaml)
//
// Licensed to The Avalonia Project under MIT License, courtesy of The .NET Foundation.

using System;

namespace Avalonia.Layout
{
    /// <summary>
    /// Defines constants that specify whether to suppress automatic recycling of the retrieved
    /// element or force creation of a new element.
    /// </summary>
    /// <remarks>
    /// When you call <see cref="VirtualizingLayoutContext.GetOrCreateElementAt(int, ElementRealizationOptions)"/>,
    /// you can specify whether to suppress automatic recycling of the retrieved element or force
    /// creation of a new element. Elements retrieved with automatic recycling suppressed
    /// (SuppressAutoRecycle) are ignored by the automatic recycling logic that clears realized
    /// elements that were not retrieved as part of the current layout pass. You must explicitly
    /// recycle these elements by passing them to the RecycleElement method to avoid memory leaks.
    /// </remarks>
    [Flags]
    public enum ElementRealizationOptions
    {
        /// <summary>
        /// No option is specified.
        /// </summary>
        None = 0x0,

        /// <summary>
        /// Creation of a new element is forced.
        /// </summary>
        ForceCreate = 0x1,

        /// <summary>
        /// The element is ignored by the automatic recycling logic.
        /// </summary>
        SuppressAutoRecycle = 0x2,
    };

    /// <summary>
    /// Represents the base class for layout context types that support virtualization.
    /// </summary>
    public abstract class VirtualizingLayoutContext : LayoutContext
    {
        private NonVirtualizingLayoutContext _contextAdapter;

        /// <summary>
        /// Gets the number of items in the data.
        /// </summary>
        /// <remarks>
        /// This property gets the value returned by ItemCountCore, which must be implemented in
        /// a derived class.
        /// </remarks>
        public int ItemCount => ItemCountCore();

        /// <summary>
        /// Gets or sets the origin point for the estimated content size.
        /// </summary>
        /// <remarks>
        /// LayoutOrigin is used by virtualizing layouts that rely on estimations when determining
        /// the size and position of content. It allows the layout to fix-up the estimated origin
        /// of the content as it changes due to on-going estimation or potentially identifying the
        /// actual size to use. For example, it’s possible that as a user is scrolling back to the
        /// top of the content that the layout's estimates for the content size that it reports as
        /// part of its MeasureOverride become increasingly accurate. If the predicted position of
        /// the content does not already match the previously predicted position (for example, if
        /// the size of the elements ends up being smaller than previously thought), then the
        /// layout can indicate a new origin. The viewport provided to the layout on subsequent
        /// passes will take into account the adjusted origin.
        /// </remarks>
        public Point LayoutOrigin { get => LayoutOriginCore; set => LayoutOriginCore = value; }

        /// <summary>
        /// Gets an area that represents the viewport and buffer that the layout should fill with
        /// realized elements.
        /// </summary>
        public Rect RealizationRect => RealizationRectCore();

        /// <summary>
        /// Gets the recommended index from which to start the generation and layout of elements.
        /// </summary>
        /// <remarks>
        /// The recommended index might be the result of programmatically realizing an element and
        /// requesting that it be brought into view. Or, it may be that a user drags the scrollbar
        /// thumb so quickly that the new viewport and the viewport and buffer previously given to
        /// the layout do not intersect, so a new index is suggested as the anchor from which to
        /// generate and layout other elements.
        /// </remarks>
        public int RecommendedAnchorIndex => RecommendedAnchorIndexCore;

        /// <summary>
        /// Implements the behavior of LayoutOrigin in a derived or custom VirtualizingLayoutContext.
        /// </summary>
        protected abstract Point LayoutOriginCore { get; set; }

        /// <summary>
        /// Implements the behavior for getting the return value of RecommendedAnchorIndex in a
        /// derived or custom <see cref="VirtualizingLayoutContext"/>.
        /// </summary>
        protected virtual int RecommendedAnchorIndexCore { get; }

        /// <summary>
        /// Retrieves the data item in the source found at the specified index.
        /// </summary>
        /// <param name="index">The index of the data item to retrieve.</param>
        public object GetItemAt(int index) => GetItemAtCore(index);

        /// <summary>
        /// Retrieves a UIElement that represents the data item in the source found at the
        /// specified index. By default, if an element already exists, it is returned; otherwise,
        /// a new element is created.
        /// </summary>
        /// <param name="index">The index of the data item to retrieve a UIElement for.</param>
        /// <remarks>
        /// This method calls <see cref="GetOrCreateElementAtCore(int, ElementRealizationOptions)"/>
        /// with options set to None. GetElementAtCore must be implemented in a derived class.
        /// </remarks>
        public ILayoutable GetOrCreateElementAt(int index)
            => GetOrCreateElementAtCore(index, ElementRealizationOptions.None);

        /// <summary>
        /// Retrieves a UIElement that represents the data item in the source found at the
        /// specified index using the specified options.
        /// </summary>
        /// <param name="index">The index of the data item to retrieve a UIElement for.</param>
        /// <param name="options">
        /// A value of <see cref="ElementRealizationOptions"/> that specifies whether to suppress
        /// automatic recycling of the retrieved element or force creation of a new element.
        /// </param>
        /// <remarks>
        /// This method calls <see cref="GetOrCreateElementAtCore(int, ElementRealizationOptions)"/>,
        /// which must be implemented in a derived class. When you request an element for the
        /// specified index, you can optionally specify whether to suppress automatic recycling of
        /// the retrieved element or force creation of a new element.Elements retrieved with
        /// automatic recycling suppressed(SuppressAutoRecycle) are ignored by the automatic
        /// recycling logic that clears realized elements that were not retrieved as part of the
        /// current layout pass.You must explicitly recycle these elements by passing them to the
        /// RecycleElement method to avoid memory leaks. These options are intended for more
        /// advanced layouts that choose to explicitly manage the realization and recycling of
        /// elements as a performance optimization.
        /// </remarks>
        public ILayoutable GetOrCreateElementAt(int index, ElementRealizationOptions options)
            => GetOrCreateElementAtCore(index, options);

        /// <summary>
        /// Clears the specified UIElement and allows it to be either re-used or released.
        /// </summary>
        /// <param name="element">The element to clear.</param>
        /// <remarks>
        /// This method calls <see cref="RecycleElementCore(ILayoutable)"/>, which must be implemented
        /// in a derived class.
        /// </remarks>
        public void RecycleElement(ILayoutable element) => RecycleElementCore(element);

        /// <summary>
        /// When implemented in a derived class, retrieves the number of items in the data.
        /// </summary>
        protected abstract int ItemCountCore();

        /// <summary>
        /// When implemented in a derived class, retrieves the data item in the source found at the
        /// specified index.
        /// </summary>
        /// <param name="index">The index of the data item to retrieve.</param>
        protected abstract object GetItemAtCore(int index);

        /// <summary>
        /// When implemented in a derived class, retrieves an area that represents the viewport and
        /// buffer that the layout should fill with realized elements.
        /// </summary>
        protected abstract Rect RealizationRectCore();

        /// <summary>
        /// When implemented in a derived class, retrieves a UIElement that represents the data item
        /// in the source found at the specified index using the specified options.
        /// </summary>
        /// <param name="index">The index of the data item to retrieve a UIElement for.</param>
        /// <param name="options">
        /// A value of <see cref="ElementRealizationOptions"/> that specifies whether to suppress
        /// automatic recycling of the retrieved element or force creation of a new element.
        /// </param>
        protected abstract ILayoutable GetOrCreateElementAtCore(int index, ElementRealizationOptions options);

        /// <summary>
        /// When implemented in a derived class, clears the specified UIElement and allows it to be
        /// either re-used or released.
        /// </summary>
        /// <param name="element">The element to clear.</param>
        protected abstract void RecycleElementCore(ILayoutable element);

        internal NonVirtualizingLayoutContext GetNonVirtualizingContextAdapter() =>
            _contextAdapter ?? (_contextAdapter = new VirtualLayoutContextAdapter(this));
    }
}
