namespace Avalonia.Input
{
    public sealed class PointerPoint
    {
        public PointerPoint(IPointer pointer, Point position, PointerPointProperties properties)
        {
            Pointer = pointer;
            Position = position;
            Properties = properties;
        }
        public IPointer Pointer { get; }
        public PointerPointProperties Properties { get; }
        public Point Position { get; }
    }

    public sealed class PointerPointProperties
    {
        public bool IsLeftButtonPressed { get; }
        public bool IsMiddleButtonPressed { get; }
        public bool IsRightButtonPressed { get; }
        public bool IsXButton1Pressed { get; }
        public bool IsXButton2Pressed { get; }

        public PointerUpdateKind PointerUpdateKind { get; }

        private PointerPointProperties()
        {            
        }
        
        public PointerPointProperties(RawInputModifiers modifiers, PointerUpdateKind kind)
        {
            PointerUpdateKind = kind;

            IsLeftButtonPressed = modifiers.HasAllFlags(RawInputModifiers.LeftMouseButton);
            IsMiddleButtonPressed = modifiers.HasAllFlags(RawInputModifiers.MiddleMouseButton);
            IsRightButtonPressed = modifiers.HasAllFlags(RawInputModifiers.RightMouseButton);
            IsXButton1Pressed = modifiers.HasAllFlags(RawInputModifiers.XButton1MouseButton);
            IsXButton2Pressed = modifiers.HasAllFlags(RawInputModifiers.XButton2MouseButton);

            // The underlying input source might be reporting the previous state,
            // so make sure that we reflect the current state
            
            if (kind == PointerUpdateKind.LeftButtonPressed)
                IsLeftButtonPressed = true;
            if (kind == PointerUpdateKind.LeftButtonReleased)
                IsLeftButtonPressed = false;
            if (kind == PointerUpdateKind.MiddleButtonPressed)
                IsMiddleButtonPressed = true;
            if (kind == PointerUpdateKind.MiddleButtonReleased)
                IsMiddleButtonPressed = false;
            if (kind == PointerUpdateKind.RightButtonPressed)
                IsRightButtonPressed = true;
            if (kind == PointerUpdateKind.RightButtonReleased)
                IsRightButtonPressed = false;
            if (kind == PointerUpdateKind.XButton1Pressed)
                IsXButton1Pressed = true;
            if (kind == PointerUpdateKind.XButton1Released)
                IsXButton1Pressed = false;
            if (kind == PointerUpdateKind.XButton2Pressed)
                IsXButton2Pressed = true;
            if (kind == PointerUpdateKind.XButton2Released)
                IsXButton2Pressed = false;
        }

        public static PointerPointProperties None { get; } = new PointerPointProperties();
    }

    public enum PointerUpdateKind
    {
        LeftButtonPressed,
        MiddleButtonPressed,
        RightButtonPressed,
        XButton1Pressed,
        XButton2Pressed,
        LeftButtonReleased,
        MiddleButtonReleased,
        RightButtonReleased,
        XButton1Released,
        XButton2Released,
        Other
    }

    public static class PointerUpdateKindExtensions
    {
        public static MouseButton GetMouseButton(this PointerUpdateKind kind)
        {
            if (kind == PointerUpdateKind.LeftButtonPressed || kind == PointerUpdateKind.LeftButtonReleased)
                return MouseButton.Left;
            if (kind == PointerUpdateKind.MiddleButtonPressed || kind == PointerUpdateKind.MiddleButtonReleased)
                return MouseButton.Middle;
            if (kind == PointerUpdateKind.RightButtonPressed || kind == PointerUpdateKind.RightButtonReleased)
                return MouseButton.Right;
            if (kind == PointerUpdateKind.XButton1Pressed || kind == PointerUpdateKind.XButton1Released)
                return MouseButton.XButton1;
            if (kind == PointerUpdateKind.XButton2Pressed || kind == PointerUpdateKind.XButton2Released)
                return MouseButton.XButton2;
            return MouseButton.None;
        }
    }
}
