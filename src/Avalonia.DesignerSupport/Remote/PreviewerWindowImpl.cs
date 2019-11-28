using System;
using System.Reactive.Disposables;
using Avalonia.Controls;
using Avalonia.Controls.Remote.Server;
using Avalonia.Input;
using Avalonia.Platform;
using Avalonia.Remote.Protocol;
using Avalonia.Remote.Protocol.Viewport;
using Avalonia.Threading;

namespace Avalonia.DesignerSupport.Remote
{
    class PreviewerWindowImpl : RemoteServerTopLevelImpl, IWindowImpl, IEmbeddableWindowImpl
    {
        private readonly IAvaloniaRemoteTransportConnection _transport;

        public PreviewerWindowImpl(IAvaloniaRemoteTransportConnection transport) : base(transport)
        {
            _transport = transport;
            ClientSize = new Size(1, 1);
        }

        public void Show()
        {
        }

        public void Hide()
        {
        }

        public PixelPoint Position { get; set; }
        public Action<PixelPoint> PositionChanged { get; set; }
        public Action Deactivated { get; set; }
        public Action Activated { get; set; }
        public Func<bool> Closing { get; set; }
        public IPlatformHandle Handle { get; }
        public WindowState WindowState { get; set; }
        public Action<WindowState> WindowStateChanged { get; set; }
        public Func<Point, WindowRegion> ClassifyWindowRegion { get; set; }
        public Size MaxClientSize { get; } = new Size(4096, 4096);
        public event Action LostFocus
        {
            add {}
            remove {}
        }

        protected override void OnMessage(IAvaloniaRemoteTransportConnection transport, object obj)
        {
            // In previewer mode we completely ignore client-side viewport size
            if (obj is ClientViewportAllocatedMessage alloc)
            {
                Dispatcher.UIThread.Post(() => SetDpi(new Vector(alloc.DpiX, alloc.DpiY)));
                return;
            }
            base.OnMessage(transport, obj);
        }
        
        public void Resize(Size clientSize)
        {
            _transport.Send(new RequestViewportResizeMessage
            {
                Width = clientSize.Width,
                Height = clientSize.Height
            });
            ClientSize = clientSize;
            RenderIfNeeded();
        }

        public void Move(PixelPoint point)
        {
            
        }

        public void SetMinMaxSize(Size minSize, Size maxSize)
        {
        }

        public IScreenImpl Screen { get; } = new ScreenStub();

        public void Activate()
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
    }
}
