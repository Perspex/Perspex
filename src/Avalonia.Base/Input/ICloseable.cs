using System;

namespace Avalonia.Input
{
    public interface ICloseable
    {
        event EventHandler Closed;
    }
}
