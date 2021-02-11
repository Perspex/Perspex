// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: ITextPointer helper methods.
//

using Avalonia;
using Avalonia.Documents;
using Avalonia.Media.TextFormatting;
using Avalonia.VisualTree;

namespace System.Windows.Documents
{
    using System;
    using MS.Internal;
    //using MS.Internal.Documents;
    using System.Globalization;
    //using System.Windows.Media; // Matrix
    //using System.Windows.Controls; // TextBlock

    // ITextPointer helper methods.
    internal static class TextPointerBase
    {
        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------

        #region Internal Methods

        /// <summary>
        /// Returns the preceeding TextPointer of a pair.
        /// </summary>
        /// <param name="position1">
        /// TextPointer to compare.
        /// </param>
        /// <param name="position2">
        /// TextPointer to compare.
        /// </param>
        internal static ITextPointer Min(ITextPointer position1, ITextPointer position2)
        {
            return position1.CompareTo(position2) <= 0 ? position1 : position2;
        }

        /// <summary>
        /// Returns the following TextPointer of a pair.
        /// </summary>
        /// <param name="position1">
        /// TextPointer to compare.
        /// </param>
        /// <param name="position2">
        /// TextPointer to compare.
        /// </param>
        internal static ITextPointer Max(ITextPointer position1, ITextPointer position2)
        {
            return position1.CompareTo(position2) >= 0 ? position1 : position2;
        }

        // Worker for GetText, accepts any ITextPointer.
        internal static string GetTextInRun(ITextPointer position, LogicalDirection direction)
        {
            char[] text;
            int textLength;
            int getTextLength;

            textLength = position.GetTextRunLength(direction);
            text = new char[textLength];

            getTextLength = position.GetTextInRun(direction, text, 0, textLength);
            Invariant.Assert(getTextLength == textLength, "textLengths returned from GetTextRunLength and GetTextInRun are innconsistent");

            return new string(text);
        }

        // Like GetText, excepts also accepts a limit parameter -- no text is returned past
        // this second position.
        // limit may be null, in which case it is ignored.
        internal static int GetTextWithLimit(ITextPointer thisPointer, LogicalDirection direction, char[] textBuffer, int startIndex, int count, ITextPointer limit)
        {
            int charsCopied;

            if (limit == null)
            {
                // No limit, just call GetText.
                charsCopied = thisPointer.GetTextInRun(direction, textBuffer, startIndex, count);
            }
            else if (direction == LogicalDirection.Forward && limit.CompareTo(thisPointer) <= 0)
            {
                // Limit completely blocks the read.
                charsCopied = 0;
            }
            else if (direction == LogicalDirection.Backward && limit.CompareTo(thisPointer) >= 0)
            {
                // Limit completely blocks the read.
                charsCopied = 0;
            }
            else
            {
                int maxCount;

                // Get an upper bound on the amount of text to copy.
                // Since GetText always stops on non-text boundaries, it's
                // ok if the count too high, it will get truncated anyways.
                if (direction == LogicalDirection.Forward)
                {
                    maxCount = Math.Min(count, thisPointer.GetOffsetToPosition(limit));
                }
                else
                {
                    maxCount = Math.Min(count, limit.GetOffsetToPosition(thisPointer));
                }
                maxCount = Math.Min(count, maxCount);

                charsCopied = thisPointer.GetTextInRun(direction, textBuffer, startIndex, maxCount);
            }

            return charsCopied;
        }

#if UNUSED
        // Returns true if the pointer is at an insertion position or next to
        // any unicode code point.  A useful performance win over 
        // IsAtInsertionPosition when only formatting scopes are important.
        internal static bool IsAtFormatNormalizedPosition(ITextPointer position)
        {
            return IsAtNormalizedPosition(position, false /* respectCaretUnitBoundaries */);
        }
#endif

        /// <summary>
        /// Return true if this TextPointer is adjacent to a character.
        /// </summary>
        /// <value>
        /// True if this TextPointer is adjacent to a unit boundary, false otherwise.
        /// </value>
        internal static bool IsAtInsertionPosition(ITextPointer position)
        {
            return IsAtNormalizedPosition(position, true /* respectCaretUnitBoundaries */);
        }

        // Tests if a position is between structural symbols where Run is potentially insertable,
        // but not present.
        // Positions in between AnchoredBlocks/InlineUIContainers/ParagraphEdges/SpanEdges
        internal static bool IsAtPotentialRunPosition(ITextPointer position)
        {
            bool isAtPotentialRunPosition = IsAtPotentialRunPosition(position, position);

            //if (!isAtPotentialRunPosition)
            //{
            //    // Test for positions inside 
            //    //  1. empty table cell 
            //    //  2. empty list item or
            //    //  3. empty flow document
            //    // They are a valid caret stop. 
            //    // Editing operations are permitted at such positions since it is a potential insertion position.
            //    isAtPotentialRunPosition = IsAtPotentialParagraphPosition(position);
            //}

            return isAtPotentialRunPosition;
        }

        // Tests whether this element is a Run inserted at potential run position.
        // It is used in decidint whether the empty element can be removed
        // or not. The Run that is at potential run position should not
        // be removed as it would cause a loss of formatting data at this
        // position.
        internal static bool IsAtPotentialRunPosition(TextElement run)
        {
            return 
                run is Run && 
                run.IsEmpty && 
                IsAtPotentialRunPosition(run.ElementStart, run.ElementEnd);
        }


