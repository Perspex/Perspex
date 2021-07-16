using System;
using System.ComponentModel;
using Avalonia.Native.Interop;
using Avalonia.Platform;

namespace Avalonia.Native
{
    internal class AvaloniaNativeApplicationPlatform : CallbackBase, IAvnApplicationEvents, IPlatformLifetimeEventsImpl
    {
        public event EventHandler<CancelEventArgs> ShutdownRequested;
        
        void IAvnApplicationEvents.FilesOpened(IAvnStringArray urls)
        {
            ((IApplicationPlatformEvents)Application.Current).RaiseUrlsOpened(urls.ToStringArray());
        }

        public int TryShutdown()
        {
            if (ShutdownRequested is null) return 1;
            var e = new CancelEventArgs();
            ShutdownRequested(this, e);
            return (!e.Cancel).AsComBool();
        }
    }
}
