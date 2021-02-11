// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: Default Framework TextContainer implementation.
// 

//#define DEBUG_SLOW

//using System;
//using System.Windows.Threading;
using MS.Internal;
//using System.Collections;
//using System.ComponentModel;
//using System.Windows.Controls;
//using System.Windows.Markup;
using MS.Internal.Documents;
//using System.Windows.Data;
using Avalonia;
using Avalonia.Documents;
using Avalonia.LogicalTree;
using Avalonia.Media.TextFormatting;
//using Avalonia.Threading;
using AvaloniaProperty = Avalonia.AvaloniaProperty;
using IAvaloniaObject = Avalonia.IAvaloniaObject;
using LocalValueEnumerator = Avalonia.LocalValueEnumerator;
using StyledElement = Avalonia.StyledElement;

namespace System.Windows.Documents
{
    /// <summary>
    /// The TextContainer class is an implementation of the abstract TextContainer class,
    /// the Framework's Text Object Model.  It serves as a general purpose
    /// backing store for text documents.
    /// 
    /// TextContainer accepts three kinds of content:
    /// 
    ///     - Text.  Raw characters/strings.
    ///     - UIElement controls.
    ///     - TextElements -- Inline, Paragraph, etc.  These elements scope other
    ///       content and provide structural information and/or hold AvaloniaProperty
    ///       values.
    /// 
    /// References to content in the TextContainer are represented by TextPointer and
    /// TextNavigator objects.  Once allocated, TextPositions may never be modified;
    /// TextNavigators may be repositioned and are the most efficent way to walk
    /// the TextContainer content.
    /// 
    /// Listeners may attach delegates to the TextChanged event to receive a
    /// notification whenever content is added, removed, or modified.
    /// 
    /// In addition to TextContainer overrides, TextContainer extends the Text Object Model
    /// with a number of features:
    /// 
    ///     - The TextContainer constructor takes an optional IAvaloniaObject argument
    ///       used to inherit AvaloniaProperty values.  If this argument is also
    ///       a StyledElement or FrameworkContentElement, it will parent all
    ///       top-level TextElements.
    ///     - Several methods are added that allow direct access to TextElement
    ///       instances.
    /// 
    /// </summary>
    //
    // INTERNAL COMMENTS.
    //
    // It's necessary understand the Avalon Text Object Model to follow the
    // TextContainer implementation.  There's a spec for the OM at
    // http://avalon/uis/TextBox%20and%20RichTextBox/Text%20Object%20Model.doc.
    //
    // The TextContainer is implemented as a tree of trees.  The outer tree follows
    // the logical relationships between TextElements and their children.  Each
    // child collection is stored as an individual splay tree.
    //
    // For example,
    //
    // <Paragraph>Hi!</Paragraph><Paragraph>Fancy <Inline>text</Inline>.</Paragraph>
    //
    // becomes
    //
    //   [TextTreeRootNode]
    //           ||
    //   [TextTreeTextElementNode]
    //           ||               \
    //           ||                \
    //           ||                 \
    //   [TextTreeTextNode "Hi!"]  [TextTreeTextElementNode]
    //                                       ||
    //                                    [TextTreeTextElementNode]
    //                                   /   ||   \
    //                                 /     ||     \
    //                               /       ||       \
    //                             /         ||         \
    //                           /           ||           \
    //                         /             ||             \
    // [TextTreeTextNode "Fancy "] [TextTreeTextNode "text"] [TextTreeTextNode "."]
    //
    // "||" is a link from tree to tree, "|" is a link between nodes in a single tree.
    //
    //  Nodes in the tree are one of
    //      - TextTreeRootNode.  One per tree, always the top-level root.  The
    //        root node is special because it holds tree global state, and because
    //        it can never be removed.  Never has siblings, may contain other
    //        nodes.
    //
    //      - TextTreeTextElementNode.  Always maps to a single TextElement.
    //        Element nodes may contain other nodes.
    //      
    //      - TextTreeTextNode.  References text.  Never contains other nodes.
    //
    //      - TextTreeObjectNode.  Always maps to a single UIElement.  Never
    //        contains other nodes.
    //
    // All nodes derive from the base TextTreeNode class.
    //
    // Raw text is stored in a separate tree of TextTreeTextBlocks.  The static
    // TextTreeText class handles all operations on the tree.
    //
    // TextPositions and TextNavigators are implemented in the TextPointer
    // and TextTreeNavigators classes respectively.
    internal class TextContainer : ITextContainer
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        #region Constructors

        /// <summary>
        /// Creates a new TextContainer instance.
        /// </summary>
        /// <param name="parent">
        /// A IAvaloniaObject used to supply inherited property values for
        /// TextElements contained within this TextContainer.
        ///
        /// parent may be null.
        ///
        /// If the object is StyledElement or FrameworkContentElement, it will be
        /// the parent of all top-level TextElements.
        /// </param>
        /// <param name="plainTextOnly">
        /// If true, only plain text may be inserted into this
        /// TextContainer and perf optimizations are enabled.
        /// </param>
        internal TextContainer(IAvaloniaObject parent, bool plainTextOnly)
        {
            _parent = parent;
            SetFlags(plainTextOnly, Flags.PlainTextOnly);
        }

        #endregion Constructors

        //------------------------------------------------------
        //
        //  Public Methods
        //
        //------------------------------------------------------

        #region Public Methods

        /// <summary>
        /// Debug only ToString override.
        /// </summary>
        public override string ToString()
        {
#if DEBUG
            return ("TextContainer Id=" + _debugId + " SymbolCount=" + this.SymbolCount);
#else
            return base.ToString();
#endif // DEBUG
        }

        #endregion Public Methods

        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------

        #region Internal Methods

        // Allocates an UndoManager for this instance.
        // This method should only be called once, but by controls
        // that also creates TextEditors and want undo.
        internal void EnableUndo(StyledElement uiScope)
        {
            //Invariant.Assert(_undoManager == null, SR.Get(SRID.TextContainer_UndoManagerCreatedMoreThanOnce));

            _undoManager = new UndoManager();
            MS.Internal.Documents.UndoManager.AttachUndoManager(uiScope, _undoManager);
        }

        internal void DisableUndo(StyledElement uiScope)
        {
            Invariant.Assert(_undoManager != null, "UndoManager not created.");

            Invariant.Assert(_undoManager == MS.Internal.Documents.UndoManager.GetUndoManager(uiScope));

            MS.Internal.Documents.UndoManager.DetachUndoManager(uiScope);
            _undoManager = null;
        }

        /// <summary>
        /// Sets a local value on the text element scoping position.
        /// </summary>
        /// <param name="position">
        /// A position scoped by the element on which to set the property.
        /// </param>
        /// <param name="property">
        /// Property to set.
        /// </param>
        /// <param name="value">
        /// Value to set.
        /// </param>
        /// <exception cref="System.InvalidOperationException">
        /// Throws InvalidOperationException if position is not scoped by a
        /// text element or if property may not be applied on the scoping text
        /// element.
        /// </exception>
        internal void SetValue(TextPointer position, AvaloniaProperty property, object value)
        {
            TextElement textElement;

//             VerifyAccess();

            if (position == null)
            {
                throw new ArgumentNullException("position");
            }
            if (property == null)
            {
                throw new ArgumentNullException("property");
            }

            EmptyDeadPositionList();

            ValidateSetValue(position);

            BeginChange();
            try
            {
                textElement = position.Parent as TextElement;
                Invariant.Assert(textElement != null);

                textElement.SetValue(property, value);
            }
            finally
            {
                EndChange();
            }
        }

        /// <summary>
        /// Sets local values on the text element scoping position.
        /// </summary>
        /// <param name="position">
        /// A position scoped by the element on which to set the property.
        /// </param>
        /// <param name="values">
        /// Values to set.
        /// </param>
        /// <exception cref="System.InvalidOperationException">
        /// Throws InvalidOperationException if position is not scoped by a
        /// text element or if a property value may not be applied on the
        /// scoping text element.
        /// </exception>
        internal void SetValues(TextPointer position, LocalValueEnumerator values)
        {
            TextElement textElement;
            LocalValueEntry property;

//             VerifyAccess();

            if (position == null)
            {
                throw new ArgumentNullException("position");
            }

            // LocalValueEnumerator is a struct.
            // if (values == null)
            // {
            //     throw new ArgumentNullException("values");
            // }

            EmptyDeadPositionList();

            ValidateSetValue(position);

            BeginChange();
            try
            {
                textElement = position.Parent as TextElement;
                Invariant.Assert(textElement != null);

                values.Reset();
                while (values.MoveNext())
                {
                    property = values.Current;

                    //  HUCK to word around a bug in property system.
                    // CachedSource property gets incorrect value in localValueEnumerator -
                    // object instead on HwndWindow. This causes crash in TextElement.SetValue.
                    // We actually don't need to transfer this property in this method,
                    // so the huck does not destory any functionality.
                    // More than that, we need to decide which properties are good to be
                    // copied this way. Actually we are interested in only formatting
                    // (serializable) properties. So we propably need to skip all other.
                    // However this bug needs a fix anyway.
                    if (property.Property.Name == "CachedSource")
                    {
                        continue;
                    }

                    // If the property is readonly on the text element, then we shouldn't
                    // try to copy the property value.
                    if (property.Property.IsReadOnly)
                    {
                        continue;
                    }

                    // Don't copy over Run.Text. This will get automatically invalidated by TextContainer
                    // when the Run's text content is set. Setting this property now will cause TextContainer
                    // changes that get us into trouble in the middle of undo.
                    if (property.Property == Run.TextProperty)
                    {
                        continue;
                    }

                    //BindingExpressionBase expr = property.Value as BindingExpressionBase;
                    //if (expr != null)
                    //{
                    //    // We can't duplicate a binding so copy over the current value instead.
                    //    textElement.SetValue(property.Property, expr.Value);
                    //}
                    //else
                    //{
                        textElement.SetValue(property.Property, property.Value);
                    //}
                }
            }
            finally
            {
                EndChange();
            }
        }

        internal void BeginChange()
        {
            BeginChange(true /* undo */);
        }

        internal void BeginChangeNoUndo()
        {
            BeginChange(false /* undo */);
        }

        /// <summary>
        /// </summary>
        internal void EndChange()
        {
            EndChange(false /* skipEvents */);
        }

