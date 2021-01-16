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
            Key key, 
            string mappedKey, RawInputModifiers modifiers)
            : base(device, timestamp, root)
        {
            Key = key;
            MappedKey = mappedKey;
            Type = type;
            Modifiers = modifiers;
        }

        public RawKeyEventArgs(
            IKeyboardDevice device,
            ulong timestamp,
            IInputRoot root,
            RawKeyEventType type,
            Key key, 
            RawInputModifiers modifiers)
            : this(device, timestamp, root, type, key, null, modifiers) { }

        public Key Key { get; set; }

        public string MappedKey { get; set; }

        public RawInputModifiers Modifiers { get; set; }

        public RawKeyEventType Type { get; set; }
    }
}
