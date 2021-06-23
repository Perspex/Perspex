using System;

namespace Avalonia.Rendering
{
    public interface IDeferredRendererLock
    {
        IDisposable TryLock();
    }
}
