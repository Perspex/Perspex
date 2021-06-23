using System;

namespace Avalonia.Input
{
    [Flags]
    public enum DragDropEffects
    {
        None = 0,
        Copy = 1,
        Move = 2,
        Link = 4,
    }
}