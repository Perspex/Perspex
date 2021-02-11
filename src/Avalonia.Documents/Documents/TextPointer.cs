// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: TextPointer object representing a location in formatted text.
//

using System;
using System.Collections.Generic;
using MS.Internal;
//using System.Threading;
//using System.Windows;
//using System.Windows.Media;
//using System.Collections;
//using System.Windows.Controls; // doc comments
using Avalonia;
using Avalonia.Controls;
using Avalonia.Documents;
using Avalonia.Media.TextFormatting;

namespace System.Windows.Documents
{
    /// <summary>
    /// Represents a location in a formatted text content.
    /// </summary>
    /// <remarks>
    /// <para>In Avalon formatted text can be contained in elements such as
    /// <see cref="TextBlock"/> or <see cref="FlowDocument"/>.
    /// We will refer to these elements as to "text containers".</para>
    /// <para>Using the properties and the methods of the TextPointer object, you can:</para>
    /// <para>a) Find out what kind of content is in forward or in backward directions from its position;</para>
    /// <para>b) Get a <see cref="TextElement"/> scoping or adjacent a position of this TextPointer;</para>
    /// <para>c) Get characters preceding or following the TextPointer when it is positioned within text run - <see cref="Run"/> element;</para>
    /// <para>d) Insert characters in a position where the TextPointer is located;</para>
    /// <para>e) Inspect line layout structure by finding line boundary positions;</para>
    /// <para>f) Perform visual hit-testing by translating back and forth positions of TextPointer objects into Point objects representing coordinates;</para>
    /// <para>g) Create an instance of a <see cref="TextRange"/> object and use it for formatting, copying, pasting and other editing operations;</para>
    /// <para></para>
    /// <para>Positions in formatted document where TextPointer objects can be located
    /// are places between characters and element tags.</para>
    /// <para>As you edit a document, TextPointer objects do not move relative to their surrounding text.
    /// That is, if text is inserted before a text pointer, then the offset of the pointer
    /// from start position of a text container is incremented to reflect its new location
    /// further down in the document (offsets between text pointers can be calculated by
    /// a <see cref="TextPointer.GetOffsetToPosition"/> method).</para>
    /// <para>If multiple TextPointer objects are located at the same position and a text
    /// is inserted into this position, then the new characters and structural tags are
    /// to the right or to the left of all of the TextPointer objects depending on their
    /// <see cref="TextPointer.LogicalDirection"/> property.</para>
    /// <para>Class <see cref="TextPointerContext"/> is an enum specifying what kind of
    /// content can be found in immediate vicility of a TextPointer. The kinds include
    /// <c>None</c> for text container boundaries, <c>ElementStart</c> and <c>ElementEnd</c>
    /// for opening and closing tags of <see cref="TextElement"/> elements, <c>EmbeddedElement</c>
    /// for UIElements inserted in text as atomic objects. The kind of context can be
    /// get from a TextPointer using method <see cref="TextPointer.GetPointerContext"/>.</para>
    /// <para>TextPointer objects are immutable - they cannot be repositioned in text content
    /// by any means; and their LogicalDirection property cannot be changed. The context
    /// around a TextPointer can be changed though, as a result of text editing.
    /// For instance, when text around a TextPointer is deleted, the TextPointer
    /// will appear in a new context - in a content remaining after deletion.</para>
    /// <para>To traverse a document content you can use a bunch of <c>Get*Position</c>
    /// methods - <see cref="GetNextContextPosition"/>, <see cref="GetNextInsertionPosition"/>, etc.</para>
    /// <para></para>
    /// <para>TextPointer class does not have public constructors.
    /// The only way to get an instance of the TextPointer class is by
    /// using properties or methods of other objects:
    /// <see cref="TextRange.Start"/> and <see cref="TextRange.End"/>, etc.
    /// <see cref="TextElement.ElementStart"/> and <see cref="TextElement.ElementEnd"/>,
    /// <see cref="TextElement.ContentStart"/> and <see cref="TextElement.ContentEnd"/>, etc.
    /// TextPointer objects can be also produced from other TextPointer objects
    /// using traversal methods like <see cref="TextPointer.GetNextContextPosition"/>,
    /// <see cref="TextPointer.GetNextInsertionPosition"/>, <see cref="TextPointer.GetPositionAtOffset(int)"/>,
    /// etc. TextPointer can be also gotten from a visual coordinate via
    /// methods like <see cref="TextBlock.GetPositionFromPoint"/>.</para>
    /// <para></para>
    /// <para>We use a concept of "insertion positions" in association with TextPointer objects,
    /// which is a key for editor behavior and for various api members.</para>
    /// <para>When caret travels over text content it can stop only at particular positions,
    /// skipping all non-appropriate ones. Positions appropriate for caret stopping are called
    /// "insertion positions". Boundary positions of <see cref="TextRange"/> and <see cref="TextSelection"/>
    /// objects are always forcefully set to insertion positions, even if you pass
    /// arbitrary position in TextRange constructor or <see cref="TextRange.Select"/> method.</para>
    /// <para>From TextPointer located at arbitrary (possibly non-insertion) position, you
    /// can get a TextPointer located at a nearest insertion position by calling
    /// <see cref="GetInsertionPosition()"/> method. To get from one insertion position to another
    /// you can use <see cref="GetNextInsertionPosition"/> method.</para>
    /// </remarks>
    /// <example>
    /// <para>Example 0. This code shows how to get an instance of a TextPointer.
    /// As TextPointer does not have any public constructors, the only way
    /// of getting a TextPointer is to use a property or method of other object.
    /// This example ContentStart and ContentEnd properties of main text containers,
    /// create a TextRange for the whole content of each of them and applies
    /// Bold formatting to it.</para>
    /// <code>
    ///     void BoldAll(FlowDocument flowDocument, TextFlow textFlow, TextBlock textBlock, RichTextBox richTextBox)
    ///     {
    ///         allContent = new TextRange(flowDocument.ContentStart, flowDocument.ContentEnd);
    ///         allContent.ApplyPropertyValue(TextElement.FontWeightProperty, FontWeights.Bold);
    ///
    ///         allContent = new TextRange(textFlow.ContentStart, textFlow.ContentEnd);
    ///         allContent.ApplyPropertyValue(TextElement.FontWeightProperty, FontWeights.Bold);
    ///
    ///         allContent = new TextRange(textBlock.ContentStart, textFlow.ContentEnd);
    ///         allContent.ApplyPropertyValue(TextElement.FontWeightProperty, FontWeights.Bold);
    ///
    ///         // Note that RichTextBox does not have ContentStart/ContentEnd properties,
    ///         // we use its Document property to get to FlowDocument contained within.
    ///         TextRange allContent = new TextRange(richTextBox.Document.ContentStart, richTextBox.Document.ContentEnd);
    ///         allContent.ApplyPropertyValue(TextElement.FontWeightProperty, FontWeights.Bold);
    ///     }
    /// </code>
    /// <para>Example 1. This code shows how to use TextPointer for finding a first Run element
    /// from a particular position in forard direction.</para>
    /// <code>
    ///     Run FindNextRun(TextPointer position)
    ///     {
    ///         // Traverse content in forward direction until the position is
    ///         // immediately after opening tag of a Run element.
    ///         while (position != null &amp;&amp;
    ///             !(position.GetPointerContext(LogicalDirection.Backward) == TextPointerContext.ElementStart
    ///               &amp;&amp;
    ///               position.Parent is Run))
    ///         {
    ///             position = position.GetNextContextPosition(LogicalDirection.Forward);
    ///         }
    ///
    ///         // Return a result
    ///         return position == null ? null : position.Parent as Run;
    ///     }
    /// </code>
    /// <para>Example 2. This code shows how to use TextPointer for finding a particular
    /// word in text content. This is a simplistic "find" algorithm, not smart enough
    /// for international issues and for words crossing formatting boundaries.</para>
    /// <code>
    ///     TextPointer FindWord(TextPointer position, string word)
    ///     {
    ///         while (position != null)
    ///         {
    ///             if (position.GetPointerContext(LogicalDirection.Forward) == TextPointerContext.Text)
    ///             {
    ///                 string textRun = position.GetTextInRun(LogicalDirection.Forward);
    ///                 int indexInRun = textRun.IndexOf(word);
    ///                 if (indexInRun &gt;= 0)
    ///                 {
    ///                     position = position.GetPositionAtOffset(indexInRun);
    ///                     break;
    ///                 }
    ///             }
    ///             else
    ///             {
    ///                 position = position.GetNextContextPosition(LogicalDirection.Forward);
    ///             }
    ///         }
    ///
    ///         return position; // will be null, if a word is not found.
    ///     }
    /// </code>
    /// <para>Example 3. This code shows how to enumerate and count all Paragraphs in a given TextRange.</para>
    /// <code>
    ///     int GetParagraphCount(TextRange range)
    ///     {
    ///         int paragraphCount = 0;
    ///         TextPointer position = range.Start;
    ///
    ///         while (position != null &amp;&amp; position.CompareTo(range.End) &lt; 0)
    ///         {
    ///             if (position.GetPointerContext(LogicalDirection.Backward) == TextPointerContext.ElementStart &amp;&amp;
    ///                 position.Parent is Paragraph)
    ///             {
    ///                 // Just entered a paragraph.
    ///                 paragraphCount ++;
    ///
    ///                 // Jump over it.
    ///                 // Schema does not allow nested paragraphs, so we will not miss any.
    ///                 position = ((Paragraph)position.Parent).ElementEnd;
    ///             }
    ///             else
    ///             {
    ///                 position = position.GetNextContextPosition(LogicalDirection.Forward);
    ///             }
    ///         }
    ///
    ///         return paragraphCount;
    ///     }
    /// </code>
    /// <para>Example 4. Idenifying whether the document is empty. The document appearing as empty
    /// in RichTextBox actually contains a Paragraph element with a Run child in it. So checking
    /// a document emptiness is a bit tricky task. In the following example we will utilize
    /// the insertion positions as the most natural mechanism for getting to character part or text content.</para>
    /// <code>
    ///     bool IsRichTextBoxEmpty(RichTextBox richTextBox)
    ///     {
    ///         FlowDocument document = richTextBox.Document; // get a document contained in a RichTextBox
    ///
    ///         TextPointer normalizedStart = document.ContentStart.GetInsertionPosition(LogicalDirection.Forward);
    ///         TextPointer normalizedEnd = document.ContentEnd.GetInsertionPosition(LogicalDirection.Backward);
    ///
    ///         // The character content is empty if normalized start and end pointers are at the same position
    ///         bool isEmpty = normalizedStart.CompareTo(normalizedEnd) == 0;
    ///
    ///         return isEmpty;
    ///     }
    /// </code>
    /// </example>
    //
    // Internal comments:
    //
    // TextContainer's implementation of the Text OM ITextPointer interface.
    //
    // TextPointers represent locations in the TextContainer.  They point to a
    // node/edge pair where operations like insert/remove/gettext take place.
    //
    // TextPointers have a property called LogicalDirection, that specifies where
    // they fall if content is insert at their position.  We track LogicalDirection
    // implicitly: forward direction means the position is always at
    // BeforeStart/BeforeEnd edges, backward direction the reverse.
    //
    // TextPointers are guaranteed to stick with their nodes across editing
    // operations.  For inserts, this happens automatically.  However, if the
    // node a TextPointer points to is removed from the tree, it is expected
    // that a TextPointer will follow its LogicalDirection to the closest neighbor
    // node still living in the tree.
    //
    // Since we don't store references to TextPointers in the tree itself,
    // we have to wait until a method on the TextPointer is called, then
    // check if the position's node is still in the tree.  This operation is
    // called synchronization, and the core method is SyncToTreeGeneration.
    //
    // SyncToTreeGeneration must be called on every public entry point before
    // attempting to use the TextPointer.
    //
    // Since positions always point to node/edge pairs, if we want to allocate
    // a position that references a character not on a node edge, we must split
    // the text node at the character position.  If we did no other work, the
    // tree could become extremely fragmented, with a text node allocated for
    // each character.  To keep the tree from fragmenting, positions ref count
    // the nodes they occupy.  We do some gymnastics using a finalizer on
    // TextPointer, adding unreferenced positions to a list we check
    // periodically in all public TextContainer methods.  Dead positions decrement
    // their nodes' ref counts, and a text node whose ref count drops to zero will
    // attempt to merge with neighbors.
    public class TextPointer : ContentPosition, ITextPointer
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        #region Constructors

        /// <summary>
        /// Creates a new instance of TextPointer object.
        /// </summary>
        /// <param name="textPointer">
        /// TextPointer from which initial properties and location are initialized.
        /// </param>
        /// <remarks>
        /// New TextPointers always have their IsFrozen property set to false,
        /// regardless of the state of the position parameter.  Otherwise the
        /// new TextPointer instance is identical to the position parameter.
        /// </remarks>
        internal TextPointer(TextPointer textPointer)
        {
            if (textPointer == null)
            {
                throw new ArgumentNullException("textPointer");
            }

            InitializeOffset(textPointer, 0, textPointer.GetGravityInternal());
        }

        // Creates a new TextPointer instance.
        internal TextPointer(TextPointer position, int offset)
        {
            if (position == null)
            {
                throw new ArgumentNullException("position");
            }

            InitializeOffset(position, offset, position.GetGravityInternal());
        }

        // Creates a new TextPointer instance.
        internal TextPointer(TextPointer position, LogicalDirection direction)
        {
            InitializeOffset(position, 0, direction);
        }

        // Creates a new TextPointer instance.
        internal TextPointer(TextPointer position, int offset, LogicalDirection direction)
        {
            InitializeOffset(position, offset, direction);
        }

        // Creates a new TextPointer instance.
        internal TextPointer(TextContainer textContainer, int offset, LogicalDirection direction)
        {
            SplayTreeNode node;
            ElementEdge edge;

            if (offset < 1 || offset > textContainer.InternalSymbolCount - 1)
            {
                throw new ArgumentException(/*SR.Get(SRID.BadDistance)*/);
            }

            textContainer.GetNodeAndEdgeAtOffset(offset, out node, out edge);

            Initialize(textContainer, (TextTreeNode)node, edge, direction, textContainer.PositionGeneration, false, false, textContainer.LayoutGeneration);
        }

        private void Initialize(TextContainer textContainer, TextTreeNode node, ElementEdge edge, LogicalDirection direction, object positionGeneration, bool v1, bool v2, object layoutGeneration)
        {
            throw new NotImplementedException();
        }

        // Creates a new TextPointer instance.
        internal TextPointer(TextContainer tree, TextTreeNode node, ElementEdge edge)
        {
            Initialize(tree, node, edge, LogicalDirection.Forward, tree.PositionGeneration, false, false, tree.LayoutGeneration);
        }

        // Creates a new TextPointer instance.
        internal TextPointer(TextContainer tree, TextTreeNode node, ElementEdge edge, LogicalDirection direction)
        {
            Initialize(tree, node, edge, direction, tree.PositionGeneration, false, false, tree.LayoutGeneration);
        }

        // Constructor equivalent to ITextPointer.CreatePointer
        internal TextPointer CreatePointer()
        {
            return new TextPointer(this);
        }

        // Constructor equivalent to ITextPointer.CreatePointer
        internal TextPointer CreatePointer(LogicalDirection gravity)
        {
            return new TextPointer(this, gravity);
        }

#if REFCOUNT_DEAD_TEXTPOINTERS
        // *** This code removed ***
        // The TextContainer originally was designed to ref count TextPointer references
        // to TextTreeNodes.  When a TextPointer is created, it addrefs its node.
        // When moved, it addrefs the destination and decrements the old position.
        // When finalized, it would decrement its final TextTreeNode.
        //
        // There are two problems with this code:
        // - The GC will null out managed fields occasionally.  This means we simply
        //   cannot use a finalizer.
        // - We don't really know/can't depend on how expensive it is to use the GC,
        //   and the whole scheme is an attempt at perf optimization.
        //
        // The current state of the code is that we still ref count on create and
        // move, but we've disabled the finalizer so TextPointers will reference
        // their final nodes "forever".  This leads to fragmentation: because
        // we split TextTreeTextNodes as TextPointer reference individual
        // characters.  However, there's an upper bound on the fragmentation
        // (we can't have more nodes than characters) and in practice no one
        // walks documents character by character.
        //
        // So, until we identify a specific perf problem, we're not attempting
        // to ressurect this code.
        //
        // If ever do identify fragmentation as a problem worth solving,
        // we can already think of at least three possible approaches:
        //
        // 1. Keep the existing logic, but instead of using a finalizer,
        //    store an array of WeakReferences on each node (usually null).
        //    Periodically check the array, pruning WeakReferences with
        //    null Targets.
        // 2. As above, but introduce a TextPointerNode instead of hanging
        //    arrays off other nodes.
        // 3. Keep a static array of TextContainers in memory, ref counted
        //    by TextPointers.  Restore the TextPointer finalizer, and in
        //    addition to decrementing the node ref count, decrement the
        //    TextContainer ref count.

        // This method adds the position to a list of "dead" positions (no
        // external references) that will be examined later to decrement
        // reference counts on nodes, and ultimately merge text nodes.
        //
        // It's important here that we don't do anything complicated
        // that might block the finalizer thread or cause too much
        // contention and hurt perf.  The same goes for code in
        // TextContainer.EmptyDeadPositionList that also uses the lock.
        /// <summary>
        /// </summary>
        ~TextPointer()
        {
            ArrayList deadPositionList;

            deadPositionList = _tree.DeadPositionList;

            lock (deadPositionList)
            {
                deadPositionList.Add(this);
            }
        }
#endif // REFCOUNT_DEAD_TEXTPOINTERS

        #endregion Constructors

        //------------------------------------------------------
        //
        //  Public Methods
        //
        //------------------------------------------------------

        #region Public Methods

