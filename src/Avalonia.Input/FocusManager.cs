using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Avalonia.Input.Navigation;
using Avalonia.Interactivity;
using Avalonia.VisualTree;

namespace Avalonia.Input
{
    /// <summary>
    /// Manages focus for the application.
    /// </summary>
    public class FocusManager : IFocusManager
    {
        /// <summary>
        /// The focus scopes in which the focus is currently defined.
        /// </summary>
        private readonly ConditionalWeakTable<IFocusScope, IInputElement?> _focusScopes =
            new ConditionalWeakTable<IFocusScope, IInputElement?>();

        /// <summary>
        /// Initializes a new instance of the <see cref="FocusManager"/> class.
        /// </summary>
        static FocusManager()
        {
            InputElement.PointerPressedEvent.AddClassHandler(
                typeof(IInputElement),
                new EventHandler<RoutedEventArgs>(OnPreviewPointerPressed),
                RoutingStrategies.Tunnel);
        }

        /// <summary>
        /// Gets the instance of the <see cref="IFocusManager"/>.
        /// </summary>
        public static IFocusManager Instance => AvaloniaLocator.Current.GetService<IFocusManager>();

        /// <summary>
        /// Gets the currently focused <see cref="IInputElement"/>.
        /// </summary>
        public IInputElement? Current => KeyboardDevice.Instance?.FocusedElement;

        /// <summary>
        /// Gets the current focus scope.
        /// </summary>
        public IFocusScope? Scope
        {
            get;
            private set;
        }

        /// <summary>
        /// Retrieves the element that should receive focus based on the specified navigation direction.
        /// </summary>
        /// <param name="direction">The direction to move in.</param>
        /// <returns>The next element, or null if no element was found.</returns>
        public IInputElement? FindNextElement(NavigationDirection direction)
        {
            var container = Current?.VisualParent;

            if (container is null)
            {
                return null;
            }

            if (container is ICustomKeyboardNavigation custom)
            {
                var (handled, next) = custom.GetNext(Current, direction);

                if (handled)
                {
                    return next;
                }
            }

            static IInputElement? GetFirst(IVisual container)
            {
                for (var i = 0; i < container.VisualChildren.Count; ++i)
                {
                    if (container.VisualChildren[i] is IInputElement ie && ie.CanFocus())
                    {
                        return ie;
                    }
                }
                
                return null;
            }

            static IInputElement? GetLast(IVisual container)
            {
                for (var i = container.VisualChildren.Count - 1; i >= 0; --i)
                {
                    if (container.VisualChildren[i] is IInputElement ie && ie.CanFocus())
                    {
                        return ie;
                    }
                }

                return null;
            }

            return direction switch
            {
                NavigationDirection.Next => TabNavigation.GetNextInTabOrder(Current, direction),
                NavigationDirection.Previous => TabNavigation.GetNextInTabOrder(Current, direction),
                NavigationDirection.First => GetFirst(container),
                NavigationDirection.Last => GetLast(container),
                _ => FindInDirection(container, Current, direction),
            };
        }

        /// <summary>
        /// Focuses a control.
        /// </summary>
        /// <param name="control">The control to focus.</param>
        /// <param name="method">The method by which focus was changed.</param>
        /// <param name="keyModifiers">Any key modifiers active at the time of focus.</param>
        public void Focus(
            IInputElement? control, 
            NavigationMethod method = NavigationMethod.Unspecified,
            KeyModifiers keyModifiers = KeyModifiers.None)
        {
            if (control != null)
            {
                var scope = GetFocusScopeAncestors(control)
                    .FirstOrDefault();

                if (scope != null)
                {
                    Scope = scope;
                    SetFocusedElement(scope, control, method, keyModifiers);
                }
            }
            else if (Current != null)
            {
                // If control is null, set focus to the topmost focus scope.
                foreach (var scope in GetFocusScopeAncestors(Current).Reverse().ToList())
                {
                    if (_focusScopes.TryGetValue(scope, out var element) && element != null)
                    {
                        Focus(element, method);
                        return;
                    }
                }

                if (Scope is object)
                {
                    // Couldn't find a focus scope, clear focus.
                    SetFocusedElement(Scope, null);
                }
            }
        }

