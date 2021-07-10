using Avalonia.Interactivity;
using Avalonia.VisualTree;
using System.Linq;
using Avalonia.Input.Raw;

namespace Avalonia.Input
{
    public class DragDropDevice : IDragDropDevice
    {
        public static readonly DragDropDevice Instance = new DragDropDevice();
        
        private Interactive? _lastTarget = null;
        
        private Interactive? GetTarget(IInputRoot root, Point local)
        {
            var target = root.InputHitTest(local)?.GetSelfAndVisualAncestors()?.OfType<Interactive>()?.FirstOrDefault();
            if (target != null && DragDrop.GetAllowDrop(target))
                return target;
            return null;
        }
        
        private DragDropEffects RaiseDragEvent(Interactive? target, IInputRoot inputRoot, Point point, RoutedEvent<DragEventArgs> routedEvent, DragDropEffects operation, IDataObject data, KeyModifiers modifiers)
        {
            if (target == null)
                return DragDropEffects.None;

            var p = inputRoot.TranslatePoint(point, target);

            if (!p.HasValue)
                return DragDropEffects.None;

            var args = new DragEventArgs(routedEvent, data, target, p.Value, modifiers)
            {
                RoutedEvent = routedEvent,
                DragEffects = operation
            };
            target.RaiseEvent(args);
            return args.DragEffects;
        }
        
        private DragDropEffects DragEnter(IInputRoot inputRoot, Point point, IDataObject data, DragDropEffects effects, KeyModifiers modifiers)
        {
            _lastTarget = GetTarget(inputRoot, point);
            return RaiseDragEvent(_lastTarget, inputRoot, point, DragDrop.DragEnterEvent, effects, data, modifiers);
        }

        private DragDropEffects DragOver(IInputRoot inputRoot, Point point, IDataObject data, DragDropEffects effects, KeyModifiers modifiers)
        {
            var target = GetTarget(inputRoot, point);

            if (target == _lastTarget)
                return RaiseDragEvent(target, inputRoot, point, DragDrop.DragOverEvent, effects, data, modifiers);
            
            try
            {
                if (_lastTarget != null)
                    _lastTarget.RaiseEvent(new RoutedEventArgs(DragDrop.DragLeaveEvent));
                return RaiseDragEvent(target, inputRoot, point, DragDrop.DragEnterEvent, effects, data, modifiers);
            }
            finally
            {
                _lastTarget = target;
            }            
        }

        private void DragLeave(IInputElement inputRoot)
        {
            if (_lastTarget == null)
                return;
            try
            {
                _lastTarget.RaiseEvent(new RoutedEventArgs(DragDrop.DragLeaveEvent));
            }
            finally 
            {
                _lastTarget = null;
            }
        }

        private DragDropEffects Drop(IInputRoot inputRoot, Point point, IDataObject data, DragDropEffects effects, KeyModifiers modifiers)
        {
            try
            {
                return RaiseDragEvent(_lastTarget, inputRoot, point, DragDrop.DropEvent, effects, data, modifiers);
            }
            finally 
            {
                _lastTarget = null;
            }
        }

        public void ProcessRawEvent(RawInputEventArgs e)
        {
            if (!e.Handled && e is RawDragEvent margs && e.Root?.IsPlatformEnabled != false)
                ProcessRawEvent(margs);
        }

        private void ProcessRawEvent(RawDragEvent e)
        {
            switch (e.Type)
            {
                case RawDragEventType.DragEnter:
                    e.Effects = DragEnter(e.Root, e.Location, e.Data, e.Effects, e.KeyModifiers);
                    break;
                case RawDragEventType.DragOver:
                    e.Effects = DragOver(e.Root, e.Location, e.Data, e.Effects, e.KeyModifiers);
                    break;
                case RawDragEventType.DragLeave:
                    DragLeave(e.Root);
                    break;
                case RawDragEventType.Drop:
                    e.Effects = Drop(e.Root, e.Location, e.Data, e.Effects, e.KeyModifiers);
                    break;
            }
        }
    }
}
