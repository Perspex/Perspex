using System;
using System.Collections.Generic;

#nullable enable

namespace Avalonia.Styling
{
    /// <summary>
    /// Defines the style host that provides styles global to the application.
    /// </summary>
    public interface IGlobalStyles : IStyleHost
    {
        /// <summary>
        /// Raised when styles are added to <see cref="Styles"/> or a nested styles collection.
        /// </summary>
        public event Action<IReadOnlyList<IStyle>> GlobalStylesAdded;

        /// <summary>
        /// Raised when styles are removed from <see cref="Styles"/> or a nested styles collection.
        /// </summary>
        public event Action<IReadOnlyList<IStyle>> GlobalStylesRemoved;
    }
}
