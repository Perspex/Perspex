// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: Static internal class providing a set of
//              helpoer methods for text editing operations
//

using Avalonia.Media.TextFormatting;

namespace System.Windows.Documents
{
    using System;
    using MS.Internal;
    //using System.Windows.Controls;
    //using MS.Internal.PtsHost.UnsafeNativeMethods; // PTS restrictions to obtain TextIndent valid value range.
    using Avalonia;
    using Avalonia.Media;

    /// <summary>
    /// The TextRange class represents a pair of TextPositions, with many
    /// rich text editing operations exposed.
    /// </summary>
    internal static class TextRangeEdit
    {
        // --------------------------------------------------------------------
        //
        // Internal Methods
        //
        // --------------------------------------------------------------------

        #region Internal Methods

        internal static TextElement InsertElementClone(TextPointer start, TextPointer end, TextElement element)
        {
            TextElement newElement = (TextElement)Activator.CreateInstance(element.GetType());

            // Copy properties to the newElement
            newElement.TextContainer.SetValues(newElement.ContentStart, element.GetLocalValueEnumerator());

            newElement.Reposition(start, end);

            return newElement;
        }

        // ....................................................................
        //
        // Character Formatting
        //
        // ....................................................................

        #region Character Formatting

        internal static TextPointer SplitFormattingElements(TextPointer splitPosition, bool keepEmptyFormatting)
        {
            return SplitFormattingElements(splitPosition, keepEmptyFormatting, /*limitingAncestor*/null);
        }

        internal static TextPointer SplitFormattingElement(TextPointer splitPosition, bool keepEmptyFormatting)
        {
            Invariant.Assert(splitPosition.Parent != null && TextSchema.IsMergeableInline(splitPosition.Parent.GetType()));

            Inline inline = (Inline)splitPosition.Parent;

            // Create a movable copy of a splitPosition
            if (splitPosition.IsFrozen)
            {
                splitPosition = new TextPointer(splitPosition);
            }

            if (!keepEmptyFormatting && splitPosition.GetPointerContext(LogicalDirection.Backward) == TextPointerContext.ElementStart)
            {
                // The first part of element is empty. We are allowed to remove empty formatting elements,
                // so we can simply move splitPotision outside of the element and we are done
                splitPosition.MoveToPosition(inline.ElementStart);
            }
            else if (!keepEmptyFormatting && splitPosition.GetPointerContext(LogicalDirection.Forward) == TextPointerContext.ElementEnd)
            {
                // The second part of element is empty. We are allowed to remove empty formatting elements,
                // so we can simply move splitPotision outside of the element and we are done.
                splitPosition.MoveToPosition(inline.ElementEnd);
            }
            else
            {
                splitPosition = SplitElement(splitPosition);
            }

            return splitPosition;
        }

