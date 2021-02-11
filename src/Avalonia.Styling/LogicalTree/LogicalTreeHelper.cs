using System;

namespace Avalonia.LogicalTree
{
    public static class LogicalTreeHelper
    {
        public static void AddLogicalChild(IAvaloniaObject parent, IAvaloniaObject child)
        {
            if (parent is ILogicalParent logicalParent)
            {
                if (child is ILogical logical)
                {
                    logicalParent.AddChild(logical);
                }
            }
        }

        public static void RemoveLogicalChild(IAvaloniaObject parent, IAvaloniaObject child)
        {
            if (parent is ILogicalParent logicalParent)
            {
                if (child is ILogical logical)
                {
                    logicalParent.RemoveChild(logical);
                }
            }
        }

        /// <summary>
        /// Get the logical parent of the given DependencyObject.
        /// The given DependencyObject must be either a FrameworkElement or FrameworkContentElement
        /// to have a logical parent.
        /// </summary>
        public static IAvaloniaObject GetParent(IAvaloniaObject current)
        {
            if (current == null)
            {
                throw new ArgumentNullException(nameof(current));
            }

            // TODO: This is most likely not correct
            if (current is ILogical fe)
            {
                return fe.LogicalParent as IAvaloniaObject;
            }

            return null;
        }

    }
}
