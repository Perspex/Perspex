using System;

namespace Avalonia.Media
{
    public interface IMutableTransform : ITransform
    {
        /// <summary>
        /// Raised when the transform changes.
        /// </summary>
        event EventHandler Changed;
    }
}
