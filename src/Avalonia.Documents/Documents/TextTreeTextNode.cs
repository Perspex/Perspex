// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: A TextContainer node representing a run of text.
//

using System;
using Avalonia.Media.TextFormatting;
using MS.Internal;

namespace System.Windows.Documents
{
    // Runs of text are represented internally by TextTreeTextNodes.
    //
    // Text nodes are regularly "split" -- broken in half -- when a
    // TextPointer needs to reference a character within the node's run.
    // We track a reference counts of the number of positions referencing a
    // text node so that we can later merge it back again when no TextPositions
    // reference it.
    //
    // Unlike other nodes, TextTreeTextNode may only ever be referenced on a
    // single edge by TextPositions.  This is necessary to ensure that we
    // can split a text node any time a TextPositions needs to reference a
    // character within the node.  With at most one edge referenced we are free
    // to create a new node covering the unreferenced edge without disturbing
    // any TextPositoins referencing the original node.
    //
    // Unlike all other nodes, text nodes may occasionally have zero symbol counts.
    // This happens when a TextPointer wants to reference a node edge, but
    // the node's other edge is already referenced by someone else.  In this case,
    // we split the text node, adding a zero-width node just for the
    // TextPointer's use.
    internal class TextTreeTextNode : TextTreeNode
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        #region Constructors

        // Creates a new TextTreeTextNode instance.
        internal TextTreeTextNode()
        {
            _symbolOffsetCache = -1;
        }
 
        #endregion Constructors
 
        //------------------------------------------------------
        //
        //  Public Methods
        //
        //------------------------------------------------------

        #region Public Methods

#if DEBUG
        // Debug-only ToString override.
        public override string ToString()
        {
            return ("TextNode Id=" + this.DebugId + " SymbolCount=" + _symbolCount);
        }
#endif // DEBUG

        #endregion Public Methods

        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------

        #region Internal Methods

        // Returns a shallow copy of this node.
        // Returns null if this node has a zero symbol count --
        // a clone would have no references, so there's no point
        // in allocating one.
        internal override TextTreeNode Clone()
        {
            TextTreeTextNode clone;

            clone = null;

            if (_symbolCount > 0)
            {
                clone = new TextTreeTextNode();
                clone._symbolCount = _symbolCount;
            }

            return clone;
        }

        // Returns the TextPointerContext of the node.
        internal override TextPointerContext GetPointerContext(LogicalDirection direction)
        {
            return TextPointerContext.Text;
        }

        // Increments a count of the number of TextPositions and TextNavigators
        // referencing this node.  Called when positions are created or moved.
        // Returns the actual node referenced -- a new node will be created if
        // there are already references to this node's other edge.
        internal override TextTreeNode IncrementReferenceCount(ElementEdge edge, int delta)
        {
            TextTreeTextNode node;
            TextTreeTextNode mergeNode;

            Invariant.Assert(delta >= 0);
            Invariant.Assert(edge == ElementEdge.BeforeStart || edge == ElementEdge.AfterEnd, "Bad edge ref to TextTreeTextNode!");

            if (delta == 0)
            {
                return this;
            }

            if (_positionRefCount > 0 && edge != _referencedEdge)
            {
                // We need to split off a node to cover the new edge.
                node = Split(edge == ElementEdge.BeforeStart ? 0 : _symbolCount, edge);

                node._referencedEdge = edge;
                node._positionRefCount += delta;

                // It's possible we need to merge a neighbor node.
                // This happens when someone calls TextContainer.GetNodeAndEdgeAtOffset,
                // which splits a text node into two non-empty nodes.  We can't merge
                // immediately, because we don't know which of the two nodes will
                // get an additional ref.
                // If the unreferenced node of the pair gets the new reference,
                // there no possibility for a merge.  Otherwise, we have to split
                // again on the already referenced node and end up here.
                if (edge == ElementEdge.BeforeStart)
                {
                    // If node B has no references it can merge with node A.
                    // <A ""/><B "x"/><newNode ""/><this "x".>
                    mergeNode = node.GetPreviousNode() as TextTreeTextNode;
                }
                else
                {
                    // If node A has no references it can merge with node B.
                    // <this "x"/><newNode ""/><A "x"/><B ""/>
                    mergeNode = node.GetNextNode() as TextTreeTextNode;
                }
                if (mergeNode != null && mergeNode._positionRefCount == 0)
                {
                    mergeNode.Merge();
                }
            }
            else
            {
                node = this;
                _referencedEdge = edge;
                _positionRefCount += delta;
            }

            return node;
        }

