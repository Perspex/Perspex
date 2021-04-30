using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Remote.Protocol;
using Avalonia.Remote.Protocol.Viewport;
using InputProtocol = Avalonia.Remote.Protocol.Input;

namespace Avalonia.DesignerSupport.Remote.HtmlTransport
{
    public class HtmlWebSocketTransport : IAvaloniaRemoteTransportConnection
    {
        private readonly IAvaloniaRemoteTransportConnection _signalTransport;
        private readonly SimpleWebSocketHttpServer _simpleServer;
        private readonly Dictionary<string, byte[]> _resources;
        private SimpleWebSocket _pendingSocket;
        private bool _disposed;
        private object _lock = new object();
        private AutoResetEvent _wakeup = new AutoResetEvent(false);
        private FrameMessage _lastFrameMessage = null;
        private FrameMessage _lastSentFrameMessage = null;
        private Action<IAvaloniaRemoteTransportConnection, object> _onMessage;
        private Action<IAvaloniaRemoteTransportConnection, Exception> _onException;
        
        private static readonly Dictionary<string, string> Mime = new Dictionary<string, string>
        {
            ["html"] = "text/html", ["htm"] = "text/html", ["js"] = "text/javascript", ["css"] = "text/css"
        };

        private static readonly byte[] NotFound = Encoding.UTF8.GetBytes("404 - Not Found");
        

        public HtmlWebSocketTransport(IAvaloniaRemoteTransportConnection signalTransport, Uri listenUri)
        {
            if (listenUri.Scheme != "http")
                throw new ArgumentException("URI scheme is not HTTP.", nameof(listenUri));

            var resourcePrefix = "Avalonia.DesignerSupport.Remote.HtmlTransport.webapp.build.";
            _resources = typeof(HtmlWebSocketTransport).Assembly.GetManifestResourceNames()
                .Where(r => r.StartsWith(resourcePrefix, StringComparison.OrdinalIgnoreCase)
                         && r.EndsWith(".gz", StringComparison.OrdinalIgnoreCase))
                .ToDictionary(
                    r => r.Substring(resourcePrefix.Length).Substring(0,r.Length-resourcePrefix.Length-3),
                    r =>
                    {

                        using (var s =
                            new GZipStream(typeof(HtmlWebSocketTransport).Assembly.GetManifestResourceStream(r),
                                CompressionMode.Decompress))
                        {
                            var ms = new MemoryStream();
                            s.CopyTo(ms);
                            return ms.ToArray();
                        }
                    });
            
            _signalTransport = signalTransport;
            var address = IPAddress.Parse(listenUri.Host);
            
            _simpleServer = new SimpleWebSocketHttpServer(address, listenUri.Port);
            _simpleServer.Listen();
            Task.Run(AcceptWorker);
            Task.Run(SocketWorker);
            _signalTransport.Send(new HtmlTransportStartedMessage { Uri = "http://" + address + ":" + listenUri.Port + "/" });
        }

        async void AcceptWorker()
        {
            while (true)
            {

                using (var req = await _simpleServer.AcceptAsync())
                {

                    if (!req.IsWebsocketRequest)
                    {

                        var key = req.Path == "/" ? "index.html" : req.Path.TrimStart('/').Replace('/', '.');
                        if (_resources.TryGetValue(key, out var data))
                        {
                            var ext = Path.GetExtension(key).Substring(1);
                            string mime = null;
                            if (ext == null || !Mime.TryGetValue(ext, out mime))
                                mime = "application/octet-stream";
                            await req.RespondAsync(200, data, mime);
                        }
                        else
                        {
                            await req.RespondAsync(404, NotFound, "text/plain");
                        }
                    }
                    else
                    {
                        var socket = await req.AcceptWebSocket();
                        SocketReceiveWorker(socket);
                        lock (_lock)
                        {
                            _pendingSocket?.Dispose();
                            _pendingSocket = socket;
                        }
                    }
                }
            }
        }

        async void SocketReceiveWorker(SimpleWebSocket socket)
        {
            try
            {
                while (true)
                {
                    var msg = await socket.ReceiveMessage().ConfigureAwait(false);
                    if(msg != null && msg.IsText)
                    {
                        var message = ParseMessage(msg.AsString());
                        if (message != null)
                            _onMessage?.Invoke(this, message);
                    }
                }
            }
            catch(Exception e)
            {
                Console.Error.WriteLine(e.ToString());
            }
        }
        
        async void SocketWorker()
        {
            try
            {
                SimpleWebSocket socket = null;
                while (true)
                {
                    if (_disposed)
                    {
                        socket?.Dispose();
                        return;
                    }

                    FrameMessage sendNow = null;
                    lock (_lock)
                    {
                        if (_pendingSocket != null)
                        {
                            socket?.Dispose();
                            socket = _pendingSocket;
                            _pendingSocket = null;
                            _lastSentFrameMessage = null;
                        }

                        if (_lastFrameMessage != _lastSentFrameMessage)
                            _lastSentFrameMessage = sendNow = _lastFrameMessage;
                    }

                    if (sendNow != null && socket != null)
                    {
                        await socket.SendMessage(
                            $"frame:{sendNow.SequenceId}:{sendNow.Width}:{sendNow.Height}:{sendNow.Stride}:{sendNow.DpiX}:{sendNow.DpiY}");
                        await socket.SendMessage(false, sendNow.Data);
                    }

                    _wakeup.WaitOne(TimeSpan.FromSeconds(1));
                }
            }
            catch(Exception e)
            {
                Console.Error.WriteLine(e.ToString());
            }
        }
        
