namespace Avalonia.Input.Raw
{
    public class RawTextInputEventArgs : RawInputEventArgs
    {
        public RawTextInputEventArgs(
            IKeyboardDevice device,
            ulong timestamp,
            IInputRoot root,
            string text)
            : base(device, timestamp, root)
        {
            Text = text;
        }

        public string Text { get; set; }
    }
}
