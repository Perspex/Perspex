using System.Collections;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.LogicalTree;
using Avalonia.Styling;

namespace Avalonia.Input.GestureRecognizers
{
    public class GestureRecognizerCollection : IReadOnlyCollection<IGestureRecognizer>, IGestureRecognizerActionsDispatcher
    {
        private readonly IInputElement _inputElement;
        private List<IGestureRecognizer>? _recognizers;
        private Dictionary<IPointer, IGestureRecognizer>? _pointerGrabs;
        
        
        public GestureRecognizerCollection(IInputElement inputElement)
        {
            _inputElement = inputElement;
        }
        
        public void Add(IGestureRecognizer recognizer)
        {
            if (_recognizers == null)
            {
                // We initialize the collection when the first recognizer is added
                _recognizers = new List<IGestureRecognizer>();
                _pointerGrabs = new Dictionary<IPointer, IGestureRecognizer>();
            }

            _recognizers.Add(recognizer);
            recognizer.Initialize(_inputElement, this);

            // Hacks to make bindings work
            
            if (_inputElement is ILogical logicalParent && recognizer is ISetLogicalParent logical)
            {
                logical.SetParent(logicalParent);
                if (recognizer is IStyleable styleableRecognizer
                    && _inputElement is IStyleable styleableParent)
                    styleableRecognizer.Bind(StyledElement.TemplatedParentProperty,
                        styleableParent.GetObservable(StyledElement.TemplatedParentProperty));
            }
        }

        static readonly List<IGestureRecognizer> s_Empty = new List<IGestureRecognizer>();

        public IEnumerator<IGestureRecognizer> GetEnumerator()
            => _recognizers?.GetEnumerator() ?? s_Empty.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public int Count => _recognizers?.Count ?? 0;


        internal bool HandlePointerPressed(PointerPressedEventArgs e)
        {
            if (_recognizers == null)
                return false;
            foreach (var r in _recognizers)
            {
                if(e.Handled)
                    break;
                r.PointerPressed(e);
            }

            return e.Handled;
        }

        internal bool HandlePointerReleased(PointerReleasedEventArgs e)
        {
            if (_recognizers == null)
                return false;
            if (_pointerGrabs!.TryGetValue(e.Pointer, out var capture))
            {
                capture.PointerReleased(e);
            }
            else
                foreach (var r in _recognizers)
                {
                    if (e.Handled)
                        break;
                    r.PointerReleased(e);
                }
            return e.Handled;
        }

        internal bool HandlePointerMoved(PointerEventArgs e)
        {
            if (_recognizers == null)
                return false;
            if (_pointerGrabs!.TryGetValue(e.Pointer, out var capture))
            {
                capture.PointerMoved(e);
            }
            else
                foreach (var r in _recognizers)
                {
                    if (e.Handled)
                        break;
                    r.PointerMoved(e);
                }
            return e.Handled;
        }

        internal void HandlePointerCaptureLost(PointerCaptureLostEventArgs e)
        {
            if (_recognizers == null)
                return;
            _pointerGrabs!.Remove(e.Pointer);
            foreach (var r in _recognizers)
            {
                r.PointerCaptureLost(e.Pointer);
            }
        }

        void IGestureRecognizerActionsDispatcher.Capture(IPointer pointer, IGestureRecognizer recognizer)
        {
            pointer.Capture(_inputElement);
            _pointerGrabs![pointer] = recognizer;
            foreach (var r in _recognizers!)
            {
                if (r != recognizer)
                    r.PointerCaptureLost(pointer);
            }
        }

    }
}