        // Compares a set of inheritable properties taken from two objects
        private static bool InheritablePropertiesAreEqual(Inline firstInline, Inline secondInline)
        {
            Invariant.Assert(firstInline != null, "null check: firstInline");
            Invariant.Assert(secondInline != null, "null check: secondInline");

            // Compare inheritable properties
            AvaloniaProperty[] inheritableProperties = TextSchema.GetInheritableProperties(typeof(Inline));
            for (int i = 0; i < inheritableProperties.Length; i++)
            {
                AvaloniaProperty property = inheritableProperties[i];

                if (TextSchema.IsStructuralCharacterProperty(property))
                {
                    if (firstInline.ReadLocalValue(property) != AvaloniaProperty.UnsetValue ||
                        secondInline.ReadLocalValue(property) != AvaloniaProperty.UnsetValue)
                    {
                        return false;
                    }
                }
                else
                {
                    if (!TextSchema.ValuesAreEqual(firstInline.GetValue(property), secondInline.GetValue(property)))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        // Compares all character formatting properties for two elements.
        // Returns true if all known properties have equal values, false otherwise.
        // Note that only statically known character formatting properties
        // are taken into account. We intentionally ignore all other properties,
        // because TextEditor is not aware (in general) about their semantics,
        // and considers unsafe to duplicate them freely.
        // Ignorance means deletion, which is considered as safer approach.
        private static bool CharacterPropertiesAreEqual(Inline firstElement, Inline secondElement)
        {
            Invariant.Assert(firstElement != null, "null check: firstElement");

            if (secondElement == null)
            {
                return false;
            }

            AvaloniaProperty[] noninheritableProperties = TextSchema.GetNoninheritableProperties(typeof(Span));
            for (int i = 0; i < noninheritableProperties.Length; i++)
            {
                AvaloniaProperty property = noninheritableProperties[i];
                if (!TextSchema.ValuesAreEqual(firstElement.GetValue(property), secondElement.GetValue(property)))
                {
                    return false;
                }
            }

            if (!InheritablePropertiesAreEqual(firstElement, secondElement))
            {
                return false;
            }

            return true;
        }
        
        /// <summary>
        /// Checks if scoping element is empty formatting.
        /// It must be removed if not situated inside of empty block.
        /// </summary>
        /// <param name="position">
        /// TextPointer scoped by the allegedly empty formatting element(s).
        /// </param>
        /// <returns>
        /// true if at least one empty formatting element was extracted.
        /// </returns>
        private static bool ExtractEmptyFormattingElements(TextPointer position)
        {
            bool elementsWereExtracted = false;

            Inline inline = position.Parent as Inline;

            if (inline != null && inline.IsEmpty)
            {
                // Delete any empty non-formatting element.
                // We can get here if an IME deletes the UIElement from inside an InlineUIContainer.
                while (inline != null && inline.IsEmpty && !TextSchema.IsFormattingType(inline.GetType()))
                {
                    inline.Reposition(null, null);
                    elementsWereExtracted = true;
                    inline = position.Parent as Inline;
                }

                // Start with removing empty Runs and Spans unconditionally.
                // If it is an empty non-derived Run or Span with no local properties on it - it's safe to delete it.
                // It does not have any formatting or any other meaning, while it can be implicitely
                // re-inserted when necessary. So remove it to minimize resulting xaml.
                while (
                    inline != null && inline.IsEmpty &&
                    (inline.GetType() == typeof(Run) || inline.GetType() == typeof(Span)) &&
                    !HasWriteableLocalPropertyValues(inline))
                {
                    inline.Reposition(null, null);
                    elementsWereExtracted = true;
                    inline = position.Parent as Inline;
                }

                // Continue deleting empty inlines that are neighbored by other formatting elements,
                // that make them inaccessible for caret position
                while (inline != null && inline.IsEmpty &&
                    ((inline.NextInline != null && TextSchema.IsFormattingType(inline.NextInline.GetType())) ||
                    (inline.PreviousInline != null && TextSchema.IsFormattingType(inline.PreviousInline.GetType()))))
                {
                    inline.Reposition(null, null);
                    elementsWereExtracted = true;
                    inline = position.Parent as Inline;
                }
            }

            return elementsWereExtracted;
        }

        /// <summary>
        /// Applies a property to a range between start and end positions.
        /// </summary>
        /// <param name="start">
        /// TextPointer identifying start of affected range.
        /// </param>
        /// <param name="end">
        /// TextPointer identifying end of affected range.
        /// </param>
        /// <param name="formattingProperty">
        /// A dependency property whose value is supposed to applied to a range.
        /// </param>
        /// <param name="value">
        /// A value for a property to apply.
        /// </param>
        /// <param name="propertyValueAction">
        /// Specifies how to use the value - as absolute, as increment or a decrement.
        /// </param>
        internal static void SetInlineProperty(TextPointer start, TextPointer end, AvaloniaProperty formattingProperty, object value, PropertyValueAction propertyValueAction)
        {
            // Check for corner case when we have siple text run with all properties set as requested.
            // This case is iportant optimization for Backspace-Type scenario, when Springload formatting applies for nothing for 50 properties
            if (start.CompareTo(end) >= 0 || 
                propertyValueAction == PropertyValueAction.SetValue &&
                start.Parent is Run &&
                start.Parent == end.Parent && TextSchema.ValuesAreEqual(start.Parent.GetValue(formattingProperty), value))
            {
                return;
            }

            // Remove unnecessary spans on range ends - to optimize resulting markup
            RemoveUnnecessarySpans(start);
            RemoveUnnecessarySpans(end);

            if (TextSchema.IsStructuralCharacterProperty(formattingProperty))
            {
                SetStructuralInlineProperty(start, end, formattingProperty, value);
            }
            else
            {
                SetNonStructuralInlineProperty(start, end, formattingProperty, value, propertyValueAction);
            }
        }

        // Merges inline elements with equivalent formatting properties at a given position
        // Returns true if some changes happened at this position, false otherwise
        internal static bool MergeFormattingInlines(TextPointer position)
        {
            // Remove unnecessary Spans around this position
            RemoveUnnecessarySpans(position);

            // Delete empty formatting elements at this position (if any)
            ExtractEmptyFormattingElements(position);

            // Skip formatting tags towards potential merging position
            while (position.GetPointerContext(LogicalDirection.Backward) == TextPointerContext.ElementStart &&
                TextSchema.IsMergeableInline(position.Parent.GetType()))
            {
                position = ((Inline)position.Parent).ElementStart;
            }
            while (position.GetPointerContext(LogicalDirection.Forward) == TextPointerContext.ElementEnd &&
                TextSchema.IsMergeableInline(position.Parent.GetType()))
            {
                position = ((Inline)position.Parent).ElementEnd;
            }

            // Merge formatting Inlines at this position
            Inline firstInline, secondInline;
            bool merged = false;
            while (
                position.GetPointerContext(LogicalDirection.Backward) == TextPointerContext.ElementEnd && 
                position.GetPointerContext(LogicalDirection.Forward) == TextPointerContext.ElementStart &&
                (firstInline = position.GetAdjacentElement(LogicalDirection.Backward) as Inline) != null &&
                (secondInline = position.GetAdjacentElement(LogicalDirection.Forward) as Inline) != null)
            {
                if (TextSchema.IsFormattingType(firstInline.GetType()) && firstInline.TextRange.IsEmpty)
                {
                    firstInline.RepositionWithContent(null);
                    merged = true;
                }
                else if (TextSchema.IsFormattingType(secondInline.GetType()) && secondInline.TextRange.IsEmpty)
                {
                    secondInline.RepositionWithContent(null);
                    merged = true;
                }
                else if (TextSchema.IsKnownType(firstInline.GetType()) && TextSchema.IsKnownType(secondInline.GetType()) &&
                    (firstInline is Run && secondInline is Run || firstInline is Span && secondInline is Span) &&
                    TextSchema.IsMergeableInline(firstInline.GetType()) && TextSchema.IsMergeableInline(secondInline.GetType())
                    && CharacterPropertiesAreEqual(firstInline, secondInline))
                {
                    firstInline.Reposition(firstInline.ElementStart, secondInline.ElementEnd);
                    secondInline.Reposition(null, null);
                    merged = true;
                }
                else
                {
                    break;
                }
            }

            // Now that Inlines have been merged we can try to optimize tree structure
            // by eliminating some unecessary wrapping Inlines
            if (merged)
            {
                RemoveUnnecessarySpans(position);
            }

            return merged;
        }

        // Inspects the tree up from a given position to find Span elements
        // wrapping exactly one other Span or Run - and removes them
        // after transferring all affected properties into inner element.
        private static void RemoveUnnecessarySpans(TextPointer position)
        {
            Inline inline = position.Parent as Inline;

            while (inline != null)
            {
                if (inline.Parent != null &&
                    TextSchema.IsMergeableInline(inline.Parent.GetType()) &&
                    TextSchema.IsKnownType(inline.Parent.GetType()) &&
                    inline.ElementStart.GetPointerContext(LogicalDirection.Backward) == TextPointerContext.ElementStart &&
                    inline.ElementEnd.GetPointerContext(LogicalDirection.Forward) == TextPointerContext.ElementEnd)
                {
                    // Parent of this inline can be deleted. Let's delete it.  

                    Span parentSpan = (Span)inline.Parent;                  

                    if (parentSpan.Parent == null)
                    {
                        break;
                    }

                    // We are going to delete a parent of this inline as it wraps only one child.
                    // Before deleting we need to transfer all properties that are affected by that parent inline.

                    // Transfer inheritable properties
                    AvaloniaProperty[] inheritableProperties = TextSchema.GetInheritableProperties(typeof(Span));
                    for (int i = 0; i < inheritableProperties.Length; i++)
                    {
                        AvaloniaProperty property = inheritableProperties[i];

                        object inlineValue = inline.GetValue(property);
                        object parentSpanValue = parentSpan.GetValue(property);

                        if (!TextSchema.ValuesAreEqual(inlineValue, parentSpanValue))
                        {
                            // Inner inline sets its own value for this property. We don't need to transfer it.
                            continue;
                        }

                        object outerValue = parentSpan.Parent.GetValue(property);

                        if (!TextSchema.ValuesAreEqual(inlineValue, outerValue))
                        {
                            inline.SetValue(property, parentSpanValue);
                        }
                    }

                    // Transfer non-inheritable properties
                    // It only aims for the specific set of non-inheritable properties defined in TextSchema.
                    // These properties are safe to be transferred from outer scope to inner scope. 
                    AvaloniaProperty[] nonInheritableProperties = TextSchema.GetNoninheritableProperties(typeof(Span));
                    for (int i = 0; i < nonInheritableProperties.Length; i++)
                    {
                        AvaloniaProperty property = nonInheritableProperties[i];

                        //bool hasModifiers;

                        // Check if the property value is default and not animated/coerced/data-bound.
                        bool isParentValueDefault = parentSpan.IsSet(property); /*(*/  //ToDo: IsDefault(property)
                        //       parentSpan.GetValueSource(property, null, out hasModifiers) == BaseValueSourceInternal.Default 
                        //    && !hasModifiers
                        //    );

                        bool isInlineValueDefault = inline.IsSet(property); /*(*/
                            //   inline.GetValueSource(property, null, out hasModifiers) == BaseValueSourceInternal.Default 
                            //&& !hasModifiers
                            //);

                        if (isInlineValueDefault && !isParentValueDefault)
                        {
                            inline.SetValue(property, parentSpan.GetValue(property));
                        }
                    }

                    // We can now remove the wrapping element
                    parentSpan.Reposition(null, null);
                }
                else
                {
                    // Parent of this inline cannot be deleted. Let's see what we can do with its parent
                    inline = inline.Parent as Inline;
                }
            }
        }

        // Removes inline properties that affect formatting from the given range
        internal static void CharacterResetFormatting(TextPointer start, TextPointer end)
        {
            if (start.CompareTo(end) < 0)
            {
                // Split formatting elements at range boundaries
                start = SplitFormattingElements(start, /*keepEmptyFormatting:*/false, /*preserveStructuralFormatting*/true, /*limitingAncestor*/null);
                end = SplitFormattingElements(end, /*keepEmptyFormatting:*/false, /*preserveStructuralFormatting*/true, /*limitingAncestor*/null);

                while (start.CompareTo(end) < 0)
                {
                    if (start.GetPointerContext(LogicalDirection.Backward) == TextPointerContext.ElementStart)
                    {
                        // When entering a next element check whether we should clear its inline properties.
                        TextElement parent = (TextElement)start.Parent;

                        // Note we do cleaning for Inline elements only - so properties set on Paragraphs
                        // and other blocks will stay unchanged even if they set as local value.

                        if (parent is Span && parent.ContentEnd.CompareTo(end) > 0)
                        {
                            // Preserve Hyperlink/Span properties when it is partially selected
                        }
                        // We can't assume that custom types derived from Span, once their formatting
                        // properties are removed, can be transformed into a Span.  So treat custom
                        // types as inlines, even if they're derived from Span.
                        else if (parent is Span && TextSchema.IsKnownType(parent.GetType()))
                        {
                            // Remember a position to merge inlines
                            TextPointer mergePosition = parent.ElementStart;

                            // Preserve only non-formatting properties of original span element.
                            Span newSpan = TransferNonFormattingInlineProperties((Span)parent);
                            if (newSpan != null)
                            {
                                newSpan.Reposition(parent.ElementStart, parent.ElementEnd);
                                mergePosition = newSpan.ElementStart;
                            }

                            // Throw away original span
                            parent.Reposition(null, null);

                            // Now that content has changed, we must try to merge inlines at this position
                            MergeFormattingInlines(mergePosition);
                        }
                        else if (parent is Inline)
                        {
                            ClearFormattingInlineProperties((Inline)parent);
                            // Now that properties may be removed we must try to merge this element with a preceding one
                            MergeFormattingInlines(parent.ElementStart);
                        }
                    }
                    start = start.GetNextContextPosition(LogicalDirection.Forward);
                }

                // At the end try ro merge elements at end position
                MergeFormattingInlines(end);
            }
        }

        // Helper to clear formatting properties from passed inline element, preserving only non-formatting ones
        private static void ClearFormattingInlineProperties(Inline inline)
        {
            // Clear all properties from this inline element
            LocalValueEnumerator properties = inline.GetLocalValueEnumerator();
            while (properties.MoveNext())
            {
                AvaloniaProperty property = properties.Current.Property;

                // Skip readonly and non-formatting properties
                if (property.IsReadOnly || TextSchema.IsNonFormattingCharacterProperty(property))
                {
                    continue;
                }

                inline.ClearValue(properties.Current.Property);
            }
        }

        // When source span has only character formatting properties, returns null.
        // Otherwise, when source span has at least one non-formatting character property (such as FlowDirection),
        // this helper returns a Span element preserving only such properties from source span.
        private static Span TransferNonFormattingInlineProperties(Span source)
        {
            Span span = null;

            AvaloniaProperty[] nonFormattingCharacterProperties = TextSchema.GetNonFormattingCharacterProperties();
            for (int i = 0; i < nonFormattingCharacterProperties.Length; i++)
            {
                object value = source.GetValue(nonFormattingCharacterProperties[i]);
                object outerContextValue = ((ITextPointer)source.ElementStart).GetValue(nonFormattingCharacterProperties[i]);

                if (!TextSchema.ValuesAreEqual(value, outerContextValue))
                {
                    if (span == null)
                    {
                        span = new Span();
                    }
                    span.SetValue(nonFormattingCharacterProperties[i], value); 
                }
            }
            return span;
        }

        #endregion Character Formatting

        #region Paragraph Editing

        // ....................................................................
        //
        // Paragraph Editing
        //
        // ....................................................................

        // Splits the parent of the given breakPosition into two
        // elements with equivalent set of properties.
        internal static TextPointer SplitElement(TextPointer position)
        {
            TextElement element = (TextElement)position.Parent;

            if (position.IsFrozen)
            {
                position = new TextPointer(position);
            }

            TextElement newElement;
            if (position.GetPointerContext(LogicalDirection.Forward) == TextPointerContext.ElementEnd)
            {
                // A simple case when the new element can be added after the old one
                newElement = InsertElementClone(element.ElementEnd, element.ElementEnd, element);

                position.MoveToPosition(element.ElementEnd);
            }
            else if (position.GetPointerContext(LogicalDirection.Backward) == TextPointerContext.ElementStart)
            {
                newElement = InsertElementClone(element.ElementStart, element.ElementStart, element);

                position.MoveToPosition(element.ElementStart);
            }
            else
            {
                newElement = InsertElementClone(position, element.ContentEnd, element);

                // Reposition the old element to the first half of content
                element.Reposition(element.ContentStart, newElement.ElementStart);

                position.MoveToPosition(element.ElementEnd);
            }

            Invariant.Assert(position.GetPointerContext(LogicalDirection.Backward) == TextPointerContext.ElementEnd, "position must be after ElementEnd");
            Invariant.Assert(position.GetPointerContext(LogicalDirection.Forward) == TextPointerContext.ElementStart, "position must be before ElementStart");
            return position;
        }

        /// <summary>
        /// Insert paragraph break at the End position of a range.
        /// It only affects specified position - not a whole range.
        /// So it is essentially TextContainer-level (low-level) operation.
        /// </summary>
        /// <param name="position">
        /// Position at which the content should be split into two paragraphs.
        /// After the operation breakPosition moved into a beginning of the
        /// second paragraph after all opening tags created by splitting
        /// (this position may be not-normalized though if there are some
        /// other opening formatting tags following the position - this may
        /// be important for reading from xml when pasting point was before
        /// some opening formatting tags but after non-whitespace characters).
        /// </param>
        /// <param name="moveIntoSecondParagraph">
        /// True means that resulting TextPointer must be moved into the second paragraph.
        /// False means that resulting pointer remains in a non-normalized position
        /// between two paragraphs (or list items).
        /// </param>
        /// <remarks>
        /// This function could be implemented from TextContainer class.
        /// </remarks>
        /// <returns>
        /// If position passed was in paragraph content, returns a TextPointer 
        /// at an ContentStart of the second paragraph.
        /// If position passed was at a structural boundary (specifically table row end,
        /// block ui container start/end or before first table in a collection of blocks),
        /// then an implicit paragraph is inserted at the boundary and a position at its
        /// ContentStart is returned.
        /// </returns>
        //internal static TextPointer InsertParagraphBreak(TextPointer position, bool moveIntoSecondParagraph)
        //{
        //    Invariant.Assert(position.TextContainer.Parent == null || TextSchema.IsValidChildOfContainer(position.TextContainer.Parent.GetType(), typeof(Paragraph)));

        //    bool structuralBoundaryCrossed = TextPointerBase.IsAtRowEnd(position) ||
        //        TextPointerBase.IsBeforeFirstTable(position) ||
        //        TextPointerBase.IsInBlockUIContainer(position);

        //    if (position.Paragraph == null)
        //    {
        //        // Ensure insertion position, in case original position is not in text content.
        //        position = TextRangeEditTables.EnsureInsertionPosition(position);
        //    }

        //    Inline ancestor = position.GetNonMergeableInlineAncestor();
        //    if (ancestor != null)
        //    {
        //        Invariant.Assert(TextPointerBase.IsPositionAtNonMergeableInlineBoundary(position), "Position must be at hyperlink boundary!");

        //        // If position is at a hyperlink boundary, move outside hyperlink element scope 
        //        // so that we can successfuly split formatting elements upto paragraph ancestor.

        //        position = position.IsAtNonMergeableInlineStart ? ancestor.ElementStart : ancestor.ElementEnd;
        //    }

        //    Paragraph paragraph = position.Paragraph;
        //    if (paragraph == null)
        //    {
        //        // At this point, we expect we're working in a fragment of Inlines only.
        //        Invariant.Assert(position.TextContainer.Parent == null);

        //        // Add a parent Paragraph to split.
        //        paragraph = new Paragraph();
        //        paragraph.Reposition(position.DocumentStart, position.DocumentEnd);
        //    }

        //    if (structuralBoundaryCrossed)
        //    {
        //        // In case structural boundary was crossed, an implicit paragraph was inserted in EnsureInsertionPosition. 
        //        // No need to insert another paragraph break.
        //        return position;
        //    }

        //    TextPointer breakPosition = position;

        //    // Split all inline elements up to this paragraph
        //    breakPosition = SplitFormattingElements(breakPosition, /*keepEmptyFormatting:*/true);
        //    Invariant.Assert(breakPosition.Parent == paragraph, "breakPosition must be in paragraph scope after splitting formatting elements");

        //    // Decide whether we need to split ListItem around this paragraph (if any).
        //    // We are splitting a list item if this paragraph is the only paragraph in a list item.
        //    // Otherwise we simply produce new paragraphs within the same list item.
        //    //bool needToSplitListItem = TextPointerBase.GetImmediateListItem(paragraph.ContentStart) != null;

        //    breakPosition = SplitElement(breakPosition);

        //    // Also split ListItem (if any)
        //    //if (needToSplitListItem)
        //    //{
        //    //    Invariant.Assert(breakPosition.Parent is ListItem, "breakPosition must be in ListItem scope");
        //    //    breakPosition = SplitElement(breakPosition);
        //    //}

        //    if (moveIntoSecondParagraph)
        //    {
        //        // Move breakPosition inside of the second paragraph
        //        while (/*!(breakPosition.Parent is Paragraph) &&*/ breakPosition.GetPointerContext(LogicalDirection.Forward) == TextPointerContext.ElementStart)
        //        {
        //            breakPosition = breakPosition.GetNextContextPosition(LogicalDirection.Forward);
        //        }

        //        // Normalize with forward gravity
        //        breakPosition = breakPosition.GetInsertionPosition(LogicalDirection.Forward);
        //    }

        //    return breakPosition;
        //}

        /// <summary>
        /// Insert a LineBreak element at the given position.
        /// If position's parent is a Paragraph or Span, simply insert a LineBreak element at this position.
        /// Otherwise, ensure insertion position and insert a LineBreak element at insertion position in text content.
        /// </summary>
        /// <param name="position">
        /// </param>
        /// <returns>
        /// TextPointer positioned in the beginning of a Run immediately following a LineBreak inserted.
        /// </returns>
        internal static TextPointer InsertLineBreak(TextPointer position)
        {
            //if (!TextSchema.IsValidChild(/*position*/position, /*childType*/typeof(LineBreak)))
            //{
            //    // Ensure insertion position, in case position's parent is not a paragraph/span element.
            //    position = TextRangeEditTables.EnsureInsertionPosition(position);
            //}

            if (TextSchema.IsInTextContent(position))
            {
                // Split parent Run element, if position is inside of Run scope.
                position = SplitElement(position);
            }

            Invariant.Assert(TextSchema.IsValidChild(/*position*/position, /*childType*/typeof(LineBreak)), 
                "position must be in valid scope now to insert a LineBreak element");

            LineBreak lineBreak = new LineBreak();

            position.InsertTextElement(lineBreak);

            return lineBreak.ElementEnd.GetInsertionPosition(LogicalDirection.Forward);
        }

        /// <summary>
        /// Applies formatting properties for whole block elements.
        /// </summary>
        /// <param name="start">
        /// a position within first block in sequence
        /// </param>
        /// <param name="end">
        /// a positionn within last block in sequence
        /// </param>
        /// <param name="property">
        /// property changed on blocks
        /// </param>
        /// <param name="value">
        /// value for the property
        /// </param>
        //internal static void SetParagraphProperty(TextPointer start, TextPointer end, AvaloniaProperty property, object value)
        //{
        //    SetParagraphProperty(start, end, property, value, PropertyValueAction.SetValue);
        //}

        /// <summary>
        /// Applies formatting properties for whole block elements.
        /// </summary>
        /// <param name="start">
        /// a position within first block in sequence
        /// </param>
        /// <param name="end">
        /// a positionn within last block in sequence
        /// </param>
        /// <param name="property">
        /// property changed on blocks
        /// </param>
        /// <param name="value">
        /// value for the property
        /// </param>
        /// <param name="propertyValueAction">
        /// Specifies how to use the value - as absolute, as increment or a decrement.
        /// </param>
        //internal static void SetParagraphProperty(TextPointer start, TextPointer end, AvaloniaProperty property, object value, PropertyValueAction propertyValueAction)
        //{
        //    Invariant.Assert(start != null, "null check: start");
        //    Invariant.Assert(end != null, "null check: end");
        //    Invariant.Assert(start.CompareTo(end) <= 0, "expecting: start <= end");
        //    Invariant.Assert(property != null, "null check: property");

        //    // Exclude last opening tag to avoid affecting a paragraph following the selection
        //    end = (TextPointer)TextRangeEdit.GetAdjustedRangeEnd(start, end);

        //    // Expand start pointer to the beginning of the first paragraph/blockuicontainer
        //    Block startParagraphOrBlockUIContainer = start.ParagraphOrBlockUIContainer;
        //    if (startParagraphOrBlockUIContainer != null)
        //    {
        //        start = startParagraphOrBlockUIContainer.ContentStart;
        //    }

        //    // Applying FlowDirection requires splitting all containing lists on the range boundaries
        //    // because the property is applied to whole List element (to affect bullet appearence)
        //    if (property == Block.FlowDirectionProperty)
        //    {
        //        // Split any boundary lists if needed.
        //        // We want to maintain the invariant that all lists and paragraphs within a list, have the same FlowDirection value.
        //        // If paragraph FlowDirection command requests a different value of FlowDirection on parts of a list, 
        //        // we split the list to maintain this invariant.
        //        if (!TextRangeEditLists.SplitListsForFlowDirectionChange(start, end, value))
        //        {
        //            // If lists at start and end cannot be split successfully, we cannot apply FlowDirection property to the paragraph content.
        //            return;
        //        }

        //        // And expand range start to the beginning of the containing list
        //        ListItem listItem = start.GetListAncestor();
        //        if (listItem != null && listItem.List != null)
        //        {
        //            start = listItem.List.ElementStart;
        //        }
        //    }

        //    // Walk all paragraphs in the affected segment. For FlowDirection property, also walk lists.
        //    SetParagraphPropertyWorker(start, end, property, value, propertyValueAction);
        //}

        // Worker for SetParagraphProperty, iterates over Blocks recursively.
        //private static void SetParagraphPropertyWorker(TextPointer start, TextPointer end, AvaloniaProperty property, object value, PropertyValueAction propertyValueAction)
        //{
        //    Block block = GetNextBlock(start, end);

        //    while (block != null)
        //    {
        //        if (TextSchema.IsParagraphOrBlockUIContainer(block.GetType()))
        //        {
        //            // Get the parent to check the parent FlowDirection with current
        //            IAvaloniaObject parent = start.TextContainer.Parent;

        //            SetPropertyOnParagraphOrBlockUIContainer(parent, block, property, value, propertyValueAction);

        //            // Go to paragraph/BUIC end position, normalize forward
        //            start = block.ElementEnd.GetPositionAtOffset(0, LogicalDirection.Forward);
        //        }
        //        else if (block is List)
        //        {
        //            // Apply property value to content first, recursively, since
        //            // (potentially) setting FlowDirection on the parent List will
        //            // affect child elements.
        //            TextPointer contentStart = block.ContentStart.GetPositionAtOffset(0, LogicalDirection.Forward); // Normalize forward;
        //            contentStart = contentStart.GetNextContextPosition(LogicalDirection.Forward); // Leave scope of initial List.
        //            TextPointer contentEnd = block.ContentEnd;
        //            SetParagraphPropertyWorker(contentStart, contentEnd, property, value, propertyValueAction);

        //            // Special cases for applying paragraph properties to Lists
        //            if (property == Block.FlowDirectionProperty)
        //            {
        //                object currentValue = block.GetValue(property);

        //                // Set FlowDirection property on List
        //                SetPropertyValue(block, property, currentValue:currentValue, newValue:value);

        //                // Only swap Left and Right margins of the list when FlowDirection changes. This ensures indentation is mirrored correctly.
        //                if (!Object.Equals(currentValue, value))
        //                {
        //                    SwapBlockLeftAndRightMargins(block);
        //                }
        //            }

        //            // Go to end position, normalize forward.
        //            start = block.ElementEnd.GetPositionAtOffset(0, LogicalDirection.Forward);
        //        }

        //        block = GetNextBlock(start, end);
        //    }
        //}

        // Helper for SetParagraphProperty -- applies given property value to passed block element.
        //private static void SetPropertyOnParagraphOrBlockUIContainer(IAvaloniaObject parent, Block block, AvaloniaProperty property, object value, PropertyValueAction propertyValueAction)
        //{
        //    // Get the parent flow direction
        //    FlowDirection parentFlowDirection;

        //    if (parent != null)
        //    {
        //        parentFlowDirection = (FlowDirection)parent.GetValue(FrameworkElement.FlowDirectionProperty);
        //    }
        //    else
        //    {
        //        parentFlowDirection = (FlowDirection)FrameworkElement.FlowDirectionProperty.GetDefaultValue(typeof(FrameworkElement));
        //    }

        //    // Some of paragraph operations depend on its flow direction, so get it first.
        //    FlowDirection flowDirection = (FlowDirection)block.GetValue(Block.FlowDirectionProperty);

        //    // Inspect a property value for this paragraph
        //    object currentValue = block.GetValue(property);
        //    object newValue = value;

        //    // If we're setting a structural property on a Paragraph, we need to preserve
        //    // the current value on its children.
        //    PreserveBlockContentStructuralProperty(block, property, currentValue, value);

        //    if (property.PropertyType == typeof(Thickness))
        //    {
        //        // For Margin, Padding, Border - apply the following logic:
        //        Invariant.Assert(currentValue is Thickness, "Expecting the currentValue to be of Thinkness type");
        //        Invariant.Assert(newValue is Thickness, "Expecting the newValue to be of Thinkness type");

        //        newValue = ComputeNewThicknessValue((Thickness)currentValue, (Thickness)newValue, parentFlowDirection, flowDirection, propertyValueAction);
        //    }
        //    else if (property == Paragraph.TextAlignmentProperty)
        //    {
        //        Invariant.Assert(value is TextAlignment, "Expecting TextAlignment as a value of a Paragraph.TextAlignmentProperty");

        //        // TextAlignment must be reverted for RightToLeft flow direction
        //        newValue = ComputeNewTextAlignmentValue((TextAlignment)value, flowDirection);

        //        // For BlockUIContainer text alignment must be translated into
        //        // HorizontalAlignment of the child embedded object.
        //        if (block is BlockUIContainer)
        //        {
        //            UIElement embeddedElement = ((BlockUIContainer)block).Child;
        //            if (embeddedElement != null)
        //            {
        //                HorizontalAlignment horizontalAlignment = GetHorizontalAlignmentFromTextAlignment((TextAlignment)newValue);

        //                // Create an undo unit for property change on embedded framework element.
        //                UIElementPropertyUndoUnit.Add(block.TextContainer, embeddedElement, FrameworkElement.HorizontalAlignmentProperty, horizontalAlignment);
        //                embeddedElement.SetValue(FrameworkElement.HorizontalAlignmentProperty, horizontalAlignment);
        //            }
        //        }
        //    }
        //    else if (currentValue is double)
        //    {
        //        newValue = GetNewDoubleValue(property, (double)currentValue, (double)newValue, propertyValueAction);
        //    }

        //    SetPropertyValue(block, property, currentValue, newValue);

        //    if (property == Block.FlowDirectionProperty)
        //    {
        //        // Only swap Left and Right margins of the paragraph when FlowDirection changes. This ensures indentation is mirrored correctly.
        //        if (!Object.Equals(currentValue, newValue))
        //        {
        //            SwapBlockLeftAndRightMargins(block);
        //        }
        //    }
        //}

        // Helper for SetPropertyOnParagraphOrBlockUIContainer.
        //
        // When setting a structural property on a Block, we must be careful to preserve
        // the current value on its children.
        //
        // A structural character property is more strict for its scope than other (non-structural) inline properties (such as fontweight).
        // While the associativity rule holds true for non-structural properties when there values are equal,
        //     (FontWeight)A (FontWeight)B == (FontWeight) AB
        // this does not hold true for structual properties even when there values may be equal,
        //     (FlowDirection)A (FlowDirection)B != (FlowDirection)A B 
        //private static void PreserveBlockContentStructuralProperty(Block block, AvaloniaProperty property, object currentValue, object newValue)
        //{
        //    Paragraph paragraph = block as Paragraph;

        //    if (paragraph != null &&
        //        TextSchema.IsStructuralCharacterProperty(property) &&
        //        !TextSchema.ValuesAreEqual(currentValue, newValue))
        //    {
        //        // First drill down to the first run of multiple children, or the first
        //        // single child with a local value.
        //        Inline firstChild = paragraph.Inlines.FirstInline;
        //        Inline lastChild = paragraph.Inlines.LastInline;

        //        while (firstChild != null &&
        //               firstChild == lastChild &&
        //               firstChild is Span &&
        //               !HasLocalPropertyValue(firstChild, property))
        //        {
        //            firstChild = ((Span)firstChild).Inlines.FirstInline;
        //            lastChild = ((Span)lastChild).Inlines.LastInline;
        //        }

        //        // Set the old value on the existing content.
        //        if (firstChild != lastChild)
        //        {
        //            Inline nextChild;

        //            do
        //            {
        //                // Find a run of children with the same property value.

        //                object firstChildValue = firstChild.GetValue(property);
        //                lastChild = firstChild;

        //                while (true)
        //                {
        //                    nextChild = (Inline)lastChild.NextElement;

        //                    if (nextChild == null)
        //                        break;
        //                    if (!TextSchema.ValuesAreEqual(nextChild.GetValue(property), firstChildValue))
        //                        break;

        //                    lastChild = nextChild;
        //                }

        //                if (TextSchema.ValuesAreEqual(firstChildValue, currentValue))
        //                {
        //                    if (firstChild != lastChild)
        //                    {
        //                        // Wrap multiple children in a new Span with the old value.
        //                        TextPointer start = firstChild.ElementStart.GetFrozenPointer(LogicalDirection.Backward);
        //                        TextPointer end = lastChild.ElementEnd.GetFrozenPointer(LogicalDirection.Forward);

        //                        // Because SetStructuralInlineProperty doesn't know that we're about to change the Paragraph's
        //                        // property value, it will optimize away Spans.  We still want to use it though, to canonicalize
        //                        // the content.
        //                        SetStructuralInlineProperty(start, end, property, currentValue);

        //                        firstChild = (Inline)start.GetAdjacentElement(LogicalDirection.Forward);
        //                        lastChild = (Inline)end.GetAdjacentElement(LogicalDirection.Backward);

        //                        if (firstChild != lastChild)
        //                        {
        //                            Span span = firstChild.Parent as Span;

        //                            if (span == null || span.Inlines.FirstInline != firstChild || span.Inlines.LastInline != lastChild)
        //                            {
        //                                span = new Span(firstChild.ElementStart, lastChild.ElementEnd);
        //                            }

        //                            span.SetValue(property, currentValue);
        //                        }
        //                    }

        //                    if (firstChild == lastChild)
        //                    {
        //                        SetStructuralPropertyOnInline(firstChild, property, currentValue);
        //                    }
        //                }

        //                firstChild = nextChild;
        //            }
        //            while (firstChild != null);
        //        }
        //        else
        //        {
        //            // If the only child is a Run, set the value directly.
        //            // Otherwise there's no need to set the value.
        //            SetStructuralPropertyOnInline(firstChild, property, currentValue);
        //        }
        //    }
        //}

        // Helper for PreserveBlockContentStructuralProperty.
        private static void SetStructuralPropertyOnInline(Inline inline, AvaloniaProperty property, object value)
        {
            if (inline is Run &&
                !inline.IsEmpty &&
                !HasLocalPropertyValue(inline, property))
            {
                // If the only child is a Run, set the value directly.
                // Otherwise there's no need to set the value.
                inline.SetValue(property, value);
            }
        }

        // Finds a Paragraph/BlockUIContainer/List element with ElementStart before or at the given pointer
        // Creates implicit paragraphs at potential paragraph positions if needed
        //private static Block GetNextBlock(TextPointer pointer, TextPointer limit)
        //{
        //    Block block = null;

        //    while (pointer != null && pointer.CompareTo(limit) <= 0)
        //    {
        //        if (pointer.GetPointerContext(LogicalDirection.Backward) == TextPointerContext.ElementStart)
        //        {
        //            block = pointer.Parent as Block;
        //            if (block is Paragraph || block is BlockUIContainer || block is List)
        //            {
        //                break;
        //            }
        //        }
                
        //        if (TextPointerBase.IsAtPotentialParagraphPosition(pointer))
        //        {
        //            pointer = TextRangeEditTables.EnsureInsertionPosition(pointer);
        //            block = pointer.Paragraph;
        //            Invariant.Assert(block != null);
        //            break;
        //        }

        //        // Advance the scanning pointer
        //        pointer = pointer.GetNextContextPosition(LogicalDirection.Forward);
        //    }

        //    return block;
        //}

        // Helper for SetParagraphProperty
        private static Thickness ComputeNewThicknessValue(Thickness currentThickness, Thickness newThickness,
            FlowDirection parentFlowDirection, FlowDirection flowDirection, PropertyValueAction propertyValueAction)
        {
            // Negative value for particular axis means "leave it unchanged"
            double topMargin = newThickness.Top < 0 
                ? currentThickness.Top
                : GetNewDoubleValue(null, currentThickness.Top, newThickness.Top, propertyValueAction);

            double bottomMargin = newThickness.Bottom < 0
                ? currentThickness.Bottom
                : GetNewDoubleValue(null, currentThickness.Bottom, newThickness.Bottom, propertyValueAction);

            double leftMargin;
            double rightMargin;

            if (parentFlowDirection != flowDirection)
            {
                // In case of mismatching FlowDirection between parent and current,
                // we apply value.Left to currentValue.Right and vice versa.
                // The caller of the method must account for that and use Left/Right margins appropriately.
                leftMargin = newThickness.Right < 0
                    ? currentThickness.Left
                    : GetNewDoubleValue(null, currentThickness.Left, newThickness.Right, propertyValueAction);

                rightMargin = newThickness.Left < 0
                    ? currentThickness.Right
                    : GetNewDoubleValue(null, currentThickness.Right, newThickness.Left, propertyValueAction);
            }
            else
            {
                leftMargin = newThickness.Left < 0
                    ? currentThickness.Left
                    : GetNewDoubleValue(null, currentThickness.Left, newThickness.Left, propertyValueAction);

                rightMargin = newThickness.Right < 0
                    ? currentThickness.Right
                    : GetNewDoubleValue(null, currentThickness.Right, newThickness.Right, propertyValueAction);
            }

            return new Thickness(leftMargin, topMargin, rightMargin, bottomMargin);
        }

        // Helper for SetParagraphProperty, flips TextAligment values when FlowDirection is RTL.
        private static TextAlignment ComputeNewTextAlignmentValue(TextAlignment textAlignment, FlowDirection flowDirection)
        {
            if (textAlignment == TextAlignment.Left)
            {
                textAlignment = (flowDirection == FlowDirection.LeftToRight) ? TextAlignment.Left : TextAlignment.Right;
            }
            else if (textAlignment == TextAlignment.Right)
            {
                textAlignment = (flowDirection == FlowDirection.LeftToRight) ? TextAlignment.Right : TextAlignment.Left;
            }

            return textAlignment;
        }

        /// <summary>
        /// Calculates valid value for specified DP, current and new (desired) value,
        /// and <see cref="PropertyValueAction"/>.
        /// The value is made to adhere editor's acceptable range of values for given property.
        /// If the value is invalid, then closest valid bound of the range is returned.
        /// </summary>
        /// <param name="property"></param>
        /// <param name="currentValue"></param>
        /// <param name="newValue"></param>
        /// <param name="propertyValueAction"></param>
        /// <returns>new value</returns>
        private static double GetNewDoubleValue(AvaloniaProperty property, double currentValue, double newValue, PropertyValueAction propertyValueAction)
        {
            double outValue = NewValue(currentValue, newValue, propertyValueAction);
            return DoublePropertyBounds.GetClosestValidValue(property, outValue);
        }

        // Applies newValue to the currentValue according to a propertyValueAction -
        // increments or just sets it.
        private static double NewValue(double currentValue, double newValue, PropertyValueAction propertyValueAction)
        {
            if (double.IsNaN(newValue))
            {
                return newValue;
            }

            if (double.IsNaN(currentValue))
            {
                currentValue = 0.0;
            }

            newValue =
                propertyValueAction == PropertyValueAction.IncreaseByAbsoluteValue ? currentValue + newValue :
                propertyValueAction == PropertyValueAction.DecreaseByAbsoluteValue ? currentValue - newValue :
                propertyValueAction == PropertyValueAction.IncreaseByPercentageValue ? currentValue * (1.0 + newValue / 100) :
                propertyValueAction == PropertyValueAction.DecreaseByPercentageValue ? currentValue * (1.0 - newValue / 100) :
                newValue;

            return newValue;
        }

        // Translates TextAlignment value into corresponding HorizontalAlignment value.
        // Used in applying Paragraph.TextAlignmentProperty to BlockUIContainer elements.
        //internal static HorizontalAlignment GetHorizontalAlignmentFromTextAlignment(TextAlignment textAlignment)
        //{
        //    HorizontalAlignment horizontalAlignment;
        //    switch (textAlignment)
        //    {
        //        default:
        //        case TextAlignment.Left:
        //            horizontalAlignment = HorizontalAlignment.Left;
        //            break;
        //        case TextAlignment.Center:
        //            horizontalAlignment = HorizontalAlignment.Center;
        //            break;
        //        case TextAlignment.Right:
        //            horizontalAlignment = HorizontalAlignment.Right;
        //            break;
        //        case TextAlignment.Justify:
        //            horizontalAlignment = HorizontalAlignment.Stretch;
        //            break;
        //    }

        //    return horizontalAlignment;
        //}

        // Translates HorizontalAlignment value into corresponding TextAlignment value.
        //internal static TextAlignment GetTextAlignmentFromHorizontalAlignment(HorizontalAlignment horizontalAlignment)
        //{
        //    TextAlignment textAlignment;
        //    switch (horizontalAlignment)
        //    {                
        //        case HorizontalAlignment.Left:
        //            textAlignment = TextAlignment.Left;
        //            break;
        //        case HorizontalAlignment.Center:
        //            textAlignment = TextAlignment.Center;
        //            break;
        //        case HorizontalAlignment.Right:
        //            textAlignment = TextAlignment.Right;
        //            break;
        //        default:
        //        case HorizontalAlignment.Stretch:
        //            textAlignment = TextAlignment.Justify;
        //            break;
        //    }

        //    return textAlignment;
        //}

        // Helper to set property value on element.
        private static void SetPropertyValue(TextElement element, AvaloniaProperty property, object currentValue, object newValue)
        {
            if (!TextSchema.ValuesAreEqual(newValue, currentValue))
            {
                // first clear and see if it will do
                element.ClearValue(property);

                // if still need it, set it
                if (!TextSchema.ValuesAreEqual(newValue, element.GetValue(property)))
                {
                    element.SetValue(property, newValue);
                }
            }
        }

        // Helper that swaps the left and right margins of a block element.
        //private static void SwapBlockLeftAndRightMargins(Block block)
        //{
        //    object value = block.GetValue(Block.MarginProperty);

        //    if (value is Thickness)
        //    {
        //        if (Paragraph.IsMarginAuto((Thickness)value))
        //        {
        //            // Nothing to do for auto thickess
        //        }
        //        else
        //        {
        //            // Swap left and right values
        //            object newValue = new Thickness(
        //                /*left*/((Thickness)value).Right, 
        //                /*top:*/((Thickness)value).Top,
        //                /*right:*/((Thickness)value).Left, 
        //                /*bottom:*/((Thickness)value).Bottom);

        //            SetPropertyValue(block, Block.MarginProperty, value, newValue);
        //        }
        //    }
        //}

        // Returns a pointer of a text range adjusted so it does not affect
        // the paragraph following the selection.
        internal static ITextPointer GetAdjustedRangeEnd(ITextPointer rangeStart, ITextPointer rangeEnd)
        {
            if (rangeStart.CompareTo(rangeEnd) < 0 && rangeEnd.GetPointerContext(LogicalDirection.Backward) == TextPointerContext.ElementStart)
            {
                rangeEnd = rangeEnd.GetNextInsertionPosition(LogicalDirection.Backward);
                if (rangeEnd == null)
                {
                    rangeEnd = rangeStart; // Recover position for container start case - we never return null from this method.
                }
            }
            //else if (TextPointerBase.IsAfterLastParagraph(rangeEnd))
            //{
            //    rangeEnd = rangeEnd.GetInsertionPosition(LogicalDirection.Backward);
            //}

            return rangeEnd;
        }

        // Merges Spans or Runs with equal FlowDirection that border at a given position.
        internal static void MergeFlowDirection(TextPointer position)
        {
            TextPointerContext backwardContext = position.GetPointerContext(LogicalDirection.Backward);
            TextPointerContext forwardContext = position.GetPointerContext(LogicalDirection.Forward);

            if (!(backwardContext == TextPointerContext.ElementStart || backwardContext == TextPointerContext.ElementEnd) &&
                !(forwardContext == TextPointerContext.ElementStart || forwardContext == TextPointerContext.ElementEnd))
            {
                // Early out if position is not at an Inline border.
                return;
            }

            // Find the common ancestor of the two adjacent content runs.
            while (position.GetPointerContext(LogicalDirection.Backward) == TextPointerContext.ElementStart &&
                TextSchema.IsMergeableInline(position.Parent.GetType()))
            {
                position = ((Inline)position.Parent).ElementStart;
            }
            while (position.GetPointerContext(LogicalDirection.Forward) == TextPointerContext.ElementEnd &&
                TextSchema.IsMergeableInline(position.Parent.GetType()))
            {
                position = ((Inline)position.Parent).ElementEnd;
            }
            TextElement commonAncestor = position.Parent as TextElement;

            if (!(commonAncestor is Span /*|| commonAncestor is Paragraph*/))
            {
                // Don't try to merge across Block boundaries.
                return;
            }

            // Find the previous content.
            TextPointer previousPosition = position.CreatePointer();
            while (previousPosition.GetPointerContext(LogicalDirection.Backward) == TextPointerContext.ElementEnd &&
                   TextSchema.IsMergeableInline(previousPosition.GetAdjacentElement(LogicalDirection.Backward).GetType()))
            {
                previousPosition = ((Inline)previousPosition.GetAdjacentElement(LogicalDirection.Backward)).ContentEnd;
            }
            Run previousRun = previousPosition.Parent as Run;

            // Find the next content.
            TextPointer nextPosition = position.CreatePointer();
            while (nextPosition.GetPointerContext(LogicalDirection.Forward) == TextPointerContext.ElementStart &&
                   TextSchema.IsMergeableInline(nextPosition.GetAdjacentElement(LogicalDirection.Forward).GetType()))
            {
                nextPosition = ((Inline)nextPosition.GetAdjacentElement(LogicalDirection.Forward)).ContentStart;
            }
            Run nextRun = nextPosition.Parent as Run;

            if (previousRun == null || previousRun.IsEmpty || nextRun == null || nextRun.IsEmpty)
            {
                // No text to make the merge meaningful.
                return;
            }

            FlowDirection midpointFlowDirection = (FlowDirection)commonAncestor.GetValue(Inline.FlowDirectionProperty);
            FlowDirection previousFlowDirection = (FlowDirection)previousRun.GetValue(Inline.FlowDirectionProperty);
            FlowDirection nextFlowDirection = (FlowDirection)nextRun.GetValue(Inline.FlowDirectionProperty);

            // If the previous and next content have the same FlowDirection, but their
            // common ancestor differs, we want to merge them.
            if (previousFlowDirection == nextFlowDirection &&
                previousFlowDirection != midpointFlowDirection)
            {
                // Expand the context out to include any scoping Spans with local FlowDirection.
                Inline scopingPreviousInline = GetScopingFlowDirectionInline(previousRun);
                Inline scopingNextInline = GetScopingFlowDirectionInline(nextRun);

                // Set a single FlowDirection Span over the whole lot of it.
                SetStructuralInlineProperty(scopingPreviousInline.ElementStart, scopingNextInline.ElementEnd, Inline.FlowDirectionProperty, previousFlowDirection);
            }
        }

        // Returns false if calling ApplyStructuralInlineProperty will throw an InvalidOperationException with the
        // same input parameters.
        //
        // In practice, this method returns false when the property apply would require that we split a
        // non-mergeable Inline such as Hyperlink.
        internal static bool CanApplyStructuralInlineProperty(TextPointer start, TextPointer end)
        {
            return ValidateApplyStructuralInlineProperty(start, end, TextPointer.GetCommonAncestor(start, end), null);
        }

        // .....................................................................
        //
        // Paragraph Editing Commands
        //
        // .....................................................................

        /// <summary>
        /// Increments/decrements paragraph leading maring property.
        /// For LeftToRight paragraphs a leading maring is the left marinng,
        /// for RightToLeft paragraphs it is the right maring.
        /// </summary>
        /// <param name="range"></param>
        /// <param name="increment"></param>
        /// <param name="propertyValueAction">
        /// Must be one of IncreaseValue or DecreaseValue.
        /// </param>
        //internal static void IncrementParagraphLeadingMargin(TextRange range, double increment, PropertyValueAction propertyValueAction)
        //{
        //    Invariant.Assert(increment >= 0);
        //    Invariant.Assert(propertyValueAction != PropertyValueAction.SetValue);

        //    if (increment == 0)
        //    {
        //        // Nothing to do. Just return.
        //        return;
        //    }

        //    // Note that SetParagraphProperty method will swap Left and Right margins for RightToLeft paragraphs.
        //    // Note that -1 values for Thickness axis means leaving its value as is.
        //    Thickness thickness = new Thickness(increment, -1, -1, -1);

        //    // Apply paragraph margin property
        //    TextRangeEdit.SetParagraphProperty(range.Start, range.End, Block.MarginProperty, thickness, propertyValueAction);
        //}

        /// <summary>
        /// Deletes a content covered by two positions assuming that
        /// the content crosses only inline boundaries (if at all) -
        /// no Paragraph or any other Block or structural elements are
        /// supposed to be crossed (including Floaters and Figures).
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        internal static void DeleteInlineContent(ITextPointer start, ITextPointer end)
        {
            DeleteParagraphContent(start, end);
        }

        /// <summary>
        /// Deletes a content covered by two positions assuming that
        /// the content crosses only paragraph-mergeable boundaries (if at all) -
        /// Paragraphs, Sections, Lists, ListItems, but not harder structural
        /// elements like Tables, TableCells, TableRows, Floaters, Figures.
        /// </summary>
        /// <param name="start">
        /// Position indicating a beginning of deleted content.
        /// </param>
        /// <param name="end">
        /// Position indicating an end of deleted content.
        /// </param>
        internal static void DeleteParagraphContent(ITextPointer start, ITextPointer end)
        {
            // Parameters validation
            Invariant.Assert(start != null, "null check: start");
            Invariant.Assert(end != null, "null check: end");
            Invariant.Assert(start.CompareTo(end) <= 0, "expecting: start <= end");

            if (!(start is TextPointer))
            {
                // Abstract text container. We can only use basic abstract functionality here:
                start.DeleteContentToPosition(end);
                return;
            }

            TextPointer startPosition = (TextPointer)start;
            TextPointer endPosition = (TextPointer)end;

            // Delete all equi-scoped content in the given range
            DeleteEquiScopedContent(startPosition, endPosition); // delete content runs from start to root
            DeleteEquiScopedContent(endPosition, startPosition); // delete contentruns from end to root

            // Merge crossed elements
            if (startPosition.CompareTo(endPosition) < 0)
            {
                //if (TextPointerBase.IsAfterLastParagraph(endPosition))
                //{
                    // This means that end position is after the last paragraph of a text container.

                    // When the last paragraph is empty (and selection crosses its end boundary)
                    // we need to delete it.
                    // When last paragraph is not empty, we have to leave it as is.
                    while (startPosition.GetPointerContext(LogicalDirection.Backward) == TextPointerContext.ElementStart &&
                        startPosition.GetPointerContext(LogicalDirection.Forward) == TextPointerContext.ElementEnd)
                    {
                        // This code is incorrect as it can delete last paragraph from TableCell, Section, TextFlow, ... or last Inline from a paragraph
                        TextElement parent = (TextElement)startPosition.Parent;
                        if (parent is Inline /*|| TextSchema.AllowsParagraphMerging(parent.GetType())*/)
                        {
                            parent.RepositionWithContent(null);
                        }
                        else
                        {
                            break;
                        }
                    }
                //}
                //else
                //{
                //    Block firstParagraphOrBlockUIContainer = startPosition.ParagraphOrBlockUIContainer;
                //    Block secondParagraphOrBlockUIContainer = endPosition.ParagraphOrBlockUIContainer;

                //    // If startPosition and/or endPosition is parented by an empty ListItem, create an implicit paragraph in it.
                //    // This will enable the following code to merge paragraphs in list items.

                //    if (firstParagraphOrBlockUIContainer == null && TextPointerBase.IsInEmptyListItem(startPosition))
                //    {
                //        startPosition = TextRangeEditTables.EnsureInsertionPosition(startPosition);
                //        firstParagraphOrBlockUIContainer = startPosition.Paragraph;
                //        Invariant.Assert(firstParagraphOrBlockUIContainer != null, "EnsureInsertionPosition must create a paragraph inside list item - 1");
                //    }
                //    if (secondParagraphOrBlockUIContainer == null && TextPointerBase.IsInEmptyListItem(endPosition))
                //    {
                //        endPosition = TextRangeEditTables.EnsureInsertionPosition(endPosition);
                //        secondParagraphOrBlockUIContainer = endPosition.Paragraph;
                //        Invariant.Assert(secondParagraphOrBlockUIContainer != null, "EnsureInsertionPosition must create a paragraph inside list item - 2");
                //    }

                //    if (firstParagraphOrBlockUIContainer != null && secondParagraphOrBlockUIContainer != null)
                //    {
                //        TextRangeEditLists.MergeParagraphs(firstParagraphOrBlockUIContainer, secondParagraphOrBlockUIContainer);
                //    }
                //    else
                //    {
                //        // When crossing BlockUIContainer boundaries we need to clear 
                //        // any empty BlockUIContainers and empty adjacent paragraphs
                //        MergeEmptyParagraphsAndBlockUIContainers(startPosition, endPosition);
                //    }
                //}
            }

            // Remove empty formatting elements
            MergeFormattingInlines(startPosition);
            MergeFormattingInlines(endPosition);

            // Check for remaining empty BlockUICOntainer or empty Hyperlink elements
            //if (startPosition.Parent is BlockUIContainer && ((BlockUIContainer)startPosition.Parent).IsEmpty)
            //{
            //    ((BlockUIContainer)startPosition.Parent).Reposition(null, null);
            //}
            /*else*/ if (startPosition.Parent is Hyperlink && ((Hyperlink)startPosition.Parent).IsEmpty)
            {
                ((Hyperlink)startPosition.Parent).Reposition(null, null);

                // After deleting an empty hyperlink, we might have inlines to merge.
                MergeFormattingInlines(startPosition);
            }
            // Anything required for custom types here?
        }

        // Helper for DeleteParagraphContent 
        // Takes 2 positions possibly parented by paragraph or BlockUIContainer
        // and deletes them if they are empty .
        //private static void MergeEmptyParagraphsAndBlockUIContainers(TextPointer startPosition, TextPointer endPosition)
        //{
        //    Block first = startPosition.ParagraphOrBlockUIContainer;
        //    Block second = endPosition.ParagraphOrBlockUIContainer;

        //    if (first is BlockUIContainer)
        //    {
        //        if (first.IsEmpty)
        //        {
        //            first.Reposition(null, null);
        //            return;
        //        }
        //        else if (second is Paragraph && Paragraph.HasNoTextContent((Paragraph) second))
        //        {
        //            second.RepositionWithContent(null);
        //            return;
        //        }
        //    }

        //    if (second is BlockUIContainer)
        //    {
        //        if (second.IsEmpty)
        //        {
        //            second.Reposition(null, null);
        //            return;
        //        }
        //        else if (second is Paragraph && Paragraph.HasNoTextContent((Paragraph) first))
        //        {
        //            first.RepositionWithContent(null);
        //            return;
        //        }
        //    }
        //}

        /// <summary>
        /// Deletes all equi-scoped segments of content from start TextPointer
        /// up to fragment root. Thus clears one half of a fragment.
        /// The other half remains untouched.
        /// All elements whose boundaries are crossed by this range
        /// remain in the tree (except for emptied formatting elements).
        /// </summary>
        /// <param name="start">
        /// A position from which content clearinng starts.
        /// All content segments between this position and a fragment
        /// root will be deleted.
        /// </param>
        /// <param name="end">
        /// A position indicating the other boundary of a fragment.
        /// This position is used for fragment root identification.
        /// </param>
        private static void DeleteEquiScopedContent(TextPointer start, TextPointer end)
        {
            // Validate parameters
            Invariant.Assert(start != null, "null check: start");
            Invariant.Assert(end != null, "null check: end");

            if (start.CompareTo(end) == 0)
            {
                return;
            }

            if (start.Parent == end.Parent)
            {
                DeleteContentBetweenPositions(start, end);
                return;
            }

            // Identify directional parameters
            LogicalDirection direction;
            LogicalDirection oppositeDirection;
            TextPointerContext enterScopeSymbol;
            TextPointerContext leaveScopeSymbol;
            ElementEdge edgeBeforeElement;
            ElementEdge edgeAfterElement;
            if (start.CompareTo(end) < 0)
            {
                direction = LogicalDirection.Forward;
                oppositeDirection = LogicalDirection.Backward;
                enterScopeSymbol = TextPointerContext.ElementStart;
                leaveScopeSymbol = TextPointerContext.ElementEnd;
                edgeBeforeElement = ElementEdge.BeforeStart;
                edgeAfterElement = ElementEdge.AfterEnd;
            }
            else
            {
                direction = LogicalDirection.Backward;
                oppositeDirection = LogicalDirection.Forward;
                enterScopeSymbol = TextPointerContext.ElementEnd;
                leaveScopeSymbol = TextPointerContext.ElementStart;
                edgeBeforeElement = ElementEdge.AfterEnd;
                edgeAfterElement = ElementEdge.BeforeStart;
            }

            // previousPosition will store a location where nondeleted content starts
            TextPointer previousPosition = new TextPointer(start);
            // nextPosition runs toward other end until level change -
            // so that we could delete all content from previousPosition
            // to nextPosition at once.
            TextPointer nextPosition = new TextPointer(start);

            // Run nextPosition forward until the very end of affected range
            while (nextPosition.CompareTo(end) != 0)
            {
                Invariant.Assert(direction == LogicalDirection.Forward && nextPosition.CompareTo(end) < 0 || direction == LogicalDirection.Backward && nextPosition.CompareTo(end) > 0,
                    "Inappropriate position ordering");
                Invariant.Assert(previousPosition.Parent == nextPosition.Parent, "inconsistent position Parents: previous and next");

                TextPointerContext pointerContext = nextPosition.GetPointerContext(direction);

                if (pointerContext == TextPointerContext.Text || pointerContext == TextPointerContext.EmbeddedElement)
                {
                    // Add this run to a collection of equi-scoped content
                    nextPosition.MoveToNextContextPosition(direction);

                    // Check if we went too far and return a little to end if necessary
                    if (direction == LogicalDirection.Forward && nextPosition.CompareTo(end) > 0 || direction == LogicalDirection.Backward && nextPosition.CompareTo(end) < 0)
                    {
                        Invariant.Assert(nextPosition.Parent == end.Parent, "inconsistent poaition Parents: next and end");
                        nextPosition.MoveToPosition(end);
                        break;
                    }
                }
                else if (pointerContext == enterScopeSymbol)
                {
                    // Jump over the element and continue collecting equi-scoped content
                    nextPosition.MoveToNextContextPosition(direction);
                    ((ITextPointer)nextPosition).MoveToElementEdge(edgeAfterElement);

                    // If our range crosses the element then we stop before its opening tag
                    if (direction == LogicalDirection.Forward && nextPosition.CompareTo(end) >= 0 || direction == LogicalDirection.Backward && nextPosition.CompareTo(end) <= 0)
                    {
                        nextPosition.MoveToNextContextPosition(oppositeDirection);
                        ((ITextPointer)nextPosition).MoveToElementEdge(edgeBeforeElement);
                        break;
                    }
                }
                else if (pointerContext == leaveScopeSymbol)
                {
                    // Delete preceding content and continue on outer level
                    DeleteContentBetweenPositions(previousPosition, nextPosition);
                    if (!ExtractEmptyFormattingElements(previousPosition))
                    {
                        // Continue on outer level
                        Invariant.Assert(nextPosition.GetPointerContext(direction) == leaveScopeSymbol, "Unexpected context of nextPosition");
                        nextPosition.MoveToNextContextPosition(direction);
                    }

                    previousPosition.MoveToPosition(nextPosition);
                }
                else
                {
                    Invariant.Assert(false, "Not expecting None context here");
                    Invariant.Assert(pointerContext == TextPointerContext.None, "Unknown pointer context");
                    break;
                }
            }
            Invariant.Assert(previousPosition.Parent == nextPosition.Parent, "inconsistent Parents: previousPosition, nextPosition");

            DeleteContentBetweenPositions(previousPosition, nextPosition);
        }

        /// <summary>
        /// Helper for TextContainer.DeleteContent allowing arbitrary
        /// order of positions and doinng nothing in case of empty range.
        /// Removes remaining empty formatting elements - if they not inside empty blocks.
        /// </summary>
        /// <param name="one">
        /// One of content boundary positions. May precede or follow the TextPointer two.
        /// Must belong to the same scope as TextPointer two.
        /// </param>
        /// <param name="two">
        /// Another content boundary position. May precede or follow the TextPointer one.
        /// Must belong to the same scope as TextPointer one.
        /// </param>
        /// <returns>
        /// true if surrounding formatting elements have beed deleted as a side effect.
        /// </returns>
        private static bool DeleteContentBetweenPositions(TextPointer one, TextPointer two)
        {
            Invariant.Assert(one.Parent == two.Parent, "inconsistent Parents: one and two");
            if (one.CompareTo(two) < 0)
            {
                one.TextContainer.DeleteContentInternal(one, two);
            }
            else if (one.CompareTo(two) > 0)
            {
                two.TextContainer.DeleteContentInternal(two, one);
            }
            Invariant.Assert(one.CompareTo(two) == 0, "Positions one and two must be equal now");

            return false;
        }

        #endregion Paragraph Editing

        #endregion Internal Methods

        // --------------------------------------------------------------------
        //
        // Private Methods
        //
        // --------------------------------------------------------------------

        #region Private Methods

        private static TextPointer SplitFormattingElements(TextPointer splitPosition, bool keepEmptyFormatting, TextElement limitingAncestor)
        {
            return SplitFormattingElements(splitPosition, keepEmptyFormatting, /*preserveStructuralFormatting*/false, limitingAncestor);
        }

        /// <summary>
        /// Splits all inline element walking up to specified limitingAncestor.
        /// limitingAncestor remains unsplit.
        /// </summary>
        /// <param name="splitPosition">
        /// Position at which splitting happens. After the operation the position
        /// is between split elements - scoped by limitingElement (if it is not frozen).
        /// </param>
        /// <param name="keepEmptyFormatting">
        /// Flag to indicate whether split operation should create empty formatting tags.
        /// </param>
        /// <param name="preserveStructuralFormatting">
        /// If true, ensures that structural properties are preserved on elements.  Runs will be split
        /// after creating a wrapping Span preserving the original structural property value, otherwise
        /// splitting will halt when a non-Run element has a local structural property (as if a limiting
        /// ancestor or non-mergeable inline had been encountered).
        /// </param>
        /// <param name="limitingAncestor">
        /// If null, this has no impact on split operation.
        /// Otherwise, this method ensures that this ancestor boundary is not crossed while splitting.
        /// </param>
        /// <returns>
        /// TextPointer positioned in between two elements.
        /// It may be the same instance as splitPosition parameter
        /// (in case if it was not frozen), or some new instance of TextPointer.
        /// </returns>
        private static TextPointer SplitFormattingElements(TextPointer splitPosition, bool keepEmptyFormatting, bool preserveStructuralFormatting, TextElement limitingAncestor)
        {
            if (preserveStructuralFormatting)
            {
                Run run = splitPosition.Parent as Run;
                if (run != null && run != limitingAncestor && 
                    ((run.Parent != null && HasLocalInheritableStructuralPropertyValue(run)) || 
                    (run.Parent == null && HasLocalStructuralPropertyValue(run))))
                {
                    // This Run has a structural property set on it (eg, FlowDirection) which cannot simply be split
                    // (two adjacent Runs with the same FlowDirection will render differently than a single Run with
                    // the same value, when the parent FlowDirection property differs).
                    // So create a wrapping Span which will survive in the loop below.
                    Span span = new Span(run.ElementStart, run.ElementEnd);
                    TransferStructuralProperties(run, span);
                }
            }

            // Splitting loop: cutting a parent element until we reach the non-inline,
            // never crossing ancestor boundary.
            while (splitPosition.Parent != null && TextSchema.IsMergeableInline(splitPosition.Parent.GetType()) && splitPosition.Parent != limitingAncestor && 
                (!preserveStructuralFormatting || 
                   ((((Inline)splitPosition.Parent).Parent != null && !HasLocalInheritableStructuralPropertyValue((Inline)splitPosition.Parent)) ||
                   (((Inline)splitPosition.Parent).Parent == null && !HasLocalStructuralPropertyValue((Inline)splitPosition.Parent)))))
            {
                splitPosition = SplitFormattingElement(splitPosition, keepEmptyFormatting);
            }

            return splitPosition;
        }

        // Copies all structural properties from source (clearing the property) to destination.
        private static void TransferStructuralProperties(Inline source, Inline destination)
        {
            bool sourceIsChild = (source.Parent == destination);

            for (int i = 0; i < TextSchema.StructuralCharacterProperties.Length; i++)
            {
                AvaloniaProperty property = TextSchema.StructuralCharacterProperties[i];
                if ((sourceIsChild && HasLocalInheritableStructuralPropertyValue(source)) ||
                    (!sourceIsChild && HasLocalStructuralPropertyValue(source)))
                {
                    object value = source.GetValue(property);
                    source.ClearValue(property);
                    destination.SetValue(property, value);
                }
            }
        }

        // Returns true if an Inline has one or more non-readonly local property values.
        private static bool HasWriteableLocalPropertyValues(Inline inline)
        {
            LocalValueEnumerator enumerator = inline.GetLocalValueEnumerator();
            bool hasLocalValues = false;

            while (!hasLocalValues && enumerator.MoveNext())
            {
                hasLocalValues = !enumerator.Current.Property.IsReadOnly;
            }

            return hasLocalValues;
        }

        // Returns true if an inline has one or more structural local property values.
        private static bool HasLocalInheritableStructuralPropertyValue(Inline inline)
        {
            int i;

            for (i = 0; i < TextSchema.StructuralCharacterProperties.Length; i++)
            {
                AvaloniaProperty inheritableProperty = TextSchema.StructuralCharacterProperties[i];
                if (!TextSchema.ValuesAreEqual(inline.GetValue(inheritableProperty), inline.Parent.GetValue(inheritableProperty)))
                    break;
            }

            return (i < TextSchema.StructuralCharacterProperties.Length);
        }

        // Returns true if an inline has one or more structural local property values.
        private static bool HasLocalStructuralPropertyValue(Inline inline)
        {
            int i;

            for (i = 0; i < TextSchema.StructuralCharacterProperties.Length; i++)
            {
                AvaloniaProperty inheritableProperty = TextSchema.StructuralCharacterProperties[i];
                if (HasLocalPropertyValue(inline, inheritableProperty))
                    break;
            }

            return (i < TextSchema.StructuralCharacterProperties.Length);
        }

        // Returns true if an inline has a local property value with higher precedence than inheritance.
        private static bool HasLocalPropertyValue(Inline inline, AvaloniaProperty property)
        {
            //bool hasModifiers;
            //BaseValueSourceInternal source = inline.GetValueSource(property, null, out hasModifiers);

            //return (source != BaseValueSourceInternal.Unknown &&
            //        source != BaseValueSourceInternal.Default &&
            //        source != BaseValueSourceInternal.Inherited);

            return inline.IsSet(property);
        }

        // Helper for MergeFlowDirection.  Returns a greatest scoping Inline of a Run
        // with matching FlowDirection.  The caller guarantees that a scoping Span
        // has differing FlowDirection.
        private static Inline GetScopingFlowDirectionInline(Run run)
        {
            FlowDirection flowDirection = run.FlowDirection;

            Inline inline = run;

            while ((FlowDirection)inline.Parent.GetValue(Inline.FlowDirectionProperty) == flowDirection)
            {
                inline = (Span)inline.Parent;
            }

            return inline;
        }


        // Helper to set non-structural Inline property to a range between start and end positions.
        private static void SetNonStructuralInlineProperty(TextPointer start, TextPointer end, AvaloniaProperty formattingProperty, object value, PropertyValueAction propertyValueAction)
        {
            // Split formatting elements at range boundaries
            start = SplitFormattingElements(start, /*keepEmptyFormatting:*/false, /*preserveStructuralFormatting*/true, /*limitingAncestor*/null);
            end = SplitFormattingElements(end, /*keepEmptyFormatting:*/false, /*preserveStructuralFormatting*/true, /*limitingAncestor*/null);

            Run run = TextRangeEdit.GetNextRun(start, end);

            while (run != null)
            {
                object currentValue = run.GetValue(formattingProperty);
                object newValue = value;

                if (propertyValueAction != PropertyValueAction.SetValue)
                {
                    Invariant.Assert(formattingProperty == TextElement.FontSizeProperty, "Only FontSize can be incremented/decremented among character properties");
                    newValue = GetNewFontSizeValue((double)currentValue, (double)value, propertyValueAction);
                }

                // Set new property value
                SetPropertyValue(run, formattingProperty, currentValue, newValue);

                // Remember a position after the current run for the following processing.
                // Normalize forward since Run.ElementEnd has backward gravity.
                TextPointer nextRunPosition = run.ElementEnd.GetPositionAtOffset(0, LogicalDirection.Forward);

                if (TextPointerBase.IsAtPotentialRunPosition(run))
                {
                    // If current run was an implicit run, we move to the next context position after its element end.
                    // This is safe because by definition of IsAtPotentialRunPosition predicate, 
                    // our current run can never have an adjacent run element or 
                    // another adjacent potential run position.
                    nextRunPosition = nextRunPosition.GetNextContextPosition(LogicalDirection.Forward);
                }

                // Merge this run with the previous one.
                // Note that this can affect text structure even after this run.
                MergeFormattingInlines(run.ContentStart);

                // Find the next Run to process
                run = TextRangeEdit.GetNextRun(nextRunPosition, end);
            }

            MergeFormattingInlines(end);
        }

        // Helper to calculate new value of Run.FontSize property when PropertyValueAction is increment/decrement.
        private static double GetNewFontSizeValue(double currentValue, double value, PropertyValueAction propertyValueAction)
        {
            double newValue = value;

            // Calculate the new value as increment/decrement from the current value
            if (propertyValueAction == PropertyValueAction.IncreaseByAbsoluteValue)
            {
                newValue = currentValue + value;
            }
            else if (propertyValueAction == PropertyValueAction.DecreaseByAbsoluteValue)
            {
                newValue = currentValue - value;
            }

            // Check limiting boundaries
            //if (newValue < TextEditorCharacters.OneFontPoint)
            //{
            //    newValue = TextEditorCharacters.OneFontPoint;
            //}
            //else if (newValue > TextEditorCharacters.MaxFontPoint)
            //{
            //    newValue = TextEditorCharacters.MaxFontPoint;
            //}

            return newValue;
        }

        // Helper to set a structural Inline property to a range between start and end positions.
        private static void SetStructuralInlineProperty(TextPointer start, TextPointer end, AvaloniaProperty formattingProperty, object value)
        {
            IAvaloniaObject commonAncestor = TextPointer.GetCommonAncestor(start, end);

            ValidateApplyStructuralInlineProperty(start, end, commonAncestor, formattingProperty);

            if (commonAncestor is Run)
            {
                ApplyStructuralInlinePropertyAcrossRun(start, end, (Run)commonAncestor, formattingProperty, value);
            }
            else if ((commonAncestor is Inline /*&& !(commonAncestor is AnchoredBlock)*/) /*||*/
                     /*commonAncestor is Paragraph*/)
            {
                // Even though we don't test for it explicitly, we
                // should never see InlineUIContainers here because start/end
                // are always normalized and the inner edges of InlineUIContainer
                // are not insertion positions.
                Invariant.Assert(!(commonAncestor is InlineUIContainer));

                ApplyStructuralInlinePropertyAcrossInline(start, end, (TextElement)commonAncestor, formattingProperty, value);
            }
            //else
            //{
            //    ApplyStructuralInlinePropertyAcrossParagraphs(start, end, formattingProperty, value);
            //}
        }

        private static void FixupStructuralPropertyEnvironment(Inline inline, AvaloniaProperty property)
        {
            // Clear property on parent Spans.
            ClearParentStructuralPropertyValue(inline, property);

            // Flatten property on previous Inlines.
            for (Inline searchInline = inline; searchInline != null; searchInline = searchInline.Parent as Span)
            {
                Inline previousSibling = (Inline)searchInline.PreviousElement;

                if (previousSibling != null)
                {
                    FlattenStructuralProperties(previousSibling);
                    break;
                }
            }

            // Flatten property on following Inlines.
            for (Inline searchInline = inline; searchInline != null; searchInline = searchInline.Parent as Span)
            {
                Inline nextSibling = (Inline)searchInline.NextElement;

                if (nextSibling != null)
                {
                    FlattenStructuralProperties(nextSibling);
                    break;
                }
            }
        }

        private static void FlattenStructuralProperties(Inline inline)
        {
            // Find the topmost Span covering this inline and only other direct ancestors.
            Span topmostSpan = inline as Span;
            Span parent = inline.Parent as Span;

            while (parent != null &&
                   parent.Inlines.FirstInline == parent.Inlines.LastInline)
            {
                topmostSpan = parent;
                parent = parent.Parent as Span;
            }

            // Push structural properties downward.
            while (topmostSpan != null && topmostSpan.Inlines.FirstInline == topmostSpan.Inlines.LastInline)
            {
                Inline child = (Inline)topmostSpan.Inlines.FirstInline;

                TransferStructuralProperties(topmostSpan, child);

                // If there are no more local values on the parent, remove it.
                if (TextSchema.IsMergeableInline(topmostSpan.GetType()) && TextSchema.IsKnownType(topmostSpan.GetType()) && !HasWriteableLocalPropertyValues(topmostSpan))
                {
                    topmostSpan.Reposition(null, null);
                }

                topmostSpan = child as Span;
            }
        }

        private static void ClearParentStructuralPropertyValue(Inline child, AvaloniaProperty property)
        {
            // Find the most distant ancestor with a local property value.
            Span conflictingParent = null;

            for (Span parent = child.Parent as Span;
                 parent != null && TextSchema.IsMergeableInline(parent.GetType());
                 parent = parent.Parent as Span)
            {
                if (HasLocalPropertyValue(parent, property))
                {
                    conflictingParent = parent;
                }
            }

            // Split down from conflictingParent, clearing property values along the way.
            if (conflictingParent != null)
            {
                TextElement limit = (TextElement)conflictingParent.Parent;
                SplitFormattingElements(child.ElementStart, /*keepEmptyFormatting*/false, limit);
                TextPointer end = SplitFormattingElements(child.ElementEnd, /*keepEmptyFormatting*/false, limit);

                Span parent = (Span)end.GetAdjacentElement(LogicalDirection.Backward);

                while (parent != null && parent != child)
                {
                    parent.ClearValue(property);

                    Span nextSpan = parent.Inlines.FirstInline as Span;

                    // If there are no more local values on the parent, remove it.
                    if (!HasWriteableLocalPropertyValues(parent))
                    {
                        // we could try to merge character properties here as well, when parent
                        // Spans are removed.  The split calls above may have fragmented other Spans
                        // unnecessarily now that we're removing a scoping Span.
                        parent.Reposition(null, null);
                    }

                    parent = nextSpan;
                }
            }
        }

        // Finds a Run element with ElementStart at or after the given pointer
        // Creates Runs at potential run positions if encounters some.
        private static Run GetNextRun(TextPointer pointer, TextPointer limit)
        {
            Run run = null;

            while (pointer != null && pointer.CompareTo(limit) < 0)
            {
                if (pointer.GetPointerContext(LogicalDirection.Forward) == TextPointerContext.ElementStart &&
                    (run = pointer.GetAdjacentElement(LogicalDirection.Forward) as Run) != null)
                {
                    break;
                }

                //if (TextPointerBase.IsAtPotentialRunPosition(pointer))
                //{
                //    pointer = TextRangeEditTables.EnsureInsertionPosition(pointer);
                //    Invariant.Assert(pointer.Parent is Run);
                //    run = pointer.Parent as Run;
                //    break;
                //}

                // Advance the scanning pointer
                pointer = pointer.GetNextContextPosition(LogicalDirection.Forward);
            }

            return run;
        }

        // Helper that walks Run and Span elements between start and end positions,
        // clearing value of passed formattingProperty on them.
        // REVIEW:benwest:5/4/2006: shouldn't this just clear top-level elements?
        private static void ClearPropertyValueFromSpansAndRuns(TextPointer start, TextPointer end, AvaloniaProperty formattingProperty)
        {
            // Normalize start position forward.
            start = start.GetPositionAtOffset(0, LogicalDirection.Forward);

            // Move to next context position before entering loop below, 
            // since in the loop we look backward.
            start = start.GetNextContextPosition(LogicalDirection.Forward);

            while (start != null && start.CompareTo(end) < 0)
            {
                if (start.GetPointerContext(LogicalDirection.Backward) == TextPointerContext.ElementStart &&
                    TextSchema.IsFormattingType(start.Parent.GetType())) // look for Run/Span elements
                {
                    start.Parent.ClearValue(formattingProperty);

                    // Remove unnecessary Spans around this position, delete empty formatting elements (if any)
                    // and merge with adjacent inlines if they have identical set of formatting properties.
                    MergeFormattingInlines(start);
                }

                start = start.GetNextContextPosition(LogicalDirection.Forward);
            }
        }

        private static void ApplyStructuralInlinePropertyAcrossRun(TextPointer start, TextPointer end, Run run, AvaloniaProperty formattingProperty, object value)
        {
            if (start.CompareTo(end) == 0)
            {
                // When the range is empty we should ignore the command, except
                // for the case of empty Run which can be encountered in empty paragraphs
                if (run.IsEmpty)
                {
                    run.SetValue(formattingProperty, value);
                }
            }
            else
            {
                // Split elements at start and end boundaries.
                start = SplitFormattingElements(start, /*keepEmptyFormatting:*/false, /*limitingAncestor*/run.Parent as TextElement);
                end = SplitFormattingElements(end, /*keepEmptyFormatting:*/false, /*limitingAncestor*/run.Parent as TextElement);

                run = (Run)start.GetAdjacentElement(LogicalDirection.Forward);
                run.SetValue(formattingProperty, value);
            }

            // Clear property value from all ancestors of this Run.
            FixupStructuralPropertyEnvironment(run, formattingProperty);
        }

        private static void ApplyStructuralInlinePropertyAcrossInline(TextPointer start, TextPointer end, TextElement commonAncestor, AvaloniaProperty formattingProperty, object value)
        {
            start = SplitFormattingElements(start, /*keepEmptyFormatting:*/false, commonAncestor);
            end = SplitFormattingElements(end, /*keepEmptyFormatting:*/false, commonAncestor);

            IAvaloniaObject forwardElement = start.GetAdjacentElement(LogicalDirection.Forward);
            IAvaloniaObject backwardElement = end.GetAdjacentElement(LogicalDirection.Backward);
            if (forwardElement == backwardElement &&
                (forwardElement is Run || forwardElement is Span))
            {
                // After splitting we have exactly one Run or Span between start and end. Use it for setting the property.
                Inline inline = (Inline)start.GetAdjacentElement(LogicalDirection.Forward);

                // Set the property to existing element.
                inline.SetValue(formattingProperty, value);

                // Clear property value from all ancestors of this inline.
                FixupStructuralPropertyEnvironment(inline, formattingProperty);

                if (forwardElement is Span)
                {
                    // Clear property value from all Span and Run children of this span.
                    ClearPropertyValueFromSpansAndRuns(inline.ContentStart, inline.ContentEnd, formattingProperty);
                }
            }
            else
            {
                Span span;

                if (commonAncestor is Span &&
                    start.GetPointerContext(LogicalDirection.Backward) == TextPointerContext.ElementStart &&
                    end.GetPointerContext(LogicalDirection.Forward) == TextPointerContext.ElementEnd &&
                    start.GetAdjacentElement(LogicalDirection.Backward) == commonAncestor)
                {
                    // Special case when start and end are at parent Span boundaries.
                    // Don't need to create a new Span in this case.
                    span = (Span)commonAncestor;
                }
                else
                {
                    // Create a new span from start to end.
                    span = new Span();
                    span.Reposition(start, end);
                }

                // Set property on the span.
                span.SetValue(formattingProperty, value);

                // Clear property value from all ancestors of this span.
                FixupStructuralPropertyEnvironment(span, formattingProperty);

                // Clear property value from all Span and Run children of this span.
                ClearPropertyValueFromSpansAndRuns(span.ContentStart, span.ContentEnd, formattingProperty);
            }
        }

        // Helper that walks paragraphs between start and end positions, applying passed formattingProperty value on them.
        //private static void ApplyStructuralInlinePropertyAcrossParagraphs(TextPointer start, TextPointer end, AvaloniaProperty formattingProperty, object value)
        //{
        //    // We assume to call this method only for paragraph crossing case
        //    Invariant.Assert(start.Paragraph != null);
        //    Invariant.Assert(start.Paragraph.ContentEnd.CompareTo(end) < 0);

        //    // Apply to first Paragraph
        //    SetStructuralInlineProperty(start, start.Paragraph.ContentEnd, formattingProperty, value);
        //    start = start.Paragraph.ElementEnd;

        //    // Apply to last paragraph
        //    if (end.Paragraph != null)
        //    {
        //        SetStructuralInlineProperty(end.Paragraph.ContentStart, end, formattingProperty, value);
        //        end = end.Paragraph.ElementStart;
        //    }

        //    // Now, loop through paragraphs between start and end positions
        //    while (start != null && start.CompareTo(end) < 0)
        //    {
        //        if (start.GetPointerContext(LogicalDirection.Backward) == TextPointerContext.ElementStart &&
        //            start.Parent is Paragraph)
        //        {
        //            Paragraph paragraph = (Paragraph)start.Parent;

        //            // Apply property to paragraph just found.
        //            SetStructuralInlineProperty(paragraph.ContentStart, paragraph.ContentEnd, formattingProperty, value);

        //            // Jump to Paragraph end to skip Inline formatting tags.
        //            start = paragraph.ElementEnd;
        //        }

        //        start = start.GetNextContextPosition(LogicalDirection.Forward);
        //    }
        //}

        // Returns false if calling ApplyStructuralInlineProperty will throw an InvalidOperationException with the
        // same input parameters.
        //
        // If property != null, this method will throw an InvalidOperation exception instead of returning false.
        private static bool ValidateApplyStructuralInlineProperty(TextPointer start, TextPointer end, IAvaloniaObject commonAncestor, AvaloniaProperty property)
        {
            if (!(commonAncestor is Inline))
            {
                return true;
            }

            Inline nonMergeableAncestor = null;
            Inline parent;

            // Find the first non-mergeable Inline scoping start.
            for (parent = (Inline)start.Parent; parent != commonAncestor; parent = (Inline)parent.Parent)
            {
                if (!TextSchema.IsMergeableInline(parent.GetType()))
                {
                    nonMergeableAncestor = parent;
                    commonAncestor = parent;
                    break;
                }
            }

            // Try to reach the start non-mergeable or original commonAncestor from end.
            for (parent = (Inline)end.Parent; parent != commonAncestor; parent = (Inline)parent.Parent)
            {
                if (!TextSchema.IsMergeableInline(parent.GetType()))
                {
                    nonMergeableAncestor = parent;
                    break;
                }
            }

            if (property != null && parent != commonAncestor)
            {
                throw new InvalidOperationException(/*SR.Get(SRID.TextRangeEdit_InvalidStructuralPropertyApply, property, nonMergeableAncestor)*/);
            }

            return (parent == commonAncestor);
        }

        #endregion Private Methods

        #region Private Types
        /// <summary>
        /// This class imposes value ranges, considered valid by editing code, for Dependency properties of type double.
        /// In other words this class defines value range policies for DPs of type double, in editing context.
        /// </summary>
        internal static class DoublePropertyBounds
        {
            /// <summary>
            /// Validates the value and if it's in permitable range then the <paramref name="value"/> is returned.
            /// Oterwise closest bound(lower/upper) of the range is returned.
            /// </summary>
            /// <param name="property"></param>
            /// <param name="value"></param>
            /// <returns></returns>
            internal static double GetClosestValidValue(AvaloniaProperty property, double value)
            {
                DoublePropertyRange valueRange = GetValueRange(property);
                return valueRange.GetClosestValue(value);
            }

            /// <summary>
            /// Returns the acceptable range of values for given property.
            /// if <paramref name="property"/> is null, or there is no value range specified for given property,
            /// then <see cref="DefaultRange "/> is returned.
            /// </summary>
            /// <param name="property"></param>
            /// <returns></returns>
            private static DoublePropertyRange GetValueRange(AvaloniaProperty property)
            {
                for (int i = 0; i < _ranges.Length; i++)
                {
                    if (property == _ranges[i].Property)
                    {
                        return _ranges[i];
                    }
                }
                return DefaultRange;
            }
            
            /// <summary>
            /// Range for properties whcih do not have explicit specification of the acceptable value ranges.
            /// </summary>
            private static DoublePropertyRange DefaultRange
            {
                get { return _ranges[0]; }
            }

            static readonly DoublePropertyRange[] _ranges = new DoublePropertyRange[]
            { 
                // 1st entry is the default value range for properties not having explicit ranges specified here.
                new DoublePropertyRange(null, 0, double.MaxValue)/*,*/
                //new DoublePropertyRange (Paragraph.TextIndentProperty, -Math.Min(1000000, PTS.MaxPageSize), Math.Min(1000000, PTS.MaxPageSize))
            };

            /// <summary>
            /// Range of <see cref="double"/> values for a given <see cref="AvaloniaProperty"/>.
            /// </summary>
            private struct DoublePropertyRange
            {
                internal DoublePropertyRange(AvaloniaProperty property, double lowerBound, double upperBound)
                {
                    Invariant.Assert(lowerBound < upperBound);
                    _lowerBound = lowerBound;
                    _upperBound = upperBound;
                    _property = property;
                }
                /// <summary>
                /// Returns <paramref name="value"/> if it is in range, or returns the closest boundary.
                /// </summary>
                /// <param name="value"></param>
                /// <returns></returns>
                internal double GetClosestValue(double value)
                {
                    double retValue = Math.Max(_lowerBound, value);
                    retValue = Math.Min(retValue, _upperBound);
                    return retValue;
                }

                internal AvaloniaProperty Property { get { return _property; } } 

                private AvaloniaProperty _property;
                private double _lowerBound;
                private double _upperBound;
            }
        }
        #endregion Private Types

    }
}
