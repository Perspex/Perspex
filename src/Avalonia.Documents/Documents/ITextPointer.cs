// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: Abstract version of TextPointer.
//

using Avalonia.Media.TextFormatting;

namespace System.Windows.Documents
{
    using System;
    using Avalonia;

    // Abstract version of TextPointer.  It has full read-only support for
    // rich content, but only supports plain text editing.
    internal interface ITextPointer
    {
        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------

        #region Internal Methods

        // Constructor.
        ITextPointer CreatePointer();

        // Constructor.
        StaticTextPointer CreateStaticPointer();

        // Constructor.
        ITextPointer CreatePointer(int offset);

        // Constructor.
        ITextPointer CreatePointer(LogicalDirection gravity);

        // Constructor.
        ITextPointer CreatePointer(int offset, LogicalDirection gravity);

        // Property accessor.
        void SetLogicalDirection(LogicalDirection direction);

        // Returns
        //  -1 if this ITextPointer is positioned before position.
        //   0 if this ITextPointer is positioned at position.
        //  +1 if this ITextPointer is positioned after position.
        int CompareTo(ITextPointer position);
        int CompareTo(StaticTextPointer position);

        // Returns true if this ITextPointer has the same logical parent has position.
        bool HasEqualScope(ITextPointer position);

        // <see cref="TextPointer.GetPointerContext"/>
        TextPointerContext GetPointerContext(LogicalDirection direction);

        // <see cref="TextPointer.GetOffsetToPosition"/>
        int GetOffsetToPosition(ITextPointer position);

        // <see cref="TextPointer.GetTextRunLength"/>
        int GetTextRunLength(LogicalDirection direction);

        // <see cref="TextPointer.GetTextInRun"/>
        string GetTextInRun(LogicalDirection direction);

        // <see cref="TextPointer.GetTextInRun"/>
        int GetTextInRun(LogicalDirection direction, char[] textBuffer, int startIndex, int count);

        // <see cref="TextPointer.GetAdjacentElement"/>
        //  this should return IAvaloniaObject (which is
        // either ContentElement or UIElement) for consistency with TextPointer.GetAdjacentElement.
        // Blocking issue: DocumentSequenceTextPointer returns an object to break
        // pages.
        object GetAdjacentElement(LogicalDirection direction);

        // <see cref="TextPointer.MoveToPosition"/>
        void MoveToPosition(ITextPointer position);

        // <see cref="TextPointer.MoveByOffset"/>
        int MoveByOffset(int offset);

        // <see cref="TextPointer.MoveToNextContextPosition"/>
        bool MoveToNextContextPosition(LogicalDirection direction);

        // <see cref="TextPointer.GetNextContextPosition"/>
        ITextPointer GetNextContextPosition(LogicalDirection direction);

        // <see cref="TextPointer.MoveToInsertionPosition"/>
        bool MoveToInsertionPosition(LogicalDirection direction);

        // <see cref="TextPointer.GetInsertionPosition"/>
        ITextPointer GetInsertionPosition(LogicalDirection direction);

        // <see cref="TextPointer.GetFormatNormalizedPosition"/>
        ITextPointer GetFormatNormalizedPosition(LogicalDirection direction);

        // <see cref="TextPointer.MoveToNextInsertionPosition"/>
        bool MoveToNextInsertionPosition(LogicalDirection direction);

        // <see cref="TextPointer.GetNextInsertionPosition"/>
        ITextPointer GetNextInsertionPosition(LogicalDirection direction);

        // Moves this ITextPointer to the specified edge of the parent text element.
        void MoveToElementEdge(ElementEdge edge);

        // <see cref="TextPointer.MoveToLineStart"/>
        int MoveToLineBoundary(int count);

        // <see cref="TextPointer.GetCharacterRect"/>
        Rect GetCharacterRect(LogicalDirection direction);

        // <see cref="TextPointer.Freeze"/>
        void Freeze();

        // <see cref="TextPointer.GetFrozenPointer"/>
        ITextPointer GetFrozenPointer(LogicalDirection logicalDirection);

        // <see cref="TextPointer.InsertText"/>
        void InsertTextInRun(string textData);

        // <see cref="TextPointer.DeleteContentToPosition"/>
        void DeleteContentToPosition(ITextPointer limit);

        // <see cref="TextPointer.GetTextElement"/>
        // rename this method to match eventual TextPointer equivalent.
        Type GetElementType(LogicalDirection direction);

        // Returns a DP value on this ITextPointer's logical parent.
        object GetValue(AvaloniaProperty formattingProperty);

        // Returns a local DP value on this ITextPointer's logical parent.
        object ReadLocalValue(AvaloniaProperty formattingProperty);

        // Returns all local values on this ITextPointer's logical parent.
        LocalValueEnumerator GetLocalValueEnumerator();

        /// <summary>
        /// Ensures layout information is available at this position.
        /// </summary>
        /// <returns>
        /// True if the position is validated, false otherwise.
        /// </returns>
        /// <remarks>
        /// Use this method before calling GetCharacterRect, MoveToLineBoundary,
        /// IsAtLineStartPosition.
        ///
        /// This method can be very expensive.  To detect an invalid layout
        /// without actually doing any work, use the HasValidLayout property.
        /// </remarks>
        bool ValidateLayout();

        #endregion Internal Methods

        //------------------------------------------------------
        //
        //  Internal Properties
        //
        //------------------------------------------------------

        #region Internal Properties

        // Associated TextContainer.
        ITextContainer TextContainer { get; }

        // <see cref="TextPointer.HasValidLayout"/>
        bool HasValidLayout { get; }

        // Returns true if this pointer is at a caret unit boundary.
        // Logically equivalent to this.TextContainer.TextView.IsAtCaretUnitBoundary(this),
        // but some implementations may have better performance.
        //
        // This method must not be called unless HasValidLayout == true.
        bool IsAtCaretUnitBoundary { get; }

        // <see cref="TextPointer.LogicalDirection"/>
        LogicalDirection LogicalDirection { get; }

        // <see cref="TextPointer.Parent"/>
        Type ParentType { get; }

        // <see cref="TextPointer.ParentContentStart"/>
        //ITextPointer ParentContentStart { get; }

        // <see cref="TextPointer.ParentContentEnd"/>
        //ITextPointer ParentContentEnd { get; }

        // <see cref="TextPointer.ContainerContentStart"/>
        //ITextPointer ContainerContentStart { get; }

        // <see cref="TextPointer.ContainerContentEnd"/>
        //ITextPointer ContainerContentEnd { get; }

        // <see cref="TextPointer.IsAtInsertionPosition"/>
        bool IsAtInsertionPosition { get; }

        // <see cref="TextPointer.IsFrozen"/>
        bool IsFrozen { get; }

        // Offset from the TextContainer.Start position.
        // Equivalent to this.TextContainer.Start.GetOffsetToPosition(this),
        // but doesn't necessarily allocate anything.
        int Offset { get; }

        // Offset in unicode chars within the document.
        //  this should probably be refactored out of ITextPointer
        // since only TextStore supports it.
        int CharOffset { get; }

        #endregion Internal Properties
    }
}
