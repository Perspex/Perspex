using System;

#nullable enable

namespace Avalonia.Controls.Generators
{
    /// <summary>
    /// Creates containers for items and maintains a list of created containers.
    /// </summary>
    public interface IItemContainerGenerator : IElementFactory
    {
        /// <summary>
        /// Gets the <see cref="ItemsControl"/> that the generator belongs to.
        /// </summary>
        ItemsControl Owner { get; }
    }

    public static class ItemContainerGeneratorExtensions
    {
        /// <summary>
        /// Gets the container control representing the item with the specified index.
        /// </summary>
        /// <param name="generator">The generator.</param>
        /// <param name="index">The index.</param>
        /// <returns>The container, or null if no container created.</returns>
        [Obsolete("Use ItemsControl.TryGetContainer")]
        public static IControl? ContainerFromIndex(this IItemContainerGenerator generator, int index)
        {
            return generator.Owner.TryGetContainer(index);
        }

        /// <summary>
        /// Gets the index of the specified container control.
        /// </summary>
        /// <param name="generator">The generator.</param>
        /// <param name="container">The container.</param>
        /// <returns>The index of the container, or -1 if not found.</returns>
        [Obsolete("Use ItemsControl.GetContainerIndex")]
        public static int IndexFromContainer(this IItemContainerGenerator generator, IControl container)
        {
            return generator.Owner.GetContainerIndex(container);
        }
    }
}
