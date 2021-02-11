// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: Provides an abstract level of TextRange implementation
//      Implemented as a static class containing a set of methods
//      implementing members of abstract ITextRange interface.
//      These members are supposed to be called from concrete
//      classes TextRange and TextSelection - to ensure that the
//      both have the same base implementation.
//
//      TextSelection is allowed to add additional actions over
//      base ones. TextRange must do pure call redirections,
//      otherwise TextSelection inheritance from TextRange
//      will be broken.
//
//      Only methods that require virtualization for TextSelection
//      implementation go here. All other methods of ITextRange
//      are implemented directly in TextRange clas in appropriate
//      ITextRange.Member.
//

using System.Collections.Specialized;
using Avalonia;
using Avalonia.Documents;
using Avalonia.Documents.Internal;
using Avalonia.Media.TextFormatting;

namespace System.Windows.Documents
{
    using MS.Internal;
    using System.Collections;
    using System.Collections.Generic;
    using System.Threading;
    using System.Globalization;
    using System.Text;
    using System.Xml;
    using System.IO;
    //using MS.Internal.Documents;
    //using System.Windows.Controls; // TextBlock

    /// <summary>
    /// A class a portion of text content.
    /// Can be contigous or disjoint; supports rectangular table ranges.
    /// Provides an API for text and table editing operations.
    /// </summary>
    internal static class TextRangeBase
    {
        //------------------------------------------------------
        //
        // ITextRange Methods
        //
        //------------------------------------------------------

        #region ITextRange Methods

        //......................................................
        //
        // Selection Building
        //
        //......................................................

