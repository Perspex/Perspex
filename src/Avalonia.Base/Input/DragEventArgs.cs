using System;
using Avalonia.Interactivity;
using Avalonia.VisualTree;

namespace Avalonia.Input
{
    public class DragEventArgs : RoutedEventArgs
    {
        private Interactive _target;
        private Point _targetLocation;

        public DragDropEffects DragEffects { get; set; }

        public IDataObject Data { get; private set; }

        [Obsolete("Use KeyModifiers")]
        public InputModifiers Modifiers { get; private set; }

        public KeyModifiers KeyModifiers { get; private set; }

        public Point GetPosition(IVisual relativeTo)
        {
            var point = new Point(0, 0);

            if (relativeTo == null)
            {
                throw new ArgumentNullException(nameof(relativeTo));
            }

            if (_target != null)
            {
                point = _target.TranslatePoint(_targetLocation, relativeTo) ?? point;
            }

            return point;
        }

        [Obsolete("Use constructor taking KeyModifiers")]
        public DragEventArgs(RoutedEvent<DragEventArgs> routedEvent, IDataObject data, Interactive target, Point targetLocation, InputModifiers modifiers)
            : base(routedEvent)
        {
            Data = data;
            _target = target;
            _targetLocation = targetLocation;
            Modifiers = modifiers;
            KeyModifiers = (KeyModifiers)(((int)modifiers) & 0xF);
        }

        public DragEventArgs(RoutedEvent<DragEventArgs> routedEvent, IDataObject data, Interactive target, Point targetLocation, KeyModifiers keyModifiers)
            : base(routedEvent)
        {
            Data = data;
            _target = target;
            _targetLocation = targetLocation;
            KeyModifiers = keyModifiers;
#pragma warning disable CS0618 // Type or member is obsolete
            Modifiers = (InputModifiers)keyModifiers;
#pragma warning restore CS0618 // Type or member is obsolete
        }

    }
}
