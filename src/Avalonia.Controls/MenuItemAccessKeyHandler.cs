using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace Avalonia.Controls
{
    /// <summary>
    /// Handles access keys within a <see cref="MenuItem"/>
    /// </summary>
    public class MenuItemAccessKeyHandler : IAccessKeyHandler
    {
        /// <summary>
        /// The registered access keys.
        /// </summary>
        private readonly List<Tuple<string, IInputElement>> _registered = new List<Tuple<string, IInputElement>>();

        /// <summary>
        /// The window to which the handler belongs.
        /// </summary>
        private IInputRoot _owner;

        /// <summary>
        /// Gets or sets the window's main menu.
        /// </summary>
        /// <remarks>
        /// This property is ignored as a menu item cannot have a main menu.
        /// </remarks>
        public IMainMenu MainMenu { get; set; }

        /// <summary>
        /// Sets the owner of the access key handler.
        /// </summary>
        /// <param name="owner">The owner.</param>
        /// <remarks>
        /// This method can only be called once, typically by the owner itself on creation.
        /// </remarks>
        public void SetOwner(IInputRoot owner)
        {
            Contract.Requires<ArgumentNullException>(owner != null);

            if (_owner != null)
            {
                throw new InvalidOperationException("AccessKeyHandler owner has already been set.");
            }

            _owner = owner;

            _owner.AddHandler(InputElement.TextInputEvent, OnTextInput);
        }

        /// <summary>
        /// Registers an input element to be associated with an access key.
        /// </summary>
        /// <param name="accessKey">The access key.</param>
        /// <param name="element">The input element.</param>
        public void Register(char accessKey, IInputElement element)
        {
            var existing = _registered.FirstOrDefault(x => x.Item2 == element);

            if (existing != null)
            {
                _registered.Remove(existing);
            }

            _registered.Add(Tuple.Create(accessKey.ToString().ToUpper(), element));
        }

        /// <summary>
        /// Unregisters the access keys associated with the input element.
        /// </summary>
        /// <param name="element">The input element.</param>
        public void Unregister(IInputElement element)
        {
            foreach (var i in _registered.Where(x => x.Item2 == element).ToList())
            {
                _registered.Remove(i);
            }
        }

        /// <summary>
        /// Handles a key being pressed in the menu.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event args.</param>
        protected virtual void OnTextInput(object sender, TextInputEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(e.Text))
            {
                var text = e.Text.ToUpper();
                var focus = _registered
                    .FirstOrDefault(x => x.Item1 == text && x.Item2.IsClosestVisualEffectivelyVisible())?.Item2;

                focus?.RaiseEvent(new RoutedEventArgs(AccessKeyHandler.AccessKeyPressedEvent));

                e.Handled = true;
            }
        }
    }
}
