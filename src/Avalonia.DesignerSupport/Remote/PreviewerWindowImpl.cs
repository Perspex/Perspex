using System;
using System.Reactive.Disposables;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Platform;
using Avalonia.Controls;
using Avalonia.Controls.Remote.Server;
using Avalonia.Input;
using Avalonia.Platform;
using Avalonia.Remote.Protocol;
using Avalonia.Remote.Protocol.Viewport;
using Avalonia.Threading;

namespace Avalonia.DesignerSupport.Remote
{
    class PreviewerWindowImpl : RemoteServerTopLevelImpl, IWindowImpl
    {
        private readonly IAvaloniaRemoteTransportConnection _transport;

        public PreviewerWindowImpl(IAvaloniaRemoteTransportConnection transport) : base(transport)
        {
            _transport = transport;
            ClientSize = new Size(1, 1);
        }

        public void Show(bool activate)
        {
        }

        public void Hide()
        {
        }

        public void BeginMoveDrag(PointerPressedEventArgs e)
        {
        }

        public void BeginResizeDrag(WindowEdge edge, PointerPressedEventArgs e)
        {
        }

        public double DesktopScaling => 1.0;
        public PixelPoint Position { get; set; }
        public Action<PixelPoint> PositionChanged { get; set; }
        public Action Deactivated { get; set; }
        public Action Activated { get; set; }
        public Func<bool> Closing { get; set; }
        public IPlatformHandle Handle { get; }
        public WindowState WindowState { get; set; }
        public Action<WindowState> WindowStateChanged { get; set; }
        public Func<IAutomationNode, AutomationPeer> AutomationStarted { get; set; }
        public Size MaxAutoSizeHint { get; } = new Size(4096, 4096);

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
        public Action GotInputWhenDisabled { get; set; }        
        
        public Action<bool> ExtendClientAreaToDecorationsChanged { get; set; }

        public Thickness ExtendedMargins { get; } = new Thickness();

        public bool IsClientAreaExtendedToDecorations { get; }

        public Thickness OffScreenMargin { get; } = new Thickness();

        public bool NeedsManagedDecorations => false;

        public void Activate()
        {
        }
        
        public void SetTitle(string title)
        {
        }

        public void ShowDialog(IWindowImpl parent)
        {
        }

        public void SetSystemDecorations(SystemDecorations enabled)
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

        public void SetParent(IWindowImpl parent)
        {
        }

        public void SetEnabled(bool enable)
        {
        }

        public void SetExtendClientAreaToDecorationsHint(bool extendIntoClientAreaHint)
        {            
        }

        public void SetExtendClientAreaChromeHints(ExtendClientAreaChromeHints hints)
        {            
        }

        public void SetExtendClientAreaTitleBarHeightHint(double titleBarHeight)
        {            
        }
    }
}
