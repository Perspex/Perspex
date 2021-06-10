#nullable enable

namespace Avalonia.Styling
{
    /// <summary>
    /// Customizes the behavior of a class when added as a value to an <see cref="ISetter"/>.
    /// </summary>
    public interface ISetterValue
    {
        /// <summary>
        /// Notifies that the object has been added as a setter value.
        /// </summary>
        void Initialize(ISetter setter);
    }
}
