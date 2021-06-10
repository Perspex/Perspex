#nullable enable

namespace Avalonia
{
    /// <summary>
    /// Defines an element with a data context that can be used for binding.
    /// </summary>
    public interface IDataContextProvider : IAvaloniaObject
    {
        /// <summary>
        /// Gets or sets the element's data context.
        /// </summary>
        object? DataContext { get; set; }
    }
}
