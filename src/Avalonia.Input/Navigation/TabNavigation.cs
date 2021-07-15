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
        public static IInputElement? GetNextTab(IInputElement e, bool goDownOnly)
        {
            return GetNextTab(e, GetGroupParent(e), goDownOnly);
        }

        public static IInputElement? GetNextTab(IInputElement? e, IInputElement container, bool goDownOnly)
        {
            var tabbingType = GetKeyNavigationMode(container);

            if (e == null)
            {
                if (IsTabStop(container))
                    return container;

                // Using ActiveElement if set
                var activeElement = GetActiveElement(container);
                if (activeElement != null)
                    return GetNextTab(null, activeElement, true);
            }
            else
            {
                if (tabbingType == KeyboardNavigationMode.Once || tabbingType == KeyboardNavigationMode.None)
                {
                    if (container != e)
                    {
                        if (goDownOnly)
                            return null;
                        var parentContainer = GetGroupParent(container);
                        return GetNextTab(container, parentContainer, goDownOnly);
                    }
                }
            }

            // All groups
            IInputElement? loopStartElement = null;
            var nextTabElement = e;
            var currentTabbingType = tabbingType;

            // Search down inside the container
            while ((nextTabElement = GetNextTabInGroup(nextTabElement, container, currentTabbingType)) != null)
            {
                // Avoid the endless loop here for Cycle groups
                if (loopStartElement == nextTabElement)
                    break;
                if (loopStartElement == null)
                    loopStartElement = nextTabElement;

                var firstTabElementInside = GetNextTab(null, nextTabElement, true);
                if (firstTabElementInside != null)
                    return firstTabElementInside;

                // If we want to continue searching inside the Once groups, we should change the navigation mode
                if (currentTabbingType == KeyboardNavigationMode.Once)
                    currentTabbingType = KeyboardNavigationMode.Contained;
            }

            // If there is no next element in the group (nextTabElement == null)

            // Search up in the tree if allowed
            // consider: Use original tabbingType instead of currentTabbingType
            if (!goDownOnly && currentTabbingType != KeyboardNavigationMode.Contained && GetParent(container) != null)
            {
                return GetNextTab(container, GetGroupParent(container), false);
            }

            return null;
        }

        public static IInputElement? GetNextTabOutside(ICustomKeyboardNavigation e)
        {
            if (e is IInputElement container)
            {
                var last = GetLastInTree(container);

                if (last is object)
                    return GetNextTab(last, false);
            }

            return null;
        }

        public static IInputElement? GetPrevTab(IInputElement? e, IInputElement? container, bool goDownOnly)
        {
            if (e is null && container is null)
                throw new InvalidOperationException("Either 'e' or 'container' must be non-null.");

            if (container is null)
                container = GetGroupParent(e!);

            KeyboardNavigationMode tabbingType = GetKeyNavigationMode(container);

            if (e == null)
            {
                // Using ActiveElement if set
                var activeElement = GetActiveElement(container);
                if (activeElement != null)
                    return GetPrevTab(null, activeElement, true);
                else
                {
                    // If we Shift+Tab on a container with KeyboardNavigationMode=Once, and ActiveElement is null
                    // then we want to go to the first item (not last) within the container
                    if (tabbingType == KeyboardNavigationMode.Once)
                    {
                        var firstTabElement = GetNextTabInGroup(null, container, tabbingType);
                        if (firstTabElement == null)
                        {
                            if (IsTabStop(container))
                                return container;
                            if (goDownOnly)
                                return null;

                            return GetPrevTab(container, null, false);
                        }
                        else
                        {
                            return GetPrevTab(null, firstTabElement, true);
                        }
                    }
                }
            }
            else
            {
                if (tabbingType == KeyboardNavigationMode.Once || tabbingType == KeyboardNavigationMode.None)
                {
                    if (goDownOnly || container == e)
                        return null;

                    // FocusedElement should not be e otherwise we will delegate focus to the same element
                    if (IsTabStop(container))
                        return container;

                    return GetPrevTab(container, null, false);
                }
            }

            // All groups (except Once) - continue
            IInputElement? loopStartElement = null;
            IInputElement? nextTabElement = e;

            // Look for element with the same TabIndex before the current element
            while ((nextTabElement = GetPrevTabInGroup(nextTabElement, container, tabbingType)) != null)
            {
                // At this point nextTabElement is TabStop or TabGroup
                // In case it is a TabStop only return the element
                if (IsTabStop(nextTabElement) && !IsGroup(nextTabElement))
                    return nextTabElement;

                // Avoid the endless loop here
                if (loopStartElement == nextTabElement)
                    break;
                if (loopStartElement == null)
                    loopStartElement = nextTabElement;

                // At this point nextTabElement is TabGroup
                var lastTabElementInside = GetPrevTab(null, nextTabElement, true);
                if (lastTabElementInside != null)
                    return lastTabElementInside;
            }

            if (tabbingType == KeyboardNavigationMode.Contained)
                return null;

            if (e != container && IsTabStop(container))
                return container;

            // If end of the subtree is reached or there no other elements above
            if (!goDownOnly && GetParent(container) != null)
            {
                return GetPrevTab(container, null, false);
            }

            return null;
        }

        public static IInputElement? GetPrevTabOutside(ICustomKeyboardNavigation e)
        {
            if (e is IInputElement container)
            {
                var first = GetFirstChild(container);

                if (first is object)
                    return GetPrevTab(first, null, false);
            }

            return null;
        }

        private static IInputElement? FocusedElement(IInputElement e)
        {
            var iie = e;
            // Focus delegation is enabled only if keyboard focus is outside the container
            if (iie != null && !iie.IsKeyboardFocusWithin)
            {
                var focusedElement = (FocusManager.Instance as FocusManager)?.GetFocusedElement(e);
                if (focusedElement != null)
                {
                    if (!IsFocusScope(e))
                    {
                        // Verify if focusedElement is a visual descendant of e
                        if (focusedElement is IVisual visualFocusedElement &&
                            visualFocusedElement != e &&
                            e.IsVisualAncestorOf(visualFocusedElement))
                        {
                            return focusedElement;
                        }
                    }
                }
            }

            return null;
        }

        private static IInputElement? GetFirstChild(IInputElement e)
        {
            // If the element has a FocusedElement it should be its first child
            if (FocusedElement(e) is IInputElement focusedElement)
                return focusedElement;

            // Return the first visible element.
            var uiElement = e as InputElement;

            if (uiElement is null || uiElement.IsVisible)
            {
                if (e is IVisual elementAsVisual)
                {
                    var children = elementAsVisual.VisualChildren;
                    var count = children.Count;

                    for (int i = 0; i < count; i++)
                    {
                        if (children[i] is InputElement ie)
                        {
                            if (ie.IsVisible)
                                return ie;
                            else
                            {
                                var firstChild = GetFirstChild(ie);
                                if (firstChild != null)
                                    return firstChild;
                            }
                        }
                    }
                }
            }

            return null;
        }

        private static IInputElement? GetLastChild(IInputElement e)
        {
            // If the element has a FocusedElement it should be its last child
            if (FocusedElement(e) is IInputElement focusedElement)
                return focusedElement;

            // Return the last visible element.
            var uiElement = e as InputElement;

            if (uiElement == null || uiElement.IsVisible)
            {
                var elementAsVisual = e as IVisual;

                if (elementAsVisual != null)
                {
                    var children = elementAsVisual.VisualChildren;
                    var count = children.Count;

                    for (int i = count - 1; i >= 0; i--)
                    {
                        if (children[i] is InputElement ie)
                        {
                            if (ie.IsVisible)
                                return ie;
                            else
                            {
                                var lastChild = GetLastChild(ie);
                                if (lastChild != null)
                                    return lastChild;
                            }
                        }
                    }
                }
            }

            return null;
        }

        private static IInputElement? GetFirstTabInGroup(IInputElement container)
        {
            IInputElement? firstTabElement = null;
            int minIndexFirstTab = int.MinValue;

            var currElement = container;
            while ((currElement = GetNextInTree(currElement, container)) != null)
            {
                if (IsTabStopOrGroup(currElement))
                {
                    int currPriority = KeyboardNavigation.GetTabIndex(currElement);

                    if (currPriority < minIndexFirstTab || firstTabElement == null)
                    {
                        minIndexFirstTab = currPriority;
                        firstTabElement = currElement;
                    }
                }
            }
            return firstTabElement;
        }

        private static IInputElement? GetLastInTree(IInputElement container)
        {
            IInputElement? result;
            IInputElement? c = container;

            do
            {
                result = c;
                c = GetLastChild(c);
            } while (c != null && !IsGroup(c));

            if (c != null)
                return c;

            return result;
        }

        private static IInputElement? GetLastTabInGroup(IInputElement container)
        {
            IInputElement? lastTabElement = null;
            int maxIndexFirstTab = int.MaxValue;
            var currElement = GetLastInTree(container);
            while (currElement != null && currElement != container)
            {
                if (IsTabStopOrGroup(currElement))
                {
                    int currPriority = KeyboardNavigation.GetTabIndex(currElement);

                    if (currPriority > maxIndexFirstTab || lastTabElement == null)
                    {
                        maxIndexFirstTab = currPriority;
                        lastTabElement = currElement;
                    }
                }
                currElement = GetPreviousInTree(currElement, container);
            }
            return lastTabElement;
        }

        private static IInputElement? GetNextInTree(IInputElement e, IInputElement container)
        {
            IInputElement? result = null;

            if (e == container || !IsGroup(e))
                result = GetFirstChild(e);

            if (result != null || e == container)
                return result;

            IInputElement? parent = e;
            do
            {
                var sibling = GetNextSibling(parent);
                if (sibling != null)
                    return sibling;

                parent = GetParent(parent);
            } while (parent != null && parent != container);

            return null;
        }

        private static IInputElement? GetNextSibling(IInputElement e)
        {
            if (GetParent(e) is IVisual parentAsVisual && e is IVisual elementAsVisual)
            {
                var children = parentAsVisual.VisualChildren;
                var count = children.Count;
                var i = 0;

                //go till itself
                for (; i < count; i++)
                {
                    var vchild = children[i];
                    if (vchild == elementAsVisual)
                        break;
                }
                i++;
                //search ahead
                for (; i < count; i++)
                {
                    var visual = children[i];
                    if (visual is IInputElement ie)
                        return ie;
                }
            }

            return null;
        }

        private static IInputElement? GetNextTabInGroup(IInputElement? e, IInputElement container, KeyboardNavigationMode tabbingType)
        {
            // None groups: Tab navigation is not supported
            if (tabbingType == KeyboardNavigationMode.None)
                return null;

            // e == null or e == container -> return the first TabStopOrGroup
            if (e == null || e == container)
            {
                return GetFirstTabInGroup(container);
            }

            if (tabbingType == KeyboardNavigationMode.Once)
                return null;

            var nextTabElement = GetNextTabWithSameIndex(e, container);
            if (nextTabElement != null)
                return nextTabElement;

            return GetNextTabWithNextIndex(e, container, tabbingType);
        }

        private static IInputElement? GetNextTabWithSameIndex(IInputElement e, IInputElement container)
        {
            var elementTabPriority = KeyboardNavigation.GetTabIndex(e);
            var currElement = e;
            while ((currElement = GetNextInTree(currElement, container)) != null)
            {
                if (IsTabStopOrGroup(currElement) && KeyboardNavigation.GetTabIndex(currElement) == elementTabPriority)
                {
                    return currElement;
                }
            }

            return null;
        }

        private static IInputElement? GetNextTabWithNextIndex(IInputElement e, IInputElement container, KeyboardNavigationMode tabbingType)
        {
            // Find the next min index in the tree
            // min (index>currentTabIndex)
            IInputElement? nextTabElement = null;
            IInputElement? firstTabElement = null;
            int minIndexFirstTab = int.MinValue;
            int minIndex = int.MinValue;
            int elementTabPriority = KeyboardNavigation.GetTabIndex(e);

            IInputElement? currElement = container;
            while ((currElement = GetNextInTree(currElement, container)) != null)
            {
                if (IsTabStopOrGroup(currElement))
                {
                    int currPriority = KeyboardNavigation.GetTabIndex(currElement);
                    if (currPriority > elementTabPriority)
                    {
                        if (currPriority < minIndex || nextTabElement == null)
                        {
                            minIndex = currPriority;
                            nextTabElement = currElement;
                        }
                    }

                    if (currPriority < minIndexFirstTab || firstTabElement == null)
                    {
                        minIndexFirstTab = currPriority;
                        firstTabElement = currElement;
                    }
                }
            }

            // Cycle groups: if not found - return first element
            if (tabbingType == KeyboardNavigationMode.Cycle && nextTabElement == null)
                nextTabElement = firstTabElement;

            return nextTabElement;
        }

        private static IInputElement? GetPrevTabInGroup(IInputElement? e, IInputElement container, KeyboardNavigationMode tabbingType)
        {
            // None groups: Tab navigation is not supported
            if (tabbingType == KeyboardNavigationMode.None)
                return null;

            // Search the last index inside the group
            if (e == null)
            {
                return GetLastTabInGroup(container);
            }

            if (tabbingType == KeyboardNavigationMode.Once)
                return null;

            if (e == container)
                return null;

            var nextTabElement = GetPrevTabWithSameIndex(e, container);
            if (nextTabElement != null)
                return nextTabElement;

            return GetPrevTabWithPrevIndex(e, container, tabbingType);
        }

        private static IInputElement? GetPrevTabWithSameIndex(IInputElement e, IInputElement container)
        {
            int elementTabPriority = KeyboardNavigation.GetTabIndex(e);
            var currElement = GetPreviousInTree(e, container);
            while (currElement != null)
            {
                if (IsTabStopOrGroup(currElement) && KeyboardNavigation.GetTabIndex(currElement) == elementTabPriority && currElement != container)
                {
                    return currElement;
                }
                currElement = GetPreviousInTree(currElement, container);
            }
            return null;
        }

        private static IInputElement? GetPrevTabWithPrevIndex(IInputElement e, IInputElement container, KeyboardNavigationMode tabbingType)
        {
            // Find the next max index in the tree
            // max (index<currentTabIndex)
            IInputElement? lastTabElement = null;
            IInputElement? nextTabElement = null;
            int elementTabPriority = KeyboardNavigation.GetTabIndex(e);
            int maxIndexFirstTab = Int32.MaxValue;
            int maxIndex = Int32.MaxValue;
            var currElement = GetLastInTree(container);
            while (currElement != null)
            {
                if (IsTabStopOrGroup(currElement) && currElement != container)
                {
                    int currPriority = KeyboardNavigation.GetTabIndex(currElement);
                    if (currPriority < elementTabPriority)
                    {
                        if (currPriority > maxIndex || nextTabElement == null)
                        {
                            maxIndex = currPriority;
                            nextTabElement = currElement;
                        }
                    }

                    if (currPriority > maxIndexFirstTab || lastTabElement == null)
                    {
                        maxIndexFirstTab = currPriority;
                        lastTabElement = currElement;
                    }
                }

                currElement = GetPreviousInTree(currElement, container);
            }

            // Cycle groups: if not found - return first element
            if (tabbingType == KeyboardNavigationMode.Cycle && nextTabElement == null)
                nextTabElement = lastTabElement;

            return nextTabElement;
        }

        private static IInputElement? GetPreviousInTree(IInputElement e, IInputElement container)
        {
            if (e == container)
                return null;

            var result = GetPreviousSibling(e);

            if (result != null)
            {
                if (IsGroup(result))
                    return result;
                else
                    return GetLastInTree(result);
            }
            else
                return GetParent(e);
        }

        private static IInputElement? GetPreviousSibling(IInputElement e)
        {
            if (GetParent(e) is IVisual parentAsVisual && e is IVisual elementAsVisual)
            {
                var children = parentAsVisual.VisualChildren;
                var count = children.Count;
                IInputElement? prev = null;
                
                for (int i = 0; i < count; i++)
                {
                    var vchild = children[i];
                    if (vchild == elementAsVisual)
                        break;
                    if (vchild.IsVisible == true && vchild is IInputElement ie)
                        prev = ie;
                }
                return prev;
            }
            return null;
        }

        private static IInputElement? GetActiveElement(IInputElement e)
        {
            return ((IAvaloniaObject)e).GetValue(KeyboardNavigation.TabOnceActiveElementProperty);
        }

        private static IInputElement GetGroupParent(IInputElement e) => GetGroupParent(e, false);

        private static IInputElement GetGroupParent(IInputElement element, bool includeCurrent)
        {
            var result = element; // Keep the last non null element
            var e = element;

            // If we don't want to include the current element,
            // start at the parent of the element.  If the element
            // is the root, then just return it as the group parent.
            if (!includeCurrent)
            {
                result = e;
                e = GetParent(e);
                if (e == null)
                    return result;
            }

            while (e != null)
            {
                if (IsGroup(e))
                    return e;

                result = e;
                e = GetParent(e);
            }

            return result;
        }

        private static IInputElement? GetParent(IInputElement e)
        {
            // For Visual - go up the visual parent chain until we find Visual.
            if (e is IVisual v)
                return v.FindAncestorOfType<IInputElement>();

            // This will need to be implemented when we have non-visual input elements.
            throw new NotSupportedException();
        }

        private static KeyboardNavigationMode GetKeyNavigationMode(IInputElement e)
        {
            return ((IAvaloniaObject)e).GetValue(KeyboardNavigation.TabNavigationProperty);
        }
        private static bool IsFocusScope(IInputElement e) => FocusManager.GetIsFocusScope(e) || GetParent(e) == null;
        private static bool IsGroup(IInputElement e) => GetKeyNavigationMode(e) != KeyboardNavigationMode.Continue;

        private static bool IsTabStop(IInputElement e)
        {
            if (e is InputElement ie)
                return ie.Focusable && KeyboardNavigation.GetIsTabStop(ie) && ie.IsVisible && ie.IsEnabled;
            return false;
        }

        private static bool IsTabStopOrGroup(IInputElement e) => IsTabStop(e) || IsGroup(e);
    }
}
