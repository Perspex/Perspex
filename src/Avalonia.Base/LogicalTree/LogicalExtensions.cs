using System;
using System.Collections.Generic;

namespace Avalonia.LogicalTree
{
    /// <summary>
    /// Provides extension methods for working with the logical tree.
    /// </summary>
    public static class LogicalExtensions
    {
        /// <summary>
        /// Enumerates the ancestors of an <see cref="ILogical"/> in the logical tree.
        /// </summary>
        /// <param name="logical">The logical.</param>
        /// <returns>The logical's ancestors.</returns>
        public static IEnumerable<ILogical> GetLogicalAncestors(this ILogical logical)
        {
            Contract.Requires<ArgumentNullException>(logical != null);

            logical = logical.LogicalParent;

            while (logical != null)
            {
                yield return logical;
                logical = logical.LogicalParent;
            }
        }

        /// <summary>
        /// Enumerates an <see cref="ILogical"/> and its ancestors in the logical tree.
        /// </summary>
        /// <param name="logical">The logical.</param>
        /// <returns>The logical and its ancestors.</returns>
        public static IEnumerable<ILogical> GetSelfAndLogicalAncestors(this ILogical logical)
        {
            yield return logical;

            foreach (var ancestor in logical.GetLogicalAncestors())
            {
                yield return ancestor;
            }
        }

        /// <summary>
        /// Finds first ancestor of given type.
        /// </summary>
        /// <typeparam name="T">Ancestor type.</typeparam>
        /// <param name="logical">The logical.</param>
        /// <param name="includeSelf">If given logical should be included in search.</param>
        /// <returns>First ancestor of given type.</returns>
        public static T FindLogicalAncestorOfType<T>(this ILogical logical, bool includeSelf = false) where T : class
        {
            if (logical is null)
            {
                return null;
            }

            ILogical parent = includeSelf ? logical : logical.LogicalParent;

            while (parent != null)
            {
                if (parent is T result)
                {
                    return result;
                }

                parent = parent.LogicalParent;
            }

            return null;
        }

        /// <summary>
        /// Enumerates the children of an <see cref="ILogical"/> in the logical tree.
        /// </summary>
        /// <param name="logical">The logical.</param>
        /// <returns>The logical children.</returns>
        public static IEnumerable<ILogical> GetLogicalChildren(this ILogical logical)
        {
            return logical.LogicalChildren;
        }

        /// <summary>
        /// Enumerates the descendants of an <see cref="ILogical"/> in the logical tree.
        /// </summary>
        /// <param name="logical">The logical.</param>
        /// <returns>The logical's ancestors.</returns>
        public static IEnumerable<ILogical> GetLogicalDescendants(this ILogical logical)
        {
            foreach (ILogical child in logical.LogicalChildren)
            {
                yield return child;

                foreach (ILogical descendant in child.GetLogicalDescendants())
                {
                    yield return descendant;
                }
            }
        }

        /// <summary>
        /// Enumerates an <see cref="ILogical"/> and its descendants in the logical tree.
        /// </summary>
        /// <param name="logical">The logical.</param>
        /// <returns>The logical and its ancestors.</returns>
        public static IEnumerable<ILogical> GetSelfAndLogicalDescendants(this ILogical logical)
        {
            yield return logical;

            foreach (var descendent in logical.GetLogicalDescendants())
            {
                yield return descendent;
            }
        }

        /// <summary>
        /// Finds first descendant of given type.
        /// </summary>
        /// <typeparam name="T">Descendant type.</typeparam>
        /// <param name="logical">The logical.</param>
        /// <param name="includeSelf">If given logical should be included in search.</param>
        /// <returns>First descendant of given type.</returns>
        public static T FindLogicalDescendantOfType<T>(this ILogical logical, bool includeSelf = false) where T : class
        {
            if (logical is null)
            {
                return null;
            }

            if (includeSelf && logical is T result)
            {
                return result;
            }

            return FindDescendantOfTypeCore<T>(logical);
        }

        /// <summary>
        /// Gets the logical parent of an <see cref="ILogical"/>.
        /// </summary>
        /// <param name="logical">The logical.</param>
        /// <returns>The parent, or null if the logical is unparented.</returns>
        public static ILogical GetLogicalParent(this ILogical logical)
        {
            return logical.LogicalParent;
        }

        /// <summary>
        /// Gets the logical parent of an <see cref="ILogical"/>.
        /// </summary>
        /// <typeparam name="T">The type of the logical parent.</typeparam>
        /// <param name="logical">The logical.</param>
        /// <returns>
        /// The parent, or null if the logical is unparented or its parent is not of type <typeparamref name="T"/>.
        /// </returns>
        public static T GetLogicalParent<T>(this ILogical logical) where T : class
        {
            return logical.LogicalParent as T;
        }

        /// <summary>
        /// Enumerates the siblings of an <see cref="ILogical"/> in the logical tree.
        /// </summary>
        /// <param name="logical">The logical.</param>
        /// <returns>The logical siblings.</returns>
        public static IEnumerable<ILogical> GetLogicalSiblings(this ILogical logical)
        {
            ILogical parent = logical.LogicalParent;

            if (parent != null)
            {
                foreach (ILogical sibling in parent.LogicalChildren)
                {
                    yield return sibling;
                }
            }
        }

        /// <summary>
        /// Tests whether an <see cref="ILogical"/> is an ancestor of another logical.
        /// </summary>
        /// <param name="logical">The logical.</param>
        /// <param name="target">The potential descendant.</param>
        /// <returns>
        /// True if <paramref name="logical"/> is an ancestor of <paramref name="target"/>;
        /// otherwise false.
        /// </returns>
        public static bool IsLogicalAncestorOf(this ILogical logical, ILogical target)
        {
            ILogical current = target?.LogicalParent;

            while (current != null)
            {
                if (current == logical)
                {
                    return true;
                }

                current = current.LogicalParent;
            }

            return false;
        }

        private static T FindDescendantOfTypeCore<T>(ILogical logical) where T : class
        {
            var logicalChildren = logical.LogicalChildren;
            var logicalChildrenCount = logicalChildren.Count;

            for (var i = 0; i < logicalChildrenCount; i++)
            {
                ILogical child = logicalChildren[i];

                if (child is T result)
                {
                    return result;
                }

                var childResult = FindDescendantOfTypeCore<T>(child);

                if (!(childResult is null))
                {
                    return childResult;
                }
            }

            return null;
        }
    }
}
