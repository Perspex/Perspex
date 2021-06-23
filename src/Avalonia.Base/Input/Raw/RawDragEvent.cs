using System;

namespace Avalonia.Input.Raw
{
    public class RawDragEvent : RawInputEventArgs
    {
        public Point Location { get; set; }
        public IDataObject Data { get; }
        public DragDropEffects Effects { get; set; }
        public RawDragEventType Type { get; }
        [Obsolete("Use KeyModifiers")]
        public InputModifiers Modifiers { get; }
        public KeyModifiers KeyModifiers { get; }

        public RawDragEvent(IDragDropDevice inputDevice, RawDragEventType type, 
            IInputRoot root, Point location, IDataObject data, DragDropEffects effects, RawInputModifiers modifiers)
            :base(inputDevice, 0, root)
        {
            Type = type;
            Location = location;
            Data = data;
            Effects = effects;
            KeyModifiers = KeyModifiersUtils.ConvertToKey(modifiers);
#pragma warning disable CS0618 // Type or member is obsolete
            Modifiers = (InputModifiers)modifiers;
#pragma warning restore CS0618 // Type or member is obsolete
        }
    }
}
