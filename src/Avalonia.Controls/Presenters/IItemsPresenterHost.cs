using Avalonia.LogicalTree;
using Avalonia.Styling;

#nullable enable

namespace Avalonia.Controls.Presenters
{
    /// <summary>
    /// Represents a control which hosts an items presenter.
    /// </summary>
    /// <remarks>
    /// This interface is implemented by <see cref="ItemsControl"/> which usually contains an
    /// <see cref="ItemsPresenter"/> and exposes it through its 
    /// <see cref="ItemsControl.Presenter"/> property. ItemsPresenters can be within
    /// nested templates or in popups and so are not necessarily created immediately when the
    /// parent control's template is instantiated so they register themselves using this 
    /// interface.
    /// </remarks>
    public interface IItemsPresenterHost : ITemplatedControl, ILogical
    {
        /// <summary>
        /// Gets the element factory for the items presenter.
        /// </summary>
        IElementFactory ElementFactory { get; }

        /// <summary>
        /// Registers an <see cref="IItemsPresenter"/> with a host control.
        /// </summary>
        /// <param name="presenter">The items presenter.</param>
        void RegisterItemsPresenter(IItemsPresenter presenter);
    }
}
