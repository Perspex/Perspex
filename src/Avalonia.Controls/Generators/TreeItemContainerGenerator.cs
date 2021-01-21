using System;
using System.Collections;
using System.Linq;
using System.Reactive.Linq;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.LogicalTree;

#nullable enable

namespace Avalonia.Controls.Generators
{
    public class TreeItemContainerGenerator<T> : ItemContainerGenerator, ITreeItemContainerGenerator
        where T : class, IControl, new()
    {
        private TreeView? _treeView;

        public TreeItemContainerGenerator(
            ItemsControl owner,
            AvaloniaProperty<object?> headerProperty,
            AvaloniaProperty<IDataTemplate?> headerTemplateProperty,
            AvaloniaProperty<IEnumerable?> itemsProperty,
            AvaloniaProperty<bool> isExpandedProperty)
            : base(owner)
        {
            HeaderProperty = headerProperty ?? throw new ArgumentNullException(nameof(headerProperty));
            HeaderTemplateProperty = headerTemplateProperty ?? throw new ArgumentNullException(nameof(headerTemplateProperty));
            ItemsProperty = itemsProperty ?? throw new ArgumentNullException(nameof(itemsProperty));
            IsExpandedProperty = isExpandedProperty;
            UpdateIndex();
        }

        /// <summary>
        /// Gets the container index for the tree.
        /// </summary>
        public TreeContainerIndex? Index { get; private set; }

        /// <summary>
        /// Gets the container's Header property.
        /// </summary>
        protected AvaloniaProperty<object?> HeaderProperty { get; }

        /// <summary>
        /// Gets the container's HeaderTemplate property.
        /// </summary>
        protected AvaloniaProperty<IDataTemplate?> HeaderTemplateProperty { get; }

        /// <summary>
        /// Gets the item container's Items property.
        /// </summary>
        protected AvaloniaProperty<IEnumerable?> ItemsProperty { get; }

        /// <summary>
        /// Gets the item container's IsExpanded property.
        /// </summary>
        protected AvaloniaProperty<bool> IsExpandedProperty { get; }

        public void UpdateIndex()
        {
            if (Owner is TreeView treeViewOwner && Index == null)
            {
                Index = new TreeContainerIndex();
                _treeView = treeViewOwner;
            }
            else
            {
                var treeView = Owner.GetSelfAndLogicalAncestors().OfType<TreeView>().FirstOrDefault();

                if (treeView != _treeView)
                {
                    Index = treeView?.ItemContainerGenerator?.Index;
                    _treeView = treeView;
                }
            }
        }

        protected override IControl CreateContainer(ElementFactoryGetArgs args)
        {
            if (args.Data is T c)
            {
                return c;
            }

            var result = new T();
            var dataCapture = args.Data;
            var itemsSelector = result.GetObservable(Control.DataContextProperty)
                .Select(x =>
                {
                    var template = GetTreeDataTemplate(x, Owner.ItemTemplate);
                    var itemsSelector = template.ItemsSelector(dataCapture);
                    return itemsSelector?.Observable ??
                        Observable.Never<object?>().StartWith(itemsSelector?.Value);
                })
                .Switch();

            result.Bind(
                HeaderProperty,
                result.GetBindingObservable(Control.DataContextProperty),
                BindingPriority.Style);
            result.Bind(
                HeaderTemplateProperty,
                Owner.GetBindingObservable(ItemsControl.ItemTemplateProperty),
                BindingPriority.Style);

            if (itemsSelector != null)
            {
                result.Bind(ItemsProperty, itemsSelector);
            }

            return result;
        }

        protected override bool DataIsContainer(object data) => data is T;

        private ITreeDataTemplate GetTreeDataTemplate(object? item, IDataTemplate? primary)
        {
            var template = Owner.FindDataTemplate(item, primary) ?? FuncDataTemplate.Default;
            var treeTemplate = template as ITreeDataTemplate ?? new WrapperTreeDataTemplate(template);
            return treeTemplate;
        }

        class WrapperTreeDataTemplate : ITreeDataTemplate
        {
            private readonly IDataTemplate _inner;
            public WrapperTreeDataTemplate(IDataTemplate inner) => _inner = inner;
            public IControl Build(object param) => _inner.Build(param);
            public bool Match(object data) => _inner.Match(data);
            public InstancedBinding? ItemsSelector(object item) => null;
        }
    }
}
