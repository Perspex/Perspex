using System;

namespace Avalonia.Platform
{
    public interface IPlatformSettings
    {
        Size DoubleClickSize { get; }

        TimeSpan DoubleClickTime { get; }
    }
}
