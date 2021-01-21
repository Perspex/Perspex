using System;
using System.Collections;
using System.Collections.Generic;

#nullable enable

namespace Avalonia.Controls.Presenters
{
    public interface IItemsPresenter : IPresenter
    {
        /// <summary>
        /// Gets the currently realized elements.
        /// </summary>
        IEnumerable<IControl> RealizedElements { get; }

        /// <summary>
        /// Gets or sets the items view containing the items to display in the presenter.
        /// </summary>
        ItemsSourceView? ItemsView { get; set; }

        /// <summary>
        /// Retrieves the index of the item from the data source that corresponds to the specified
        /// <see cref="IControl"/>.
        /// </summary>
        /// <param name="element">
        /// The element that corresponds to the item to get the index of.
        /// </param>
        /// <returns>
        /// The index of the item from the data source that corresponds to the specified UIElement,
        /// or -1 if the element is not supported.
        /// </returns>
        int GetElementIndex(IControl element);

        /// <summary>
        /// Retrieves the realized <see cref="IControl"/> that corresponds to the item at the
        /// specified index in the data source.
        /// </summary>
        /// <param name="index">The index of the item.</param>
        /// <returns>
        /// The <see cref="IControl"/> that corresponds to the item at the specified index if the
        /// item is realized, or null if the item is not realized.
        /// </returns>
        IControl? TryGetElement(int index);

        /// <summary>
        /// Scrolls the specified item into view.
        /// </summary>
        /// <param name="index">The index of the item.</param>
        /// <returns>
        /// True if the control was scrolled into view; otherwise false.
        /// </returns>
        bool ScrollIntoView(int index);

        /// <summary>
        /// Occurs each time an element is prepared for use.
        /// </summary>
        /// <remarks>
        /// The prepared element might be newly created or an existing element that is being re-
        /// used.
        /// </remarks>
        event EventHandler<ElementPreparedEventArgs>? ElementPrepared;

        /// <summary>
        /// Occurs each time an element is cleared and made available to be re-used.
        /// </summary>
        /// <remarks>
        /// This event is raised immediately each time an element is cleared, such as when it falls
        /// outside the range of realized items. Elements are cleared when they become available
        /// for re-use.
        /// </remarks>
        event EventHandler<ElementClearingEventArgs>? ElementClearing;

        /// <summary>
        /// Occurs for each realized element when the index for the item it represents has changed.
        /// </summary>
        /// <remarks>
        /// This event is raised for each realized element where the index for the item it
        /// represents has changed. For example, when another item is added or removed in the data
        /// source, the index for items that come after in the ordering will be impacted.
        /// </remarks>
        event EventHandler<ElementIndexChangedEventArgs>? ElementIndexChanged;
    }
}
