using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.VisualTree;

#nullable enable

namespace Avalonia.Input
{
    /// <summary>
    /// Defines extensions for the <see cref="IInputElement"/> interface.
    /// </summary>
    public static class InputExtensions
    {

        private static readonly Func<IVisual, bool> s_hitTestDelegate = v => v is IInputElement
        {
            IsHitTestVisible: true, IsEffectivelyEnabled: true
        };

        public static IVisual? GetClosestVisual(this IInputElement? element)
        {
            var e = element;
            while (e != null)
            {
                if (e is IVisual visual)
                {
                    return visual;
                }

                e = e.InputParent;
            }

            return null;
        }

        /// <summary>
        /// Returns the topmost active input element at a point on an <see cref="IInputElement"/>.
        /// </summary>
        /// <param name="element">The element to hit test on.</param>
        /// <param name="p">The point on element.</param>
        /// <returns>The topmost <see cref="IInputElement"/> at the specified position.</returns>
        public static IInputElement? InputHitTest(this IInputElement element, Point p)
        {
            var inputElement = element.GetClosestVisual()?.GetVisualAt(p, s_hitTestDelegate) as IInputElement;
            // The visual under the cursor might be a container for further input elements
            // that are themselves not visuals
            if (inputElement is IContentInputHost contentHost)
            {
                return contentHost.InputHitTest(p);
            }
            return inputElement;
        }

        /// <summary>
        /// Returns the topmost active input element at a point on an <see cref="IInputElement"/>,
        /// but only considers input elements that satisfy the given filter.
        /// </summary>
        /// <param name="element">The element to hit test on.</param>
        /// <param name="p">The point on element.</param>
        /// <param name="filter">Only input elements satisfying this filter will be considered,
        /// including any parent nodes</param>
        /// <returns>The topmost <see cref="IInputElement"/> at the specified position.</returns>
        public static IInputElement? InputHitTest(this IInputElement element, Point p, Func<IInputElement, bool> filter)
        {
            var inputElement = element.GetClosestVisual()?.GetVisualAt(p, x =>
            {
                return s_hitTestDelegate(x) && x is IInputElement e && filter(e);
            }) as IInputElement;

            // The visual under the cursor might be a container for further input elements
            // that are themselves not visuals, hence this recursive call
            if (inputElement is IContentInputHost contentHost)
            {
                return contentHost.InputHitTest(p);
            }
            return inputElement;
        }

        /// <summary>
        /// Enumerates an <see cref="IInputElement"/> and its ancestors in the input tree.
        /// </summary>
        /// <param name="element">The input element.</param>
        /// <returns>The element and its ancestors.</returns>
        public static IEnumerable<IInputElement> GetSelfAndInputAncestors(this IInputElement element)
        {
            element = element ?? throw new ArgumentNullException(nameof(element));

            yield return element;

            foreach (var ancestor in element.GetInputAncestors())
            {
                yield return ancestor;
            }
        }

        /// <summary>
        /// Enumerates an <see cref="IInputElement"/>'s ancestors in the input tree.
        /// </summary>
        /// <param name="element">The input element.</param>
        /// <returns>The element's ancestors.</returns>
        public static IEnumerable<IInputElement> GetInputAncestors(this IInputElement? element)
        {
            element = element ?? throw new ArgumentNullException(nameof(element));

            var parent = element.InputParent;
            while (parent != null)
            {
                yield return parent;
                parent = parent.InputParent;
            }
        }

        /// <summary>
        /// Tests whether an <see cref="IInputElement"/> is an ancestor of another input element.
        /// </summary>
        /// <param name="element">The input element.</param>
        /// <param name="target">The potential descendant.</param>
        /// <returns>
        /// True if <paramref name="element"/> is an ancestor of <paramref name="target"/>;
        /// otherwise false.
        /// </returns>
        public static bool IsInputAncestorOf(this IInputElement element, IInputElement? target)
        {
            var current = target?.InputParent;

            while (current != null)
            {
                if (current == element)
                {
                    return true;
                }

                current = current.InputParent;
            }

            return false;
        }

        public static bool IsClosestVisualVisible(this IInputElement element)
        {
            return element.GetClosestVisual()?.IsVisible == true;
        }

        public static bool IsClosestVisualEffectivelyVisible(this IInputElement element)
        {
            return element.GetClosestVisual()?.IsEffectivelyVisible == true;
        }

        /// <summary>
        /// Checks if the closest visual control of the input element is attached to the visual tree.
        /// </summary>
        public static bool IsAttachedToInputTree(this IInputElement element)
        {
            return element.GetClosestVisual()?.IsAttachedToVisualTree == true;
        }

    }
}