        // Worker implementing IsAtPotentialRunPosition(position) method.
        // It is used for testing whether an empty Run element is at potential run potision.
        // For this purpose the method is supposed to be called with
        // backwardPosition==run.ElementStart and forwardPosition==run.ElementEnd.
        private static bool IsAtPotentialRunPosition(ITextPointer backwardPosition, ITextPointer forwardPosition)
        {
            Invariant.Assert(backwardPosition.HasEqualScope(forwardPosition));

            if (TextSchema.IsValidChild(/*position*/backwardPosition, /*childType*/typeof(Run)))
            {
                Type forwardType = forwardPosition.GetElementType(LogicalDirection.Forward);
                Type backwardType = backwardPosition.GetElementType(LogicalDirection.Backward);
                if (forwardType != null && backwardType != null)
                {
                    TextPointerContext forwardContext = forwardPosition.GetPointerContext(LogicalDirection.Forward);
                    TextPointerContext backwardContext = backwardPosition.GetPointerContext(LogicalDirection.Backward);
                    if (// Test if the position inside empty Paragraph or Span
                        backwardContext == TextPointerContext.ElementStart &&
                        forwardContext == TextPointerContext.ElementEnd
                        ||
                        // Test if the position between opening tag and an embedded object
                        backwardContext == TextPointerContext.ElementStart && TextSchema.IsNonFormattingInline(forwardType) &&
                        !IsAtNonMergeableInlineStart(backwardPosition)
                        ||
                        // Test if the position between an embedded object and a closing tag
                        forwardContext == TextPointerContext.ElementEnd && TextSchema.IsNonFormattingInline(backwardType) &&
                        !IsAtNonMergeableInlineEnd(forwardPosition)
                        ||
                        // Test if the position between two embedded objects
                        backwardContext == TextPointerContext.ElementEnd && forwardContext == TextPointerContext.ElementStart &&
                        TextSchema.IsNonFormattingInline(backwardType) && TextSchema.IsNonFormattingInline(forwardType)
                        ||
                        // Test if the position is adjacent to a non-mergeable inline (Hyperlink).
                        backwardContext == TextPointerContext.ElementEnd &&
                        typeof(Inline).IsAssignableFrom(backwardType) && !TextSchema.IsMergeableInline(backwardType) && !typeof(Run).IsAssignableFrom(forwardType) &&
                        (forwardContext != TextPointerContext.ElementEnd || !IsAtNonMergeableInlineEnd(forwardPosition))
                        ||
                        forwardContext == TextPointerContext.ElementStart &&
                        typeof(Inline).IsAssignableFrom(forwardType) && !TextSchema.IsMergeableInline(forwardType) && !typeof(Run).IsAssignableFrom(backwardType) &&
                        (backwardContext != TextPointerContext.ElementStart || !IsAtNonMergeableInlineStart(backwardPosition))
                        )
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        // Tests if a position is between structural symbols where Paragraph is potentially insertable,
        // but not present.
        // Positions in empty list item, table cell or flow document.
        //internal static bool IsAtPotentialParagraphPosition(ITextPointer position)
        //{
        //    Type parentType = position.ParentType;
        //    TextPointerContext backwardContext = position.GetPointerContext(LogicalDirection.Backward);
        //    TextPointerContext forwardContext = position.GetPointerContext(LogicalDirection.Forward);

        //    if (backwardContext == TextPointerContext.ElementStart && forwardContext == TextPointerContext.ElementEnd)
        //    {
        //        return
        //            typeof(ListItem).IsAssignableFrom(parentType) ||
        //            typeof(TableCell).IsAssignableFrom(parentType);                    
        //    }
        //    else if (backwardContext == TextPointerContext.None && forwardContext == TextPointerContext.None)
        //    {
        //        return
        //            typeof(FlowDocumentView).IsAssignableFrom(parentType) ||
        //            typeof(FlowDocument).IsAssignableFrom(parentType);
        //    }

        //    return false;
        //}

        // Tests if position is before the first Table element in a collection of Blocks at that level.
        // We treat this as a potential insertion position to allow editing operations before the table.
        // This property identifies such a position.
        //internal static bool IsBeforeFirstTable(ITextPointer position)
        //{
        //    TextPointerContext forwardContext = position.GetPointerContext(LogicalDirection.Forward);
        //    TextPointerContext backwardContext = position.GetPointerContext(LogicalDirection.Backward);

        //    return (forwardContext == TextPointerContext.ElementStart &&
        //            (backwardContext == TextPointerContext.ElementStart || backwardContext == TextPointerContext.None) &&
        //            typeof(Table).IsAssignableFrom(position.GetElementType(LogicalDirection.Forward)));
        //}

        // Tests if position is parented by a BlockUIContainer element.
        // We allow caret stops around BlockUIContainer, to permit editing operations on its boundaries.
        //internal static bool IsInBlockUIContainer(ITextPointer position)
        //{
        //    return (typeof(BlockUIContainer).IsAssignableFrom(position.ParentType));
        //}

        //internal static bool IsAtBlockUIContainerStart(ITextPointer position)
        //{
        //    return IsInBlockUIContainer(position) &&
        //        position.GetPointerContext(LogicalDirection.Backward) == TextPointerContext.ElementStart;
        //}

        //internal static bool IsAtBlockUIContainerEnd(ITextPointer position)
        //{
        //    return IsInBlockUIContainer(position) &&
        //        position.GetPointerContext(LogicalDirection.Forward) == TextPointerContext.ElementEnd;
        //}

        // Walks parents from this position until it hits limiting ancestor type.
        private static bool IsInAncestorScope(ITextPointer position, Type allowedParentType, Type limitingType)
        {
            ITextPointer navigator = position.CreatePointer();
            Type parentType = navigator.ParentType;

            while (parentType != null && allowedParentType.IsAssignableFrom(parentType))
            {
                if (limitingType.IsAssignableFrom(parentType))
                {
                    return true;
                }
                navigator.MoveToElementEdge(ElementEdge.BeforeStart);
                parentType = navigator.ParentType;
            }

            return false;
        }

        // Returns true if position is within the scope of AnchoredBlock element
        //internal static bool IsInAnchoredBlock(ITextPointer position)
        //{
        //    return IsInAncestorScope(position, typeof(TextElement), typeof(AnchoredBlock));
        //}

        // Returns true if position is inside the scope of Hyperlink element
        internal static bool IsInHyperlinkScope(ITextPointer position)
        {
            return IsInAncestorScope(position, typeof(Inline), typeof(Hyperlink));
        }

        // If position is before the start boundary of a non-mergeable inline (Hyperlink),
        // this method returns a position immediately preceding its content (which is not an insertion position).
        // This method will skip past leading InlineUIContainers and BlockUIContainers. Otherwise returns null.
        internal static ITextPointer GetFollowingNonMergeableInlineContentStart(ITextPointer position)
        {
            ITextPointer navigator = position.CreatePointer();
            bool moved = false;
            Type elementType;
            
            while (true)
            {
                BorderingElementCategory category = GetBorderingElementCategory(navigator, LogicalDirection.Forward);

                // If position is before formatting closing scope, skip to outside the formatting scope.
                if (category == BorderingElementCategory.MergeableScopingInline)
                {
                    do
                    {
                        navigator.MoveToNextContextPosition(LogicalDirection.Forward);
                    }
                    while (GetBorderingElementCategory(navigator, LogicalDirection.Forward) == BorderingElementCategory.MergeableScopingInline);

                    moved = true;
                }

                // Skip all InlineUIContainers and BlockUIContainers.
                elementType = navigator.GetElementType(LogicalDirection.Forward);
                if (elementType == typeof(InlineUIContainer) /*|| elementType == typeof(BlockUIContainer)*/)
                {
                    // We are next to an InlineUIContainer/BlockUIContainer. Skip the following element.
                    navigator.MoveToNextContextPosition(LogicalDirection.Forward);
                    navigator.MoveToElementEdge(ElementEdge.AfterEnd);
                }
                else if (navigator.ParentType == typeof(InlineUIContainer) /*|| navigator.ParentType == typeof(BlockUIContainer)*/)
                {
                    // We are inside an InlineUIContainer/BlockUIContainer. Skip this element.
                    navigator.MoveToElementEdge(ElementEdge.AfterEnd);
                }
                else
                {
                    break;
                }

		// Move to next insertion position if we are done skipping a string of sequential UICs to get a
		// valid selection start pointer. We need to make sure we land at a position that would be given 
		// to this function had the UICs not been there. This ensures that we end up inside Runs that
		// follow instead of before them, e.g. </Span><Run>|abc</Run> instead of |</Span><Run>abc</Run>.
                elementType = navigator.GetElementType(LogicalDirection.Forward);
                if (!(elementType == typeof(InlineUIContainer)) /*&& !(elementType == typeof(BlockUIContainer))*/)
                {
                    navigator.MoveToNextInsertionPosition(LogicalDirection.Forward);
                }

                moved = true;
            }

            if (typeof(Inline).IsAssignableFrom(elementType) && !TextSchema.IsMergeableInline(elementType))
            {
                // We are adjacent to a nonmergeable inline.  Find its content.

                // Just skip over all opening contexts.
                do
                {
                    navigator.MoveToNextContextPosition(LogicalDirection.Forward);
                }
                while (navigator.GetPointerContext(LogicalDirection.Forward) == TextPointerContext.ElementStart);

                moved = true;
            }

            return moved ? navigator : null;
        }

        // Returns true if position is at the start boundary of a non-mergeable inline ancestor (hyperlink)
        internal static bool IsAtNonMergeableInlineStart(ITextPointer position)
        {
            return IsAtNonMergeableInlineEdge(position, LogicalDirection.Backward);
        }

        // Returns true if position is at the end boundary of a non-mergeable inline ancestor (hyperlink)
        internal static bool IsAtNonMergeableInlineEnd(ITextPointer position)
        {
            return IsAtNonMergeableInlineEdge(position, LogicalDirection.Forward);
        }

        // Returns true if passed position is in scope of a Hyperlink or other non-mergeable inline and
        // is an insertion position at the boundary of such an inline.
        internal static bool IsPositionAtNonMergeableInlineBoundary(ITextPointer position)
        {
            return IsAtNonMergeableInlineStart(position) || IsAtNonMergeableInlineEnd(position);
        }

        internal static bool IsAtFormatNormalizedPosition(ITextPointer position, LogicalDirection direction)
        {
            return IsAtNormalizedPosition(position, direction, false /* respectCaretUnitBoundaries */);
        }

        internal static bool IsAtInsertionPosition(ITextPointer position, LogicalDirection direction)
        {
            return IsAtNormalizedPosition(position, direction, true /* respectCaretUnitBoundaries */);
        }

        internal static bool IsAtNormalizedPosition(ITextPointer position, LogicalDirection direction, bool respectCaretUnitBoundaries)
        {
            if (!IsAtNormalizedPosition(position, respectCaretUnitBoundaries))
            {
                return false;
            }

            // Consider moving this into IsAtInsertionPosition(position) method.
            // Any empty element - including an inline - is an insertion position in both directions
            if (position.GetPointerContext(LogicalDirection.Backward) == TextPointerContext.ElementStart &&
                position.GetPointerContext(LogicalDirection.Forward) == TextPointerContext.ElementEnd)
            {
                return true;
            }

            // Check if there is no any formatting tags in the given direction
            if (TextSchema.IsFormattingType(position.GetElementType(direction)))
            {
                position = position.CreatePointer();
                while (TextSchema.IsFormattingType(position.GetElementType(direction)))
                {
                    position.MoveToNextContextPosition(direction);
                }

                if (IsAtNormalizedPosition(position, respectCaretUnitBoundaries))
                {
                    // So there is a possibility to move over formatting tags only
                    // and reach some insertion position. This means
                    // that our position was not normalized in the given direction.
                    return false;
                }
            }

            return true;
        }

        // <see cref="ITextPointer.Offset"/>
        internal static int GetOffset(ITextPointer thisPosition)
        {
            return thisPosition.TextContainer.Start.GetOffsetToPosition(thisPosition);
        }

        /// <summary>
        /// Returns true if the position is at the word boundary
        /// in the given direction.
        /// </summary>
        /// <param name="thisPosition">ITextPointer to examine.</param>
        /// <param name="insideWordDirection">
        /// If insideWordDirection == LogicalDirection.Forward, returns true iff the
        /// position is at the beginning of a word.
        /// 
        /// If direction == LogicalDirection.Backward, returns true iff the
        /// position is at the end of a word.
        /// </param>
        /// <returns></returns>
        internal static bool IsAtWordBoundary(ITextPointer thisPosition, LogicalDirection insideWordDirection)
        {
            bool isAtWordBoundary;
            ITextPointer navigator = thisPosition.CreatePointer();

            // Skip over any formatting.
            if (navigator.GetPointerContext(insideWordDirection) != TextPointerContext.Text)
            {
                navigator.MoveToInsertionPosition(insideWordDirection);
            }

            if (navigator.GetPointerContext(insideWordDirection) == TextPointerContext.Text)
            {
                // We're adjacent to text, so use the word breaker.

                char[] text;
                int position;

                GetWordBreakerText(thisPosition, out text, out position);

                isAtWordBoundary = SelectionWordBreaker.IsAtWordBoundary(text, position, insideWordDirection);
            }
            else
            {
                // If we're not adjacent to text then we always want to consider this
                // position a "word break" -- as far as selection is concerned.  In practice,
                // we're most likely next to an embedded object or block boundary.
                isAtWordBoundary = true;
            }

            return isAtWordBoundary;
        }

        /// <summary>
        /// Returns a TextSegment covering the word containing this TextPointer.
        /// </summary>
        /// <remarks>
        /// If this TextPointer is between two words, the following word is returned.
        /// 
        /// The return value includes trailing whitespace, if any.
        /// </remarks>
        internal static TextSegment GetWordRange(ITextPointer thisPosition)
        {
            return GetWordRange(thisPosition, LogicalDirection.Forward);
        }

        /// <summary>
        /// Returns a TextSegment covering the word containing this TextPointer.
        /// </summary>
        /// <remarks>
        /// If this TextPointer is between two words, direction specifies whether
        /// the preceeding or following word is returned.
        /// 
        /// The return value includes trailing whitespace, if any.
        /// </remarks>
        internal static TextSegment GetWordRange(ITextPointer thisPosition, LogicalDirection direction)
        {
            if (!thisPosition.IsAtInsertionPosition)
            {
                // Normalize original text pointer so it is at an insertion position.
                thisPosition = thisPosition.GetInsertionPosition(direction);
            }

            if (!thisPosition.IsAtInsertionPosition)
            {
                // In case there is no insertion position in the entire document, return an empty segment.
                // GetInsertionPosition() guarantees that navigator is moved back to original position.
                return new TextSegment(thisPosition, thisPosition);
            }

            // Find the next word end edge.
            ITextPointer navigator = thisPosition.CreatePointer();
            bool moved = MoveToNextWordBoundary(navigator, direction);

            ITextPointer wordEnd = navigator;

            // Find the corresponding word start edge.
            ITextPointer wordStart;
            if (moved && IsAtWordBoundary(thisPosition, /*insideWordDirection:*/LogicalDirection.Forward))
            {
                wordStart = thisPosition;
            }
            else
            {
                navigator = thisPosition.CreatePointer();
                MoveToNextWordBoundary(navigator, direction == LogicalDirection.Backward ? LogicalDirection.Forward : LogicalDirection.Backward);
                wordStart = navigator;
            }

            if (direction == LogicalDirection.Backward)
            {
                // If this is a backward search, need to swap start/end pointers.
                navigator = wordStart;
                wordStart = wordEnd;
                wordEnd = navigator;
            }

            // Make sure that we are not crossing any block boundaries.
            wordStart = RestrictWithinBlock(thisPosition, wordStart, LogicalDirection.Backward);
            wordEnd = RestrictWithinBlock(thisPosition, wordEnd, LogicalDirection.Forward);

            // Make sure that positions do not cross - as in TextRangeBase.cs
            if (wordStart.CompareTo(wordEnd) < 0)
            {
                wordStart = wordStart.GetFrozenPointer(LogicalDirection.Backward);
                wordEnd = wordEnd.GetFrozenPointer(LogicalDirection.Forward);
            }
            else
            {
                wordStart = wordEnd.GetFrozenPointer(LogicalDirection.Backward);
                wordEnd = wordStart;
            }

            Invariant.Assert(wordStart.CompareTo(wordEnd) <= 0, "expecting wordStart <= wordEnd");
            return new TextSegment(wordStart, wordEnd);
        }

        private static ITextPointer RestrictWithinBlock(ITextPointer position, ITextPointer limit, LogicalDirection direction)
        {
            Invariant.Assert(!(direction == LogicalDirection.Backward) || position.CompareTo(limit) >= 0, "for backward direction position must be >= than limit");
            Invariant.Assert(!(direction == LogicalDirection.Forward) || position.CompareTo(limit) <= 0, "for forward direcion position must be <= than linit");

            while (direction == LogicalDirection.Backward ? position.CompareTo(limit) > 0 : position.CompareTo(limit) < 0)
            {
                TextPointerContext context = position.GetPointerContext(direction);
                if (context == TextPointerContext.ElementStart || context == TextPointerContext.ElementEnd)
                {
                    Type elementType = position.GetElementType(direction);
                    if (!typeof(Inline).IsAssignableFrom(elementType))
                    {
                        limit = position;
                        break;
                    }
                }
                else if (context == TextPointerContext.EmbeddedElement)
                {
                    limit = position;
                    break;
                }
                position = position.GetNextContextPosition(direction);
            }

            // Return normalized position - in the direction towards a center position.
            return limit.GetInsertionPosition(direction == LogicalDirection.Backward ? LogicalDirection.Forward : LogicalDirection.Backward);
        }


        // <summary>
        // Checks if there is a Environment.NewLine symbol immediately
        // next to the position. Used only for plain text scenarios.
        // RichText case will always return false.
        // </summary>
        internal static bool IsNextToPlainLineBreak(ITextPointer thisPosition, LogicalDirection direction)
        {
            char[] textBuffer = new char[2];
            int actualCount = thisPosition.GetTextInRun(direction, textBuffer, /*startIndex:*/0, /*count:*/2);

            return
                (actualCount == 1 && IsCharUnicodeNewLine(textBuffer[0]))
                ||
                (actualCount == 2 &&
                    (
                        (direction == LogicalDirection.Backward && IsCharUnicodeNewLine(textBuffer[1]))
                        ||
                        (direction == LogicalDirection.Forward && IsCharUnicodeNewLine(textBuffer[0]))
                    )
                );
        }

        // Following Unicode newline guideline from http://www.unicode.org/unicode/standard/reports/tr13/tr13-5.html
        // To the standard list requirements we added '\v' and '\f'
        internal static Char[] NextLineCharacters = new char[] { '\n', '\r', '\v', '\f', '\u0085' /*NEL*/, '\u2028' /*LS*/, '\u2029' /*PS*/ };

        // Returns true if a specified char matches the Unicode definition of "newline".
        internal static bool IsCharUnicodeNewLine(char ch)
        {
            return Array.IndexOf(NextLineCharacters, ch) > -1;
        }

        /// <summary>
        /// Returns true if the position is adjacent to a LineBreak element,
        /// ignoring any intermediate formatting elements.
        /// </summary>
        internal static bool IsNextToRichLineBreak(ITextPointer thisPosition, LogicalDirection direction)
        {
            return IsNextToRichBreak(thisPosition, direction, typeof(LineBreak));
        }

        /// <summary>
        /// Returns true if the position is adjacent to a Paragraph element,
        /// ignoring any intermediate formatting elements.
        /// </summary>
        //internal static bool IsNextToParagraphBreak(ITextPointer thisPosition, LogicalDirection direction)
        //{
        //    return IsNextToRichBreak(thisPosition, direction, typeof(Paragraph));
        //}

        // <summary>
        // Checks if there is a "paragraph break" symbol immediately
        // before the position. Paragraph break is either plaintext
        // newline character or a combination equivalent to
        // [close-paragraph;open-paragraph] tag combination
        // </summary>
        internal static bool IsNextToAnyBreak(ITextPointer thisPosition, LogicalDirection direction)
        {
            if (!thisPosition.IsAtInsertionPosition)
            {
                thisPosition = thisPosition.GetInsertionPosition(direction);
            }

            return (IsNextToPlainLineBreak(thisPosition, direction) || IsNextToRichBreak(thisPosition, direction, null));
        }

        /// <summary>
        /// Checks if line wrapping is happening at this position
        /// </summary>
        internal static bool IsAtLineWrappingPosition(ITextPointer position, ITextView textView)
        {
            Invariant.Assert(position != null, "null check: position");

            if (!position.HasValidLayout)
            {
                return false;
            }

            Invariant.Assert(textView != null, "textView cannot be null because the position has valid layout");
            TextSegment lineSegment = textView.GetLineRange(position);

            if (lineSegment.IsNull)
            {
                return false;
            }

            bool isAtLineWrappingPosition = position.LogicalDirection == LogicalDirection.Forward 
                ? position.CompareTo(lineSegment.Start) == 0 
                : position.CompareTo(lineSegment.End) == 0;

            return isAtLineWrappingPosition;
        }


        // Position at row end (immediately before Row closing tag) is a valid stopper for a caret.
        // Editing operations are restricted here (e.g. typing should automatically jump
        // to the following character position.
        // This property identifies such special position.
        //internal static bool IsAtRowEnd(ITextPointer thisPosition)
        //{
        //    return typeof(TableRow).IsAssignableFrom(thisPosition.ParentType) &&
        //           thisPosition.GetPointerContext(LogicalDirection.Forward) == TextPointerContext.ElementEnd &&
        //           thisPosition.GetPointerContext(LogicalDirection.Backward) != TextPointerContext.ElementStart;
        //    // Note that only non-empty TableRows are good for insertion positions.
        //    // Totally empty TableRow is treated as any other incomplete content - not an insertion.
        //}

        // Position at document end - after the last paragraph/list/table is
        // considered as valid insertion point position.
        // It has though a special behavior for caret positioning and text insertion
        //internal static bool IsAfterLastParagraph(ITextPointer thisPosition)
        //{
        //    return thisPosition.GetPointerContext(LogicalDirection.Forward) == TextPointerContext.None &&
        //           thisPosition.GetPointerContext(LogicalDirection.Backward) == TextPointerContext.ElementEnd &&
        //           !typeof(Inline).IsAssignableFrom(thisPosition.GetElementType(LogicalDirection.Backward));
        //}

        // Returns true if pointer is at the start of a paragraph.
        //internal static bool IsAtParagraphOrBlockUIContainerStart(ITextPointer pointer)
        //{
        //    // Is pointer at a potential paragraph position?
        //    if (IsAtPotentialParagraphPosition(pointer))
        //    {
        //        return true;
        //    }

        //    // Can you find a <Paragraph> start tag looking backwards? 
        //    // Loop to skip multiple formatting opening tags, never crossing parent element boundary.
        //    while (pointer.GetPointerContext(LogicalDirection.Backward) == TextPointerContext.ElementStart)
        //    {
        //        if (TextSchema.IsParagraphOrBlockUIContainer(pointer.ParentType))
        //        {
        //            return true;
        //        }
        //        pointer = pointer.GetNextContextPosition(LogicalDirection.Backward);
        //    }
        //    return false;
        //}

        // Returns a ListItem from a current pointer if it exists as a parent of current paragraph.
        // Otherwise returns null.
        //internal static ListItem GetListItem(TextPointer pointer)
        //{
        //    if (pointer.Parent is ListItem)
        //    {
        //        return (ListItem)pointer.Parent;
        //    }

        //    Block paragraphOrBlockUIContainer = pointer.ParagraphOrBlockUIContainer;

        //    return paragraphOrBlockUIContainer == null ? null : (paragraphOrBlockUIContainer.Parent as ListItem);
        //}

        // Returns a ListItem if it exists and the current paragraph is the first block in it.
        // Otherwise returns null.
        // Non-null ImmediateListItem means that current paragraph has visual bullet on it,
        // so it must be treated as a list item from editing perspective.
        //internal static ListItem GetImmediateListItem(TextPointer position)
        //{
        //    if (position.Parent is ListItem)
        //    {
        //        return (ListItem)position.Parent;
        //    }

        //    Block paragraphOrBlockUIContainer = position.ParagraphOrBlockUIContainer;
        //    if (paragraphOrBlockUIContainer != null && paragraphOrBlockUIContainer.Parent is ListItem &&
        //        paragraphOrBlockUIContainer.ElementStart.GetPointerContext(LogicalDirection.Backward) == TextPointerContext.ElementStart)
        //    {
        //        return (ListItem)paragraphOrBlockUIContainer.Parent;
        //    }

        //    return null;
        //}

        // Returns true if position is parented by a ListItem element, which is empty.
        // Checks specifically for element emptyness, <ListItem></ListItem>.
        //internal static bool IsInEmptyListItem(TextPointer position)
        //{
        //    ListItem listItem = position.Parent as ListItem;
        //    return listItem != null && listItem.IsEmpty;
        //}

        //This overload is to cover for a bug that prevents line by line navigation in Fixed documents PS#1742102
        //MoveToLineBoundary returns the previous line if the first position on the next line is inside a Hyperlink tag
        //resulting in infinite loops, no line navigation etc.
        internal static int MoveToLineBoundary(ITextPointer thisPointer, ITextView textView, int count)
        {
            return MoveToLineBoundary(thisPointer, textView, count, false /* respectNonMeargeableInlineStart */);
        }

        // <see cref="System.Windows.Documents.TextPointer.MoveToLineBoundary"/>
        internal static int MoveToLineBoundary(ITextPointer thisPointer, ITextView textView, int count, bool respectNonMeargeableInlineStart)
        {
            ITextPointer position;
            double newSuggestedX;

            Invariant.Assert(!thisPointer.IsFrozen, "Can't reposition a frozen pointer!");
            Invariant.Assert(textView != null, "Null TextView!"); // Did you check ITextPointer.HasValidLayout?

            position = textView.GetPositionAtNextLine(thisPointer, Double.NaN, count, out newSuggestedX, out count);

            if (!position.IsAtInsertionPosition)
            {
                if (!respectNonMeargeableInlineStart || 
                    (!IsAtNonMergeableInlineStart(position) && !IsAtNonMergeableInlineEnd(position)))
                {
                    position.MoveToInsertionPosition(position.LogicalDirection);
                }
            }

            //if (IsAtRowEnd(position))
            //{
            //    // We will find outselves at a row end when we have incomplete
            //    // markup like
            //    //
            //    //  <TableCell></TableCell> <!-- No inner Run! -->
            //    //
            //    // In that case the end-of-row is the entire line.
            //    thisPointer.MoveToPosition(position);
            //    thisPointer.SetLogicalDirection(position.LogicalDirection);
            //}
            //else
            //{
                TextSegment lineRange = textView.GetLineRange(position);

                if (!lineRange.IsNull)
                {
                    thisPointer.MoveToPosition(lineRange.Start);
                    thisPointer.SetLogicalDirection(lineRange.Start.LogicalDirection);
                }
                else if (count > 0)
                {
                    // It is possible to get a non-zero return value from ITextView.GetPositionAtNextLine
                    // when moving into a BlockUIContainer.  The container is the "next line" but does
                    // not contain any lines itself -- GetLineRange will return null.
                    thisPointer.MoveToPosition(position);
                    thisPointer.SetLogicalDirection(position.LogicalDirection);
                }
            //}

            return count;
        }

        internal static Rect GetCharacterRect(ITextPointer thisPointer, LogicalDirection direction)
        {
            return GetCharacterRect(thisPointer, direction, /*transformToUiScope*/true);
        }

        // <see cref="TextPointer.GetCharacterRect"/>
        internal static Rect GetCharacterRect(ITextPointer thisPointer, LogicalDirection direction, bool transformToUiScope)
        {
            ITextView textView = thisPointer.TextContainer.TextView;

            Invariant.Assert(textView != null, "Null TextView!"); // Did you check ITextPointer.HasValidLayout?
            Invariant.Assert(textView.RenderScope != null, "Null RenderScope");
            Invariant.Assert(thisPointer.TextContainer != null, "Null TextContainer");
            Invariant.Assert(thisPointer.TextContainer.Parent != null, "Null parent of TextContainer");

            // Try to ask for a Rect from an insertion position.
            if (!thisPointer.IsAtInsertionPosition)
            {
                ITextPointer insertionPosition = thisPointer.GetInsertionPosition(direction);

                if (insertionPosition != null)
                {
                    thisPointer = insertionPosition;
                }
            }

            Rect rect = textView.GetRectangleFromTextPosition(thisPointer.CreatePointer(direction));

            if (transformToUiScope)
            {
                Visual templatedParent;

                //if (thisPointer.TextContainer.Parent is FlowDocument && textView.RenderScope is FlowDocumentView)
                //{
                //    //  Need a cleaner way of working with FlowDocument in RichTextBox
                //    templatedParent = ((FlowDocumentView)textView.RenderScope).TemplatedParent as Visual;
                //    if (templatedParent == null && ((FlowDocumentView)textView.RenderScope).Parent is FrameworkElement)
                //    {
                //        templatedParent = ((FrameworkElement)((FlowDocumentView)textView.RenderScope).Parent).TemplatedParent as Visual;
                //    }
                //}
                //else
                if (thisPointer.TextContainer.Parent is Visual)
                {
                    Invariant.Assert(textView.RenderScope == thisPointer.TextContainer.Parent || ((Visual)thisPointer.TextContainer.Parent).IsVisualAncestorOf( /*descendant:*/textView.RenderScope),
                        "Unexpected location of RenderScope within visual tree");
                    templatedParent = (Visual)thisPointer.TextContainer.Parent;
                }
                else
                {
                    templatedParent = null;
                }

                //ToDo: Is this ever needed?
                if (templatedParent != null && templatedParent.IsVisualAncestorOf( /*descendant:*/textView.RenderScope))
                {
                    // translate the rect from renderscope to uiscope coordinate system (from FlowDocumentView to RichTextBox)
                    var transformFromRenderToUiScope = textView.RenderScope.TransformToVisual(/*ancestor:*/templatedParent);

                    rect = transformFromRenderToUiScope.HasValue ? rect.TransformToAABB(transformFromRenderToUiScope.Value) : rect;
                }
            }

            return rect;
        }

        // Move to the closest insertion position, treating all unicode code points
        // as valid insertion positions.  A useful performance win over 
        // MoveToNextInsertionPosition when only formatting scopes are important.
        internal static bool MoveToFormatNormalizedPosition(ITextPointer thisNavigator, LogicalDirection direction)
        {
            return NormalizePosition(thisNavigator, direction, false /* respectCaretUnitBoundaries */);
        }

        /// <summary>
        /// Moves the navigator to a closest insertion position (caret stop position).
        /// The parameter direction is used in cases of ambiguity:
        /// when two caret positions are separated by only formatting tags,
        /// or when starting position is beteen end and start of two consequitive blocks
        /// (say, between Paragraphs).
        /// In such cases of ambiguity the position in a given direction is chosen.
        /// </summary>
        internal static bool MoveToInsertionPosition(ITextPointer thisNavigator, LogicalDirection direction)
        {
            return NormalizePosition(thisNavigator, direction, true /* respectCaretUnitBoundaries */);
        }

        /// <summary>
        /// Advances this TextNavigator by a count number of characters.
        /// </summary>
        /// <param name="thisNavigator">ITextPointer to advance.</param>
        /// <param name="direction">
        /// A direction in which to search a next characters.
        /// </param>
        /// <returns>
        /// True if the navigator is advanced, false if the end of document is
        /// encountered and the navigator is not repositioned.
        /// </returns>
        /// <remarks>
        /// A "character" in this context is a sequence of one or several text
        /// symbols: one or more Unicode code points may be a character, every
        /// embedded object is a character, a sequence of closing block tags
        /// followed by opening block tags may also be a unit. Formatting tags
        /// do not contribute in any unit.
        /// </remarks>
        internal static bool MoveToNextInsertionPosition(ITextPointer thisNavigator, LogicalDirection direction)
        {
            Invariant.Assert(!thisNavigator.IsFrozen, "Can't reposition a frozen pointer!");

            bool moved = true;

            int increment = direction == LogicalDirection.Forward ? +1 : -1;

            ITextPointer initialPosition = thisNavigator.CreatePointer();

            if (!IsAtInsertionPosition(thisNavigator))
            {
                // If the TextPointer is not currently at an insertion position,
                // move the TextPointer to the next insertion position in
                // the indicated direction, just like the MoveToInsertionPosition method.

                if (!MoveToInsertionPosition(thisNavigator, direction))
                {
                    // No insertion position in all content. MoveToInsertionPosition() guarantees that navigator is moved back to initial position.
                    moved = false;
                    goto Exit;
                }

                if ((direction == LogicalDirection.Forward && initialPosition.CompareTo(thisNavigator) < 0) ||
                    (direction == LogicalDirection.Backward && thisNavigator.CompareTo(initialPosition) < 0))
                {
                    // We have found an insertion position in requested direction.
                    goto Exit;
                }
            }

            // Start with skipping character formatting tags in this direction
            while (TextSchema.IsFormattingType(thisNavigator.GetElementType(direction)))
            {
                thisNavigator.MoveByOffset(increment);
            }

            do
            {
                if (thisNavigator.GetPointerContext(direction) != TextPointerContext.None)
                {
                    thisNavigator.MoveByOffset(increment);
                }
                else
                {
                    // No insertion position in this direction; Move back
                    thisNavigator.MoveToPosition(initialPosition);
                    moved = false;
                    goto Exit;
                }
            }
            while (!IsAtInsertionPosition(thisNavigator));

            // We must leave position normalized in backward direction
            if (direction == LogicalDirection.Backward)
            {
                // For this we must skip character formatting tags if we have any
                while (TextSchema.IsFormattingType(thisNavigator.GetElementType(direction)))
                {
                    thisNavigator.MoveByOffset(increment);
                }

                // However if it is block start we should back off
                TextPointerContext context = thisNavigator.GetPointerContext(direction);
                if (context == TextPointerContext.ElementStart || context == TextPointerContext.None)
                {
                    increment = -increment;
                    while (TextSchema.IsFormattingType(thisNavigator.GetElementType(LogicalDirection.Forward))
                           && !IsAtInsertionPosition(thisNavigator))
                    {
                        thisNavigator.MoveByOffset(increment);
                    }
                }
            }

        Exit:
            if (moved)
            {
                if (direction == LogicalDirection.Forward)
                {
                    Invariant.Assert(thisNavigator.CompareTo(initialPosition) > 0, "thisNavigator is expected to be moved from initialPosition - 1");
                }
                else
                {
                    Invariant.Assert(thisNavigator.CompareTo(initialPosition) < 0, "thisNavigator is expected to be moved from initialPosition - 2");
                }
            }
            else
            {
                Invariant.Assert(thisNavigator.CompareTo(initialPosition) == 0, "thisNavigator must stay at initial position");
            }
            return moved;
        }

        /// <summary>
        /// Moves the navigator in the given direction to a position of the next
        /// word boundary.
        /// </summary>
        /// <param name="thisNavigator">ITextPointer to advance.</param>
        /// <param name="movingDirection">
        /// Direction to move.
        /// </param>
        /// <returns></returns>
        // consider adding a version of this method
        // with a startEdge parameter: bool MoveToNextWordBoundary(LogicalDirection direction, bool startEdge).
        // Currently, there's no way to find the end of words that _doesn't_
        // include trailing whitespace.  If we exposed the overload, apps could
        // be explicit about whether they want to find a word start or end.
        internal static bool MoveToNextWordBoundary(ITextPointer thisNavigator, LogicalDirection movingDirection)
        {
            int moveCounter = 0;

            Invariant.Assert(!thisNavigator.IsFrozen, "Can't reposition a frozen pointer!");
            ITextPointer startPosition = thisNavigator.CreatePointer();

            while (thisNavigator.MoveToNextInsertionPosition(movingDirection))
            {
                moveCounter++;

                // Need to break the loop for weird case when there is no word break in text content.
                // When the word looks too long, consider end of textRun as a word break.
                //  Think of better way of breaking the unreasonably long loop
                if (moveCounter > 64) // 64 was taken as a random number. Probably not big enough though...
                {
                    thisNavigator.MoveToPosition(startPosition);
                    thisNavigator.MoveToNextContextPosition(movingDirection);
                    break;
                }

                if (IsAtWordBoundary(thisNavigator, /*insideWordDirection:*/LogicalDirection.Forward))
                {
                    // Note that we always use Forward direction for word orientation.
                    break;
                }
            }

            return moveCounter > 0;
        }

        // <see cref="System.Windows.Documents.TextPointer.GetFrozenPointer"/>
        internal static ITextPointer GetFrozenPointer(ITextPointer thisPointer, LogicalDirection logicalDirection)
        {
            ITextPointer frozenPointer;

            if (thisPointer.IsFrozen && thisPointer.LogicalDirection == logicalDirection)
            {
                frozenPointer = thisPointer;
            }
            else
            {
                frozenPointer = thisPointer.CreatePointer(logicalDirection);
                frozenPointer.Freeze();
            }

            return frozenPointer;
        }

        /// <see cref="ITextPointer.ValidateLayout"/>
        internal static bool ValidateLayout(ITextPointer thisPointer, ITextView textView)
        {
            if (textView == null)
            {
                return false;
            }

            return textView.Validate(thisPointer);
        }

        #endregion Internal methods

        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------

        #region Private Methods

        // Worker for MoveToNextFormatNormalizedPosition/MoveToNextInsertionPosition.
        private static bool NormalizePosition(ITextPointer thisNavigator, LogicalDirection direction, bool respectCaretUnitBoundaries)
        {
            Invariant.Assert(!thisNavigator.IsFrozen, "Can't reposition a frozen pointer!");

            int symbolCount = 0;
            int increment;
            LogicalDirection oppositeDirection;
            TextPointerContext directEnterScope;
            TextPointerContext oppositeEnterScope;

            if (direction == LogicalDirection.Forward)
            {
                increment = +1;
                oppositeDirection = LogicalDirection.Backward;
                directEnterScope = TextPointerContext.ElementStart;
                oppositeEnterScope = TextPointerContext.ElementEnd;
            }
            else
            {
                increment = -1;
                oppositeDirection = LogicalDirection.Forward;
                directEnterScope = TextPointerContext.ElementEnd;
                oppositeEnterScope = TextPointerContext.ElementStart;
            }

            // When the pointer appears in between structural tags we need to start
            // from sliding into the deepest possible position without
            // leaving any structural units. We need to do that only
            // if we are not at insertion position already.
            if (!IsAtNormalizedPosition(thisNavigator, respectCaretUnitBoundaries))
            {
                // Go inside an innermost structured element (non-inline)
                while (
                    thisNavigator.GetPointerContext(direction) == directEnterScope &&
                    !typeof(Inline).IsAssignableFrom(thisNavigator.GetElementType(direction)) &&
                    !IsAtNormalizedPosition(thisNavigator, respectCaretUnitBoundaries))
                {
                    thisNavigator.MoveToNextContextPosition(direction);
                    symbolCount += increment;
                }
                while (
                    thisNavigator.GetPointerContext(oppositeDirection) == oppositeEnterScope &&
                    !typeof(Inline).IsAssignableFrom(thisNavigator.GetElementType(oppositeDirection)) &&
                    !IsAtNormalizedPosition(thisNavigator, respectCaretUnitBoundaries))
                {
                    thisNavigator.MoveToNextContextPosition(oppositeDirection);
                    symbolCount -= increment;
                }
            }

            // Get out of a Hyperlink, etc. inner edge.
            symbolCount = LeaveNonMergeableInlineBoundary(thisNavigator, direction, symbolCount);

            // Get out of a compound sequence if any.
            if (respectCaretUnitBoundaries)
            {
                while (!IsAtCaretUnitBoundary(thisNavigator))
                {
                    symbolCount += increment;
                    thisNavigator.MoveByOffset(increment);
                }
            }

            // Here is the core part of this method's logic - skipping all formatting tags in the given direction.
            // Skip character formatting tags if they are present in this direction.
            // Even if an insertion position can be in the middle of this formatting sequence,
            // we want to skip it all and reach the farthest possible insertion position in that direction.
            // Such approach guarantees that repeated calls of this normalization will give the same reauls.
            // In case if there is an inserrtion position in the middle (say, in empty Run),
            // the loop moving in opposite direction below will find it if needed.
            while (TextSchema.IsMergeableInline(thisNavigator.GetElementType(direction)))
            {
                thisNavigator.MoveToNextContextPosition(direction);
                symbolCount += increment;
            }

            if (!IsAtNormalizedPosition(thisNavigator, respectCaretUnitBoundaries))
            {
                // If still not at insertion point, try skipping inline tags in the opposite direction
                // now possibly stopping inside of empty element
                while (!IsAtNormalizedPosition(thisNavigator, respectCaretUnitBoundaries) &&
                    TextSchema.IsMergeableInline(thisNavigator.GetElementType(oppositeDirection)))
                {
                    thisNavigator.MoveToNextContextPosition(oppositeDirection);
                    symbolCount -= increment;
                }

                // If still not at insertion point, then try harder - skipping block tags
                // First in "preferred" direction
                while (!IsAtNormalizedPosition(thisNavigator, respectCaretUnitBoundaries) &&
                    thisNavigator.MoveToNextContextPosition(direction))
                {
                    symbolCount += increment;
                }

                // And finally in apposite direction
                while (!IsAtNormalizedPosition(thisNavigator, respectCaretUnitBoundaries) &&
                    thisNavigator.MoveToNextContextPosition(oppositeDirection))
                {
                    symbolCount -= increment;
                }

                if (!IsAtNormalizedPosition(thisNavigator, respectCaretUnitBoundaries))
                {
                    // When there is no insertion positions in the whole document
                    // we return the position back to its original place.
                    thisNavigator.MoveByOffset(-symbolCount);
                }
            }

            return symbolCount != 0;
        }

        // If thisNavigator is at the inner edge of a non-mergeable inline, this method
        // repositions it outside the scope of the non-mergeable.
        // Otherwise, this method does nothing.
        private static int LeaveNonMergeableInlineBoundary(ITextPointer thisNavigator, LogicalDirection direction, int symbolCount)
        {
            if (IsAtNonMergeableInlineStart(thisNavigator))
            {
                if (direction == LogicalDirection.Forward && IsAtNonMergeableInlineEnd(thisNavigator))
                {
                    symbolCount += LeaveNonMergeableAncestor(thisNavigator, LogicalDirection.Forward);
                }
                else
                {
                    symbolCount += LeaveNonMergeableAncestor(thisNavigator, LogicalDirection.Backward);
                }
            }
            else if (IsAtNonMergeableInlineEnd(thisNavigator))
            {
                if (direction == LogicalDirection.Backward && IsAtNonMergeableInlineStart(thisNavigator))
                {
                    symbolCount += LeaveNonMergeableAncestor(thisNavigator, LogicalDirection.Backward);
                }
                else
                {
                    symbolCount += LeaveNonMergeableAncestor(thisNavigator, LogicalDirection.Forward);
                }
            }

            return symbolCount;
        }

        // Exits the scope of a non-mergeable inline with inner edge in the direction indicated.
        private static int LeaveNonMergeableAncestor(ITextPointer thisNavigator, LogicalDirection direction)
        {
            int symbolCount = 0;
            int increment = (direction == LogicalDirection.Forward) ? +1 : -1;

            while (TextSchema.IsMergeableInline(thisNavigator.ParentType))
            {
                thisNavigator.MoveToNextContextPosition(direction);
                symbolCount += increment;
            }

            thisNavigator.MoveToNextContextPosition(direction);
            symbolCount += increment;

            return symbolCount;
        }

        // Worker for IsAtFormatNormalizedPosition/IsAtInsertionPosition.
        private static bool IsAtNormalizedPosition(ITextPointer position, bool respectCaretUnitBoundaries)
        {
            if (IsPositionAtNonMergeableInlineBoundary(position))
            {
                // The inner edge of a Hyperlink is not a valid insertion position.
                return false;
            }
            else if (TextSchema.IsValidChild(/*position*/position, /*childType*/typeof(string)))
            {
                return respectCaretUnitBoundaries ? IsAtCaretUnitBoundary(position) : true;
            }
            else
            {
                // Special positions outside of Run elements that allow caret stops
                return /*IsAtRowEnd(position) ||*/
                    IsAtPotentialRunPosition(position) /*||*/
                    //IsBeforeFirstTable(position) ||
                    /*IsInBlockUIContainer(position)*/;
            }
        }

        // Returns true if the position is on the caret unit boundary.
        // Call TextView's IsAtCaretUnitBoundary if TextView is valid for this position
        // and it appears strictly within text run.
        // We consider all markup-boundary positions as caret unit boundaries.
        // If TextView information is not available call IsInsideCompoundSequence.
        private static bool IsAtCaretUnitBoundary(ITextPointer position)
        {
            bool isAtCaretUnitBoundary;

            TextPointerContext forwardContext = position.GetPointerContext(LogicalDirection.Forward);
            TextPointerContext backwardContext = position.GetPointerContext(LogicalDirection.Backward);

            if (backwardContext == TextPointerContext.Text && forwardContext == TextPointerContext.Text)
            {
                if (position.HasValidLayout)
                {
                    // Check the insertion position with TextView's IsAtCaretUnitBoundary
                    // that will acurately check the caret unit bounday for surrogate and international
                    // characters
                    isAtCaretUnitBoundary = position.IsAtCaretUnitBoundary;
                }
                else
                {
                    // Check the insertion position with the internal compound sequence
                    isAtCaretUnitBoundary = !IsInsideCompoundSequence(position);
                }
            }
            else
            {
                isAtCaretUnitBoundary = true;
            }

            return isAtCaretUnitBoundary;
        }

        // Returns true if the position is inside of a pair of surrogate characters
        // or inside of Newline sequence "\r\n".
        // Such position is not valid position for caret stopping or for text insertion.
        private static bool IsInsideCompoundSequence(ITextPointer position)
        {
            // OK, so we're surrounded by text runs (possibly empty), try getting a character
            // in each direction -- it's OK to position the caret if there's no characters 
            // before or after it
            Char[] neighborhood = new char[2];

            if (position.GetTextInRun(LogicalDirection.Backward, neighborhood, 0, 1) == 1 &&
                position.GetTextInRun(LogicalDirection.Forward, neighborhood, 1, 1) == 1)
            {
                if (Char.IsSurrogatePair(neighborhood[0], neighborhood[1]) ||
                    neighborhood[0] == '\r' && neighborhood[1] == '\n')
                {
                    return true;
                }

                // Check for combining marks.
                //
                // See Unicode 3.1, Section 3.5 (Combination), D13 and D14 for
                // strict definitions of "combining character" and "base character".
                //
                // The CLR source for StringInfo is also informative.
                //
                // In brief: we're looking for a character followed by a
                // combining mark.
                //
                UnicodeCategory category1 = Char.GetUnicodeCategory(neighborhood[1]);
                if (category1 == UnicodeCategory.SpacingCombiningMark ||
                    category1 == UnicodeCategory.NonSpacingMark ||
                    category1 == UnicodeCategory.EnclosingMark)
                {
                    UnicodeCategory category0 = Char.GetUnicodeCategory(neighborhood[0]);

                    if (category0 != UnicodeCategory.Control &&
                        category0 != UnicodeCategory.Format &&
                        category0 != UnicodeCategory.OtherNotAssigned)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        // Initializes an array with the minimum necessary text to support a call to
        // SelectionWordBreaker.IsAtWordBoundary.
        //
        // position on exit holds the offset into the text array corresponding to
        // pointer's location in the document.
        //
        // Called by IsAtWordBoundary.
        private static void GetWordBreakerText(ITextPointer pointer, out char[] text, out int position)
        {
            char[] preceedingText = new char[SelectionWordBreaker.MinContextLength];
            char[] followingText = new char[SelectionWordBreaker.MinContextLength];
            int preceedingCount = 0;
            int followingCount = 0;
            int runLength;
            ITextPointer navigator;

            navigator = pointer.CreatePointer();

            // Try to back up SelectionWordBreaker.MinContextLength chars, ignoring formatting.
            do
            {
                runLength = Math.Min(navigator.GetTextRunLength(LogicalDirection.Backward), SelectionWordBreaker.MinContextLength - preceedingCount);
                preceedingCount += runLength;

                navigator.MoveByOffset(-runLength);
                navigator.GetTextInRun(LogicalDirection.Forward, preceedingText, SelectionWordBreaker.MinContextLength - preceedingCount, runLength);

                if (preceedingCount == SelectionWordBreaker.MinContextLength)
                    break;

                // Skip over any formatting.
                navigator.MoveToInsertionPosition(LogicalDirection.Backward);
            }
            while (navigator.GetPointerContext(LogicalDirection.Backward) == TextPointerContext.Text);

            navigator.MoveToPosition(pointer);

            // Try to advance SelectionWordBreaker.MinContextLength chars, ignoring formatting.
            do
            {
                runLength = Math.Min(navigator.GetTextRunLength(LogicalDirection.Forward), SelectionWordBreaker.MinContextLength - followingCount);

                navigator.GetTextInRun(LogicalDirection.Forward, followingText, followingCount, runLength);

                followingCount += runLength;

                if (followingCount == SelectionWordBreaker.MinContextLength)
                    break;

                navigator.MoveByOffset(runLength);
                // Skip over any formatting.
                navigator.MoveToInsertionPosition(LogicalDirection.Forward);
            }
            while (navigator.GetPointerContext(LogicalDirection.Forward) == TextPointerContext.Text);

            // Combine the preceeding and following text into a single array.
            text = new char[preceedingCount + followingCount];
            Array.Copy(preceedingText, SelectionWordBreaker.MinContextLength - preceedingCount, text, 0, preceedingCount);
            Array.Copy(followingText, 0, text, preceedingCount, followingCount);

            position = preceedingCount;
        }

        // Worker for IsAtNonMergeableInlineStart/IsAtNonMergeableInlineEnd.
        private static bool IsAtNonMergeableInlineEdge(ITextPointer position, LogicalDirection direction)
        {
            BorderingElementCategory elementType = GetBorderingElementCategory(position, direction);

            if (elementType == BorderingElementCategory.MergeableScopingInline)
            {
                ITextPointer navigator = position.CreatePointer();

                do
                {
                    navigator.MoveToNextContextPosition(direction);
                }
                while ((elementType = GetBorderingElementCategory(navigator, direction)) == BorderingElementCategory.MergeableScopingInline);
            }

            return (elementType == BorderingElementCategory.NonMergeableScopingInline);
        }

        // Tests for the presence of a non-mergeable Inline bordering a position.
        // Helper for IsAtNonMergeableInlineEdge.
        private static BorderingElementCategory GetBorderingElementCategory(ITextPointer position, LogicalDirection direction)
        {
            TextPointerContext context = (direction == LogicalDirection.Forward) ? TextPointerContext.ElementEnd : TextPointerContext.ElementStart;
            BorderingElementCategory category;

            if (position.GetPointerContext(direction) != context ||
                !typeof(Inline).IsAssignableFrom(position.ParentType))
            {
                category = BorderingElementCategory.NotScopingInline;
            }
            else if (TextSchema.IsMergeableInline(position.ParentType))
            {
                category = BorderingElementCategory.MergeableScopingInline;
            }
            else
            {
                category = BorderingElementCategory.NonMergeableScopingInline;
            }

            return category;
        }

        // Returns true if the position is adjacent to a LineBreak or Paragraph element,
        // ignoring any intermediate formatting elements.
        //
        // If lineBreakType is null, any line break element is considered valid.
        private static bool IsNextToRichBreak(ITextPointer thisPosition, LogicalDirection direction, Type lineBreakType)
        {
            Invariant.Assert(lineBreakType == null || lineBreakType == typeof(LineBreak) /*|| lineBreakType == typeof(Paragraph)*/);

            bool result = false;

            while (true)
            {
                Type neighbor = thisPosition.GetElementType(direction);

                if (lineBreakType == null)
                {
                    if (typeof(LineBreak).IsAssignableFrom(neighbor) /*||*/
                        /*typeof(Paragraph).IsAssignableFrom(neighbor)*/)
                    {
                        result = true;
                        break;
                    }
                }
                else if (lineBreakType.IsAssignableFrom(neighbor))
                {
                    result = true;
                    break;
                }

                if (!TextSchema.IsFormattingType(neighbor))
                    break;

                thisPosition = thisPosition.GetNextContextPosition(direction);
            }

            return result;
        }

        #endregion Private methods

        //------------------------------------------------------
        //
        //  Private Types
        //
        //------------------------------------------------------

        #region Private Types

        // Element types for GetBorderingElementCategory.
        private enum BorderingElementCategory { MergeableScopingInline, NonMergeableScopingInline, NotScopingInline };

        #endregion Private Types
    }
}
