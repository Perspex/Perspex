using System;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Remote.Protocol;
using Avalonia.Remote.Protocol.Designer;
using Avalonia.Threading;

namespace Avalonia.DesignerSupport.Remote
{
    class FileWatcherTransport : IAvaloniaRemoteTransportConnection, ITransportWithEnforcedMethod
    {
        private readonly string _appPath;
        private string _path;
        private string _lastContents;
        private bool _disposed;

        public FileWatcherTransport(Uri file, string appPath)
        {
            _appPath = appPath;
            _path = file.LocalPath;
        }

        public void Dispose()
        {
            _disposed = true;
        }

        void Dump(object o, string pad)
        {
            foreach (var p in o.GetType().GetProperties())
            {
                Console.Write($"{pad}{p.Name}: ");
                var v = p.GetValue(o);
                if (v == null || v.GetType().IsPrimitive || v is string || v is Guid)
                    Console.WriteLine(v);
                else
                {
                    Console.WriteLine();
                    Dump(v, pad + "    ");
                }
            }
        }
            
        
        public Task Send(object data)
        {
            Console.WriteLine(data.GetType().Name);
            Dump(data, "    ");
            return Task.CompletedTask;
        }

        private Action<IAvaloniaRemoteTransportConnection, object> _onMessage;
        public event Action<IAvaloniaRemoteTransportConnection, object> OnMessage
        {
            add
            {
                _onMessage+=value;
            }
            remove { _onMessage -= value; }
        }

        public event Action<IAvaloniaRemoteTransportConnection, Exception> OnException { add { } remove { } }
        public void Start()
        {
            UpdaterThread();
        }

        // I couldn't get FileSystemWatcher working on Linux, so I came up with this abomination
        async void UpdaterThread()
        {
            while (!_disposed)
            {
                var data = File.ReadAllText(_path);
                if (data != _lastContents)
                {
                    Console.WriteLine("Triggering XAML update");
                    _lastContents = data;
                    _onMessage?.Invoke(this, new UpdateXamlMessage
                    {
                        Xaml = data,
                        AssemblyPath = _appPath
                    });
                }

                await Task.Delay(100);
            }
        }

        public string PreviewerMethod => RemoteDesignerEntryPoint.Methods.Html;
    }

    interface ITransportWithEnforcedMethod
    {
        string PreviewerMethod { get; }
    }
}