        /// <summary>
        /// </summary>
        internal void EndChange(bool skipEvents)
        {
            TextContainerChangedEventArgs changes;

            Invariant.Assert(_changeBlockLevel > 0, "Unmatched EndChange call!");

            _changeBlockLevel--;

            if (_changeBlockLevel == 0)
            {
                try
                {
                    //
                    // Re-enable processing of the queue before the change notification.
                    //
                    //_rootNode.DispatcherProcessingDisabled.Dispose();

                    //
                    // Raise the Changed event.
                    //
                    if (_changes != null)
                    {
                        changes = _changes;
                        _changes = null;

                        if (this.ChangedHandler != null && !skipEvents)
                        {
#if PROPERTY_CHANGES
                            changes.MergePropertyChanges();
#endif
                            ChangedHandler(this, changes);
                        }
                    }
                }
                finally
                {
                    //
                    // Close the undo unit.
                    //
                    if (_changeBlockUndoRecord != null)
                    {
                        try
                        {
                            _changeBlockUndoRecord.OnEndChange();
                        }
                        finally
                        {
                            _changeBlockUndoRecord = null;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// </summary>
        void ITextContainer.BeginChange()
        {
            BeginChange();
        }

        void ITextContainer.BeginChangeNoUndo()
        {
            BeginChangeNoUndo();
        }

        /// <summary>
        /// </summary>
        void ITextContainer.EndChange()
        {
            EndChange(false /* skipEvents */);
        }

        /// <summary>
        /// </summary>
        void ITextContainer.EndChange(bool skipEvents)
        {
            EndChange(skipEvents);
        }

        // Allocate a new ITextPointer at the specified offset.
        // Equivalent to this.Start.CreatePointer(offset), but does not
        // necessarily allocate this.Start.
        ITextPointer ITextContainer.CreatePointerAtOffset(int offset, LogicalDirection direction)
        {
            return CreatePointerAtOffset(offset, direction);
        }

        // Allocate a new ITextPointer at the specified offset.
        // Equivalent to this.Start.CreatePointer(offset), but does not
        // necessarily allocate this.Start.
        internal TextPointer CreatePointerAtOffset(int offset, LogicalDirection direction)
        {
            EmptyDeadPositionList();
            DemandCreatePositionState();

            return new TextPointer(this, offset + 1, direction);
        }

        // Allocate a new ITextPointer at a specificed offset in unicode chars within the document.
        ITextPointer ITextContainer.CreatePointerAtCharOffset(int charOffset, LogicalDirection direction)
        {
            return CreatePointerAtCharOffset(charOffset, direction);
        }

        // Allocate a new ITextPointer at the specified character offset.
        // Returns null if this.IMECharCount == 0 -- there are no char offsets.
        // If a character offset has two possible mappings, for example
        //
        //   <Run>abc</Run><Run>def</Run>
        //
        // character offset 3 could be at the end of the first Run or the start
        // of the second -- then this method always returns the leftmost position
        // (ie, end of the first Run).
        internal TextPointer CreatePointerAtCharOffset(int charOffset, LogicalDirection direction)
        {
            EmptyDeadPositionList();
            DemandCreatePositionState();

            TextTreeNode node;
            ElementEdge edge;

            GetNodeAndEdgeAtCharOffset(charOffset, out node, out edge);

            return (node == null) ? null : new TextPointer(this, node, edge, direction);
        }

        ITextPointer ITextContainer.CreateDynamicTextPointer(StaticTextPointer position, LogicalDirection direction)
        {
            return CreatePointerAtOffset(GetInternalOffset(position) - 1, direction);
        }

        internal StaticTextPointer CreateStaticPointerAtOffset(int offset)
        {
            SplayTreeNode node;
            ElementEdge edge;
            int nodeOffset;

            EmptyDeadPositionList();
            DemandCreatePositionState();

            // Add +1 to offset because we're converting from external to internal offsets.
            GetNodeAndEdgeAtOffset(offset + 1, false /* splitNode */, out node, out edge);

            nodeOffset = (offset + 1) - node.GetSymbolOffset(this.Generation);

            return new StaticTextPointer(this, node, nodeOffset);
        }

        StaticTextPointer ITextContainer.CreateStaticPointerAtOffset(int offset)
        {
            return CreateStaticPointerAtOffset(offset);
        }

        TextPointerContext ITextContainer.GetPointerContext(StaticTextPointer pointer, LogicalDirection direction)
        {
            TextTreeNode node = (TextTreeNode)pointer.Handle0;
            int nodeOffset = pointer.Handle1;
            TextPointerContext context;
            ElementEdge edge;
            
            if (node is TextTreeTextNode && nodeOffset > 0 && nodeOffset < node.SymbolCount)
            {
                context = TextPointerContext.Text;
            }
            else if (direction == LogicalDirection.Forward)
            {
                edge = node.GetEdgeFromOffset(nodeOffset, LogicalDirection.Forward);
                context = TextPointer.GetPointerContextForward(node, edge);
            }
            else
            {
                edge = node.GetEdgeFromOffset(nodeOffset, LogicalDirection.Backward);
                context = TextPointer.GetPointerContextBackward(node, edge);
            }

            return context;
        }

        // Returns the "internal" offset of a StaticTextPointer, which includes
        // an extra symbol for the root node.
        internal int GetInternalOffset(StaticTextPointer position)
        {
            TextTreeNode node = (TextTreeNode)position.Handle0;
            int nodeOffset = position.Handle1;
            int offset;

            if (node is TextTreeTextNode)
            {
                offset = node.GetSymbolOffset(this.Generation) + nodeOffset;
            }
            else
            {
                offset = TextPointer.GetSymbolOffset(this, node, node.GetEdgeFromOffsetNoBias(nodeOffset));
            }

            return offset;
        }

        int ITextContainer.GetOffsetToPosition(StaticTextPointer position1, StaticTextPointer position2)
        {
            return GetInternalOffset(position2) - GetInternalOffset(position1);
        }

        int ITextContainer.GetTextInRun(StaticTextPointer position, LogicalDirection direction, char[] textBuffer, int startIndex, int count)
        {
            TextTreeNode node = (TextTreeNode)position.Handle0;
            int nodeOffset = position.Handle1;
            TextTreeTextNode textNode;

            textNode = node as TextTreeTextNode;
            if (textNode == null || nodeOffset == 0 || nodeOffset == node.SymbolCount)
            {
                textNode = TextPointer.GetAdjacentTextNodeSibling(node, node.GetEdgeFromOffsetNoBias(nodeOffset), direction);
                nodeOffset = -1;
            }

            return textNode == null ? 0 : TextPointer.GetTextInRun(this, textNode.GetSymbolOffset(this.Generation), textNode, nodeOffset, direction, textBuffer, startIndex, count);
        }

        object ITextContainer.GetAdjacentElement(StaticTextPointer position, LogicalDirection direction)
        {
            TextTreeNode node = (TextTreeNode)position.Handle0;
            int nodeOffset = position.Handle1;
            IAvaloniaObject adjacentElement;

            if (node is TextTreeTextNode && nodeOffset > 0 && nodeOffset < node.SymbolCount)
            {
                adjacentElement = null;
            }
            else
            {
                adjacentElement = TextPointer.GetAdjacentElement(node, node.GetEdgeFromOffset(nodeOffset, direction), direction);
            }

            return adjacentElement;
        }

        private TextTreeNode GetScopingNode(StaticTextPointer position)
        {
            TextTreeNode node = (TextTreeNode)position.Handle0;
            int nodeOffset = position.Handle1;
            TextTreeNode scopingNode;

            if (node is TextTreeTextNode && nodeOffset > 0 && nodeOffset < node.SymbolCount)
            {
                scopingNode = node;
            }
            else
            {
                scopingNode = TextPointer.GetScopingNode(node, node.GetEdgeFromOffsetNoBias(nodeOffset));
            }

            return scopingNode;
        }

        IAvaloniaObject ITextContainer.GetParent(StaticTextPointer position)
        {
            return GetScopingNode(position).GetLogicalTreeNode();
        }

        StaticTextPointer ITextContainer.CreatePointer(StaticTextPointer position, int offset)
        {
            int positionOffset = GetInternalOffset(position) - 1; // -1 to convert to external offset.
            return ((ITextContainer)this).CreateStaticPointerAtOffset(positionOffset + offset);
        }

        StaticTextPointer ITextContainer.GetNextContextPosition(StaticTextPointer position, LogicalDirection direction)
        {
            TextTreeNode node = (TextTreeNode)position.Handle0;
            int nodeOffset = position.Handle1;
            ElementEdge edge;
            StaticTextPointer nextContextPosition;
            bool moved;

            if (node is TextTreeTextNode && nodeOffset > 0 && nodeOffset < node.SymbolCount)
            {
                // Jump to the edge of this run of text nodes.
                if (this.PlainTextOnly)
                {
                    // Instead of iterating through N text nodes, exploit
                    // the fact (when we can) that text nodes are only ever contained in
                    // runs with no other content.  Jump straight to the end.
                    node = (TextTreeNode)node.GetContainingNode();
                    edge = (direction == LogicalDirection.Backward) ? ElementEdge.AfterStart : ElementEdge.BeforeEnd;
                }
                else
                {
                    while (true)
                    {
                        TextTreeTextNode nextTextNode = ((direction == LogicalDirection.Forward) ? node.GetNextNode() : node.GetPreviousNode()) as TextTreeTextNode;
                        if (nextTextNode == null)
                            break;
                        node = nextTextNode;
                    }
                    edge = (direction == LogicalDirection.Backward) ? ElementEdge.BeforeStart : ElementEdge.AfterEnd;
                }
                moved = true;
            }
            else if (direction == LogicalDirection.Forward)
            {
                edge = node.GetEdgeFromOffset(nodeOffset, LogicalDirection.Forward);
                moved = TextPointer.GetNextNodeAndEdge(node, edge, this.PlainTextOnly, out node, out edge);
            }
            else
            {
                edge = node.GetEdgeFromOffset(nodeOffset, LogicalDirection.Backward);
                moved = TextPointer.GetPreviousNodeAndEdge(node, edge, this.PlainTextOnly, out node, out edge);
            }

            if (moved)
            {
                nextContextPosition = new StaticTextPointer(this, node, node.GetOffsetFromEdge(edge));
            }
            else
            {
                nextContextPosition = StaticTextPointer.Null;
            }

            return nextContextPosition;
        }

        int ITextContainer.CompareTo(StaticTextPointer position1, StaticTextPointer position2)
        {
            int offsetThis;
            int offsetPosition;
            int result;

            offsetThis = GetInternalOffset(position1);
            offsetPosition = GetInternalOffset(position2);

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

        int ITextContainer.CompareTo(StaticTextPointer position1, ITextPointer position2)
        {
            int offsetThis;
            int offsetPosition;
            int result;

            offsetThis = GetInternalOffset(position1);
            offsetPosition = position2.Offset + 1; // Convert to internal offset with +1.

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

        object ITextContainer.GetValue(StaticTextPointer position, AvaloniaProperty formattingProperty)
        {
            IAvaloniaObject parent = GetScopingNode(position).GetDependencyParent();

            return (parent == null) ? AvaloniaProperty.UnsetValue : parent.GetValue(formattingProperty);
        }
        
        // Prepares the tree for an AddChange call, and raises the Changing
        // event if it has not already fired in this change block.
        //
        // This method must be called before a matching AddChange.
        //
        // (We cannot simply merge BeforeAddChange/AddChange because
        // in practice callers will sometimes raise LogicalTree events which
        // must be fired between Changing/Changed events.)
        internal void BeforeAddChange()
        {
            Invariant.Assert(_changeBlockLevel > 0, "All public APIs must call BeginChange!");

            if (this.HasListeners)
            {
                // Contact any listeners.
                if (this.ChangingHandler != null)
                {
                    ChangingHandler(this, EventArgs.Empty);
                }

                if (_changes == null)
                {
                    _changes = new TextContainerChangedEventArgs();
                }
            }
        }

        // Adds a change to the current change block.
        // This call must be preceeded by a matching BeforeAddChange.
        internal void AddChange(TextPointer startPosition, int symbolCount, int charCount, PrecursorTextChangeType textChange)
        {
            AddChange(startPosition, symbolCount, charCount, textChange, null, false);
        }

        // Adds a change to the current change block.
        // This call must be preceeded by a matching BeforeAddChange.
        internal void AddChange(TextPointer startPosition, int symbolCount, int charCount, PrecursorTextChangeType textChange, AvaloniaProperty property, bool affectsRenderOnly)
        {
            Invariant.Assert(textChange != PrecursorTextChangeType.ElementAdded &&
                             textChange != PrecursorTextChangeType.ElementExtracted,
                             "Need second TextPointer for ElementAdded/Extracted operations!");

            AddChange(startPosition, null, symbolCount, /* leftEdgeCharCount */ 0, charCount, textChange, property, affectsRenderOnly);
        }

        // Adds a change to the current change block.
        // This call must be preceeded by a matching BeforeAddChange.
        internal void AddChange(TextPointer startPosition, TextPointer endPosition,
                                int symbolCount, int leftEdgeCharCount, int childCharCount,
                                PrecursorTextChangeType textChange, AvaloniaProperty property, bool affectsRenderOnly)
        {
            Invariant.Assert(_changeBlockLevel > 0, "All public APIs must call BeginChange!");
            Invariant.Assert(!CheckFlags(Flags.ReadOnly) ||
                             textChange == PrecursorTextChangeType.PropertyModified,
                             "Illegal to modify TextContainer structure inside Change event scope!");

            if (this.HasListeners)
            {
                // Lazy init _changes.  This looks redundant with the BeforeAddChange call
                // we already require -- strictly speaking that's true.  But in practice,
                // we want the Invariant in this method to remind callers to think about
                // when they must call BeforeAddChange ahead of logical tree events.  Then,
                // in practice, there's a subtle bug where a listener might not initially
                // exist but is added during the logical tree events.  That we handle
                // here with an additional BeforeAddChange call instead of requiring all
                // our callers to remember to handle the more subtle case.
                if (_changes == null)
                {
                    _changes = new TextContainerChangedEventArgs();
                }

                Invariant.Assert(_changes != null, "Missing call to BeforeAddChange!");
                _changes.AddChange(textChange, startPosition.Offset, symbolCount, this.CollectTextChanges);
                if (this.ChangeHandler != null)
                {
                    FireChangeEvent(startPosition, endPosition, symbolCount, leftEdgeCharCount, childCharCount, textChange, property, affectsRenderOnly);
                }
            }
        }

        // Set the bit on the current change collection that remembers that
        // a local property value has changed on a TextElement covered by
        // the collection.
        internal void AddLocalValueChange()
        {
            Invariant.Assert(_changeBlockLevel > 0, "All public APIs must call BeginChange!");

            _changes.SetLocalPropertyValueChanged();
        }

        // The InsertText worker.  Adds text to the tree at a specified position.
        // text is either a string or char[] to insert.
        internal void InsertTextInternal(TextPointer position, object text)
        {
            TextTreeTextNode textNode;
            SplayTreeNode containingNode;
            TextPointer originalPosition;
            int symbolOffset;
            int textLength;
            LogicalDirection direction;

            Invariant.Assert(text is string || text is char[], "Unexpected type for 'text' parameter!");

            textLength = GetTextLength(text);

            if (textLength == 0)
                return;

            DemandCreateText();

            position.SyncToTreeGeneration();

            if (Invariant.Strict)
            {
                if (position.Node.SymbolCount == 0)
                {
                    // We expect only TextTreeTextNodes ever have zero symbol counts.
                    // This can happen in two cases:
                    //
                    // <TextNode referencedEdge=BeforeStart symbolCount=1+/> <TextNode referencedEdge=AfterEnd symbolCount=0/>
                    // or
                    // <TextNode referencedEdge=BeforeStart symbolCount=0/> <TextNode referencedEdge=AfterEnd symbolCount=1+/>
                    //
                    Invariant.Assert(position.Node is TextTreeTextNode);
                    Invariant.Assert((position.Edge == ElementEdge.AfterEnd && position.Node.GetPreviousNode() is TextTreeTextNode && position.Node.GetPreviousNode().SymbolCount > 0) ||
                                 (position.Edge == ElementEdge.BeforeStart && position.Node.GetNextNode() is TextTreeTextNode && position.Node.GetNextNode().SymbolCount > 0));
                }
            }

            BeforeAddChange();

            // During document load we won't have listeners and we can save
            // an allocation on every insert.  This can easily save 1000's of allocations during boot.
            originalPosition = this.HasListeners ? new TextPointer(position, LogicalDirection.Backward) : null;

            // Find a bordering TextTreeTextNode, if any.

            // We know position already points to the current TextNode, if there is one, so
            // we can't append text to that node (it would disrespect position's gravity to do so).
            // So we either have to find a neighboring text node with no position references, or
            // create a new node.

            // Look for a bordering text node.
            if (position.Edge == ElementEdge.BeforeStart || position.Edge == ElementEdge.BeforeEnd)
            {
                direction = LogicalDirection.Backward;
            }
            else
            {
                direction = LogicalDirection.Forward;
            }
            textNode = position.GetAdjacentTextNodeSibling(direction);

            if (textNode != null)
            {
                // We can't use a text node that is already referred to by text positions.
                // Doing so could displace the positions, since they expect to remain at
                // the node edge no matter what happens.
                if ((direction == LogicalDirection.Backward && textNode.AfterEndReferenceCount) ||
                    (direction == LogicalDirection.Forward && textNode.BeforeStartReferenceCount))
                {
                    textNode = null;
                }
            }

            if (textNode == null)
            {
                // No text node available.  Create and insert one.
                textNode = new TextTreeTextNode();
                textNode.InsertAtPosition(position);
                containingNode = textNode.GetContainingNode();
            }
            else
            {
                // We didn't insert a new node, so splay textNode to the root so
                // we don't invalidate any LeftSymbolCounts of ancestor nodes.
                textNode.Splay();
                containingNode = textNode.ParentNode;
            }

            // Update the symbol counts.
            textNode.SymbolCount += textLength; // This simultaneously updates textNode.IMECharCount.
            UpdateContainerSymbolCount(containingNode, /* symbolCount */ textLength, /* charCount */ textLength);

            // Insert the raw text.
            symbolOffset = textNode.GetSymbolOffset(this.Generation);
            TextTreeText.InsertText(_rootNode.RootTextBlock, symbolOffset, text);

            // Handle undo.
            TextTreeUndo.CreateInsertUndoUnit(this, symbolOffset, textLength);

            // Announce the change.
            NextGeneration(false /* deletedContent */);

            AddChange(originalPosition, /* symbolCount */ textLength, /* charCount */ textLength, PrecursorTextChangeType.ContentAdded);

            // Notify the TextElement of a content change.
            TextElement textElement = position.Parent as TextElement;
            if (textElement != null)
            {
                textElement.OnTextUpdated();
            }
        }

        // InsertElement worker.  Adds a TextElement to the tree.
        // If element is already in a tree, we remove it, do a deep copy of its content,
        // and insert that too.
        internal void InsertElementInternal(TextPointer startPosition, TextPointer endPosition, TextElement element)
        {
            TextTreeTextElementNode elementNode;
            int symbolOffset;
            int childSymbolCount;
            TextPointer startEdgePosition;
            TextPointer endEdgePosition;
            char[] elementText;
            ExtractChangeEventArgs extractChangeEventArgs;
            IAvaloniaObject parentLogicalNode;
            bool newElementNode;
            int deltaCharCount;

            Invariant.Assert(!this.PlainTextOnly);
            Invariant.Assert(startPosition.TextContainer == this);
            Invariant.Assert(endPosition.TextContainer == this);

            DemandCreateText();

            startPosition.SyncToTreeGeneration();
            endPosition.SyncToTreeGeneration();

            bool scopesExistingContent = startPosition.CompareTo(endPosition) != 0;

            BeforeAddChange();

            // Remove element from any previous tree.
            // When called from a public method we already checked all the
            // illegal cases in CanInsertElementInternal.
            if (element.TextElementNode != null)
            {
                // This element is already in a tree.  Remove it!

                bool sameTextContainer = (this == element.TextContainer);

                if (!sameTextContainer)
                {
                    // This is a cross-tree extract.
                    // We need to start a change block now, so that we can
                    // raise a Changing event inside ExtractElementInternal
                    // before raising the LogicalTree events below.
                    // We'll make an EndChange call to wrap up below.
                    element.TextContainer.BeginChange();
                }

                bool exceptionThrown = true;
                try
                {
                    // ExtractElementInternal will raise LogicalTree events which
                    // could raise exceptions from external code.
                    elementText = element.TextContainer.ExtractElementInternal(element, true /* deep */, out extractChangeEventArgs);
                    exceptionThrown = false;
                }
                finally
                {
                    if (exceptionThrown && !sameTextContainer)
                    {
                        // If an exception is thrown, make sure we close the
                        // change block we opened above before unwinding.
                        element.TextContainer.EndChange();
                    }
                }

                elementNode = element.TextElementNode;
                deltaCharCount = extractChangeEventArgs.ChildIMECharCount;

                if (sameTextContainer)
                {
                    // Re-sync the TextPointers in case we just extracted from this tree.
                    startPosition.SyncToTreeGeneration();
                    endPosition.SyncToTreeGeneration();

                    // We must add the extract change now, before we move on to the insert.
                    // (When !sameTextContainer we want to delay the notification in the extract
                    // tree until the insert tree is in an accessible state, ie at the end of this method.)
                    extractChangeEventArgs.AddChange();

                    // Don't re-raise the change below.
                    extractChangeEventArgs = null;
                }

                newElementNode = false;
            }
            else
            {
                // Allocate a node in the tree to hold the element.
                elementText = null;
                elementNode = new TextTreeTextElementNode();
                deltaCharCount = 0;
                newElementNode = true;
                extractChangeEventArgs = null;
            }

            parentLogicalNode = startPosition.GetLogicalTreeNode();

            // Invalidate any TextElementCollection that depends on the parent.
            // Make sure we do that before raising any public events.
            TextElementCollectionHelper.MarkDirty(parentLogicalNode);

            // Link the TextElement to the TextElementNode.
            if (newElementNode)
            {
                elementNode.TextElement = element;
                element.TextElementNode = (TextTreeTextElementNode)elementNode;
            }

            // If the new element will become the parent of an old element,
            // the old element may become a firstIMEVisibleSibling.
            TextTreeTextElementNode newFirstIMEVisibleNode = null;
            int newFirstIMEVisibleNodeCharDelta = 0;
            if (scopesExistingContent)
            {
                newFirstIMEVisibleNode = startPosition.GetAdjacentTextElementNodeSibling(LogicalDirection.Forward);
                if (newFirstIMEVisibleNode != null)
                {
                    newFirstIMEVisibleNodeCharDelta = -newFirstIMEVisibleNode.IMELeftEdgeCharCount;
                    newFirstIMEVisibleNode.IMECharCount += newFirstIMEVisibleNodeCharDelta;
                }
            }

            // Attach the element node.
            childSymbolCount = InsertElementToSiblingTree(startPosition, endPosition, elementNode);

            // Add the edge char count to our delta.  We couldn't get this before
            // because it depends on the position of the element in the tree.
            deltaCharCount += elementNode.IMELeftEdgeCharCount;

            TextTreeTextElementNode formerFirstIMEVisibleNode = null;
            int formerFirstIMEVisibleNodeCharDelta = 0;
            if (element.IsFirstIMEVisibleSibling && !scopesExistingContent)
            {
                formerFirstIMEVisibleNode = (TextTreeTextElementNode)elementNode.GetNextNode();
                if (formerFirstIMEVisibleNode != null)
                {
                    // The following node was the former first ime visible sibling.
                    // It just moved, and gains an edge character.
                    formerFirstIMEVisibleNodeCharDelta = formerFirstIMEVisibleNode.IMELeftEdgeCharCount;
                    formerFirstIMEVisibleNode.IMECharCount += formerFirstIMEVisibleNodeCharDelta;
                }
            }

            // Ancester nodes gain the two edge symbols.
            UpdateContainerSymbolCount(elementNode.GetContainingNode(), /* symbolCount */ elementText == null ? 2 : elementText.Length, deltaCharCount + formerFirstIMEVisibleNodeCharDelta + newFirstIMEVisibleNodeCharDelta);

            symbolOffset = elementNode.GetSymbolOffset(this.Generation);

            if (newElementNode)
            {
                // Insert text to account for the element edges.
                TextTreeText.InsertElementEdges(_rootNode.RootTextBlock, symbolOffset, childSymbolCount);
            }
            else
            {
                // element already has an existing child, just copy over the corresponding text.
                TextTreeText.InsertText(_rootNode.RootTextBlock, symbolOffset, elementText);
            }

            NextGeneration(false /* deletedContent */);

            // Handle undo.
            TextTreeUndo.CreateInsertElementUndoUnit(this, symbolOffset, elementText != null /* deep */);

            // If we extracted the TextElement from another tree, raise that event now.
            // We can't raise this event any earlier, because prior to now _this_ tree
            // is in an invalid state and this tree could be referenced by a listener
            // to changes on the other tree.
            if (extractChangeEventArgs != null)
            {
                // Announce the extract from the old tree.
                // NB: we already Removed the element from the original logical tree with LogicalTreeHelper,
                // and did a BeginChange above.
                extractChangeEventArgs.AddChange();
                extractChangeEventArgs.TextContainer.EndChange();
            }

            // Raise the public event for the insert into this tree.
            // During document load we won't have listeners and we can save
            // an allocation on every insert.  This can easily save 1000's of allocations during boot.
            if (this.HasListeners)
            {
                // REVIEW:benwest: this is a great place to use StaticTextPointer to eliminate allocations.
                startEdgePosition = new TextPointer(this, elementNode, ElementEdge.BeforeStart);

                if (childSymbolCount == 0 || elementText != null)
                {
                    AddChange(startEdgePosition, elementText == null ? 2 : elementText.Length, deltaCharCount, PrecursorTextChangeType.ContentAdded);
                }
                else
                {
                    endEdgePosition = new TextPointer(this, elementNode, ElementEdge.BeforeEnd);

                    AddChange(startEdgePosition, endEdgePosition, elementNode.SymbolCount,
                              elementNode.IMELeftEdgeCharCount, elementNode.IMECharCount - elementNode.IMELeftEdgeCharCount,
                              PrecursorTextChangeType.ElementAdded, null, false);
                }

                if (formerFirstIMEVisibleNodeCharDelta != 0)
                {
                    RaiseEventForFormerFirstIMEVisibleNode(formerFirstIMEVisibleNode);
                }

                if (newFirstIMEVisibleNodeCharDelta != 0)
                {
                    RaiseEventForNewFirstIMEVisibleNode(newFirstIMEVisibleNode);
                }
            }

            // Insert the element into a Framework logical tree
            element.BeforeLogicalTreeChange();
            try
            {
                LogicalTreeHelper.AddLogicalChild(parentLogicalNode, element);
            }
            finally
            {
                element.AfterLogicalTreeChange();
            }

            // Reparent all children.
            // We only need to do this if we created a new element node.
            if (newElementNode)
            {
                ReparentLogicalChildren(elementNode, elementNode.TextElement, parentLogicalNode /* oldParent */);
            }

            // Notify the TextElement of a content change if it was moved to parent new content. This 
            // can happen when Runs get merged.
            if (scopesExistingContent)
            {
                element.OnTextUpdated();
            }
        }


        // InsertEmbeddedObject worker.  Adds a UIElement to the tree.
        internal void InsertEmbeddedObjectInternal(TextPointer position, IAvaloniaObject embeddedObject)
        {
            TextTreeNode objectNode;
            int symbolOffset;
            IAvaloniaObject parentLogicalNode;
            TextPointer insertPosition;

            Invariant.Assert(!this.PlainTextOnly);

            DemandCreateText();

            position.SyncToTreeGeneration();

            BeforeAddChange();

            parentLogicalNode = position.GetLogicalTreeNode();

            // Insert a node.
            objectNode = new TextTreeObjectNode(embeddedObject);
            objectNode.InsertAtPosition(position);

            // Update the symbol count.
            UpdateContainerSymbolCount(objectNode.GetContainingNode(), objectNode.SymbolCount, objectNode.IMECharCount);

            // Insert the corresponding text.
            symbolOffset = objectNode.GetSymbolOffset(this.Generation);
            TextTreeText.InsertObject(_rootNode.RootTextBlock, symbolOffset);

            NextGeneration(false /* deletedContent */);

            // Handle undo.
            TextTreeUndo.CreateInsertUndoUnit(this, symbolOffset, 1);

            // Tell parent to update Logical Tree
            LogicalTreeHelper.AddLogicalChild(parentLogicalNode, embeddedObject);

            // Raise the public event.
            // During document load we won't have listeners and we can save
            // an allocation on every insert.  This can easily save 1000's of allocations during boot.
            if (this.HasListeners)
            {
                insertPosition = new TextPointer(this, objectNode, ElementEdge.BeforeStart);
                AddChange(insertPosition, 1, 1, PrecursorTextChangeType.ContentAdded);
            }
        }

        // DeleteContent worker.  Removes content from the tree.
        internal void DeleteContentInternal(TextPointer startPosition, TextPointer endPosition)
        {
            TextTreeNode containingNode;
            int symbolCount;
            int charCount;
            TextTreeUndoUnit undoUnit;
            TextPointer deletePosition;

            startPosition.SyncToTreeGeneration();
            endPosition.SyncToTreeGeneration();

            if (startPosition.CompareTo(endPosition) == 0)
                return;

            BeforeAddChange();

            undoUnit = TextTreeUndo.CreateDeleteContentUndoUnit(this, startPosition, endPosition);

            containingNode = startPosition.GetScopingNode();

            // Invalidate any TextElementCollection that depends on the parent.
            // Make sure we do that before raising any public events.
            TextElementCollectionHelper.MarkDirty(containingNode.GetLogicalTreeNode());

            int nextIMEVisibleNodeCharDelta = 0;
            TextTreeTextElementNode nextIMEVisibleNode = GetNextIMEVisibleNode(startPosition, endPosition);
            if (nextIMEVisibleNode != null)
            {
                // The node following the delete just became the first sibling.
                // This might affect its ime char count.
                nextIMEVisibleNodeCharDelta = -nextIMEVisibleNode.IMELeftEdgeCharCount;
                nextIMEVisibleNode.IMECharCount += nextIMEVisibleNodeCharDelta;
            }

            // First cut: remove all top-level TextElements and their chilren.
            // We need to put each TextElement in its own tree, so that any outside
            // references can still play with the TextElements safely.
            symbolCount = CutTopLevelLogicalNodes(containingNode, startPosition, endPosition, out charCount);

            // Cut what's left.
            int remainingCharCount;
            symbolCount += DeleteContentFromSiblingTree(containingNode, startPosition, endPosition, nextIMEVisibleNodeCharDelta != 0, out remainingCharCount);
            charCount += remainingCharCount;

            Invariant.Assert(symbolCount > 0);

            if (undoUnit != null)
            {
                undoUnit.SetTreeHashCode();
            }

            // Public tree event.
            deletePosition = new TextPointer(startPosition, LogicalDirection.Forward);
            AddChange(deletePosition, symbolCount, charCount, PrecursorTextChangeType.ContentRemoved);

            if (nextIMEVisibleNodeCharDelta != 0)
            {
                RaiseEventForNewFirstIMEVisibleNode(nextIMEVisibleNode);
            }
        }

        internal void GetNodeAndEdgeAtOffset(int offset, out SplayTreeNode node, out ElementEdge edge)
        {
            GetNodeAndEdgeAtOffset(offset, true /* splitNode */, out node, out edge);
        }

        // Finds a node/edge pair matching a given symbol offset in the tree.
        // If the pair matches a character within a text node, the text node is split.
        internal void GetNodeAndEdgeAtOffset(int offset, bool splitNode, out SplayTreeNode node, out ElementEdge edge)
        {
            int nodeOffset;
            int siblingTreeOffset;
            bool checkZeroWidthNode;

            // Offset zero/SymbolCount-1 are before/after the root node, which
            // is an illegal position -- you can't add or remove content there
            // and it's never exposed publicly.
            Invariant.Assert(offset >= 1 && offset <= this.InternalSymbolCount - 1, "Bogus symbol offset!");

            // If this flag is set true on exit, we need to consider the case
            // where we've found a "zero-width" (SymbolCount == 0) text node.
            // Zero width nodes needs special handling, since they are logically
            // part of a following or preceding node.
            checkZeroWidthNode = false;

            // Find the node.
            node = _rootNode;
            nodeOffset = 0;

            // Each iteration walks through one tree.
            while (true)
            {
                // While we're at it, fix up the node's SymbolOffsetCache,
                // since we're doing the work already.
                Invariant.Assert(node.Generation != _rootNode.Generation ||
                             node.SymbolOffsetCache == -1 ||
                             node.SymbolOffsetCache == nodeOffset, "Bad node offset cache!");

                node.Generation = _rootNode.Generation;
                node.SymbolOffsetCache = nodeOffset;

                if (offset == nodeOffset)
                {
                    edge = ElementEdge.BeforeStart;
                    checkZeroWidthNode = true;
                    break;
                }
                if (node is TextTreeRootNode || node is TextTreeTextElementNode)
                {
                    if (offset == nodeOffset + 1)
                    {
                        edge = ElementEdge.AfterStart;
                        break;
                    }
                    if (offset == nodeOffset + node.SymbolCount - 1)
                    {
                        edge = ElementEdge.BeforeEnd;
                        break;
                    }
                }
                if (offset == nodeOffset + node.SymbolCount)
                {
                    edge = ElementEdge.AfterEnd;
                    checkZeroWidthNode = true;
                    break;
                }

                // No child node?  That means we're inside a TextTreeTextNode.
                if (node.ContainedNode == null)
                {
                    Invariant.Assert(node is TextTreeTextNode);
                    // Need to split the TextTreeTextNode.
                    // Here we want a character buried inside a single node, split
                    // the node open....
                    if (splitNode)
                    {
                        node = ((TextTreeTextNode)node).Split(offset - nodeOffset, ElementEdge.AfterEnd);
                    }
                    edge = ElementEdge.BeforeStart;
                    break;
                }

                // Need to look into one of the child nodes.
                node = node.ContainedNode;
                nodeOffset += 1; // Skip over the parent element start edge.

                // Walk down the sibling tree.
                node = node.GetSiblingAtOffset(offset - nodeOffset, out siblingTreeOffset);
                nodeOffset += siblingTreeOffset;
            }

            // If we're on a zero-width TextTreeTextNode we need some special handling.
            if (checkZeroWidthNode)
            {
                node = AdjustForZeroWidthNode(node, edge);
            }
        }

        // Finds a node/edge pair matching a given char offset in the tree.
        // If the pair matches a character within a text node, the text node is split.
        internal void GetNodeAndEdgeAtCharOffset(int charOffset, out TextTreeNode node, out ElementEdge edge)
        {
            int nodeCharOffset;
            int siblingTreeCharOffset;
            bool checkZeroWidthNode;

            // Offset zero/SymbolCount-1 are before/after the root node, which
            // is an illegal position -- you can't add or remove content there
            // and it's never exposed publicly.
            Invariant.Assert(charOffset >= 0 && charOffset <= this.IMECharCount, "Bogus char offset!");

            if (this.IMECharCount == 0)
            {
                node = null;
                edge = ElementEdge.BeforeStart;
                return;
            }

            // If this flag is set true on exit, we need to consider the case
            // where we've found a "zero-width" (SymbolCount == 0) text node.
            // Zero width nodes needs special handling, since they are logically
            // part of a following or preceding node.
            checkZeroWidthNode = false;

            // Find the node.
            node = _rootNode;
            nodeCharOffset = 0;

            // Each iteration walks through one tree.
            while (true)
            {
                int leftEdgeCharCount = 0;
                TextTreeTextElementNode textElementNode = node as TextTreeTextElementNode;

                if (textElementNode != null)
                {
                    leftEdgeCharCount = textElementNode.IMELeftEdgeCharCount;
                    if (leftEdgeCharCount > 0)
                    {
                        if (charOffset == nodeCharOffset)
                        {
                            edge = ElementEdge.BeforeStart;
                            break;
                        }
                        if (charOffset == nodeCharOffset + leftEdgeCharCount)
                        {
                            edge = ElementEdge.AfterStart;
                            break;
                        }
                    }
                }
                else if (node is TextTreeTextNode || node is TextTreeObjectNode)
                {
                    if (charOffset == nodeCharOffset)
                    {
                        edge = ElementEdge.BeforeStart;
                        checkZeroWidthNode = true;
                        break;
                    }
                    if (charOffset == nodeCharOffset + node.IMECharCount)
                    {
                        edge = ElementEdge.AfterEnd;
                        checkZeroWidthNode = true;
                        break;
                    }
                }

                // No child node?  That means we're inside a TextTreeTextNode.
                if (node.ContainedNode == null)
                {
                    Invariant.Assert(node is TextTreeTextNode);
                    // Need to split the TextTreeTextNode.
                    // Here we want a character buried inside a single node, split
                    // the node open....
                    node = ((TextTreeTextNode)node).Split(charOffset - nodeCharOffset, ElementEdge.AfterEnd);
                    edge = ElementEdge.BeforeStart;
                    break;
                }

                // Need to look into one of the child nodes.
                node = (TextTreeNode)node.ContainedNode;
                nodeCharOffset += leftEdgeCharCount; // Skip over the parent element start edge.

                // Walk down the sibling tree.
                node = (TextTreeNode)node.GetSiblingAtCharOffset(charOffset - nodeCharOffset, out siblingTreeCharOffset);
                nodeCharOffset += siblingTreeCharOffset;
            }

            // If we're on a zero-width TextTreeTextNode we need some special handling.
            if (checkZeroWidthNode)
            {
                node = (TextTreeNode)AdjustForZeroWidthNode(node, edge);
            }
        }

        // This method checks for any finalized positions and, if found, decrements
        // their nodes' reference counts before finally releasing the positions
        // into the void.  See TextPointer for an explanation
        // of the entire process.
        //
        // Called from all public entry points.
        internal void EmptyDeadPositionList()
        {
#if REFCOUNT_DEAD_TEXTPOINTERS
            TextPointer[] localList;
            TextPointer position;
            ArrayList deadPositionList;
            int i;

            if (_rootNode == null)
                return; // No TextPositions allocated yet.

            deadPositionList = _rootNode.DeadPositionList;
            localList = null;

            // We need to lock deadPositionList before accessing it because
            // it is also referenced by the finalizer thread.
            // We hold it just long enough to make a copy and clear it.
            //
            // !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
            // NB: This lock will occasionally cause reentrancy.
            // !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
            // See PS Task Item 14229: there's a general problem where any CLR
            // lock will rev up a message pump if blocked.  The Core team is
            // expecting a CLR drop in 3.3 that will enable us to change this
            // behavior, within Avalon. benwest:3/31/2004.
            lock (deadPositionList)
            {
                if (deadPositionList.Count > 0)
                {
                    localList = new TextPointer[deadPositionList.Count];
                    deadPositionList.CopyTo(localList);
                    deadPositionList.Clear();
                }
            }

            // Now go through the thread safe copy and decrement reference
            // counts.  Ultimately this will merge text nodes that have been
            // split by positions.
            if (localList != null)
            {
                for (i = 0; i < localList.Length; i++)
                {
                    position = localList[i];
                    position.SyncToTreeGeneration();
                    position.Node.DecrementReferenceCount(position.Edge);
                }
            }
#endif // REFCOUNT_DEAD_TEXTPOINTERS
        }

        // Returns the lenth of a text object.  Text objects are always either
        // strings or char arrays.
        internal static int GetTextLength(object text)
        {
            string textString;
            int length;

            Invariant.Assert(text is string || text is char[], "Bad text parameter!");

            textString = text as string;
            if (textString != null)
            {
                length = textString.Length;
            }
            else
            {
                length = ((char[])text).Length;
            }

            return length;
        }

        internal void AssertTree()
        {
#if DEBUG_SLOW
            if (_rootNode != null && _rootNode.ContainedNode != null)
            {
                AssertTreeRecursive(_rootNode);
            }
#endif // DEBUG_SLOW
        }

        // Returns a hash code identifying the current content state.
        // Used to flag errors in the undo code.
        internal int GetContentHashCode()
        {
            return this.InternalSymbolCount;
        }

        // Increments the tree's layout generation counter.
        // This happens whenever a layout related property value
        // changes on a TextElement.
        internal void NextLayoutGeneration()
        {
            _rootNode.LayoutGeneration++;
        }

        // Removes a TextElement from the tree.
        // Any TextElement content is left in the tree.
        internal void ExtractElementInternal(TextElement element)
        {
            ExtractChangeEventArgs extractChangeEventArgs;

            ExtractElementInternal(element, false /* deep */, out extractChangeEventArgs);
        }

        // Wrapper for this.TextView.IsAtCaretUnitBoundary, adds a cache.
        internal bool IsAtCaretUnitBoundary(TextPointer position)
        {
            position.DebugAssertGeneration();
            Invariant.Assert(position.HasValidLayout);

            if (_rootNode.CaretUnitBoundaryCacheOffset != position.GetSymbolOffset())
            {
                _rootNode.CaretUnitBoundaryCacheOffset = position.GetSymbolOffset();
                _rootNode.CaretUnitBoundaryCache = _textview.IsAtCaretUnitBoundary(position);

                if (!_rootNode.CaretUnitBoundaryCache && position.LogicalDirection == LogicalDirection.Backward)
                {
                    // In MIL Text and TextView worlds, a position at trailing edge of a newline (with backward gravity)
                    // is not an allowed caret stop. 
                    // However, in TextPointer world we must allow such a position to be a valid insertion position,
                    // since it breaks textrange normalization for empty ranges.
                    // Hence, we need to check for TextView.IsAtCaretUnitBoundary in reverse direction here.

                    TextPointer positionForwardGravity = position.GetPositionAtOffset(0, LogicalDirection.Forward);
                    _rootNode.CaretUnitBoundaryCache = _textview.IsAtCaretUnitBoundary(positionForwardGravity);
                }
            }

            return _rootNode.CaretUnitBoundaryCache;
        }

        #endregion Internal Methods        

        //------------------------------------------------------
        //
        //  Internal Properties
        //
        //------------------------------------------------------

        #region Internal Properties

        /// <summary>
        /// A position preceding the first symbol of this TextContainer.
        /// </summary>
        /// <remarks>
        /// The returned TextPointer has LogicalDirection.Backward gravity.
        /// </remarks>
        internal TextPointer Start
        {
            get
            {
                TextPointer startPosition;

//                 VerifyAccess();

                EmptyDeadPositionList();
                DemandCreatePositionState();

                startPosition = new TextPointer(this, _rootNode, ElementEdge.AfterStart, LogicalDirection.Backward);
                startPosition.Freeze();

#if REFCOUNT_DEAD_TEXTPOINTERS
                // Since start/end position are always on the root node, and the
                // root node can never be removed, we don't need to ref count it
                // and therefore we don't need to run the TextPointer finalizer.
                GC.SuppressFinalize(startPosition);
#endif // REFCOUNT_DEAD_TEXTPOINTERS

                return startPosition;
            }
        }

        /// <summary>
        /// A position following the last symbol of this TextContainer.
        /// </summary>
        /// <remarks>
        /// The returned TextPointer has LogicalDirection.Forward gravity.
        /// </remarks>
        internal TextPointer End
        {
            get
            {
                TextPointer endPosition;

//                 VerifyAccess();

                EmptyDeadPositionList();
                DemandCreatePositionState();

                endPosition = new TextPointer(this, _rootNode, ElementEdge.BeforeEnd, LogicalDirection.Forward);
                endPosition.Freeze();

#if REFCOUNT_DEAD_TEXTPOINTERS
                // Since start/end position are always on the root node, and the
                // root node can never be removed, we don't need to ref count it
                // and therefore we don't need to run the TextPointer finalizer.
                GC.SuppressFinalize(endPosition);
#endif // REFCOUNT_DEAD_TEXTPOINTERS

                return endPosition;
            }
        }

        /// <summary>
        /// An object from which property values are inherited.
        /// </summary>
        /// <remarks>
        /// May be null.
        /// </remarks>
        internal IAvaloniaObject Parent
        {
            get
            {
//                 VerifyAccess();

                return _parent;
            }
        }

        bool ITextContainer.IsReadOnly
        {
            get
            {
                return CheckFlags(Flags.ReadOnly);
            }
        }

        ITextPointer ITextContainer.Start
        {
            get
            {
                return this.Start;
            }
        }

        ITextPointer ITextContainer.End
        {
            get
            {
                return this.End;
            }
        }

        // The tree generation.  Incremented whenever the tree content changes.
        // Use NextGeneration to increment the generation.
        uint ITextContainer.Generation
        {
            get
            {
                return this.Generation;
            }
        }

        Highlights ITextContainer.Highlights
        {
            get
            {
                return this.Highlights;
            }
        }

        IAvaloniaObject ITextContainer.Parent
        {
            get
            {
                return this.Parent;
            }
        }

        // TextEditor owns setting and clearing this property inside its
        // ctor/OnDetach methods.
        ITextSelection ITextContainer.TextSelection
        {
            get
            {
                return this.TextSelection;
            }

            set
            {
                _textSelection = value;
            }
        }

        // Optional undo manager, may be null.
        UndoManager ITextContainer.UndoManager
        {
            get
            {
                return this.UndoManager;
            }
        }

        // <see cref="System.Windows.Documents.ITextContainer/>
        ITextView ITextContainer.TextView
        {
            get
            {
                return this.TextView;
            }

            set
            {
                this.TextView = value;
            }
        }

        // <see cref="System.Windows.Documents.ITextContainer/>
        internal ITextView TextView
        {
            get
            {
                return _textview;
            }

            set
            {
                _textview = value;
            }
        }

        // Count of symbols in this tree, equivalent to this.Start.GetOffsetToPosition(this.End),
        // but doesn't necessarily allocate anything.
        int ITextContainer.SymbolCount
        {
            get
            {
                return this.SymbolCount;
            }
        }

        // Count of symbols in this tree, equivalent to this.Start.GetOffsetToPosition(this.End),
        // but doesn't necessarily allocate anything.
        internal int SymbolCount
        {
            get
            {
                // ITextContainer symbol count does not include the root node,
                // so subtract 2 symbols.
                return this.InternalSymbolCount - 2;
            }
        }

        // The symbol count for the entire tree.  This includes the private
        // root node, which has 2 symbols, one for each edge.
        internal int InternalSymbolCount
        {
            get
            {
                return (_rootNode == null ? 2 : _rootNode.SymbolCount);
            }
        }

        // Character count for the entire tree.
        internal int IMECharCount
        {
            get
            {
                return (_rootNode == null ? 0 : _rootNode.IMECharCount);
            }
        }

        // Character count for the entire tree.
        int ITextContainer.IMECharCount
        {
            get
            {
                return this.IMECharCount;
            }
        }

        // A tree of TextTreeTextBlocks, used to store raw text.
        // Callers must use DemandCreateText before accessing this property.
        internal TextTreeRootTextBlock RootTextBlock
        {
            get
            {
                Invariant.Assert(_rootNode != null, "Asking for TextBlocks before root node create!");
                return _rootNode.RootTextBlock;
            }
        }

        // The tree generation.  Incremented whenever the tree content changes.
        // Use NextGeneration to increment the generation.
        internal uint Generation
        {
            get
            {
                // It shouldn't be possible to modify the tree before allocating
                // a TextPointer...which creates the root node.
                Invariant.Assert(_rootNode != null, "Asking for Generation before root node create!");
                return _rootNode.Generation;
            }
        }

        // Like Generation, but only updated when a change could affect positions.
        // Positions only need to synchronize after deletes, inserts are harmless.
        internal uint PositionGeneration
        {
            get
            {
                Invariant.Assert(_rootNode != null, "Asking for PositionGeneration before root node create!");
                return _rootNode.PositionGeneration;
            }
        }

        // Like Generation, but incremented on each layout related property change
        // to a TextElement in the tree.
        internal uint LayoutGeneration
        {
            get
            {
                Invariant.Assert(_rootNode != null, "Asking for LayoutGeneration before root node create!");
                return _rootNode.LayoutGeneration;
            }
        }

#if REFCOUNT_DEAD_TEXTPOINTERS
        // A list of positions ready to be garbage collected.  The TextPointer
        // finalizer adds positions to this list.
        internal ArrayList DeadPositionList
        {
            get
            {
                // It shouldn't be possible to get here before allocating a position,
                // which also allocates the root node.
                Invariant.Assert(_rootNode != null, "Asking for DeadPositionList before root node create!");
                return _rootNode.DeadPositionList;
            }
        }
#endif // REFCOUNT_DEAD_TEXTPOINTERS

        // Collection of highlights applied to TextContainer content.
        internal Highlights Highlights
        {
            get
            {
                if (_highlights == null)
                {
                    _highlights = new Highlights(this);
                }

                return _highlights;
            }
        }

        // The root node -- contains all content.
        internal TextTreeRootNode RootNode
        {
            get { return _rootNode; }
        }

        // The first node contained by the root node -- first toplevel node.
        internal TextTreeNode FirstContainedNode
        {
            get
            {
                return (_rootNode == null) ? null : (TextTreeNode)_rootNode.GetFirstContainedNode();
            }
        }

        // The last node contained by the root node -- last toplevel node.
        internal TextTreeNode LastContainedNode
        {
            get
            {
                return (_rootNode == null) ? null : (TextTreeNode)_rootNode.GetLastContainedNode();
            }
        }

        // Undo manager associated with this TextContainer.
        // May be null.
        internal UndoManager UndoManager
        { 
            get
            { 
                return _undoManager;
            }
        }

        // TextSelection associated with this container.
        internal ITextSelection TextSelection
        {
            get
            {
                return _textSelection;
            }
        }

        // Returns true if anyone is listening to the Changing, Change or Changed events.
        // Notably, this property usually returns false during document load.
        internal bool HasListeners
        {
            get
            {
                return (this.ChangingHandler != null || this.ChangeHandler != null || this.ChangedHandler != null);
            }
        }

        // Set in the ctor.  If true, only plain text may be inserted
        // into the TextContainer and perf optimizations are enabled.
        internal bool PlainTextOnly
        {
            get
            {
                return CheckFlags(Flags.PlainTextOnly);
            }
        }

        // If TRUE, text changes will be collected in TextContainerChangedEventArgs.Changes when
        // TextContainerChangedEventArgs.AddChange() is called.
        internal bool CollectTextChanges
        {
            get
            {
                return CheckFlags(Flags.CollectTextChanges);
            }
            set
            {
                SetFlags(value, Flags.CollectTextChanges);
            }
        }

        
        #endregion Internal Properties

        //------------------------------------------------------
        //
        //  Internal Events
        //
        //------------------------------------------------------

        #region Internal Events

        event EventHandler ITextContainer.Changing
        {
            add { Changing += value; }
            remove { Changing -= value; }
        }

        event TextContainerChangeEventHandler ITextContainer.Change
        {
            add { Change += value; }
            remove { Change -= value; }
        }

        event TextContainerChangedEventHandler ITextContainer.Changed
        {
            add { Changed += value; }
            remove { Changed -= value; }
        }

        internal event EventHandler Changing
        {
            add { ChangingHandler += value; }
            remove { ChangingHandler -= value; }
        }

        internal event TextContainerChangeEventHandler Change
        {
            add { ChangeHandler += value; }
            remove { ChangeHandler -= value; }
        }

        internal event TextContainerChangedEventHandler Changed
        {
            add { ChangedHandler += value; }
            remove { ChangedHandler -= value; }
        }

        #endregion Internal Events

        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------

        #region Private Methods

        // Scans all the immediate contained nodes of a TextElementNode, and
        // calls AddLogicalChild methods if supported to alert the children
        // about a new parent.
        private void ReparentLogicalChildren(SplayTreeNode containerNode, IAvaloniaObject newParentLogicalNode, IAvaloniaObject oldParentLogicalNode)
        {
            ReparentLogicalChildren(containerNode.GetFirstContainedNode(), null, newParentLogicalNode, oldParentLogicalNode);
        }

        // Scans all the immediate contained nodes of a TextElementNode, and
        // calls AddLogicalChild methods if supported to alert the children
        // about a new parent.
        private void ReparentLogicalChildren(SplayTreeNode firstChildNode, SplayTreeNode lastChildNode, IAvaloniaObject newParentLogicalNode, IAvaloniaObject oldParentLogicalNode)
        {
            SplayTreeNode node;
            IAvaloniaObject logicalTreeNode;
            TextTreeTextElementNode elementNode;
            TextTreeObjectNode uiElementNode;

            Invariant.Assert(!(newParentLogicalNode == null && oldParentLogicalNode == null), "Both new and old parents should not be null");

            for (node = firstChildNode; node != null; node = node.GetNextNode())
            {
                logicalTreeNode = null;

                elementNode = node as TextTreeTextElementNode;
                if (elementNode != null)
                {
                    logicalTreeNode = elementNode.TextElement;
                }
                else
                {
                    uiElementNode = node as TextTreeObjectNode;
                    if (uiElementNode != null)
                    {
                        logicalTreeNode = uiElementNode.EmbeddedElement;
                    }
                }

                TextElement textElement = logicalTreeNode as TextElement;
                if (textElement != null)
                {
                    textElement.BeforeLogicalTreeChange();
                }

                try
                {
                    if (oldParentLogicalNode != null)
                    {
                        LogicalTreeHelper.RemoveLogicalChild(oldParentLogicalNode, logicalTreeNode);
                    }

                    if (newParentLogicalNode != null)
                    {
                        LogicalTreeHelper.AddLogicalChild(newParentLogicalNode, logicalTreeNode);
                    }
                }
                finally
                {
                    if (textElement != null)
                    {
                        textElement.AfterLogicalTreeChange();
                    }
                }

                if (node == lastChildNode)
                    break;
            }
        }

        // This method is called by GetNodeAndEdgeAtOffset to adjust a node
        // matching a symbol offset.
        //
        // When text positions reference both sides of a text run
        // we always split the run into a zero-width TextTreeTextNode
        // followed by a non-zero-width node.  We do this so that
        // later we can safely split the non-zero node without disturbing
        // existing text positions.
        //
        // Here, we have to be very careful never to reference a node
        // edge between the zero-width node and the non-zero width node.
        // 
        // A: <TextTreeTextNode SymbolCount=0/><TextTreeTextNode SymbolCount=1+/>
        // B: <TextTreeTextNode SymbolCount=1+/><TextTreeTextNode SymbolCount=0/>
        //
        // In case A, if we're searching for the first character in the second
        // node, we want to return the first (zero-width) node.  A TextPointer
        // could be at node1/BeforeBegin but node2/BeforeBegin is invalid.
        //
        // In case B, if we're searching for the last character in the first
        // node, we want to return the second (zero-width) node. A TextPointer
        // could be at node2/AfterEnd but not node1/AfterEnd is invalid.
        private SplayTreeNode AdjustForZeroWidthNode(SplayTreeNode node, ElementEdge edge)
        {
            TextTreeTextNode textNode;
            SplayTreeNode nextNode;
            SplayTreeNode previousNode;

            textNode = node as TextTreeTextNode;

            if (textNode == null)
            {
                Invariant.Assert(node.SymbolCount > 0, "Only TextTreeTextNodes may have zero symbol counts!");
                return node;
            }

            if (textNode.SymbolCount == 0)
            {
                // There are only ever at most two consectuative zero-width
                // text nodes (one for each possible position gravity).  Make sure
                // we chose the following one.  This ensures consistency with
                // the typical non-zero width case -- we return the node whose
                // start edge is closest to a character offset.
                nextNode = textNode.GetNextNode();
                if (nextNode != null)
                {
                    if (Invariant.Strict)
                    {
                        if (nextNode.SymbolCount == 0)
                        {
                            // Node and previousNode are logically one text run.
                            // <TextTreeTextNode SymbolCount=1+/><node SymbolCount=0/><TextTreeTextNode SymbolCount=0/><TextTreeTextNode SymbolCount=1+/>
                            Invariant.Assert(nextNode is TextTreeTextNode);
                            Invariant.Assert(!textNode.BeforeStartReferenceCount);
                            Invariant.Assert(!((TextTreeTextNode)nextNode).AfterEndReferenceCount);
                            Invariant.Assert(textNode.GetPreviousNode() == null || textNode.GetPreviousNode().SymbolCount > 0, "Found three consecutive zero-width text nodes! (1)");
                            Invariant.Assert(nextNode.GetNextNode() == null || nextNode.GetNextNode().SymbolCount > 0, "Found three consecutive zero-width text nodes! (2)");
                        }
                    }

                    if (!textNode.BeforeStartReferenceCount)
                    {
                        // Node and previousNode are logically one text run.
                        // <TextTreeTextNode SymbolCount=1+/><node SymbolCount=0/><AnyNode/>
                        node = nextNode;
                    }
                }
            }
            else if (edge == ElementEdge.BeforeStart)
            {
                if (textNode.AfterEndReferenceCount)
                {
                    // <TextTreeTextNode SymbolCount=0/><node SymbolCount=1+/>
                    // Case A.  Check for previous zero-width node.
                    previousNode = textNode.GetPreviousNode();
                    if (previousNode != null && previousNode.SymbolCount == 0 && !((TextTreeNode)previousNode).AfterEndReferenceCount)
                    {
                        Invariant.Assert(previousNode is TextTreeTextNode);
                        node = previousNode;
                    }
                }
            }
            else // edge == ElementEdge.AfterEnd
            {
                if (textNode.BeforeStartReferenceCount)
                {
                    // B: <node SymbolCount=1+/><TextTreeTextNode SymbolCount=0/>
                    // Case B.  Check for following zero-width node.
                    nextNode = textNode.GetNextNode();
                    if (nextNode != null && nextNode.SymbolCount == 0 && !((TextTreeNode)nextNode).BeforeStartReferenceCount)
                    {
                        Invariant.Assert(nextNode is TextTreeTextNode);
                        node = nextNode;
                    }
                }
            }

            return node;
        }

        // Inserts an element node into a sibling tree.
        // Returns the symbol count of any contained nodes the elementNode covers.
        private int InsertElementToSiblingTree(TextPointer startPosition, TextPointer endPosition, TextTreeTextElementNode elementNode)
        {
            int childSymbolCount = 0;
            int childCharCount = 0;

            if (startPosition.CompareTo(endPosition) == 0)
            {
                // Simple insert, no children for elementNode.

                // Calculate childCharCount now, before we change position.
                // IMELeftEdgeCharCount depends on context.
                int childIMECharCount = elementNode.IMECharCount - elementNode.IMELeftEdgeCharCount;

                elementNode.InsertAtPosition(startPosition);
                if (elementNode.ContainedNode != null)
                {
                    childSymbolCount = elementNode.SymbolCount - 2;
                    childCharCount = childIMECharCount;
                }
            }
            else
            {
                // Complex insert, elementNode is going to have children.
                childSymbolCount = InsertElementToSiblingTreeComplex(startPosition, endPosition, elementNode, out childCharCount);
            }

            elementNode.SymbolCount = childSymbolCount + 2;
            elementNode.IMECharCount = childCharCount + elementNode.IMELeftEdgeCharCount;

            return childSymbolCount;
        }

        // Inserts an element node into a sibling tree.  The node is expected to cover existing content.
        // Returns the symbol count of all contained nodes the elementNode covers.
        private int InsertElementToSiblingTreeComplex(TextPointer startPosition, TextPointer endPosition, TextTreeTextElementNode elementNode,
            out int childCharCount)
        {
            SplayTreeNode containingNode;
            SplayTreeNode leftSubTree;
            SplayTreeNode middleSubTree;
            SplayTreeNode rightSubTree;
            int childSymbolCount;

            containingNode = startPosition.GetScopingNode();

            // Rip out all the nodes the new element node is going to contain.
            childSymbolCount = CutContent(startPosition, endPosition, out childCharCount, out leftSubTree, out middleSubTree, out rightSubTree);

            // Join left/right trees under elementNode.
            TextTreeNode.Join(elementNode, leftSubTree, rightSubTree);

            // Reparent middle tree under elementNode.
            elementNode.ContainedNode = middleSubTree;
            middleSubTree.ParentNode = elementNode;

            // Reparent the whole thing under the original container.
            containingNode.ContainedNode = elementNode;
            elementNode.ParentNode = containingNode;

            return childSymbolCount;
        }

        // Removes nodes from a sibling tree.  containingNode must scope start/end.
        // Returns the combined symbol count of all the removed nodes.
        private int DeleteContentFromSiblingTree(SplayTreeNode containingNode, TextPointer startPosition, TextPointer endPosition, bool newFirstIMEVisibleNode, out int charCount)
        {
            SplayTreeNode leftSubTree;
            SplayTreeNode middleSubTree;
            SplayTreeNode rightSubTree;
            SplayTreeNode rootNode;
            TextTreeNode previousNode;
            ElementEdge previousEdge;
            TextTreeNode nextNode;
            ElementEdge nextEdge;
            int symbolCount;
            int symbolOffset;

            // Early out in the no-op case. CutContent can't handle an empty content span.
            if (startPosition.CompareTo(endPosition) == 0)
            {
                if (newFirstIMEVisibleNode)
                {
                    UpdateContainerSymbolCount(containingNode, /* symbolCount */ 0, /* charCount */ -1);
                }
                charCount = 0;
                return 0;
            }

            // Get the symbol offset now before the CutContent call invalidates startPosition.
            symbolOffset = startPosition.GetSymbolOffset();

            // Do the cut.  middleSubTree is what we want to remove.
            symbolCount = CutContent(startPosition, endPosition, out charCount, out leftSubTree, out middleSubTree, out rightSubTree);

            // We need to remember the original previous/next node for the span
            // we're about to drop, so any orphaned positions can find their way
            // back.
            if (middleSubTree != null)
            {
                if (leftSubTree != null)
                {
                    previousNode = (TextTreeNode)leftSubTree.GetMaxSibling();
                    previousEdge = ElementEdge.AfterEnd;
                }
                else
                {
                    previousNode = (TextTreeNode)containingNode;
                    previousEdge = ElementEdge.AfterStart;
                }
                if (rightSubTree != null)
                {
                    nextNode = (TextTreeNode)rightSubTree.GetMinSibling();
                    nextEdge = ElementEdge.BeforeStart;
                }
                else
                {
                    nextNode = (TextTreeNode)containingNode;
                    nextEdge = ElementEdge.BeforeEnd;
                }

                // Increment previous/nextNode reference counts. This may involve
                // splitting a text node, so we use refs.
                AdjustRefCountsForContentDelete(ref previousNode, previousEdge, ref nextNode, nextEdge, (TextTreeNode)middleSubTree);

                // Make sure left/rightSubTree stay local roots, we might
                // have inserted new elements in the AdjustRefCountsForContentDelete call.
                if (leftSubTree != null)
                {
                    leftSubTree.Splay();
                }
                if (rightSubTree != null)
                {
                    rightSubTree.Splay();
                }
                // Similarly, middleSubtree might not be a local root any more,
                // so splay it too.
                middleSubTree.Splay();

                // Note TextContainer now has no references to middleSubTree, if there are
                // no orphaned positions this allocation won't be kept around.
                Invariant.Assert(middleSubTree.ParentNode == null, "Assigning fixup node to parented child!");
                middleSubTree.ParentNode = new TextTreeFixupNode(previousNode, previousEdge, nextNode, nextEdge);
            }

            // Put left/right sub trees back into the TextContainer.
            rootNode = TextTreeNode.Join(leftSubTree, rightSubTree);
            containingNode.ContainedNode = rootNode;
            if (rootNode != null)
            {
                rootNode.ParentNode = containingNode;
            }

            if (symbolCount > 0)
            {
                int nextNodeCharDelta = 0;
                if (newFirstIMEVisibleNode)
                {
                    // The following node is the new first ime visible sibling.
                    // It just moved, and loses an edge character.
                    nextNodeCharDelta = -1;
                }

                UpdateContainerSymbolCount(containingNode, -symbolCount, -charCount + nextNodeCharDelta);
                TextTreeText.RemoveText(_rootNode.RootTextBlock, symbolOffset, symbolCount);
                NextGeneration(true /* deletedContent */);

                // Notify the TextElement of a content change. Note that any full TextElements
                // between startPosition and endPosition will be handled by CutTopLevelLogicalNodes,
                // which will move them from this tree to their own private trees without changing
                // their contents.
                Invariant.Assert(startPosition.Parent == endPosition.Parent);
                TextElement textElement = startPosition.Parent as TextElement;
                if (textElement != null)
                {               
                    textElement.OnTextUpdated();                    
                }
            }

            return symbolCount;
        }

        // Does a deep extract of all top-level TextElements between two positions.
        // Returns the combined symbol count of all extracted elements.
        // Each extracted element (and its children) are moved into a private tree.
        // This insures that outside references to the TextElement can still use
        // the TextElements freely, inserting or removing content, etc.
        //
        // Also calls AddLogicalChild on any top-level UIElements encountered.
        private int CutTopLevelLogicalNodes(TextTreeNode containingNode, TextPointer startPosition, TextPointer endPosition, out int charCount)
        {
            SplayTreeNode node;
            SplayTreeNode nextNode;
            SplayTreeNode stopNode;
            TextTreeTextElementNode elementNode;
            TextTreeObjectNode uiElementNode;
            char[] elementText;
            int symbolCount;
            TextContainer tree;
            TextPointer newTreeStart;
            IAvaloniaObject logicalParent;
            IAvaloniaObject currentLogicalChild;

            Invariant.Assert(startPosition.GetScopingNode() == endPosition.GetScopingNode(), "startPosition/endPosition not in same sibling tree!");

            node = startPosition.GetAdjacentSiblingNode(LogicalDirection.Forward);
            stopNode = endPosition.GetAdjacentSiblingNode(LogicalDirection.Forward);

            symbolCount = 0;
            charCount = 0;

            logicalParent = containingNode.GetLogicalTreeNode();

            while (node != stopNode)
            {
                currentLogicalChild = null;

                // Get the next node now, before we extract any TextElementNodes.
                nextNode = node.GetNextNode();

                elementNode = node as TextTreeTextElementNode;
                if (elementNode != null)
                {
                    // Grab the IMECharCount before we modify the node.
                    // This value depends on the node's current context.
                    int imeCharCountInOriginalContainer = elementNode.IMECharCount;

                    // Cut and record the matching symbols.
                    elementText = TextTreeText.CutText(_rootNode.RootTextBlock, elementNode.GetSymbolOffset(this.Generation), elementNode.SymbolCount);

                    // Rip the element out of its sibling tree.
                    // textElementNode.TextElement's TextElementNode will be updated
                    // with a deep copy of all contained nodes. We need a deep copy
                    // to ensure the new element/tree has no TextPointer references.
                    ExtractElementFromSiblingTree(containingNode, elementNode, true /* deep */);
                    // Assert that the TextElement now points to a new TextElementNode, not the original one.
                    Invariant.Assert(elementNode.TextElement.TextElementNode != elementNode);
                    // We want to start referring to the copied node, update elementNode.
                    elementNode = elementNode.TextElement.TextElementNode;

                    UpdateContainerSymbolCount(containingNode, -elementNode.SymbolCount, -imeCharCountInOriginalContainer);
                    NextGeneration(true /* deletedContent */);

                    // Stick it in a private tree so it's safe for the outside world to play with.
                    tree = new TextContainer(null, false /* plainTextOnly */);
                    newTreeStart = tree.Start;

                    tree.InsertElementToSiblingTree(newTreeStart, newTreeStart, elementNode);
                    Invariant.Assert(elementText.Length == elementNode.SymbolCount);
                    tree.UpdateContainerSymbolCount(elementNode.GetContainingNode(), elementNode.SymbolCount, elementNode.IMECharCount);
                    tree.DemandCreateText();
                    TextTreeText.InsertText(tree.RootTextBlock, 1 /* symbolOffset */, elementText);
                    tree.NextGeneration(false /* deletedContent */);

                    currentLogicalChild = elementNode.TextElement;

                    // Keep a running total of how many symbols we've removed.
                    symbolCount += elementNode.SymbolCount;
                    charCount += imeCharCountInOriginalContainer;
                }
                else
                {
                    uiElementNode = node as TextTreeObjectNode;
                    if (uiElementNode != null)
                    {
                        currentLogicalChild = uiElementNode.EmbeddedElement;
                    }
                }

                // Remove the child from the logical tree
                LogicalTreeHelper.RemoveLogicalChild(logicalParent, currentLogicalChild);

                node = nextNode;
            }

            if (symbolCount > 0)
            {
                startPosition.SyncToTreeGeneration();
                endPosition.SyncToTreeGeneration();
            }

            return symbolCount;
        }

        // Increments the position reference counts on nodes immediately
        // preceding and following a delete operation.
        //
        // Whenever we delete a span of content, we have to worry about any
        // positions still referencing the deleted content.  They have enough
        // information to find their way back to the surrounding nodes, but
        // we need to increment the ref count on those nodes now so that they'll
        // still be around when the positions need them.
        //
        // Because incrementing a ref count on a text node edge may involve
        // splitting the text node, this method takes refs to nodes and will
        // update the refs if a node is split.
        //
        // Called by DeleteContentFromSiblingTree and ExtractElementInternal.
        private void AdjustRefCountsForContentDelete(ref TextTreeNode previousNode, ElementEdge previousEdge,
                                                     ref TextTreeNode nextNode, ElementEdge nextEdge,
                                                     TextTreeNode middleSubTree)
        {
            bool leftEdgeReferenceCount;
            bool rightEdgeReferenceCount;

            leftEdgeReferenceCount = false;
            rightEdgeReferenceCount = false;

            // Get the count of all positions referencing text node edges across the deleted content.
            GetReferenceCounts((TextTreeNode)middleSubTree.GetMinSibling(), ref leftEdgeReferenceCount, ref rightEdgeReferenceCount);

            previousNode = previousNode.IncrementReferenceCount(previousEdge, rightEdgeReferenceCount);
            nextNode = nextNode.IncrementReferenceCount(nextEdge, leftEdgeReferenceCount);
        }

        // Sums the reference counts for a node and all following or contained nodes.
        private void GetReferenceCounts(TextTreeNode node, ref bool leftEdgeReferenceCount, ref bool rightEdgeReferenceCount)
        {
            do
            {
                // We can combine BeforeStart/BeforeEnd and AfterStart/AfterEnd because
                // they include all positions with equal gravity.
                leftEdgeReferenceCount |= node.BeforeStartReferenceCount || node.BeforeEndReferenceCount;
                rightEdgeReferenceCount |= node.AfterStartReferenceCount || node.AfterEndReferenceCount;

                if (node.ContainedNode != null)
                {
                    GetReferenceCounts((TextTreeNode)node.ContainedNode.GetMinSibling(), ref leftEdgeReferenceCount, ref rightEdgeReferenceCount);
                }

                node = (TextTreeNode)node.GetNextNode();
            }
            while (node != null);
        }

        // Increments the position reference counts on nodes immediately
        // preceding and following a delete operation on a single TextElementNode.
        // This is similar to AdjustRefCountsForContentDelete, except that
        // in this case we deleting a single node, and positions at the
        // BeforeStart/AfterEnd edges may move into contained content, which
        // is still live in the tree.
        //
        // Whenever we delete a span of content, we have to worry about any
        // positions still referencing the deleted content.  They have enough
        // information to find their way back to the surrounding nodes, but
        // we need to increment the ref count on those nodes now so that they'll
        // still be around when the positions need them.
        //
        // Because incrementing a ref count on a text node edge may involve
        // splitting the text node, this method takes refs to nodes and will
        // update the refs if a node is split.
        //
        // Called by ExtractElementFromSiblingTree.
        private void AdjustRefCountsForShallowDelete(ref TextTreeNode previousNode, ElementEdge previousEdge,
                                                     ref TextTreeNode nextNode,ElementEdge nextEdge,
                                                     ref TextTreeNode firstContainedNode, ref TextTreeNode lastContainedNode,
                                                     TextTreeTextElementNode extractedElementNode)
        {
            previousNode = previousNode.IncrementReferenceCount(previousEdge, extractedElementNode.AfterStartReferenceCount);

            nextNode = nextNode.IncrementReferenceCount(nextEdge, extractedElementNode.BeforeEndReferenceCount);

            if (firstContainedNode != null)
            {
                firstContainedNode = firstContainedNode.IncrementReferenceCount(ElementEdge.BeforeStart, extractedElementNode.BeforeStartReferenceCount);
            }
            else
            {
                nextNode = nextNode.IncrementReferenceCount(nextEdge, extractedElementNode.BeforeStartReferenceCount);
            }

            if (lastContainedNode != null)
            {
                lastContainedNode = lastContainedNode.IncrementReferenceCount(ElementEdge.AfterEnd, extractedElementNode.AfterEndReferenceCount);
            }
            else
            {
                previousNode = previousNode.IncrementReferenceCount(previousEdge, extractedElementNode.AfterEndReferenceCount);
            }
        }

        // Splits a sibling tree into three sub trees -- a tree with content before startPosition,
        // a tree with content between startPosition/endPosition, and a tree with content following endPosition.
        // Any of the subtrees may be null on exit, if they contain no content (eg, if
        // startPosition == endPosition, middleSubTree will be null on exit, and so forth).
        //
        // All returned roots have null ParentNode pointers -- the caller MUST
        // reparent all of them, even if deleting content, to ensure orphaned
        // TextPositions can find their way back to the original tree.
        //
        // Returns the symbol count of middleSubTree -- all the content between startPosition and endPosition.
        private int CutContent(TextPointer startPosition, TextPointer endPosition, out int charCount, out SplayTreeNode leftSubTree, out SplayTreeNode middleSubTree, out SplayTreeNode rightSubTree)
        {
            SplayTreeNode childNode;
            int symbolCount;

            Invariant.Assert(startPosition.GetScopingNode() == endPosition.GetScopingNode(), "startPosition/endPosition not in same sibling tree!");
            Invariant.Assert(startPosition.CompareTo(endPosition) != 0, "CutContent doesn't expect empty span!");

            // Get the root of all nodes to the left of the split.
            switch (startPosition.Edge)
            {
                case ElementEdge.BeforeStart:
                    leftSubTree = startPosition.Node.GetPreviousNode();
                    break;

                case ElementEdge.AfterStart:
                    leftSubTree = null;
                    break;

                case ElementEdge.BeforeEnd:
                default:
                    Invariant.Assert(false, "Unexpected edge!"); // Should have gone to simple insert case.
                    leftSubTree = null;
                    break;

                case ElementEdge.AfterEnd:
                    leftSubTree = startPosition.Node;
                    break;
            }

            // Get the root of all nodes to the right of the split.
            switch (endPosition.Edge)
            {
                case ElementEdge.BeforeStart:
                    rightSubTree = endPosition.Node;
                    break;

                case ElementEdge.AfterStart:
                default:
                    Invariant.Assert(false, "Unexpected edge! (2)"); // Should have gone to simple insert case.
                    rightSubTree = null;
                    break;

                case ElementEdge.BeforeEnd:
                    rightSubTree = null;
                    break;

                case ElementEdge.AfterEnd:
                    rightSubTree = endPosition.Node.GetNextNode();
                    break;
            }

            // Get the root of all nodes covered by startPosition/endPosition.
            if (rightSubTree == null)
            {
                if (leftSubTree == null)
                {
                    middleSubTree = startPosition.GetScopingNode().ContainedNode;
                }
                else
                {
                    middleSubTree = leftSubTree.GetNextNode();
                }
            }
            else
            {
                middleSubTree = rightSubTree.GetPreviousNode();
                if (middleSubTree == leftSubTree)
                {
                    middleSubTree = null;
                }
            }

            // Split the tree into three sub trees matching the roots we've found.

            if (leftSubTree != null)
            {
                leftSubTree.Split();
                Invariant.Assert(leftSubTree.Role == SplayTreeNodeRole.LocalRoot);
                leftSubTree.ParentNode.ContainedNode = null;
                leftSubTree.ParentNode = null;
            }

            symbolCount = 0;
            charCount = 0;

            if (middleSubTree != null)
            {
                if (rightSubTree != null)
                {
                    // Split will move middleSubTree up to the root.
                    middleSubTree.Split();
                }
                else
                {
                    // Make sure middleSubTree is a root.
                    middleSubTree.Splay();
                }
                Invariant.Assert(middleSubTree.Role == SplayTreeNodeRole.LocalRoot, "middleSubTree is not a local root!");

                if (middleSubTree.ParentNode != null)
                {
                    middleSubTree.ParentNode.ContainedNode = null;
                    middleSubTree.ParentNode = null;
                }

                // Calc the symbol count of the middle tree.
                for (childNode = middleSubTree; childNode != null; childNode = childNode.RightChildNode)
                {
                    symbolCount += childNode.LeftSymbolCount + childNode.SymbolCount;
                    charCount += childNode.LeftCharCount + childNode.IMECharCount;
                }
            }

            if (rightSubTree != null)
            {
                // Make sure rightSubTree is a root before returning.
                // We haven't done anything yet to ensure this.
                rightSubTree.Splay();
            }

            Invariant.Assert(leftSubTree == null || leftSubTree.Role == SplayTreeNodeRole.LocalRoot);
            Invariant.Assert(middleSubTree == null || middleSubTree.Role == SplayTreeNodeRole.LocalRoot);
            Invariant.Assert(rightSubTree == null || rightSubTree.Role == SplayTreeNodeRole.LocalRoot);

            return symbolCount;
        }

        // ExtractElement worker.  Removes a TextElement from the tree.
        //
        // If deep is true, also removes any content covered by the element.
        // In this case element.TextTreeElementNode will be replaced with a
        // deep copy of all contained nodes.  Since this is a copy, it can
        // be safely inserted into a new tree -- no positions reference it.
        //
        // deep is true when this method is called during a cross-tree insert
        // (that is, when a TextElement is extracted from one tree and inserted
        // into another via a call to InsertElement).
        //
        // If deep is true, returns the raw text corresponding to element and
        // its contained content.  Otherwise returns null.
        //
        // If deep is true, extractChangeEventArgs will be non-null on exit,
        // containing all the information needed to raise a matching TextChanged
        // event.  Otherwise, extractChangeEventArgs will be null on exit.
        private char[] ExtractElementInternal(TextElement element, bool deep, out ExtractChangeEventArgs extractChangeEventArgs)
        {
            TextTreeTextElementNode elementNode;
            SplayTreeNode containingNode;
            TextPointer startPosition;
            TextPointer endPosition;
            bool empty;
            int symbolOffset;
            char[] elementText;
            TextTreeUndoUnit undoUnit;
            SplayTreeNode firstContainedChildNode;
            SplayTreeNode lastContainedChildNode;
            IAvaloniaObject oldLogicalParent;

            BeforeAddChange();

            firstContainedChildNode = null;
            lastContainedChildNode = null;
            extractChangeEventArgs = null;

            elementText = null;
            elementNode = element.TextElementNode;
            containingNode = elementNode.GetContainingNode();
            empty = (elementNode.ContainedNode == null);

            startPosition = new TextPointer(this, elementNode, ElementEdge.BeforeStart, LogicalDirection.Backward);
            // We only need the end position if this element originally spanned any content.
            endPosition = null;
            if (!empty)
            {
                endPosition = new TextPointer(this, elementNode, ElementEdge.AfterEnd, LogicalDirection.Backward);
            }

            symbolOffset = elementNode.GetSymbolOffset(this.Generation);

            // Remember the old parent
            oldLogicalParent = ((TextTreeNode)containingNode).GetLogicalTreeNode();

            // Invalidate any TextElementCollection that depends on the parent.
            // Make sure we do that before raising any public events.
            TextElementCollectionHelper.MarkDirty(oldLogicalParent);


            // Remove the element from the logical tree.
            // NB: we do this even for a deep extract, because we can't wait --
            // during a deep extract/move to new tree, the property system must be
            // notified before the element moves into its new tree.
            element.BeforeLogicalTreeChange();
            try
            {
                LogicalTreeHelper.RemoveLogicalChild(oldLogicalParent, element);
            }
            finally
            {
                element.AfterLogicalTreeChange();
            }

            // Handle undo.
            if (deep && !empty)
            {
                undoUnit = TextTreeUndo.CreateDeleteContentUndoUnit(this, startPosition, endPosition);
            }
            else
            {
                undoUnit = TextTreeUndo.CreateExtractElementUndoUnit(this, elementNode);
            }

            // Save the first/last contained node now -- after the ExtractElementFromSiblingTree
            // call it will be too late to find them.
            if (!deep && !empty)
            {
                firstContainedChildNode = elementNode.GetFirstContainedNode();
                lastContainedChildNode = elementNode.GetLastContainedNode();
            }

            // Record all the IME related char state before the extract.
            int imeCharCount = elementNode.IMECharCount;
            int imeLeftEdgeCharCount = elementNode.IMELeftEdgeCharCount;
            
            int nextNodeCharDelta = 0;
            
            // DevDiv.1092668 We care about the next node only if it will become the First IME Visible Sibling 
            // after the extraction. If this is a deep extract we shouldn't care if the element is empty, 
            // since all of its contents are getting extracted as well
            TextTreeTextElementNode nextNode = null;
            if ((deep || empty) && element.IsFirstIMEVisibleSibling)
            {
                nextNode = (TextTreeTextElementNode)elementNode.GetNextNode();
                
                if (nextNode != null)
                {
                    // The following node is the new first ime visible sibling.
                    // It just moved, and loses an edge character.
                    nextNodeCharDelta = -nextNode.IMELeftEdgeCharCount;
                    nextNode.IMECharCount += nextNodeCharDelta;
                }
            }

            // Rip the element out of its sibling tree.
            // If this is a deep extract element's TextElementNode will be updated
            // with a deep copy of all contained nodes.
            ExtractElementFromSiblingTree(containingNode, elementNode, deep);

            // The first contained node of the extracted node may no longer
            // be a first sibling after the parent extract.  If that's the case,
            // update its char count.
            int containedNodeCharDelta = 0;
            TextTreeTextElementNode firstContainedElementNode = firstContainedChildNode as TextTreeTextElementNode;
            if (firstContainedElementNode != null)
            {
                containedNodeCharDelta = firstContainedElementNode.IMELeftEdgeCharCount;
                firstContainedElementNode.IMECharCount += containedNodeCharDelta;
            }

            if (!deep)
            {
                // Unlink the TextElement from the TextElementNode.
                element.TextElementNode = null;

                // Pull out the edge symbols from the text store.            
                TextTreeText.RemoveElementEdges(_rootNode.RootTextBlock, symbolOffset, elementNode.SymbolCount);
            }
            else
            {
                // We leave element.TextElement alone, since for a deep extract we've already
                // stored a copy of the original nodes there that we'll use in a following insert.

                // Cut and return the matching symbols.
                elementText = TextTreeText.CutText(_rootNode.RootTextBlock, symbolOffset, elementNode.SymbolCount);
            }

            // Ancestor nodes lose either the whole node or two element edge symbols, depending
            // on whether or not this is a deep extract.
            if (deep)
            {
                UpdateContainerSymbolCount(containingNode, -elementNode.SymbolCount, -imeCharCount + nextNodeCharDelta + containedNodeCharDelta);
            }
            else
            {
                UpdateContainerSymbolCount(containingNode, /* symbolCount */ -2, /* charCount */ -imeLeftEdgeCharCount + nextNodeCharDelta + containedNodeCharDelta);
            }

            NextGeneration(true /* deletedContent */);

            if (undoUnit != null)
            {
                undoUnit.SetTreeHashCode();
            }

            // Raise the public event.
            if (deep)
            {
                extractChangeEventArgs = new ExtractChangeEventArgs(this, startPosition, elementNode, nextNodeCharDelta == 0 ? null : nextNode, containedNodeCharDelta == 0 ? null : firstContainedElementNode, imeCharCount, imeCharCount - imeLeftEdgeCharCount);
            }
            else if (empty)
            {
                AddChange(startPosition, /* symbolCount */ 2, /* charCount */ imeCharCount, PrecursorTextChangeType.ContentRemoved);
            }
            else
            {
                AddChange(startPosition, endPosition, elementNode.SymbolCount,
                          imeLeftEdgeCharCount,
                          imeCharCount - imeLeftEdgeCharCount,
                          PrecursorTextChangeType.ElementExtracted, null, false);
            }

            // Raise events for nodes that just gained or lost an IME char due
            // to changes in their surroundings.
            if (extractChangeEventArgs == null)
            {
                if (nextNodeCharDelta != 0)
                {
                    RaiseEventForNewFirstIMEVisibleNode(nextNode);
                }

                if (containedNodeCharDelta != 0)
                {
                    RaiseEventForFormerFirstIMEVisibleNode(firstContainedElementNode);
                }
            }

            if (!deep && !empty)
            {
                ReparentLogicalChildren(firstContainedChildNode, lastContainedChildNode, oldLogicalParent /* new parent */, element /* old parent */);
            }

            //
            // Remove char count for logical break, since the element is leaving the tree.
            //
            if (null != element.TextElementNode)
            {
                element.TextElementNode.IMECharCount -= imeLeftEdgeCharCount;
            }

            return elementText;
        }

        // Removes an element node from its sibling tree.
        // 
        // If deep == true, then this method also removes any contained nodes
        // and returns a deep copy of them.
        //
        // If deep == false, any contained nodes are inserted into the original
        // node's sibling tree.
        private void ExtractElementFromSiblingTree(SplayTreeNode containingNode, TextTreeTextElementNode elementNode, bool deep)
        {
            TextTreeNode previousNode;
            ElementEdge previousEdge;
            TextTreeNode nextNode;
            ElementEdge nextEdge;
            SplayTreeNode childNode;
            SplayTreeNode minChildNode;
            SplayTreeNode maxChildNode;
            SplayTreeNode localRootNode;
            TextTreeNode firstContainedNode;
            TextTreeNode lastContainedNode;

            // Remember the nodes surrounding the one we're going to remove.
            previousNode = (TextTreeNode)elementNode.GetPreviousNode();
            previousEdge = ElementEdge.AfterEnd;
            if (previousNode == null)
            {
                previousNode = (TextTreeNode)containingNode;
                previousEdge = ElementEdge.AfterStart;
            }
            nextNode = (TextTreeNode)elementNode.GetNextNode();
            nextEdge = ElementEdge.BeforeStart;
            if (nextNode == null)
            {
                nextNode = (TextTreeNode)containingNode;
                nextEdge = ElementEdge.BeforeEnd;
            }

            // Remove the element node.
            elementNode.Remove();
            Invariant.Assert(elementNode.Role == SplayTreeNodeRole.LocalRoot);

            if (deep)
            {
                // Increment previous/nextNode reference counts. This may involve
                // splitting a text node, so we use refs.
                AdjustRefCountsForContentDelete(ref previousNode, previousEdge, ref nextNode, nextEdge, elementNode);

                // Reparent the removed node with a FixupNode, so that any orphaned
                // positions can find their way back to the tree.
                // We have to do this after the AdjustRefCountsForContentDelete call, because the fixup
                // node doesn't act like a regular node.
                elementNode.ParentNode = new TextTreeFixupNode(previousNode, previousEdge, nextNode, nextEdge);

                DeepCopy(elementNode);
            }
            else
            {
                // Reparent contained nodes to elementNode's parent.
                childNode = elementNode.ContainedNode;
                elementNode.ContainedNode = null;
                if (childNode != null)
                {
                    childNode.ParentNode = null;
                    firstContainedNode = (TextTreeNode)childNode.GetMinSibling();
                    lastContainedNode = (TextTreeNode)childNode.GetMaxSibling();
                }
                else
                {
                    firstContainedNode = null;
                    lastContainedNode = null;
                }

                // Increment previous/nextNode reference counts. This may involve
                // splitting a text node, so we use refs.
                AdjustRefCountsForShallowDelete(ref previousNode, previousEdge, ref nextNode, nextEdge, ref firstContainedNode, ref lastContainedNode, elementNode);

                // Reparent the removed node with a FixupNode, so that any orphaned
                // positions can find their way back to the tree.
                // We have to do this after the AdjustRefCountsForContentDelete call, because the fixup
                // node doesn't act like a regular node.
                elementNode.ParentNode = new TextTreeFixupNode(previousNode, previousEdge, nextNode, nextEdge, firstContainedNode, lastContainedNode);

                if (childNode != null)
                {
                    // Get previous/next nodes into roots of individual trees.
                    // Then merge them with the element's children.

                    // We need to splay childNode because it may no longer be a local root.
                    // The addrefs in AdjustRefCountsForShallowDelete may have created new nodes
                    // and shuffled the tree.
                    childNode.Splay();
                    localRootNode = childNode;

                    if (previousNode != containingNode)
                    {
                        previousNode.Split();
                        Invariant.Assert(previousNode.Role == SplayTreeNodeRole.LocalRoot);
                        Invariant.Assert(previousNode.RightChildNode == null);

                        minChildNode = childNode.GetMinSibling();
                        minChildNode.Splay();

                        previousNode.RightChildNode = minChildNode;
                        minChildNode.ParentNode = previousNode;

                        localRootNode = previousNode;
                    }

                    if (nextNode != containingNode)
                    {
                        nextNode.Splay();
                        Invariant.Assert(nextNode.Role == SplayTreeNodeRole.LocalRoot);
                        Invariant.Assert(nextNode.LeftChildNode == null);

                        maxChildNode = childNode.GetMaxSibling();
                        maxChildNode.Splay();

                        nextNode.LeftChildNode = maxChildNode;
                        nextNode.LeftSymbolCount += maxChildNode.LeftSymbolCount + maxChildNode.SymbolCount;
                        nextNode.LeftCharCount += maxChildNode.LeftCharCount + maxChildNode.IMECharCount;
                        maxChildNode.ParentNode = nextNode;

                        localRootNode = nextNode;
                    }

                    containingNode.ContainedNode = localRootNode;
                    if (localRootNode != null)
                    {
                        localRootNode.ParentNode = containingNode;
                    }
                }
            }
        }

        // Returns a copy of elementNode and all its children.  The copy is
        // guaranteed to have no position references.
        //
        // All TextElements referencing TextTreeTextElementNodes in the copy
        // are adjusted to point to the new copied nodes.
        private TextTreeTextElementNode DeepCopy(TextTreeTextElementNode elementNode)
        {
            TextTreeTextElementNode clone;

            clone = (TextTreeTextElementNode)elementNode.Clone();
            elementNode.TextElement.TextElementNode = clone;

            if (elementNode.ContainedNode != null)
            {
                clone.ContainedNode = DeepCopyContainedNodes((TextTreeNode)elementNode.ContainedNode.GetMinSibling());
                clone.ContainedNode.ParentNode = clone;
            }

            return clone;
        }

        // Returns a copy of a sibling tree.  node is expected to be the first sibling.
        private TextTreeNode DeepCopyContainedNodes(TextTreeNode node)
        {
            TextTreeNode rootClone;
            TextTreeNode previousClone;
            TextTreeNode clone;
            TextTreeTextElementNode elementNode;

            rootClone = null;
            previousClone = null;

            do
            {
                elementNode = node as TextTreeTextElementNode;
                if (elementNode != null)
                {
                    clone = DeepCopy(elementNode);
                }
                else
                {
                    clone = node.Clone();
                }

                // clone will be null in one case: if we're trying to clone an
                // empty TextNode.  We can skip empty TextNodes (symbol count == 0)
                // because we know the clones have no TextPointer references, so
                // an empty node serves no purpose.
                Invariant.Assert(clone != null || node is TextTreeTextNode && node.SymbolCount == 0);
                if (clone != null)
                {
                    clone.ParentNode = previousClone;
                    if (previousClone != null)
                    {
                        previousClone.RightChildNode = clone;
                    }
                    else
                    {
                        Invariant.Assert(clone.Role == SplayTreeNodeRole.LocalRoot);
                        // Remember the first clone created.
                        rootClone = clone;
                    }

                    previousClone = clone;
                }

                node = (TextTreeNode)node.GetNextNode();
            }
            while (node != null);

            return rootClone;
        }

        // Lazy allocates the root node, which we don't need until someone asks
        // for Start/End.
        private void DemandCreatePositionState()
        {
            if (_rootNode == null)
            {
                _rootNode = new TextTreeRootNode(this);
            }
        }

        // Lazy initializer for the TextTreeText.  Called just before content insertion.
        private void DemandCreateText()
        {
            Invariant.Assert(_rootNode != null, "Unexpected DemandCreateText call before position allocation.");

            if (_rootNode.RootTextBlock == null)
            {
                _rootNode.RootTextBlock = new TextTreeRootTextBlock();
                // Insert the root node (this TextTree) element edges.
                TextTreeText.InsertElementEdges(_rootNode.RootTextBlock, 0, 0);
            }
        }

        // Updates the SymbolCount for node's container nodes -- all the way to the TextTree root.
        private void UpdateContainerSymbolCount(SplayTreeNode containingNode, int symbolCount, int charCount)
        {
            do
            {
                containingNode.Splay();
                containingNode.SymbolCount += symbolCount;
                containingNode.IMECharCount += charCount;
                containingNode = containingNode.ParentNode;
            }
            while (containingNode != null);
        }

        // Increments the tree's generation counter.  This method should be
        // called anytime the tree's content changes.
        //
        // TextPointers only need to worry about deletions, hence the deletedContent
        // parameter which should be set true if any content was removed.
        //
        // Note we're ignoring wrap-around on the generation counter. If we average
        // one edit per second it will take 136 years to overflow.
        private void NextGeneration(bool deletedContent)
        {
            AssertTree();
            AssertTreeAndTextSize();

            _rootNode.Generation++;

            if (deletedContent)
            {
                _rootNode.PositionGeneration++;
            }

            // Layout generation is a superset.
            NextLayoutGeneration();
        }

        // Copies a LocalValueEnumerator properties into an array of AvaloniaProperty.
        // This method is useful because LocalValueEnumerator does not support
        // setting or clearing local values while enumerating.
        private AvaloniaProperty[] LocalValueEnumeratorToArray(LocalValueEnumerator valuesEnumerator)
        {
            AvaloniaProperty[] properties;
            int count;

            properties = new AvaloniaProperty[valuesEnumerator.Count];

            count = 0;
            valuesEnumerator.Reset();
            while (valuesEnumerator.MoveNext())
            {
                properties[count++] = valuesEnumerator.Current.Property;
            }

            return properties;
        }

        #region ValidateXXXHelpers

        // Validation for SetValue.
        private void ValidateSetValue(TextPointer position)
        {
            TextElement element;

            if (position.TextContainer != this)
            {
                throw new InvalidOperationException(/*SR.Get(SRID.NotInThisTree, "position")*/);
            }

            position.SyncToTreeGeneration();

            element = position.Parent as TextElement;
            if (element == null)
            {
                throw new InvalidOperationException(/*SR.Get(SRID.NoElement)*/);
            }
        }

        #endregion ValidateXXXHelpers

        // Verifies the TextContainer symbol count is in synch with the TextTreeText
        // character count.
        private void AssertTreeAndTextSize()
        {
            if (Invariant.Strict)
            {
                int count;
                TextTreeTextBlock textBlock;

                if (_rootNode.RootTextBlock != null)
                {
                    count = 0;

                    for (textBlock = (TextTreeTextBlock)_rootNode.RootTextBlock.ContainedNode.GetMinSibling(); textBlock != null; textBlock = (TextTreeTextBlock)textBlock.GetNextNode())
                    {
                        Invariant.Assert(textBlock.Count > 0, "Empty TextBlock!");
                        count += textBlock.Count;
                    }

                    Invariant.Assert(count == this.InternalSymbolCount, "TextContainer.SymbolCount does not match TextTreeText size!");
                }
            }
        }

#if DEBUG_SLOW
        private void AssertTreeRecursive(TextTreeNode containingNode)
        {
            TextTreeNode node;

            if (containingNode.ContainedNode == null)
            {
                Invariant.Assert(containingNode.ParentNode == null || containingNode.ParentNode.ContainedNode == containingNode);
                return;
            }

            Invariant.Assert(containingNode.ContainedNode.ParentNode == containingNode);

            for (node = (TextTreeNode)containingNode.ContainedNode.GetMinSibling(); node != null; node = (TextTreeNode)node.GetNextNode())
            {
                if (node != containingNode.ContainedNode)
                {
                    Invariant.Assert(node.ParentNode.LeftChildNode == node || node.ParentNode.RightChildNode == node);
                }
                Invariant.Assert(node.SymbolCount >= 0);
                if (node.SymbolCount == 0)
                {
                    Invariant.Assert(node is TextTreeTextNode);
                    Invariant.Assert(node.BeforeStartReferenceCount > 0 || node.AfterEndReferenceCount > 0);
                }

                if (node.ContainedNode != null)
                {
                    AssertTreeRecursive((TextTreeNode)node.ContainedNode);
                }
            }
        }
#endif // DEBUG_SLOW

        // Worker for internal Begin methods.
        private void BeginChange(bool undo)
        {
            if (undo && _changeBlockUndoRecord == null && _changeBlockLevel == 0)
            {
                Invariant.Assert(_changeBlockLevel == 0);
                _changeBlockUndoRecord = new ChangeBlockUndoRecord(this, String.Empty);
            }

            // Disable processing of the queue during change notifications to prevent reentrancy.
            if (_changeBlockLevel == 0)
            {
                DemandCreatePositionState(); // Ensure _rootNode != null.

                //if (this.Dispatcher != null)
                //{
                //    _rootNode.DispatcherProcessingDisabled = this.Dispatcher.DisableProcessing();
                //}
            }

            _changeBlockLevel++;

            // We'll raise the Changing event when/if we get an actual
            // change added, inside BeforeAddChange.
        }

        // Worker for AddChange, fires a Change event.
        private void FireChangeEvent(TextPointer startPosition, TextPointer endPosition, int symbolCount,
                                     int leftEdgeCharCount, int childCharCount,
                                     PrecursorTextChangeType precursorTextChange, AvaloniaProperty property, bool affectsRenderOnly)
        {
            Invariant.Assert(this.ChangeHandler != null);

            // Set a flag to disallow reentrant edits.  We really can't support
            // that here, because any edits to the document would break the
            // BeginChange/EndChange contract (no edits by listeners in a change
            // block!).
            // (Note, we will allow re-entrant property changes only, because
            // property change events are not exposed publicly on TextBox or RTB.)
            SetFlags(true, Flags.ReadOnly);

            try
            {
                if (precursorTextChange == PrecursorTextChangeType.ElementAdded)
                {
                    Invariant.Assert(symbolCount > 2, "ElementAdded must span at least two element edges and one content symbol!");

                    TextContainerChangeEventArgs args1 = new TextContainerChangeEventArgs(startPosition, 1, leftEdgeCharCount, TextChangeType.ContentAdded);
                    TextContainerChangeEventArgs args2 = new TextContainerChangeEventArgs(endPosition, 1, 0, TextChangeType.ContentAdded);
                    ChangeHandler(this, args1);
                    ChangeHandler(this, args2);
                }
                else if (precursorTextChange == PrecursorTextChangeType.ElementExtracted)
                {
                    Invariant.Assert(symbolCount > 2, "ElementExtracted must span at least two element edges and one content symbol!");

                    TextContainerChangeEventArgs args1 = new TextContainerChangeEventArgs(startPosition, 1, leftEdgeCharCount, TextChangeType.ContentRemoved);
                    TextContainerChangeEventArgs args2 = new TextContainerChangeEventArgs(endPosition, 1, 0, TextChangeType.ContentRemoved);
                    ChangeHandler(this, args1);
                    ChangeHandler(this, args2);
                }
                else
                {
                    TextContainerChangeEventArgs args = new TextContainerChangeEventArgs(startPosition, symbolCount, leftEdgeCharCount + childCharCount, ConvertSimplePrecursorChangeToTextChange(precursorTextChange), property, affectsRenderOnly);
                    ChangeHandler(this, args);
                }
            }
            finally
            {
                SetFlags(false, Flags.ReadOnly);
            }
        }

        // Returns the TextChange matching an ContentAdded, ContentRemoved,
        // or PropertyModified PrecursorTextChange.
        private TextChangeType ConvertSimplePrecursorChangeToTextChange(PrecursorTextChangeType precursorTextChange)
        {
            Invariant.Assert(precursorTextChange != PrecursorTextChangeType.ElementAdded && precursorTextChange != PrecursorTextChangeType.ElementExtracted);
            return (TextChangeType)precursorTextChange;
        }

        // Helper for DeleteContentInternal.
        // If startPosition is placed at the very front of its parent's sibling list,
        // returns the next sibling following endPositoin (the new head of the sibling
        // list).  The new head node is interesting because its IMELeftEdgeCharCount may
        // change because of its new position.
        private TextTreeTextElementNode GetNextIMEVisibleNode(TextPointer startPosition, TextPointer endPosition)
        {
            TextTreeTextElementNode nextIMEVisibleNode = null;

            TextElement adjacentElement = startPosition.GetAdjacentElement(LogicalDirection.Forward) as TextElement;
            if (adjacentElement != null && adjacentElement.IsFirstIMEVisibleSibling)
            {
                nextIMEVisibleNode = (TextTreeTextElementNode)endPosition.GetAdjacentSiblingNode(LogicalDirection.Forward);
            }

            return nextIMEVisibleNode;
        }

        // Fires Change events for IMELeftEdgeCharCount deltas to a node after it
        // moves out of position such that it is no longer the leftmost child
        // of its parent.
        private void RaiseEventForFormerFirstIMEVisibleNode(TextTreeNode node)
        {
            TextPointer startEdgePosition = new TextPointer(this, node, ElementEdge.BeforeStart);

            // Next node was the old first node.  Its IMECharCount
            // just bumped up, report that.
            AddChange(startEdgePosition, /* symbolCount */ 0, /* IMECharCount */ 1, PrecursorTextChangeType.ContentAdded);
        }

        // Fires Change events for IMELeftEdgeCharCount deltas to a node after it
        // moves into position such that it is the leftmost child
        // of its parent.
        private void RaiseEventForNewFirstIMEVisibleNode(TextTreeNode node)
        {
            TextPointer startEdgePosition = new TextPointer(this, node, ElementEdge.BeforeStart);

            // node was the old second node.  Its IMECharCount
            // just dropped down, report that.
            AddChange(startEdgePosition, /* symbolCount */ 0, /* IMECharCount */ 1, PrecursorTextChangeType.ContentRemoved);
        }

        // Sets boolean state.
        private void SetFlags(bool value, Flags flags)
        {
            _flags = value ? (_flags | flags) : (_flags & (~flags));
        }

        // Reads boolean state.
        private bool CheckFlags(Flags flags)
        {
            return ((_flags & flags) == flags);
        }

        #endregion Private methods

        //------------------------------------------------------
        //
        //  Private Properties
        //
        //------------------------------------------------------

        #region Private Properties

        // Dispatcher associated with the parent of this TextContainer, or
        // null if the TextContainer has no parent.
        //private Dispatcher Dispatcher
        //{
        //    get
        //    {
        //        return (this.Parent != null) ? this.Parent.Dispatcher : null;
        //    }
        //}

        #endregion Private Properties

        //------------------------------------------------------
        //
        //  Private Types
        //
        //------------------------------------------------------

        #region Private Types

        // Holds the state for delayed ChangeEvent.
        // We delay change events for element extracts when a TextElement is
        // being moved from tree to another.  When the TextElement is safely
        // in the new tree, then we raise a change event in the old tree.
        private class ExtractChangeEventArgs
        {
            //------------------------------------------------------
            //
            //  Constructors
            //
            //------------------------------------------------------

            #region Constructors

            // Creates a new instance.
            internal ExtractChangeEventArgs(TextContainer textTree, TextPointer startPosition, TextTreeTextElementNode node,
                TextTreeTextElementNode newFirstIMEVisibleNode, TextTreeTextElementNode formerFirstIMEVisibleNode, int charCount, int childCharCount)
            {
                _textTree = textTree;
                _startPosition = startPosition;
                _symbolCount = node.SymbolCount;
                _charCount = charCount;
                _childCharCount = childCharCount;
                _newFirstIMEVisibleNode = newFirstIMEVisibleNode;
                _formerFirstIMEVisibleNode = formerFirstIMEVisibleNode;
            }

            #endregion Constructors

            //------------------------------------------------------
            //
            //  Internal Methods
            //
            //------------------------------------------------------

            #region Internal Methods

            // Fires change event recorded in this object.
            internal void AddChange()
            {
                _textTree.AddChange(_startPosition, _symbolCount, _charCount, PrecursorTextChangeType.ContentRemoved);

                if (_newFirstIMEVisibleNode != null)
                {
                    _textTree.RaiseEventForNewFirstIMEVisibleNode(_newFirstIMEVisibleNode);
                }

                if (_formerFirstIMEVisibleNode != null)
                {
                    _textTree.RaiseEventForFormerFirstIMEVisibleNode(_formerFirstIMEVisibleNode);
                }
            }

            #endregion Internal Methods

            //------------------------------------------------------
            //
            //  Internal Properties
            //
            //------------------------------------------------------

            #region Internal Properties

            // TextContainer associated with the pending event.
            internal TextContainer TextContainer { get { return _textTree; } }

            // Count of chars covered the extracted element, not counting edges.
            internal int ChildIMECharCount { get { return _childCharCount; } }

            #endregion Internal Properties

            //------------------------------------------------------
            //
            //  Private Fields
            //
            //------------------------------------------------------

            #region Private Fields

            // TextContainer associated with the pending event.
            private readonly TextContainer _textTree;

            // TextPointer associated with the pending event.
            // This is the position immediately precedining the former location
            // of the extracted element.
            private readonly TextPointer _startPosition;

            // Count of symbols covered by the extracted element, including the
            // two element edges.
            private readonly int _symbolCount;

            // Count of chars covered the extracted element.
            private readonly int _charCount;

            // Count of chars covered the extracted element, not counting edges.
            private readonly int _childCharCount;

            // Next node following the extracted node.
            private readonly TextTreeTextElementNode _newFirstIMEVisibleNode;

            // Former first contained node of the extracted node.
            private readonly TextTreeTextElementNode _formerFirstIMEVisibleNode;

            #endregion Private Fields
        }

        // Booleans for the _flags field.
        [System.Flags]
        private enum Flags
        {
            // Set true during TextContainer.Change event callback.
            // When true, modifying the TextContainer is disallowed.
            ReadOnly = 0x1,

            // Set in the ctor.  If true, only plain text may be inserted
            // into the TextContainer and perf optimizations are enabled.
            PlainTextOnly = 0x2,

            // Set in the ctor.  Passed on to TextContainerChangedEventArgs to control
            // whether or not content changes are tracked.
            CollectTextChanges = 0x4,
        }

        #endregion Private Types

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        #region Private Fields

        //
        // !!! IMPORTANT !!!
        //
        // Before adding new fields to TextContainer, consider adding them instead
        // to TextTreeRootNode.  We lazy allocate the root node when TextPositions
        // are allocated (before any content can be inserted), so it's usually
        // a more appropriate place for state.  Keep empty trees cheap!
        //

        // Parent IAvaloniaObject, supplied in ctor.  May be null.
        private readonly IAvaloniaObject _parent;

        // Root node of the tree.  Lazy allocated when the first TextPointer
        // is requested.
        private TextTreeRootNode _rootNode;

        // Collection of highlights applied to TextContainer content.
        private Highlights _highlights;

        // BeginChange ref count.  When non-zero, we are inside a change block.
        private int _changeBlockLevel;

        // Array of pending changes in the current change block.
        // Null outside of a change block.
        private TextContainerChangedEventArgs _changes;

        // TextView associated with this TextContainer.
        private ITextView _textview;

        // Undo manager associated with this TextContainer.
        // May be null.
        private UndoManager _undoManager;

        // TextSelection associated with this container.
        private ITextSelection _textSelection;

        // Undo unit associated with the current change block, if any.
        private ChangeBlockUndoRecord _changeBlockUndoRecord;

        // implementation of ITextContainer.Changing
        private EventHandler ChangingHandler;

        // implementation of ITextContainer.Change
        private TextContainerChangeEventHandler ChangeHandler;

        // implementation of ITextContainer.Changed
        private TextContainerChangedEventHandler ChangedHandler;

        // Boolean flags, set with Flags enum.
        private Flags _flags;

#if DEBUG
        private int _debugId = TextTreeNode.GetDebugId();
#endif // DEBUG

        #endregion Private Fields
    }
}
