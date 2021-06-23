using Avalonia.Input;

#nullable enable

namespace Avalonia.Platform
{
    public interface ICursorFactory
    {
        ICursorImpl GetCursor(StandardCursorType cursorType);
        ICursorImpl CreateCursor(IBitmapImpl cursor, PixelPoint hotSpot);
    }
}
