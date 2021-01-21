using System.Linq;
using Avalonia.Controls.Generators;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Mixins;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Selection;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.LogicalTree;

namespace Avalonia.Controls
{
    /// <summary>
    /// An item in a <see cref="TreeView"/>.
    /// </summary>
    [PseudoClasses(":pressed", ":selected")]
    public class TreeViewItem : HeaderedItemsControl, ISelectable
    {
        /// <summary>
        /// Defines the <see cref="IsExpanded"/> property.
        /// </summary>
        public static readonly DirectProperty<TreeViewItem, bool> IsExpandedProperty =
            AvaloniaProperty.RegisterDirect<TreeViewItem, bool>(
                nameof(IsExpanded),
                o => o.IsExpanded,
                (o, v) => o.IsExpanded = v);

        /// <summary>
        /// Defines the <see cref="IsSelected"/> property.
        /// </summary>
        public static readonly StyledProperty<bool> IsSelectedProperty =
            ListBoxItem.IsSelectedProperty.AddOwner<TreeViewItem>();

        /// <summary>
        /// Defines the <see cref="Level"/> property.
        /// </summary>
        public static readonly DirectProperty<TreeViewItem, int> LevelProperty =
            AvaloniaProperty.RegisterDirect<TreeViewItem, int>(
                nameof(Level), o => o.Level);

        private IControl _header;
        private bool _isExpanded;
        private int _level;

        /// <summary>
        /// Initializes static members of the <see cref="TreeViewItem"/> class.
        /// </summary>
        static TreeViewItem()
        {
            SelectableMixin.Attach<TreeViewItem>(IsSelectedProperty);
            PressedMixin.Attach<TreeViewItem>();
            FocusableProperty.OverrideDefaultValue<TreeViewItem>(true);
            LayoutProperty.OverrideDefaultValue<TreeViewItem>(new NonVirtualizingStackLayout
            {
                Orientation = Orientation.Vertical,
            });
            ParentProperty.Changed.AddClassHandler<TreeViewItem>((o, e) => o.OnParentChanged(e));
            RequestBringIntoViewEvent.AddClassHandler<TreeViewItem>((x, e) => x.OnRequestBringIntoView(e));
        }

        /// <summary>
        /// Gets or sets a value indicating whether the item is expanded to show its children.
        /// </summary>
        public bool IsExpanded
        {
            get { return _isExpanded; }
            set { SetAndRaise(IsExpandedProperty, ref _isExpanded, value); }
        }

        /// <summary>
        /// Gets or sets the selection state of the item.
        /// </summary>
        public bool IsSelected
        {
            get { return GetValue(IsSelectedProperty); }
            set { SetValue(IsSelectedProperty, value); }
        }

        /// <summary>
        /// Gets the level/indentation of the item.
        /// </summary>
        public int Level
        {
            get { return _level; }
            private set { SetAndRaise(LevelProperty, ref _level, value); }
        }

        /// <summary>
        /// Gets the <see cref="ITreeItemContainerGenerator"/> for the tree view.
        /// </summary>
        public new ITreeItemContainerGenerator ItemContainerGenerator =>
            (ITreeItemContainerGenerator)base.ItemContainerGenerator;

        /// <summary>
        /// Gets the tree view that the item is a part of.
        /// </summary>
        internal TreeView TreeView { get; private set; }

        internal IndexPath IndexPath { get; set; }

        protected override IItemContainerGenerator CreateItemContainerGenerator()
        {
            return new TreeItemContainerGenerator<TreeViewItem>(
                this,
                TreeViewItem.HeaderProperty,
                TreeViewItem.HeaderTemplateProperty,
                TreeViewItem.ItemsProperty,
                TreeViewItem.IsExpandedProperty);
        }

