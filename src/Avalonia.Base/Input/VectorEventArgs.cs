using System;
using Avalonia.Interactivity;

namespace Avalonia.Input
{
    public class VectorEventArgs : RoutedEventArgs
    {
        public Vector Vector { get; set; }
    }
}
