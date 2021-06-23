using System;
using System.Collections.Generic;
using System.Text;

namespace Avalonia.Animation
{
    public interface IClock : IObservable<TimeSpan>
    {
        PlayState PlayState { get; set; }
    }
}
