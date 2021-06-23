namespace Avalonia.Input.Raw
{
    public enum RawKeyEventType
    {
        KeyDown,
        KeyUp
    }

    public class RawKeyEventArgs : RawInputEventArgs
    {
        public RawKeyEventArgs(
            IKeyboardDevice device,
            ulong timestamp,
            IInputRoot root,
            RawKeyEventType type,
            Key key, RawInputModifiers modifiers)
            : base(device, timestamp, root)
        {
            Key = key;
            Type = type;
            Modifiers = modifiers;
        }

        public Key Key { get; set; }

        public RawInputModifiers Modifiers { get; set; }

        public RawKeyEventType Type { get; set; }
    }
}
