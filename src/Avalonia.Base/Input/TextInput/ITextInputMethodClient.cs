using System;
using Avalonia.VisualTree;

namespace Avalonia.Input.TextInput
{
    public interface ITextInputMethodClient
    {
        /// <summary>
        /// The cursor rectangle relative to the TextViewVisual
        /// </summary>
        Rect CursorRectangle { get; }
        /// <summary>
        /// Should be fired when cursor rectangle is changed inside the TextViewVisual
        /// </summary>
        event EventHandler CursorRectangleChanged;
        /// <summary>
        /// The visual that's showing the text
        /// </summary>
        IVisual TextViewVisual { get; }
        /// <summary>
        /// Should be fired when text-hosting visual is changed
        /// </summary>
        event EventHandler TextViewVisualChanged;
        /// <summary>
        /// Indicates if TextViewVisual is capable of displaying non-commited input on the cursor position
        /// </summary>
        bool SupportsPreedit { get; }
        /// <summary>
        /// Sets the non-commited input string
        /// </summary>
        void SetPreeditText(string text);
        /// <summary>
        /// Indicates if text input client is capable of providing the text around the cursor
        /// </summary>
        bool SupportsSurroundingText { get; }
        /// <summary>
        /// Returns the text around the cursor, usually the current paragraph, the cursor position inside that text and selection start position
        /// </summary>
        TextInputMethodSurroundingText SurroundingText { get; }
        /// <summary>
        /// Should be fired when surrounding text changed
        /// </summary>
        event EventHandler SurroundingTextChanged;
        /// <summary>
        /// Returns the text before the cursor. Must return a non-empty string if cursor is not at the beginning of the text entry
        /// </summary>
        string TextBeforeCursor { get; }
        /// <summary>
        /// Returns the text before the cursor. Must return a non-empty string if cursor is not at the end of the text entry
        /// </summary>
        string TextAfterCursor { get; }
    }

    public struct TextInputMethodSurroundingText
    {
        public string Text { get; set; }
        public int CursorOffset { get; set; }
        public int AnchorOffset { get; set; }
    }
}
