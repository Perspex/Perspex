using System.ComponentModel;
using System.Runtime.CompilerServices;
using Avalonia.Input.Raw;
using Avalonia.Input.TextInput;
using Avalonia.Interactivity;
using Avalonia.VisualTree;

namespace Avalonia.Input
{
    public class KeyboardDevice : IKeyboardDevice, INotifyPropertyChanged
    {
        private IInputElement? _focusedElement;
        private IInputRoot? _focusedRoot;

        public event PropertyChangedEventHandler? PropertyChanged;

        public static IKeyboardDevice Instance => AvaloniaLocator.Current.GetService<IKeyboardDevice>();

        public IInputManager InputManager => AvaloniaLocator.Current.GetService<IInputManager>();

        public IFocusManager FocusManager => AvaloniaLocator.Current.GetService<IFocusManager>();
        
        // This should live in the FocusManager, but with the current outdated architecture
        // the source of truth about the input focus is in KeyboardDevice
        private readonly TextInputMethodManager _textInputManager = new TextInputMethodManager();

        public IInputElement? FocusedElement
        {
            get
            {
                return _focusedElement;
            }

            private set
            {
                _focusedElement = value;
                _focusedRoot = _focusedElement?.InputRoot;

                RaisePropertyChanged();
                _textInputManager.SetFocusedElement(value);
            }
        }

        private void ClearFocusWithinInclusiveAncestors(IInputElement? element)
        {
            var el = element;
            
            while (el != null)
            {
                SetIsKeyboardFocusWithin(el, false);
                el = el.InputParent;
            }
        }

        // Clears IsKeyboardFocusWithin on descendants of the given element, but
        // not on the given element itself.
        private void ClearFocusWithinBelow(IInputElement element)
        {
            foreach (var child in element.InputChildren)
            {
                if (child.IsKeyboardFocusWithin)
                {
                    ClearFocusWithinBelow(child);
                    SetIsKeyboardFocusWithin(child, false);
                    break; // Assuming only one child can have the keyboard focus
                }
            }
        }

        private void SetIsKeyboardFocusWithin(IInputElement node, bool value)
        {
            if (node is InputElement element)
            {
                element.IsKeyboardFocusWithin = value;
            }
            else if (node is ContentInputElement contentElement)
            {
                contentElement.IsKeyboardFocusWithin = value;
            }
        }

        private void SetIsFocusWithin(IInputElement? oldElement, IInputElement? newElement)
        {
            if (newElement == null && oldElement != null)
            {
                ClearFocusWithinInclusiveAncestors(oldElement);
                return;
            }

            // Find the first ancestor of the new focus element that already
            // had focus in its subtree.
            IInputElement? branch = null;

            var el = newElement;

            while (el != null)
            {
                if (el.IsKeyboardFocusWithin)
                {
                    branch = el;
                    break;
                }

                el = el.InputParent;
            }

            // Clear any existing IsFocusWithin flags beneath the element
            // that will still maintain that flag after moving focus
            if (oldElement != null && branch != null)
            {
                ClearFocusWithinBelow(branch);
            }

            // Start marking every inclusive-ancestor of the new focus element
            // as having focus within, up until the element that aleady had it.
            el = newElement;
            while (el != null && el != branch)
            {
                SetIsKeyboardFocusWithin(el, true);

                el = el.InputParent;
            }
        }
        
        private void ClearChildrenFocusWithin(IInputElement element, bool clearRoot)
        {
            foreach (var child in element.InputChildren)
            {
                if (child.IsKeyboardFocusWithin)
                {
                    ClearChildrenFocusWithin(child, true);
                    break;
                }
            }
            
            if (clearRoot)
            {
                SetIsKeyboardFocusWithin(element, false);
            }
        }

        public void SetFocusedElement(
            IInputElement? element, 
            NavigationMethod method,
            KeyModifiers keyModifiers)
        {
            if (element != FocusedElement)
            {
                // If the focus is moving from one focus root to another (i.e. between windows)
                // Clear the focus in the old focus root entirely. This is also the case
                // If the previously focused element is no longer in the same focus root
                // as it was when it was focused (i.e. it was hidden and lost focus because of that)
                if (FocusedElement != null && 
                    (_focusedRoot != FocusedElement.InputRoot ||
                     _focusedRoot != element?.InputRoot) &&
                    _focusedRoot != null)
                {
                    ClearChildrenFocusWithin(_focusedRoot, true);
                }

                // Now set the focus within along the ancestors of the new focus
                // element, if possible maintaining it for common ancestors of
                // the new and old focus element.
                SetIsFocusWithin(FocusedElement, element);

                var previouslyFocused = FocusedElement;
                FocusedElement = element;

                previouslyFocused?.RaiseEvent(new RoutedEventArgs
                {
                    RoutedEvent = InputElement.LostFocusEvent,
                });

                element?.RaiseEvent(new GotFocusEventArgs
                {
                    RoutedEvent = InputElement.GotFocusEvent,
                    NavigationMethod = method,
                    KeyModifiers = keyModifiers,
                });
            }
        }

        protected void RaisePropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void ProcessRawEvent(RawInputEventArgs e)
        {
            if(e.Handled)
                return;

            var element = FocusedElement ?? e.Root;

            if (e is RawKeyEventArgs keyInput)
            {
                switch (keyInput.Type)
                {
                    case RawKeyEventType.KeyDown:
                    case RawKeyEventType.KeyUp:
                        var routedEvent = keyInput.Type == RawKeyEventType.KeyDown
                            ? InputElement.KeyDownEvent
                            : InputElement.KeyUpEvent;

                        KeyEventArgs ev = new KeyEventArgs
                        {
                            RoutedEvent = routedEvent,
                            Device = this,
                            Key = keyInput.Key,
                            KeyModifiers = KeyModifiersUtils.ConvertToKey(keyInput.Modifiers),
                            Source = element,
                        };

                        var currentHandler = element;
                        while (currentHandler != null && !ev.Handled && keyInput.Type == RawKeyEventType.KeyDown)
                        {
                            var bindings = currentHandler.KeyBindings;
                            foreach (var binding in bindings)
                            {
                                if (ev.Handled)
                                    break;
                                binding.TryHandle(ev);
                            }
                            currentHandler = currentHandler.InputParent;
                        }

                        element.RaiseEvent(ev);
                        e.Handled = ev.Handled;
                        break;
                }
            }

            if (e is RawTextInputEventArgs text)
            {
                var ev = new TextInputEventArgs()
                {
                    Device = this,
                    Text = text.Text,
                    Source = element,
                    RoutedEvent = InputElement.TextInputEvent
                };

                element.RaiseEvent(ev);
                e.Handled = ev.Handled;
            }
        }
    }
}