        /// <summary>
        /// </summary>
        // need to accound for position.LogicalDirection. -- see TextSegment.Contains implementation
        internal static bool Contains(ITextRange thisRange, ITextPointer textPointer)
        {
            NormalizeRange(thisRange);

            if (textPointer == null)
            {
                throw new ArgumentNullException("textPointer");
            }

            if (textPointer.TextContainer != thisRange.Start.TextContainer)
            {
                throw new ArgumentException(/*SR.Get(SRID.NotInAssociatedTree), "textPointer"*/);
            }

            // Correct position normalization on range boundary so that
            // our test would not depend on what side of formatting tags
            // pointer is located.
            if (textPointer.CompareTo(thisRange.Start) < 0)
            {
                textPointer = textPointer.GetFormatNormalizedPosition(LogicalDirection.Forward);
            }
            else if (textPointer.CompareTo(thisRange.End) > 0)
            {
                textPointer = textPointer.GetFormatNormalizedPosition(LogicalDirection.Backward);
            }

            // Check if at least one segment contains this position.
            for (int i = 0; i < thisRange._TextSegments.Count; i++)
            {
                if (thisRange._TextSegments[i].Contains(textPointer))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Base implementation of ITextRange.Select method.
        /// </summary>
        /// <param name="thisRange">
        /// The range which is an object of this operation.
        /// </param>
        /// <param name="position1">
        /// One of two boundary positions for building selection.
        /// In case of table cell-crossing selection it is considered
        /// as an "anchor" position, so the selection will always include
        /// the cell at this position
        /// </param>
        /// <param name="position2">
        /// The other of two buondary positions for building selection.
        /// In case of table cell-crossing selection it is considered
        /// as a "moving" position, so the selection may not include the cell
        /// at this position - when the cell has bigger index than the anchor
        /// one and when it is positioned at the very beginning of a cell,
        /// (if any of these two conditions if false then the cell at this position is included).
        /// </param>
        internal static void Select(ITextRange thisRange, ITextPointer position1, ITextPointer position2)
        {
            Select(thisRange, position1, position2, /*includeCellAtMovingPosition:*/false);
        }

        /// <summary>
        /// Base implementation of ITextRange.Select method.
        /// </summary>
        /// <param name="thisRange">
        /// The range which is an object of this operation.
        /// </param>
        /// <param name="position1">
        /// One of two boundary positions for building selection.
        /// In case of table cell-crossing selection it is considered
        /// as an "anchor" position, so the selection will always include
        /// the cell at this position
        /// </param>
        /// <param name="position2">
        /// The other of two buondary positions for building selection.
        /// In case of table cell-crossing selection it is considered
        /// as a "moving" position, so the selection may not include the cell
        /// at this position - when the cell has bigger index than the anchor
        /// one and when it is positioned at the very beginning of a cell,
        /// and when includeCellAtMovingPosition==false (if any of these three
        /// conditions if false then the cell at this position is included).
        /// </param>
        /// <param name="includeCellAtMovingPosition">
        /// True indicates that a cell at a movingPosition must be included
        /// into a selection even when it is at cell start.
        /// False indicates that when a movingPosition is at cell start
        /// and the cell has bigger index than anchor cell, then selection
        /// should not include it - it only indicates cell crossing.
        /// When we build a table range from existing range's Start/End pair
        /// we must use false for this parameter - because the end position
        /// of a table range is not included into it - by construction.
        /// When you use independent position - say, from hit-testing -
        /// then you typically use "true" for this parameter, unnless
        /// you intentially cross cell boundary - as for one cell celection.
        /// </param>
        internal static void Select(ITextRange thisRange, ITextPointer position1, ITextPointer position2, bool includeCellAtMovingPosition)
        {
            if (thisRange._TextSegments == null)
            {
                // This is initializing call from TextRange constructor.
                // No need in change notifications, no need in position verification.
                TextRangeBase.SelectPrivate(thisRange, position1, position2, includeCellAtMovingPosition, /*markRangeChanged*/false);
            }
            else
            {
                ValidationHelper.VerifyPosition(thisRange.Start.TextContainer, position1, "position1");
                ValidationHelper.VerifyPosition(thisRange.Start.TextContainer, position2, "position2");

                TextRangeBase.BeginChange(thisRange);
                try
                {
                    TextRangeBase.SelectPrivate(thisRange, position1, position2, includeCellAtMovingPosition, /*markRangeChanged*/true);
                }
                finally
                {
                    TextRangeBase.EndChange(thisRange);
                }
            }
        }

        /// <summary>
        /// Selects a word containing this position
        /// </summary>
        /// <param name="thisRange"></param>
        /// <param name="position">
        /// A TextPointer containing a word to select.
        /// </param>
        internal static void SelectWord(ITextRange thisRange, ITextPointer position)
        {
            if (position == null)
            {
                throw new ArgumentNullException("position");
            }

            // Move position to character boundary (also respect atomics)
            // Shouldn't we do this on lower level - inside of TextPointer.GetWordRange ?
            // Is Backward correct direction for nornalization? We really want that atomic would appear in forward direction... 
            ITextPointer normalizedPosition = position.CreatePointer();
            normalizedPosition.MoveToInsertionPosition(LogicalDirection.Backward);

            TextSegment wordRange = TextPointerBase.GetWordRange(normalizedPosition);

            TextRangeBase.Select(thisRange, wordRange.Start, wordRange.End);
        }

        // Returns a word within which empty selection is located.
        // Returns TextSegment.Null if selection is not empty or
        // if the position is between or at word boundary.
        internal static TextSegment GetAutoWord(ITextRange thisRange)
        {
            TextSegment autoWordRange = TextSegment.Null;

            if (thisRange.IsEmpty && //
                !TextPointerBase.IsAtWordBoundary(thisRange.Start, LogicalDirection.Forward) && //
                !TextPointerBase.IsAtWordBoundary(thisRange.Start, LogicalDirection.Backward))
            {
                //REVIEW: BenWest: Trimming with Unicode 0x20 is not a general solution. Also review the string allocation here.

                autoWordRange = TextPointerBase.GetWordRange(thisRange.Start);
                string autoWord = TextRangeBase.GetTextInternal(autoWordRange.Start, autoWordRange.End).TrimEnd(' ');

                string textFromWordStart = TextRangeBase.GetTextInternal(autoWordRange.Start, thisRange.Start);

                if (textFromWordStart.Length >= autoWord.Length)
                {
                    // The caret is beyond the end of a word (in a whitespace area)
                    autoWordRange = TextSegment.Null;
                }
            }

            return autoWordRange;
        }

        /// <summary>
        /// Selects a paragraph around the given position.
        /// </summary>
        /// <param name="thisRange"></param>
        /// <param name="position">
        /// A position identifying a paragraph to select.
        /// </param>
        internal static void SelectParagraph(ITextRange thisRange, ITextPointer position)
        {
            if (position == null)
            {
                throw new ArgumentNullException("position");
            }

            ITextPointer start;
            ITextPointer end;
            FindParagraphOrListItemBoundaries(position, out start, out end);

            // Select the paragraph contents
            TextRangeBase.Select(thisRange, start, end);
        }

        // Apply initial typing heuristics -- adjust range for typing
        // when it spans one or more TableCells.
        //
        // ApplyInitialTypingHeuristics/ApplyFinalTypingHeuristics are
        // called together, with a an extra step in between for TextSelection
        // overrides of the ApplyTypingHueristic method.
        //internal static void ApplyInitialTypingHeuristics(ITextRange thisRange)
        //{
        //    // When table cells selected, clear the start cell and collapse selection into it
        //    if (thisRange.IsTableCellRange)
        //    {
        //        TableCell cell;
        //        if (thisRange.Start is TextPointer &&
        //            (cell = TextRangeEditTables.GetTableCellFromPosition((TextPointer)thisRange.Start)) != null)
        //        {
        //            // Select the first cell content to make springload formatting happen below
        //            thisRange.Select(cell.ContentStart, cell.ContentEnd);
        //        }
        //        else
        //        {
        //            thisRange.Select(thisRange.Start, thisRange.Start);
        //        }
        //    }
        //}

        // Apply typing heuristics
        //  - extend for overtype.
        //  - prevent paragraph merges when only the leading edge of that
        //    last paragraph is selected.
        //
        // ApplyInitialTypingHeuristics/ApplyFinalTypingHeuristics are
        // called together, with a an extra step in between for TextSelection
        // overrides of the ApplyTypingHueristic method.
        internal static void ApplyFinalTypingHeuristics(ITextRange thisRange, bool overType)
        {
            // Expand empty selection forward in overtype mode
            if (overType && thisRange.IsEmpty &&
                !TextPointerBase.IsNextToAnyBreak(thisRange.End, LogicalDirection.Forward))
            {
                // There is a bug here: when textData contains more than 1 character
                // we need to eat that number of symbols in TextContainer.
                // Currently we eat only one always.
                ITextPointer nextPosition = thisRange.End.CreatePointer();
                nextPosition.MoveToNextInsertionPosition(LogicalDirection.Forward);
                //if (!TextRangeEditTables.IsTableStructureCrossed(thisRange.Start, nextPosition))
                //{
                    TextRange range = new TextRange(thisRange.Start, nextPosition);
                    //Invariant.Assert(!range.IsTableCellRange);

                    range.Text = String.Empty;
                //}
            }

            // If the range is non-empty, and its end just passes a paragraph break,
            // pull the end back to stop a paragraph merge on the next keystroke.
            if (!thisRange.IsEmpty &&
                (TextPointerBase.IsNextToAnyBreak(thisRange.End, LogicalDirection.Backward) /*||*/
                 /*TextPointerBase.IsAfterLastParagraph(thisRange.End)*/))
            {
                ITextPointer newEnd = thisRange.End.GetNextInsertionPosition(LogicalDirection.Backward);
                thisRange.Select(thisRange.Start, newEnd);
            }
        }

        /// <summary>
        /// <see cref="System.Windows.Documents.ITextRange.ApplyTypingHeuristics"/>
        /// </summary>
        internal static void ApplyTypingHeuristics(ITextRange thisRange, bool overType)
        {
            BeginChange(thisRange);
            try
            {
                //ApplyInitialTypingHeuristics(thisRange);
                ApplyFinalTypingHeuristics(thisRange, overType);
            }
            finally
            {
                EndChange(thisRange);
            }
        }

        internal static void FindParagraphOrListItemBoundaries(ITextPointer position, out ITextPointer start, out ITextPointer end)
        {
            // Identify a maximum portion of text around navigator
            // which may be wrapped by Paragraph
            start = position.CreatePointer();
            end = position.CreatePointer();
            SkipParagraphContent(start, LogicalDirection.Backward);
            SkipParagraphContent(end, LogicalDirection.Forward);
        }

        // Moves the navigator in the given direction over all characters,
        // embedded objects and formatting tags.
        // Delete this techniquer after schema validation is complete.
        // Remove similar piece of code from Controls\TextRangeAdapter.cs@MoveToNext
        private static void SkipParagraphContent(ITextPointer navigator, LogicalDirection direction)
        {
            TextPointerContext nextContext = navigator.GetPointerContext(direction);

            while (true)
            {
                if (nextContext == TextPointerContext.None //
                    || //
                    // Entering non-inline content
                    (nextContext == TextPointerContext.ElementStart && direction == LogicalDirection.Forward || //
                    nextContext == TextPointerContext.ElementEnd && direction == LogicalDirection.Backward) && //
                    !typeof(Inline).IsAssignableFrom(navigator.GetElementType(direction)) //
                    ||
                    // Exiting non-inline content
                    (nextContext == TextPointerContext.ElementEnd && direction == LogicalDirection.Forward || //
                    nextContext == TextPointerContext.ElementStart && direction == LogicalDirection.Backward) && //
                    !typeof(Inline).IsAssignableFrom(navigator.ParentType))
                {
                    // End of paragraph content reached. Stop here.
                    break;
                }

                //Need to bail out if MoveToNextContentPosition fails
                if (!navigator.MoveToNextContextPosition(direction))
                {
                    break;
                }
                nextContext = navigator.GetPointerContext(direction);
            }
        }

        // Calculates a value of a given property on this range
        internal static object GetPropertyValue(ITextRange thisRange, AvaloniaProperty formattingProperty)
        {
            if (TextSchema.IsCharacterProperty(formattingProperty))
            {
                return GetCharacterPropertyValue(thisRange, formattingProperty);
            }

            return null;
            //else
            //{
            //    Invariant.Assert(TextSchema.IsParagraphProperty(formattingProperty), "The property is expected to be one of either character or paragraph formatting one");
            //    return GetParagraphPropertyValue(thisRange, formattingProperty);
            //}
        }

        // Calculates character formatting property
        private static object GetCharacterPropertyValue(ITextRange thisRange, AvaloniaProperty formattingProperty)
        {
            // This code works for inherited properies only. TextDecorations and BaselineAlignment properties are not supported

            object startValue = GetCharacterValueFromPosition(thisRange.Start, formattingProperty);

            // Need to run over all text runs to check that the value is the same for all of them.
            // We'll stop on the first different value if any; and return MixedValue.Instance
            for (int i = 0; i < thisRange._TextSegments.Count; i++)
            {
                TextSegment textSegment = thisRange._TextSegments[i];

                ITextPointer position = textSegment.Start.CreatePointer();
                bool moved = true;
                while (moved && position.CompareTo(textSegment.End) < 0)
                {
                    // Check whether the value in this text run is the same as at the beginning
                    object value = GetCharacterValueFromPosition(position, formattingProperty);
                    if (!TextSchema.ValuesAreEqual(value, startValue))
                    {
                        return AvaloniaProperty.UnsetValue; // Return MixedValue.Instance instead of this.
                    }

                    // Skip text run
                    if (position.GetPointerContext(LogicalDirection.Forward) == TextPointerContext.Text)
                    {
                        moved = position.MoveToNextContextPosition(LogicalDirection.Forward);
                    }

                    // try to skip formatting tags
                    moved = position.MoveToInsertionPosition(LogicalDirection.Forward);

                    if (!moved)
                    {
                        // Go to the next run, of there was no formatting boundary
                        moved = position.MoveToNextInsertionPosition(LogicalDirection.Forward);
                    }
                }
            }

            return startValue;
        }

        // Gets a non-inherited property from a given position
        private static object GetCharacterValueFromPosition(ITextPointer pointer, AvaloniaProperty formattingProperty)
        {
            object value = null;

            if (formattingProperty != Inline.TextDecorationsProperty)
            {
                value = pointer.GetValue(formattingProperty);
            }
            else
            {
                if (pointer is TextPointer) // Implement only for concrete TextCotainer returning null otherwise - for optimization
                {
                    IAvaloniaObject element = ((TextPointer)pointer).Parent as TextElement;
                    while (value == null && (element is Inline || /*element is Paragraph ||*/ element is NewTextBlock))
                    {
                        value = element.GetValue(formattingProperty);

                        element = element is TextElement ? ((TextElement)element).Parent : null;
                    }
                }
            }

            return value;
        }

        // Calculates paragraph formatting property
        // Returns AvaloniaProperty.UnsetValue if different areas of the range have different value for this property.
        //private static object GetParagraphPropertyValue(ITextRange thisRange, AvaloniaProperty formattingProperty)
        //{
        //    object startValue = null;

        //    // Need to run over all text runs to check that the value is the same for all of them.
        //    // We'll stop on the first different value if any; and return MixedValue.Instance
        //    for (int i = 0; i < thisRange._TextSegments.Count; i++)
        //    {
        //        TextSegment textSegment = thisRange._TextSegments[i];

        //        ITextPointer position = textSegment.Start.CreatePointer();

        //        // Find position scoped by paragraph - in backward direction - to get start value
        //        while (!typeof(Paragraph).IsAssignableFrom(position.ParentType) &&
        //            position.MoveToNextContextPosition(LogicalDirection.Backward)) ;

        //        // Traverse the segment to find all other paragraph positions
        //        bool moved = true;
        //        while (moved && position.CompareTo(textSegment.End) <= 0)
        //        {
        //            if (typeof(Paragraph).IsAssignableFrom(position.ParentType))
        //            {
        //                object value = position.GetValue(formattingProperty);
        //                if (startValue == null)
        //                {
        //                    startValue = value;
        //                }

        //                if (!TextSchema.ValuesAreEqual(value, startValue))
        //                {
        //                    return AvaloniaProperty.UnsetValue;
        //                }

        //                position.MoveToElementEdge(ElementEdge.AfterEnd);
        //            }
        //            moved = position.MoveToNextContextPosition(LogicalDirection.Forward);
        //        }
        //    }

        //    // Most properties does not allow null as a value,
        //    // so if we still have null try to get a value from range start position.
        //    // For some properties (like TextDecorations) it still may remain null.
        //    if (startValue == null)
        //    {
        //        startValue = thisRange.Start.GetValue(formattingProperty);
        //    }

        //    return startValue;
        //}

        // Returns true if this range start and end pointers cross a paragraph boundary, false otherwise.
        //internal static bool IsParagraphBoundaryCrossed(ITextRange thisRange)
        //{
        //    ITextPointer startNavigator = thisRange.Start.CreatePointer();
        //    ITextPointer endNavigator = thisRange.End.CreatePointer();

        //    if (TextPointerBase.IsAfterLastParagraph(endNavigator))
        //    {
        //        endNavigator.MoveToInsertionPosition(LogicalDirection.Backward);
        //    }

        //    // Walk upto the closest block ancestor
        //    while (typeof(Inline).IsAssignableFrom(startNavigator.ParentType))
        //    {
        //        startNavigator.MoveToElementEdge(ElementEdge.AfterEnd);
        //    }
        //    while (typeof(Inline).IsAssignableFrom(endNavigator.ParentType))
        //    {
        //        endNavigator.MoveToElementEdge(ElementEdge.AfterEnd);
        //    }

        //    // start and end are within the scope of the same paragraph?
        //    return !startNavigator.HasEqualScope(endNavigator);
        //}

        //.........................................................
        //
        //  Change Notifications
        //
        //.........................................................

        /// <summary>
        /// <see cref="ITextRange.BeginChange"/>
        /// </summary>
        internal static void BeginChange(ITextRange thisRange)
        {
            BeginChangeWorker(thisRange, String.Empty);
        }

        /// <summary>
        /// <see cref="ITextRange.BeginChangeNoUndo"/>
        /// </summary>
        internal static void BeginChangeNoUndo(ITextRange thisRange)
        {
            BeginChangeWorker(thisRange, null);
        }

        /// <summary>
        /// <see cref="ITextRange.EndChange()"/>
        /// </summary>
        internal static void EndChange(ITextRange thisRange)
        {
            EndChange(thisRange, false /* disableScroll */, false /* skipEvents */ );
        }

        /// <summary>
        /// <see cref="ITextRange.EndChange(bool,bool)"/>
        /// </summary>
        internal static void EndChange(ITextRange thisRange, bool disableScroll, bool skipEvents)
        {
            ChangeBlockUndoRecord changeBlockUndoRecord;
            bool isChanged;
            ITextContainer textContainer;

            Invariant.Assert(thisRange._ChangeBlockLevel > 0, "Unmatched EndChange call!");

            textContainer = thisRange.Start.TextContainer;

            try
            {
                //
                // Complete the content changed block.
                //
                try
                {
                    // Raise first public event -- TextContainer.EndChange.
                    textContainer.EndChange(skipEvents);
                }
                finally
                {
                    // Always drop the ChangeBlockLevel, no matter what happens.
                    // This ensures that we won't ignore future events if the
                    // application recovers from an exception.
                    thisRange._ChangeBlockLevel--;

                    // Clear out thisRange.IsChanged now so that it isn't
                    // left dangling if TextContainer.EndChange throws
                    // an exception.
                    isChanged = thisRange._IsChanged;
                    if (thisRange._ChangeBlockLevel == 0)
                    {
                        thisRange._IsChanged = false;
                    }
                }

                //
                // Complete the range repositioned block.
                //
                if (thisRange._ChangeBlockLevel == 0 && isChanged)
                {
                    // Raise the second public event -- TextRange.Changed.
                    thisRange.NotifyChanged(disableScroll, skipEvents);
                }
            }
            finally
            {
                // Make sure we close the undo record no matter what happened.
                changeBlockUndoRecord = (ChangeBlockUndoRecord)thisRange._ChangeBlockUndoRecord;
                if (changeBlockUndoRecord != null && thisRange._ChangeBlockLevel == 0)
                {
                    try
                    {
                        changeBlockUndoRecord.OnEndChange();
                    }
                    finally
                    {
                        thisRange._ChangeBlockUndoRecord = null;
                    }
                }
            }
        }

        /// <summary>
        /// <see cref="ITextRange.NotifyChanged(bool,bool)"/>
        /// </summary>
        internal static void NotifyChanged(ITextRange thisRange, bool disableScroll)
        {
            thisRange.FireChanged();
        }

        #endregion ITextRange Methods

        // ....................................................................
        //
        // Static Helpers for dealing with content without range instantiation
        //
        // ....................................................................

        #region TextRange Helpers

        // Returns the text covered by two TextPositions as a string.
        // Includes rules for translating paragraph breaks and embedded objects.
        internal static string GetTextInternal(ITextPointer startPosition, ITextPointer endPosition)
        {
            Char[] charArray = null; // used for extracting text runs

            return GetTextInternal(startPosition, endPosition, ref charArray);
        }

        // Returns the text covered by two TextPositions as a string.
        // Includes rules for translating paragraph breaks and embedded objects.
        //
        // Use this overload when looping over large quantities of text to avoid
        // re-allocating a temporary buffer.
        internal static string GetTextInternal(ITextPointer startPosition, ITextPointer endPosition, ref Char[] charArray)
        {
            // Buffer for building a resulting plain text
            StringBuilder textBuffer = new StringBuilder();

            // Stack of List context - needed for efficient bullet generation
            //Stack<int> listItemCounter = null;

            ITextPointer navigator = startPosition.CreatePointer();

            Invariant.Assert(startPosition.CompareTo(endPosition) <= 0, "expecting: startPosition <= endPosition");

            while (navigator.CompareTo(endPosition) < 0)
            {
                Type elementType;

                TextPointerContext symbolType = navigator.GetPointerContext(LogicalDirection.Forward);
                switch (symbolType)
                {
                    case TextPointerContext.Text:
                        PlainConvertTextRun(textBuffer, navigator, endPosition, ref charArray);
                        break;
                    case TextPointerContext.ElementEnd:
                        elementType = navigator.ParentType;

                        //if (typeof(Paragraph).IsAssignableFrom(elementType) ||
                        //    typeof(BlockUIContainer).IsAssignableFrom(elementType))
                        //{
                        //    PlainConvertParagraphEnd(textBuffer, navigator);
                        //}
                        /*else*/ if (typeof(LineBreak).IsAssignableFrom(elementType))
                        {
                            navigator.MoveToNextContextPosition(LogicalDirection.Forward);
                            textBuffer.Append(Environment.NewLine);
                        }
                        //else if (typeof(List).IsAssignableFrom(elementType))
                        //{
                        //    PlainConvertListEnd(navigator, ref listItemCounter);
                        //}
                        else
                        {
                            // All other closing tags - just skip them
                            navigator.MoveToNextContextPosition(LogicalDirection.Forward);
                        }
                        break;
                    case TextPointerContext.EmbeddedElement :
                        textBuffer.Append('\u0020'); // Substitute SPACE for embedded objects.
                        navigator.MoveToNextContextPosition(LogicalDirection.Forward);
                        break;
                    case TextPointerContext.ElementStart :
                        elementType = navigator.GetElementType(LogicalDirection.Forward);
                        //if (typeof(AnchoredBlock).IsAssignableFrom(elementType))
                        //{
                        //    // Floaters and figures must start from a new line
                        //    textBuffer.Append(Environment.NewLine);
                        //}
                        //else if (typeof(List).IsAssignableFrom(elementType) && navigator is TextPointer)
                        //{
                        //    // New list level opens
                        //    PlainConvertListStart(navigator, ref listItemCounter);
                        //}
                        //else if (typeof(ListItem).IsAssignableFrom(elementType))
                        //{
                        //    // List items must be preceeded by a list marker
                        //    PlainConvertListItemStart(textBuffer, navigator, ref listItemCounter);
                        //}
                        //else
                        //{
                        //    PlainConvertAccessKey(textBuffer, navigator);
                        //}
                        navigator.MoveToNextContextPosition(LogicalDirection.Forward);
                        break;

                    default:
                        Invariant.Assert(false, "Unexpected vlue for TextPointerContext");
                        break;
                }
            }

            return textBuffer.ToString();
        }

        // Part of plain text converter: called from GetTextInternal when processing Text runs
        private static void PlainConvertTextRun(StringBuilder textBuffer, ITextPointer navigator, ITextPointer endPosition, ref Char[] charArray)
        {
            // Copy this text run into destination
            int runLength = navigator.GetTextRunLength(LogicalDirection.Forward);
            charArray = EnsureCharArraySize(charArray, runLength);
            runLength = TextPointerBase.GetTextWithLimit(navigator, LogicalDirection.Forward, charArray, 0, runLength, endPosition);
            textBuffer.Append(charArray, 0, runLength);
            navigator.MoveToNextContextPosition(LogicalDirection.Forward);
        }

        // Part of plain text converter: called from GetTextInternal when processing ElementEnd for Paragraph elements.
        // Outputs \n - for regular paragraphs and TableRow ends or \t for TableCell ends.
        //private static void PlainConvertParagraphEnd(StringBuilder textBuffer, ITextPointer navigator)
        //{
        //    // Check for a special case for a single paragraph within a TableCell
        //    // which must be serialized as "\t" character.
        //    navigator.MoveToElementEdge(ElementEdge.BeforeStart);
        //    bool theParagraphIsTheFirstInCollection = navigator.GetPointerContext(LogicalDirection.Backward) == TextPointerContext.ElementStart;
        //    navigator.MoveToNextContextPosition(LogicalDirection.Forward);
        //    navigator.MoveToElementEdge(ElementEdge.AfterEnd);
        //    TextPointerContext symbolType = navigator.GetPointerContext(LogicalDirection.Forward);

        //    if (theParagraphIsTheFirstInCollection && symbolType == TextPointerContext.ElementEnd &&
        //        typeof(TableCell).IsAssignableFrom(navigator.ParentType))
        //    {
        //        // This is an end of a table cell

        //        navigator.MoveToNextContextPosition(LogicalDirection.Forward);
        //        symbolType = navigator.GetPointerContext(LogicalDirection.Forward);
        //        if (symbolType == TextPointerContext.ElementStart)
        //        {
        //            // Next table cell starts after this one. Use '\t' as a cell separator
        //            textBuffer.Append('\t');
        //        }
        //        else
        //        {
        //            // This was the last cell in a row. Use '\r\n' as a line separator
        //            textBuffer.Append(Environment.NewLine);
        //        }
        //    }
        //    else
        //    {
        //        // Ordinary paragraph end
        //        textBuffer.Append(Environment.NewLine);
        //    }
        //}

        // Part of plain text converter: called from GetTextInternal when processing ElementStart for List elements.
        // Initializes a stack of list item counters and pushes a new zero for the opened list level.
        //private static void PlainConvertListStart(ITextPointer navigator, ref Stack<int> listItemCounter)
        //{
        //    List list = (List)navigator.GetAdjacentElement(LogicalDirection.Forward);

        //    // Initialize list context
        //    if (listItemCounter == null)
        //    {
        //        listItemCounter = new Stack<int>(1);
        //    }
        //    listItemCounter.Push(0);
        //}

        // Part of plain text converter: called from GetTextInternal when processing ElementEnd for Listelements
        // Pops a current value from a stack of list item indices.
        private static void PlainConvertListEnd(ITextPointer navigator, ref Stack<int> listItemCounter)
        {
            // Note that we do not expect List tag balansing:
            // We can get more List closing tags than we had opening ones -
            // it happens when range starts in the middle of a list.
            if (listItemCounter != null && listItemCounter.Count > 0)
            {
                listItemCounter.Pop();
            }
            navigator.MoveToNextContextPosition(LogicalDirection.Forward);
        }

        // Part of plain text converter: called from GetTextInternal when processing ElementStart for ListItem elements
        // Uses s stack of list items indices and updates it for following list items.
        //private static void PlainConvertListItemStart(StringBuilder textBuffer, ITextPointer navigator, ref Stack<int> listItemCounter)
        //{
        //    if (navigator is TextPointer) // can do somethinng useful only in concrete TextContainer - not in an abstract one
        //    {
        //        List list = (List)((TextPointer)navigator).Parent;
        //        ListItem listItem = (ListItem)navigator.GetAdjacentElement(LogicalDirection.Forward);

        //        // Initialize list context
        //        if (listItemCounter == null)
        //        {
        //            listItemCounter = new Stack<int>(1);
        //        }
        //        if (listItemCounter.Count == 0)
        //        {
        //            // List is taken from its middle position. Need to identify starting item number
        //            listItemCounter.Push(((IList)listItem.SiblingListItems).IndexOf(listItem));
        //        }

        //        // Get list item number
        //        Invariant.Assert(listItemCounter.Count > 0, "expectinng listItemCounter.Count > 0");
        //        int listItemIndex = listItemCounter.Pop();
        //        int indexBase = list != null ? list.StartIndex : 0;
        //        TextMarkerStyle markerStyle = list != null ? list.MarkerStyle : TextMarkerStyle.Disc;

        //        WriteListMarker(textBuffer, markerStyle, listItemIndex + indexBase);

        //        // Advance
        //        listItemIndex++;
        //        listItemCounter.Push(listItemIndex);
        //    }
        //}

        // Part of plain text converter: called from GetTextInternal when processing ElementStart for AccessKey elements
        // Uses s stack of list items indices and updates it for following list items.
        //private static void PlainConvertAccessKey(StringBuilder textBuffer, ITextPointer navigator)
        //{
        //    // Creating an "_" prefix for AccessKey character (represented as a Run with special serialization attribution)
        //    object element = navigator.GetAdjacentElement(LogicalDirection.Forward);
        //    if (AccessText.HasCustomSerialization(element))
        //    {
        //        textBuffer.Append(AccessText.AccessKeyMarker);
        //    }
        //}

        // Helper for GetTextInternal, manages a char buffer.
        // NOTE: Does not preserve the content of a buffer
        private static Char[] EnsureCharArraySize(Char[] charArray, int textLength)
        {
            if (charArray == null)
            {
                charArray = new char[textLength + 10];
            }
            else if (charArray.Length < textLength)
            {
                int newLength = charArray.Length * 2;
                if (newLength < textLength)
                {
                    newLength = textLength + 10;
                }

                charArray = new Char[newLength];
            }
            return charArray;
        }

        // Writes a text representation of a list marker
        //private static void WriteListMarker(StringBuilder textBuffer, TextMarkerStyle listMarkerStyle, int listItemNumber)
        //{
        //    string markerText = null;
        //    Char[] charArray = null;

        //    switch (listMarkerStyle)
        //    {
        //        case TextMarkerStyle.None :
        //            markerText = "";
        //            break;
        //        case TextMarkerStyle.Disc :
        //            markerText = "\x2022"; // Bullet // not a "\x9f"; 
        //            break;
        //        case TextMarkerStyle.Circle :
        //            markerText = "\x25CB"; // White Circle // not a "\xa1"; 
        //            break;
        //        case TextMarkerStyle.Square :
        //            markerText = "\x25A1"; // White Box // not a "\x71"; 
        //            break;
        //        case TextMarkerStyle.Box :
        //            markerText = "\x25A0"; // Black Box // not a "\xa7"; 
        //            break;

        //        case TextMarkerStyle.Decimal:
        //            charArray = ConvertNumberToString(listItemNumber, false, DecimalNumerics);
        //            break;

        //        case TextMarkerStyle.LowerLatin:
        //            charArray = ConvertNumberToString(listItemNumber, true, LowerLatinNumerics);
        //            break;

        //        case TextMarkerStyle.UpperLatin:
        //            charArray = ConvertNumberToString(listItemNumber, true, UpperLatinNumerics);
        //            break;

        //        case TextMarkerStyle.LowerRoman:
        //            markerText = ConvertNumberToRomanString(listItemNumber, false);
        //            break;

        //        case TextMarkerStyle.UpperRoman:
        //            markerText = ConvertNumberToRomanString(listItemNumber, true);
        //            break;
        //    }

        //    if (markerText != null)
        //    {
        //        textBuffer.Append(markerText);
        //    }
        //    else if (charArray != null)
        //    {
        //        textBuffer.Append(charArray, 0, charArray.Length);
        //    }
        //    textBuffer.Append('\t');
        //}

        private const char NumberSuffix = '.';

        private const string DecimalNumerics = "0123456789";
        private const string LowerLatinNumerics = "abcdefghijklmnopqrstuvwxyz";
        private const string UpperLatinNumerics = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";

        private static string[][] RomanNumerics = new string[][]
        {
            new string[] { "m??", "cdm", "xlc", "ivx" }, 
            new string[] { "M??", "CDM", "XLC", "IVX" }
        };

        /// <summary>
        /// Convert a number to string, consisting of digits followed by the NumberSuffix character.
        /// </summary>
        /// <param name="number">Number to convert.</param>
        /// <param name="oneBased">True if there is no zero digit (e.g., alpha numbering).</param>
        /// <param name="numericSymbols">Set of digits (e.g., 0-9 or a-z).</param>
        /// <returns>Returns the number string as an array of characters.</returns>
        private static char[] ConvertNumberToString(int number, bool oneBased, string numericSymbols)
        {
            //  Whether zero-based or one-based numbering is used affects how we
            //  count and how we determine the maximum number of values for a
            //  given number of digits.
            //
            //  The following table illustrates how counting differs. In both
            //  cases we're using base-2 numbering (i.e., two distinct digits),
            //  but with 1-based counting each of those two digits can be a
            //  significant leading digit.
            //
            //            0-based     1-based
            //    ----------------------------
            //      0           0          --
            //      1           1           a
            //      2          10           b
            //      3          11          aa
            //      4         100          ab
            //      5         101          ba
            //      6         110          bb
            //      7         111         aaa
            //      8        1000         aab
            //      9        1001         aba
            //     10        1010         abb
            //     11        1011         baa
            //     12        1100         bab
            //     13        1101         bba
            //     14        1110         bbb
            //     15        1111        aaaa
            //     16       10000        aaab
            //
            //  For zero-based counting, adding a leading zero does not change
            //  the value of a number. Thus, the set of all N-digit numbers is
            //  a proper subset of the set of (N+1)-digit numbers. Thus the set
            //  of values that can be represented by N *or fewer* digits is the
            //  same as the number of combinations of exactly N digits, i.e.,
            //
            //      b ^ N
            //
            //  where b is the base of the numbering system.
            //
            //  For one-based counting, there is no zero digit. Thus, the set
            //  of N-digit numbers and the set of (N+1)-digit numbers are
            //  disjoint sets. Thus, while the number of combinations of
            //  *exactly* N digits is still b ^ N, the maximum value that
            //  can be represented by N *or fewer* digits is:
            //
            //  Max(N)
            //      where N = 1   :   b
            //      where N > 1   :   (b ^ N) + Max(N - 1)
            //
            if (oneBased)
            {
                // Subtract 1 from 1-based numbers so we can use zero-based
                // indexing. The formula for Max(N) given above should now be
                // thought of as a limit rather than a maximum.
                --number;
            }

            Invariant.Assert(number >= 0, "expecting: number >= 0");

            char[] result;

            int b = numericSymbols.Length;
            if (number < b)
            {
                // Optimize common case of single-digit numbers.
                result = new char[2]; // digit + suffix
                result[0] = numericSymbols[number];
                result[1] = NumberSuffix;
            }
            else
            {
                // Disjoint is 1 if and only if the set of numbers with N
                // digits and the set of numbers with (N+1) digits are
                // disjoint (see comment above). Otherwise it is zero.
                int disjoint = oneBased ? 1 : 0;

                // Count digits.
                int digits = 1;
                for (int limit = b, pow = b; number >= limit; ++digits)
                {
                    pow *= b;
                    limit = pow + (limit * disjoint);
                }

                // Build string in reverse order starting with suffix.
                result = new char[digits + 1]; // digits + suffix
                result[digits] = NumberSuffix;
                for (int i = digits - 1; i >= 0; --i)
                {
                    result[i] = numericSymbols[number % b];
                    number = (number / b) - disjoint;
                }
            }

            return result;
        }

        /// <summary>
        /// Convert 1-based number to a Roman numeric string
        /// followed by NumberSuffix character.
        /// </summary>
        /// <remarks>
        /// Roman number is 1-based. The Roman numeric string is a series of symbols. Following
        /// is the list of symbols and its value.
        /// 
        ///     Symbol      Value
        ///         I           1
        ///         V           5  
        ///         X          10
        ///         L          50
        ///         C         100
        ///         D         500
        ///         M        1000
        /// 
        /// The rule of Roman number prohibits the use of more than 3 consecutive identical symbol
        /// but using subtraction of symbol standing for multiples of 10, so the value 4 is written
        /// as IV (5-1) rather than IIII. 
        /// 
        /// Due to the writing rule and the fact that the symbol represents not the numeral digit 
        /// but the value of the number. Roman number system cannot represent value larger than 3999.
        /// 
        /// See, http://www.ccsn.nevada.edu/math/ancient_systems.htm
        /// 
        /// However, there exists a more relaxing use of Roman numbers to represent values 4000 and  
        /// 4999 by using 4 consecutive M. The value 4999 is than written as 'MMMMCMXCIX'. Such use
        /// however is not widely accepted.
        /// 
        /// See, http://www.guernsey.net/~sgibbs/roman.html
        /// 
        /// For values larger than 3999, an overscore is used on the symbol to indicate 1000 multiplication.
        ///                                    ___
        /// So, value 7000 would be written as VII. This writing rule has a fair amount of disagreement
        /// since it is widely understood that it is not invented by the Romans and they rarely had a
        /// need for large numbers during their time. Furthermore, accepting this writing rule just
        /// for the sake of being able to write larger number would create a new limitation of the values
        /// greater than 3,999,999. Unicode 4.0 does not encode these overscore symbols.
        /// 
        /// See, http://www.gwydir.demon.co.uk/jo/roman/number.htm
        ///      http://www.novaroma.org/via_romana/numbers.html
        /// 
        /// Implementation-wise, IE adopts a general limitation of 3999 and simply convert the value
        /// into a regular numeric form.
        /// 
        /// We'll follow the mainstream and adopt the 3999 limit. The fallback would also do would IE does.
        /// 
        /// </remarks>
        private static string ConvertNumberToRomanString(
            int number,
            bool uppercase
            )
        {
            if (number > 3999)
            {
                // Roman numeric string not supported
                return number.ToString(CultureInfo.InvariantCulture);
            }

            StringBuilder builder = new StringBuilder();

            AddRomanNumeric(builder, number / 1000, RomanNumerics[uppercase ? 1 : 0][0]);
            number %= 1000;
            AddRomanNumeric(builder, number / 100, RomanNumerics[uppercase ? 1 : 0][1]);
            number %= 100;
            AddRomanNumeric(builder, number / 10, RomanNumerics[uppercase ? 1 : 0][2]);
            number %= 10;
            AddRomanNumeric(builder, number, RomanNumerics[uppercase ? 1 : 0][3]);

            builder.Append(NumberSuffix);

            return builder.ToString();
        }


        /// <summary>
        /// Convert number 0 - 9 into Roman numeric
        /// </summary>
        /// <param name="builder">string builder</param>
        /// <param name="number">number to convert</param>
        /// <param name="oneFiveTen">Roman numeric char for one five and ten</param>
        private static void AddRomanNumeric(
            StringBuilder builder,
            int number,
            string oneFiveTen
            )
        {
            Invariant.Assert(number >= 0 && number <= 9, "expecting: number >= 0 && number <= 9");

            if (number >= 1 && number <= 9)
            {
                if (number == 4 || number == 9)
                    builder.Append(oneFiveTen[0]);

                if (number == 9)
                {
                    builder.Append(oneFiveTen[2]);
                }
                else
                {
                    if (number >= 4)
                        builder.Append(oneFiveTen[1]);

                    for (int i = number % 5; i > 0 && i < 4; i--)
                        builder.Append(oneFiveTen[0]);
                }
            }
        }

        #endregion TextRange Helpers

        //------------------------------------------------------
        //
        // ITextRange Properties
        //
        //------------------------------------------------------

        #region ITextRange Properties

        //......................................................
        //
        //  Boundary Positions
        //
        //......................................................

        internal static ITextPointer GetStart(ITextRange thisRange)
        {
            NormalizeRange(thisRange);

            Invariant.Assert(thisRange._TextSegments != null && thisRange._TextSegments.Count > 0, "expecting nonempty _TextSegments array for Start position");
            return thisRange._TextSegments[0].Start;
        }

        internal static ITextPointer GetEnd(ITextRange thisRange)
        {
            NormalizeRange(thisRange);

            Invariant.Assert(thisRange._TextSegments != null && thisRange._TextSegments.Count > 0, "expecting nonempty _TextSegments array for End position");
            return thisRange._TextSegments[thisRange._TextSegments.Count - 1].End;
        }

        internal static bool GetIsEmpty(ITextRange thisRange)
        {
            NormalizeRange(thisRange);

            // We assume that if a range is empty then it uses the same instance
            // of TextPointer for both Start and End positions.
            Invariant.Assert(
                (thisRange._TextSegments.Count == 1 &&
                (object)thisRange._TextSegments[0].Start == (object)thisRange._TextSegments[0].End)
                ==
                (thisRange.Start.CompareTo(thisRange.End) == 0),
                "Range emptiness assumes using one instance of TextPointer for both start and end");

            return (thisRange._TextSegments.Count == 1 && 
                 (object)thisRange._TextSegments[0].Start == (object)thisRange._TextSegments[0].End);
        }

        internal static List<TextSegment> GetTextSegments(ITextRange thisRange)
        {
            // NOTE: We cannot normalize thisRange because it will rebuild a collection
            // of textSegments and will cause range move notification, leading to stack overflow.
            // Investigate how to avoid stack overflow correclty.
            //NormalizeRange(thisRange);

            return thisRange._TextSegments;
        }

        //......................................................
        //
        //  Content - rich and plain
        //
        //......................................................

        // Implementation of a getter for a ITextRange.Text property
        internal static string GetText(ITextRange thisRange)
        {
            NormalizeRange(thisRange);

            //if (!thisRange.IsTableCellRange)
            //{
                // Pretty contraversial decision: where to do this auto-expansion for first bullet inclusion:
                // here (in TextRange.get) or on TextEditor level?

                // Extend the range from its start position to include initial list marker (if any).
                // We do not do this auto-extension inside GetTextInternal to avoid undesirable
                // "bulleting" effects on random plain text (say, in Run.TextProperty serialization).
                // THis is TextRange.get_Text-specific feature.
                ITextPointer start = thisRange.Start;
                while (start.GetPointerContext(LogicalDirection.Backward) == TextPointerContext.ElementStart /*&& !typeof(AnchoredBlock).IsAssignableFrom(start.ParentType)*/)
                {
                    // Any start tag is harmless except for AnchoredBlocks that would produce extra NewLines. So don't cross them.
                    start = start.GetNextContextPosition(LogicalDirection.Backward);
                }

                return TextRangeBase.GetTextInternal(start, thisRange.End);
            //}
            //else
            //{
            //    string text;

            //    // use a StringBuilder here.
            //    text = String.Empty;
            //    for (int i = 0; i < thisRange._TextSegments.Count; i++)
            //    {
            //        TextSegment textSegment;

            //        textSegment = thisRange._TextSegments[i];

            //        text += TextRangeBase.GetTextInternal(textSegment.Start, textSegment.End);
            //        // Move the called method in this file
            //    }

            //    return text;
            //}
        }

        // Implementation of a setter fot ITextRange.Text property
        internal static void SetText(ITextRange thisRange, string textData)
        {
            NormalizeRange(thisRange);

            if (textData == null)
            {
                throw new ArgumentNullException("textData");
            }

            ITextPointer explicitInsertPosition = null;

            TextRangeBase.BeginChange(thisRange);
            try
            {
                // Delete content covered by this range
                if (!thisRange.IsEmpty)
                {
                    if (thisRange.Start is TextPointer && 
                        ((TextPointer)thisRange.Start).Parent == ((TextPointer)thisRange.End).Parent && 
                        ((TextPointer)thisRange.Start).Parent is Run && 
                        textData.Length > 0)
                    {
                        // When textrange start/end are parented by the same Run, we can optimize 
                        // and delete content without any checks. 
                        //
                        // Note that NOT doing so has a serious side effect in this case.
                        // Low-level code in TextRangeEdit does not preserve an empty run
                        // with no formatting properties after deletion. 
                        // We dont want to loose the empty Run, 
                        // when we are just about to set the range text to non-empty string.
                        // Otherwise, newly inserted text might have undesirable formatting properties 
                        // applied due to an insertion position within an adjacent Run.

                        if (thisRange.Start.GetPointerContext(LogicalDirection.Backward) == TextPointerContext.Text &&
                            thisRange.End.GetPointerContext(LogicalDirection.Forward) == TextPointerContext.Text)
                        {
                            // If we're deleting with surrounding text, make sure we insert later between the surrounding text.
                            // Because we will invalidate layout with the delete, it's possible that thisRange.Start
                            // will normalize itself to a different character offset on the next reference.
                            // This is because -- unfortunately -- when layout is valid we use ITextView.IsAtCaretUnitBoundary
                            // to normalize unicode offsets, but when layout is dirty we use a different code path
                            // that ignores the current font and simply checks Unicode values for surrogates and
                            // combining marks.  See bug 1683515 for an example.
                            explicitInsertPosition = thisRange.Start;
                        }

                        TextContainer textContainer = ((TextPointer)thisRange.Start).TextContainer;
                        textContainer.DeleteContentInternal((TextPointer)thisRange.Start, (TextPointer)thisRange.End);
                    }
                    else
                    {
                        thisRange.Start.DeleteContentToPosition(thisRange.End);
                    }

                    if (thisRange.Start is TextPointer)
                    {
                        TextRangeEdit.MergeFlowDirection((TextPointer)thisRange.Start);
                    }

                    thisRange.Select(thisRange.Start, thisRange.Start);
                }

                // Insert text at end position
                // Note that the non-emptiness check below is not an optimization:
                // In case of empty text the code block in it would change an empty range
                // orientation, which is undesirable side effect.
                // Also if the inserted text is empty we need to avoid ensuring insertion position,
                // which can create paragraphs etc.
                if (textData.Length > 0)
                {
                    ITextPointer insertPosition = (explicitInsertPosition == null) ? thisRange.Start : explicitInsertPosition;

                    // Ensure last paragraph existence and prepare ends for the new selection
                    bool pastedFragmentEndsWithNewLine = textData.EndsWith("\n", StringComparison.Ordinal);

                    // We are going to insert paragraph implicitly when the block content becomes totally empty.
                    // Store the fact that implicit paragraph was inserted to exclude ane extra paragraph break
                    // from the end of pasted fragment

                    //insertPosition is TextPointer && TextSchema.IsValidChild(/*position*/insertPosition, /*childType*/typeof(Block)) && (insertPosition.GetPointerContext(LogicalDirection.Backward) == TextPointerContext.None ||
                    bool implicitParagraphInserted =
                        (insertPosition.GetPointerContext(LogicalDirection.Backward) ==
                         TextPointerContext.ElementStart) &&
                        (insertPosition.GetPointerContext(LogicalDirection.Forward) == TextPointerContext.None ||
                         insertPosition.GetPointerContext(LogicalDirection.Forward) == TextPointerContext.ElementEnd);

                    // Make sure that the range is positioned at insertion position
                    if (insertPosition is TextPointer && explicitInsertPosition == null)
                    {
                        TextPointer insertionPosition = /*TextRangeEditTables.EnsureInsertionPosition(*/(TextPointer)insertPosition/*)*/;
                        thisRange.Select(insertionPosition, insertionPosition);
                        insertPosition = thisRange.Start;
                    }
                    Invariant.Assert(TextSchema.IsInTextContent(insertPosition), "range.Start is expected to be in text content");

                    ITextPointer newStart = insertPosition.GetFrozenPointer(LogicalDirection.Backward);
                    ITextPointer newEnd = insertPosition.CreatePointer(LogicalDirection.Forward);

                    //if ((newStart is TextPointer) && ((TextPointer)newStart).Paragraph != null)
                    //{
                    //    // Rich text - '\n' must be replaced by Paragraphs
                    //    TextPointer insertionPosition = (TextPointer)newStart.CreatePointer(LogicalDirection.Forward);
                    //    string[] textParagraphs = textData.Split(new string[] { "\r\n", "\n" }, StringSplitOptions.None);

                    //    int length = textParagraphs.Length;
                    //    if (implicitParagraphInserted && pastedFragmentEndsWithNewLine)
                    //    {
                    //        length--;
                    //    }

                    //    for (int i = 0; i < length; i++)
                    //    {
                    //        insertionPosition.InsertTextInRun(textParagraphs[i]);
                    //        if (i < length - 1)
                    //        {
                    //            if (insertionPosition.HasNonMergeableInlineAncestor)
                    //            {
                    //                // We cannot split a Hyperlink or other non-mergeable Inline element, 
                    //                // so insert a space character instead (similar to embedded object).
                    //                // Note that this means, SetText would loose 
                    //                // paragraph break information in this case.
                    //                insertionPosition.InsertTextInRun(" ");
                    //            }
                    //            else
                    //            {
                    //                // insertionPosition gets repositioned to just inside
                    //                // the following Paragraph.
                    //                insertionPosition = insertionPosition.InsertParagraphBreak();
                    //            }
                    //            // Keep newEnd in sync with the paragraph break.
                    //            // We can't rely on LogicalDirection alone for
                    //            // anything other than simple text inserts.
                    //            newEnd = insertionPosition;
                    //        }
                    //    }

                    //    if (implicitParagraphInserted && pastedFragmentEndsWithNewLine)
                    //    {
                    //        // We must include ending paragraph break into a resulting range
                    //        newEnd = newEnd.GetNextInsertionPosition(LogicalDirection.Forward);
                    //        if (newEnd == null)
                    //        {
                    //            newEnd = newStart.TextContainer.End; // set end of range to IsAfterLastParagraph position
                    //        }

                    //        // Note: As a result of this logic with implicitParagraphInserted && pastedFragmentEndsWithNewLine
                    //        // we have the following behavior:
                    //        // Given that:
                    //        //    range = new TextRange(flowDocument.ContentStart, flowDocument.ContentEnd);
                    //        // the statement:
                    //        //    range.Text = "foo\r\n";
                    //        // has the effect of leaving flowDocument with this content (note: just one paragraph):
                    //        //    <Paragraph>foo</Paragraph>
                    //        // and range selecting the whole content:
                    //        //    range.Text == "foo\r\n"
                    //        //
                    //        // the statement:
                    //        //    range.Text = "foo";
                    //        // results with the same content in flowDocument (one paragraph)
                    //        // but the range is not extended beyond last paragraph end:
                    //        //    range.Text == "foo".
                    //    }
                    //}
                    //else
                    //{
                        // Non-paragraph text - insert without '\n' conversion
                        newStart.InsertTextInRun(textData);
                    //}

                    // Select the range
                    TextRangeBase.SelectPrivate(thisRange, newStart, newEnd, /*includeCellAtMovingPosition:*/false, /*markRangeChanged*/true);
                }
            }
            finally
            {
                TextRangeBase.EndChange(thisRange);
            }
        }

        //internal static string GetXml(ITextRange thisRange)
        //{
        //    NormalizeRange(thisRange);

        //    // Create XmlWriter
        //    StringWriter stringWriter = new StringWriter(CultureInfo.InvariantCulture);
        //    XmlTextWriter xmlWriter = new XmlTextWriter(stringWriter);

        //    TextRangeSerialization.WriteXaml(xmlWriter, thisRange, /*useFlowDocumentAsRoot:*/false, /*wpfPayload:*/null);

        //    return stringWriter.ToString();
        //}

        //internal static bool CanSave(ITextRange thisRange, string dataFormat)
        //{
        //    NormalizeRange(thisRange);

        //    bool canSave = (
        //        dataFormat == DataFormats.Text ||
        //        dataFormat == DataFormats.Xaml ||
        //        ((dataFormat == DataFormats.XamlPackage ||
        //            dataFormat == DataFormats.Rtf)));

        //    return canSave;
        //}

        //internal static bool CanLoad(ITextRange thisRange, string dataFormat)
        //{
        //    NormalizeRange(thisRange);

        //    bool canLoad = (
        //        dataFormat == DataFormats.Text ||
        //        dataFormat == DataFormats.Xaml ||
        //        ((dataFormat == DataFormats.XamlPackage ||
        //            dataFormat == DataFormats.Rtf)));

        //    return canLoad;
        //}

        //internal static void Save(ITextRange thisRange, Stream stream, string dataFormat, bool preserveTextElements)
        //{
        //    if (stream == null)
        //    {
        //        throw new ArgumentNullException("stream");
        //    }
        //    if (dataFormat == null)
        //    {
        //        throw new ArgumentNullException("dataFormat");
        //    }

        //    NormalizeRange(thisRange);

        //    if (dataFormat == DataFormats.Text)
        //    {
        //        string text = thisRange.Text;
        //        StreamWriter textStreamWriter = new StreamWriter(stream);
        //        textStreamWriter.Write(text);
        //        textStreamWriter.Flush();
        //    }
        //    else if (dataFormat == DataFormats.Xaml)
        //    {
        //        StreamWriter xamlStreamWriter = new StreamWriter(stream);
        //        XmlTextWriter xamlXmlWriter = new XmlTextWriter(xamlStreamWriter);
        //        // Passing null as wpfPayload parameter we request to produce
        //        // xaml without images - all of them will be repllaced by whitespaces.
        //        TextRangeSerialization.WriteXaml(xamlXmlWriter, thisRange, /*useFlowDocumentAsRoot:*/false, /*wpfPayload:*/null, preserveTextElements);
        //        xamlXmlWriter.Flush();
        //    }
        //    else if (dataFormat == DataFormats.XamlPackage)
        //    {
        //        // Non-null stream here means unconditional request to create a WPF package for the range
        //        // independently whether there are images in it or not.
        //        WpfPayload.SaveRange(thisRange, ref stream, /*useFlowDocumentAsRoot:*/false, preserveTextElements);
        //    }
        //    else if (dataFormat == DataFormats.Rtf)
        //    {
        //        Stream wpfPayloadMemory = null;
        //        // Passing null as a wpfPayloadStream we allow to not create wpf package
        //        // when it is not needed (there is no images in the range)
        //        string xamlText = WpfPayload.SaveRange(thisRange, ref wpfPayloadMemory, /*useFlowDocumentAsRoot:*/false);
        //        // Convert xaml to rtf text to set rtf data into data object.
        //        string rtfText = TextEditorCopyPaste.ConvertXamlToRtf(xamlText, wpfPayloadMemory);
        //        StreamWriter rtfStreamWriter = new StreamWriter(stream);
        //        rtfStreamWriter.Write(rtfText);
        //        rtfStreamWriter.Flush();
        //    }
        //    else
        //    {
        //        // Unsupported format - thows exception
        //        throw new ArgumentException(SR.Get(SRID.TextRange_UnsupportedDataFormat, dataFormat), "dataFormat");
        //    }
        //}

        //internal static void Load(TextRange thisRange, Stream stream, string dataFormat)
        //{
        //    if (stream == null)
        //    {
        //        throw new ArgumentNullException("stream");
        //    }
        //    if (dataFormat == null)
        //    {
        //        throw new ArgumentNullException("dataFormat");
        //    }

        //    NormalizeRange(thisRange);

        //    // Reset the stream position to the beginning
        //    if (stream.CanSeek)
        //    {
        //        stream.Seek(0, SeekOrigin.Begin);
        //    }

        //    if (dataFormat == DataFormats.Text)
        //    {
        //        StreamReader textStreamReader = new StreamReader(stream);
        //        string text = textStreamReader.ReadToEnd();
        //        thisRange.Text = text;
        //    }
        //    else if (dataFormat == DataFormats.Xaml)
        //    {
        //        StreamReader xamlStreamReader = new StreamReader(stream);
        //        string xamlText = xamlStreamReader.ReadToEnd();
        //        thisRange.Xml = xamlText;
        //    }
        //    else if (dataFormat == DataFormats.XamlPackage)
        //    {
        //        object element = WpfPayload.LoadElement(stream);
        //        if (!(element is BitVector32.Section) && !(element is Span))
        //        {
        //            throw new ArgumentException(SR.Get(SRID.TextRange_UnrecognizedStructureInDataFormat, dataFormat), "stream");
        //        }
        //        thisRange.SetXmlVirtual((TextElement)element);
        //    }
        //    else if (dataFormat == DataFormats.Rtf)
        //    {
        //        // Need to use streams instead of intrermediate strings
        //        StreamReader rtfStreamReader = new StreamReader(stream);
        //        string rtfText = rtfStreamReader.ReadToEnd();
        //        MemoryStream memoryStream = TextEditorCopyPaste.ConvertRtfToXaml(rtfText);
        //        if (memoryStream == null)
        //        {
        //            throw new ArgumentException(SR.Get(SRID.TextRange_UnrecognizedStructureInDataFormat, dataFormat), "stream");
        //        }
        //        TextElement textElement = WpfPayload.LoadElement(memoryStream) as TextElement;
        //        if (!(textElement is BitVector32.Section) && !(textElement is Span))
        //        {
        //            throw new ArgumentException(SR.Get(SRID.TextRange_UnrecognizedStructureInDataFormat, dataFormat), "stream");
        //        }
        //        thisRange.SetXmlVirtual(textElement);
        //    }
        //    else
        //    {
        //        // Unsupported format - thows exception
        //        throw new ArgumentException(/*SR.Get(SRID.TextRange_UnsupportedDataFormat, dataFormat), "dataFormat"*/);
        //    }
        //}

        // Ref count of open change blocks -- incremented/decremented
        // around BeginChange/EndChange calls.
        internal static int GetChangeBlockLevel(ITextRange thisRange)
        {
            return thisRange._ChangeBlockLevel;
        }

        //......................................................
        //
        //  Embedded Object Selection
        //
        //......................................................

        internal static StyledElement GetUIElementSelected(ITextRange range)
        {
            ITextPointer start = range.Start.CreatePointer();
            TextPointerContext context = start.GetPointerContext(LogicalDirection.Forward);
            while (context == TextPointerContext.ElementStart || context == TextPointerContext.ElementEnd)
            {
                start.MoveToNextContextPosition(LogicalDirection.Forward);
                context = start.GetPointerContext(LogicalDirection.Forward);
            }
            if (context == TextPointerContext.EmbeddedElement)
            {
                ITextPointer end = range.End.CreatePointer();
                context = end.GetPointerContext(LogicalDirection.Backward);
                while (context == TextPointerContext.ElementStart || context == TextPointerContext.ElementEnd)
                {
                    end.MoveToNextContextPosition(LogicalDirection.Backward);
                    context = end.GetPointerContext(LogicalDirection.Backward);
                }
                if (context == TextPointerContext.EmbeddedElement && start.GetOffsetToPosition(end) == 1)
                {
                    return start.GetAdjacentElement(LogicalDirection.Forward) as StyledElement;
                }
            }
            return null;
        }

        //......................................................
        //
        //  Table Selection Properties
        //
        //......................................................

        //internal static bool GetIsTableCellRange(ITextRange thisRange)
        //{
        //    NormalizeRange(thisRange);

        //    return thisRange._IsTableCellRange;
        //}

        #endregion ITextRange Properties

        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------

        #region Private Methods

        // Worker for the BeginChange/BeginChangeNoUndo variants.
        // If description is null, no default undo unit is opened.
        private static void BeginChangeWorker(ITextRange thisRange, string description)
        {
            ITextContainer textContainer = thisRange.Start.TextContainer;

            if (description != null && thisRange._ChangeBlockUndoRecord == null && thisRange._ChangeBlockLevel == 0)
            {
                thisRange._ChangeBlockUndoRecord = new ChangeBlockUndoRecord(textContainer, description);
            }

            Invariant.Assert(thisRange._ChangeBlockLevel > 0 || !thisRange._IsChanged, "_changed must be false on new move sequence");
            thisRange._ChangeBlockLevel++;

            if (description != null)
            {
                textContainer.BeginChange();
            }
            else
            {
                textContainer.BeginChangeNoUndo();
            }
        }

        // Creates a one-segment collection from a pair of text positions
        // The segment normalization is done by the following rules:
        // 1. If start and end pointers have equal positions, or became equal after normalization,
        //    then the segment uses one instance of ITextPointer for both ends,
        //    which guarantees the segment emptiness in the subsequente editing
        //    around it. This single pointer takes orientation from start parameter
        //    and normalized in that direction.
        // 2. In case when a segment is non-empty, two positions will be created
        //    and normalized inward - towards a segment contents;
        //    Their gravities will be directed outward (start - Backward, end -  Forward),
        //    so that any insertion happend at segment edge position goes inside a segment
        //    - the behavior we need to inserting stuff into TextRanges.
        private static void CreateNormalizedTextSegment(ITextRange thisRange, ITextPointer start, ITextPointer end)
        {
            ValidationHelper.VerifyPositionPair(start, end);

            // Normalize the segment
            if (start.CompareTo(end) == 0)
            {
                // When the range is empty we must keep it that way during normalization
                if (!IsAtNormalizedPosition(thisRange, start, start.LogicalDirection))
                {
                    start = GetNormalizedPosition(thisRange, start, start.LogicalDirection);
                    end = start;
                } 
            }
            else
            {
                start = GetNormalizedPosition(thisRange, start, LogicalDirection.Forward);
                //if (!TextPointerBase.IsAfterLastParagraph(end))
                //{
                    // NOTE: Position after the last paragraph is special.
                    // Even though this position is not valid insertion position,
                    // we allow ranges to reach it. This is necessary to be able
                    // to "select all" content, and select the last paragraph.
                    end = GetNormalizedPosition(thisRange, end, LogicalDirection.Backward);
                //}

                // Collapse range in case of overlapped normalization result
                if (start.CompareTo(end) >= 0)
                {
                    // The range is effectuvely empty, so collapse it to single pointer instance
                    if (start.LogicalDirection == LogicalDirection.Backward)
                    {
                        // Choose a position normalized backward,
                        start = end.GetFrozenPointer(LogicalDirection.Backward);

                        // NOTE that otherwise we will use start position,
                        // which is oriented and normalizd Forward
                    }
                    end = start;
                }
                else
                {
                    // Handle Floater/Figure boundaries: non-empty ranges never cross them
                    if (start is TextPointer)
                    {
                        TextPointer adjustedStart = (TextPointer)start;
                        TextPointer adjustedEnd = (TextPointer)end;
                        //NormalizeAnchoredBlockBoundaries(ref adjustedStart, ref adjustedEnd);
                        start = adjustedStart;
                        end = adjustedEnd;
                    }

                    Invariant.Assert(start.CompareTo(end) <= 0, "expecting start <= end");

                    // Normalize the segment, start and end may have become equal now.
                    if (start.CompareTo(end) == 0)
                    {
                        // When the range is empty we must keep it that way during normalization
                        if (!IsAtNormalizedPosition(thisRange, start, start.LogicalDirection))
                        {
                            start = GetNormalizedPosition(thisRange, start, start.LogicalDirection);
                            end = start;
                        }
                    }

                    // Finalize the invariant assertions for pointer normalization in case of incomplete content (like empty List).
                    // Note: we cannot guarantee pointer normalization because of potentially incomplete content
                    if (!(TextPointerBase.IsAtInsertionPosition(start, LogicalDirection.Forward)))
                    {
                        Invariant.Assert(TextPointerBase.IsAtInsertionPosition(start, LogicalDirection.Forward));
                    }
                    if (!(TextPointerBase.IsAtInsertionPosition(end, LogicalDirection.Backward) /*|| TextPointerBase.IsAfterLastParagraph(end)*/))
                    {
                        Invariant.Assert(TextPointerBase.IsAtInsertionPosition(end, LogicalDirection.Backward) /*|| TextPointerBase.IsAfterLastParagraph(end)*/);
                    }
                }
            }

            // Set this text segment as a selected range
            // Consider reusing existing TextSegments list to avoid unnecessary allocation (perf)
            thisRange._TextSegments = new List<TextSegment>(1);
            thisRange._TextSegments.Add(new TextSegment(start, end));
            //thisRange._IsTableCellRange = false;
        }

        private static bool IsAtNormalizedPosition(ITextRange thisRange, ITextPointer position, LogicalDirection direction)
        {
            bool isAtNormalizedPosition;

            if (thisRange.IgnoreTextUnitBoundaries)
            {
                isAtNormalizedPosition = TextPointerBase.IsAtFormatNormalizedPosition(position, direction);
            }
            else
            {
                isAtNormalizedPosition = TextPointerBase.IsAtInsertionPosition(position, direction);
            }

            return isAtNormalizedPosition;
        }

        private static ITextPointer GetNormalizedPosition(ITextRange thisRange, ITextPointer position, LogicalDirection direction)
        {
            ITextPointer normalizedPosition;

            if (thisRange.IgnoreTextUnitBoundaries)
            {
                normalizedPosition = position.GetFormatNormalizedPosition(direction);
            }
            else
            {
                normalizedPosition = position.GetInsertionPosition(direction);
            }

            return normalizedPosition;
        }

        // Helper for CreateNormalizedTextSegment
        // Checks whether start and end cross any Floater/Figure boundaries.
        // If yes, normalizes the position(s) so that a non-empty range never crosses Floater/Figure boundaries.
        // Returns true if the range crosses AnchoredBlock boundary and was adjusted to not do so.
        // Returns false if it does not cross AnchoredBlock boundary and start/end stay where they were.
        //internal static void NormalizeAnchoredBlockBoundaries(ref TextPointer start, ref TextPointer end)
        //{
        //    // Check AnchoredBlocks ancestors at start
        //    TextElement outerAnchoredBlock = start.Parent as TextElement;
        //    while (outerAnchoredBlock != null)
        //    {
        //        // Find the next ancestor AncoredBlock
        //        while (outerAnchoredBlock != null && !typeof(AnchoredBlock).IsAssignableFrom(outerAnchoredBlock.GetType()))
        //        {
        //            outerAnchoredBlock = outerAnchoredBlock.Parent as TextElement;
        //        }

        //        if (outerAnchoredBlock != null)
        //        {
        //            // Anchored block found. Check whether the other position belongs to it.
        //            AnchoredBlock innerAnchoredBlock = null;
        //            TextElement innerElement = end.Parent as TextElement;
        //            while (innerElement != null && innerElement != outerAnchoredBlock)
        //            {
        //                if (innerElement is AnchoredBlock)
        //                {
        //                    innerAnchoredBlock = (AnchoredBlock)innerElement;
        //                }
        //                innerElement = innerElement.Parent as TextElement;
        //            }
        //            if (innerElement == outerAnchoredBlock)
        //            {
        //                // Common ancestor AnchoredBlock is found.
        //                if (innerAnchoredBlock != null)
        //                {
        //                    end = innerAnchoredBlock.ElementEnd;
        //                }
        //                return;
        //            }

        //            // The AnchoredElement found at start position does not include end.
        //            // Expand start to include the whole outerAnchoredBlock
        //            start = outerAnchoredBlock.ElementStart;

        //            // and go to the next possible AnchoredBlock level
        //            outerAnchoredBlock = outerAnchoredBlock.Parent as TextElement;
        //        }
        //    }

        //    // Check AnchoredBlocks ancestors at end
        //    outerAnchoredBlock = end.Parent as TextElement;
        //    while (outerAnchoredBlock != null)
        //    {
        //        // Find the next ancestor AncoredBlock
        //        while (outerAnchoredBlock != null && !typeof(AnchoredBlock).IsAssignableFrom(outerAnchoredBlock.GetType()))
        //        {
        //            outerAnchoredBlock = outerAnchoredBlock.Parent as TextElement;
        //        }

        //        if (outerAnchoredBlock != null)
        //        {
        //            // Anchored block found. Check whether the other position belongs to it.
        //            AnchoredBlock innerAnchoredBlock = null;
        //            TextElement innerElement = start.Parent as TextElement;
        //            while (innerElement != null && innerElement != outerAnchoredBlock)
        //            {
        //                if (innerElement is AnchoredBlock)
        //                {
        //                    innerAnchoredBlock = (AnchoredBlock)innerElement;
        //                }
        //                innerElement = innerElement.Parent as TextElement;
        //            }
        //            if (innerElement == outerAnchoredBlock)
        //            {
        //                // Common ancestor AnchoredBlock is found.
        //                if (innerAnchoredBlock != null)
        //                {
        //                    start = innerAnchoredBlock.ElementStart;
        //                }
        //                return;
        //            }

        //            // The AnchoredElement found at end position does not include start.
        //            // Expand end to include the whole outerAnchoredBlock
        //            end = outerAnchoredBlock.ElementEnd;

        //            // and go to the next possible AnchoredBlock level
        //            outerAnchoredBlock = outerAnchoredBlock.Parent as TextElement;
        //        }
        //    }
        //}

        // Method used in all public entry points to
        // ensure that thisRange is really normalized appropriately.
        private static void NormalizeRange(ITextRange thisRange)
        {
            if (thisRange._ContentGeneration == thisRange._TextSegments[0].Start.TextContainer.Generation)
            {
                // There were no content changes since range has been built,
                // so no normalization needed.
                return;
            }

            ITextPointer start = thisRange._TextSegments[0].Start;
            ITextPointer end = thisRange._TextSegments[thisRange._TextSegments.Count - 1].End;

            //if (thisRange._IsTableCellRange)
            //{
            //    Invariant.Assert(thisRange._TextSegments[0].Start is TextPointer);

            //    // Table range - normalization may lead to full range rebuild
            //    TextRangeEditTables.IdentifyValidBoundaries(thisRange, out start, out end);

            //    // We cannot open a change block here (even though the content of thisRange may change),
            //    // because it will cause another normalization request, leading to stack overflow.
            //    SelectPrivate(thisRange, start, end, /*includeCellAtMovingPosition:*/false, /*markRangeChanged*/false);
            //}
            //else
            //{
                // Text range - normalization on both ends must be ensured
                bool needNormalization = false;

                if ((object)start == (object)end)
                {
                    if (!TextPointerBase.IsAtInsertionPosition(start, start.LogicalDirection))
                    {
                        // Empty range can be normalized in any direction,
                        // so we use direction-neutral predicate
                        needNormalization = true;
                    }
                }
                else if (start.CompareTo(end) == 0)
                {
                    // The range which initially was not empty is not collapsed,
                    // so we need to re-create it to use one TextPointer instance
                    // instead of two.
                    // Note that gravity for the start pointer is Backward
                    // in this case - so that resulting caret will be normalized/
                    // oriented backward.
                    needNormalization = true;
                }
                else if (
                    !TextPointerBase.IsAtInsertionPosition(start, LogicalDirection.Forward) ||
                    !TextPointerBase.IsAtInsertionPosition(end, LogicalDirection.Backward))
                {
                    // If for a non-empty range, start/end are not at insertion position,
                    // we need to normalize it.
                    needNormalization = true;
                }

                if (needNormalization)
                {
                    CreateNormalizedTextSegment(thisRange, start, end);
                }
            //}

            // Store content generation which will be needed in range normalization for
            // avoiding unnecessary work.
            thisRange._ContentGeneration = thisRange._TextSegments[0].Start.TextContainer.Generation;
        }

        // Implementation body of range Select method
        // When the range is being built for the very first time OR a table range is being rebuilt during normalization, 
        // we do not fire any range notifications.
        // NOTE: Because this method is called from NormalizeRange method we should totally avoid calling
        // any ITextRange methods involving normalization (such as Start/End)
        private static void SelectPrivate(ITextRange thisRange, ITextPointer position1, ITextPointer position2, bool includeCellAtMovingPosition, bool markRangeChanged)
        {
            //List<TextSegment> textSegments = null;
            //bool isTableCellRange;

            Invariant.Assert(position1 != null, "null check: position1");
            Invariant.Assert(position2 != null, "null check: position2");

            //if (position1 is TextPointer)
            //{
            //    textSegments = TextRangeEditTables.BuildTableRange(
            //        /*anchorPosition:*/(TextPointer)position1, 
            //        /*movingPosition:*/(TextPointer)position2, 
            //        includeCellAtMovingPosition, 
            //        out isTableCellRange);
            //}
            //else
            //{
            //    // We have abstract TextContainer - never expect/build table range in this case
            //    Invariant.Assert(!thisRange._IsTableCellRange, "range is not expected to be in IsTableCellRange state - 1");
            //    textSegments = null;
            //    isTableCellRange = false;
            //}

            //if (textSegments != null)
            //{
            //    // Note also that table range is always in normalized condition - by construction
            //    // Check though whether normalization really happens when one end is outside of a table?
            //    thisRange._TextSegments = textSegments;
            //    thisRange._IsTableCellRange = isTableCellRange;
            //}
            //else
            //{
                // Simple textsegment case
                ITextPointer newStart = position1;
                ITextPointer newEnd = position2;

                // Swap pointers to order them properly.
                // We do not sewap them when they are equal,
                // so that for empty range we are predictable about
                // collapsed position orientation - taken from the first parameter
                // (position1) (as per CreateNormalizedTextSegment semantics).
                if (position1.CompareTo(position2) > 0)
                {
                    newStart = position2;
                    newEnd = position1;
                }

                // Create new segment. Note that we do this unconditionally,
                // not trying to bypass it if new positions look the same as currently set,
                // because we need to ensure range normalization here.
                TextRangeBase.CreateNormalizedTextSegment(thisRange, newStart, newEnd);
                //Invariant.Assert(!thisRange._IsTableCellRange, "Expecting that the range is in text segment state now - must be set by CreateNOrmalizedTextSegment");

                // Before setting final range state we need to check if we are still in TextSegment condition
                //if (position1 is TextPointer)
                //{
                    //ITextPointer finalStart = thisRange._TextSegments[0].Start;
                    //ITextPointer finalEnd = thisRange._TextSegments[thisRange._TextSegments.Count - 1].End;
                    //if (finalStart.CompareTo(newStart) != 0 || finalEnd.CompareTo(newEnd) != 0)
                    //{
                    //    // This means that as a result of position normalization they have been moved
                    //    // so we must check what is the range state now.
                    //    // NOTE: We are in TextSegment state now, so anchor/moving ordering is not important,
                    //    // so we use thisRange.Start/End (normalized) whose order may be different from
                    //    // position1/position2.
                    //    // Note: we use includeCellAtMovingPosition=false here because the movingPosition is taken from a constructed table range, not from input
                    //    textSegments = TextRangeEditTables.BuildTableRange(
                    //        /*anchorPosition:*/(TextPointer)finalStart, 
                    //        /*movingPosition:*/(TextPointer)finalEnd, 
                    //        /*includeCellAtMovingPosition:*/false, 
                    //        out isTableCellRange);
                    //    if (textSegments != null)
                    //    {
                    //        thisRange._TextSegments = textSegments;
                    //        thisRange._IsTableCellRange = isTableCellRange;
                    //    }
                    //}
                //}
            //}

            // Store content generation which will be needed in range normalization for
            // avoiding unnecessary work.
            thisRange._ContentGeneration = thisRange._TextSegments[0].Start.TextContainer.Generation;

            if (markRangeChanged)
            {
                // Do not fire Changed notification if the new range is the same as previous.
                TextRangeBase.MarkRangeChanged(thisRange);
            }
        }

        /// <summary>
        /// Raises the Changed event for this range.
        /// 
        /// It must be called within a BeginChange/EndChange
        /// block.
        /// </summary>
        private static void MarkRangeChanged(ITextRange thisRange)
        {
            Invariant.Assert(thisRange._ChangeBlockLevel > 0, "changeBlockLevel > 0 is expected");
            thisRange._IsChanged = true;
        }

        #endregion Private Methods
    }
}
