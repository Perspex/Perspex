using System;
using Avalonia.Controls.Platform;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;

#nullable enable

namespace Avalonia.Controls
{
    /// <summary>
    /// A top-level menu control.
    /// </summary>
    public class Menu : MenuBase, IMainMenu
    {
        private static readonly ITemplate<IPanel> DefaultPanel =
            new FuncTemplate<IPanel>(() => new StackPanel { Orientation = Orientation.Horizontal });


        /// <summary>
        /// Defines the <see cref="Opened"/> event.
        /// </summary>
        public static readonly RoutedEvent<RoutedEventArgs> OpenedEvent =
            RoutedEvent.Register<Menu, RoutedEventArgs>(nameof(Opened), RoutingStrategies.Bubble);

        /// <summary>
        /// Defines the <see cref="Closed"/> event.
        /// </summary>
        public static readonly RoutedEvent<RoutedEventArgs> ClosedEvent =
            RoutedEvent.Register<Menu, RoutedEventArgs>(nameof(Closed), RoutingStrategies.Bubble);

        /// <summary>
        /// Occurs when a <see cref="Menu"/> is opened.
        /// </summary>
        public event EventHandler<RoutedEventArgs> Opened
        {
            add { AddHandler(OpenedEvent, value); }
            remove { RemoveHandler(OpenedEvent, value); }
        }

        /// <summary>
        /// Occurs when a <see cref="Menu"/> is closed.
        /// </summary>
        public event EventHandler<RoutedEventArgs> Closed
        {
            add { AddHandler(ClosedEvent, value); }
            remove { RemoveHandler(ClosedEvent, value); }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Menu"/> class.
        /// </summary>
        public Menu()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Menu"/> class.
        /// </summary>
        /// <param name="interactionHandler">The menu interaction handler.</param>
        public Menu(IMenuInteractionHandler interactionHandler)
            : base(interactionHandler)
        {
        }

        static Menu()
        {
            ItemsPanelProperty.OverrideDefaultValue(typeof(Menu), DefaultPanel);
        }

        /// <inheritdoc/>
        public override void Close()
        {
            if (!IsOpen)
            {
                return;
            }

            foreach (var i in ((IMenu)this).SubItems)
            {
                i.Close();
            }

            IsOpen = false;
            SelectedIndex = -1;

            RaiseEvent(new RoutedEventArgs
            {
                RoutedEvent = ClosedEvent,
                Source = this,
            });
        }

        /// <inheritdoc/>
        public override void Open()
        {
            if (IsOpen)
            {
                return;
            }

            IsOpen = true;

            RaiseEvent(new RoutedEventArgs
            {
                RoutedEvent = OpenedEvent,
                Source = this,
            });
        }

        /// <inheritdoc/>
        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);

            var inputRoot = e.Root as IInputRoot;

            if (inputRoot?.AccessKeyHandler != null)
            {
                inputRoot.AccessKeyHandler.MainMenu = this;
            }
        }
    }
}
