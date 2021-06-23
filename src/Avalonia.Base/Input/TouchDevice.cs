using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Input.Raw;
using Avalonia.VisualTree;

namespace Avalonia.Input
{
    /// <summary>
    /// Handles raw touch events
    /// <remarks>
    /// This class is supposed to be used on per-toplevel basis, don't use a shared one
    /// </remarks>
    /// </summary>
    public class TouchDevice : IInputDevice, IDisposable
    {
        private readonly Dictionary<long, Pointer> _pointers = new Dictionary<long, Pointer>();
        private bool _disposed;
        
        KeyModifiers GetKeyModifiers(RawInputModifiers modifiers) =>
            (KeyModifiers)(modifiers & RawInputModifiers.KeyboardMask);

        RawInputModifiers GetModifiers(RawInputModifiers modifiers, bool isLeftButtonDown)
        {
            var rv = modifiers &= RawInputModifiers.KeyboardMask;
            if (isLeftButtonDown)
                rv |= RawInputModifiers.LeftMouseButton;
            return rv;
        }
        
        public void ProcessRawEvent(RawInputEventArgs ev)
        {
            if(_disposed)
                return;
            var args = (RawTouchEventArgs)ev;
            if (!_pointers.TryGetValue(args.TouchPointId, out var pointer))
            {
                if (args.Type == RawPointerEventType.TouchEnd)
                    return;
                var hit = args.Root.InputHitTest(args.Position);

                _pointers[args.TouchPointId] = pointer = new Pointer(Pointer.GetNextFreeId(),
                    PointerType.Touch, _pointers.Count == 0);
                pointer.Capture(hit);
            }
            

            var target = pointer.Captured ?? args.Root;
            if (args.Type == RawPointerEventType.TouchBegin)
            {
                target.RaiseEvent(new PointerPressedEventArgs(target, pointer,
                    args.Root, args.Position, ev.Timestamp,
                    new PointerPointProperties(GetModifiers(args.InputModifiers, true),
                        PointerUpdateKind.LeftButtonPressed),
                    GetKeyModifiers(args.InputModifiers)));
            }

            if (args.Type == RawPointerEventType.TouchEnd)
            {
                _pointers.Remove(args.TouchPointId);
                using (pointer)
                {
                    target.RaiseEvent(new PointerReleasedEventArgs(target, pointer,
                        args.Root, args.Position, ev.Timestamp,
                        new PointerPointProperties(GetModifiers(args.InputModifiers, false),
                            PointerUpdateKind.LeftButtonReleased),
                        GetKeyModifiers(args.InputModifiers), MouseButton.Left));
                }
            }

            if (args.Type == RawPointerEventType.TouchCancel)
            {
                _pointers.Remove(args.TouchPointId);
                using (pointer)
                    pointer.Capture(null);
            }

            if (args.Type == RawPointerEventType.TouchUpdate)
            {
                var modifiers = GetModifiers(args.InputModifiers, pointer.IsPrimary);
                target.RaiseEvent(new PointerEventArgs(InputElement.PointerMovedEvent, target, pointer, args.Root,
                    args.Position, ev.Timestamp,
                    new PointerPointProperties(GetModifiers(args.InputModifiers, true), PointerUpdateKind.Other),
                    GetKeyModifiers(args.InputModifiers)));
            }

            
        }

        public void Dispose()
        {
            if(_disposed)
                return;
            var values = _pointers.Values.ToList();
            _pointers.Clear();
            _disposed = true;
            foreach (var p in values)
                p.Dispose();
        }
        
    }
}
