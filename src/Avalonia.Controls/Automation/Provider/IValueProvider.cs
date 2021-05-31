#nullable enable

namespace Avalonia.Automation.Provider
{
    /// <summary>
    /// Exposes methods and properties to support access by a UI Automation client to controls
    /// that have an intrinsic value that does not span a range and that can be represented as
    /// a string.
    /// </summary>
    public interface IValueProvider
    {
        /// <summary>
        /// Gets a value that indicates whether the value of a control is read-only.
        /// </summary>
        bool IsReadOnly { get; }

        /// <summary>
        /// Gets the value of the control.
        /// </summary>
        public string? Value { get; }

        /// <summary>
        /// Sets the value of a control.
        /// </summary>
        /// <param name="value">
        /// The value to set. The provider is responsible for converting the value to the
        /// appropriate data type.
        /// </param>
        public void SetValue(string? value);
    }
}