        // Decrements a count of the number of TextPositions and TextNavigators
        // referencing this node.
        // This method attempts to merge adjacent TextTreeTextNodes when the ref count
        // transitions from 1 to 0.
        //
        // Be careful!  This can modify the tree, removing
        // TextTreeTextNodes.
        internal override void DecrementReferenceCount(ElementEdge edge)
        {
            Invariant.Assert(edge == _referencedEdge, "Bad edge decrement!");

            _positionRefCount--;
            Invariant.Assert(_positionRefCount >= 0, "Bogus PositionRefCount! ");

            // If the ref count drops to zero, we may be able to merge this node with an adjacent one.            
            if (_positionRefCount == 0)
            {
                Merge();
            }
        }

        // Splits this node into two adjacent nodes.
        // Returns the node which contains the original requested edge
        // (e.g., if edge == ElementEdge.BeforeStart, this method returns the
        // preceding node, if edge == ElementEdge.AfterEnd, it returns the
        // following node).
        internal TextTreeTextNode Split(int localOffset, ElementEdge edge)
        {
            TextTreeTextNode newNode;
            TextTreeTextNode edgeNode;
            ElementEdge newNodeEdge;

            Invariant.Assert(_symbolCount > 0, "Splitting a zero-width TextNode!");
            Invariant.Assert(localOffset >= 0 && localOffset <= _symbolCount, "Bad localOffset!");
            Invariant.Assert(edge == ElementEdge.BeforeStart || edge == ElementEdge.AfterEnd, "Bad edge parameter!");

#if DEBUG
            if (localOffset == 0)
            {
                TextTreeNode previousNode;

                Invariant.Assert(edge == ElementEdge.BeforeStart, "Unexpected edge!");
                Invariant.Assert(edge != _referencedEdge, "Splitting at referenced edge!");

                previousNode = (TextTreeNode)GetPreviousNode();
                Invariant.Assert(previousNode == null || previousNode.SymbolCount > 0 || previousNode.AfterEndReferenceCount,
                             "Found preceding zero-width text node inside Split!");
            }
            else if (localOffset == _symbolCount)
            {
                TextTreeNode nextNode;

                Invariant.Assert(edge == ElementEdge.AfterEnd, "Unexpected edge!");
                Invariant.Assert(edge != _referencedEdge, "Splitting at referenced edge!");

                nextNode = (TextTreeNode)GetNextNode();
                Invariant.Assert(nextNode == null || nextNode.SymbolCount > 0 || nextNode.BeforeStartReferenceCount,
                             "Found following zero-width text node inside Split! (2)");
            }
#endif // DEBUG

            newNode = new TextTreeTextNode();
            newNode._generation = _generation;

            // Splay this node to the root so we don't corrupt any LeftSymbolCounts
            // of ancestor nodes when we fixup _symbolCount below.
            Splay();

            if (_positionRefCount > 0 && _referencedEdge == ElementEdge.BeforeStart)
            {
                // New node is the following node.
                newNode._symbolOffsetCache = (_symbolOffsetCache == -1) ? -1 : _symbolOffsetCache + localOffset;
                newNode._symbolCount = _symbolCount - localOffset;

                _symbolCount = localOffset;

                newNodeEdge = ElementEdge.AfterEnd;

                edgeNode = (edge == ElementEdge.BeforeStart) ? this : newNode;
            }
            else
            {
                // New node is the preceding node.
                newNode._symbolOffsetCache = _symbolOffsetCache;
                newNode._symbolCount = localOffset;

                _symbolOffsetCache = (_symbolOffsetCache == -1) ? -1 : _symbolOffsetCache + localOffset;
                _symbolCount -= localOffset;

                newNodeEdge = ElementEdge.BeforeStart;

                edgeNode = (edge == ElementEdge.BeforeStart) ? newNode : this;
            }

            Invariant.Assert(_symbolCount >= 0);
            Invariant.Assert(newNode._symbolCount >= 0);

            newNode.InsertAtNode(this, newNodeEdge);

            return edgeNode;            
        }

