// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// 
// Description: Span class - inline element grouping several other inline elements
//

using MS.Internal;                  // Invariant.Assert
//using System.Windows.Controls;      // TextBlock
//using System.Windows.Markup; // ContentProperty
using System.ComponentModel;
using Avalonia.Media.TextFormatting;
using Avalonia.Metadata; // DesignerSerializationVisibility

namespace System.Windows.Documents 
{
    /// <summary>
    /// Span element used for grouping other Inline elements.
    /// </summary>
    public class Span : Inline
    {
        //-------------------------------------------------------------------
        //
        // Constructors
        //
        //-------------------------------------------------------------------

        #region Constructors

        /// <summary>
        /// Initializes a new instance of a Span element.
        /// </summary>
        public Span()
        {
        }

        /// <summary>
        /// Initializes a new instance of a Span element.
        /// </summary>
        /// <param name="childInline">
        /// An Inline element added to this Span as its first child.
        /// </param>
        public Span(Inline childInline) : this(childInline, null)
        {
        }

        /// <summary>
        /// Creates a new Span instance.
        /// </summary>
        /// <param name="childInline">
        /// Optional child Inline for the new Span.  May be null.
        /// </param>
        /// <param name="insertionPosition">
        /// Optional position at which to insert the new Span.  May be null.
        /// </param>
        public Span(Inline childInline, TextPointer insertionPosition)
        {
            if (insertionPosition != null)
            {
                insertionPosition.TextContainer.BeginChange();
            }
            try
            {
                if (insertionPosition != null)
                {
                    // This will throw InvalidOperationException if schema validity is violated.
                    insertionPosition.InsertInline(this);
                }

                if (childInline != null)
                {
                    this.Inlines.Add(childInline);
                }
            }
            finally
            {
                if (insertionPosition != null)
                {
                    insertionPosition.TextContainer.EndChange();
                }
            }
        }

        /// <summary>
        /// Creates a new Span instance covering existing content.
        /// </summary>
        /// <param name="start">
        /// Start position of the new Span.
        /// </param>
        /// <param name="end">
        /// End position of the new Span.
        /// </param>
        /// <remarks>
        /// start and end must both be parented by the same Paragraph, otherwise
        /// the method will raise an ArgumentException.
        /// </remarks>
        public Span(TextPointer start, TextPointer end)
        {
            if (start == null)
            {
                throw new ArgumentNullException("start");
            }
            if (end == null)
            {
                throw new ArgumentNullException("start");
            }
            if (start.TextContainer != end.TextContainer)
            {
                throw new ArgumentException(/*SR.Get(SRID.InDifferentTextContainers, "start", "end")*/);
            }
            if (start.CompareTo(end) > 0)
            {
                throw new ArgumentException(/*SR.Get(SRID.BadTextPositionOrder, "start", "end")*/);
            }

            start.TextContainer.BeginChange();
            try
            {
                //start = TextRangeEditTables.EnsureInsertionPosition(start);
                Invariant.Assert(start.Parent is Run);
                //end = TextRangeEditTables.EnsureInsertionPosition(end);
                Invariant.Assert(end.Parent is Run);

                //if (start.Paragraph != end.Paragraph)
                //{
                //    throw new ArgumentException(/*SR.Get(SRID.InDifferentParagraphs, "start", "end")*/);
                //}

                // If start or end positions have a Hyperlink ancestor, we cannot split them.
                Inline nonMergeableAncestor;
                if ((nonMergeableAncestor = start.GetNonMergeableInlineAncestor()) != null)
                {
                    throw new InvalidOperationException(/*SR.Get(SRID.TextSchema_CannotSplitElement, nonMergeableAncestor.GetType().Name)*/);
                }
                if ((nonMergeableAncestor = end.GetNonMergeableInlineAncestor()) != null)
                {
                    throw new InvalidOperationException(/*SR.Get(SRID.TextSchema_CannotSplitElement, nonMergeableAncestor.GetType().Name)*/);
                }

                TextElement commonAncestor = TextElement.GetCommonAncestor((TextElement)start.Parent, (TextElement)end.Parent);

                while (start.Parent != commonAncestor)
                {
                    start = SplitElement(start);
                }
                while (end.Parent != commonAncestor)
                {
                    end = SplitElement(end);
                }

                if (start.Parent is Run)
                {
                    start = SplitElement(start);
                }
                if (end.Parent is Run)
                {
                    end = SplitElement(end);
                }

                Invariant.Assert(start.Parent == end.Parent);
                Invariant.Assert(TextSchema.IsValidChild(/*position*/start, /*childType*/typeof(Span)));

                this.Reposition(start, end);
            }
            finally
            {
                start.TextContainer.EndChange();
            }
        }

        #endregion Constructors

        //-------------------------------------------------------------------
        //
        // Public Properties
        //
        //-------------------------------------------------------------------

        #region Public Properties

        /// <value>
        /// Collection of Inline items contained in this Section.
        /// </value>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
        [Content]
        public InlineCollection Inlines
        {
            get
            {
                return new InlineCollection(this, /*isOwnerParent*/true);
            }
        }

        #endregion Public Properties

        //-------------------------------------------------------------------
        //
        // Internal Methods
        //
        //-------------------------------------------------------------------

        #region Internal Methods

        /// <summary>
        /// This method is used by TypeDescriptor to determine if this property should
        /// be serialized.
        /// </summary>
        //[EditorBrowsable(EditorBrowsableState.Never)]
        //public bool ShouldSerializeInlines(XamlDesignerSerializationManager manager)
        //{
        //    return manager != null && manager.XmlWriter == null;
        //}

        #endregion Internal Methods

        //-------------------------------------------------------------------
        //
        // Private Methods
        //
        //-------------------------------------------------------------------

        #region Private Methods

        // Splits the parent TextElement of a TextPointer, returning a TextPointer
        // between the two split halves.
        // If the TextPointer is adjacent to one of the TextElement's edges,
        // this method does not split the element, and instead returns a pointer
        // adjacent to the bordered edge, outside the TextElemetn scope.
        private TextPointer SplitElement(TextPointer position)
        {
            if (position.GetPointerContext(LogicalDirection.Backward) == TextPointerContext.ElementStart)
            {
                position = position.GetNextContextPosition(LogicalDirection.Backward);
            }
            else if (position.GetPointerContext(LogicalDirection.Forward) == TextPointerContext.ElementEnd)
            {
                position = position.GetNextContextPosition(LogicalDirection.Forward);
            }
            else
            {
                position = TextRangeEdit.SplitElement(position);
            }

            return position;
        }

        #endregion Private Methods
    }
}
