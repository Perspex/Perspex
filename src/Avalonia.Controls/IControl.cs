using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.VisualTree;

#nullable enable

namespace Avalonia.Controls
{
    /// <summary>
    /// Interface for Avalonia controls.
    /// </summary>
    public interface IControl : IVisual,
        IDataTemplateHost,
        ILayoutable,
        IInputElement,
        INamed,
        IStyledElement
    {
        new IControl? Parent { get; }
    }
}
