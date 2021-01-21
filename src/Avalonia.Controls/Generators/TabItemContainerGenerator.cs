using Avalonia.Controls.Primitives;

namespace Avalonia.Controls.Generators
{    
    public class TabItemContainerGenerator : ItemContainerGenerator<TabItem>
    {
        public TabItemContainerGenerator(TabControl owner)
            : base(owner, ContentControl.ContentProperty, ContentControl.ContentTemplateProperty)
        {
            Owner = owner;
        }

        public new TabControl Owner { get; }

        protected override IControl CreateContainer(ElementFactoryGetArgs args)
        {
            var tabItem = (TabItem)base.CreateContainer(args);

            tabItem[~TabControl.TabStripPlacementProperty] = Owner[~TabControl.TabStripPlacementProperty];

            if (tabItem.HeaderTemplate == null)
            {
                tabItem[~HeaderedContentControl.HeaderTemplateProperty] = Owner[~ItemsControl.ItemTemplateProperty];
            }

            if (tabItem.Header == null)
            {
                if (args.Data is IHeadered headered)
                {
                    tabItem.Header = headered.Header;
                }
                else
                {
                    if (!(tabItem.DataContext is IControl))
                    {
                        tabItem.Header = tabItem.DataContext;
                    }
                }
            }

            if (!(tabItem.Content is IControl))
            {
                tabItem[~ContentControl.ContentTemplateProperty] = Owner[~TabControl.ContentTemplateProperty];
            }

            return tabItem;
        }
    }
}
