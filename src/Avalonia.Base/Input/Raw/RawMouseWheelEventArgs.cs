
namespace Avalonia.Input.Raw
{
    public class RawMouseWheelEventArgs : RawPointerEventArgs
    {
        public RawMouseWheelEventArgs(
            IInputDevice device,
            ulong timestamp,
            IInputRoot root,
            Point position,
            Vector delta, RawInputModifiers inputModifiers)
            : base(device, timestamp, root, RawPointerEventType.Wheel, position, inputModifiers)
        {
            Delta = delta;
        }

        public Vector Delta { get; private set; }
    }
}