        /// <summary>
        /// Returns true if this TextPointer is positioned within the same
        /// text containner as another TextPointer.
        /// </summary>
        /// <param name="textPosition">
        /// TextPointer to compare.
        /// </param>
        /// <remarks>
        /// <para>TextPointer objects positioned in different containers cannot
        /// participate in any operations dealing with several pointers.
        /// For instance, TextPointer objects from two different text containers
        /// cannot be compared with each other (by calling the method <see cref="CompareTo"/>).</para>
        /// <para>The purpose of this method is to test whether two TextPointer
        /// objects belong to the same text container or not.</para>
        /// <para>Formatted text can be contained within one these elements in Avalon:
        /// <see cref="TextBlock"/> or <see cref="FlowDocument"/>.
        /// We refer to them as to "text containers".</para>
        /// <para>Note, that if one text container is nested within another
        /// TextPointer objects positioned within a nested text container
        /// are not considered as belonging to the enclosing one.</para>
        /// </remarks>
        /// <example>
        /// <para>Example 1. This example shows how to check whether a given TextPointer
        /// is positioned between two other TextPointer objects - in a situation
        /// when there is no guarantee that all three positions belong to
        /// the same text container</para>
        /// <code>
        ///     bool IsPositionContainedBetween(TextPointer test, TextPointer start, TextPointer end)
        ///     {
        ///         if (!test.IsInSameDocument(start) || !test.IsInSameDocument(end))
        ///         {
        ///             return false;
        ///         }
        ///         return start.CompareTo(test) &lt;= 0 &amp;&amp; test.CompareTo(end) &lt;= 0;
        ///     }
        /// </code>
        /// </example>
        public bool IsInSameDocument(TextPointer textPosition)
        {
            if (textPosition == null)
            {
                throw new ArgumentNullException("textPosition");
            }

            _tree.EmptyDeadPositionList();

            return (this.TextContainer == textPosition.TextContainer);
        }

        /// <summary>
        /// Compares positions of this TextPointer with another TextPointer.
        /// </summary>
        /// <param name="position">
        /// The TextPointer to compare with.
        /// </param>
        /// <returns>
        /// Less than zero: this TextPointer preceeds position.
        /// Zero: this TextPointer is at the same location as position.
        /// Greater than zero: this TextPointer follows position.
        /// </returns>
        /// <exception cref="System.ArgumentException">
        /// Throws ArgumentException if position does not belong to the same
        /// text container as this TextPointer (you can use <see cref="TextPointer.IsInSameDocument"/>
        /// method to detect whether comparison is possible).
        /// </exception>
        public int CompareTo(TextPointer position)
        {
            int offsetThis;
            int offsetPosition;
            int result;

            _tree.EmptyDeadPositionList();

            ValidationHelper.VerifyPosition(_tree, position);

            SyncToTreeGeneration();
            position.SyncToTreeGeneration();

            offsetThis = GetSymbolOffset();
            offsetPosition = position.GetSymbolOffset();

            if (offsetThis < offsetPosition)
            {
                result = -1;
            }
            else if (offsetThis > offsetPosition)
            {
                result = +1;
            }
            else
            {
                result = 0;
            }

            return result;
        }

        /// <summary>
        /// Returns the type of content to one side of this TextPointer.
        /// </summary>
        /// <param name="direction">
        /// Direction to query.
        /// </param>
        /// <returns>
        /// <para>Returns <see cref="TextPointerContext.None"/> if this TextPointer
        /// is positioned at the beginning of a text container and the requested direction
        /// is <see cref="Avalonia.Media.TextFormatting.LogicalDirection.Backward"/>, or if it is positioned
        /// at the end of a text container  and the requested direction is
        /// <see cref="Avalonia.Media.TextFormatting.LogicalDirection.Forward"/>.</para>
        /// <para>Returns <see cref="TextPointerContext.ElementStart"/> if the TextPointer
        /// has an openenig tag of some of TextElements in the requested direction.</para>
        /// <para>Returns <see cref="TextPointerContext.ElementEnd"/> if the TextPointer
        /// has a closing tag of some of TextElements in the requested direction.</para>
        /// <para>Returns <see cref="TextPointerContext.Text"/> if the TextPointer
        /// is positioned within <see cref="Run"/> element and has some non-emty sequence of characters
        /// in requested direction.</para>
        /// <para>Returns <see cref="TextPointerContext.EmbeddedElement"/> is the TextPointer
        /// is positioned within <see cref="InlineUIContainer"/> or <see cref="BlockUIContainer"/>
        /// element and has <see cref="UIElement"/> as atomic symbol in a requested direction.</para>
        /// </returns>
        /// <example>
        /// <para>This example shows how to use <c>GetPointerContext</c> method in text content
        /// traversal algorithms. It implements an algorithm calculating a balanse of
        /// opening and closing tags between two TextPointer positions (each opening tag
        /// counted as +1, while a closing one as -1).</para>
        /// <code>
        ///     int GetElementTagBalance(TextPointer start, TextPointer end)
        ///     {
        ///         int balanse = 0;
        ///
        ///         while (start != null &amp;&amp; start.CompareTo(end) &lt; 0)
        ///         {
        ///             TextPointerContext forwardContext = start.GetPointerContext(LogicalDirection.Forward);
        ///
        ///             if (forwardContext == TextPointerContext.ElementStart)
        ///             {
        ///                 balanse++;
        ///             }
        ///             else if (forwardContext == TextPointerContext.ElementEnd)
        ///             {
        ///                 balanse--;
        ///             }
        ///             start = start.GetNextContextPosition(LogicalDirection.Forward);
        ///         }
        ///
        ///         return balanse;
        ///     }
        /// </code>
        /// </example>
        public TextPointerContext GetPointerContext(LogicalDirection direction)
        {
            ValidationHelper.VerifyDirection(direction, "direction");

            _tree.EmptyDeadPositionList();

            SyncToTreeGeneration();

            return (direction == LogicalDirection.Forward) ? GetPointerContextForward(_node, this.Edge) : GetPointerContextBackward(_node, this.Edge);
        }

        /// <summary>
        /// Returns the count of Unicode characters between this TextPointer and the
        /// edge of an element in the given direction.
        /// </summary>
        /// <param name="direction">
        /// Direction to query.
        /// </param>
        /// <remarks>
        /// If the TetPointer is positioned not inside a <see cref="Run"/> element,
        /// then the method always returns zero.
        /// </remarks>
        public int GetTextRunLength(LogicalDirection direction)
        {
            ValidationHelper.VerifyDirection(direction, "direction");

            _tree.EmptyDeadPositionList();

            SyncToTreeGeneration();

            int count = 0;

            // Combine adjacent text nodes into a single run.
            // This isn't just a perf optimization.  Because text positions
            // split text nodes, if we just returned a single node's text
            // callers would see strange side effects where position.GetTextLength() !=
            // position.GetText if a position is moved between the calls.
            if (_tree.PlainTextOnly)
            {
                // Optimize for TextBox, which only ever contains (sometimes
                // very large quantities of) text nodes.
                Invariant.Assert(this.GetScopingNode() is TextTreeRootNode);

                if (direction == LogicalDirection.Forward)
                {
                    count = _tree.InternalSymbolCount - this.GetSymbolOffset() - 1;
                }
                else
                {
                    count = this.GetSymbolOffset() - 1;
                }
            }
            else
            {
                TextTreeNode textNode = GetAdjacentTextNodeSibling(direction);

                while (textNode != null)
                {
                    count += textNode.SymbolCount;
                    textNode = ((direction == LogicalDirection.Forward) ? textNode.GetNextNode() : textNode.GetPreviousNode()) as TextTreeTextNode;
                }
            }

            return count;
        }

        /// <summary>
        /// Returns the distance between this TextPointer and another.
        /// </summary>
        /// <param name="position">
        /// TextPointer to compare.
        /// </param>
        /// <exception cref="System.ArgumentException">
        /// Throws an ArgumentException if the TextPointer position is not
        /// positioned within the same document as this TextPointer.
        /// </exception>
        /// <returns>
        /// <para>The return value will be negative if the TextPointer position
        /// preceeds this TextPointer, zero if the two TextPointers
        /// are equally positioned, or positive if position follows this
        /// TextPointer.</para>
        /// </returns>
        /// <remarks>
        /// <para>The distance is represented as a number of "symbols"
        /// between these two pointers.</para>
        /// <para>Each opening and each closing tag of any TextElement
        /// is considered as one symbol. So an empty TextElement contributes
        /// two symbols - one for each of tags.</para>
        /// <para>UIElement placed within InlineUIContainer or BlockUIContainer
        /// represented as one symbol - independently of how complex
        /// is its content. Even if the UIElement contains or is a
        /// text container it is treated as atomic entity - single symbol.
        /// This may be confusing especially if you do not pay
        /// muchy attention to a difference between the <see cref="TextElement"/>
        /// the <see cref="UIElement"/> class.</para>
        /// <para>Each 16-bit unicode character inside a <see cref="Run"/> element
        /// is considered as one symbol.</para>
        /// <para>For instance, for the following xaml:
        /// &lt;Run&gt;abc&lt;/Run&gt;&lt;InlineUIContainer&gt;&lt;Button&gt;OK&lt;/Button&gt;&lt;/InlineUIContainer&gt;
        /// the offset from itw content start to content end will be 8 -
        /// one for each of: (1) Run start, (2) "a", (3) "b", (4) "c", (5) Run end, (6) InlineUIContainer start,
        /// (7) whole Button element, (8) InlineUIContainer end. Note that <c>Button</c>
        /// element considered as one symbol even though it is represented
        /// by two tags and two characters.</para>
        /// </remarks>
        /// <example>
        /// <para>In this example we show how to use TextPointer offsets for
        /// persisting positional information. Assuming that the content of
        /// a RichTextBox is not changed between calls of
        /// GetPersistedSelection and RestoreSelectionFromPersistedRange
        /// methods, the selection will be restored to its original state.</para>
        /// <code>
        ///     struct PersistedTextRange { int Start; int End; }
        ///
        ///     PersistedTextRange GetPersistedSelection(RichTextBox richTextBox)
        ///     {
        ///         PersistedTextRange persistedSelection;
        ///
        ///         TextPointer contentStart = richTextBox.Document.ContentStart;
        ///         persistedSelection.Start = contentStart.GetOffsetToPosition(richTextBox.Selection.Start);
        ///         persistedSelection.End = contentStart.GetOffsetToPosition(richTextBox.Selection.End);
        ///
        ///         return persistedSelection;
        ///     }
        ///
        ///     RestoreSelectionFromPersistedRange(RichTextBox richTextBox, PersistedTextRange persistedRange)
        ///     {
        ///         TextPointer contentStart = richTextBox.Document.ContentStart;
        ///
        ///         richTextBox.Selection.Select(
        ///             contentStart.GetPositionAtOffset(persistedRange.Start),
        ///             contentStart.GetPositionAtOffset(persistedRange.End));
        ///     }
        ///
        /// </code>
        /// </example>
        public int GetOffsetToPosition(TextPointer position)
        {
            _tree.EmptyDeadPositionList();

            ValidationHelper.VerifyPosition(_tree, position);

            SyncToTreeGeneration();
            position.SyncToTreeGeneration();

            return (position.GetSymbolOffset() - GetSymbolOffset());
        }

        /// <summary>
        /// Returns text bordering this TextPointer from one side or another.
        /// </summary>
        /// <param name="direction">
        /// Direction to query.
        /// </param>
        /// <remarks>
        /// See GetTextInRun(direction, textBuffer, startIndex, count) method
        /// remarks for semantics of the returned text.
        /// </remarks>
        /// <example>
        /// <para>This is an example of simplistic plain text converter.
        /// This algorithm produces a string concatenating all text runs
        /// between two TextPointers.</para>
        /// <para>Note that this is really simplistic algorithm. You sould use
        /// <see cref="TextRange.Text"/> property for more sophisticated
        /// plain text conversion.</para>
        /// <code>
        ///     string GetPlainText(TextPointer start, TextPointer end)
        ///     {
        ///         StringBuilder buffer = new StringBuilder();
        ///
        ///         while (start != null &amp;&amp; start.CompareTo(end) &lt; end)
        ///         {
        ///             if (start.GetPointerContext(LogicalDirection.Forward) == TextPointerContext.Text)
        ///             {
        ///                 // Check if this text run reaches beyond the end position
        ///                 // and trancate the string if needed.
        ///                 string textRun = start.GetTextInRum(LogicalDirection.Forward);
        ///                 if (textRun.Length &gt; start.GetOffsetToPosition(end))
        ///                 {
        ///                     textRun = textRun.Substring(0, start.GetOffsetToPosition(end));
        ///                 }
        ///
        ///                 // Add characters from this text run to output buffer.
        ///                 buffer.Add(textRun);
        ///             }
        ///
        ///             start = start.GetNextContextPosition(LogicalDirection.Forward);
        ///             // Note that for text run this method skips the whole run, not just one character.
        ///         }
        ///         return buffer.ToString();
        ///     }
        /// </code>
        /// </example>
        public string GetTextInRun(LogicalDirection direction)
        {
            ValidationHelper.VerifyDirection(direction, "direction");

            return TextPointerBase.GetTextInRun(this, direction);
        }

        /// <summary>
        /// Copies characters bordering this TextPointer into a caller supplied char array.
        /// </summary>
        /// <param name="direction">
        /// Direction to query.
        /// </param>
        /// <param name="textBuffer">
        /// Buffer into which chars are copied.
        /// </param>
        /// <param name="startIndex">
        /// Index within the textBuffer array at which the copy is started.
        /// </param>
        /// <param name="count">
        /// The maximum number of characters to copy. Must be less than
        /// or equal to a (<c>textBuffer.Length - startIndex</c>).
        /// </param>
        /// <returns>
        /// The count of chars actually copied.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// Is thrown in the following cases: (a) when <c>startIndex</c> is less than zero,
        /// (b) when <c>startIndex</c> is greater than <c>textBuffer.Length</c>,
        /// (c) when <c>count</c> is less than zero, (d) when <c>count</c>
        /// is greater than size available for copying (<c>textBuffer.Length - startIndex</c>).
        /// </exception>
        /// <remarks>
        /// This method only returns uninterrupted runs of text -- no text will
        /// be returned if any symbol type other than text borders this
        /// TextPointer in the specified direction.  Similarly, text will only
        /// be returned up to the next non-text symbol.
        /// </remarks>
        public int GetTextInRun(LogicalDirection direction, char[] textBuffer, int startIndex, int count)
        {
            TextTreeTextNode textNode;

            ValidationHelper.VerifyDirection(direction, "direction");

            SyncToTreeGeneration();

            textNode = GetAdjacentTextNodeSibling(direction);

            return textNode == null ? 0 : GetTextInRun(_tree, GetSymbolOffset(), textNode, -1, direction, textBuffer, startIndex, count);
        }

        /// <summary>
        /// Returns an element represented by a symbol, if any, bordering
        /// this TextPointer in the specified direction.
        /// </summary>
        /// <param name="direction">
        /// Direction to query.
        /// </param>
        /// <returns>
        /// The element if its opening or closing tag exists
        /// in a specified direction. Otherwize returns null.
        /// </returns>
        /// <remarks>
        /// <para>The returned element may be both a <see cref="TextElement"/>
        /// and a <see cref="UIElement"/>.</para>
        /// <para><see cref="TextElement"/> object will be returned when
        /// this TextPointer is located before or after of either opening
        /// or closing tag in appropriate direction.</para>
        /// <para><see cref="UIElement"/> object can be returned only when
        /// the pointer is located outside its opening or closing tag - within
        /// <see cref="InlineUIContainer"/> or <see cref="BlockUIContainer"/>.</para>
        /// </remarks>
        public IAvaloniaObject GetAdjacentElement(LogicalDirection direction)
        {
            ValidationHelper.VerifyDirection(direction, "direction");

            _tree.EmptyDeadPositionList();
            SyncToTreeGeneration();

            return GetAdjacentElement(_node, this.Edge, direction);
        }

        /// <summary>
        /// Returns a TextPointer at a new position by a specified symbol
        /// count.
        /// </summary>
        /// <param name="offset">
        /// Number of symbols to advance.  offset may be negative, in which
        /// case the TextPointer is moved backwards.
        /// </param>
        /// <returns>
        /// TextPointer located at requested position in case if requested position
        /// does exist, otherwize returns null. LogicalDirection of the TextPointer
        /// returned is the same as of this TexPointer.
        /// </returns>
        /// <remarks>
        /// <para>This method, like all other TextPointer methods, defines a symbol
        /// as one of:</para>
        /// <para>- 16 bit Unicode character.</para>
        /// <para>- opening or closing tag of a <see cref="TextElement"/>.</para>
        /// <para>- the whole <see cref="UIElement"/> as atomic embedded object.</para>
        /// </remarks>
        /// <example>
        /// <para>This example shows how to use this method for creating TextPointers
        /// from a persisted index-based position representation.
        /// The first method returns a integer offset of a TextPointer
        /// from the beginning of a Paragraph. The second method re-creates
        /// a pointer from an integer ofset at the same relative position.</para>
        /// <code>
        ///     int GetPersistedPositionRelativeToParagraph(TextPointer position)
        ///     {
        ///         Paragraph paragraph = position.Paragraph;
        ///
        ///         if (paragraph == null)
        ///         {
        ///             return 0; // Some positions may be not within any Paragraph,
        ///             // so we need to return something; or throw exception.
        ///         }
        ///
        ///         return paragraph.ContentStart.GetOffsetToPosition(position);
        ///     }
        ///
        ///     int GetTextPointerRelativeToParagraph(Paragraph paragraph, int persistedPositionRelativeToParagraph)
        ///     {
        ///         // Check whether persisted position is still within this paragraph
        ///         if (persistedPositionRelativeToParagraph &gt;
        ///             paragraph.ContentStart.GetOffsetToPosition(paragraph.ContentEnd))
        ///         {
        ///             // the index is beyond the paragraph end. Return the farthest position within the paragraph.
        ///             return paragraph.ContentEnd;
        ///         }
        ///
        ///         return paragraph.ContentStart.GetPositionAtOffset(persistedPositionRelativeToParagraph);
        ///     }
        /// </code>
        /// </example>
        public TextPointer GetPositionAtOffset(int offset)
        {
            return GetPositionAtOffset(offset, this.LogicalDirection);
        }

