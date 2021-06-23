using System;

namespace Avalonia.Input.Raw
{
    public class RawSizeEventArgs : EventArgs
    {
        public RawSizeEventArgs(Size size)
        {
            Size = size;
        }

        public RawSizeEventArgs(double width, double height)
        {
            Size = new Size(width, height);
        }

        public Size Size { get; private set; }
    }
}
