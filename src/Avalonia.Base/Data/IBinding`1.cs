using System;

#nullable enable

namespace Avalonia.Data
{
    /// <summary>
    /// Holds a typed binding that can be applied to a property on an object.
    /// </summary>
    public interface IBinding<T> : IBinding
    {
        /// <summary>
        /// Applies the binding to a styled property on a target object.
        /// </summary>
        /// <param name="target">The target object.</param>
        /// <param name="property">The target property.</param>
        /// <returns>A disposable which will cancel the binding.</returns>
        IDisposable Bind(
            IAvaloniaObject target,
            StyledPropertyBase<T> property);

        /// <summary>
        /// Applies the binding to a direct property on a target object.
        /// </summary>
        /// <param name="target">The target object.</param>
        /// <param name="property">The target property.</param>
        /// <returns>A disposable which will cancel the binding.</returns>
        IDisposable Bind(
            IAvaloniaObject target,
            DirectPropertyBase<T> property);
    }
}
