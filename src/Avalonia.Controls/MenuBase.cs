using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls.Generators;
using Avalonia.Controls.Platform;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;

#nullable enable

namespace Avalonia.Controls
{
    /// <summary>
    /// Base class for menu controls.
    /// </summary>
    public abstract class MenuBase : SelectingItemsControl, IFocusScope, IMenu
    {
        /// <summary>
        /// Defines the <see cref="IsOpen"/> property.
        /// </summary>
        public static readonly DirectProperty<Menu, bool> IsOpenProperty =
            AvaloniaProperty.RegisterDirect<Menu, bool>(
                nameof(IsOpen),
                o => o.IsOpen);

        /// <summary>
        /// Defines the <see cref="MenuOpened"/> event.
        /// </summary>
        public static readonly RoutedEvent<RoutedEventArgs> MenuOpenedEvent =
            RoutedEvent.Register<MenuBase, RoutedEventArgs>(nameof(MenuOpened), RoutingStrategies.Bubble);

        /// <summary>
        /// Defines the <see cref="MenuClosed"/> event.
        /// </summary>
        public static readonly RoutedEvent<RoutedEventArgs> MenuClosedEvent =
            RoutedEvent.Register<MenuBase, RoutedEventArgs>(nameof(MenuClosed), RoutingStrategies.Bubble);

        private bool _isOpen;

        /// <summary>
        /// Initializes a new instance of the <see cref="MenuBase"/> class.
        /// </summary>
        public MenuBase()
        {
            InteractionHandler = new DefaultMenuInteractionHandler(false);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MenuBase"/> class.
        /// </summary>
        /// <param name="interactionHandler">The menu interaction handler.</param>
        public MenuBase(IMenuInteractionHandler interactionHandler)
        {
            InteractionHandler = interactionHandler ?? throw new ArgumentNullException(nameof(interactionHandler));
        }

        /// <summary>
        /// Initializes static members of the <see cref="MenuBase"/> class.
        /// </summary>
        static MenuBase()
        {
            MenuItem.SubmenuOpenedEvent.AddClassHandler<MenuBase>((x, e) => x.OnSubmenuOpened(e));
        }

        /// <summary>
        /// Gets a value indicating whether the menu is open.
        /// </summary>
        public bool IsOpen
        {
            get { return _isOpen; }
            protected set { SetAndRaise(IsOpenProperty, ref _isOpen, value); }
        }

        /// <inheritdoc/>
        IMenuInteractionHandler IMenu.InteractionHandler => InteractionHandler;

        /// <inheritdoc/>
        IMenuItem? IMenuElement.SelectedItem
        {
            get
            {
                var index = SelectedIndex;
                return (index != -1) ? (IMenuItem)TryGetContainer(index) : null;
            }
            set
            {
                SelectedIndex = value is object ? GetContainerIndex(value) : -1;
            }
        }

        /// <inheritdoc/>
        IEnumerable<IMenuItem> IMenuElement.SubItems => Presenter.RealizedElements.OfType<IMenuItem>();

        /// <summary>
        /// Gets the interaction handler for the menu.
        /// </summary>
        protected IMenuInteractionHandler InteractionHandler { get; }

        /// <summary>
        /// Occurs when a <see cref="Menu"/> is opened.
        /// </summary>
        public event EventHandler<RoutedEventArgs> MenuOpened
        {
            add { AddHandler(MenuOpenedEvent, value); }
            remove { RemoveHandler(MenuOpenedEvent, value); }
        }

        /// <summary>
        /// Occurs when a <see cref="Menu"/> is closed.
        /// </summary>
        public event EventHandler<RoutedEventArgs> MenuClosed
        {
            add { AddHandler(MenuClosedEvent, value); }
            remove { RemoveHandler(MenuClosedEvent, value); }
        }

        /// <summary>
        /// Closes the menu.
        /// </summary>
        public abstract void Close();

        /// <summary>
        /// Opens the menu.
        /// </summary>
        public abstract void Open();

        /// <inheritdoc/>
        bool IMenuElement.MoveSelection(NavigationDirection direction, bool wrap) => MoveSelection(direction, wrap);

        protected override IItemContainerGenerator CreateItemContainerGenerator()
        {
            return new ItemContainerGenerator<MenuItem>(this, MenuItem.HeaderProperty, MenuItem.HeaderTemplateProperty);
        }

        /// <inheritdoc/>
        protected override void OnKeyDown(KeyEventArgs e)
        {
            // Don't handle here: let the interaction handler handle it.
        }

        /// <inheritdoc/>
        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);
            InteractionHandler.Attach(this);
        }

        /// <inheritdoc/>
        protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnDetachedFromVisualTree(e);
            InteractionHandler.Detach(this);
        }

        /// <summary>
        /// Called when a submenu opens somewhere in the menu.
        /// </summary>
        /// <param name="e">The event args.</param>
        protected virtual void OnSubmenuOpened(RoutedEventArgs e)
        {
            if (e.Source is MenuItem menuItem && menuItem.Parent == this)
            {
                foreach (var child in this.GetLogicalChildren().OfType<MenuItem>())
                {
                    if (child != menuItem && child.IsSubMenuOpen)
                    {
                        child.IsSubMenuOpen = false;
                    }
                }
            }

            IsOpen = true;
        }
    }
}