        #endregion Internal methods

        //------------------------------------------------------
        //
        //  Internal Properties
        //
        //------------------------------------------------------

        #region Internal Properties

        // If this node is a local root, then ParentNode contains it.
        // Otherwise, this is the node parenting this node within its tree.
        internal override SplayTreeNode ParentNode
        {
            get
            {
                return _parentNode;
            }
            
            set
            {
                _parentNode = (TextTreeNode)value;
            }
        }

        // TextTreeTextNode never has contained nodes.
        internal override SplayTreeNode ContainedNode
        {
            get
            {
                return null;
            }
            
            set
            {
                Invariant.Assert(false, "Can't set child on a TextTreeTextNode!");
            }
        }

        // Count of symbols of all siblings preceding this node.
        internal override int LeftSymbolCount
        {
            get
            {
                return _leftSymbolCount;
            }

            set
            {
                _leftSymbolCount = value;
            }
        }

        // Count of chars of all siblings preceding this node.
        internal override int LeftCharCount
        {
            get
            {
                return _leftCharCount;
            }

            set
            {
                _leftCharCount = value;
            }
        }

        // Left child node in a sibling tree.
        internal override SplayTreeNode LeftChildNode
        {
            get
            {
                return _leftChildNode;
            }
            
            set
            {
                _leftChildNode = (TextTreeNode)value;
            }
        }

        // Right child node in a sibling tree.
        internal override SplayTreeNode RightChildNode
        {
            get
            {
                return _rightChildNode;
            }
            
            set
            {
                _rightChildNode = (TextTreeNode)value;
            }
        }

        // The TextContainer's generation when SymbolOffsetCache was last updated.
        // If the current generation doesn't match TextContainer.Generation, then
        // SymbolOffsetCache is invalid.
        internal override uint Generation
        {
            get
            {
                return _generation;
            }
            
            set
            {
                _generation = value;
            }
        }

        // Cached symbol offset.
        internal override int SymbolOffsetCache
        {
            get
            {
                return _symbolOffsetCache;
            }
            
            set
            {
                _symbolOffsetCache = value;
            }
        }

        // Count of symbols covered by this node.
        internal override int SymbolCount
        {
            get
            {
                return _symbolCount;
            }
            
            set
            {
                _symbolCount = value;
            }
        }

        // Count of chars covered by this node.
        internal override int IMECharCount
        {
            get
            {
                return this.SymbolCount;
            }

            set
            {
                // Tracked redundently by _symbolCount.
            }
        }

        // Count of TextPositions referencing the node's BeforeStart edge.
        internal override bool BeforeStartReferenceCount
        {
            get
            {
                return _referencedEdge == ElementEdge.BeforeStart ? _positionRefCount > 0 : false;
            }

            set
            {
                Invariant.Assert(false, "Can't set TextTreeTextNode ref counts directly!");
            }
        }

        // Count of TextPositions referencing the node's AfterStart edge.
        // Since text nodes don't have an AfterStart edge, this is always zero.
        internal override bool AfterStartReferenceCount
        {
            get
            {
                return false;
            }

            set
            {
                Invariant.Assert(false, "Text nodes don't have an AfterStart edge!");
            }
        }

        // Count of TextPositions referencing the node's BeforeEnd edge.
        // Since text nodes don't have an BeforeEnd edge, this is always zero.
        internal override bool BeforeEndReferenceCount
        {
            get
            {
                return false;
            }

            set
            {
                Invariant.Assert(false, "Text nodes don't have a BeforeEnd edge!");
            }
        }

