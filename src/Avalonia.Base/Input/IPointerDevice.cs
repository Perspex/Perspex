using System;
using Avalonia.VisualTree;

namespace Avalonia.Input
{
    public interface IPointerDevice : IInputDevice
    {
        [Obsolete("Use IPointer")]
        IInputElement? Captured { get; }
        
        [Obsolete("Use IPointer")]
        void Capture(IInputElement? control);

        [Obsolete("Use PointerEventArgs.GetPosition")]
        Point GetPosition(IVisual relativeTo);
    }
}