        protected override void OnContainerPrepared(ElementPreparedEventArgs e)
        {
            base.OnContainerPrepared(e);

            if (e.Element is TreeViewItem item)
            {
                item.IndexPath = IndexPath.CloneWithChildIndex(e.Index);
            }

            ItemContainerGenerator.Index.Add(ItemsView[e.Index], e.Element);
            TreeView?.RaiseTreeContainerPrepared(IndexPath, e);
        }

        protected override void OnContainerClearing(ElementClearingEventArgs e)
        {
            base.OnContainerClearing(e);

            ItemContainerGenerator.Index?.Remove(e.Element);
            TreeView?.RaiseTreeContainerClearing(e);

            if (e.Element is TreeViewItem item)
            {
                item.IndexPath = default;
            }
        }

        protected override void OnContainerIndexChanged(ElementIndexChangedEventArgs e)
        {
            base.OnContainerIndexChanged(e);

            if (e.Element is TreeViewItem item)
            {
                item.IndexPath = IndexPath.CloneWithChildIndex(e.NewIndex);
            }

            TreeView?.RaiseTreeContainerIndexChanged(IndexPath, e);
        }

        /// <inheritdoc/>
        protected override void OnAttachedToLogicalTree(LogicalTreeAttachmentEventArgs e)
        {
            base.OnAttachedToLogicalTree(e);

            (Level, TreeView) = FindOwner();
            ItemContainerGenerator.UpdateIndex();

            if (ItemTemplate == null && TreeView?.ItemTemplate != null)
            {
                ItemTemplate = TreeView.ItemTemplate;
            }
        }

        protected override void OnDetachedFromLogicalTree(LogicalTreeAttachmentEventArgs e)
        {
            base.OnDetachedFromLogicalTree(e);

            UpdateIndex();

            var (_, owner) = FindOwner();

            if (TreeView is object && owner is null)
            {
                TreeView = null;
            }
        }

        protected virtual void OnRequestBringIntoView(RequestBringIntoViewEventArgs e)
        {
            if (e.TargetObject == this && _header != null)
            {
                var m = _header.TransformToVisual(this);

                if (m.HasValue)
                {
                    var bounds = new Rect(_header.Bounds.Size);
                    var rect = bounds.TransformToAABB(m.Value);
                    e.TargetRect = rect;
                }
            }
        }

        /// <inheritdoc/>
        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (!e.Handled)
            {
                switch (e.Key)
                {
                    case Key.Right:
                        if (Items != null && Items.Cast<object>().Any())
                        {
                            IsExpanded = true;
                        }

                        e.Handled = true;
                        break;

                    case Key.Left:
                        IsExpanded = false;
                        e.Handled = true;
                        break;
                }
            }

            // Don't call base.OnKeyDown - let events bubble up to containing TreeView.
        }

        protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
        {
            _header = e.NameScope.Find<IControl>("PART_Header");
        }

        private (int distance, TreeView owner) FindOwner()
        {
            var c = Parent;
            var i = 0;

            while (c != null)
            {
                if (c is TreeView treeView)
                {
                    return (i, treeView);
                }

                c = c.Parent;
                ++i;
            }

            return (-1, null);
        }

        private void OnParentChanged(AvaloniaPropertyChangedEventArgs e)
        {
            if (!((ILogical)this).IsAttachedToLogicalTree && e.NewValue is null)
            {
                var oldIndex = ItemContainerGenerator.Index;

                // If we're not attached to the logical tree, then OnDetachedFromLogicalTree isn't going to be
                // called when the item is removed. This results in the item not being removed from the index,
                // causing #3551. In this case, update the index when Parent is changed to null.
                UpdateIndex();
                TreeView = null;
            }
        }

        private void UpdateIndex()
        {
            var index = ItemContainerGenerator.Index;
            
            ItemContainerGenerator.UpdateIndex();

            if (ItemContainerGenerator.Index != index)
            {
                foreach (var c in Presenter.RealizedElements)
                {
                    index.Remove(c);
                }
            }
        }
    }
}
