using System.Collections.Generic;

#nullable enable

namespace Avalonia.Controls
{
    /// <summary>
    /// An indexed dictionary of resources.
    /// </summary>
    public interface IResourceDictionary : IResourceProvider, IDictionary<object, object?>
    {
        /// <summary>
        /// Gets a collection of child resource dictionaries.
        /// </summary>
        IList<IResourceProvider> MergedDictionaries { get; }
    }
}
