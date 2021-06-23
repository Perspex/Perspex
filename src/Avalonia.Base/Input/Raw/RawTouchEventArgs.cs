namespace Avalonia.Input.Raw
{
    public class RawTouchEventArgs : RawPointerEventArgs
    {
        public RawTouchEventArgs(IInputDevice device, ulong timestamp, IInputRoot root,
            RawPointerEventType type, Point position, RawInputModifiers inputModifiers,
            long touchPointId) 
            : base(device, timestamp, root, type, position, inputModifiers)
        {
            TouchPointId = touchPointId;
        }

        public long TouchPointId { get; set; }
    }
}
