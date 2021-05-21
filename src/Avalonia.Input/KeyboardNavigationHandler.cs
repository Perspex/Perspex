using System;
using System.Linq;
using Avalonia.Input.Navigation;
using Avalonia.VisualTree;

namespace Avalonia.Input
{
    /// <summary>
    /// Handles keyboard navigation for a window.
    /// </summary>
    public class KeyboardNavigationHandler : IKeyboardNavigationHandler
    {
        /// <summary>
        /// The window to which the handler belongs.
        /// </summary>
        private IInputRoot? _owner;

        /// <summary>
        /// Sets the owner of the keyboard navigation handler.
        /// </summary>
        /// <param name="owner">The owner.</param>
        /// <remarks>
        /// This method can only be called once, typically by the owner itself on creation.
        /// </remarks>
        public void SetOwner(IInputRoot owner)
        {
            if (_owner != null)
            {
                throw new InvalidOperationException("AccessKeyHandler owner has already been set.");
            }

            _owner = owner ?? throw new ArgumentNullException(nameof(owner));
            _owner.AddHandler(InputElement.KeyDownEvent, OnKeyDown);
        }

        /// <summary>
        /// Gets the next control in the specified navigation direction.
        /// </summary>
        /// <param name="element">The element.</param>
        /// <param name="direction">The navigation direction.</param>
        /// <returns>
        /// The next element in the specified direction, or null if <paramref name="element"/>
        /// was the last in the requested direction.
        /// </returns>
        public static IInputElement? GetNext(
            IInputElement element,
            NavigationDirection direction)
        {
            element = element ?? throw new ArgumentNullException(nameof(element));

            var customHandler = element.GetSelfAndInputAncestors()
                .OfType<ICustomKeyboardNavigation>()
                .FirstOrDefault();

            if (customHandler != null)
            {
                var (handled, next) = customHandler.GetNext(element, direction);

                if (handled)
                {
                    if (next != null)
                    {
                        return next;
                    }
                    else if (direction == NavigationDirection.Next || direction == NavigationDirection.Previous)
                    {
                        return TabNavigation.GetNextInTabOrder((IInputElement)customHandler, direction, true);
                    }
                    else
                    {
                        return null;
                    }
                }
            }

            if (direction == NavigationDirection.Next || direction == NavigationDirection.Previous)
            {
                return TabNavigation.GetNextInTabOrder(element, direction);
            }
            else
            {
                throw new NotSupportedException();
            }
        }

        /// <summary>
        /// Moves the focus in the specified direction.
        /// </summary>
        /// <param name="element">The current element.</param>
        /// <param name="direction">The direction to move.</param>
        /// <param name="keyModifiers">Any key modifiers active at the time of focus.</param>
        public void Move(
            IInputElement element, 
            NavigationDirection direction,
            KeyModifiers keyModifiers = KeyModifiers.None)
        {
            element = element ?? throw new ArgumentNullException(nameof(element));

            var next = GetNext(element, direction);

            if (next != null)
            {
                var method = direction == NavigationDirection.Next ||
                             direction == NavigationDirection.Previous ?
                             NavigationMethod.Tab : NavigationMethod.Directional;
                FocusManager.Instance.Focus(next, method, keyModifiers);
            }
        }

        /// <summary>
        /// Handles the Tab key being pressed in the window.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event args.</param>
        protected virtual void OnKeyDown(object sender, KeyEventArgs e)
        {
            var current = FocusManager.Instance.Current;

            if (current != null && e.Key == Key.Tab)
            {
                var direction = (e.KeyModifiers & KeyModifiers.Shift) == 0 ?
                    NavigationDirection.Next : NavigationDirection.Previous;
                Move(current, direction, e.KeyModifiers);
                e.Handled = true;
            }
        }
    }
}
