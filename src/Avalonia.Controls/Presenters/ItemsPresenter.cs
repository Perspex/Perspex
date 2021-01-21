using System.Collections.Generic;
using System.Linq;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.VisualTree;

#nullable enable

namespace Avalonia.Controls.Presenters
{
    /// <summary>
    /// Displays items in a <see cref="ItemsControl"/>.
    /// </summary>
    public class ItemsPresenter : ItemsRepeater, IItemsPresenter
    {
        public static readonly DirectProperty<ItemsPresenter, ItemsSourceView?> ItemsViewProperty =
            AvaloniaProperty.RegisterDirect<ItemsPresenter, ItemsSourceView?>(
                nameof(ItemsView),
                o => o.ItemsView,
                (o, v) => o.ItemsView = v);

        private IItemsPresenterHost? _host;

        public ItemsSourceView? ItemsView
        {
            get => (ItemsSourceView?)Items;
            set => Items = value;
        }

        public IEnumerable<IControl> RealizedElements
        {
            get
            {
                foreach (var child in Children)
                {
                    var virtInfo = GetVirtualizationInfo(child);

                    if (virtInfo?.IsRealized == true)
                    {
                        yield return child;
                    }
                }
            }
        }

        public bool ScrollIntoView(int index)
        {
            var layoutManager = (VisualRoot as ILayoutRoot)?.LayoutManager;

            if (index >= 0 && index < ItemsSourceView.Count && layoutManager != null)
            {
                var element = GetOrCreateElement(index);

                if (element != null)
                {
                    layoutManager.ExecuteLayoutPass();
                    element.BringIntoView();
                    return true;
                }
            }

            return false;
        }

        protected override void OnGotFocus(GotFocusEventArgs e)
        {
            base.OnGotFocus(e);

            var child = ((IVisual?)e.Source)?.GetSelfAndVisualAncestors()
                .FirstOrDefault(x => x.VisualParent == this);

            if (child != null)
            {
                KeyboardNavigation.SetTabOnceActiveElement(this, (IInputElement)child);
            }
        }

        protected override void OnPropertyChanged<T>(AvaloniaPropertyChangedEventArgs<T> change)
        {
            if (change.Property == TemplatedParentProperty)
            {
                _host = change.NewValue.GetValueOrDefault<IItemsPresenterHost>();

                if (_host is object)
                {
                    _host.RegisterItemsPresenter(this);
                    ItemTemplate = _host.ElementFactory;
                }
            }

            base.OnPropertyChanged(change);
        }
    }
}
