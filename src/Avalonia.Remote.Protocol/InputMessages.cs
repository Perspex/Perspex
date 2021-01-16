using System;
/*
 We are keeping copies of core events here, so they can be used 
 without referencing Avalonia itself, e. g. from projects that
 are using WPF, GTK#, etc
 */
namespace Avalonia.Remote.Protocol.Input
{
    /// <summary>
    /// Keep this in sync with InputModifiers in the main library
    /// </summary>
    [Flags]
    public enum InputModifiers
    {
        Alt,
        Control,
        Shift,
        Windows,
        LeftMouseButton,
        RightMouseButton,
        MiddleMouseButton
    }

    /// <summary>
    /// Keep this in sync with InputModifiers in the main library
    /// </summary>
    public enum MouseButton
    {
        None,
        Left,
        Right,
        Middle
    }

    public abstract class InputEventMessageBase
    {
        public InputModifiers[] Modifiers { get; set; }
    }

    public abstract class PointerEventMessageBase : InputEventMessageBase
    {
        public double X { get; set; }
        public double Y { get; set; }
    }

    [AvaloniaRemoteMessageGuid("6228F0B9-99F2-4F62-A621-414DA2881648")]
    public class PointerMovedEventMessage : PointerEventMessageBase
    {
        
    }

    [AvaloniaRemoteMessageGuid("7E9E2818-F93F-411A-800E-6B1AEB11DA46")]
    public class PointerPressedEventMessage : PointerEventMessageBase
    {
        public MouseButton Button { get; set; }
    }
    
    [AvaloniaRemoteMessageGuid("4ADC84EE-E7C8-4BCF-986C-DE3A2F78EDE4")]
    public class PointerReleasedEventMessage : PointerEventMessageBase
    {
        public MouseButton Button { get; set; }
    }

    [AvaloniaRemoteMessageGuid("79301A05-F02D-4B90-BB39-472563B504AE")]
    public class ScrollEventMessage : PointerEventMessageBase
    {
        public double DeltaX { get; set; }
        public double DeltaY { get; set; }
    }

    [AvaloniaRemoteMessageGuid("1C3B691E-3D54-4237-BFB0-9FEA83BC1DB8")]
    public class KeyEventMessage : InputEventMessageBase
    {
        public bool IsDown { get; set; }
        public Key Key { get; set; }
        public string MappedKey { get; set; }
    }

    [AvaloniaRemoteMessageGuid("C174102E-7405-4594-916F-B10B8248A17D")]
    public class TextInputEventMessage : InputEventMessageBase
    {
        public string Text { get; set; }
    }

}