        public void Dispose()
        {
            _disposed = true;
            _pendingSocket?.Dispose();
            _simpleServer.Dispose();
        }
        
        public Task Send(object data)
        {
            if (data is FrameMessage frame)
            {
                _lastFrameMessage = frame;
                _wakeup.Set();
                return Task.CompletedTask;
            }
            if (data is RequestViewportResizeMessage req)
            {
                return Task.CompletedTask;
            }
            return _signalTransport.Send(data);
        }

        public void Start()
        {
            _onMessage?.Invoke(this, new Avalonia.Remote.Protocol.Viewport.ClientSupportedPixelFormatsMessage
            {
                Formats = new []{PixelFormat.Rgba8888}
            });
            _signalTransport.Start();
        }
        
        #region Forward
        public event Action<IAvaloniaRemoteTransportConnection, object> OnMessage
        {
            add
            {
                bool subscribeToInner;
                lock (_lock)
                {
                     subscribeToInner = _onMessage == null;
                    _onMessage += value;
                }

                if (subscribeToInner)
                    _signalTransport.OnMessage += OnSignalTransportMessage;
            }
            remove
            {
                lock (_lock)
                {
                    _onMessage -= value;
                    if (_onMessage == null)
                        _signalTransport.OnMessage -= OnSignalTransportMessage;
                }
            }
        }
        
        private void OnSignalTransportMessage(IAvaloniaRemoteTransportConnection signal, object message) => _onMessage?.Invoke(this, message);

        public event Action<IAvaloniaRemoteTransportConnection, Exception> OnException
        {
            add
            {
                lock (_lock)
                {
                    var subscribeToInner = _onException == null;
                    _onException += value;
                    if (subscribeToInner)
                        _signalTransport.OnException += OnSignalTransportException;
                }
            }
            remove
            {
                lock (_lock)
                {
                    _onException -= value;
                    if(_onException==null)
                        _signalTransport.OnException -= OnSignalTransportException;
                }
                
            }
        }

        private void OnSignalTransportException(IAvaloniaRemoteTransportConnection arg1, Exception ex)
        {
            _onException?.Invoke(this, ex);
        }
        #endregion

        private static object ParseMessage(string message)
        {
            var parts = message.Split(':');
            var key = parts[0];
            if (key.Equals("frame-received", StringComparison.OrdinalIgnoreCase))
            {
                return new FrameReceivedMessage { SequenceId = long.Parse(parts[1]) };
            }
            else if (key.Equals("pointer-released", StringComparison.OrdinalIgnoreCase))
            {
                return new InputProtocol.PointerReleasedEventMessage
                {
                    Modifiers = ParseInputModifiers(parts[1]),
                    X = ParseDouble(parts[2]),
                    Y = ParseDouble(parts[3]),
                    Button = ParseMouseButton(parts[4]),
                };
            }
            else if (key.Equals("pointer-pressed", StringComparison.OrdinalIgnoreCase))
            {
                return new InputProtocol.PointerPressedEventMessage
                {
                    Modifiers = ParseInputModifiers(parts[1]),
                    X = ParseDouble(parts[2]),
                    Y = ParseDouble(parts[3]),
                    Button = ParseMouseButton(parts[4]),
                };
            }
            else if (key.Equals("pointer-moved", StringComparison.OrdinalIgnoreCase))
            {
                return new InputProtocol.PointerMovedEventMessage
                {
                    Modifiers = ParseInputModifiers(parts[1]),
                    X = ParseDouble(parts[2]),
                    Y = ParseDouble(parts[3]),
                };
            }
            else if (key.Equals("scroll", StringComparison.OrdinalIgnoreCase))
            {
                return new InputProtocol.ScrollEventMessage
                {
                    Modifiers = ParseInputModifiers(parts[1]),
                    X = ParseDouble(parts[2]),
                    Y = ParseDouble(parts[3]),
                    DeltaX = ParseDouble(parts[4]),
                    DeltaY = ParseDouble(parts[5]),
                };
            }
            
            return null;
        }

        private static InputProtocol.InputModifiers[] ParseInputModifiers(string modifiersText) =>
            string.IsNullOrWhiteSpace(modifiersText)
            ? null
            : modifiersText
                .Split(',')
                .Select(x => (InputProtocol.InputModifiers)Enum.Parse(
                    typeof(InputProtocol.InputModifiers), x, true))
                .ToArray();

        private static InputProtocol.MouseButton ParseMouseButton(string buttonText) =>
            string.IsNullOrWhiteSpace(buttonText)
            ? InputProtocol.MouseButton.None
            : (InputProtocol.MouseButton)Enum.Parse(
                typeof(InputProtocol.MouseButton), buttonText, true);

        private static double ParseDouble(string text) =>
            double.Parse(text, NumberStyles.Float, CultureInfo.InvariantCulture);
    }
}
