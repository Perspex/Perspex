using Avalonia.Controls.Templates;

namespace Avalonia.Controls
{
    /// <summary>
    /// Represents the optional arguments to use when calling an implementation of the
    /// <see cref="IElementFactory"/>'s <see cref="IElementFactory.GetElement"/> method.
    /// </summary>
    public class ElementFactoryGetArgs
    {
        /// <summary>
        /// Gets or sets the data item for which an appropriate element tree should be realized
        /// when calling <see cref="IElementFactory.GetElement"/>.
        /// </summary>
        public object Data { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="IControl"/> that is expected to be the parent of the
        /// realized element from <see cref="IElementFactory.GetElement"/>.
        /// </summary>
        public IControl Parent { get; set; }

        /// <summary>
        /// Gets or sets the index of the item that should be realized.
        /// </summary>
        public int Index { get; set; }
    }

    /// <summary>
    /// Represents the optional arguments to use when calling an implementation of the
    /// <see cref="IElementFactory"/>'s <see cref="IElementFactory.GetElement"/> method.
    /// </summary>
    public class ElementFactoryRecycleArgs
    {
        /// <summary>
        /// Gets or sets the <see cref="IControl"/> to recycle when calling 
        /// <see cref="IElementFactory.RecycleElement"/>.
        /// </summary>
        public IControl Element { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="IControl"/> that is expected to be the parent of the
        /// realized element from <see cref="IElementFactory.GetElement"/>.
        /// </summary>
        public IControl Parent { get; set; }
    }

    /// <summary>
    /// A data template that supports creating and recycling elements for an <see cref="ItemsRepeater"/>.
    /// </summary>
    public interface IElementFactory : IDataTemplate
    {
        /// <summary>
        /// Gets an <see cref="IControl"/>.
        /// </summary>
        /// <param name="args">The element args.</param>
        public IControl GetElement(ElementFactoryGetArgs args);

        /// <summary>
        /// Recycles an <see cref="IControl"/> that was previously retrieved using
        /// <see cref="GetElement"/>.
        /// </summary>
        /// <param name="args">The recycle args.</param>
        public void RecycleElement(ElementFactoryRecycleArgs args);
    }
}
