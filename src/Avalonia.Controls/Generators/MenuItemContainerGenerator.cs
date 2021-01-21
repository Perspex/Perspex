namespace Avalonia.Controls.Generators
{
    public class MenuItemContainerGenerator : ItemContainerGenerator<MenuItem>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ItemContainerGenerator{T}"/> class.
        /// </summary>
        /// <param name="owner">The owner control.</param>
        public MenuItemContainerGenerator(ItemsControl owner)
            : base(owner, MenuItem.HeaderProperty, MenuItem.HeaderTemplateProperty)
        {
        }

        protected override IControl CreateContainer(ElementFactoryGetArgs args)
        {
            return args.Data is Separator s ? s : base.CreateContainer(args);
        }
    }
}
