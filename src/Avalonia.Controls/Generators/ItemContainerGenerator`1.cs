using System;
using Avalonia.Controls.Templates;
using Avalonia.Data;

#nullable enable

namespace Avalonia.Controls.Generators
{
    /// <summary>
    /// Creates containers for items and maintains a list of created containers.
    /// </summary>
    /// <typeparam name="T">The type of the container.</typeparam>
    public class ItemContainerGenerator<T> : ItemContainerGenerator where T : class, IControl, new()
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ItemContainerGenerator{T}"/> class.
        /// </summary>
        /// <param name="owner">The owner control.</param>
        /// <param name="contentProperty">The container's Content property.</param>
        /// <param name="contentTemplateProperty">The container's ContentTemplate property.</param>
        public ItemContainerGenerator(
            ItemsControl owner,
            AvaloniaProperty<object?> contentProperty,
            AvaloniaProperty<IDataTemplate?> contentTemplateProperty)
            : base(owner)
        {
            ContentProperty = contentProperty ?? throw new ArgumentNullException(nameof(contentProperty));
            ContentTemplateProperty = contentTemplateProperty ??
                throw new ArgumentNullException(nameof(contentTemplateProperty));
        }

        /// <summary>
        /// Gets the container's Content property.
        /// </summary>
        protected AvaloniaProperty<object?> ContentProperty { get; }

        /// <summary>
        /// Gets the container's ContentTemplate property.
        /// </summary>
        protected AvaloniaProperty<IDataTemplate?> ContentTemplateProperty { get; }

        protected override IControl CreateContainer(ElementFactoryGetArgs args)
        {
            if (args.Data is T t)
            {
                return t;
            }

            var result = new T();

            result.Bind(
                ContentProperty,
                result.GetBindingObservable(Control.DataContextProperty),
                BindingPriority.Style);
            result.Bind(
                ContentTemplateProperty,
                Owner.GetBindingObservable(ItemsControl.ItemTemplateProperty),
                BindingPriority.Style);

            return result;
        }

        protected override bool DataIsContainer(object data) => data is T;
    }
}
