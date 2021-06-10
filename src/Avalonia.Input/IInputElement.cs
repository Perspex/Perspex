using System;
using System.Collections.Generic;
using Avalonia.Interactivity;
using Avalonia.VisualTree;

#nullable enable

namespace Avalonia.Input
{
    /// <summary>
    /// Defines input-related functionality for a control.
    /// </summary>
    public interface IInputElement : IInteractive, IVisual
    {
        /// <summary>
        /// Occurs when the control receives focus.
        /// </summary>
        event EventHandler<GotFocusEventArgs> GotFocus;

        /// <summary>
        /// Occurs when the control loses focus.
        /// </summary>
        event EventHandler<RoutedEventArgs> LostFocus;

        /// <summary>
        /// Occurs when a key is pressed while the control has focus.
        /// </summary>
        event EventHandler<KeyEventArgs> KeyDown;

        /// <summary>
        /// Occurs when a key is released while the control has focus.
        /// </summary>
        event EventHandler<KeyEventArgs> KeyUp;

        /// <summary>
        /// Occurs when a user typed some text while the control has focus.
        /// </summary>
        event EventHandler<TextInputEventArgs> TextInput;

        /// <summary>
        /// Occurs when the pointer enters the control.
        /// </summary>
        event EventHandler<PointerEventArgs> PointerEnter;

        /// <summary>
        /// Occurs when the pointer leaves the control.
        /// </summary>
        event EventHandler<PointerEventArgs> PointerLeave;

        /// <summary>
        /// Occurs when the pointer is pressed over the control.
        /// </summary>
        event EventHandler<PointerPressedEventArgs> PointerPressed;

        /// <summary>
        /// Occurs when the pointer moves over the control.
        /// </summary>
        event EventHandler<PointerEventArgs> PointerMoved;

        /// <summary>
        /// Occurs when the pointer is released over the control.
        /// </summary>
        event EventHandler<PointerReleasedEventArgs> PointerReleased;

        /// <summary>
        /// Occurs when the mouse wheel is scrolled over the control.
        /// </summary>
        event EventHandler<PointerWheelEventArgs> PointerWheelChanged;

        /// <summary>
        /// Gets or sets a value indicating whether the control can receive keyboard focus.
        /// </summary>
        bool Focusable { get; }

        /// <summary>
        /// Gets or sets a value indicating whether the control is enabled for user interaction.
        /// </summary>
        bool IsEnabled { get; }

        /// <summary>
        /// Gets or sets the associated mouse cursor.
        /// </summary>
        Cursor? Cursor { get; }

        /// <summary>
        /// Gets a value indicating whether this control and all its parents are enabled.
        /// </summary>
        /// <remarks>
        /// The <see cref="IsEnabled"/> property is used to toggle the enabled state for individual
        /// controls. The <see cref="IsEffectivelyEnabled"/> property takes into account the
        /// <see cref="IsEnabled"/> value of this control and its parent controls.
        /// </remarks>
        bool IsEffectivelyEnabled { get; }
        
        /// <summary>
        /// Gets a value indicating whether keyboard focus is anywhere within the element or its visual tree child elements.
        /// </summary>
        bool IsKeyboardFocusWithin { get; }

        /// <summary>
        /// Gets a value indicating whether the control is focused.
        /// </summary>
        bool IsFocused { get; }

        /// <summary>
        /// Gets a value indicating whether the control is considered for hit testing.
        /// </summary>
        bool IsHitTestVisible { get; }

        /// <summary>
        /// Gets a value indicating whether the pointer is currently over the control.
        /// </summary>
        bool IsPointerOver { get; }

        /// <summary>
        /// Focuses the control.
        /// </summary>
        void Focus();

        /// <summary>
        /// Gets the key bindings for the element.
        /// </summary>
        List<KeyBinding> KeyBindings { get; }
    }
}