        // Count of TextPositions referencing the node's AfterEnd edge.
        internal override bool AfterEndReferenceCount
        {
            get
            {
                return _referencedEdge == ElementEdge.AfterEnd ? _positionRefCount > 0 : false;
            }

            set
            {
                Invariant.Assert(false, "Can't set TextTreeTextNode ref counts directly!");
            }
        }

        #endregion Internal Properties

        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------

        #region Private Methods

        // Attempts to merge this node with adjacent TextTreeTextNodes.
        // Called when the position ref counts drops from 1 to 0.
        private void Merge()
        {
            TextTreeTextNode previousNode;
            TextTreeTextNode nextNode;

            Invariant.Assert(_positionRefCount == 0, "Inappropriate Merge call!");
            
            // Check the previous node.
            previousNode = GetPreviousNode() as TextTreeTextNode;

            if (previousNode != null &&
                (previousNode._positionRefCount == 0 || previousNode._referencedEdge == ElementEdge.BeforeStart))
            {
                // The previous node must take the place of this one, since previous
                // may still have references.
                Remove();
                // null _parentNode out so that if there's a bug and someone still references this node we'll hear about it.
                _parentNode = null;
                previousNode.Splay();

                previousNode._symbolCount += _symbolCount;
            }
            else
            {
                previousNode = this;
            }
            
            // Check the following node.
            nextNode = previousNode.GetNextNode() as TextTreeTextNode;

            if (nextNode != null)
            {
                if (previousNode._positionRefCount == 0 &&
                    (nextNode._positionRefCount == 0 || (nextNode._referencedEdge == ElementEdge.AfterEnd)))
                {
                    // nextNode must take the place of previousNode, since nextNode
                    // may still have references.
                    previousNode.Remove();
                    // null _parentNode out so that if there's a bug and someone still references this node we'll hear about it.
                    previousNode._parentNode = null;
                    nextNode.Splay();

                    if (nextNode._symbolOffsetCache != -1)
                    {
                        nextNode._symbolOffsetCache -= previousNode._symbolCount;
                    }
                    nextNode._symbolCount += previousNode._symbolCount;
                }
                else if ((previousNode._positionRefCount == 0 || previousNode._referencedEdge == ElementEdge.BeforeStart) &&
                         nextNode._positionRefCount == 0)
                {
                    // The previous node must take the place of next one, since previousNode
                    // may still have references.
                    nextNode.Remove();
                    // null _parentNode out so that if there's a bug and someone still references this node we'll hear about it.
                    nextNode._parentNode = null;
                    previousNode.Splay();

                    previousNode._symbolCount += nextNode._symbolCount;
                }
            }
        }

        #endregion Private methods

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        #region Private Fields

        // Count of symbols of all siblings preceding this node.
        private int _leftSymbolCount;

        // Count of chars of all siblings preceding this node.
        private int _leftCharCount;

        // If this node is a local root, then ParentNode contains it.
        // Otherwise, this is the node parenting this node within its tree.
        private TextTreeNode _parentNode;

        // Left child node in a sibling tree.
        private TextTreeNode _leftChildNode;

        // Right child node in a sibling tree.
        private TextTreeNode _rightChildNode; // we could combine with _parentNode, if we can accept the increased complexity.

        // The TextContainer's generation when SymbolOffsetCache was last updated.
        // If the current generation doesn't match TextContainer.Generation, then
        // SymbolOffsetCache is invalid.
        private uint _generation;

        // Cached symbol offset.
        private int _symbolOffsetCache;

        // Count of symbols/chars covered by this node.
        private int _symbolCount;

        // The number of TextPositions referencing an edge of this node.
        // If _positionRefCount is zero, it's safe to merge with surrouding nodes.
        // we could combine _positionRefCount and _referencedEdge into a lazy allocated structure.
        // this can bit a single bitflag, we don't need 32 bits.
        private int _positionRefCount;

        // The edge referenced by one or more TextPositions.
        // Only valid when _positionRefCount > 0.
        private ElementEdge _referencedEdge;

        #endregion Private Fields
    }
}
