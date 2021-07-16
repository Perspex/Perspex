using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Interactivity;
using Avalonia.Platform;
using Avalonia.Threading;

namespace Avalonia.Controls.ApplicationLifetimes
{
    public class ClassicDesktopStyleApplicationLifetime : IClassicDesktopStyleApplicationLifetime, IDisposable
    {
        private int _exitCode;
        private CancellationTokenSource _cts;
        private bool _isShuttingDown;
        private HashSet<Window> _windows = new HashSet<Window>();

        private static ClassicDesktopStyleApplicationLifetime _activeLifetime;
        static ClassicDesktopStyleApplicationLifetime()
        {
            Window.WindowOpenedEvent.AddClassHandler(typeof(Window), OnWindowOpened);
            Window.WindowClosedEvent.AddClassHandler(typeof(Window), WindowClosedEvent);
        }

        private static void WindowClosedEvent(object sender, RoutedEventArgs e)
        {
            _activeLifetime?._windows.Remove((Window)sender);
            _activeLifetime?.HandleWindowClosed((Window)sender);
        }

        private static void OnWindowOpened(object sender, RoutedEventArgs e)
        {
            _activeLifetime?._windows.Add((Window)sender);
        }

        public ClassicDesktopStyleApplicationLifetime()
        {
            if (_activeLifetime != null)
                throw new InvalidOperationException(
                    "Can not have multiple active ClassicDesktopStyleApplicationLifetime instances and the previously created one was not disposed");
            _activeLifetime = this;
        }

        /// <inheritdoc/>
        public event EventHandler<ControlledApplicationLifetimeStartupEventArgs> Startup;

        /// <inheritdoc/>
        public event EventHandler<CancelEventArgs> ShutdownRequested;

        /// <inheritdoc/>
        public event EventHandler<ControlledApplicationLifetimeExitEventArgs> Exit;

        /// <summary>
        /// Gets the arguments passed to the AppBuilder Start method.
        /// </summary>
        public string[] Args { get; set; }
        
        /// <inheritdoc/>
        public ShutdownMode ShutdownMode { get; set; }
        
        /// <inheritdoc/>
        public Window MainWindow { get; set; }

        public IReadOnlyList<Window> Windows => _windows.ToList();

        private void HandleWindowClosed(Window window)
        {
            if (window == null)
                return;
            
            if (_isShuttingDown)
                return;

            if (ShutdownMode == ShutdownMode.OnLastWindowClose && _windows.Count == 0)
                Shutdown();
            else if (ShutdownMode == ShutdownMode.OnMainWindowClose && window == MainWindow)
                Shutdown();
        }

        public void Shutdown(int exitCode = 0)
        {
            if (_isShuttingDown)
                throw new InvalidOperationException("Application is already shutting down.");
            
            _exitCode = exitCode;
            _isShuttingDown = true;

            try
            {
                foreach (var w in Windows)
                    w.Close();
                var e = new ControlledApplicationLifetimeExitEventArgs(exitCode);
                Exit?.Invoke(this, e);
                _exitCode = e.ApplicationExitCode;                
            }
            finally
            {
                _cts?.Cancel();
                _cts = null;
                _isShuttingDown = false;
            }
        }
        
        
        public int Start(string[] args)
        {
            Startup?.Invoke(this, new ControlledApplicationLifetimeStartupEventArgs(args));

            var options = AvaloniaLocator.Current.GetService<ClassicDesktopStyleApplicationLifetimeOptions>();
            
            if(options != null && options.ProcessUrlActivationCommandLine && args.Length > 0)
            {
                ((IApplicationPlatformEvents)Application.Current).RaiseUrlsOpened(args);
            }

            var lifetimeEvents = AvaloniaLocator.Current.GetService<IPlatformLifetimeEventsImpl>(); 

            if (lifetimeEvents != null)
                lifetimeEvents.ShutdownRequested += OnShutdownRequested;

            _cts = new CancellationTokenSource();
            MainWindow?.Show();
            Dispatcher.UIThread.MainLoop(_cts.Token);
            Environment.ExitCode = _exitCode;
            return _exitCode;
        }

        public void Dispose()
        {
            if (_activeLifetime == this)
                _activeLifetime = null;
        }
        
        private void OnShutdownRequested(object sender, CancelEventArgs e)
        {
            ShutdownRequested?.Invoke(this, e);

            if (e.Cancel)
                return;

            // When an OS shutdown request is received, try to close all non-owned windows. Windows can cancel
            // shutdown by setting e.Cancel = true in the Closing event. Owned windows will be shutdown by their
            // owners.
            foreach (var w in Windows)
                if (w.Owner is null)
                    w.Close();
            if (Windows.Count > 0)
                e.Cancel = true;
        }
    }
    
    public class ClassicDesktopStyleApplicationLifetimeOptions
    {
        public bool ProcessUrlActivationCommandLine { get; set; }
    }
}

namespace Avalonia
{
    public static class ClassicDesktopStyleApplicationLifetimeExtensions
    {
        public static int StartWithClassicDesktopLifetime<T>(
            this T builder, string[] args, ShutdownMode shutdownMode = ShutdownMode.OnLastWindowClose)
            where T : AppBuilderBase<T>, new()
        {
            var lifetime = new ClassicDesktopStyleApplicationLifetime()
            {
                Args = args,
                ShutdownMode = shutdownMode
            };
            builder.SetupWithLifetime(lifetime);
            return lifetime.Start(args);
        }
    }
}
