using System;
using Avalonia.Controls.Presenters;
using Avalonia.Data;

#nullable enable

namespace Avalonia.Controls.Generators
{
    /// <summary>
    /// Creates containers for items in an <see cref="ItemsControl"/>.
    /// </summary>
    /// <remarks>
    /// As implemented in <see cref="ItemContainerGenerator"/>, creates a
    /// <see cref="ContentPresenter"/> as a container. To create a different type of container use
    /// <see cref="ItemContainerGenerator{T}"/>.
    /// </remarks>
    public class ItemContainerGenerator : IItemContainerGenerator
    {
        private static readonly AttachedProperty<bool> PreventRecycleProperty =
            AvaloniaProperty.RegisterAttached<ItemContainerGenerator, Control, bool>("PreventRecycle");

        private RecyclePool _recyclePool = new RecyclePool();

        public ItemContainerGenerator(ItemsControl owner)
        {
            Owner = owner ?? throw new ArgumentNullException(nameof(owner));
        }

        public ItemsControl Owner { get; }

        public bool Match(object data) => true;

        public IControl Build(object param)
        {
            return GetElement(new ElementFactoryGetArgs { Data = param });
        }

        public IControl GetElement(ElementFactoryGetArgs args)
        {
            if (DataIsContainer(args.Data))
            {
                var result = (Control)args.Data;
                result.SetValue(PreventRecycleProperty, true);
                return result;
            }
            else
            {
                var result = _recyclePool.TryGetElement(string.Empty, args.Parent);

                if (result is null)
                {
                    result = CreateContainer(args);

                    if (result.Parent == null)
                    {
                        ((ISetLogicalParent)result).SetParent(Owner);
                    }
                }

                return result;
            }
        }

        public void RecycleElement(ElementFactoryRecycleArgs args)
        {
            if (!args.Element.GetValue(PreventRecycleProperty))
            {
                _recyclePool.PutElement(args.Element, string.Empty, args.Parent);
            }
        }

        protected virtual IControl CreateContainer(ElementFactoryGetArgs args)
        {
            var result = new ContentPresenter();
            result.Bind(
                ContentPresenter.ContentProperty,
                result.GetBindingObservable(Control.DataContextProperty),
                BindingPriority.Style);

            if (Owner.ItemTemplate is object)
            {
                result.SetValue(
                    ContentPresenter.ContentTemplateProperty,
                    Owner.ItemTemplate,
                    BindingPriority.TemplatedParent);
            }

            return result;
        }

        protected virtual bool DataIsContainer(object data) => data is Control;
    }
}
