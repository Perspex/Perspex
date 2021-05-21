using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.VisualTree;

namespace Avalonia.Input.Navigation
{
    /// <summary>
    /// The implementation for default tab navigation.
    /// </summary>
    internal static class TabNavigation
    {
        /// <summary>
        /// Gets the next control in the specified tab direction.
        /// </summary>
        /// <param name="element">The element.</param>
        /// <param name="direction">The tab direction. Must be Next or Previous.</param>
        /// <param name="outsideElement">
        /// If true will not descend into <paramref name="element"/> to find next control.
        /// </param>
        /// <returns>
        /// The next element in the specified direction, or null if <paramref name="element"/>
        /// was the last in the requested direction.
        /// </returns>
        public static IInputElement? GetNextInTabOrder(
            IInputElement element,
            NavigationDirection direction,
            bool outsideElement = false)
        {
            element = element ?? throw new ArgumentNullException(nameof(element));

            if (direction != NavigationDirection.Next && direction != NavigationDirection.Previous)
            {
                throw new ArgumentException("Invalid direction: must be Next or Previous.");
            }

            var container = element.InputParent;

            if (container != null)
            {
                switch (container.TabNavigation)
                {
                    case KeyboardNavigationMode.Continue:
                        return GetNextInContainer(element, container, direction, outsideElement) ??
                               GetFirstInNextContainer(element, element, direction);
                    case KeyboardNavigationMode.Cycle:
                        return GetNextInContainer(element, container, direction, outsideElement) ??
                               GetFocusableDescendant(container, direction);
                    case KeyboardNavigationMode.Contained:
                        return GetNextInContainer(element, container, direction, outsideElement);
                    default:
                        return GetFirstInNextContainer(element, container, direction);
                }
            }
            else
            {
                return GetFocusableDescendants(element, direction).FirstOrDefault();
            }
        }

        /// <summary>
        /// Gets the first or last focusable descendant of the specified element.
        /// </summary>
        /// <param name="container">The element.</param>
        /// <param name="direction">The direction to search.</param>
        /// <returns>The element or null if not found.##</returns>
        private static IInputElement? GetFocusableDescendant(IInputElement container, NavigationDirection direction)
        {
            return direction == NavigationDirection.Next ?
                GetFocusableDescendants(container, direction).FirstOrDefault() :
                GetFocusableDescendants(container, direction).LastOrDefault();
        }

        /// <summary>
        /// Gets the focusable descendants of the specified element.
        /// </summary>
        /// <param name="element">The element.</param>
        /// <param name="direction">The tab direction. Must be Next or Previous.</param>
        /// <returns>The element's focusable descendants.</returns>
        private static IEnumerable<IInputElement> GetFocusableDescendants(IInputElement element,
            NavigationDirection direction)
        {
            var mode = element.TabNavigation;

            if (mode == KeyboardNavigationMode.None)
            {
                yield break;
            }

            var children = element.InputChildren;

            if (mode == KeyboardNavigationMode.Once)
            {
                var active = element.TabOnceActiveElement;

                if (active != null)
                {
                    yield return active;
                    yield break;
                }
                else
                {
                    children = children.Take(1);
                }
            }

            foreach (var child in children)
            {
                var customNext = GetCustomNext(child, direction);

                if (customNext.handled)
                {
                    yield return customNext.next!;
                }
                else
                {
                    if (child.CanFocus() && child.IsTabFocusable)
                    {
                        yield return child;
                    }

                    if (child.CanFocusDescendants())
                    {
                        foreach (var descendant in GetFocusableDescendants(child, direction))
                        {
                            yield return descendant;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Gets the next item that should be focused in the specified container.
        /// </summary>
        /// <param name="element">The starting element/</param>
        /// <param name="container">The container.</param>
        /// <param name="direction">The direction.</param>
        /// <param name="outsideElement">
        /// If true will not descend into <paramref name="element"/> to find next control.
        /// </param>
        /// <returns>The next element, or null if the element is the last.</returns>
        private static IInputElement? GetNextInContainer(
            IInputElement element,
            IInputElement container,
            NavigationDirection direction,
            bool outsideElement)
        {
            var e = element;

            if (direction == NavigationDirection.Next && !outsideElement)
            {
                var descendant = GetFocusableDescendants(element, direction).FirstOrDefault();

                if (descendant != null)
                {
                    return descendant;
                }
            }

            // TODO: Do a spatial search here if the container doesn't implement
            // INavigableContainer.
            if (container is INavigableContainer navigable)
            {
                while (e != null)
                {
                    e = navigable.GetControl(direction, e, false);

                    if (e != null && e.CanFocus() && e.IsTabFocusable)
                    {
                        break;
                    }
                }
            }
            else
            {
                // TODO: Do a spatial search here if the container doesn't implement
                // INavigableContainer.
                e = null;
            }

            if (e != null && direction == NavigationDirection.Previous)
            {
                var descendant = GetFocusableDescendants(e, direction).LastOrDefault();

                if (descendant != null)
                {
                    return descendant;
                }
            }

            return e;
        }

        /// <summary>
        /// Gets the first item that should be focused in the next container.
        /// </summary>
        /// <param name="element">The element being navigated away from.</param>
        /// <param name="container">The container.</param>
        /// <param name="direction">The direction of the search.</param>
        /// <returns>The first element, or null if there are no more elements.</returns>
        private static IInputElement? GetFirstInNextContainer(
            IInputElement element,
            IInputElement container,
            NavigationDirection direction)
        {
            var parent = container.InputParent;
            IInputElement? next;

            if (parent != null)
            {
                if (direction == NavigationDirection.Previous &&
                    parent.CanFocus() && 
                    parent.IsTabFocusable)
                {
                    return parent;
                }

                var allSiblings = parent.InputChildren.Where(FocusExtensions.CanFocusDescendants);
                var siblings = direction == NavigationDirection.Next ?
                    allSiblings.SkipWhile(x => x != container).Skip(1) :
                    allSiblings.TakeWhile(x => x != container).Reverse();

                foreach (var sibling in siblings)
                {
                    var customNext = GetCustomNext(sibling, direction);
                    if (customNext.handled)
                    {
                        return customNext.next;
                    }

                    if (sibling.CanFocus() && sibling.IsTabFocusable)
                    {
                        return sibling;
                    }

                    next = direction == NavigationDirection.Next ?
                        GetFocusableDescendants(sibling, direction).FirstOrDefault() :
                        GetFocusableDescendants(sibling, direction).LastOrDefault();

                    if (next != null)
                    {
                        return next;
                    }
                }

                next = GetFirstInNextContainer(element, parent, direction);
            }
            else
            {
                next = direction == NavigationDirection.Next ?
                    GetFocusableDescendants(container, direction).FirstOrDefault() :
                    GetFocusableDescendants(container, direction).LastOrDefault();
            }

            return next;
        }

        private static (bool handled, IInputElement? next) GetCustomNext(IInputElement element,
            NavigationDirection direction)
        {
            if (element is ICustomKeyboardNavigation custom)
            {
                return custom.GetNext(element, direction);
            }

            return (false, null);
        }
    }
}
