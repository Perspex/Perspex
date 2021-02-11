// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: An abstract interface describing a linear text document.
//

using System;
using Avalonia;
using Avalonia.Media.TextFormatting;
using MS.Internal.Documents;

namespace System.Windows.Documents
{
    internal interface ITextContainer
    {
        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------

        #region Internal Methods

        void BeginChange();

        // Like BeginChange, but does not ever create an undo unit.
        // This method is called before UndoManager.Undo, and can't have
        // an open undo unit while running Undo.
        void BeginChangeNoUndo();

        void EndChange();

        void EndChange(bool skipEvents);

        // Allocate a new ITextPointer at the specified offset.
        // Equivalent to this.Start.CreatePointer(offset), but does not
        // necessarily allocate this.Start.
        ITextPointer CreatePointerAtOffset(int offset, LogicalDirection direction);

        // Allocate a new ITextPointer at a specificed offset in unicode chars within the document.
        //  this should probably be refactored out of ITextContainer
        // since only TextStore supports it.
        ITextPointer CreatePointerAtCharOffset(int charOffset, LogicalDirection direction);

        ITextPointer CreateDynamicTextPointer(StaticTextPointer position, LogicalDirection direction);

        StaticTextPointer CreateStaticPointerAtOffset(int offset);

        TextPointerContext GetPointerContext(StaticTextPointer pointer, LogicalDirection direction);

        int GetOffsetToPosition(StaticTextPointer position1, StaticTextPointer position2);

        int GetTextInRun(StaticTextPointer position, LogicalDirection direction, char[] textBuffer, int startIndex, int count);

        object GetAdjacentElement(StaticTextPointer position, LogicalDirection direction);

        IAvaloniaObject GetParent(StaticTextPointer position);

        StaticTextPointer CreatePointer(StaticTextPointer position, int offset);

        StaticTextPointer GetNextContextPosition(StaticTextPointer position, LogicalDirection direction);

        int CompareTo(StaticTextPointer position1, StaticTextPointer position2);

        int CompareTo(StaticTextPointer position1, ITextPointer position2);

        object GetValue(StaticTextPointer position, AvaloniaProperty formattingProperty);

        #endregion Internal Methods

        //------------------------------------------------------
        //
        //  Internal Properties
        //
        //------------------------------------------------------

        #region Internal Properties

        /// <summary>
        /// Specifies whether or not the content of this TextContainer may be
        /// modified.
        /// </summary>
        /// <value>
        /// True if content may be modified, false otherwise.
        /// </value>
        /// <remarks>
        /// Methods that modify the TextContainer, such as InsertText or
        /// DeleteContent, will throw InvalidOperationExceptions if this
        /// property returns true.
        /// </remarks>
        bool IsReadOnly { get; }

        /// <summary>
        /// A position preceding the first symbol of this TextContainer.
        /// </summary>
        /// <remarks>
        /// The returned ITextPointer has LogicalDirection.Backward gravity.
        /// </remarks>
        ITextPointer Start { get; }

        /// <summary>
        /// A position following the last symbol of this TextContainer.
        /// </summary>
        /// <remarks>
        /// The returned ITextPointer has LogicalDirection.Forward gravity.
        /// </remarks>
        ITextPointer End { get; } 

        /// <summary>
        /// The object containing this TextContainer, from which property
        /// values are inherited.
        /// </summary>
        /// <remarks>
        /// May be null.
        /// </remarks>
        IAvaloniaObject Parent { get; }

        /// <summary>
        /// Collection of highlights applied to TextContainer content.
        /// </summary>
        Highlights Highlights { get; }

        // Optional text selection, may be null if there's no TextEditor
        // associated with an ITextContainer.
        // TextEditor owns setting and clearing this property inside its
        // ctor/OnDetach methods.
        //
        // 9/29/05: It may be possible to remove this property
        // by relying on a tree walk up from the Parent property looking
        // for an attached TextEditor (which maps 1-to-1 with TextSelection).
        ITextSelection TextSelection { get; set; }

        // Optional undo manager, may be null.
        UndoManager UndoManager { get; }

        // Optional TextView, may be null.
        // When several views are nested, this view is always the "top-level"
        // view, the one used by the TextEditor.
        ITextView TextView { get; set; }

        // Count of symbols in this tree, equivalent to this.Start.GetOffsetToPosition(this.End),
        // but doesn't necessarily allocate anything.
        int SymbolCount { get; }

        // Count of unicode characters in the tree.
        //  this should probably be refactored out of ITextContainer
        // since only TextStore supports it.
        int IMECharCount { get; }

        // Autoincremented counter of content changes in this TextContainer
        uint Generation { get; }

        #endregion Internal Properties

        //------------------------------------------------------
        //
        //  Internal Events
        //
        //------------------------------------------------------

        #region Internal Events

        // Fired before each edit to the ITextContainer.
        // This event is useful to flag reentrant calls to the listener while the
        // container is being modified (via logical tree events) before the
        // matching Change event is fired.
        // Listener has READ-ONLY access to the ITextContainer inside the scope
        // of the callback.
        event EventHandler Changing;

        // Fired on each edit.
        // Listener has READ-ONLY access to the ITextContainer inside the scope
        // of the callback.
        event TextContainerChangeEventHandler Change;

        // Fired once as a change block exits -- after one or more edits.
        // It is legal to modify the ITextContainer inside this event.
        event TextContainerChangedEventHandler Changed;

        #endregion Internal Events
    }
}
