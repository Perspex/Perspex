using System;
using System.Collections.Generic;
using System.IO;
using System.Reactive.Disposables;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Controls.Platform;
using Avalonia.Controls.Primitives.PopupPositioning;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Input.Raw;
using Avalonia.Platform;
using Avalonia.Rendering;

namespace Avalonia.DesignerSupport.Remote
{
    class WindowStub : IWindowImpl, IPopupImpl
    {
        public Action Deactivated { get; set; }
        public Action Activated { get; set; }
        public IPlatformHandle Handle { get; }
        public Size MaxClientSize { get; }
        public Size ClientSize { get; }
        public double Scaling { get; } = 1.0;
        public IEnumerable<object> Surfaces { get; }
        public Action<RawInputEventArgs> Input { get; set; }
        public Action<Rect> Paint { get; set; }
        public Action<Size> Resized { get; set; }
        public Action<double> ScalingChanged { get; set; }
        public Func<bool> Closing { get; set; }
        public Action Closed { get; set; }
        public IMouseDevice MouseDevice { get; } = new MouseDevice();
        public IPopupImpl CreatePopup() => new WindowStub(this);

        public PixelPoint Position { get; set; }
        public Action<PixelPoint> PositionChanged { get; set; }
        public WindowState WindowState { get; set; }
        public Action<WindowState> WindowStateChanged { get; set; }
        public Func<Point, WindowRegion> ClassifyWindowRegion { get; set; }

        public WindowStub(IWindowImpl parent = null)
        {
            if (parent != null)
                PopupPositioner = new ManagedPopupPositioner(new ManagedPopupPositionerPopupImplHelper(parent,
                    (_, size, __) =>
                    {
                        Resize(size);
                    }));
        }

        public IRenderer CreateRenderer(IRenderRoot root) => new ImmediateRenderer(root);
        public void Dispose()
        {
        }
        public void Invalidate(Rect rect)
        {
        }

        public void SetInputRoot(IInputRoot inputRoot)
        {
        }

        public Point PointToClient(PixelPoint p) => p.ToPoint(1);

        public PixelPoint PointToScreen(Point p) => PixelPoint.FromPoint(p, 1);

        public void SetCursor(IPlatformHandle cursor)
        {
        }

        public void Show()
        {
        }

        public void Hide()
        {
        }

        public void Activate()
        {
        }

        public void Resize(Size clientSize)
        {
        }

        public void Move(PixelPoint point)
        {

        }

        public IScreenImpl Screen { get; } = new ScreenStub();

        public void SetMinMaxSize(Size minSize, Size maxSize)
        {
        }

        public void SetTitle(string title)
        {
        }

        public void ShowDialog(IWindowImpl parent)
        {
        }

        public void SetSystemDecorations(bool enabled)
        {
        }

        public void SetIcon(IWindowIconImpl icon)
        {
        }

        public void ShowTaskbarIcon(bool value)
        {
        }

        public void CanResize(bool value)
        {
        }

        public void SetTopmost(bool value)
        {
        }

        public IPopupPositioner PopupPositioner { get; }
    }

    class ClipboardStub : IClipboard
    {
        public Task<string> GetTextAsync() => Task.FromResult("");

        public Task SetTextAsync(string text) => Task.CompletedTask;

        public Task ClearAsync() => Task.CompletedTask;
    }

    class CursorFactoryStub : IStandardCursorFactory
    {
        public IPlatformHandle GetCursor(StandardCursorType cursorType) => new PlatformHandle(IntPtr.Zero, "STUB");
    }

    class IconLoaderStub : IPlatformIconLoader
    {
        class IconStub : IWindowIconImpl
        {
            public void Save(Stream outputStream)
            {

            }
        }

        public IWindowIconImpl LoadIcon(string fileName) => new IconStub();

        public IWindowIconImpl LoadIcon(Stream stream) => new IconStub();

        public IWindowIconImpl LoadIcon(IBitmapImpl bitmap) => new IconStub();
    }

    class SystemDialogsStub : ISystemDialogImpl
    {
        public Task<string[]> ShowFileDialogAsync(FileDialog dialog, IWindowImpl parent) =>
            Task.FromResult((string[])null);

        public Task<string> ShowFolderDialogAsync(OpenFolderDialog dialog, IWindowImpl parent) =>
            Task.FromResult((string)null);
    }

    class ScreenStub : IScreenImpl
    {
        public int ScreenCount => 1;

        public IReadOnlyList<Screen> AllScreens { get; } =
            new Screen[] { new Screen(1, new PixelRect(0, 0, 4000, 4000), new PixelRect(0, 0, 4000, 4000), true) };
    }
}
