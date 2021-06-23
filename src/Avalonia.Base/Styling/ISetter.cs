using System;

#nullable enable

namespace Avalonia.Styling
{
    /// <summary>
    /// Represents a setter for a <see cref="Style"/>.
    /// </summary>
    public interface ISetter
    {
        /// <summary>
        /// Instances a setter on a control.
        /// </summary>
        /// <param name="target">The control.</param>
        /// <returns>An <see cref="ISetterInstance"/>.</returns>
        /// <remarks>
        /// This method should return an <see cref="ISetterInstance"/> which can be used to apply
        /// the setter to the specified control. Note that it should not apply the setter value 
        /// until <see cref="ISetterInstance.Start(bool)"/> is called.
        /// </remarks>
        ISetterInstance Instance(IStyleable target);
    }
}