        /// <summary>
        /// Returns a TextPointer at a new position by a specified symbol
        /// count.
        /// </summary>
        /// <param name="offset">
        /// Number of symbols to advance.  offset may be negative, in which
        /// case the TextPointer is moved backwards.
        /// </param>
        /// <param name="direction">
        /// LogicalDirection desired for a returned TextPointer.
        /// </param>
        /// <returns>
        /// TextPointer located at requested position in case if requested position
        /// does exist, otherwize returns null. LogicalDirection of the TextPointer
        /// returned is as specified by a <paramref name="direction"/>.
        /// </returns>
        /// <remarks>
        /// <para>This method, like all other TextPointer methods, defines a symbol
        /// as one of:</para>
        /// <para>- 16 bit Unicode character.</para>
        /// <para>- opening or closing tag of a <see cref="TextElement"/>.</para>
        /// <para>- the whole <see cref="UIElement"/> as atomic embedded object.</para>
        /// <para>See examples in <seealso cref="TextPointer.GetPositionAtOffset(int)"/> method with one parameter.</para>
        /// </remarks>
        public TextPointer GetPositionAtOffset(int offset, LogicalDirection direction)
        {
            TextPointer position = new TextPointer(this, direction);
            int actualCount = position.MoveByOffset(offset);
            if (actualCount == offset)
            {
                position.Freeze();
                return position;
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Returns a pointer at the next symbol in a specified
        /// direction, or past all following Unicode characters if the
        /// bordering content is Unicode text.
        /// </summary>
        /// <param name="direction">
        /// Direction to move.
        /// </param>
        /// <returns>
        /// TextPointer in a requested direction, null if this TextPointer
        /// borders the start or end of the document.
        /// </returns>
        /// <remarks>
        /// <para>If the following symbol is of type EmbeddedElement, ElementStart,
        /// or ElementEnd (as returned by the GetPointerContext method), then
        /// the TextPointer is advanced by exactly one symbol.</para>
        /// <para>If the following symbol is of type Text, then the TextPointer is
        /// advanced until it passes all following text (ie, until it reaches
        /// a position with a different return value for GetPointerContext).
        /// The exact symbol count crossed can be calculated in advance by
        /// calling GetTextLength.</para>
        /// <para>If there is no following symbol (start or end of the document),
        /// then the method returns null.</para>
        /// </remarks>
        /// <example>
        /// <para>This example shows how to use this method for traversing
        /// text content and examine its structure. The method implements
        /// a simplistic text content serializer, producing an xml-looking
        /// text.</para>
        /// <para>Note that to produce really well formed xml System.Xml
        /// interfaces must be used. We use this simplification only
        /// to make it more readable for people not familiar with System.Xml api.</para>
        /// <code>
        ///     string GetXaml(TextElement element)
        ///     {
        ///         StringBuilder buffer = new StringBuilder();
        ///
        ///         // Position a "navigator" pointer before the opening tag of the element.
        ///         TextPointer navigator = element.ElementStart;
        ///
        ///         while (navigator.CompareTo(element.ElementEnd) &lt; 0)
        ///         {
        ///             switch (navigator.GetPointerContext(LogicalDirection.Forward))
        ///             {
        ///                 case TextPointerContext.ElementStart :
        ///                     // Output opening tag of the TextElement
        ///                     buffer.AddFormat("&lt;{0}&gt;", navigator.GetAdjacentElement(LogicalDirection.Forward).GetType().Name);
        ///                     break;
        ///                 case TextPointerContext.ElementEnd :
        ///                     // Output closing tag of the TextElement
        ///                     buffer.AddFormat("&lt;/{0}&gt;", navigator.GetAdjacentElement(LogicalDirection.Forward).GetType().Name);
        ///                     break;
        ///                 case TextPointerContent.EmbeddedElement :
        ///                     // Output simple tag for embedded element
        ///                     buffer.AddFormat("&lt;{0}/&gt;", navigator.GetAdjacentElement(LogicalDirection.Forward).GetType().Name);
        ///                     break;
        ///                 case TextPointerContext.Text :
        ///                     // Output the text content of thi text run
        ///                     buffer.Add(navigator.GetTextInRun(LoigcalDirection.Forward);
        ///                     break;
        ///                 case TextPointerContext.None :
        ///                     Assert(false, "We do not expect to reach end of text container in this loop");
        ///                     break;
        ///             }
        ///
        ///             // Advance the naviagtor to the next context position.
        ///             navigator = navigator.GetNextContextPosition(LogicalDirection.Forward);
        ///
        ///             Assert(navigator != null, "We do not expect to reach an end of a text container in this loop, as it is limited by element.ContentEnd bounadry");
        ///         }
        ///     }
        /// </code>
        /// </example>
        public TextPointer GetNextContextPosition(LogicalDirection direction)
        {
            return (TextPointer)((ITextPointer)this).GetNextContextPosition(direction);
        }

        /// <summary>
        /// Returns a TextPointer at the closest insertion position in a
        /// specified direction.
        /// </summary>
        /// <param name="direction">
        /// Direction to search a closest insertion position.
        /// </param>
        /// <returns>
        /// TextPointer positioned at inserion point. The value is never null.
        /// </returns>
        /// <remarks>
        /// <para>The concept of insertion position is a convenience
        /// for traversing text content across structural boundaries,
        /// between table cells, paragraphs, list items etc.</para>
        /// <para>An insertion position is anywhere the containing document
        /// would normally place the caret.  Examples of positions that are not
        /// insertion positions include locations between Paragraphs
        /// (between closing tag of a preceding paragraph and an opening tag
        /// of the following paragraph). A position within text runs
        /// in the middle of a surrogate Unicode surrogate pair is also
        /// not an insertion position.</para>
        /// <para>The method can be used for disambiguating insertion positions
        /// in two cases: when the text has two insertion positions separated by
        /// a sequence of formatting tags, as between "d" and "t" in this
        /// markup: "&lt;Bold&gt;Bold&lt;/Bold&gt;text" - we have an insertion position
        /// before closing tag of Bold element and immediately after it. Both are
        /// valid insertion position and caret would stop on each of them
        /// depending on the direction of keyboard navigation. The method
        /// GetInsertionPosition allows user to pick one or another
        /// without moving to the "next" insertion position.</para>
        /// <para>Another important case when the method is useful is
        /// when a sequence of structural tags is involved. If you
        /// have a position, say between closing and opening paragraph tags,
        /// and want to fing a nearest insertion position the <c>direction</c>
        /// parameter will tell which of two possible positions to take:
        /// in the end of the preceding or in the begining of the following paragraph.</para>
        /// <para>If the pointer is already at insertion position
        /// but there is a non-empty sequence formatting in the given direction,
        /// then the position after all formatting tags will be returned.</para>
        /// <para>If the pointer is already at insertion position
        /// and there is no any formatting tags in the given direction,
        /// then the returned position is the same as this one.</para>
        /// <para>Somethimes the whole document does not have even
        /// one insertion position - it happens when the content
        /// is structurally incomplete, say in empty <see cref="List{T}"/>
        /// or <see cref="Table"/>element. In such case the method
        /// will return the  original position even though it is not
        /// an insertion position. The method never returns null.</para>
        /// </remarks>
        /// <example>
        /// <para>This example shows how to use the method <c>GetInsertionPosition</c>
        /// as a convenience of finding a starting "editable" position.</para>
        /// <code>
        ///     bool IsElementEmpty(TextElement element)
        ///     {
        ///         // Find first and last insertion positions in this element.
        ///         // We use inward directions to make sure that insertion position
        ///         // will be found correctly in case when the element is inline formatting one
        ///         // (i.e. Run or Span).
        ///         TextPointer start = element.ContentStart.GetInsertionPosition(LogicalDirection.Forward);
        ///         TextPointer end = element.ContentEnd.GetInsertionPosition(LogicalDirection.Backward);
        ///
        ///         // Element has empty printable content if its first and last
        ///         // insertion positions are equal.
        ///         return start.CompareTo(end) == 0;
        ///     }
        /// </code>
        /// </example>
        public TextPointer GetInsertionPosition(LogicalDirection direction)
        {
            return (TextPointer)((ITextPointer)this).GetInsertionPosition(direction);
        }

        // Used for pointer normalization in cases when direction does not matter.
        internal TextPointer GetInsertionPosition()
        {
            return GetInsertionPosition(LogicalDirection.Forward);
        }

        /// <summary>
        /// Returns a TextPointer in the direction indicated to the following
        /// insertion position.
        /// </summary>
        /// <param name="direction">
        /// Direction to move.
        /// </param>
        /// <returns>
        /// A TextPointer at an insertion position in a requested direction,
        /// null if there is no more insertion positions in that direction.
        /// </returns>
        /// <remarks>
        /// <para>The concept of insertion position is a convenience
        /// for traversing text content across structural boundaries,
        /// between table cells, paragraphs, list items etc.</para>
        /// <para>See more detailed definition of the concept of
        /// "insertion position" in the <see cref="TextPointer.GetInsertionPosition(LogicalDirection)"/>
        /// method.</para>
        /// <para>If the TextPointer is not currently at an insertion position, this
        /// method will move the TextPointer to the next insertion position in
        /// the indicated direction, just like the MoveToInsertionPosition
        /// method.</para>
        /// <para>If the TextPointer is currently at an insertion position, this
        /// method will move the TextPointer to following insertion position,
        /// if the end of document is not encountered.</para>
        /// </remarks>
        /// <example>
        /// <para>In this example we use the method <c>GetNextInsertionPosition</c>
        /// for passing over structural boundaries in a proces of
        /// enumerating all <see cref="Paragraph"/> in a range.</para>
        /// <code>
        ///     int GetParagraphCount(TextPointer start, TextPointer end)
        ///     {
        ///         int paragraphCount = 0;
        ///
        ///         while (start != null &amp;&amp; start.CompareTo(end) &lt; 0)
        ///         {
        ///             Paragraph paragraph = start.Paragraph;
        ///
        ///             if (paragraph != null)
        ///             {
        ///                 paragraphCount++;
        ///
        ///                 // Advance start to an end of the paragraph found
        ///                 start = paragraph.ContentEnd;
        ///             }
        ///
        ///             // Use GetNextInsertionPosition method to skip a sequence
        ///             // of structural tags
        ///             start = start.GetNextInsertionPosition(LogicalDirection.Forward);
        ///         }
        ///
        ///         return paragraphCount;
        ///     }
        /// </code>
        /// </example>
        public TextPointer GetNextInsertionPosition(LogicalDirection direction)
        {
            return (TextPointer)((ITextPointer)this).GetNextInsertionPosition(direction);
        }

        /// <summary>
        /// Returns a TextPointer at the start of line after skipping
        /// a given number of line starts in forward or backward direction.
        /// </summary>
        /// <param name="count">
        /// Number of line starts to skip when finding a desired line start position.
        /// Negative values specify preceding lines, zero specifies the current line,
        /// positive values specify following lines.
        /// </param>
        /// <exception cref="System.InvalidOperationException">
        /// Throws an InvalidOperationException if this TextPointer's HasValidLayout
        /// property is set false.  Without a calculated layout it is not possible
        /// to position relative to rendered lines.
        /// </exception>
        /// <returns>
        /// TextPointer positioned at the begining of a line requested
        /// (with LogicalDirection set to Forward).
        /// If there is no sufficient lines in requested direction,
        /// returns null.
        /// </returns>
        /// <remarks>
        /// <para>Line identification is possible only from normalized insertion positions;
        /// Line identification from not-normalized positions is mbigous and can produce
        /// unexpected results. Say, if a position is between closing and opening
        /// Paragraph tags, then GetInsertionPosition(LogicalDirection) is needed
        /// to decide whether we start from the end of previous Paragraph or
        /// from the start of the following one. Without such call
        /// </para>
        /// <para>If this TextPointer is at an otherwise ambiguous position, exactly
        /// between two lines, the LogicalDirection property is used to determine
        /// current position.  So a TextPointer with backward LogicalDirection
        /// is considered to be at the end of line, and calling MoveToLineBoundary(0)
        /// would reposition it at the start of the preceding line.  Making the
        /// same call with forward LogicalDirection would leave the TextPointer
        /// positioned where it started -- at the start of the following line.
        /// </para>
        /// </remarks>
        public TextPointer GetLineStartPosition(int count)
        {
            int actualCount;

            TextPointer lineStartPosition = GetLineStartPosition(count, out actualCount);

            return (actualCount != count) ? null : lineStartPosition;
        }

        /// <summary>
        /// Returns a TextPointer at the start of line after skipping
        /// a given number of line starts in forward or backward direction.
        /// </summary>
        /// <param name="count">
        /// Offset of the destination line.  Negative values specify preceding
        /// lines, zero specifies the current line, positive values specify
        /// following lines.
        /// </param>
        /// <param name="actualCount">
        /// The offset of the line moved to.  This value may be less than
        /// requested if the beginning or end of document is encountered.
        /// </param>
        /// <returns>
        /// TextPointer positioned at the begining of a line requested
        /// (with LogicalDirection set to Forward).
        /// If there is no sufficient lines in requested direction,
        /// returns a position at the beginning of a farthest line
        /// in this direction. In such case out parameter actualCount
        /// gets a number of lines actually skipped.
        /// Unlike the other override in this case the returned pointer is never null.
        /// </returns>
        /// <remarks>
        /// If this TextPointer is at an otherwise ambiguous position, exactly
        /// between two lines, the LogicalDirection property is used to determine
        /// current position.  So a TextPointer with backward LogicalDirection
        /// is considered to be at the end of line, and calling MoveToLineBoundary(0)
        /// would reposition it at the start of the preceding line.  Making the
        /// same call with forward LogicalDirection would leave the TextPointer
        /// positioned where it started -- at the start of the following line.
        /// </remarks>
        public TextPointer GetLineStartPosition(int count, out int actualCount)
        {
            this.ValidateLayout();

            TextPointer position = new TextPointer(this);

            if (this.HasValidLayout)
            {
                actualCount = position.MoveToLineBoundary(count);
            }
            else
            {
                actualCount = 0;
            }

            position.SetLogicalDirection(LogicalDirection.Forward);
            position.Freeze();

            return position;
        }

        /// <summary>
        /// Returns the bounding box of the content bordering this TextPointer
        /// in a specified direction.
        /// </summary>
        /// <param name="direction">
        /// Direction of content.
        /// </param>
        /// <remarks>
        /// <para>TextElement edges are not considered content for the purposes of
        /// this method.  If the TextPointer is positioned before a TextElement
        /// edge, the return value will be the bounding box of the next
        /// non-TextElement content.</para>
        /// <para>If there is no content in the specified direction, a zero-width
        /// Rect is returned with height matching the preceding content.</para>
        /// </remarks>
        public Rect GetCharacterRect(LogicalDirection direction)
        {
            ValidationHelper.VerifyDirection(direction, "direction");

            _tree.EmptyDeadPositionList();
            SyncToTreeGeneration();

            this.ValidateLayout();

            if (!this.HasValidLayout)
            {
                return Rect.Empty;
            }

            return TextPointerBase.GetCharacterRect(this, direction);
        }

        /// <summary>
        /// Inserts text at this TextPointer's position.
        /// </summary>
        /// <param name="textData">
        /// Text to insert.
        /// </param>
        /// <remarks>
        /// The LogicalDirection property specifies whether this TextPointer
        /// will be positioned before or after the new text.
        /// </remarks>
        public void InsertTextInRun(string textData)
        {
            if (textData == null)
            {
                throw new ArgumentNullException("textData");
            }

            _tree.EmptyDeadPositionList();
            SyncToTreeGeneration();

            TextPointer insertPosition;

            if (TextSchema.IsInTextContent(this))
            {
                insertPosition = this;

                _tree.BeginChange();
                try
                {
                    _tree.InsertTextInternal(insertPosition, textData);
                }
                finally
                {
                    _tree.EndChange();
                }
            }
            //else
            //{
            //    insertPosition = TextRangeEditTables.EnsureInsertionPosition(this);
            //}
        }

        /// <summary>
        /// Deletes text in Run at this TextPointer's position
        /// </summary>
        /// <remarks></remarks>
        /// <param name="count">
        /// Number of characters to delete.
        /// Positive count deletes text following this TextPointer in Run.
        /// Negative count deletes text preceding this TextPointer in Run.
        /// </param>
        /// <returns>
        /// Returns the actual count of deleted chars.
        /// The actual count may be less than requested in cases
        /// when original requested count exceeds text run length in given direction.
        /// </returns>
        public int DeleteTextInRun(int count)
        {
            _tree.EmptyDeadPositionList();
            SyncToTreeGeneration();

            // TextSchema Validation
            if (!TextSchema.IsInTextContent(this))
            {
                return 0;
            }

            // Direction to delete text in run
            LogicalDirection direction = count < 0 ? LogicalDirection.Backward : LogicalDirection.Forward;

            // Get text run length in given direction
            int maxDeleteCount = this.GetTextRunLength(direction);

            // Truncate count if it extends past the run in given direction
            if (count > 0 && count > maxDeleteCount)
            {
                count = maxDeleteCount;
            }
            else if (count < 0 && count < -maxDeleteCount)
            {
                count = -maxDeleteCount;
            }

            // Get a new pointer for deletion
            TextPointer deleteToPosition = new TextPointer(this, count);

            _tree.BeginChange();
            try
            {
                if (count > 0)
                {
                    _tree.DeleteContentInternal(this, deleteToPosition);
                }
                else if (count < 0)
                {
                    _tree.DeleteContentInternal(deleteToPosition, this);
                }
            }
            finally
            {
                _tree.EndChange();
            }

            return count;
        }

        /// <summary>
        /// Inserts a TextElement at this TextPointer's position.
        /// </summary>
        /// <param name="textElement">
        /// ContentElement to insert.
        /// </param>
        /// <remarks>
        /// The LogicalDirection property specifies whether this TextPointer
        /// will be positioned before or after the TextElement.
        /// </remarks>
        /// <exception cref="ArgumentException">
        /// Throws ArgumentException is textElement is not valid
        /// according to flow schema.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Throws InvalidOperationException if textElement cannot be inserted
        /// at this position because it belongs to another tree.
        /// </exception>
        internal void InsertTextElement(TextElement textElement)
        {
            Invariant.Assert(textElement != null);

            _tree.EmptyDeadPositionList();

            SyncToTreeGeneration();

            ValidationHelper.ValidateChild(this, textElement, "textElement");

            if (textElement.Parent != null)
            {
                throw new InvalidOperationException(/*SR.Get(SRID.TextPointer_CannotInsertTextElementBecauseItBelongsToAnotherTree)*/);
            }

            textElement.RepositionWithContent(this);
        }

        /// <summary>
        /// Insert a paragraph break at this position by splitting all elements upto its paragraph ancestor.
        /// </summary>
        /// <returns>
        /// When this position has a paragraph parent, this method returns a
        /// normalized position in the beginning of a second paragraph.
        ///
        /// Otherwise, if the position is not parented by a paragraph
        /// (for special insertion positions such as table row end, BlockUIContainer boundaries, etc),
        /// this method creates a paragraph by using rules of EnsureInsertionPosition()
        /// and returns a normalized position at the start of the paragraph created.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// Throws InvalidOperationException when this position has a non-splittable ancestor such as Hyperlink,
        /// since we cannot successfully split upto the parent paragraph in this case.
        /// </exception>
        //public TextPointer InsertParagraphBreak()
        //{
        //    _tree.EmptyDeadPositionList();
        //    SyncToTreeGeneration();

        //    if (this.TextContainer.Parent != null)
        //    {
        //        Type containerType = this.TextContainer.Parent.GetType();
        //        if (!TextSchema.IsValidChildOfContainer(containerType, typeof(Paragraph)))
        //        {
        //            throw new InvalidOperationException(/*SR.Get(SRID.TextSchema_IllegalElement, "Paragraph", containerType)*/);
        //        }
        //    }

        //    Inline ancestor = this.GetNonMergeableInlineAncestor();

        //    if (ancestor != null)
        //    {
        //        // Cannot split a hyperlink element!
        //        throw new InvalidOperationException(/*SR.Get(SRID.TextSchema_CannotSplitElement, ancestor.GetType().Name)*/);
        //    }

        //    TextPointer position;

        //    _tree.BeginChange();
        //    try
        //    {
        //        position = TextRangeEdit.InsertParagraphBreak(this, /*moveIntoSecondParagraph:*/true);
        //    }
        //    finally
        //    {
        //        _tree.EndChange();
        //    }

        //    return position;
        //}

        /// <summary>
        /// Insert a line break at this position.
        /// If the position is parented by a Run, the Run element is split at this position and then a line break inserted.
        /// </summary>
        /// <returns>
        /// TextPointer positioned immediately after the closing tag of
        /// a <see cref="LineBreak"/> element inserted by this method.
        /// </returns>
        public TextPointer InsertLineBreak()
        {
            _tree.EmptyDeadPositionList();
            SyncToTreeGeneration();

            TextPointer position;

            _tree.BeginChange();
            try
            {
                position = TextRangeEdit.InsertLineBreak(this);
            }
            finally
            {
                _tree.EndChange();
            }

            return position;
        }

        /// <summary>
        /// Debug only ToString override.
        /// </summary>
        public override string ToString()
        {
#if DEBUG
            return "TextPointer Id=" + _debugId + " NodeId=" + _node.DebugId + " Edge=" + this.Edge;
#else
            return base.ToString();
#endif // DEBUG
        }

        #endregion Public Methods

        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------

        #region Public Properties

        /// <summary>
        /// Returns true if layout is calculated at the current position.
        /// </summary>
        /// <remarks>
        /// Methods that depend on layout -- GetLineStartPosition,
        /// GetCharacterRect, and IsAtLineStartPosition -- will attempt
        /// to re-calculate a dirty layout when called.  Recalculating
        /// layout can be extremely expensive, however, and this method
        /// lets the caller detect when layout is dirty.
        /// </remarks>
        // Internal methods that depend on this property:
        //  - MoveToNextCaretPosition
        //  - MoveToBackspaceCaretPosition
        public bool HasValidLayout
        {
            get
            {
                return _tree.TextView == null ? false : _tree.TextView.IsValid && _tree.TextView.Contains(this);
            }
        }

        /// <summary>
        /// Specifies whether the TextPointer is associated with preceding or
        /// following content.
        /// </summary>
        /// <remarks>
        /// <para>If new content is insert at the TextPointer's current position, it
        /// will move to the edge of the new content that also borders its
        /// original associated content.</para>
        /// </remarks>
        public LogicalDirection LogicalDirection
        {
            get
            {
                return GetGravityInternal();
            }
        }

        /// <summary>
        /// Returns the logical parent scoping this TextPointer.
        /// </summary>
        public IAvaloniaObject Parent
        {
            get
            {
                _tree.EmptyDeadPositionList();
                SyncToTreeGeneration();

                return GetLogicalTreeNode();
            }
        }

        /// <summary>
        /// Returns true if this TextPointer is positioned at an insertion
        /// position.
        /// </summary>
        /// <remarks>
        /// <para>An "insertion position" is a position where where the containing document
        /// would normally place the caret.  Examples of positions that are not
        /// insertion positions include spaces between Paragraphs, or between
        /// Unicode surrogate pairs.</para>
        /// </remarks>
        public bool IsAtInsertionPosition
        {
            get
            {
                _tree.EmptyDeadPositionList();
                SyncToTreeGeneration();

                return TextPointerBase.IsAtInsertionPosition(this);
            }
        }

        /// <summary>
        /// Returns true if this TextPointer is positioned at the start of a
        /// line.
        /// </summary>
        /// <exception cref="System.InvalidOperationException">
        /// Throws an InvalidOperationException if this TextPointer's HasValidLayout
        /// property is set false.  Without a calculated layout it is not possible
        /// to determine where the current line starts or ends.
        /// </exception>
        /// <remarks>
        /// <para>If this TextPointer is at an otherwise ambiguous position, exactly
        /// between two lines, the LogicalDirection property is used to determine
        /// current position.  So a TextPointer with backward LogicalDirection
        /// will never have a true IsAtLineStartPosition unless it is positioned at the
        /// head of a document.</para>
        /// <para>This property is always false when HasValidLayout is false.</para>
        /// </remarks>
        public bool IsAtLineStartPosition
        {
            get
            {
                _tree.EmptyDeadPositionList();
                SyncToTreeGeneration();

                this.ValidateLayout();

                if (!this.HasValidLayout)
                {
                    return false;
                }

                TextSegment lineRange = _tree.TextView.GetLineRange(this);

                // Null lineRange if no layout is available.
                if (!lineRange.IsNull)
                {
                    TextPointer position = new TextPointer(this);
                    TextPointerContext backwardContext = position.GetPointerContext(LogicalDirection.Backward);

                    // Skip past any formatting.
                    while ((backwardContext == TextPointerContext.ElementStart || backwardContext == TextPointerContext.ElementEnd) &&
                        TextSchema.IsFormattingType(position.GetAdjacentElement(LogicalDirection.Backward).GetType()))
                    {
                        position.MoveToNextContextPosition(LogicalDirection.Backward);
                        backwardContext = position.GetPointerContext(LogicalDirection.Backward);
                    }

                    if (position.CompareTo((TextPointer)lineRange.Start) <= 0)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        /// <summary>
        /// Returns the paragraph scoping this textpointer
        /// </summary>
        /// <remarks>
        /// <para>When TextPointer is at insertion position it usually
        /// have non-null paragraph. The only exception is when
        /// it is positioned at the end of TableRow, where
        /// there is no scoping paragraph.</para>
        /// <para>When TextPointer is positioned outside of a paragraph,
        /// the property returns null.</para>
        /// </remarks>
        //public Paragraph Paragraph
        //{
        //    get
        //    {
        //        _tree.EmptyDeadPositionList();
        //        SyncToTreeGeneration();

        //        return this.ParentBlock as Paragraph;
        //    }
        //}

        /// <summary>
        /// Returns the paragraph-like parent of the pointer
        /// </summary>
        /// <remarks>
        /// If we would have a common base class for Paragraph and BlockUIContainer,
        /// we would return it here.
        /// </remarks>
        //internal Block ParagraphOrBlockUIContainer
        //{
        //    //  Introduce a new class - common base for Paragraph and BlockUIContainer
        //    get
        //    {
        //        _tree.EmptyDeadPositionList();
        //        SyncToTreeGeneration();

        //        Block parentBlock = this.ParentBlock;
        //        return (parentBlock is Paragraph) || (parentBlock is BlockUIContainer) ? parentBlock : null;
        //    }
        //}

        /// <summary>
        /// The start position of the document's content
        /// </summary>
        /// <remarks>
        /// <para>This property may be useful as a base for persistent
        /// position indexing - for calculating offsets
        /// to all other pointers.</para>
        /// <para>The <see cref="TextPointer.Parent"/> property for this
        /// position is not a TextElement - it is a text container,
        /// which can be one of <see cref="TextBlock"/> or
        /// <see cref="FlowDocument"/>.</para>
        /// </remarks>
        public TextPointer DocumentStart
        {
            get
            {
                return TextContainer.Start;
            }
        }

        /// <summary>
        /// The end position of the document's content.
        /// </summary>
        /// <remarks>
        /// <para>The <see cref="TextPointer.Parent"/> property for this
        /// position is not a TextElement - it is a text container,
        /// which can be one of <see cref="TextBlock"/> or
        /// <see cref="FlowDocument"/>.</para>
        /// </remarks>
        public TextPointer DocumentEnd
        {
            get
            {
                return TextContainer.End;
            }
        }

        #endregion Public Properties

        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------

        #region Internal Methods

        // Returns this TextPointer's topmost Inline ancestor, which is not a mergeable (or splittable) Inline element. (e.g. Hyperlink)
        internal Inline GetNonMergeableInlineAncestor()
        {
            Inline ancestor = this.Parent as Inline;

            while (ancestor != null && TextSchema.IsMergeableInline(ancestor.GetType()))
            {
                ancestor = ancestor.Parent as Inline;
            }

            return ancestor;
        }

        // Returns this TextPointer's closest ListItem ancestor.
        //internal ListItem GetListAncestor()
        //{
        //    TextElement ancestor = this.Parent as TextElement;

        //    while (ancestor != null && !(ancestor is ListItem))
        //    {
        //        ancestor = ancestor.Parent as TextElement;
        //    }

        //    return ancestor as ListItem;
        //}

        internal static int GetTextInRun(TextContainer textContainer, int symbolOffset, TextTreeTextNode textNode, int nodeOffset, LogicalDirection direction, char[] textBuffer, int startIndex, int count)
        {
            int skipCount;
            int finalCount;

            if (textBuffer == null)
            {
                throw new ArgumentNullException("textBuffer");
            }
            if (startIndex < 0)
            {
                throw new ArgumentException(/*SR.Get(SRID.NegativeValue, "startIndex")*/);
            }
            if (startIndex > textBuffer.Length)
            {
                throw new ArgumentException(/*SR.Get(SRID.StartIndexExceedsBufferSize, startIndex, textBuffer.Length)*/);
            }
            if (count < 0)
            {
                throw new ArgumentException(/*SR.Get(SRID.NegativeValue, "count")*/);
            }
            if (count > textBuffer.Length - startIndex)
            {
                throw new ArgumentException(/*SR.Get(SRID.MaxLengthExceedsBufferSize, count, textBuffer.Length, startIndex)*/);
            }
            Invariant.Assert(textNode != null, "textNode is expected to be non-null");

            textContainer.EmptyDeadPositionList();

            if (nodeOffset < 0)
            {
                skipCount = 0;
            }
            else
            {
                skipCount = (direction == LogicalDirection.Forward) ? nodeOffset : textNode.SymbolCount - nodeOffset;
                symbolOffset += nodeOffset;
            }
            finalCount = 0;

            // Loop and combine adjacent text nodes into a single run.
            // This isn't just a perf optimization.  Because text positions
            // split text nodes, if we just returned a single node's text
            // callers would see strange side effects where position.GetTextLength() !=
            // position.GetText() if another position is moved between the calls.
            while (textNode != null)
            {
                // Never return more textBuffer than the text following this position in the current text node.
                finalCount += Math.Min(count - finalCount, textNode.SymbolCount - skipCount);
                skipCount = 0;
                if (finalCount == count)
                    break;
                textNode = ((direction == LogicalDirection.Forward) ? textNode.GetNextNode() : textNode.GetPreviousNode()) as TextTreeTextNode;
            }

            // If we're reading backwards, need to fixup symbolOffset to point into the node.
            if (direction == LogicalDirection.Backward)
            {
                symbolOffset -= finalCount;
            }

            if (finalCount > 0) // We may not have allocated textContainer.RootTextBlock if no text was ever inserted.
            {
                TextTreeText.ReadText(textContainer.RootTextBlock, symbolOffset, finalCount, textBuffer, startIndex);
            }

            return finalCount;
        }

        internal static IAvaloniaObject GetAdjacentElement(TextTreeNode node, ElementEdge edge, LogicalDirection direction)
        {
            TextTreeNode adjacentNode;
            IAvaloniaObject element;

            adjacentNode = GetAdjacentNode(node, edge, direction);

            if (adjacentNode is TextTreeObjectNode)
            {
                element = ((TextTreeObjectNode)adjacentNode).EmbeddedElement;
            }
            else if (adjacentNode is TextTreeTextElementNode)
            {
                element = ((TextTreeTextElementNode)adjacentNode).TextElement;
            }
            else
            {
                // We're adjacent to a text node, or have no sibling in the specified direction.
                element = null;
            }

            return element;
        }

        /// <summary>
        /// Moves this TextPointer to another TextPointer's position.
        /// </summary>
        /// <param name="textPosition">
        /// Position to move to.
        /// </param>
        /// <exception cref="System.ArgumentException">
        /// Throws an ArgumentException if textPosition is not
        /// positioned within the same document.
        /// </exception>
        /// <exception cref="System.InvalidOperationException">
        /// Throws an InvalidOperationException if this TextPointer's IsFrozen
        /// property is set true.  Frozen TextPointers may not be repositioned.
        /// </exception>
        internal void MoveToPosition(TextPointer textPosition)
        {
            ValidationHelper.VerifyPosition(_tree, textPosition);

            VerifyNotFrozen();

            _tree.EmptyDeadPositionList();

            SyncToTreeGeneration();
            textPosition.SyncToTreeGeneration();

            MoveToNode(_tree, textPosition.Node, textPosition.Edge);
        }

        /// <summary>
        /// Advances this TextPointer to a new position by a specified symbol
        /// count.
        /// </summary>
        /// <param name="offset">
        /// Number of symbols to advance.  offset may be negative, in which
        /// case the TextPointer is moved backwards.
        /// </param>
        /// <exception cref="System.InvalidOperationException">
        /// Throws an InvalidOperationException if this TextPointer's IsFrozen
        /// property is set true.  Frozen TextPointers may not be repositioned.
        /// </exception>
        /// <remarks>
        /// This method, like all other TextPointer methods, defines a symbol
        /// as a
        /// - 16 bit Unicode character.
        /// - TextElement start or end edge.
        /// - UIElement.
        /// - ContentElement other than TextElement.
        /// </remarks>
        /// <returns>
        /// The number of symbols actually advanced.  The absolute value of the
        /// count returned may be less than requested if the end of document is
        /// encountered while advancing.
        /// </returns>
        internal int MoveByOffset(int offset)
        {
            SplayTreeNode node;
            ElementEdge edge;
            int symbolOffset;
            int currentOffset;

            VerifyNotFrozen();

            _tree.EmptyDeadPositionList();

            SyncToTreeGeneration();

            if (offset != 0)
            {
                currentOffset = GetSymbolOffset();
                symbolOffset = unchecked(currentOffset + offset);

                if (symbolOffset < 1)
                {
                    if (offset > 0)
                    {
                        // Rolled past Int32.MaxValue.  Go to end of doc.
                        symbolOffset = _tree.InternalSymbolCount - 1;
                        offset = symbolOffset - currentOffset;
                    }
                    else
                    {
                        // Underflow.  Go to start of doc.
                        offset += (1 - symbolOffset);
                        symbolOffset = 1;
                    }
                }
                else if (symbolOffset > _tree.InternalSymbolCount - 1)
                {
                    // Overflow.  Go to end of doc.
                    // NB: there's no symmetric check here for rolling under with distance=Int32.MinValue.
                    // Since GetSymbolOffset is always positive, we can't roll-around with a min value.
                    offset -= (symbolOffset - (_tree.InternalSymbolCount - 1));
                    symbolOffset = _tree.InternalSymbolCount - 1;
                }

                _tree.GetNodeAndEdgeAtOffset(symbolOffset, out node, out edge);
                MoveToNode(_tree, (TextTreeNode)node, edge);
            }

            return offset;
        }

        /// <summary>
        /// Advances this TextPointer to the next symbol in a specified
        /// direction, or past all following Unicode characters if the
        /// bordering content is Unicode text.
        /// </summary>
        /// <param name="direction">
        /// Direction to move.
        /// </param>
        /// <exception cref="System.InvalidOperationException">
        /// Throws an InvalidOperationException if this TextPointer's IsFrozen
        /// property is set true.  Frozen TextPointers may not be repositioned.
        /// </exception>
        /// <returns>
        /// true if the TextPointer is repositioned, false if the TextPointer
        /// borders the start or end of the document.
        /// </returns>
        /// <remarks>
        /// If the following symbol is of type EmbeddedElement, ElementStart,
        /// or ElementEnd (as returned by the GetPointerContext method), then
        /// the TextPointer is advanced by exactly one symbol.
        ///
        /// If the following symbol is of type Text, then the TextPointer is
        /// advanced until it passes all following text (ie, until it reaches
        /// a position with a different return value for GetPointerContext).
        /// The exact symbol count crossed can be calculated in advance by
        /// calling GetTextLength.
        ///
        /// If there is no following symbol (start or end of the document),
        /// then the method does nothing and returns false.
        /// </remarks>
        internal bool MoveToNextContextPosition(LogicalDirection direction)
        {
            TextTreeNode node;
            ElementEdge edge;
            bool moved;

            ValidationHelper.VerifyDirection(direction, "direction");
            VerifyNotFrozen();

            _tree.EmptyDeadPositionList();

            SyncToTreeGeneration();

            if (direction == LogicalDirection.Forward)
            {
                moved = GetNextNodeAndEdge(out node, out edge);
            }
            else
            {
                moved = GetPreviousNodeAndEdge(out node, out edge);
            }

            if (moved)
            {
                SetNodeAndEdge(AdjustRefCounts(node, edge, _node, this.Edge), edge);
                DebugAssertGeneration();
            }

            AssertState();

            return moved;
        }


        /// <summary>
        /// Moves this TextPointer to the closest insertion position in a
        /// specified direction. If the pointer is already at insertion point
        /// but there is a non-empty sequence formatting in the given direction,
        /// then the position moves to the other instance of this insertion
        /// position.
        /// </summary>
        /// <param name="direction">
        /// Direction to move.
        /// </param>
        /// <exception cref="System.InvalidOperationException">
        /// Throws an InvalidOperationException if this TextPointer's IsFrozen
        /// property is set true.  Frozen TextPointers may not be repositioned.
        /// </exception>
        /// <remarks>
        /// An "insertion position" is a position where new content may be added
        /// without breaking any semantic rules of the containing document.
        ///
        /// In practice, an insertion position is anywhere the containing document
        /// would normally place the caret.  Examples of positions that are not
        /// insertion positions include spaces between Paragraphs, or between
        /// Unicode surrogate pairs.
        /// </remarks>
        /// <returns>
        /// True if the TextPointer is repositioned, false otherwise.
        /// </returns>
        internal bool MoveToInsertionPosition(LogicalDirection direction)
        {
            ValidationHelper.VerifyDirection(direction, "direction");
            VerifyNotFrozen();

            _tree.EmptyDeadPositionList();
            SyncToTreeGeneration();

            return TextPointerBase.MoveToInsertionPosition(this, direction);
        }

        /// <summary>
        /// Advances this TextPointer in the direction indicated to the following
        /// insertion position.
        /// </summary>
        /// <param name="direction">
        /// Direction to move.
        /// </param>
        /// <exception cref="System.InvalidOperationException">
        /// Throws an InvalidOperationException if this TextPointer's IsFrozen
        /// property is set true.  Frozen TextPointers may not be repositioned.
        /// </exception>
        /// <remarks>
        /// An "insertion position" is a position where new content may be added
        /// without breaking any semantic rules of the containing document.
        ///
        /// In practice, an insertion position is anywhere the containing document
        /// would normally place the caret.  Examples of positions that are not
        /// insertion positions include spaces between Paragraphs, or between
        /// Unicode surrogate pairs.
        ///
        /// If the TextPointer is not currently at an insertion position, this
        /// method will move the TextPointer to the next insertion position in
        /// the indicated direction, just like the MoveToInsertionPosition
        /// method.
        ///
        /// If the TextPointer is currently at an insertion position, this
        /// method will move the TextPointer to following insertion position,
        /// if the end of document is not encountered.
        /// </remarks>
        /// <returns>
        /// True if the TextPointer is repositioned, false otherwise.
        /// </returns>
        internal bool MoveToNextInsertionPosition(LogicalDirection direction)
        {
            ValidationHelper.VerifyDirection(direction, "direction");
            VerifyNotFrozen();

            _tree.EmptyDeadPositionList();
            SyncToTreeGeneration();

            return TextPointerBase.MoveToNextInsertionPosition(this, direction);
        }

        /// <summary>
        /// Advances this TextPointer to the start of a neighboring line.
        /// </summary>
        /// <param name="count">
        /// Offset of the destination line.  Negative values specify preceding
        /// lines, zero specifies the current line, positive values specify
        /// following lines.
        /// </param>
        /// <exception cref="System.InvalidOperationException">
        /// Throws an InvalidOperationException if this TextPointer's IsFrozen
        /// property is set true.  Frozen TextPointers may not be repositioned.
        /// </exception>
        /// <returns>
        /// The offset of the line moved to.  This value may be less than
        /// requested if the beginning or end of document is encountered.
        /// </returns>
        /// <remarks>
        /// If this TextPointer is at an otherwise ambiguous position, exactly
        /// between two lines, the LogicalDirection property is used to determine
        /// current position.  So a TextPointer with backward LogicalDirection
        /// is considered to be at the end of line, and calling MoveToLineBoundary(0)
        /// would reposition it at the start of the preceding line.  Making the
        /// same call with forward LogicalDirection would leave the TextPointer
        /// positioned where it started -- at the start of the following line.
        /// </remarks>
        internal int MoveToLineBoundary(int count)
        {
            VerifyNotFrozen();

            this.ValidateLayout();

            if (!this.HasValidLayout)
            {
                return 0;
            }

            _tree.EmptyDeadPositionList();
            SyncToTreeGeneration();

            return TextPointerBase.MoveToLineBoundary(this, _tree.TextView, count);
        }

        /// <summary>
        /// Inserts a UIElement at this TextPointer's position.
        /// </summary>
        /// <param name="uiElement">
        /// UIElement to insert.
        /// </param>
        /// <remarks>
        /// The LogicalDirection property specifies whether this TextPointer
        /// will be positioned before or after the UIElement.
        /// </remarks>
        /// <exception cref="ArgumentException">
        /// Throws ArgumentException is contentElement is not valid
        /// according to flow schema.
        /// </exception>
        internal void InsertUIElement(IControl uiElement)
        {
            if (uiElement == null)
            {
                throw new ArgumentNullException("uiElement");
            }

            _tree.EmptyDeadPositionList();
            SyncToTreeGeneration();

            ValidationHelper.ValidateChild(this, uiElement, "uiElement");

            if (!((TextElement)this.Parent).IsEmpty) // the parent may be InlineUIContainer or BlockUIContainer
            {
                throw new InvalidOperationException(/*SR.Get(SRID.TextSchema_UIElementNotAllowedInThisPosition)*/);
            }

            _tree.BeginChange();
            try
            {
                _tree.InsertEmbeddedObjectInternal(this, uiElement);
            }
            finally
            {
                _tree.EndChange();
            }
        }

        // consider adding this to public API.
        internal TextElement GetAdjacentElementFromOuterPosition(LogicalDirection direction)
        {
            TextTreeTextElementNode elementNode;

            _tree.EmptyDeadPositionList();
            SyncToTreeGeneration();

            elementNode = GetAdjacentTextElementNodeSibling(direction);
            return (elementNode == null) ? null : elementNode.TextElement;
        }

        /// <summary>
        /// Sets the logical direction of this textpointer.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Throws an InvalidOperationException if this TextPointer's Freeze() method has been called.
        /// </exception>
        /// <param name="direction"></param>
        internal void SetLogicalDirection(LogicalDirection direction)
        {
            SplayTreeNode newNode;
            ElementEdge edge;

            ValidationHelper.VerifyDirection(direction, "direction");

            VerifyNotFrozen();

            _tree.EmptyDeadPositionList();

            if (GetGravityInternal() != direction)
            {
                SyncToTreeGeneration();

                newNode = _node;

                // We need to shift nodes to match the new gravity.
                switch (this.Edge)
                {
                    case ElementEdge.BeforeStart:
                        newNode = _node.GetPreviousNode();
                        if (newNode != null)
                        {
                            // Move to the previous sibling.
                            edge = ElementEdge.AfterEnd;
                        }
                        else
                        {
                            // Move to parent inner edge.
                            newNode = _node.GetContainingNode();
                            Invariant.Assert(newNode != null, "Bad tree state: newNode must be non-null (BeforeStart)");
                            edge = ElementEdge.AfterStart;
                        }
                        break;

                    case ElementEdge.AfterStart:
                        newNode = _node.GetFirstContainedNode();
                        if (newNode != null)
                        {
                            // Move to first child.
                            edge = ElementEdge.BeforeStart;
                        }
                        else
                        {
                            // Move to opposite edge.
                            newNode = _node;
                            edge = ElementEdge.BeforeEnd;
                        }

                        break;

                    case ElementEdge.BeforeEnd:
                        newNode = _node.GetLastContainedNode();
                        if (newNode != null)
                        {
                            // Move to last child.
                            edge = ElementEdge.AfterEnd;
                        }
                        else
                        {
                            // Move to opposite edge.
                            newNode = _node;
                            edge = ElementEdge.AfterStart;
                        }
                        break;

                    case ElementEdge.AfterEnd:
                        newNode = _node.GetNextNode();
                        if (newNode != null)
                        {
                            // Move to the next sibling.
                            edge = ElementEdge.BeforeStart;
                        }
                        else
                        {
                            // Move to parent inner edge.
                            newNode = _node.GetContainingNode();
                            Invariant.Assert(newNode != null, "Bad tree state: newNode must be non-null (AfterEnd)");
                            edge = ElementEdge.BeforeEnd;
                        }
                        break;

                    default:
                        Invariant.Assert(false, "Bad ElementEdge value");
                        edge = this.Edge;
                        break;
                }

                SetNodeAndEdge(AdjustRefCounts((TextTreeNode)newNode, edge, _node, this.Edge), edge);
                Invariant.Assert(GetGravityInternal() == direction, "Inconsistent position gravity");
            }
        }

        /// <summary>
        /// True if the Freeze method has been called, in which case
        /// this TextPointer is immutable and may not be repositioned.
        /// </summary>
        /// <Remarks>
        /// By default, TextPointers are mutable -- they may be
        /// repositioned with calls to methods like MoveByOffset, and
        /// LogicalDirection may be changed freely.  After Freeze is
        /// called, a TextPointer is locked down -- any attempt to set
        /// LogicalDirection or call repositioning methods will raise an
        /// InvalidOperationException.
        /// </Remarks>
        internal bool IsFrozen
        {
            get
            {
                _tree.EmptyDeadPositionList();

                return (_flags & (uint)Flags.IsFrozen) == (uint)Flags.IsFrozen;
            }
        }

        /// <summary>
        /// Makes this TextPointer immutable.
        /// </summary>
        /// <Remarks>
        /// By default, TextPointers are mutable -- they may be
        /// repositioned with calls to methods like MoveByOffset, and
        /// LogicalDirection may be changed freely.  After this method is
        /// called, a TextPointer is locked down -- any attempt to set
        /// LogicalDirection or call repositioning methods will raise an
        /// InvalidOperationException.
        ///
        /// The IsFrozen property will return true after this method is called.
        ///
        /// Calling Freeze multiple times has no additional effect.
        /// </Remarks>
        internal void Freeze()
        {
            _tree.EmptyDeadPositionList();

            SetIsFrozen();
        }

        /// <summary>
        /// Returns an immutable TextPointer instance positioned equally to
        /// this one, with a specified LogicalDirection.
        /// </summary>
        /// <param name="logicalDirection">
        /// LogicalDirection of the returned TextPointer.
        /// </param>
        /// <remarks>
        /// The TextPointer returned will always have its IsFrozen property set
        /// true.
        ///
        /// The return value will be a new TextPointer instance unless this
        /// TextPointer is already frozen with a matching LogicalDirection, in
        /// which case this TextPointer will be returned.
        /// </remarks>
        internal TextPointer GetFrozenPointer(LogicalDirection logicalDirection)
        {
            ValidationHelper.VerifyDirection(logicalDirection, "logicalDirection");

            _tree.EmptyDeadPositionList();

            return (TextPointer)TextPointerBase.GetFrozenPointer(this, logicalDirection);
        }

        void ITextPointer.SetLogicalDirection(LogicalDirection direction)
        {
            SetLogicalDirection(direction);
        }

        int ITextPointer.CompareTo(ITextPointer position)
        {
            return CompareTo((TextPointer)position);
        }

        int ITextPointer.CompareTo(StaticTextPointer position)
        {
            int offsetThis;
            int offsetPosition;
            int result;

            offsetThis = this.Offset + 1;
            offsetPosition = TextContainer.GetInternalOffset(position);

            if (offsetThis < offsetPosition)
            {
                result = -1;
            }
            else if (offsetThis > offsetPosition)
            {
                result = +1;
            }
            else
            {
                result = 0;
            }

            return result;
        }

        int ITextPointer.GetOffsetToPosition(ITextPointer position)
        {
            return GetOffsetToPosition((TextPointer)position);
        }

        TextPointerContext ITextPointer.GetPointerContext(LogicalDirection direction)
        {
            return GetPointerContext(direction);
        }

        int ITextPointer.GetTextRunLength(LogicalDirection direction)
        {
            return GetTextRunLength(direction);
        }

        // <see cref="System.Windows.Documents.ITextPointer.GetTextInRun"/>
        string ITextPointer.GetTextInRun(LogicalDirection direction)
        {
            return TextPointerBase.GetTextInRun(this, direction);
        }

        int ITextPointer.GetTextInRun(LogicalDirection direction, char[] textBuffer, int startIndex, int count)
        {
            return GetTextInRun(direction, textBuffer, startIndex, count);
        }

        object ITextPointer.GetAdjacentElement(LogicalDirection direction)
        {
            return GetAdjacentElement(direction);
        }

        Type ITextPointer.GetElementType(LogicalDirection direction)
        {
            IAvaloniaObject element;

            ValidationHelper.VerifyDirection(direction, "direction");

            _tree.EmptyDeadPositionList();

            SyncToTreeGeneration();

            element = GetElement(direction);

            return element != null ? element.GetType() : null;
        }

        bool ITextPointer.HasEqualScope(ITextPointer position)
        {
            TextTreeNode parent1;
            TextTreeNode parent2;
            TextPointer textPointer;

            _tree.EmptyDeadPositionList();

            ValidationHelper.VerifyPosition(_tree, position);

            textPointer = (TextPointer)position;

            SyncToTreeGeneration();
            textPointer.SyncToTreeGeneration();

            parent1 = GetScopingNode();
            parent2 = textPointer.GetScopingNode();

            return (parent1 == parent2);
        }

        // Candidate for replacing MoveToNextContextPosition for immutable TextPointer model
        ITextPointer ITextPointer.GetNextContextPosition(LogicalDirection direction)
        {
            ITextPointer pointer = ((ITextPointer)this).CreatePointer();
            if (pointer.MoveToNextContextPosition(direction))
            {
                pointer.Freeze();
            }
            else
            {
                pointer = null;
            }
            return pointer;
        }

        // Candidate for replacing MoveToInsertionPosition for immutable TextPointer model
        ITextPointer ITextPointer.GetInsertionPosition(LogicalDirection direction)
        {
            ITextPointer pointer = ((ITextPointer)this).CreatePointer();
            pointer.MoveToInsertionPosition(direction);
            pointer.Freeze();
            return pointer;
        }

        // Returns the closest insertion position, treating all unicode code points
        // as valid insertion positions.  A useful performance win over
        // GetNextInsertionPosition when only formatting scopes are important.
        ITextPointer ITextPointer.GetFormatNormalizedPosition(LogicalDirection direction)
        {
            ITextPointer pointer = ((ITextPointer)this).CreatePointer();
            TextPointerBase.MoveToFormatNormalizedPosition(pointer, direction);
            pointer.Freeze();
            return pointer;
        }

        // Candidate for replacing MoveToNextInsertionPosition for immutable TextPointer model
        ITextPointer ITextPointer.GetNextInsertionPosition(LogicalDirection direction)
        {
            ITextPointer pointer = ((ITextPointer)this).CreatePointer();
            if (pointer.MoveToNextInsertionPosition(direction))
            {
                pointer.Freeze();
            }
            else
            {
                pointer = null;
            }
            return pointer;
        }

        object ITextPointer.GetValue(AvaloniaProperty formattingProperty)
        {
            IAvaloniaObject parent;
            object val;

            if (formattingProperty == null)
            {
                throw new ArgumentNullException("formattingProperty");
            }

            _tree.EmptyDeadPositionList();

            SyncToTreeGeneration();

            parent = GetDependencyParent();

            if (parent == null)
            {
                val = AvaloniaProperty.UnsetValue;
            }
            else
            {
                val = parent.GetValue(formattingProperty);
            }

            return val;
        }

        object ITextPointer.ReadLocalValue(AvaloniaProperty formattingProperty)
        {
            TextElement element;

            if (formattingProperty == null)
            {
                throw new ArgumentNullException("formattingProperty");
            }

            _tree.EmptyDeadPositionList();

            SyncToTreeGeneration();

            element = this.Parent as TextElement;
            if (element == null)
            {
                throw new InvalidOperationException(/*SR.Get(SRID.NoScopingElement, "This TextPointer")*/);
            }

            return element.ReadLocalValue(formattingProperty);
        }

        LocalValueEnumerator ITextPointer.GetLocalValueEnumerator()
        {
            IAvaloniaObject element;

            _tree.EmptyDeadPositionList();

            SyncToTreeGeneration();

            element = this.Parent as TextElement;
            if (element == null)
            {
                //  Look into adding an empty ctor to LocalValueEnumerator.
                return new LocalValueEnumerator();
            }

            return element.GetLocalValueEnumerator();
        }

        ITextPointer ITextPointer.CreatePointer()
        {
            return ((ITextPointer)this).CreatePointer(0, this.LogicalDirection);
        }

        StaticTextPointer ITextPointer.CreateStaticPointer()
        {
            _tree.EmptyDeadPositionList();
            SyncToTreeGeneration();

            return new StaticTextPointer(_tree, _node, _node.GetOffsetFromEdge(this.Edge));
        }

        ITextPointer ITextPointer.CreatePointer(int offset)
        {
            return ((ITextPointer)this).CreatePointer(offset, this.LogicalDirection);
        }

        ITextPointer ITextPointer.CreatePointer(LogicalDirection gravity)
        {
            return ((ITextPointer)this).CreatePointer(0, gravity);
        }

        ITextPointer ITextPointer.CreatePointer(int offset, LogicalDirection gravity)
        {
            return new TextPointer(this, offset, gravity);
        }

        // <see cref="ITextPointer.Freeze"/>
        void ITextPointer.Freeze()
        {
            Freeze();
        }

        ITextPointer ITextPointer.GetFrozenPointer(LogicalDirection logicalDirection)
        {
            return GetFrozenPointer(logicalDirection);
        }

        // Worker for Min, accepts any ITextPointer.
        bool ITextPointer.MoveToNextContextPosition(LogicalDirection direction)
        {
            return MoveToNextContextPosition(direction);
        }

        int ITextPointer.MoveByOffset(int offset)
        {
            return MoveByOffset(offset);
        }

        void ITextPointer.MoveToPosition(ITextPointer position)
        {
            MoveToPosition((TextPointer)position);
        }

        void ITextPointer.MoveToElementEdge(ElementEdge edge)
        {
            MoveToElementEdge(edge);
        }

        internal void MoveToElementEdge(ElementEdge edge)
        {
            TextTreeTextElementNode elementNode;

            ValidationHelper.VerifyElementEdge(edge, "edge");
            VerifyNotFrozen();

            _tree.EmptyDeadPositionList();

            SyncToTreeGeneration();

            TextTreeNode scopingNode = GetScopingNode();
            elementNode = scopingNode as TextTreeTextElementNode;
            if (elementNode == null)
            {
                // if we're at the root of the tree, the pointer is
                // already at the element edge, and nothing more need be done.
                // This case can arise when a text tree contains only a
                // BlockUIContainer (and no text)
                if (scopingNode is TextTreeRootNode)
                {
                    return;
                }

                throw new InvalidOperationException(/*SR.Get(SRID.NoScopingElement, "This TextNavigator")*/);
            }

            MoveToNode(_tree, elementNode, edge);
        }

        // <see cref="TextPointer.MoveToLineBoundary"/>
        int ITextPointer.MoveToLineBoundary(int count)
        {
            return MoveToLineBoundary(count);
        }

        // <see cref="TextPointer.GetCharacterRect"/>
        Rect ITextPointer.GetCharacterRect(LogicalDirection direction)
        {
            return GetCharacterRect(direction);
        }

        bool ITextPointer.MoveToInsertionPosition(LogicalDirection direction)
        {
            return MoveToInsertionPosition(direction);
        }

        bool ITextPointer.MoveToNextInsertionPosition(LogicalDirection direction)
        {
            return MoveToNextInsertionPosition(direction);
        }

        // The caret methods are debug only until we actually start to use them.
        //  enable this code in retail once it is used.
#if DEBUG
        /// <summary>
        /// </summary>
        internal bool MoveToCaretPosition(LogicalDirection contentDirection)
        {
            TextPointer position;
            LogicalDirection oppositeDirection;
            bool moved;

            ValidationHelper.VerifyDirection(contentDirection, "contentDirection");

            _tree.EmptyDeadPositionList();
            SyncToTreeGeneration();

            VerifyNotFrozen();

            this.ValidateLayout();

            if (!this.HasValidLayout)
            {
                return false;
            }

            moved = false;

            if (!_tree.TextView.IsAtCaretUnitBoundary(this))
            {
                oppositeDirection = (contentDirection == LogicalDirection.Forward) ? LogicalDirection.Backward : LogicalDirection.Forward;
                position = (TextPointer)_tree.TextView.GetNextCaretUnitPosition(this, oppositeDirection);
                MoveToPosition(position);
                moved = true;
            }

            return moved;
        }

        /// <summary>
        /// </summary>
        internal bool MoveToNextCaretPosition(LogicalDirection direction)
        {
            TextPointer position;
            bool moved;

            ValidationHelper.VerifyDirection(direction, "direction");

            _tree.EmptyDeadPositionList();
            SyncToTreeGeneration();

            VerifyNotFrozen();

            this.ValidateLayout();

            if (!this.HasValidLayout)
            {
                return false;
            }

            position = (TextPointer)_tree.TextView.GetNextCaretUnitPosition(this, direction);
            moved = false;

            if (this.CompareTo(position)  != 0)
            {
                MoveToPosition(position);
                moved = true;
            }

            return moved;
        }

        /// <summary>
        /// </summary>
        internal bool MoveToBackspaceCaretPosition()
        {
            TextPointer position;
            bool moved;

            _tree.EmptyDeadPositionList();
            SyncToTreeGeneration();

            VerifyNotFrozen();

            this.ValidateLayout();

            if (!this.HasValidLayout)
            {
                return false;
            }

            position = (TextPointer)_tree.TextView.GetBackspaceCaretUnitPosition(this);
            moved = false;

            if (this.CompareTo(position) != 0)
            {
                MoveToPosition(position);
                moved = true;
            }

            return moved;
        }
#endif

        void ITextPointer.InsertTextInRun(string textData)
        {
            this.InsertTextInRun(textData);
        }

        //  this method no longer has a matching public analogue.
        // We should consider removing it, probably replacing it with
        // DeleteTextInRun.
        // Also need to consider whether or not it's appropriate to create a
        // default change block here.
        void ITextPointer.DeleteContentToPosition(ITextPointer limit)
        {
            _tree.BeginChange();
            try
            {
                // DeleteContent is clever enough to handle the this > limit case.
                TextRangeEdit.DeleteParagraphContent(this, (TextPointer)limit);
            }
            finally
            {
                _tree.EndChange();
            }
        }

        /// <see cref="ITextPointer.ValidateLayout"/>
        bool ITextPointer.ValidateLayout()
        {
            return this.ValidateLayout();
        }

        /// <see cref="ITextPointer.ValidateLayout"/>
        internal bool ValidateLayout()
        {
            return TextPointerBase.ValidateLayout(this, _tree.TextView);
        }

        // Returns the TextTreeTextNode in the direction indicated bordering
        // a TextPointer, or null if no such node exists.
        internal TextTreeTextNode GetAdjacentTextNodeSibling(LogicalDirection direction)
        {
            return GetAdjacentSiblingNode(direction) as TextTreeTextNode;
        }

        // Returns the TextTreeTextNode in the direction indicated bordering
        // a TextPointer, or null if no such node exists.
        internal static TextTreeTextNode GetAdjacentTextNodeSibling(TextTreeNode node, ElementEdge edge, LogicalDirection direction)
        {
            return GetAdjacentSiblingNode(node, edge, direction) as TextTreeTextNode;
        }

        // Returns the TextTreeTextNode in the direction indicated bordering
        // a TextPointer, or null if no such node exists.
        internal TextTreeTextElementNode GetAdjacentTextElementNodeSibling(LogicalDirection direction)
        {
            return GetAdjacentSiblingNode(direction) as TextTreeTextElementNode;
        }

        // Returns the TextTreeTextNode in the direction indicated bordering
        // a TextPointer, or null if no such node exists.
        internal TextTreeTextElementNode GetAdjacentTextElementNode(LogicalDirection direction)
        {
            return GetAdjacentNode(direction) as TextTreeTextElementNode;
        }

        // Returns the sibling node (ie, node in the same scope) in the direction indicated bordering
        // a TextPointer, or null if no such node exists.
        internal TextTreeNode GetAdjacentSiblingNode(LogicalDirection direction)
        {
            DebugAssertGeneration();

            return GetAdjacentSiblingNode(_node, this.Edge, direction);
        }

        internal static TextTreeNode GetAdjacentSiblingNode(TextTreeNode node, ElementEdge edge, LogicalDirection direction)
        {
            SplayTreeNode sibling;

            if (direction == LogicalDirection.Forward)
            {
                switch (edge)
                {
                    case ElementEdge.BeforeStart:
                        sibling = node;
                        break;

                    case ElementEdge.AfterStart:
                        sibling = node.GetFirstContainedNode();
                        break;

                    case ElementEdge.BeforeEnd:
                    default:
                        sibling = null;
                        break;

                    case ElementEdge.AfterEnd:
                        sibling = node.GetNextNode();
                        break;
                }
            }
            else // direction == LogicalDirection.Backward
            {
                switch (edge)
                {
                    case ElementEdge.BeforeStart:
                        sibling = node.GetPreviousNode();
                        break;

                    case ElementEdge.AfterStart:
                    default:
                        sibling = null;
                        break;

                    case ElementEdge.BeforeEnd:
                        sibling = node.GetLastContainedNode();
                        break;

                    case ElementEdge.AfterEnd:
                        sibling = node;
                        break;
                }
            }

            return (TextTreeNode)sibling;
        }

        // Returns the symbol offset within the TextContainer of this Position.
        internal int GetSymbolOffset()
        {
            DebugAssertGeneration();

            return GetSymbolOffset(_tree, _node, this.Edge);
        }

        // Returns the symbol offset within the TextContainer of this Position.
        internal static int GetSymbolOffset(TextContainer tree, TextTreeNode node, ElementEdge edge)
        {
            int offset;

            switch (edge)
            {
                case ElementEdge.BeforeStart:
                    offset = node.GetSymbolOffset(tree.Generation);
                    break;

                case ElementEdge.AfterStart:
                    offset = node.GetSymbolOffset(tree.Generation) + 1;
                    break;

                case ElementEdge.BeforeEnd:
                    offset = node.GetSymbolOffset(tree.Generation) + node.SymbolCount - 1;
                    break;

                case ElementEdge.AfterEnd:
                    offset = node.GetSymbolOffset(tree.Generation) + node.SymbolCount;
                    break;

                default:
                    Invariant.Assert(false, "Unknown value for position edge");
                    offset = 0;
                    break;
            }

            return offset;
        }

        // Returns the Logical Tree Node scoping this position.
        internal IAvaloniaObject GetLogicalTreeNode()
        {
            DebugAssertGeneration();

            return GetScopingNode().GetLogicalTreeNode();
        }

        // Updates the position state if the node referenced by this position has
        // been removed from the TextContainer.  This method must be called before
        // referencing the position's state when a public entry point is called.
        internal void SyncToTreeGeneration()
        {
            SplayTreeNode node;
            SplayTreeNode searchNode;
            SplayTreeNode parentNode;
            SplayTreeNode splayNode;
            ElementEdge edge;
            TextTreeFixupNode fixup = null;

            // If the tree hasn't had any deletions since the last time we
            // checked there's no work to do.
            if (_generation == _tree.PositionGeneration)
                return;

            // Invalidate the caret unit boundary cache -- the surrounding
            // content may have changed.
            this.IsCaretUnitBoundaryCacheValid = false;

            node = _node;
            edge = this.Edge;

            // If we can find a fixup node in the ancestor chain, this position
            // needs to be updated.
            //
            // It's possible to have cascading deletes -- some content was
            // deleted, then the nodes pointed to by a fixup node were themselves
            // deleted, and so forth.  So we have to keep checking all the
            // way up to the root.

            while (true)
            {
                searchNode = node;
                splayNode = node;

                while (true)
                {
                    parentNode = searchNode.ParentNode;
                    if (parentNode == null) // The root node is always valid.
                        break;

                    fixup = parentNode as TextTreeFixupNode;
                    if (fixup != null)
                        break;

                    if (searchNode.Role == SplayTreeNodeRole.LocalRoot)
                    {
                        splayNode.Splay();
                        splayNode = parentNode;
                    }
                    searchNode = parentNode;
                }

                if (parentNode == null)
                    break; // Checked all the way to the root, position is valid.

                // If we make it here we've found a fixup node.  Our gravity
                // tells us which direction to follow it.
                if (GetGravityInternal() == LogicalDirection.Forward)
                {
                    if (edge == ElementEdge.BeforeStart && fixup.FirstContainedNode != null)
                    {
                        // We get here if and only if a single TextElementNode was removed.
                        // Because only a single element was removed, we don't have to worry
                        // about whether the position was originally in some contained content.
                        // It originally pointed to the extracted node, so we can always
                        // move to contained content.
                        node = fixup.FirstContainedNode;
                        Invariant.Assert(edge == ElementEdge.BeforeStart, "edge BeforeStart is expected");
                    }
                    else
                    {
                        node = fixup.NextNode;
                        edge = fixup.NextEdge;
                    }
                }
                else
                {
                    if (edge == ElementEdge.AfterEnd && fixup.LastContainedNode != null)
                    {
                        // We get here if and only if a single TextElementNode was removed.
                        // Because only a single element was removed, we don't have to worry
                        // about whether the position was originally in some contained content.
                        // It originally pointed to the extracted node, so we can always
                        // move to contained content.
                        node = fixup.LastContainedNode;
                        Invariant.Assert(edge == ElementEdge.AfterEnd, "edge AfterEnd is expected");
                    }
                    else
                    {
                        node = fixup.PreviousNode;
                        edge = fixup.PreviousEdge;
                    }
                }
            }

            // Note we intentionally don't call AdjustRefCounts here.
            // We already incremented ref counts when the old target
            // node was deleted.
            SetNodeAndEdge((TextTreeNode)node, edge);

            // Update the position generation, so we don't do this work again
            // until the tree changes.
            _generation = _tree.PositionGeneration;

            AssertState();
        }

        // Returns the logical parent node of a text position.
        internal TextTreeNode GetScopingNode()
        {
            return GetScopingNode(_node, this.Edge);
        }

        internal static TextTreeNode GetScopingNode(TextTreeNode node, ElementEdge edge)
        {
            TextTreeNode scopingNode;

            switch (edge)
            {
                case ElementEdge.BeforeStart:
                case ElementEdge.AfterEnd:
                    scopingNode = (TextTreeNode)node.GetContainingNode();
                    break;

                case ElementEdge.AfterStart:
                case ElementEdge.BeforeEnd:
                default:
                    scopingNode = node;
                    break;
            }

            return scopingNode;
        }

        // Debug only -- asserts this TextPointer is synchronized to the current tree generation.
        internal void DebugAssertGeneration()
        {
            Invariant.Assert(_generation == _tree.PositionGeneration, "TextPointer not synchronized to tree generation!");
        }

        internal bool GetNextNodeAndEdge(out TextTreeNode node, out ElementEdge edge)
        {
            DebugAssertGeneration();

            return GetNextNodeAndEdge(_node, this.Edge, _tree.PlainTextOnly, out node, out edge);
        }

        // Finds the next run, returned as a node/edge pair.
        // Returns false if there is no following run, in which case node/edge will match the input position.
        // The returned node/edge pair respects the input position's gravity.
        internal static bool GetNextNodeAndEdge(TextTreeNode sourceNode, ElementEdge sourceEdge, bool plainTextOnly, out TextTreeNode node, out ElementEdge edge)
        {
            SplayTreeNode currentNode;
            SplayTreeNode newNode;
            SplayTreeNode nextNode;
            SplayTreeNode containingNode;
            bool startedAdjacentToTextNode;
            bool endedAdjacentToTextNode;

            node = sourceNode;
            edge = sourceEdge;

            newNode = node;
            currentNode = node;

            // If we started next to a TextTreeTextNode, and the next node
            // is also a TextTreeTextNode, then skip past the second node
            // as well -- multiple text nodes count as a single Move run.
            do
            {
                startedAdjacentToTextNode = false;
                endedAdjacentToTextNode = false;

                switch (edge)
                {
                    case ElementEdge.BeforeStart:
                        newNode = currentNode.GetFirstContainedNode();
                        if (newNode != null)
                        {
                            // Move to inner edge/first child.
                        }
                        else if (currentNode is TextTreeTextElementNode)
                        {
                            // Move to inner edge.
                            newNode = currentNode;
                            edge = ElementEdge.BeforeEnd;
                        }
                        else
                        {
                            // Move to next node.
                            startedAdjacentToTextNode = currentNode is TextTreeTextNode;
                            edge = ElementEdge.BeforeEnd;
                            goto case ElementEdge.BeforeEnd;
                        }
                        break;

                    case ElementEdge.AfterStart:
                        newNode = currentNode.GetFirstContainedNode();
                        if (newNode != null)
                        {
                            // Move to first child/second child or first child/first child child
                            if (newNode is TextTreeTextElementNode)
                            {
                                edge = ElementEdge.AfterStart;
                            }
                            else
                            {
                                startedAdjacentToTextNode = newNode is TextTreeTextNode;
                                endedAdjacentToTextNode = newNode.GetNextNode() is TextTreeTextNode;
                                edge = ElementEdge.AfterEnd;
                            }
                        }
                        else if (currentNode is TextTreeTextElementNode)
                        {
                            // Move to next node.
                            newNode = currentNode;
                            edge = ElementEdge.AfterEnd;
                        }
                        else
                        {
                            Invariant.Assert(currentNode is TextTreeRootNode, "currentNode is expected to be TextTreeRootNode");
                            // This is the root node, leave newNode null.
                        }
                        break;

                    case ElementEdge.BeforeEnd:
                        newNode = currentNode.GetNextNode();
                        if (newNode != null)
                        {
                            // Move to next node;
                            endedAdjacentToTextNode = newNode is TextTreeTextNode;
                            edge = ElementEdge.BeforeStart;
                        }
                        else
                        {
                            // Move to inner edge of parent.
                            newNode = currentNode.GetContainingNode();
                        }
                        break;

                    case ElementEdge.AfterEnd:
                        nextNode = currentNode.GetNextNode();
                        startedAdjacentToTextNode = nextNode is TextTreeTextNode;

                        newNode = nextNode;
                        if (newNode != null)
                        {
                            // Move to next node/first child;
                            if (newNode is TextTreeTextElementNode)
                            {
                                edge = ElementEdge.AfterStart;
                            }
                            else
                            {
                                // Move to next node/next next node.
                                endedAdjacentToTextNode = newNode.GetNextNode() is TextTreeTextNode;
                            }
                        }
                        else
                        {
                            containingNode = currentNode.GetContainingNode();

                            if (!(containingNode is TextTreeRootNode))
                            {
                                // Move to parent.
                                newNode = containingNode;
                            }
                        }
                        break;

                    default:
                        Invariant.Assert(false, "Unknown ElementEdge value");
                        break;
                }

                currentNode = newNode;

                // Multiple text nodes count as a single Move run.
                // Instead of iterating through N text nodes, exploit
                // the fact (when we can) that text nodes are only ever contained in
                // runs with no other content.  Jump straight to the end.
                if (startedAdjacentToTextNode && endedAdjacentToTextNode && plainTextOnly)
                {
                    newNode = newNode.GetContainingNode();
                    Invariant.Assert(newNode is TextTreeRootNode);

                    if (edge == ElementEdge.BeforeStart)
                    {
                        edge = ElementEdge.BeforeEnd;
                    }
                    else
                    {
                        newNode = newNode.GetLastContainedNode();
                        Invariant.Assert(newNode != null);
                        Invariant.Assert(edge == ElementEdge.AfterEnd);
                    }

                    break;
                }
            }
            while (startedAdjacentToTextNode && endedAdjacentToTextNode);

            if (newNode != null)
            {
                node = (TextTreeNode)newNode;
            }

            return (newNode != null);
        }

        internal bool GetPreviousNodeAndEdge(out TextTreeNode node, out ElementEdge edge)
        {
            DebugAssertGeneration();

            return GetPreviousNodeAndEdge(_node, this.Edge, _tree.PlainTextOnly, out node, out edge);
        }

        // Finds the previous run, returned as a node/edge pair.
        // Returns false if there is no preceding run, in which case node/edge will match the input position.
        // The returned node/edge pair respects the input positon's gravity.
        internal static bool GetPreviousNodeAndEdge(TextTreeNode sourceNode, ElementEdge sourceEdge, bool plainTextOnly, out TextTreeNode node, out ElementEdge edge)
        {
            SplayTreeNode currentNode;
            SplayTreeNode newNode;
            SplayTreeNode containingNode;
            bool startedAdjacentToTextNode;
            bool endedAdjacentToTextNode;

            node = sourceNode;
            edge = sourceEdge;

            newNode = node;
            currentNode = node;

            // If we started next to a TextTreeTextNode, and the next node
            // is also a TextTreeTextNode, then skip past the second node
            // as well -- multiple text nodes count as a single Move run.
            do
            {
                startedAdjacentToTextNode = false;
                endedAdjacentToTextNode = false;

                switch (edge)
                {
                    case ElementEdge.BeforeStart:
                        newNode = currentNode.GetPreviousNode();
                        if (newNode != null)
                        {
                            // Move to next node/last child;
                            if (newNode is TextTreeTextElementNode)
                            {
                                // Move to previous node last child/previous node
                                edge = ElementEdge.BeforeEnd;
                            }
                            else
                            {
                                // Move to previous previous node/previous node.
                                startedAdjacentToTextNode = newNode is TextTreeTextNode;
                                endedAdjacentToTextNode = startedAdjacentToTextNode && newNode.GetPreviousNode() is TextTreeTextNode;
                            }
                        }
                        else
                        {
                            containingNode = currentNode.GetContainingNode();

                            if (!(containingNode is TextTreeRootNode))
                            {
                                // Move to parent.
                                newNode = containingNode;
                            }
                        }
                        break;

                    case ElementEdge.AfterStart:
                        newNode = currentNode.GetPreviousNode();
                        if (newNode != null)
                        {
                            endedAdjacentToTextNode = newNode is TextTreeTextNode;

                            // Move to previous node;
                            edge = ElementEdge.AfterEnd;
                        }
                        else
                        {
                            // Move to inner edge of parent.
                            newNode = currentNode.GetContainingNode();
                        }
                        break;

                    case ElementEdge.BeforeEnd:
                        newNode = currentNode.GetLastContainedNode();
                        if (newNode != null)
                        {
                            // Move to penultimate child/last child or inner edge of last child.
                            if (newNode is TextTreeTextElementNode)
                            {
                                edge = ElementEdge.BeforeEnd;
                            }
                            else
                            {
                                startedAdjacentToTextNode = newNode is TextTreeTextNode;
                                endedAdjacentToTextNode = startedAdjacentToTextNode && newNode.GetPreviousNode() is TextTreeTextNode;
                                edge = ElementEdge.BeforeStart;
                            }
                        }
                        else if (currentNode is TextTreeTextElementNode)
                        {
                            // Move to next node.
                            newNode = currentNode;
                            edge = ElementEdge.BeforeStart;
                        }
                        else
                        {
                            Invariant.Assert(currentNode is TextTreeRootNode, "currentNode is expected to be a TextTreeRootNode");
                            // This is the root node, leave newNode null.
                        }
                        break;

                    case ElementEdge.AfterEnd:
                        newNode = currentNode.GetLastContainedNode();
                        if (newNode != null)
                        {
                            // Move to inner edge/last child.
                        }
                        else if (currentNode is TextTreeTextElementNode)
                        {
                            // Move to opposite edge.
                            newNode = currentNode;
                            edge = ElementEdge.AfterStart;
                        }
                        else
                        {
                            // Move to previous node.
                            startedAdjacentToTextNode = currentNode is TextTreeTextNode;
                            edge = ElementEdge.AfterStart;
                            goto case ElementEdge.AfterStart;
                        }
                        break;

                    default:
                        Invariant.Assert(false, "Unknown ElementEdge value");
                        break;
                }

                currentNode = newNode;

                // Multiple text nodes count as a single Move run.
                // Instead of iterating through N text nodes, exploit
                // the fact (when we can) that text nodes are only ever contained in
                // runs with no other content.  Jump straight to the start.
                if (startedAdjacentToTextNode && endedAdjacentToTextNode && plainTextOnly)
                {
                    newNode = newNode.GetContainingNode();
                    Invariant.Assert(newNode is TextTreeRootNode);

                    if (edge == ElementEdge.AfterEnd)
                    {
                        edge = ElementEdge.AfterStart;
                    }
                    else
                    {
                        newNode = newNode.GetFirstContainedNode();
                        Invariant.Assert(newNode != null);
                        Invariant.Assert(edge == ElementEdge.BeforeStart);
                    }

                    break;
                }
            }
            while (startedAdjacentToTextNode && endedAdjacentToTextNode);

            if (newNode != null)
            {
                node = (TextTreeNode)newNode;
            }

            return (newNode != null);
        }

        internal static TextPointerContext GetPointerContextForward(TextTreeNode node, ElementEdge edge)
        {
            TextTreeNode nextNode;
            TextTreeNode firstContainedNode;
            TextPointerContext symbolType;

            switch (edge)
            {
                case ElementEdge.BeforeStart:
                    symbolType = node.GetPointerContext(LogicalDirection.Forward);
                    break;

                case ElementEdge.AfterStart:
                    if (node.ContainedNode != null)
                    {
                        firstContainedNode = (TextTreeNode)node.GetFirstContainedNode();
                        symbolType = firstContainedNode.GetPointerContext(LogicalDirection.Forward);
                    }
                    else
                    {
                        goto case ElementEdge.BeforeEnd;
                    }
                    break;

                case ElementEdge.BeforeEnd:
                    // The root node is special, there's no ElementStart/End, so test for null parent.
                    Invariant.Assert(node.ParentNode != null || node is TextTreeRootNode, "Inconsistent node.ParentNode");
                    symbolType = (node.ParentNode != null) ? TextPointerContext.ElementEnd : TextPointerContext.None;
                    break;

                case ElementEdge.AfterEnd:
                    nextNode = (TextTreeNode)node.GetNextNode();
                    if (nextNode != null)
                    {
                        symbolType = nextNode.GetPointerContext(LogicalDirection.Forward);
                    }
                    else
                    {
                        // The root node is special, there's no ElementStart/End, so test for null parent.
                        Invariant.Assert(node.GetContainingNode() != null, "Bad position!"); // Illegal to be at root AfterEnd.
                        symbolType = (node.GetContainingNode() is TextTreeRootNode) ? TextPointerContext.None : TextPointerContext.ElementEnd;
                    }
                    break;

                default:
                    Invariant.Assert(false, "Unreachable code.");
                    symbolType = TextPointerContext.Text;
                    break;
            }

            return symbolType;
        }

        // Returns the symbol type preceding thisPosition.
        internal static TextPointerContext GetPointerContextBackward(TextTreeNode node, ElementEdge edge)
        {
            TextPointerContext symbolType;
            TextTreeNode previousNode;
            TextTreeNode lastChildNode;

            switch (edge)
            {
                case ElementEdge.BeforeStart:
                    previousNode = (TextTreeNode)node.GetPreviousNode();
                    if (previousNode != null)
                    {
                        symbolType = previousNode.GetPointerContext(LogicalDirection.Backward);
                    }
                    else
                    {
                        // The root node is special, there's no ElementStart/End, so test for null parent.
                        Invariant.Assert(node.GetContainingNode() != null, "Bad position!"); // Illegal to be at root BeforeStart.
                        symbolType = (node.GetContainingNode() is TextTreeRootNode) ? TextPointerContext.None : TextPointerContext.ElementStart;
                    }
                    break;

                case ElementEdge.AfterStart:
                    // The root node is special, there's no ElementStart/End, so test for null parent.
                    Invariant.Assert(node.ParentNode != null || node is TextTreeRootNode, "Inconsistent node.ParentNode");
                    symbolType = (node.ParentNode != null) ? TextPointerContext.ElementStart : TextPointerContext.None;
                    break;

                case ElementEdge.BeforeEnd:
                    lastChildNode = (TextTreeNode)node.GetLastContainedNode();
                    if (lastChildNode != null)
                    {
                        symbolType = lastChildNode.GetPointerContext(LogicalDirection.Backward);
                    }
                    else
                    {
                        goto case ElementEdge.AfterStart;
                    }
                    break;

                case ElementEdge.AfterEnd:
                    symbolType = node.GetPointerContext(LogicalDirection.Backward);
                    break;

                default:
                    Invariant.Assert(false, "Unknown ElementEdge value");
                    symbolType = TextPointerContext.Text;
                    break;
            }

            return symbolType;
        }

        // Inserts an Inline at the current location, adding contextual
        // elements as needed to enforce the schema.
        internal void InsertInline(Inline inline)
        {
            TextPointer position = this;

            // Check for hyperlink schema validity first -- we'll throw on an illegal Hyperlink descendent insert.
            bool isValidChild = TextSchema.ValidateChild(position, /*childType*/inline.GetType(), false /* throwIfIllegalChild */, true /* throwIfIllegalHyperlinkDescendent */);

            // Now, it is safe to assume that !isValidChild will be the case of incomplete content.
            if (!isValidChild)
            {
                if (position.Parent == null)
                {
                    // We should try to fix up the schema by adding elements instead of throwing here.
                    throw new InvalidOperationException(/*SR.Get(SRID.TextSchema_CannotInsertContentInThisPosition)*/);
                }

                // Ensure text content.
                //position = TextRangeEditTables.EnsureInsertionPosition(this);
                Invariant.Assert(position.Parent is Run, "EnsureInsertionPosition() must return a position in text content");
                Run run = (Run)position.Parent;

                if (run.IsEmpty)
                {
                    // Remove the implicit (empty) Run, since we are going to insert an inline at this position.
                    run.RepositionWithContent(null);
                }
                else
                {
                    // Position is parented by Run, split formatting elements to prepare for inserting inline at this position.
                    position = TextRangeEdit.SplitFormattingElement(position, /*keepEmptyFormatting:*/false);
                }

                Invariant.Assert(TextSchema.IsValidChild(position, /*childType*/inline.GetType()));
            }

            inline.RepositionWithContent(position);
        }

        // Helper that returns a IAvaloniaObject which is a common ancestor of two pointers.
        internal static IAvaloniaObject GetCommonAncestor(TextPointer position1, TextPointer position2)
        {
            TextElement element1 = position1.Parent as TextElement;
            TextElement element2 = position2.Parent as TextElement;

            IAvaloniaObject commonAncestor;

            if (element1 == null)
            {
                commonAncestor = position1.Parent;
            }
            else if (element2 == null)
            {
                commonAncestor = position2.Parent;
            }
            else
            {
                commonAncestor = TextElement.GetCommonAncestor(element1, element2);
            }

            return commonAncestor;
        }

        #endregion Internal methods

        //------------------------------------------------------
        //
        //  Internal Properties
        //
        //------------------------------------------------------

        #region Internal Properties

        // <see cref="System.Windows.Documents.ITextPointer.ParentType"/>
        Type ITextPointer.ParentType
        {
            get
            {
                _tree.EmptyDeadPositionList();

                SyncToTreeGeneration();

                IAvaloniaObject element = this.Parent;

                return element != null ? element.GetType() : null;
            }
        }

        /// <summary>
        ///  Returns the TextContainer that this TextPointer is a part of.
        /// </summary>
        ITextContainer ITextPointer.TextContainer
        {
            get
            {
                return this.TextContainer;
            }
        }

        // <see cref="TextPointer.HasValidLayout"/>
        bool ITextPointer.HasValidLayout
        {
            get
            {
                return this.HasValidLayout;
            }
        }

        // <see cref="ITextPointer.IsAtCaretUnitBoundary"/>
        bool ITextPointer.IsAtCaretUnitBoundary
        {
            get
            {
                _tree.EmptyDeadPositionList();
                SyncToTreeGeneration(); // NB: this call might set this.IsCaretUnitBoundaryCacheValid == false.

                this.ValidateLayout();
                if (!this.HasValidLayout)
                {
                    return false;
                }

                if (_layoutGeneration != _tree.LayoutGeneration)
                {
                    this.IsCaretUnitBoundaryCacheValid = false;
                }

                if (!this.IsCaretUnitBoundaryCacheValid)
                {
                    this.CaretUnitBoundaryCache = _tree.IsAtCaretUnitBoundary(this);
                    _layoutGeneration = _tree.LayoutGeneration;
                    this.IsCaretUnitBoundaryCacheValid = true;
                }

                return this.CaretUnitBoundaryCache;
            }
        }

        LogicalDirection ITextPointer.LogicalDirection
        {
            get
            {
                return this.LogicalDirection;
            }

            /*
            set
            {
                this.LogicalDirection = value;
            }
            */
        }

        bool ITextPointer.IsAtInsertionPosition
        {
            get { return this.IsAtInsertionPosition; }
        }

        // <see cref="TextPointer.IsFrozen"/>
        bool ITextPointer.IsFrozen
        {
            get
            {
                return this.IsFrozen;
            }
        }

        // <see cref="ITextPointer.Offset"/>
        int ITextPointer.Offset
        {
            get
            {
                return this.Offset;
            }
        }

        // <see cref="ITextPointer.Offset"/>
        internal int Offset
        {
            get
            {
                _tree.EmptyDeadPositionList();
                SyncToTreeGeneration();

                return GetSymbolOffset() - 1;
            }
        }

        // Offset in unicode chars within the document.
        int ITextPointer.CharOffset
        {
            get
            {
                return this.CharOffset;
            }
        }

        // Offset in unicode chars within the document.
        internal int CharOffset
        {
            get
            {
                TextTreeTextElementNode elementNode;

                _tree.EmptyDeadPositionList();
                SyncToTreeGeneration();

                int charOffset;

                switch (this.Edge)
                {
                    case ElementEdge.BeforeStart:
                        charOffset = _node.GetIMECharOffset();
                        break;

                    case ElementEdge.AfterStart:
                        charOffset = _node.GetIMECharOffset();

                        elementNode = _node as TextTreeTextElementNode;
                        if (elementNode != null)
                        {
                            charOffset += elementNode.IMELeftEdgeCharCount;
                        }
                        break;

                    case ElementEdge.BeforeEnd:
                    case ElementEdge.AfterEnd:
                        charOffset = _node.GetIMECharOffset() + _node.IMECharCount;
                        break;

                    default:
                        Invariant.Assert(false, "Unknown value for position edge");
                        charOffset = 0;
                        break;
                }

                return charOffset;
            }
        }

        /// <summary>
        ///  Returns the TextContainer that this TextPointer is a part of.
        /// </summary>
        internal TextContainer TextContainer
        {
            get
            {
                return _tree;
            }
        }

        /// <summary>
        /// A FrameworkElement owning a TextContainer to which this TextPointer belongs.
        /// </summary>
        internal StyledElement ContainingFrameworkElement
        {
            get
            {
                return ((StyledElement)_tree.Parent);
            }
        }

        // Position at row end (immediately before Row closing tag) is a valid stopper for a caret.
        // Editing operations are restricted here (e.g. typing should automatically jump
        // to the following character position.
        // This property identifies such special position.
        //internal bool IsAtRowEnd
        //{
        //    get
        //    {
        //        return TextPointerBase.IsAtRowEnd(this);
        //    }
        //}

#if DEBUG
        // Debug-only unique identifier for this instance.
        int DebugId
        {
            get
            {
                return _debugId;
            }
        }
#endif // DEBUG

        // Indicates if this TextPointer has an ancestor that is not a mergeable (or splittable) Inline element. (e.g. Hyperlink)
        internal bool HasNonMergeableInlineAncestor
        {
            get
            {
                Inline ancestor = this.GetNonMergeableInlineAncestor();

                return ancestor != null;
            }
        }

        // Returns true if position is at the start boundary of a non-mergeable inline ancestor (hyperlink)
        internal bool IsAtNonMergeableInlineStart
        {
            get
            {
                return TextPointerBase.IsAtNonMergeableInlineStart(this);
            }
        }

        // The node referenced by this position.
        internal TextTreeNode Node
        {
            get
            {
                return _node;
            }
        }

        // The edge referenced by this position.
        internal ElementEdge Edge
        {
            get
            {
                return (ElementEdge)(_flags & (uint)Flags.EdgeMask);
            }
        }

        // Returns the Block parenting this TextPointer, or null if none exists.
        //internal Block ParentBlock
        //{
        //    get
        //    {
        //        _tree.EmptyDeadPositionList();
        //        SyncToTreeGeneration();

        //        IAvaloniaObject parentBlock = this.Parent;

        //        while (parentBlock is Inline && !(parentBlock is AnchoredBlock))
        //        {
        //            parentBlock = ((Inline)parentBlock).Parent;
        //        }

        //        return parentBlock as Block;
        //    }
        //}

        #endregion Internal Properties

        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------

        #region Private Methods

        // Called by the TextPointer ctor.  Initializes this instance.
        private void InitializeOffset(TextPointer position, int distance, LogicalDirection direction)
        {
            SplayTreeNode node;
            ElementEdge edge;
            int offset;
            bool isCaretUnitBoundaryCacheValid;

            // We MUST sync to the current tree, otherwise we could addref
            // an orphaned node, resulting in a future unmatched release...
            // Ref counts on orphaned nodes are only considered at the time
            // of removal, not afterwards.
            position.SyncToTreeGeneration();

            if (distance != 0)
            {
                offset = position.GetSymbolOffset() + distance;
                if (offset < 1 || offset > position.TextContainer.InternalSymbolCount - 1)
                {
                    throw new ArgumentException(/*SR.Get(SRID.BadDistance)*/);
                }

                position.TextContainer.GetNodeAndEdgeAtOffset(offset, out node, out edge);

                isCaretUnitBoundaryCacheValid = false;
            }
            else
            {
                node = position.Node;
                edge = position.Edge;
                isCaretUnitBoundaryCacheValid = position.IsCaretUnitBoundaryCacheValid;
            }

            Initialize(position.TextContainer, (TextTreeNode)node, edge, direction, position.TextContainer.PositionGeneration,
                position.CaretUnitBoundaryCache, isCaretUnitBoundaryCacheValid, position._layoutGeneration);
        }

        // Called by the TextPointer ctor.  Initializes this instance.
        private void Initialize(TextContainer tree, TextTreeNode node, ElementEdge edge, LogicalDirection gravity, uint generation,
            bool caretUnitBoundaryCache, bool isCaretUnitBoundaryCacheValid, uint layoutGeneration)
        {
            _tree = tree;

            // Fixup of the target node based on gravity.
            // Positions always cling to a node edge that matches their gravity,
            // so that insert ops never affect the position.
            RepositionForGravity(ref node, ref edge, gravity);

            SetNodeAndEdge(node.IncrementReferenceCount(edge), edge);
            _generation = generation;

            this.CaretUnitBoundaryCache = caretUnitBoundaryCache;
            this.IsCaretUnitBoundaryCacheValid = isCaretUnitBoundaryCacheValid;
            _layoutGeneration = layoutGeneration;

            VerifyFlags();
            tree.AssertTree();
            AssertState();
        }

        // Throws an exception if this TextPointer is frozen.
        private void VerifyNotFrozen()
        {
            if (this.IsFrozen)
            {
                throw new InvalidOperationException(/*SR.Get(SRID.TextPositionIsFrozen)*/);
            }
        }

        // Inc/decs the position ref counts on TextTreeTextNodes as the navigator
        // is repositioned.
        // If the new ref is to a TextTreeTextNode, the node may be split.
        // Returns the actual node referenced, which will always be newNode,
        // unless newNode is a TextTreeTextNode that gets split.  The caller
        // should use the returned node to position navigators.
        private TextTreeNode AdjustRefCounts(TextTreeNode newNode, ElementEdge newNodeEdge, TextTreeNode oldNode, ElementEdge oldNodeEdge)
        {
            TextTreeNode node;

            // This test should walk the tree upwards to catch all errors...probably not worth the slowdown though.
            Invariant.Assert(oldNode.ParentNode == null || oldNode.IsChildOfNode(oldNode.ParentNode), "Trying to add ref a dead node!");
            Invariant.Assert(newNode.ParentNode == null || newNode.IsChildOfNode(newNode.ParentNode), "Trying to add ref a dead node!");

            node = newNode;

            if (newNode != oldNode || newNodeEdge != oldNodeEdge)
            {
                node = newNode.IncrementReferenceCount(newNodeEdge);
                oldNode.DecrementReferenceCount(oldNodeEdge);
            }

            return node;
        }

        // For any logical position (location between two symbols) there are two
        // possible node/edge pairs.  This method choses the pair that fits a
        // specified gravity, such that future inserts won't require that a text
        // position be moved, based on its gravity, at the node/edge pair.
        private static void RepositionForGravity(ref TextTreeNode node, ref ElementEdge edge, LogicalDirection gravity)
        {
            SplayTreeNode newNode;
            ElementEdge newEdge;

            newNode = node;
            newEdge = edge;

            switch (edge)
            {
                case ElementEdge.BeforeStart:
                    if (gravity == LogicalDirection.Backward)
                    {
                        newNode = node.GetPreviousNode();
                        newEdge = ElementEdge.AfterEnd;
                        if (newNode == null)
                        {
                            newNode = node.GetContainingNode();
                            newEdge = ElementEdge.AfterStart;
                        }
                    }
                    break;

                case ElementEdge.AfterStart:
                    if (gravity == LogicalDirection.Forward)
                    {
                        newNode = node.GetFirstContainedNode();
                        newEdge = ElementEdge.BeforeStart;
                        if (newNode == null)
                        {
                            newNode = node;
                            newEdge = ElementEdge.BeforeEnd;
                        }
                    }
                    break;

                case ElementEdge.BeforeEnd:
                    if (gravity == LogicalDirection.Backward)
                    {
                        newNode = node.GetLastContainedNode();
                        newEdge = ElementEdge.AfterEnd;
                        if (newNode == null)
                        {
                            newNode = node;
                            newEdge = ElementEdge.AfterStart;
                        }
                    }
                    break;

                case ElementEdge.AfterEnd:
                    if (gravity == LogicalDirection.Forward)
                    {
                        newNode = node.GetNextNode();
                        newEdge = ElementEdge.BeforeStart;
                        if (newNode == null)
                        {
                            newNode = node.GetContainingNode();
                            newEdge = ElementEdge.BeforeEnd;
                        }
                    }
                    break;
            }

            node = (TextTreeNode)newNode;
            edge = newEdge;
        }

        // Worker for GetGravity.  No parameter validation.
        private LogicalDirection GetGravityInternal()
        {
            return (this.Edge == ElementEdge.BeforeStart || this.Edge == ElementEdge.BeforeEnd) ? LogicalDirection.Forward : LogicalDirection.Backward;
        }

        // Returns the IAvaloniaObject scoping this position.
        private IAvaloniaObject GetDependencyParent()
        {
            DebugAssertGeneration();

            return GetScopingNode().GetDependencyParent();
        }

        // Returns the node in the direction indicated bordering
        // a TextPointer, or null if no such node exists.
        internal TextTreeNode GetAdjacentNode(LogicalDirection direction)
        {
            return GetAdjacentNode(_node, this.Edge, direction);
        }

        internal static TextTreeNode GetAdjacentNode(TextTreeNode node, ElementEdge edge, LogicalDirection direction)
        {
            TextTreeNode adjacentNode;

            adjacentNode = GetAdjacentSiblingNode(node, edge, direction);

            if (adjacentNode == null)
            {
                // We're the first or last child, try the parent.
                if (edge == ElementEdge.AfterStart || edge == ElementEdge.BeforeEnd)
                {
                    adjacentNode = node;
                }
                else
                {
                    adjacentNode = (TextTreeNode)node.GetContainingNode();
                }
            }

            return adjacentNode;
        }

        // Positions this navigator at a node/edge pair.
        // Node/edge are adjusted based on the current gravity.
        private void MoveToNode(TextContainer tree, TextTreeNode node, ElementEdge edge)
        {
            RepositionForGravity(ref node, ref edge, GetGravityInternal());

            _tree = tree;
            SetNodeAndEdge(AdjustRefCounts(node, edge, _node, this.Edge), edge);
            _generation = tree.PositionGeneration;
        }

        /// <summary>
        /// Returns the text element whose edge is in a specified direction
        /// from position.
        /// </summary>
        /// <returns>
        /// If the symbol in the specified direction is
        /// TextPointerContext.ElementStart or TextPointerContext.ElementEnd, then this
        /// method will return the element whose edge preceeds this TextPointer.
        ///
        /// Otherwise, the method returns null.
        /// </returns>
        private TextElement GetElement(LogicalDirection direction)
        {
            TextTreeTextElementNode elementNode;

            DebugAssertGeneration();

            elementNode = GetAdjacentTextElementNode(direction);

            return (elementNode == null) ? null : elementNode.TextElement;
        }

        // Invariant.Strict only.  Asserts this position has good state.
        private void AssertState()
        {
            if (Invariant.Strict)
            {
                // Positions must never have a null tree pointer.
                Invariant.Assert(_node != null, "Null position node!");

                if (GetGravityInternal() == LogicalDirection.Forward)
                {
                    // Positions with forward gravity must stay at left edges, otherwise inserts could displace them.
                    Invariant.Assert(this.Edge == ElementEdge.BeforeStart || this.Edge == ElementEdge.BeforeEnd, "Bad position edge/gravity pair! (1)");
                }
                else
                {
                    // Positions with backward gravity must stay at right edges, otherwise inserts could displace them.
                    Invariant.Assert(this.Edge == ElementEdge.AfterStart || this.Edge == ElementEdge.AfterEnd, "Bad position edge/gravity pair! (2)");
                }

                if (_node is TextTreeRootNode)
                {
                    // Positions may never be at the outer edge of the root node, since you can't insert content there.
                    Invariant.Assert(this.Edge != ElementEdge.BeforeStart && this.Edge != ElementEdge.AfterEnd, "Position at outer edge of root!");
                }
                else if (_node is TextTreeTextNode || _node is TextTreeObjectNode)
                {
                    // Text and object nodes have no inner edges/chilren, so you can't put a position there.
                    Invariant.Assert(this.Edge != ElementEdge.AfterStart && this.Edge != ElementEdge.BeforeEnd, "Position at inner leaf node edge!");
                }
                else
                {
                    // Add new asserts for new node types here.
                    Invariant.Assert(_node is TextTreeTextElementNode, "Unknown node type!");
                }

                Invariant.Assert(_tree != null, "Position has no tree!");

#if DEBUG_SLOW
                // This test is so slow we can't afford to run it even with Invariant.Strict.
                // It grinds execution to a halt.

                int count;

                if (_tree.RootTextBlock == null)
                {
                    count = 2; // Empty tree has two implicit edge symbols.
                }
                else
                {
                    count = 0;
                    for (TextTreeTextBlock textBlock = (TextTreeTextBlock)_tree.RootTextBlock.ContainedNode.GetMinSibling();
                         textBlock != null;
                         textBlock = (TextTreeTextBlock)textBlock.GetNextNode())
                    {
                        Invariant.Assert(textBlock.Count > 0, "Empty TextBlock!");
                        count += textBlock.Count;
                    }
                }
                Invariant.Assert(_tree.InternalSymbolCount == count, "Bad root symbol count!");

                Invariant.Assert((_tree.RootNode == null && count == 2) || count == GetNodeSymbolCountSlow(_tree.RootNode), "TextNode symbol count not in synch with tree!");

                if (_tree.RootNode != null)
                {
                    DebugWalkTree(_tree.RootNode.GetMinSibling());
                }
#endif // DEBUG_SLOW
            }
        }

#if DEBUG_SLOW
        // This test is so slow we can't afford to run it even with Invariant.Strict.
        // It grinds execution to a halt.
        private static void DebugWalkTree(SplayTreeNode node)
        {
            SplayTreeNode previousNode;
            SplayTreeNode previousPreviousNode;

            previousNode = null;
            previousPreviousNode = null;

            for (; node != null; node = node.GetNextNode())
            {
                if (node.SymbolCount == 0 &&
                    previousNode != null && previousNode.SymbolCount == 0 &&
                    previousPreviousNode != null && previousPreviousNode.SymbolCount == 0)
                {
                    Invariant.Assert(false, "Found three consecuative zero length nodes!");
                }

                previousPreviousNode = previousNode;
                previousNode = node;

                if (node.ContainedNode != null)
                {
                    DebugWalkTree(node.ContainedNode.GetMinSibling());
                }
            }
        }

        // Debug only.  Walks a node and all its children to get a brute force
        // symbol count.
        private static int GetNodeSymbolCountSlow(SplayTreeNode node)
        {
            SplayTreeNode child;
            int count;

            if (node is TextTreeRootNode || node is TextTreeTextElementNode)
            {
                count = 2;
                for (child = node.GetFirstContainedNode(); child != null; child = child.GetNextNode())
                {
                    count += GetNodeSymbolCountSlow(child);
                }
            }
            else
            {
                Invariant.Assert(node.ContainedNode == null, "Expected leaf node!");
                count = node.SymbolCount;
            }

            return count;
        }
#endif // DEBUG_SLOW

        // Repositions the TextPointer and clears any relevant caches.
        private void SetNodeAndEdge(TextTreeNode node, ElementEdge edge)
        {
            Invariant.Assert(edge == ElementEdge.BeforeStart ||
                             edge == ElementEdge.AfterStart ||
                             edge == ElementEdge.BeforeEnd ||
                             edge == ElementEdge.AfterEnd);

            _node = node;
            _flags = (_flags & ~(uint)Flags.EdgeMask) | (uint)edge;
            VerifyFlags();

            // Always clear the caret unit boundary cache when we move to a new position.
            this.IsCaretUnitBoundaryCacheValid = false;
        }

        // Setter for the public IsFrozen property.
        private void SetIsFrozen()
        {
            _flags |= (uint)Flags.IsFrozen;
            VerifyFlags();
        }

        // Ensure we have a valid _flags field.
        // See bug 1249258.
        private void VerifyFlags()
        {
            ElementEdge edge = (ElementEdge)(_flags & (uint)Flags.EdgeMask);

            Invariant.Assert(edge == ElementEdge.BeforeStart ||
                             edge == ElementEdge.AfterStart ||
                             edge == ElementEdge.BeforeEnd ||
                             edge == ElementEdge.AfterEnd);
        }

        #endregion Private methods

        // True when the CaretUnitBoundaryCache is ready for use.
        // If false the cache is not reliable.
        private bool IsCaretUnitBoundaryCacheValid
        {
            get
            {
                return (_flags & (uint)Flags.IsCaretUnitBoundaryCacheValid) == (uint)Flags.IsCaretUnitBoundaryCacheValid;
            }

            set
            {
                _flags = (_flags & ~(uint)Flags.IsCaretUnitBoundaryCacheValid) | (value ? (uint)Flags.IsCaretUnitBoundaryCacheValid : 0);
                VerifyFlags();
            }
        }

        // Cached value from this.TextContainer.TextView.IsAtCaretUnitBoundary.
        private bool CaretUnitBoundaryCache
        {
            get
            {
                return (_flags & (uint)Flags.CaretUnitBoundaryCache) == (uint)Flags.CaretUnitBoundaryCache;
            }

            set
            {
                _flags = (_flags & ~(uint)Flags.CaretUnitBoundaryCache) | (value ? (uint)Flags.CaretUnitBoundaryCache : 0);
                VerifyFlags();
            }
        }

        //------------------------------------------------------
        //
        //  Private Types
        //
        //------------------------------------------------------

        #region Private Types

        // Enum used for the _flags bitfield.
        [Flags]
        private enum Flags
        {
            EdgeMask                      = 15, // 4 low-order bis are an ElementEdge.
            IsFrozen                      = 16,
            IsCaretUnitBoundaryCacheValid = 32,
            CaretUnitBoundaryCache        = 64,
        }

        #endregion Private Types

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        #region Private Fields

        // The position's TextContainer.
        private TextContainer _tree;

        // The node referenced by this position.
        private TextTreeNode _node;

        // The value of TextContainer.PositionGeneration the last time this position
        // called SyncToTreeGeneration.
        private uint _generation;

        // The value of TextContainer.LayoutGeneration the last time
        // this position queried ITextView.IsAtCaretUnitBoundary.
        private uint _layoutGeneration;

        // Bitfield used by Edge, IsFrozen, IsCaretUnitBoundaryCacheValid, and
        // CaretUnitBoundaryCache properties.
        private uint _flags;

#if DEBUG
        // Debug-only unique identifier for this instance.
        private readonly int _debugId = _debugIdCounter++;

        // Debug-only id counter.
        private static int _debugIdCounter;
#endif // DEBUG

        #endregion Private Fields
    }
}
