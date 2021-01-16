using Avalonia.Input;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Utilities;

namespace Avalonia.Headless
{
    public interface IHeadlessWindow
    {
        IRef<IWriteableBitmapImpl> GetLastRenderedFrame();
        void KeyPress(Key key, string mappedKey, RawInputModifiers modifiers);
        void KeyRelease(Key key, string mappedKey, RawInputModifiers modifiers);
        void MouseDown(Point point, int button, RawInputModifiers modifiers = RawInputModifiers.None);
        void MouseMove(Point point, RawInputModifiers modifiers = RawInputModifiers.None);
        void MouseUp(Point point, int button, RawInputModifiers modifiers = RawInputModifiers.None);
    }
}