        /// <summary>
        /// Sets the currently focused element in the specified scope.
        /// </summary>
        /// <param name="scope">The focus scope.</param>
        /// <param name="element">The element to focus. May be null.</param>
        /// <param name="method">The method by which focus was changed.</param>
        /// <param name="keyModifiers">Any key modifiers active at the time of focus.</param>
        /// <remarks>
        /// If the specified scope is the current <see cref="Scope"/> then the keyboard focus
        /// will change.
        /// </remarks>
        public void SetFocusedElement(
            IFocusScope scope,
            IInputElement? element,
            NavigationMethod method = NavigationMethod.Unspecified,
            KeyModifiers keyModifiers = KeyModifiers.None)
        {
            scope = scope ?? throw new ArgumentNullException(nameof(scope));

            if (_focusScopes.TryGetValue(scope, out var existingElement))
            {
                if (element != existingElement)
                {
                    _focusScopes.Remove(scope);
                    _focusScopes.Add(scope, element);
                }
            }
            else
            {
                _focusScopes.Add(scope, element);
            }

            if (Scope == scope)
            {
                KeyboardDevice.Instance?.SetFocusedElement(element, method, keyModifiers);
            }
        }

        /// <summary>
        /// Notifies the focus manager of a change in focus scope.
        /// </summary>
        /// <param name="scope">The new focus scope.</param>
        public void SetFocusScope(IFocusScope scope)
        {
            scope = scope ?? throw new ArgumentNullException(nameof(scope));

            if (!_focusScopes.TryGetValue(scope, out var e))
            {
                // TODO: Make this do something useful, i.e. select the first focusable
                // control, select a control that the user has specified to have default
                // focus etc.
                e = scope as IInputElement;
                _focusScopes.Add(scope, e);
            }

            Scope = scope;
            Focus(e);
        }

        /// <summary>
        /// Checks if the specified element can be focused.
        /// </summary>
        /// <param name="e">The element.</param>
        /// <returns>True if the element can be focused.</returns>
        private static bool CanFocus(IInputElement e) => e.Focusable && e.IsEffectivelyEnabled && e.IsVisible;

        /// <summary>
        /// Gets the focus scope ancestors of the specified control, traversing popups.
        /// </summary>
        /// <param name="control">The control.</param>
        /// <returns>The focus scopes.</returns>
        private static IEnumerable<IFocusScope> GetFocusScopeAncestors(IInputElement control)
        {
            IInputElement? c = control;

            while (c != null)
            {
                var scope = c as IFocusScope;

                if (scope != null && c.VisualRoot?.IsVisible == true)
                {
                    yield return scope;
                }

                c = c.GetVisualParent<IInputElement>() ??
                    ((c as IHostedVisualTreeRoot)?.Host as IInputElement);
            }
        }


        private IInputElement? FindInDirection(
            IVisual container,
            IInputElement from,
            NavigationDirection direction)
        {
            static double Distance(NavigationDirection direction, IInputElement from, IInputElement to)
            {
                return direction switch
                {
                    NavigationDirection.Left => from.Bounds.Right - to.Bounds.Right,
                    NavigationDirection.Right => to.Bounds.X - from.Bounds.X,
                    NavigationDirection.Up => from.Bounds.Bottom - to.Bounds.Bottom,
                    NavigationDirection.Down => to.Bounds.Y - from.Bounds.Y,
                    _ => throw new NotSupportedException("direction must be Up, Down, Left or Right"),
                };
            }

            IInputElement? result = null;
            var resultDistance = double.MaxValue;

            foreach (var visual in container.VisualChildren)
            {
                if (visual is IInputElement child && child != from && child.CanFocus())
                {
                    var distance = Distance(direction, from, child);

                    if (distance > 0 && distance < resultDistance)
                    {
                        result = child;
                        resultDistance = distance;
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Global handler for pointer pressed events.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event args.</param>
        private static void OnPreviewPointerPressed(object sender, RoutedEventArgs e)
        {
            var ev = (PointerPressedEventArgs)e;
            var visual = (IVisual)sender;

            if (sender == e.Source && ev.GetCurrentPoint(visual).Properties.IsLeftButtonPressed)
            {
                IVisual? element = ev.Pointer?.Captured ?? e.Source as IInputElement;

                while (element != null)
                {
                    if (element is IInputElement inputElement && CanFocus(inputElement))
                    {
                        Instance?.Focus(inputElement, NavigationMethod.Pointer, ev.KeyModifiers);

                        break;
                    }
                    
                    element = element.VisualParent;
                }
            }
        }
    }
}
