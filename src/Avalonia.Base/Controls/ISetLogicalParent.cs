using Avalonia.LogicalTree;

#nullable enable

namespace Avalonia.Controls
{
    /// <summary>
    /// Defines an interface through which a <see cref="StyledElement"/>'s logical parent can be set.
    /// </summary>
    /// <remarks>
    /// You should not usually need to use this interface - it is for advanced scenarios only.
    /// </remarks>
    public interface ISetLogicalParent
    {
        /// <summary>
        /// Sets the control's parent.
        /// </summary>
        /// <param name="parent">The parent.</param>
        void SetParent(ILogical? parent);
    }
}
